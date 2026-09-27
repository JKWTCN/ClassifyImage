using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ClassifyImage
{
    public partial class MainWindow
    {
        private readonly HashSet<Key> heldClassificationKeys = new();
        private readonly HashSet<int> selectedCategories = new();

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (imageScale == null || isInitialized) return;
            EndImagePan();
            imageScale.ScaleX = imageScale.ScaleY = e.NewValue / 100;
        }

        private void ImageViewer_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateImageSize();

        private void UpdateImageSize()
        {
            if (isInitialized || imageViewer == null || now_display_img?.Source is not BitmapSource source) return;
            double width = Math.Max(1, imageViewer.ActualWidth - 20);
            double height = Math.Max(1, imageViewer.ActualHeight - 20);
            double scale = Math.Min(width / source.PixelWidth, height / source.PixelHeight);
            now_display_img.Width = source.PixelWidth * scale;
            now_display_img.Height = source.PixelHeight * scale;
        }

        private static int GetCategory(Key key)
        {
            if (key >= Key.D0 && key <= Key.D9) return key - Key.D0;
            if (key >= Key.NumPad0 && key <= Key.NumPad9) return key - Key.NumPad0;
            return -1;
        }

        private void ClearClassificationKeys()
        {
            heldClassificationKeys.Clear();
            selectedCategories.Clear();
        }

        private void UpdateCropSizeInputs()
        {
            if (now_display_img.Source is not BitmapSource source) return;
            cropWidthInput.Text = Math.Max(1, (int)Math.Round(cropRectangle.Width * source.PixelWidth / cropCanvas.Width)).ToString();
            cropHeightInput.Text = Math.Max(1, (int)Math.Round(cropRectangle.Height * source.PixelHeight / cropCanvas.Height)).ToString();
        }

        private void ApplyCropSize_Click(object sender, RoutedEventArgs e)
        {
            if (!isInitialized || now_display_img.Source is not BitmapSource source) return;
            if (!int.TryParse(cropWidthInput.Text, out int width) ||
                !int.TryParse(cropHeightInput.Text, out int height) ||
                width < 1 || height < 1 || width > source.PixelWidth || height > source.PixelHeight)
            {
                MessageBox.Show($"请输入有效尺寸：宽度 1～{source.PixelWidth}，高度 1～{source.PixelHeight} 像素");
                return;
            }
            cropRectangle.Width = width * cropCanvas.Width / source.PixelWidth;
            cropRectangle.Height = height * cropCanvas.Height / source.PixelHeight;
            System.Windows.Controls.Canvas.SetLeft(cropRectangle,
                Math.Min(System.Windows.Controls.Canvas.GetLeft(cropRectangle), cropCanvas.Width - cropRectangle.Width));
            System.Windows.Controls.Canvas.SetTop(cropRectangle,
                Math.Min(System.Windows.Controls.Canvas.GetTop(cropRectangle), cropCanvas.Height - cropRectangle.Height));
            UpdateResizeThumbsPosition();
        }

        private void Window_Deactivated(object? sender, EventArgs e)
        {
            ClearClassificationKeys();
            EndImagePan();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (isInitialized || img_paths.Count == 0 || Keyboard.Modifiers != ModifierKeys.None) return;
            int category = GetCategory(e.Key);
            if (category < 0 || e.IsRepeat) return;
            heldClassificationKeys.Add(e.Key);
            selectedCategories.Add(category);
            e.Handled = true;
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (isInitialized)
            {
                if (e.Key == Key.Escape) EndCropMode();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Left) { LeftImg(); e.Handled = true; return; }
            if (e.Key == Key.Right) { RightImg(); e.Handled = true; return; }
            int category = GetCategory(e.Key);
            if (category < 0 || !heldClassificationKeys.Remove(e.Key)) return;
            e.Handled = true;
            if (Keyboard.Modifiers != ModifierKeys.None)
            {
                ClearClassificationKeys();
                return;
            }
            if (Settings.Default.mut_kind_check && heldClassificationKeys.Count > 0) return;
            int[] categories = Settings.Default.mut_kind_check
                ? selectedCategories.OrderBy(value => value).ToArray()
                : new[] { category };
            ClearClassificationKeys();
            ClassifyCurrentImage(categories);
        }

        private void ClassifyCurrentImage(int[] categories)
        {
            if (img_paths.Count == 0 || now_display_img.Source == null || categories.Length == 0) return;
            try
            {
                // 先检查全部配置，避免漏配类别时只保存了一部分。
                string[] destinations = categories.Select(category =>
                {
                    string path = (string)Settings.Default[$"KeyPath{category}"];
                    if (string.IsNullOrWhiteSpace(path))
                        throw new InvalidOperationException($"未设置分类{category}保存路径");
                    return Path.GetFullPath(path);
                }).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

                if (Settings.Default.mut_kind_check)
                {
                    // 多类别保存保留原图，允许同一图片属于多个类别。
                    foreach (string destination in destinations)
                        Tools.TransferImage(now_img_path, destination, copy: true);
                }
                else
                {
                    if (!MoveCurrentImage(destinations[0])) return;
                }
                if (Settings.Default.auto_next_check) AdvanceImage();
                else UpdataDisplayImg();
                statusText.Text = Settings.Default.mut_kind_check
                    ? $"已复制到分类 {string.Join("、", categories)}"
                    : $"已移动到分类 {categories[0]}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"分类失败: {ex.Message}");
            }
        }

        private bool MoveCurrentImage(string destination)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(destination))
                    throw new InvalidOperationException("未设置保存路径，请到软件配置中配置");
                string newPath = Tools.TransferImage(now_img_path, destination, copy: false);
                img_paths[now_img_index] = newPath;
                now_img_path = newPath;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移动失败: {ex.Message}");
                return false;
            }
        }
    }
}
