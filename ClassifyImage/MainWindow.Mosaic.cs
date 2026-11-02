using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ClassifyImage;

public partial class MainWindow
{
    private bool isMosaicMode;
    private BitmapSource? mosaicOriginal;
    private bool isDrawingMosaic;
    private Point mosaicDrawStart;

    private void MosaicButton_Click(object sender, RoutedEventArgs e) => StartImageEdit(true);

    private void MosaicBlockSize_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (isMosaicMode && isInitialized) UpdateMosaicPreview();
    }

    private void UpdateMosaicPreview()
    {
        if (mosaicOriginal == null || cropCanvas.Width <= 0 || cropCanvas.Height <= 0) return;
        double scaleX = mosaicOriginal.PixelWidth / cropCanvas.Width;
        double scaleY = mosaicOriginal.PixelHeight / cropCanvas.Height;
        int x = Math.Clamp((int)Math.Round(Canvas.GetLeft(cropRectangle) * scaleX), 0, mosaicOriginal.PixelWidth - 1);
        int y = Math.Clamp((int)Math.Round(Canvas.GetTop(cropRectangle) * scaleY), 0, mosaicOriginal.PixelHeight - 1);
        int width = Math.Clamp((int)Math.Round(cropRectangle.Width * scaleX), 1, mosaicOriginal.PixelWidth - x);
        int height = Math.Clamp((int)Math.Round(cropRectangle.Height * scaleY), 1, mosaicOriginal.PixelHeight - y);
        // 每次从进入编辑时的原图预览，移动或缩小选区不会留下旧马赛克。
        now_display_img.Source = Tools.ApplyMosaic(mosaicOriginal, new Int32Rect(x, y, width, height), (int)mosaicBlockSize.Value);
    }

    private void SaveMosaic()
    {
        if (!isInitialized || mosaicOriginal == null) return;
        try
        {
            UpdateMosaicPreview();
            Tools.SaveBitmap((BitmapSource)now_display_img.Source, now_img_path);
            now_img_size_text.Text = $"{new FileInfo(now_img_path).Length / 1024}KB";
            mosaicOriginal = null;
            isMosaicMode = false;
            EndCropMode();
            statusText.Text = "马赛克已保存到原图";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存马赛克失败: {ex.Message}");
        }
    }

    private void SetMosaicSelection(Point start, Point end)
    {
        start.X = Math.Clamp(start.X, 0, cropCanvas.Width);
        start.Y = Math.Clamp(start.Y, 0, cropCanvas.Height);
        end.X = Math.Clamp(end.X, 0, cropCanvas.Width);
        end.Y = Math.Clamp(end.Y, 0, cropCanvas.Height);
        double left = Math.Min(Math.Min(start.X, end.X), cropCanvas.Width - 1);
        double top = Math.Min(Math.Min(start.Y, end.Y), cropCanvas.Height - 1);
        Canvas.SetLeft(cropRectangle, left);
        Canvas.SetTop(cropRectangle, top);
        cropRectangle.Width = Math.Min(Math.Max(1, Math.Abs(end.X - start.X)), cropCanvas.Width - left);
        cropRectangle.Height = Math.Min(Math.Max(1, Math.Abs(end.Y - start.Y)), cropCanvas.Height - top);
        UpdateResizeThumbsPosition();
    }

    private void MosaicCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!isMosaicMode || e.OriginalSource != cropCanvas) return;
        mosaicDrawStart = e.GetPosition(cropCanvas);
        isDrawingMosaic = cropCanvas.CaptureMouse();
        SetMosaicSelection(mosaicDrawStart, mosaicDrawStart);
        e.Handled = true;
    }

    private void MosaicCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!isDrawingMosaic) return;
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            cropCanvas.ReleaseMouseCapture();
            return;
        }
        SetMosaicSelection(mosaicDrawStart, e.GetPosition(cropCanvas));
        e.Handled = true;
    }

    private void MosaicCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!isDrawingMosaic) return;
        SetMosaicSelection(mosaicDrawStart, e.GetPosition(cropCanvas));
        isDrawingMosaic = false;
        cropCanvas.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void MosaicCanvas_LostMouseCapture(object sender, MouseEventArgs e)
    {
        if (e.OriginalSource == cropCanvas) isDrawingMosaic = false;
    }
}
