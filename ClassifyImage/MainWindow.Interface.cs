using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ClassifyImage;

public partial class MainWindow
{
    private bool updatingQuickOptions;
    private SettingWindow? activeSettingsWindow;
    private readonly List<Button> categoryButtons = new();

    private void InitializeInterface()
    {
        for (int category = 0; category < 10; category++)
        {
            var button = new Button
            {
                Tag = category, Margin = new Thickness(0, 0, 0, 6), Padding = new Thickness(10, 8, 10, 8),
                HorizontalContentAlignment = HorizontalAlignment.Stretch, Focusable = false
            };
            button.Click += CategoryButton_Click;
            categoryButtons.Add(button);
            categoryPanel.Children.Add(button);
        }
        Settings.Default.PropertyChanged += InterfaceSettingsChanged;
        Closed += (_, _) => Settings.Default.PropertyChanged -= InterfaceSettingsChanged;
        RefreshCategoryButtons();
        RefreshImageInterface();
    }

    private void InterfaceSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 配置窗口允许并行操作时，目录和模式立即同步到主窗口。
        if (Dispatcher.CheckAccess()) RefreshCategoryButtons();
        else Dispatcher.BeginInvoke(new Action(RefreshCategoryButtons));
    }

    private void RefreshCategoryButtons()
    {
        updatingQuickOptions = true;
        quickAutoNext.IsChecked = Settings.Default.auto_next_check;
        quickMultiCategory.IsChecked = Settings.Default.mut_kind_check;
        updatingQuickOptions = false;
        classificationHint.Text = Settings.Default.mut_kind_check
            ? "同时按住多个数字键，全部松开后复制；也可点击单个类别"
            : "按数字键或点击类别，将图片移动到对应目录";
        foreach (var button in categoryButtons)
        {
            int category = (int)button.Tag;
            string path = (string)Settings.Default[$"KeyPath{category}"];
            string name = string.IsNullOrWhiteSpace(path) ? "未设置目录" : Path.GetFileName(path.TrimEnd('\\', '/'));
            if (string.IsNullOrWhiteSpace(name)) name = path;
            if (button.Content is not Grid content)
            {
                content = new Grid();
                content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
                content.ColumnDefinitions.Add(new ColumnDefinition());
                content.Children.Add(new TextBlock { Text = category.ToString(), FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)FindResource("AccentBrush"), VerticalAlignment = VerticalAlignment.Center });
                var label = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(label, 1);
                content.Children.Add(label);
                button.Content = content;
            }
            ((TextBlock)content.Children[1]).Text = name;
            button.ToolTip = string.IsNullOrWhiteSpace(path) ? $"请在配置中设置分类 {category} 的目录" : $"数字键 {category}\n{path}";
            button.IsEnabled = !isInitialized && img_paths.Count > 0 && now_display_img.Source != null && !string.IsNullOrWhiteSpace(path);
        }
    }

    private void RefreshImageInterface()
    {
        bool loaded = img_paths.Count > 0 && now_display_img.Source != null;
        bool hasPreview = now_display_img.Source != null;
        bool hasImages = img_paths.Count > 0;
        fileNameText.Text = hasImages ? Path.GetFileName(now_img_path) : hasPreview ? "启动预览" : "尚未打开图片";
        imageCounter.Text = hasImages ? $"{now_img_index + 1} / {img_paths.Count}" : "";
        fileNameText.ToolTip = hasImages ? now_img_path : null;
        imageActions.IsEnabled = loaded && !isInitialized;
        left_btn.IsEnabled = right_btn.IsEnabled = hasImages && !isInitialized;
        emptyStateTitle.Text = hasImages ? "图片已移动、删除或无法打开" : "准备好下一组图片了吗？";
        emptyStateHint.Text = hasImages ? "图片可能已删除或移动，可使用左右方向键继续浏览" : "打开图片文件夹，使用数字键快速分类";
        emptyState.Visibility = hasPreview ? Visibility.Collapsed : Visibility.Visible;
        imageViewer.Visibility = hasPreview ? Visibility.Visible : Visibility.Collapsed;
        cropToolbar.Visibility = isInitialized ? Visibility.Visible : Visibility.Collapsed;
        imageBrowseTools.Visibility = isInitialized ? Visibility.Collapsed : Visibility.Visible;
        RefreshCategoryButtons();
    }

    private void QuickOption_Changed(object sender, RoutedEventArgs e)
    {
        if (updatingQuickOptions || categoryButtons.Count == 0) return;
        if (sender == quickAutoNext) Settings.Default.auto_next_check = quickAutoNext.IsChecked == true;
        else if (sender == quickMultiCategory) Settings.Default.mut_kind_check = quickMultiCategory.IsChecked == true;
        ClearClassificationKeys();
        Settings.Default.Save();
    }

    private void CategoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (isInitialized) return;
        ClearClassificationKeys();
        ClassifyCurrentImage(new[] { (int)((Button)sender).Tag });
    }

    private void NavigationButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender == left_btn) LeftImg();
        else RightImg();
    }

    private void ZoomButton_Click(object sender, RoutedEventArgs e)
    {
        if (isInitialized) return;
        zoomSlider.Value += sender == img_plus ? zoomSlider.SmallChange : -zoomSlider.SmallChange;
    }

    private void ResetZoom_Click(object sender, RoutedEventArgs e)
    {
        if (!isInitialized) zoomSlider.Value = 100;
    }

    private void ImageViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control || isInitialized) return;
        zoomSlider.Value += Math.Sign(e.Delta) * zoomSlider.SmallChange;
        e.Handled = true;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.IsRepeat || isInitialized || e.OriginalSource is TextBox) return;
        var modifiers = Keyboard.Modifiers;
        if (e.Key == Key.O && modifiers == ModifierKeys.Control) open_img_btn_Click(sender, new RoutedEventArgs());
        else if (e.Key == Key.O && modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) open_file_folders_btn_Click(sender, new RoutedEventArgs());
        else if (e.Key == Key.F2 && modifiers == ModifierKeys.None) setting_btn_Click(sender, new RoutedEventArgs());
        else if (e.Key == Key.E && modifiers == ModifierKeys.None && img_paths.Count > 0) edit_img_btn_Click(sender, new RoutedEventArgs());
        else if (e.Key == Key.C && modifiers == ModifierKeys.Control && img_paths.Count > 0) copy_clipboard_btn_Click(sender, new RoutedEventArgs());
        else return;
        e.Handled = true;
    }
}
