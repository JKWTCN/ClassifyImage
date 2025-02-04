using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

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
        List<string> img_paths = new List<string>();
        //当前图片索引
        int now_img_index = 0;

        public MainWindow()
        {
            InitializeComponent();
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
                if (now_img_index > 0)
                {
                    now_img_index--;
                    UpdataDisplayImg();
                }

            }
            //下一张图片
            else if (sender == right_btn)
            {
                if (now_img_index < img_paths.Count - 1)
                {
                    now_img_index++;
                    UpdataDisplayImg();
                }
            }

        }

        private void edit_img_btn_Click(object sender, RoutedEventArgs e)
        {


        }

        private void setting_btn_Click(object sender, RoutedEventArgs e)
        {
            SettingWindow settingWindow = new();
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
                img_paths = new List<string>(Tools.GetImages(fullPathToFolder, ["*.jpg", "*.png", "*.bmp"]));
                now_img_index = 0;
                now_display_img.Source = new BitmapImage(new Uri(img_paths[now_img_index]));
                UpdataDisplayImg();
            }
        }
        //上一张图片
        private void LeftImg()
        {
            if (now_img_index > 0)
            {
                now_img_index--;
                UpdataDisplayImg();
            }
        }
        //下一张图片
        private void RightImg()
        {
            if (now_img_index < img_paths.Count - 1)
            {
                now_img_index++;
                UpdataDisplayImg();
            }
        }

        //更新显示图片
        private void UpdataDisplayImg()
        {
            if (now_img_index >= 0 && now_img_index < img_paths.Count)
            {
                Title = img_paths[now_img_index];
                now_display_img.Source = new BitmapImage(new Uri(img_paths[now_img_index]));
                now_img_path = img_paths[now_img_index];
                List<int> now_img_resolution = Tools.GetImageSize(now_img_path);
                now_img_resolution_text.Text = $"{now_img_resolution[0]}x{now_img_resolution[1]}";
                now_img_size_text.Text = $"{new FileInfo(now_img_path).Length / 1024}KB";
            }
        }
        //快捷键捕捉
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            //MessageBox.Show($"您按下了键:{e.KeyStates}");
            //同时按下
            // if (e.KeyStates == Keyboard.GetKeyStates(Key.C) && Keyboard.Modifiers == ModifierKeys.Alt)
            if (e.KeyStates == Keyboard.GetKeyStates(Key.A))
            {
                LeftImg();
            }
            else if (e.KeyStates == Keyboard.GetKeyStates(Key.D))
            {
                RightImg();
            }
        }
    }
}