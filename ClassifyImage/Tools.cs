using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ClassifyImage
{
    static class Tools
    {
        //加载图片
        public static BitmapImage LoadBitmapImage(String path)
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
                    System.Diagnostics.Process.Start("explorer.exe", path);
                    return null;

                }

            }
            return bitmap;
        }


        //获取图片目录的所有图片
        public static string[] GetImages(string dirPath)
        {

            DirectoryInfo TheFolder = new DirectoryInfo(dirPath);
            var files = TheFolder.GetFiles();
            List<String> img_paths = new List<string>();
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Extension.ToLower() == ".jpg" || files[i].Extension.ToLower() == ".png" || files[i].Extension.ToLower() == ".jpeg" || files[i].Extension.ToLower() == ".bmp")
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
            var witdh = bitmap.Width;
            var height = bitmap.Height;
            return new List<int> { (int)witdh, (int)height };

        }
    }
}
