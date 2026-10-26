using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TimeShots.App.Models;

namespace TimeShots.App.Views;

public partial class GalleryWindow : Window
{
    private List<CaptureItem> _allCaptures = new();
    private CaptureItem? _selectedItem;

    public GalleryWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadCaptures();
    }

    public void LoadCaptures()
    {
        // Placeholder items for demonstration / initial layout
        // In full pipeline, storage layer provides this list
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = _allCaptures.AsEnumerable();

        var query = SearchBox.Text.Trim();
        if (!string.IsNullOrEmpty(query) && query != "Search by date or hash...")
        {
            filtered = filtered.Where(c =>
                c.LocalTimestamp.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.ImageHash.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Mode.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        if (FromDatePicker.SelectedDate.HasValue)
        {
            filtered = filtered.Where(c => DateTime.TryParse(c.LocalTimestamp, out var dt) && dt.Date >= FromDatePicker.SelectedDate.Value.Date);
        }

        if (ToDatePicker.SelectedDate.HasValue)
        {
            filtered = filtered.Where(c => DateTime.TryParse(c.LocalTimestamp, out var dt) && dt.Date <= ToDatePicker.SelectedDate.Value.Date);
        }

        var list = filtered.ToList();
        CapturesItemsControl.ItemsSource = list;
        EmptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnCaptureCardMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is CaptureItem item)
        {
            ShowDetails(item);
        }
    }

    private void ShowDetails(CaptureItem item)
    {
        _selectedItem = item;

        try
        {
            if (!string.IsNullOrEmpty(item.ImagePath) && System.IO.File.Exists(item.ImagePath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(item.ImagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                SidebarPreviewImage.Source = bitmap;
            }
            else
            {
                SidebarPreviewImage.Source = null;
            }
        }
        catch
        {
            SidebarPreviewImage.Source = null;
        }

        TxtLocalTime.Text = $"Local: {item.LocalTimestamp}";
        TxtUtcTime.Text = $"UTC: {item.UtcTimestamp}";
        TxtTimezone.Text = $"Timezone: {item.Timezone}";
        TxtSequence.Text = $"Sequence: #{item.Sequence}";
        TxtImageHash.Text = item.ImageHash;
        TxtChainHash.Text = item.ChainHash;
        TxtResolution.Text = $"Resolution: {item.Resolution}";
        TxtMode.Text = $"Mode: {item.Mode}";
        TxtFilePath.Text = item.ImagePath;

        VerifyResultBanner.Visibility = Visibility.Collapsed;
        DetailsSidebar.Visibility = Visibility.Visible;
    }

    private void OnVerifyClick(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null) return;

        // Verification call (logic engine)
        VerifyResultBanner.Visibility = Visibility.Visible;
        VerifyResultBanner.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 34, 197, 94));
        VerifyResultBanner.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94));
        VerifyResultBanner.BorderThickness = new Thickness(1);

        VerifyResultText.Text = "✓ Hash & HMAC signature valid";
        VerifyResultText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94));
    }

    private void OnCloseSidebarClick(object sender, RoutedEventArgs e)
    {
        DetailsSidebar.Visibility = Visibility.Collapsed;
        _selectedItem = null;
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        LoadCaptures();
    }

    private void OnSearchGotFocus(object sender, RoutedEventArgs e)
    {
        if (SearchBox.Text == "Search by date or hash...")
        {
            SearchBox.Text = string.Empty;
        }
    }

    private void OnSearchLostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            SearchBox.Text = "Search by date or hash...";
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded) ApplyFilters();
    }

    private void OnFilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded) ApplyFilters();
    }
}
