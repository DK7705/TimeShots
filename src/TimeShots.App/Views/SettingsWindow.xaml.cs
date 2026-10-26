using System.Windows;
using System.Windows.Forms;

namespace TimeShots.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void OnBrowseStorageClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select TimeShots storage folder",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            TxtStoragePath.Text = dialog.SelectedPath;
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        TxtStatus.Visibility = Visibility.Visible;
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        timer.Tick += (_, _) =>
        {
            TxtStatus.Visibility = Visibility.Collapsed;
            timer.Stop();
        };
        timer.Start();
    }
}
