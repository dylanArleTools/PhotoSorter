using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Shell32;

namespace PhotoSorter
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowViewModel()
        {

        }

        private SelectedDatesCollection _sortByDayDates;
        public SelectedDatesCollection SortByDayDates
        {
            get => _sortByDayDates;
            set
            {
                if (value == _sortByDayDates) return;

                _sortByDayDates = value;
                NotifyPropertyChanged(nameof(SortByDayDates));
            }
        }

        private string _outputLabelContent = "Ready";
        public string OutputLabelContent
        {
            get => _outputLabelContent;
            set
            {
                if (value == _outputLabelContent) return;

                _outputLabelContent = value;
                NotifyPropertyChanged(nameof(OutputLabelContent));
            }
        }

        private int _progressValue = 0;
        public int ProgressValue
        {
            get => _progressValue;
            set
            {
                if (value == _progressValue) return;

                _progressValue = value;
                NotifyPropertyChanged(nameof(ProgressValue));
            }
        }

        public int Hwnd { get; private set; }

        public void SortFiles()
        {
            // Execute in an async task on a separate thread to avoid blocking UI responsiveness
            Task sortTask = Task.Run(() =>
            {
                string sourcePath = "";
                string destinationRoot = "";
                string temporaryPath = Path.Combine(Path.GetTempPath(), "PhotoSort");
                // Execute on dispatcher thread to allow access to UI and system resources
                var fileCopyResult = Application.Current.Dispatcher.Invoke(() =>
                {
                    OutputLabelContent = "Sorting...";

                    // Use Shell32 to select source folders that do not show up as a normal directory on a connected Android device
                    Folder sourceFolder;
                    Shell shell = new Shell();
                    Folder folder = shell.BrowseForFolder((int)Hwnd, "Select directory to sort", 0, 0);
                    if (folder == null)
                    {
                        OutputLabelContent = "Sort cancelled";
                        return false;
                    }
                    else
                    {
                        FolderItem folderItem = (folder as Folder3).Self;
                        sourceFolder = folderItem.GetFolder;
                        sourcePath = folderItem.Path;
                    }

                    Folder destinationFolder;
                    Folder folder2 = shell.BrowseForFolder((int)Hwnd, "Select directory to sort", 0, 0);
                    if (folder2 == null)
                    {
                        OutputLabelContent = "Sort cancelled";
                        return false;
                    }
                    else
                    {
                        FolderItem fi = (folder2 as Folder3).Self;
                        destinationFolder = fi.GetFolder;
                        destinationRoot = fi.Path;
                    }

                    try
                    {
                        Directory.CreateDirectory(temporaryPath);
                        Folder temporaryFolder = shell.NameSpace(temporaryPath.Replace(@"\\", @"\"));
                        temporaryFolder.CopyHere(sourceFolder.Items());
                    }
                    catch (Exception e)
                    {
                        OutputLabelContent = $"Copy Error: {e}";
                        if (Directory.Exists(temporaryPath))
                        {
                            Directory.Delete(temporaryPath, true);
                        }
                        return false;
                    }

                    return true;
                });
                
                if (!fileCopyResult)
                {
                    return false;
                }

                string[] sourceFiles = Directory.GetFiles(temporaryPath, "*.jpg", SearchOption.AllDirectories);
                string duplicateRoot = Path.Combine(destinationRoot, $"Duplicates");
                if (Directory.Exists(duplicateRoot))
                {
                    Directory.Delete(duplicateRoot, true);
                }

                int copied = 0;
                int duplicates = 0;
                double sortCount = sourceFiles.Count();
                double sortIndex = 1.0;
                foreach (string file in sourceFiles)
                {
                    // Execute on dispatcher thread to allow updates to the UI
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OutputLabelContent = $"Sorting file {sortIndex} of {sortCount}";
                        ProgressValue = (int)((sortIndex / sortCount) * 100);
                    });

                    DateTime date = File.GetLastWriteTime(file);
                    string monthDirectoryName = date.ToString("MM-yyyy");
                    string destinationPath = Path.Combine(destinationRoot, monthDirectoryName);
                    Directory.CreateDirectory(destinationPath);
                    string dayDirectoryName = date.ToString("dd");
                    if (DoesSortByDayDatesContain(date))
                    {
                        destinationPath = Path.Combine(destinationPath, dayDirectoryName);
                        Directory.CreateDirectory(destinationPath);
                    }

                    if (File.Exists(Path.Combine(destinationPath, Path.GetFileName(file))))
                    {
                        string duplicateDirectory = Path.Combine(destinationRoot, $"Duplicates\\{monthDirectoryName}");
                        Directory.CreateDirectory(duplicateDirectory);
                        if (DoesSortByDayDatesContain(date))
                        {
                            duplicateDirectory = Path.Combine(duplicateDirectory, dayDirectoryName);
                            Directory.CreateDirectory(duplicateDirectory);
                        }
                        File.Copy(file, Path.Combine(duplicateDirectory, Path.GetFileName(file)), true);
                        duplicates++;
                    }
                    else
                    {
                        File.Copy(file, Path.Combine(destinationPath, Path.GetFileName(file)));
                        copied++;
                    }

                    sortIndex++;
                }

                // Execute on dispatcher thread to allow updates to the UI
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OutputLabelContent = $"{copied} files copied. {duplicates} duplicates already in the destination.";
                });

                Directory.Delete(temporaryPath, true);
                // Open Windows Explorer to display the destination directory
                Process.Start(destinationRoot);
                return true;
            }).ContinueWith(
            (t) =>
            {
                // Execute on dispatcher thread to allow updates to the UI
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OutputLabelContent = $"Task Error: {t.Exception.Message}";
                });
            },
            TaskContinuationOptions.OnlyOnFaulted);
        }

        private bool DoesSortByDayDatesContain(DateTime dateTime)
        {
            if (SortByDayDates == null) return false;

            return SortByDayDates.Contains(dateTime.Date);
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
