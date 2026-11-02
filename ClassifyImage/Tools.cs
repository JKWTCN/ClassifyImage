using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClassifyImage
{
    static class Tools
    {
        public const string ImageFileFilter = "图片|*.jpg;*.jpeg;*.bmp;*.jfif;*.png;*.gif;*.webp";

        public static bool IsGifOrWebP(string path) =>
            Path.GetExtension(path).ToLowerInvariant() is ".gif" or ".webp";

        //加载图片
        public static BitmapImage LoadBitmapImage(String path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            try
            {
                return DecodeBitmap(BitmapCreateOptions.None);
            }
            catch (IOException)
            {
                // 某些图片的颜色信息无法被 WPF 读取，保留像素并跳过颜色配置重试。
                return DecodeBitmap(BitmapCreateOptions.IgnoreColorProfile);
            }

            BitmapImage DecodeBitmap(BitmapCreateOptions options)
            {
                using var stream = new MemoryStream(bytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = options;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }


        //获取图片目录的所有图片
        public static string[] GetImages(string dirPath)
        {

            DirectoryInfo TheFolder = new DirectoryInfo(dirPath);
            var files = TheFolder.GetFiles().OrderByDescending(f => f.CreationTime).ToArray();
            switch (ClassifyImage.Settings.Default.file_order_combo)
            {
                case 0:
                    //按创建时间降序排序
                    files = TheFolder.GetFiles().OrderByDescending(f => f.CreationTime).ToArray();
                    break;
                case 1:
                    //按创建时间升序排序
                    files = TheFolder.GetFiles().OrderBy(f => f.CreationTime).ToArray();
                    break;
                case 2:
                    //按文件名降序排序
                    files = TheFolder.GetFiles().OrderByDescending(f => f.Name).ToArray();
                    break;
                case 3:
                    //按文件名升序排序
                    files = TheFolder.GetFiles().OrderBy(f => f.Name).ToArray();
                    break;
                case 4:
                    //按大小降序排序
                    files = TheFolder.GetFiles().OrderByDescending(f => f.Length).ToArray();
                    break;
                case 5:
                    //按大小升序排序
                    files = TheFolder.GetFiles().OrderBy(f => f.Length).ToArray();
                    break;
                case 6:
                    //按最后修改时间降序排序
                    files = TheFolder.GetFiles().OrderByDescending(f => f.LastWriteTime).ToArray();
                    break;
                case 7:
                    //按最后修改时间升序排序
                    files = TheFolder.GetFiles().OrderBy(f => f.LastWriteTime).ToArray();
                    break;
            }


            List<String> img_paths = new List<string>();
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Extension.ToLowerInvariant() is ".jpg" or ".png" or ".jpeg" or ".bmp" or ".jfif" or ".gif" or ".webp")
                {
                    img_paths.Add(files[i].FullName);
                }
            }
            return img_paths.ToArray();
        }
        //获取指定图片的分辨率
        public static List<int> GetImageSize(string imagePath)
        {
            var bitmap = LoadBitmapImage(imagePath);
            var witdh = bitmap.PixelWidth;
            var height = bitmap.PixelHeight;
            return new List<int> { (int)witdh, (int)height };

        }

        // 同名目标使用数字后缀，文件操作成功后调用方再更新图片路径。
        public static string TransferImage(string source, string destinationFolder, bool copy)
        {
            source = Path.GetFullPath(source);
            destinationFolder = Path.GetFullPath(destinationFolder);
            Directory.CreateDirectory(destinationFolder);
            string destination = Path.Combine(destinationFolder, Path.GetFileName(source));
            if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase)) return source;
            string name = Path.GetFileNameWithoutExtension(source);
            string extension = Path.GetExtension(source);
            int suffix = 1;
            while (File.Exists(destination) || Directory.Exists(destination))
                destination = Path.Combine(destinationFolder, $"{name}_{suffix++}{extension}");
            if (copy) File.Copy(source, destination);
            else File.Move(source, destination);
            return destination;
        }

        public static BitmapSource ApplyMosaic(BitmapSource source, Int32Rect area, int blockSize)
        {
            if (blockSize < 2) throw new ArgumentOutOfRangeException(nameof(blockSize));
            if (area.X < 0 || area.Y < 0 || area.Width <= 0 || area.Height <= 0 ||
                area.X > source.PixelWidth - area.Width || area.Y > source.PixelHeight - area.Height)
                throw new ArgumentOutOfRangeException(nameof(area));

            var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            int stride = checked(source.PixelWidth * 4);
            byte[] pixels = new byte[checked(stride * source.PixelHeight)];
            converted.CopyPixels(pixels, stride, 0);
            for (int top = area.Y; top < area.Y + area.Height; top += blockSize)
            {
                int bottom = Math.Min(top + blockSize, area.Y + area.Height);
                for (int left = area.X; left < area.X + area.Width; left += blockSize)
                {
                    int right = Math.Min(left + blockSize, area.X + area.Width);
                    long blue = 0, green = 0, red = 0, alpha = 0;
                    for (int y = top; y < bottom; y++)
                        for (int x = left; x < right; x++)
                        {
                            int offset = y * stride + x * 4;
                            int a = pixels[offset + 3];
                            blue += pixels[offset] * a;
                            green += pixels[offset + 1] * a;
                            red += pixels[offset + 2] * a;
                            alpha += a;
                        }
                    // 按透明度加权，避免透明像素的隐藏颜色污染方块颜色。
                    byte b = alpha == 0 ? (byte)0 : (byte)(blue / alpha);
                    byte g = alpha == 0 ? (byte)0 : (byte)(green / alpha);
                    byte r = alpha == 0 ? (byte)0 : (byte)(red / alpha);
                    byte averageAlpha = (byte)(alpha / ((long)(right - left) * (bottom - top)));
                    for (int y = top; y < bottom; y++)
                        for (int x = left; x < right; x++)
                        {
                            int offset = y * stride + x * 4;
                            pixels[offset] = b;
                            pixels[offset + 1] = g;
                            pixels[offset + 2] = r;
                            pixels[offset + 3] = averageAlpha;
                        }
                }
            }
            var result = BitmapSource.Create(source.PixelWidth, source.PixelHeight, source.DpiX, source.DpiY,
                PixelFormats.Bgra32, null, pixels, stride);
            result.Freeze();
            return result;
        }

        // 先完整编码到同目录临时文件，避免保存失败时损坏原图。
        public static void SaveBitmap(BitmapSource bitmap, string path)
        {
            BitmapEncoder encoder = Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" or ".jfif" => new JpegBitmapEncoder(),
                ".png" => new PngBitmapEncoder(),
                ".bmp" => new BmpBitmapEncoder(),
                _ => throw new NotSupportedException("不支持该图片保存格式")
            };
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            string temporaryPath = path + $".{Guid.NewGuid():N}.tmp";
            try
            {
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew))
                    encoder.Save(stream);
                File.Replace(temporaryPath, path, null);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }
    }
}
