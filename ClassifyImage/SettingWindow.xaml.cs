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
                }
                else if (sender == open_btn_1)
                {
                    path_1_text.Text = fullPathToFolder;
                }
                else if (sender == open_btn_2)
                {
                    path_2_text.Text = fullPathToFolder;
                }
                else if (sender == open_btn_3)
                {
                    path_3_text.Text = fullPathToFolder;
                }
                else if (sender == open_btn_4)
                {
                    path_4_text.Text = fullPathToFolder;
                }
                else if (sender == open_btn_5)
                {
                    path_5_text.Text = fullPathToFolder;
                }
                else if (sender == open_btn_6)
                {
                    path_6_text.Text = fullPathToFolder;
                }
                else if (sender == open_btn_7)
                {
                    path_7_text.Text = fullPathToFolder;
                }
                else if (sender == open_btn_8)
                {
                    path_8_text.Text = fullPathToFolder;
                }
                else if (sender == open_btn_9)
                {
                    path_9_text.Text = fullPathToFolder;
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
    }
}
