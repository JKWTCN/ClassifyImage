using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ClassifyImage
{
    static class Tools
    {
        //加载图片
        public static BitmapImage? LoadBitmapImage(String path)
        {
            BitmapImage bitmap = new BitmapImage();
            using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(path)))
            {
                try
                {
                    bitmap = new BitmapImage();
                    //bitmap.DecodePixelHeight = 100;
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;//设置缓存模式
                    bitmap.StreamSource = ms;//通过StreamSource加载图片
                    bitmap.EndInit();
                    bitmap.Freeze();
                }
                catch (System.Exception e)
                {
                    MessageBox.Show($"错误：{path}，{e.Message}");
                    return null;

                }

            }
            return bitmap;
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
                if (files[i].Extension.ToLowerInvariant() is ".jpg" or ".png" or ".jpeg" or ".bmp" or ".jfif")
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
            if (bitmap == null) throw new InvalidOperationException("无法读取图片尺寸");
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
