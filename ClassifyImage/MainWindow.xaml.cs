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
        //待移动目录
        List<String> to_move_folders = new();

        private Point cropStartPoint;
        private bool isCropping = false;
        private CroppedBitmap croppedBitmap;


        public MainWindow()
        {
            InitializeComponent();
            if (!ClassifyImage.Settings.Default.MyGO_easter_egg_check)
            {
                now_display_img.Source = null;
            }

        }



        private void Image_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //放大图片
            if (sender == img_plus)
            {

            }
            //缩小图片
            else if (sender == img_minus)
            {

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
            if (now_display_img.Source == null)
            {
                MessageBox.Show("没有可编辑的图片");
                return;
            }

            // 进入裁剪模式
            StartCropMode();

        }

        private void StartCropMode()
        {
            // 显示裁剪相关控件
            cropCanvas.Visibility = Visibility.Visible;
            btnCancelCrop.Visibility = Visibility.Visible;
            btnConfirmCrop.Visibility = Visibility.Visible;

            // 禁用其他按钮
            edit_img_btn.IsEnabled = false;
            setting_btn.IsEnabled = false;
            open_img_btn.IsEnabled = false;
            open_file_folders_btn.IsEnabled = false;
            left_btn.IsEnabled = false;
            right_btn.IsEnabled = false;

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
            UpdateResizeThumbsPosition();


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

            isInitialized = false;
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
                var x = (int)Canvas.GetLeft(cropRectangle);
                var y = (int)Canvas.GetTop(cropRectangle);
                var width = (int)cropRectangle.Width;
                var height = (int)cropRectangle.Height;

                // 获取原始图片
                var source = (BitmapSource)now_display_img.Source;

                // 计算裁剪比例（从显示大小映射到原始图片大小）
                double scaleX = source.PixelWidth / now_display_img.ActualWidth;
                double scaleY = source.PixelHeight / now_display_img.ActualHeight;

                // 计算实际裁剪区域
                int actualX = (int)(x * scaleX);
                int actualY = (int)(y * scaleY);
                int actualWidth = (int)(width * scaleX);
                int actualHeight = (int)(height * scaleY);

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

                // 显示裁剪后的图片
                now_display_img.Source = croppedBitmap;

                // 保存裁剪后的图片
                SaveCroppedImage();

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

            // 生成保存路径
            string originalPath = img_paths[now_img_index];
            string directory = Path.GetDirectoryName(originalPath);
            string fileName = Path.GetFileNameWithoutExtension(originalPath);
            string extension = Path.GetExtension(originalPath);
            //string newPath = Path.Combine(directory, $"{fileName}_cropped{extension}");
            string newPath = img_paths[now_img_index];

            // 确保文件名唯一
            //int counter = 1;
            //while (File.Exists(newPath))
            //{
            //    newPath = Path.Combine(directory, $"{fileName}_cropped_{counter}{extension}");
            //    counter++;
            //}

            // 保存图片
            using (var fileStream = new FileStream(newPath, FileMode.Create))
            {
                BitmapEncoder encoder = extension.ToLower() switch
                {
                    ".jpg" or ".jpeg" => new JpegBitmapEncoder(),
                    ".png" => new PngBitmapEncoder(),
                    ".bmp" => new BmpBitmapEncoder(),
                    _ => new PngBitmapEncoder()
                };

                encoder.Frames.Add(BitmapFrame.Create(croppedBitmap));
                encoder.Save(fileStream);
            }

            // 更新当前图片路径
            img_paths[now_img_index] = newPath;
            now_img_path = newPath;

            // 更新显示信息
            now_img_resolution_text.Text = $"{croppedBitmap.PixelHeight}x{croppedBitmap.PixelWidth}";
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
            SettingWindow settingWindow = new();
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
                img_paths = new List<string>([filename]);
                now_img_index = 0;
                UpdataDisplayImg();
            }
        }

        private void open_explorer_btn_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", img_paths[now_img_index]);
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
                UpdataDisplayImg();
            }
        }
        //上一张图片
        private void LeftImg()
        {
            now_img_index = (now_img_index + img_paths.Count - 1) % img_paths.Count;
            UpdataDisplayImg();
            //GC.Collect();
        }
        //下一张图片
        private void RightImg()
        {
            if (ClassifyImage.Settings.Default.default_path_check)
            {
                if (ClassifyImage.Settings.Default.default_path != "")
                {
                    FileInfo now_file_info = new FileInfo(img_paths[now_img_index]);
                    if (now_file_info.DirectoryName == now_folder_path)
                    {
                        var old_index = now_img_index;
                        var to_path = $"{ClassifyImage.Settings.Default.default_path}\\{new FileInfo(img_paths[old_index]).Name}";
                        if (!File.Exists(to_path))
                            File.Move(img_paths[old_index], to_path);
                        img_paths[old_index] = to_path;
                    }
                }
                else
                {
                    MessageBox.Show($"未设置下一步默认保存路径，请到软件配置中配置");
                    return;
                }
            }
            now_img_index = (now_img_index + 1) % img_paths.Count;
            UpdataDisplayImg();
            //GC.Collect();

        }

        //更新显示图片
        private void UpdataDisplayImg()
        {
            Title = $"({now_img_index + 1}/{img_paths.Count}){img_paths[now_img_index]}";
            now_img_path = img_paths[now_img_index];
            BitmapImage now_bit_map_img = Tools.LoadBitmapImage(now_img_path);
            if (now_bit_map_img == null)
            {
                return;
            }
            now_display_img.Source = now_bit_map_img;
            now_img_resolution_text.Text = $"{now_bit_map_img.Height}x{now_bit_map_img.Width}";
            now_img_size_text.Text = $"{new FileInfo(now_img_path).Length / 1024}KB";
        }
        //快捷键捕捉
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            //MessageBox.Show($"您按下了键:{e.KeyStates}");
            //同时按下
            // if (e.KeyStates == Keyboard.GetKeyStates(Key.C) && Keyboard.Modifiers == ModifierKeys.Alt)
            if (e.Key == Key.Left)
            {
                LeftImg();
            }
            else if (e.Key == Key.Right)
            {
                RightImg();
            }

            if (!ClassifyImage.Settings.Default.mut_kind_check)
            {
                if ((e.Key == Key.D0 || e.Key == Key.NumPad0))
                {
                    ProSavePath(0);

                }
                else if ((e.Key == Key.D1 || e.Key == Key.NumPad1))
                {
                    ProSavePath(1);

                }
                else if ((e.Key == Key.D2 || e.Key == Key.NumPad2))
                {
                    ProSavePath(2);

                }
                else if ((e.Key == Key.D4 || e.Key == Key.NumPad4))
                {
                    ProSavePath(4);

                }
                else if ((e.Key == Key.D5 || e.Key == Key.NumPad5))
                {
                    ProSavePath(5);

                }
                else if ((e.Key == Key.D6 || e.Key == Key.NumPad6))
                {
                    ProSavePath(6);
                }
                else if ((e.Key == Key.D7 || e.Key == Key.NumPad7))
                {
                    ProSavePath(7);

                }
                else if ((e.Key == Key.D8 || e.Key == Key.NumPad8))
                {

                    ProSavePath(8);
                }
                else if ((e.Key == Key.D9 || e.Key == Key.NumPad9))
                {

                    ProSavePath(9);
                }



            }
            void ProSavePath(int num)
            {
                String KeyPath = "";
                switch (num)
                {
                    case 0:
                        KeyPath = ClassifyImage.Settings.Default.KeyPath0;
                        break;
                    case 1:
                        KeyPath = ClassifyImage.Settings.Default.KeyPath1;
                        break;
                    case 2:
                        KeyPath = ClassifyImage.Settings.Default.KeyPath2;
                        break;
                    case 3:
                        KeyPath = ClassifyImage.Settings.Default.KeyPath3;
                        break;
                    case 4:
                        KeyPath = ClassifyImage.Settings.Default.KeyPath4;
                        break;
                    case 5:
                        KeyPath = ClassifyImage.Settings.Default.KeyPath5;
                        break;
                    case 6:
                        KeyPath = ClassifyImage.Settings.Default.KeyPath6;
                        break;
                    case 7:
                        KeyPath = ClassifyImage.Settings.Default.KeyPath7;
                        break;
                    case 8:
                        KeyPath = ClassifyImage.Settings.Default.KeyPath8;
                        break;
                    case 9:
                        KeyPath = ClassifyImage.Settings.Default.KeyPath9;
                        break;
                    default:
                        KeyPath = ".";
                        break;
                }
                if (KeyPath != "")
                {
                    if (!ClassifyImage.Settings.Default.auto_next_check)
                    {
                        var old_index = now_img_index;
                        var to_path = $"{KeyPath}\\{new FileInfo(img_paths[old_index]).Name}";
                        if (!File.Exists(to_path))
                            File.Move(img_paths[old_index], to_path);
                        img_paths[old_index] = to_path;
                        UpdataDisplayImg();
                    }
                    else
                    {
                        var old_index = now_img_index;
                        RightImg();
                        var to_path = $"{KeyPath}\\{new FileInfo(img_paths[old_index]).Name}";
                        if (!File.Exists(to_path))
                            File.Move(img_paths[old_index], to_path);
                        img_paths[old_index] = to_path;
                    }
                }
                else
                {
                    MessageBox.Show($"未设置分类{num}保存路径");
                    return;
                }

            }
        }
    }
}