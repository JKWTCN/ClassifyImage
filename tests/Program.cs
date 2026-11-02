using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
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
            Check(image.Source is BitmapImage normal && normal.CreateOptions == BitmapCreateOptions.None,
                "normal images retain default color handling");
            // A truncated EXIF orientation directory makes WPF's ColorContexts read fail.
            // Use generated pixels and metadata rather than a copy of a user's image.
            string malformedMetadata = Path.Combine(root, "jpeg-with-incomplete-exif.PNG");
            var jpegEncoder = new JpegBitmapEncoder();
            jpegEncoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var jpeg = new MemoryStream())
            {
                jpegEncoder.Save(jpeg);
                byte[] bytes = jpeg.ToArray();
                using var output = File.Create(malformedMetadata);
                output.Write(bytes, 0, 2);
                output.Write(Convert.FromHexString("FFE1001E4578696600004D4D002A000000080001011200030000000100010000"));
                output.Write(bytes, 2, bytes.Length - 2);
            }
            Load(malformedMetadata);
            Check(image.Source is BitmapImage recovered && recovered.PixelWidth == 200 && recovered.PixelHeight == 100 &&
                recovered.CreateOptions == BitmapCreateOptions.IgnoreColorProfile && recovered.IsFrozen &&
                Field<StackPanel>(window, "emptyState").Visibility == Visibility.Collapsed,
                "JPEG with PNG extension and incomplete EXIF loads via color profile fallback");
            File.Delete(malformedMetadata);
            Check(!File.Exists(malformedMetadata), "fallback releases the source file");
            Load(original);
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

            // Small independent Pillow-encoded fixtures: 4x3 pixels, red/blue frames,
            // transparent top-left pixel, 70/130 ms delays, two complete plays.
            string formats = Path.Combine(root, "formats");
            Directory.CreateDirectory(formats);
            string gif = Path.Combine(formats, "animated.GIF");
            string webp = Path.Combine(formats, "animated.WebP");
            string staticGif = Path.Combine(formats, "static.gif");
            string staticWebp = Path.Combine(formats, "static.webp");
            File.WriteAllBytes(gif, Convert.FromBase64String("R0lGODlhBAADAIEAAP8AAAAAAAAAAAAAACH/C05FVFNDQVBFMi4wAwEBAAAh+QQJBwABACwAAAAABAADAAAICQADABhIsCCAgAAh+QQJDQABACwAAAAABAADAIEAAP8AAAAAAAAAAAAICQADABhIsCCAgAA7"));
            File.WriteAllBytes(webp, Convert.FromBase64String("UklGRogAAABXRUJQVlA4WAoAAAASAAAAAwAAAgAAQU5JTQYAAAAAAAAAAgBBTk1GKgAAAAAAAAAAAAMAAAIAAEYAAAJWUDhMEgAAAC8DgAAQDxDzH/MfjDs8RPQ/DEFOTUYqAAAAAAAAAAAAAwAAAgAAggAAAFZQOEwSAAAALwOAABAPEDH/8x+MOzxE9D8M"));
            File.WriteAllBytes(staticGif, Convert.FromBase64String("R0lGODdhBAADAIEAAP8AAAAAAAAAAAAAACwAAAAABAADAAAICAABCBxIUGBAADs="));
            File.WriteAllBytes(staticWebp, Convert.FromBase64String("UklGRhwAAABXRUJQVlA4TA8AAAAvA4AAEAcQ/Y8CBiKi/wEA"));
            File.WriteAllText(Path.Combine(formats, "ignored.txt"), "not an image");
            var tools = typeof(MainWindow).Assembly.GetType("ClassifyImage.Tools")!;
            var scanned = (string[])tools.GetMethod("GetImages")!.Invoke(null, new object[] { formats })!;
            Check(scanned.Length == 4 && new[] { gif, webp, staticGif, staticWebp }.All(scanned.Contains),
                "folder scanning includes GIF and WebP regardless of extension case");
            byte[] Pixels()
            {
                var source = (BitmapSource)image.Source!;
                byte[] pixels = new byte[source.PixelWidth * source.PixelHeight * 4];
                source.CopyPixels(pixels, source.PixelWidth * 4, 0);
                return pixels;
            }
            foreach (string path in new[] { gif, webp })
            {
                Load(path);
                var timer = Field<DispatcherTimer>(window, "animationTimer");
                Check(resolution.Text == "4x3" && Pixels()[3] == 0 && Pixels()[6] == 255 &&
                    timer.IsEnabled && timer.Interval.TotalMilliseconds == 70,
                    $"{Path.GetExtension(path)} animation starts with correct pixels, transparency and delay");
                Invoke(window, "StartCropMode");
                Check(!Field<Button>(window, "edit_img_btn").IsEnabled && !Field<bool>(window, "isInitialized") &&
                    Field<Button>(window, "copy_clipboard_btn").IsEnabled && categoryButtons[0].IsEnabled,
                    "new formats allow copying and classification while preventing crop");
                Invoke(window, "AnimationTimer_Tick", timer, EventArgs.Empty);
                Check(Pixels()[4] == 255 && Pixels()[6] == 0 && timer.Interval.TotalMilliseconds == 130,
                    "animation advances to the blue frame with its own delay");
                Invoke(window, "AnimationTimer_Tick", timer, EventArgs.Empty);
                Check(Pixels()[6] == 255 && timer.IsEnabled, "animation repeats from its first frame");
                Invoke(window, "AnimationTimer_Tick", timer, EventArgs.Empty);
                Invoke(window, "AnimationTimer_Tick", timer, EventArgs.Empty);
                Check(!timer.IsEnabled && Pixels()[4] == 255, "finite animation stops on its final frame after two plays");
                Load(original);
                Check(Field<object>(window, "imageAnimation") == null && Field<object>(window, "animationTimer") == null &&
                    Field<Button>(window, "edit_img_btn").IsEnabled, "navigation disposes animation and restores static image actions");
            }
            foreach (string path in new[] { staticGif, staticWebp })
            {
                Load(path);
                Check(resolution.Text == "4x3" && Field<object>(window, "animationTimer") == null,
                    $"static {Path.GetExtension(path)} displays without an animation timer");
            }
            Check(Pixels()[3] == 128 && Pixels()[6] == 255, "static WebP preserves partial alpha and channel order");
            Load(webp);
            var activeAnimationTimer = Field<DispatcherTimer>(window, "animationTimer");
            string animationCategory = Path.Combine(root, "animation-category");
            Setting("KeyPath3", animationCategory);
            byte[] animationBytes = File.ReadAllBytes(webp);
            Invoke(window, "ClassifyCurrentImage", (object)new[] { 3 });
            Check(!activeAnimationTimer.IsEnabled && File.ReadAllBytes(webp).SequenceEqual(animationBytes) &&
                File.ReadAllBytes(Path.Combine(animationCategory, Path.GetFileName(webp))).SequenceEqual(animationBytes),
                "classifying a playing animation copies the unchanged file and replaces its timer");
            Setting("mut_kind_check", false);
            Invoke(window, "ClassifyCurrentImage", (object)new[] { 3 });
            Check(!File.Exists(webp) && File.ReadAllBytes(Field<string>(window, "now_img_path")).SequenceEqual(animationBytes) &&
                Field<DispatcherTimer>(window, "animationTimer").IsEnabled,
                "playing WebP can be moved without a file lock and continues playing");
            Setting("mut_kind_check", true);
            Setting("KeyPath3", "");
            Load(gif);
            activeAnimationTimer = Field<DispatcherTimer>(window, "animationTimer");
            string invalidWebp = Path.Combine(root, "invalid.webp");
            File.WriteAllText(invalidWebp, "invalid image data");
            Load(invalidWebp);
            Check(!activeAnimationTimer.IsEnabled && image.Source == null &&
                Field<TextBlock>(window, "emptyStateTitle").Text == "图片读取失败",
                "invalid WebP stops the preceding animation and shows the load failure");
            Load(original);

            // Reproduce D -> deleted C -> B -> A without a blocking error dialog.
            var navigationPaths = new[] { "A.png", "B.png", "C.png", "D.png" }
                .Select(name => Path.Combine(root, name)).ToList();
            foreach (string path in navigationPaths) File.Copy(original, path);
            SetField(window, "img_paths", navigationPaths);
            SetField(window, "now_img_index", 3);
            Invoke(window, "UpdataDisplayImg");
            File.Delete(navigationPaths[2]);
            Key("Window_KeyUp", System.Windows.Input.Key.Left);
            Check(image.Source == null && Field<int>(window, "now_img_index") == 2 &&
                Field<StackPanel>(window, "emptyState").Visibility == Visibility.Visible,
                "deleted image shows an inline message and preserves navigation position");
            Check(Field<TextBlock>(window, "emptyStateTitle").Text == "图片文件不存在",
                "missing file is identified separately from decoding failures");
            Screenshot(window, "main-missing", 860, 560);
            Check(Field<Button>(window, "left_btn").IsEnabled && Field<Button>(window, "right_btn").IsEnabled &&
                !Field<Button>(window, "edit_img_btn").IsEnabled && categoryButtons.All(button => !button.IsEnabled),
                "missing image keeps navigation enabled while disabling image actions");
            Check(Field<TextBlock>(window, "imageCounter").Text == "3 / 4" &&
                Field<TextBlock>(window, "emptyStateHint").Text.Contains("方向键"),
                "missing image retains its counter and explains continued navigation");
            Setting("default_path_check", true);
            Setting("default_path", Path.Combine(root, "missing-image-default"));
            Key("Window_KeyUp", System.Windows.Input.Key.Right);
            Check(Field<string>(window, "now_img_path") == navigationPaths[3] && image.Source != null &&
                !Directory.Exists(Path.Combine(root, "missing-image-default")),
                "forward navigation from missing image bypasses default move");
            Setting("default_path_check", false);
            // Restore the failed C position to verify backward navigation.
            SetField(window, "now_img_index", 2);
            SetField(window, "now_img_path", navigationPaths[2]);
            image.Source = null;
            Invoke(window, "RefreshImageInterface");
            Key("Window_KeyUp", System.Windows.Input.Key.Left);
            Check(Field<string>(window, "now_img_path") == navigationPaths[1] && image.Source != null,
                "left key returns to B after deleted C");
            Field<Button>(window, "left_btn").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(Field<string>(window, "now_img_path") == navigationPaths[0] && image.Source != null,
                "previous button continues to A");
            // D -> C -> deleted B -> A -> deleted B -> C must restore the image size immediately.
            File.Copy(original, navigationPaths[2]);
            File.Delete(navigationPaths[1]);
            SetField(window, "now_img_index", 3);
            Invoke(window, "UpdataDisplayImg");
            Layout();
            foreach (int index in new[] { 2, 1, 0 })
            {
                Key("Window_KeyUp", System.Windows.Input.Key.Left);
                Layout();
                Check(Field<int>(window, "now_img_index") == index, "backward navigation retains deleted B position");
            }
            foreach (int index in new[] { 1, 2 })
            {
                Key("Window_KeyUp", System.Windows.Input.Key.Right);
                Layout();
                Check(Field<int>(window, "now_img_index") == index, "forward navigation retains deleted B position");
            }
            Check(image.Source != null && image.ActualWidth > 100 && image.ActualHeight > 50 &&
                Field<ScrollViewer>(window, "imageViewer").Visibility == Visibility.Visible,
                "C renders at usable dimensions immediately after passing deleted B twice");
            string corrupt = Path.Combine(root, "corrupt.png");
            File.WriteAllText(corrupt, "invalid image data");
            Load(corrupt);
            Check(image.Source == null && Field<StackPanel>(window, "emptyState").Visibility == Visibility.Visible,
                "corrupt image shows the central unavailable state without a dialog");
            Check(Field<TextBlock>(window, "emptyStateTitle").Text == "图片读取失败" &&
                Field<TextBlock>(window, "emptyStateHint").ToolTip is string,
                "unreadable file reports a load failure with error details");
            Load(original);
            Check(Field<TextBlock>(window, "emptyStateHint").ToolTip == null,
                "successful load clears the previous error");

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
            Check(Field<TextBlock>(window, "statusText").Text == "已移动到分类 0" &&
                Field<StackPanel>(window, "emptyState").Visibility == Visibility.Collapsed,
                "successful classification keeps feedback in the status bar and the image visible");

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
            Layout(860, 560);
            var toolbar = Field<Border>(window, "cropToolbar");
            var contentRoot = (FrameworkElement)window.Content;
            Check(toolbar.TranslatePoint(new Point(0, toolbar.ActualHeight), contentRoot).Y <=
                viewer.TranslatePoint(new Point(0, 0), contentRoot).Y,
                "crop toolbar reserves space above the image at minimum size");
            var openButton = Field<Button>(window, "open_file_folders_btn");
            Check(Math.Abs(toolbar.TranslatePoint(new Point(0, toolbar.ActualHeight / 2), contentRoot).Y -
                openButton.TranslatePoint(new Point(0, openButton.ActualHeight / 2), contentRoot).Y) < 2 &&
                toolbar.TranslatePoint(new Point(0, 0), contentRoot).X >
                openButton.TranslatePoint(new Point(openButton.ActualWidth, 0), contentRoot).X,
                "crop controls stay on the right in the same toolbar row at minimum size");
            Check(toolbar.ActualWidth + toolbar.Margin.Left + toolbar.Margin.Right >= toolbar.DesiredSize.Width - 1,
                "crop controls fit without clipping at minimum size");
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

            // A nonuniform, high-DPI image makes both pixel averaging and coordinate errors visible.
            string mosaicPath = Path.Combine(root, "mosaic.png");
            byte[] mosaicPixels = new byte[12 * 8 * 4];
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 12; x++)
                {
                    int offset = (y * 12 + x) * 4;
                    mosaicPixels[offset] = (byte)(x * 10);
                    mosaicPixels[offset + 1] = (byte)(y * 20);
                    mosaicPixels[offset + 2] = (byte)(x * 10 + y * 5);
                    mosaicPixels[offset + 3] = 255;
                }
            var mosaicSource = BitmapSource.Create(12, 8, 300, 300, PixelFormats.Bgra32, null, mosaicPixels, 48);
            var mosaicEncoder = new PngBitmapEncoder();
            mosaicEncoder.Frames.Add(BitmapFrame.Create(mosaicSource));
            using (var stream = File.Create(mosaicPath)) mosaicEncoder.Save(stream);
            byte[] beforeMosaic = File.ReadAllBytes(mosaicPath);
            var applyMosaic = tools.GetMethod("ApplyMosaic")!;
            var pixelated = (BitmapSource)applyMosaic.Invoke(null, new object[] { mosaicSource, new Int32Rect(2, 1, 5, 5), 4 })!;
            byte[] expectedMosaic = new byte[mosaicPixels.Length];
            pixelated.CopyPixels(expectedMosaic, 48, 0);
            Check(expectedMosaic[(1 * 12 + 2) * 4] == 35 && expectedMosaic[(1 * 12 + 2) * 4 + 1] == 50 &&
                expectedMosaic[(1 * 12 + 2) * 4 + 2] == 47 && expectedMosaic[(5 * 12 + 6) * 4] == 60 &&
                expectedMosaic[(5 * 12 + 6) * 4 + 1] == 100,
                "mosaic averages full blocks and partial blocks at selection edges");
            Check(Enumerable.Range(0, 12 * 8).Where(i => i % 12 < 2 || i % 12 >= 7 || i / 12 < 1 || i / 12 >= 6)
                .All(i => expectedMosaic.AsSpan(i * 4, 4).SequenceEqual(mosaicPixels.AsSpan(i * 4, 4))) &&
                pixelated.PixelWidth == 12 && pixelated.PixelHeight == 8 && pixelated.DpiX == 300,
                "mosaic leaves pixels outside the selection, dimensions and DPI unchanged");
            var transparentSource = BitmapSource.Create(2, 1, 96, 96, PixelFormats.Bgra32, null,
                new byte[] { 0, 0, 255, 255, 255, 0, 0, 0 }, 8);
            var transparentMosaic = (BitmapSource)applyMosaic.Invoke(null, new object[] { transparentSource, new Int32Rect(0, 0, 2, 1), 4 })!;
            byte[] alphaPixels = new byte[8];
            transparentMosaic.CopyPixels(alphaPixels, 8, 0);
            Check(alphaPixels.SequenceEqual(new byte[] { 0, 0, 255, 127, 0, 0, 255, 127 }),
                "mosaic weights colors by alpha without leaking hidden transparent colors");
            foreach (var invalid in new[] { (new Int32Rect(0, 0, 1, 1), 1), (new Int32Rect(11, 7, 2, 2), 4) })
            {
                bool rejected = false;
                try { applyMosaic.Invoke(null, new object[] { mosaicSource, invalid.Item1, invalid.Item2 }); }
                catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException) { rejected = true; }
                Check(rejected, "mosaic rejects invalid block size or out-of-bounds selection");
            }
            Load(mosaicPath);
            slider.Value = 200;
            Layout();
            Invoke(window, "MosaicButton_Click", window, new RoutedEventArgs());
            Layout(860, 560);
            Check(Field<bool>(window, "isMosaicMode") && !slider.IsEnabled &&
                Field<StackPanel>(window, "cropSizePanel").Visibility == Visibility.Collapsed &&
                Field<StackPanel>(window, "mosaicOptions").Visibility == Visibility.Visible &&
                categoryButtons.All(button => !button.IsEnabled) &&
                toolbar.ActualWidth + toolbar.Margin.Left + toolbar.Margin.Right >= toolbar.DesiredSize.Width - 1,
                "mosaic editor disables navigation and classification and fits the minimum toolbar");
            var selectionCanvas = Field<Canvas>(window, "cropCanvas");
            void MosaicSelection(int x, int y, int width, int height) => Invoke(window, "SetMosaicSelection",
                new Point((x + width) * selectionCanvas.Width / 12, (y + height) * selectionCanvas.Height / 8),
                new Point(x * selectionCanvas.Width / 12, y * selectionCanvas.Height / 8));
            Field<Slider>(window, "mosaicBlockSize").Value = 4;
            MosaicSelection(2, 1, 5, 5);
            Check(Pixels().SequenceEqual(expectedMosaic) && File.ReadAllBytes(mosaicPath).SequenceEqual(beforeMosaic),
                "reverse drag selects the intended pixels after zoom while preview leaves the file unchanged");
            MosaicSelection(8, 0, 4, 4);
            Check(Pixels()[(1 * 12 + 2) * 4] == mosaicPixels[(1 * 12 + 2) * 4],
                "moving the mosaic selection restores pixels from the previous selection");
            Key("Window_KeyUp", System.Windows.Input.Key.Escape);
            Check(Pixels().SequenceEqual(mosaicPixels) && File.ReadAllBytes(mosaicPath).SequenceEqual(beforeMosaic) &&
                !Field<bool>(window, "isInitialized") && slider.IsEnabled,
                "Escape cancels mosaic preview and restores the original pixels and controls");
            Invoke(window, "MosaicButton_Click", window, new RoutedEventArgs());
            MosaicSelection(2, 1, 5, 5);
            Invoke(window, "btnConfirmCrop_Click", window, new RoutedEventArgs());
            Load(mosaicPath);
            Check(Pixels().SequenceEqual(expectedMosaic) && resolution.Text == "12x8" &&
                !Directory.EnumerateFiles(root, "*.tmp").Any(), "saving mosaic writes the preview at the original size and cleans temporary files");
            Load(gif);
            Invoke(window, "MosaicButton_Click", window, new RoutedEventArgs());
            Check(!Field<bool>(window, "isInitialized") && !Field<Button>(window, "mosaicButton").IsEnabled,
                "animated formats cannot enter mosaic editing");

            Setting("auto_next_check", false);
            Setting("mut_kind_check", true);
            var settingsWindow = new SettingWindow();
            Check(!(bool)settings.GetType().GetProperty("auto_next_check")!.GetValue(settings)! &&
                (bool)settings.GetType().GetProperty("mut_kind_check")!.GetValue(settings)!, "settings initialization preserves options");
            Screenshot(settingsWindow, "settings-directories", 740, 680);
            Field<TabControl>(settingsWindow, "settingsTabs").SelectedIndex = 1;
            Screenshot(settingsWindow, "settings-behavior", 740, 680);
            settingsWindow.Close();
            Load(gif);
            var closingAnimationTimer = Field<DispatcherTimer>(window, "animationTimer");
            window.Close();
            Check(!closingAnimationTimer.IsEnabled && Field<object>(window, "imageAnimation") == null,
                "closing the window releases animation resources");
            application.Shutdown();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
