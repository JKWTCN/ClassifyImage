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
        List<string> img_paths = new();
        //当前图片索引
        int now_img_index = 0;
        //待移动目录
        List<String> to_move_folders = new();
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

        private void edit_img_btn_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", img_paths[now_img_index]);

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