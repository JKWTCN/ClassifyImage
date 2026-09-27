using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClassifyImage;

internal static class Program
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static object Invoke(object instance, string method, params object[] arguments) =>
        instance.GetType().GetMethod(method, Private)!.Invoke(instance, arguments)!;
    private static T Field<T>(object instance, string field) => (T)instance.GetType().GetField(field, Private)!.GetValue(instance)!;
    private static void SetField(object instance, string field, object value) => instance.GetType().GetField(field, Private)!.SetValue(instance, value);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Console.WriteLine("PASS " + message);
    }

    [STAThread]
    private static void Main(string[] args)
    {
        string root = Path.Combine(Path.GetTempPath(), "ClassifyImage-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var application = new Application();
            object settings = typeof(MainWindow).Assembly.GetType("ClassifyImage.Settings")!
                .GetProperty("Default", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
            void Setting(string name, object value) => settings.GetType().GetProperty(name)!.SetValue(settings, value);
            Setting("MyGO_easter_egg_check", false);
            Setting("auto_next_check", false);
            Setting("mut_kind_check", false);
            Setting("default_path_check", false);
            var window = new MainWindow();
            void Layout(double width = 1000, double height = 700)
            {
                var content = (FrameworkElement)window.Content;
                content.Measure(new Size(width, height));
                content.Arrange(new Rect(0, 0, width, height));
                content.UpdateLayout();
            }
            void Screenshot(Window target, string name, int width, int height)
            {
                if (!args.Contains("--screenshots")) return;
                var content = (FrameworkElement)target.Content;
                content.Measure(new Size(width, height));
                content.Arrange(new Rect(0, 0, width, height));
                content.UpdateLayout();
                var rendered = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                rendered.Render(content);
                var png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(rendered));
                string output = Path.Combine(Path.GetTempPath(), "ClassifyImage-ui-review");
                Directory.CreateDirectory(output);
                using (var stream = File.Create(Path.Combine(output, name + ".png"))) png.Save(stream);
                Console.WriteLine("SCREENSHOT " + Path.Combine(output, name + ".png"));
            }
            void Load(string path)
            {
                SetField(window, "img_paths", new List<string> { path });
                SetField(window, "now_img_index", 0);
                SetField(window, "now_folder_path", Path.GetDirectoryName(path)!);
                Invoke(window, "UpdataDisplayImg");
                Layout();
            }
            Layout();
            Invoke(window, "LeftImg");
            Invoke(window, "RightImg");
            Check(Field<int>(window, "now_img_index") == 0, "empty navigation is safe");
            Check(!Field<Button>(window, "edit_img_btn").IsEnabled &&
                Field<StackPanel>(window, "emptyState").Visibility == Visibility.Visible,
                "empty state disables unavailable image actions");
            Screenshot(window, "main-empty", 1160, 740);

            var image = Field<Image>(window, "now_display_img");
            var resolution = Field<TextBlock>(window, "now_img_resolution_text");
            foreach (double dpi in new[] { 72.0, 96.0, 144.0, 300.0 })
            {
                // 641 pixels at 300 DPI has a fractional WPF logical width.
                image.Source = BitmapSource.Create(641, 359, dpi, dpi, PixelFormats.Bgr32,
                    null, new byte[641 * 359 * 4], 641 * 4);
                Layout();
                Check(resolution.Text == "641x359", $"resolution uses integer pixels at {dpi} DPI");
            }
            image.Source = null;
            Layout();
            Check(resolution.Text == "", "resolution clears when image is cleared");

            string original = Path.Combine(root, "sample.png");
            var bitmap = BitmapSource.Create(200, 100, 300, 300, PixelFormats.Bgr32, null, new byte[200 * 100 * 4], 200 * 4);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = File.Create(original)) encoder.Save(stream);
            Load(original);
            Check(resolution.Text == "200x100", "loaded high-DPI image displays pixel dimensions");
            string category0 = Path.Combine(root, "category0");
            string category1 = Path.Combine(root, "category1");
            Setting("KeyPath0", category0);
            Setting("KeyPath1", category1);
            Setting("mut_kind_check", true);
            var categoryButtons = Field<List<Button>>(window, "categoryButtons");
            Check(categoryButtons.Count == 10 && categoryButtons[0].IsEnabled && !categoryButtons[2].IsEnabled,
                "sidebar enables configured categories only");
            Check(Field<CheckBox>(window, "quickMultiCategory").IsChecked == true,
                "sidebar synchronizes classification settings");
            Layout(860, 560);
            var actions = Field<StackPanel>(window, "imageActions");
            var filename = Field<TextBlock>(window, "fileNameText");
            Screenshot(window, "main-compact", 860, 560);
            Check(filename.ActualWidth > 0 && actions.ActualWidth + actions.Margin.Left + actions.Margin.Right + 1 >= actions.DesiredSize.Width,
                "minimum window keeps image actions and filename visible");
            Screenshot(window, "main-loaded", 1160, 740);
            Layout();

            // Exercise a complete overlapping key chord using a hidden input source.
            using var inputSource = new HwndSource(new HwndSourceParameters("ClassifyImage smoke tests")
            {
                WindowStyle = 0, Width = 0, Height = 0
            });
            void Key(string method, System.Windows.Input.Key key) =>
                Invoke(window, method, window, new KeyEventArgs(Keyboard.PrimaryDevice, inputSource, 0, key)
                {
                    RoutedEvent = method == "Window_KeyDown" ? Keyboard.KeyDownEvent : Keyboard.KeyUpEvent
                });
            Key("Window_KeyDown", System.Windows.Input.Key.D0);
            Key("Window_KeyDown", System.Windows.Input.Key.NumPad1);
            Key("Window_KeyUp", System.Windows.Input.Key.D0);
            Check(!Directory.Exists(category0), "multi-key classification waits for all releases");
            Key("Window_KeyUp", System.Windows.Input.Key.NumPad1);
            Check(File.Exists(original) && File.Exists(Path.Combine(category0, "sample.png")) &&
                File.Exists(Path.Combine(category1, "sample.png")), "multi-category copies preserve original");

            Setting("mut_kind_check", false);
            Setting("auto_next_check", true);
            Setting("default_path_check", true);
            Setting("default_path", Path.Combine(root, "default"));
            Invoke(window, "ClassifyCurrentImage", (object)new[] { 0 });
            string moved = Field<string>(window, "now_img_path");
            Check(!File.Exists(original) && Path.GetFileName(moved) == "sample_1.png" &&
                File.Exists(Path.Combine(category0, "sample.png")), "single classification resolves collisions");
            Check(!Directory.Exists(Path.Combine(root, "default")), "auto advance bypasses default move");

            var slider = Field<Slider>(window, "zoomSlider");
            slider.Value = 200;
            Check(Field<ScaleTransform>(window, "imageScale").ScaleX == 2, "slider updates image zoom");
            Check(resolution.Text == "200x100", "zoom does not change resolution");
            Layout();
            var viewer = Field<ScrollViewer>(window, "imageViewer");
            Check(viewer.ScrollableWidth > 100 && viewer.ScrollableHeight > 30,
                "zoomed image exceeds viewport in both directions");
            Check((bool)Invoke(window, "BeginImagePan", new Point(150, 100)), "zoomed image starts panning");
            Invoke(window, "MoveImagePan", new Point(90, 70));
            Layout();
            Check(Math.Abs(viewer.HorizontalOffset - 60) < 1 && Math.Abs(viewer.VerticalOffset - 30) < 1,
                "drag pans image by viewport distance");
            Invoke(window, "MoveImagePan", new Point(10000, 10000));
            Layout();
            Check(viewer.HorizontalOffset == 0 && viewer.VerticalOffset == 0, "panning clamps at image start");
            Invoke(window, "MoveImagePan", new Point(-10000, -10000));
            Layout();
            Check(Math.Abs(viewer.HorizontalOffset - viewer.ScrollableWidth) < 1 &&
                Math.Abs(viewer.VerticalOffset - viewer.ScrollableHeight) < 1, "panning clamps at image end");
            Invoke(window, "Window_Deactivated", window, EventArgs.Empty);
            Check(!Field<bool>(window, "isPanning"), "losing window focus ends panning");
            slider.Value = 100;
            Layout();
            Check(!(bool)Invoke(window, "BeginImagePan", new Point(100, 100)), "fitted image does not start panning");
            slider.Value = 200;
            Layout();
            Invoke(window, "BeginImagePan", new Point(100, 100));
            Invoke(window, "StartCropMode");
            Check(!Field<bool>(window, "isPanning") && !(bool)Invoke(window, "BeginImagePan", new Point(100, 100)),
                "crop mode ends and prevents image panning");
            Check(!slider.IsEnabled, "crop prevents zoom changes");
            Check(Field<System.Windows.Controls.Border>(window, "cropToolbar").Visibility == Visibility.Visible &&
                categoryButtons.All(button => !button.IsEnabled), "crop shows toolbar and disables classification");
            Screenshot(window, "main-crop", 860, 560);
            Layout();
            Field<TextBox>(window, "cropWidthInput").Text = "80";
            Field<TextBox>(window, "cropHeightInput").Text = "40";
            Invoke(window, "ApplyCropSize_Click", window, new RoutedEventArgs());
            Invoke(window, "btnConfirmCrop_Click", window, new RoutedEventArgs());
            var result = new BitmapImage();
            result.BeginInit();
            result.CacheOption = BitmapCacheOption.OnLoad;
            result.UriSource = new Uri(moved);
            result.EndInit();
            Check(result.PixelWidth == 80 && result.PixelHeight == 40, "crop saves exact pixel dimensions after zoom");
            Check(resolution.Text == "80x40", "resolution updates immediately after crop");
            Load(moved);
            Check(resolution.Text == "80x40", "reloading cropped image preserves pixel resolution");
            Check(slider.IsEnabled && !Directory.EnumerateFiles(category0, "*.tmp").Any(), "crop restores controls and cleans temporary file");

            Setting("auto_next_check", false);
            Setting("mut_kind_check", true);
            var settingsWindow = new SettingWindow();
            Check(!(bool)settings.GetType().GetProperty("auto_next_check")!.GetValue(settings)! &&
                (bool)settings.GetType().GetProperty("mut_kind_check")!.GetValue(settings)!, "settings initialization preserves options");
            Screenshot(settingsWindow, "settings-directories", 740, 680);
            Field<TabControl>(settingsWindow, "settingsTabs").SelectedIndex = 1;
            Screenshot(settingsWindow, "settings-behavior", 740, 680);
            settingsWindow.Close();
            window.Close();
            application.Shutdown();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
