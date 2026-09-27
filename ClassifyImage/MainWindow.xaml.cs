using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace ClassifyImage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //当前文件夹位置
        string now_folder_path = "";
        //当前图片路径
        string now_img_path = "";
        //文件夹中图片
        List<string> img_paths = new();
        //当前图片索引
        int now_img_index = 0;

        private CroppedBitmap? croppedBitmap;


        public MainWindow()
        {
            InitializeComponent();
            if (!ClassifyImage.Settings.Default.MyGO_easter_egg_check)
            {
                now_display_img.Source = null;
            }
            InitializeInterface();

        }



        private void Image_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //放大图片
            if (sender == img_plus)
            {
                zoomSlider.Value += zoomSlider.SmallChange;
            }
            //缩小图片
            else if (sender == img_minus)
            {
                zoomSlider.Value -= zoomSlider.SmallChange;
            }
            //上一张图片
            else if (sender == left_btn)
            {
                LeftImg();

            }
            //下一张图片
            else if (sender == right_btn)
            {
                RightImg();
            }

        }
        private const double MinCropSize = 50;
        private bool isInitialized = false;
        private void edit_img_btn_Click(object sender, RoutedEventArgs e)
        {
            //System.Diagnostics.Process.Start("explorer.exe", img_paths[now_img_index]);
            if (img_paths.Count == 0 || now_display_img.Source == null)
            {
                MessageBox.Show("没有可编辑的图片");
                return;
            }

            // 进入裁剪模式
            StartCropMode();

        }

        private void StartCropMode()
        {
            EndImagePan();
            // 显示裁剪相关控件
            cropCanvas.Visibility = Visibility.Visible;
            cropSizePanel.Visibility = Visibility.Visible;
            btnCancelCrop.Visibility = Visibility.Visible;
            btnConfirmCrop.Visibility = Visibility.Visible;

            // 禁用其他按钮
            edit_img_btn.IsEnabled = false;
            setting_btn.IsEnabled = false;
            open_img_btn.IsEnabled = false;
            open_file_folders_btn.IsEnabled = false;
            left_btn.IsEnabled = false;
            right_btn.IsEnabled = false;

            ClearClassificationKeys();
            zoomSlider.IsEnabled = false;
            img_plus.IsEnabled = false;
            img_minus.IsEnabled = false;

            // 初始化裁剪画布大小
            cropCanvas.Width = now_display_img.ActualWidth;
            cropCanvas.Height = now_display_img.ActualHeight;


            cropRectangle.Width = now_display_img.ActualWidth;
            cropRectangle.Height = now_display_img.ActualHeight;
            Canvas.SetLeft(cropRectangle, 0);
            Canvas.SetTop(cropRectangle, 0);
            cropRectangle.Visibility = Visibility.Visible;


            // 确保所有Thumb可见
            foreach (var thumb in new[] { topLeftThumb, topThumb, topRightThumb,
                    leftThumb, rightThumb,
                    bottomLeftThumb, bottomThumb, bottomRightThumb })
            {
                thumb.Visibility = Visibility.Visible;
            }

            // 立即更新Thumb位置
            isInitialized = true;
            UpdatePanCursor();
            UpdateResizeThumbsPosition();
            RefreshImageInterface();


        }
        // 更新调整大小的Thumb位置
        private void UpdateResizeThumbsPosition()
        {
            if (!isInitialized) return;

            double left = Canvas.GetLeft(cropRectangle);
            double top = Canvas.GetTop(cropRectangle);
            double right = left + cropRectangle.Width;
            double bottom = top + cropRectangle.Height;
            double centerX = left + cropRectangle.Width / 2;
            double centerY = top + cropRectangle.Height / 2;

            // 四个角
            SetThumbPosition(topLeftThumb, left - topLeftThumb.Width / 2, top - topLeftThumb.Height / 2);
            SetThumbPosition(topRightThumb, right - topRightThumb.Width / 2, top - topRightThumb.Height / 2);
            SetThumbPosition(bottomLeftThumb, left - bottomLeftThumb.Width / 2, bottom - bottomLeftThumb.Height / 2);
            SetThumbPosition(bottomRightThumb, right - bottomRightThumb.Width / 2, bottom - bottomRightThumb.Height / 2);

            // 四条边
            SetThumbPosition(topThumb, centerX - topThumb.Width / 2, top - topThumb.Height / 2);
            SetThumbPosition(bottomThumb, centerX - bottomThumb.Width / 2, bottom - bottomThumb.Height / 2);
            SetThumbPosition(leftThumb, left - leftThumb.Width / 2, centerY - leftThumb.Height / 2);
            SetThumbPosition(rightThumb, right - rightThumb.Width / 2, centerY - rightThumb.Height / 2);
            UpdateCropSizeInputs();
        }

        private void SetThumbPosition(Thumb thumb, double left, double top)
        {
            // 确保Thumb不会超出画布范围
            //left = Math.Max(0, Math.Min(left, cropCanvas.ActualWidth - thumb.Width));
            //top = Math.Max(0, Math.Min(top, cropCanvas.ActualHeight - thumb.Height));
            Canvas.SetLeft(thumb, left);
            Canvas.SetTop(thumb, top);
            thumb.Visibility = Visibility.Visible; // 确保Thumb可见
        }

        // 移动裁剪框
        private void CropThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            double newLeft = Canvas.GetLeft(cropRectangle) + e.HorizontalChange;
            double newTop = Canvas.GetTop(cropRectangle) + e.VerticalChange;

            // 限制移动范围
            newLeft = Math.Max(0, Math.Min(newLeft, cropCanvas.ActualWidth - cropRectangle.Width));
            newTop = Math.Max(0, Math.Min(newTop, cropCanvas.ActualHeight - cropRectangle.Height));

            Canvas.SetLeft(cropRectangle, newLeft);
            Canvas.SetTop(cropRectangle, newTop);

            UpdateResizeThumbsPosition();
        }

        // 调整裁剪框大小
        private void ResizeThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            string thumbPosition = (string)((Thumb)sender).Tag;
            double left = Canvas.GetLeft(cropRectangle);
            double top = Canvas.GetTop(cropRectangle);
            double width = cropRectangle.Width;
            double height = cropRectangle.Height;

            switch (thumbPosition)
            {
                case "TopLeft":
                    left += e.HorizontalChange;
                    top += e.VerticalChange;
                    width -= e.HorizontalChange;
                    height -= e.VerticalChange;
                    break;
                case "Top":
                    top += e.VerticalChange;
                    height -= e.VerticalChange;
                    break;
                case "TopRight":
                    top += e.VerticalChange;
                    width += e.HorizontalChange;
                    height -= e.VerticalChange;
                    break;
                case "Left":
                    left += e.HorizontalChange;
                    width -= e.HorizontalChange;
                    break;
                case "Right":
                    width += e.HorizontalChange;
                    break;
                case "BottomLeft":
                    left += e.HorizontalChange;
                    width -= e.HorizontalChange;
                    height += e.VerticalChange;
                    break;
                case "Bottom":
                    height += e.VerticalChange;
                    break;
                case "BottomRight":
                    width += e.HorizontalChange;
                    height += e.VerticalChange;
                    break;
            }

            // 限制最小尺寸
            if (width < MinCropSize)
            {
                if (thumbPosition == "TopLeft" || thumbPosition == "Left" || thumbPosition == "BottomLeft")
                    left -= (MinCropSize - width);
                width = MinCropSize;
            }

            if (height < MinCropSize)
            {
                if (thumbPosition == "TopLeft" || thumbPosition == "Top" || thumbPosition == "TopRight")
                    top -= (MinCropSize - height);
                height = MinCropSize;
            }

            // 限制在画布范围内
            if (left < 0)
            {
                width += left;
                left = 0;
            }

            if (top < 0)
            {
                height += top;
                top = 0;
            }

            if (left + width > cropCanvas.ActualWidth)
            {
                width = cropCanvas.ActualWidth - left;
            }

            if (top + height > cropCanvas.ActualHeight)
            {
                height = cropCanvas.ActualHeight - top;
            }

            // 应用新尺寸和位置
            Canvas.SetLeft(cropRectangle, left);
            Canvas.SetTop(cropRectangle, top);
            cropRectangle.Width = width;
            cropRectangle.Height = height;

            UpdateResizeThumbsPosition();
        }
        private void EndCropMode()
        {
            // 隐藏裁剪相关控件
            cropCanvas.Visibility = Visibility.Collapsed;
            cropSizePanel.Visibility = Visibility.Collapsed;
            cropRectangle.Visibility = Visibility.Collapsed;
            btnCancelCrop.Visibility = Visibility.Collapsed;
            btnConfirmCrop.Visibility = Visibility.Collapsed;

            // 隐藏所有调整大小的Thumb
            foreach (var thumb in new[] { topLeftThumb, topThumb, topRightThumb,
                                leftThumb, rightThumb,
                                bottomLeftThumb, bottomThumb, bottomRightThumb })
            {
                thumb.Visibility = Visibility.Collapsed;
            }

            // 启用其他按钮
            edit_img_btn.IsEnabled = true;
            setting_btn.IsEnabled = true;
            open_img_btn.IsEnabled = true;
            open_file_folders_btn.IsEnabled = true;
            left_btn.IsEnabled = true;
            right_btn.IsEnabled = true;

            zoomSlider.IsEnabled = true;
            img_plus.IsEnabled = true;
            img_minus.IsEnabled = true;
            _isDragging = false;
            cropRectangle.ReleaseMouseCapture();
            isInitialized = false;
            UpdatePanCursor();
            UpdateImageSize();
            RefreshImageInterface();
        }
        private void btnConfirmCrop_Click(object sender, RoutedEventArgs e)
        {
            if (!isInitialized || cropRectangle.Width == 0 || cropRectangle.Height == 0)
            {
                MessageBox.Show("请先调整裁剪区域");
                return;
            }

            try
            {
                // 获取裁剪区域
                var x = Canvas.GetLeft(cropRectangle);
                var y = Canvas.GetTop(cropRectangle);
                var width = cropRectangle.Width;
                var height = cropRectangle.Height;

                // 获取原始图片
                var source = (BitmapSource)now_display_img.Source;

                // 计算裁剪比例（从显示大小映射到原始图片大小）
                double scaleX = source.PixelWidth / now_display_img.ActualWidth;
                double scaleY = source.PixelHeight / now_display_img.ActualHeight;

                // 计算实际裁剪区域
                int actualX = (int)(x * scaleX);
                int actualY = (int)(y * scaleY);
                int actualWidth = Math.Max(1, (int)Math.Round(width * scaleX));
                int actualHeight = Math.Max(1, (int)Math.Round(height * scaleY));

                // 确保裁剪区域在图片范围内
                if (actualX < 0) actualX = 0;
                if (actualY < 0) actualY = 0;
                if (actualX + actualWidth > source.PixelWidth)
                    actualWidth = source.PixelWidth - actualX;
                if (actualY + actualHeight > source.PixelHeight)
                    actualHeight = source.PixelHeight - actualY;

                // 执行裁剪
                croppedBitmap = new CroppedBitmap(source,
                    new System.Windows.Int32Rect(actualX, actualY, actualWidth, actualHeight));

                SaveCroppedImage();
                now_display_img.Source = croppedBitmap;

                EndCropMode();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"裁剪失败: {ex.Message}");
                EndCropMode();
            }
        }
        private void SaveCroppedImage()
        {
            if (croppedBitmap == null) return;

            string newPath = img_paths[now_img_index];
            Tools.SaveBitmap(croppedBitmap, newPath);
            now_img_path = newPath;
            now_img_size_text.Text = $"{new FileInfo(newPath).Length / 1024}KB";

            //MessageBox.Show($"图片已保存到: {newPath}");
        }
        private Point _dragStart;
        private bool _isDragging;

        private void CropRectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragStart = e.GetPosition(cropCanvas);
            cropRectangle.CaptureMouse();
        }

        private void CropRectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            Point currentPos = e.GetPosition(cropCanvas);
            double offsetX = currentPos.X - _dragStart.X;
            double offsetY = currentPos.Y - _dragStart.Y;

            double newLeft = Canvas.GetLeft(cropRectangle) + offsetX;
            double newTop = Canvas.GetTop(cropRectangle) + offsetY;

            // 限制移动范围
            newLeft = Math.Max(0, Math.Min(newLeft, cropCanvas.ActualWidth - cropRectangle.Width));
            newTop = Math.Max(0, Math.Min(newTop, cropCanvas.ActualHeight - cropRectangle.Height));

            Canvas.SetLeft(cropRectangle, newLeft);
            Canvas.SetTop(cropRectangle, newTop);

            _dragStart = currentPos;

            // 更新Thumb位置
            UpdateResizeThumbsPosition();
        }

        private void CropRectangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            cropRectangle.ReleaseMouseCapture();

            // 确保最终位置正确
            UpdateResizeThumbsPosition();
        }
        private void btnCancelCrop_Click(object sender, RoutedEventArgs e)
        {
            EndCropMode();
        }
        private void setting_btn_Click(object sender, RoutedEventArgs e)
        {
            if (activeSettingsWindow != null)
            {
                activeSettingsWindow.Activate();
                return;
            }
            SettingWindow settingWindow = new() { Owner = this };
            activeSettingsWindow = settingWindow;
            settingWindow.Closed += (_, _) => activeSettingsWindow = null;
            if (ClassifyImage.Settings.Default.control_main_setting_windows_check)
                settingWindow.Show();
            else
                settingWindow.ShowDialog();
        }

        private void open_img_btn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = ".."; // Default file name
            dialog.DefaultExt = ".jpg"; // Default file extension
            dialog.Filter = "图片|*.jpg;*.jpeg;*.bmp;*.jfif;*.png;";
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                string filename = dialog.FileName;
                now_folder_path = Path.GetDirectoryName(filename) ?? "";
                img_paths = new List<string>([filename]);
                now_img_index = 0;
                ClearClassificationKeys();
                zoomSlider.Value = 100;
                UpdataDisplayImg();
            }
        }

        private void open_explorer_btn_Click(object sender, RoutedEventArgs e)
        {
            if (img_paths.Count == 0) return;
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(now_img_path) { UseShellExecute = true });
            }
            catch (Exception ex) { MessageBox.Show($"打开失败: {ex.Message}"); }
        }

        private async void copy_clipboard_btn_Click(object sender, RoutedEventArgs e)
        {
            if (now_display_img.Source == null)
            {
                MessageBox.Show("没有可复制的图片");
                return;
            }

            try
            {
                // 获取当前显示的图片
                var bitmapSource = now_display_img.Source as BitmapSource;

                if (bitmapSource != null)
                {
                    // 剪贴板可能被其他程序短暂占用，遇到该情况时稍后重试。
                    await CopyImageToClipboardWithRetryAsync(bitmapSource);
                    statusText.Text = "图片已复制到剪贴板";
                }
                else
                {
                    MessageBox.Show("无法复制图片");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败: {ex.Message}");
            }
        }

        private static async Task CopyImageToClipboardWithRetryAsync(BitmapSource bitmapSource)
        {
            const int clipboardCannotOpenHResult = unchecked((int)0x800401D0);
            const int maxAttempts = 10;

            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    Clipboard.SetImage(bitmapSource);
                    return;
                }
                catch (System.Runtime.InteropServices.COMException ex)
                    when (ex.HResult == clipboardCannotOpenHResult && attempt < maxAttempts)
                {
                    await Task.Delay(25 * attempt);
                }
            }
        }

        private void open_file_folders_btn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFolderDialog dialog = new();
            dialog.Multiselect = false;
            dialog.Title = "选择图片文件夹";
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                string fullPathToFolder = dialog.FolderName;
                string folderNameOnly = dialog.SafeFolderName;
                now_folder_path = fullPathToFolder;
                img_paths = new List<string>(Tools.GetImages(fullPathToFolder));
                now_img_index = 0;
                ClearClassificationKeys();
                zoomSlider.Value = 100;
                UpdataDisplayImg();
            }
        }
        private void LeftImg()
        {
            if (isInitialized || img_paths.Count == 0) return;
            ClearClassificationKeys();
            now_img_index = (now_img_index + img_paths.Count - 1) % img_paths.Count;
            UpdataDisplayImg();
        }

        private void RightImg()
        {
            if (isInitialized || img_paths.Count == 0) return;
            ClearClassificationKeys();
            if (now_display_img.Source != null && Settings.Default.default_path_check &&
                string.Equals(Path.GetDirectoryName(now_img_path), now_folder_path, StringComparison.OrdinalIgnoreCase))
            {
                if (!MoveCurrentImage(Settings.Default.default_path)) return;
            }
            AdvanceImage();
        }

        private void AdvanceImage()
        {
            now_img_index = (now_img_index + 1) % img_paths.Count;
            UpdataDisplayImg();
        }

        //更新显示图片
        private void UpdataDisplayImg()
        {
            EndImagePan();
            if (img_paths.Count == 0)
            {
                now_img_path = "";
                now_display_img.Source = null;
                now_img_size_text.Text = "";
                Title = "图片分类器（文件夹中没有图片）";
                RefreshImageInterface();
                return;
            }
            try
            {
                Title = $"({now_img_index + 1}/{img_paths.Count}){img_paths[now_img_index]}";
                now_img_path = img_paths[now_img_index];
                BitmapImage? now_bit_map_img = Tools.LoadBitmapImage(now_img_path);
                if (now_bit_map_img == null)
                {
                    now_display_img.Source = null;
                    now_img_size_text.Text = "";
                    RefreshImageInterface();
                    return;
                }
                now_display_img.Source = now_bit_map_img;
                UpdateImageSize();
                now_img_size_text.Text = $"{new FileInfo(now_img_path).Length / 1024}KB";
                RefreshImageInterface();
            }
            catch (Exception)
            {
                now_display_img.Source = null;
                now_img_size_text.Text = "";
                RefreshImageInterface();
                MessageBox.Show($"打开失败: 图片可能已移动。");

            }

        }
    }
}
