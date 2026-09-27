using System.Windows;
using System.Windows.Input;

namespace ClassifyImage;

public partial class MainWindow
{
    private bool isPanning;
    private Point panStartPoint;
    private double panStartHorizontalOffset;
    private double panStartVerticalOffset;

    private bool CanPanImage => !isInitialized && now_display_img?.Source != null && imageViewer != null &&
        (imageViewer.ScrollableWidth > 0 || imageViewer.ScrollableHeight > 0);

    private void UpdatePanCursor()
    {
        if (imageViewer == null) return;
        imageViewer.Cursor = isPanning ? Cursors.SizeAll : CanPanImage ? Cursors.Hand : null;
    }

    private bool BeginImagePan(Point position)
    {
        if (!CanPanImage) return false;
        panStartPoint = position;
        panStartHorizontalOffset = imageViewer.HorizontalOffset;
        panStartVerticalOffset = imageViewer.VerticalOffset;
        isPanning = true;
        UpdatePanCursor();
        return true;
    }

    private void MoveImagePan(Point position)
    {
        if (!isPanning) return;
        // 使用未缩放的视口坐标，拖动距离不受图片缩放倍数影响。
        Vector movement = position - panStartPoint;
        imageViewer.ScrollToHorizontalOffset(Math.Clamp(panStartHorizontalOffset - movement.X, 0, imageViewer.ScrollableWidth));
        imageViewer.ScrollToVerticalOffset(Math.Clamp(panStartVerticalOffset - movement.Y, 0, imageViewer.ScrollableHeight));
    }

    private void EndImagePan()
    {
        isPanning = false;
        if (imageViewer?.IsMouseCaptured == true) imageViewer.ReleaseMouseCapture();
        UpdatePanCursor();
    }

    private void DisplayImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!BeginImagePan(e.GetPosition(imageViewer))) return;
        if (!imageViewer.CaptureMouse())
        {
            EndImagePan();
            return;
        }
        e.Handled = true;
    }

    private void ImageViewer_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!isPanning) return;
        if (e.LeftButton != MouseButtonState.Pressed) { EndImagePan(); return; }
        MoveImagePan(e.GetPosition(imageViewer));
        e.Handled = true;
    }

    private void ImageViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!isPanning) return;
        EndImagePan();
        e.Handled = true;
    }

    private void ImageViewer_LostMouseCapture(object sender, MouseEventArgs e) => EndImagePan();

    private void ImageViewer_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e) => UpdatePanCursor();
}
