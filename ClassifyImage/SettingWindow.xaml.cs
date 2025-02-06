using System.Windows;

namespace ClassifyImage
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : Window
    {
        public SettingWindow()
        {
            InitializeComponent();
            path_0_text.Text = ClassifyImage.Settings.Default.KeyPath0;
            path_1_text.Text = ClassifyImage.Settings.Default.KeyPath1;
            path_2_text.Text = ClassifyImage.Settings.Default.KeyPath2;
            path_3_text.Text = ClassifyImage.Settings.Default.KeyPath3;
            path_4_text.Text = ClassifyImage.Settings.Default.KeyPath4;
            path_5_text.Text = ClassifyImage.Settings.Default.KeyPath5;
            path_6_text.Text = ClassifyImage.Settings.Default.KeyPath6;
            path_7_text.Text = ClassifyImage.Settings.Default.KeyPath7;
            path_8_text.Text = ClassifyImage.Settings.Default.KeyPath8;
            path_9_text.Text = ClassifyImage.Settings.Default.KeyPath9;
            auto_next_check.IsChecked = ClassifyImage.Settings.Default.auto_next_check;
            mut_kind_check.IsChecked = ClassifyImage.Settings.Default.mut_kind_check;
            control_main_setting_windows_check.IsChecked = ClassifyImage.Settings.Default.control_main_setting_windows_check;
            MyGO_easter_egg_check.IsChecked = ClassifyImage.Settings.Default.MyGO_easter_egg_check;

        }

        private void ChooseSaveClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFolderDialog dialog = new();
            dialog.Multiselect = false;
            dialog.Title = "选择图片文件夹";
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                string fullPathToFolder = dialog.FolderName;
                string folderNameOnly = dialog.SafeFolderName;
                if (sender == open_btn_0)
                {
                    path_0_text.Text = fullPathToFolder;
                    ClassifyImage.Settings.Default.KeyPath0 = fullPathToFolder;
                    ClassifyImage.Settings.Default.Save();
                }
                else if (sender == open_btn_1)
                {
                    path_1_text.Text = fullPathToFolder;
                    ClassifyImage.Settings.Default.KeyPath1 = fullPathToFolder;
                    ClassifyImage.Settings.Default.Save();
                }
                else if (sender == open_btn_2)
                {
                    path_2_text.Text = fullPathToFolder;
                    ClassifyImage.Settings.Default.KeyPath2 = fullPathToFolder;
                    ClassifyImage.Settings.Default.Save();
                }
                else if (sender == open_btn_3)
                {
                    path_3_text.Text = fullPathToFolder;
                    ClassifyImage.Settings.Default.KeyPath3 = fullPathToFolder;
                    ClassifyImage.Settings.Default.Save();
                }
                else if (sender == open_btn_4)
                {
                    path_4_text.Text = fullPathToFolder;
                    ClassifyImage.Settings.Default.KeyPath4 = fullPathToFolder;
                    ClassifyImage.Settings.Default.Save();
                }
                else if (sender == open_btn_5)
                {
                    path_5_text.Text = fullPathToFolder;
                    ClassifyImage.Settings.Default.KeyPath5 = fullPathToFolder;
                    ClassifyImage.Settings.Default.Save();
                }
                else if (sender == open_btn_6)
                {
                    path_6_text.Text = fullPathToFolder;
                    ClassifyImage.Settings.Default.KeyPath6 = fullPathToFolder;
                    ClassifyImage.Settings.Default.Save();
                }
                else if (sender == open_btn_7)
                {
                    path_7_text.Text = fullPathToFolder;
                    ClassifyImage.Settings.Default.KeyPath7 = fullPathToFolder;
                    ClassifyImage.Settings.Default.Save();
                }
                else if (sender == open_btn_8)
                {
                    path_8_text.Text = fullPathToFolder;
                    ClassifyImage.Settings.Default.KeyPath8 = fullPathToFolder;
                    ClassifyImage.Settings.Default.Save();
                }
                else if (sender == open_btn_9)
                {
                    path_9_text.Text = fullPathToFolder;
                    ClassifyImage.Settings.Default.KeyPath9 = fullPathToFolder;
                    ClassifyImage.Settings.Default.Save();
                }
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            if (sender == github_url_hyperlink)
            {
                System.Diagnostics.Process.Start("explorer.exe", "https://github.com/JKWTCN");
            }
            else if (sender == bilibili_url_hyperlink)
            {
                System.Diagnostics.Process.Start("explorer.exe", "https://space.bilibili.com/283390377");

            }
        }
        private void HandleCheck(object sender, RoutedEventArgs e)
        {
            if (sender == auto_next_check)
            {
                ClassifyImage.Settings.Default.auto_next_check = true;
            }
            else if (sender == mut_kind_check)
            {
                ClassifyImage.Settings.Default.mut_kind_check = true;
            }
            else if (sender == control_main_setting_windows_check)
            {
                ClassifyImage.Settings.Default.control_main_setting_windows_check = true;
            }
            else if (sender == MyGO_easter_egg_check)
            {
                ClassifyImage.Settings.Default.MyGO_easter_egg_check = true;
            }
            ClassifyImage.Settings.Default.Save();
        }

        private void HandleUnchecked(object sender, RoutedEventArgs e)
        {
            if (sender == auto_next_check)
            {
                ClassifyImage.Settings.Default.auto_next_check = false;
            }
            else if (sender == mut_kind_check)
            {
                ClassifyImage.Settings.Default.mut_kind_check = false;
            }
            else if (sender == control_main_setting_windows_check)
            {
                ClassifyImage.Settings.Default.control_main_setting_windows_check = false;
            }
            else if (sender == MyGO_easter_egg_check)
            {
                ClassifyImage.Settings.Default.MyGO_easter_egg_check = false;
            }
            ClassifyImage.Settings.Default.Save();
        }
    }



}
