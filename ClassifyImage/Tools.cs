using System.IO;
using System.Windows.Media.Imaging;

namespace ClassifyImage
{
    static class Tools
    {
        //获取图片目录的所有图片
        public static string[]? GetImages(string dirPath, params string[] searchPatterns)
        {
            if (searchPatterns.Length <= 0)
            {
                return null;
            }
            else
            {
                DirectoryInfo di = new DirectoryInfo(dirPath);
                FileInfo[][] fis = new FileInfo[searchPatterns.Length][];
                int count = 0;
                for (int i = 0; i < searchPatterns.Length; i++)
                {
                    FileInfo[] fileInfos = di.GetFiles(searchPatterns[i]);
                    fis[i] = fileInfos;
                    count += fileInfos.Length;
                }
                string[] files = new string[count];
                int n = 0;
                for (int i = 0; i <= fis.GetUpperBound(0); i++)
                {
                    for (int j = 0; j < fis[i].Length; j++)
                    {
                        string temp = fis[i][j].FullName;
                        files[n] = temp;
                        n++;
                    }
                }
                return files;
            }
        }
        //获取指定图片的分辨率
        public static List<int> GetImageSize(string imagePath)
        {
            var bitmap = new BitmapImage(new Uri(imagePath));
            var witdh = bitmap.Width;
            var height = bitmap.Height;
            return new List<int> { (int)witdh, (int)height };

        }
    }
}
