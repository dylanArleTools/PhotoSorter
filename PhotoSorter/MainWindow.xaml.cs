using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace PhotoSorter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            Title = "Photo Sorter";
            sortByDayCalendar.SelectedDatesChanged += SortByDayCalendar_SelectedDatesChanged;
        }

        private void SortByDayCalendar_SelectedDatesChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!(DataContext is MainWindowViewModel vm)) return;

            vm.SortByDayDates = sortByDayCalendar.SelectedDates;

            // Prevents multiple clicks from being requited to activate control
            Mouse.Capture(null);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is MainWindowViewModel vm)) return;

            vm.SortFiles();
        }
    }
}
