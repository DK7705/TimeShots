using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Point = System.Windows.Point;

namespace TimeShots.App.Views;

public partial class RegionOverlayWindow : Window
{
    private Point _startPoint;
    private bool _isDragging;

    public event Action<Rectangle>? RegionSelected;

    public RegionOverlayWindow()
    {
        InitializeComponent();

        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            _startPoint = e.GetPosition(this);
            _isDragging = true;

            Canvas.SetLeft(SelectionRect, _startPoint.X);
            Canvas.SetTop(SelectionRect, _startPoint.Y);
            SelectionRect.Width = 0;
            SelectionRect.Height = 0;
            SelectionRect.Visibility = Visibility.Visible;
            DimensionBadge.Visibility = Visibility.Visible;
        }
        else if (e.ChangedButton == MouseButton.Right)
        {
            Close();
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        var currentPoint = e.GetPosition(this);

        var x = Math.Min(_startPoint.X, currentPoint.X);
        var y = Math.Min(_startPoint.Y, currentPoint.Y);
        var width = Math.Abs(currentPoint.X - _startPoint.X);
        var height = Math.Abs(currentPoint.Y - _startPoint.Y);

        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = width;
        SelectionRect.Height = height;

        DimensionText.Text = $"{(int)width} × {(int)height}";
        Canvas.SetLeft(DimensionBadge, x);
        Canvas.SetTop(DimensionBadge, Math.Max(0, y - 25));
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;

        var endPoint = e.GetPosition(this);
        var x = (int)Math.Min(_startPoint.X, endPoint.X);
        var y = (int)Math.Min(_startPoint.Y, endPoint.Y);
        var width = (int)Math.Abs(endPoint.X - _startPoint.X);
        var height = (int)Math.Abs(endPoint.Y - _startPoint.Y);

        Close();

        if (width > 5 && height > 5)
        {
            var screenX = (int)(Left + x);
            var screenY = (int)(Top + y);
            RegionSelected?.Invoke(new Rectangle(screenX, screenY, width, height));
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}
