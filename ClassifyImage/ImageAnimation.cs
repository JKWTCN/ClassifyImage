using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageMagick;

namespace ClassifyImage;

// 解码器负责合成透明、局部更新和帧处置；只为当前显示帧创建 WPF 位图。
internal sealed class ImageAnimation : IDisposable
{
    private readonly MagickImageCollection frames = new();

    public ImageAnimation(string path)
    {
        try
        {
            frames.Read(File.ReadAllBytes(path));
            frames.Coalesce();
        }
        catch
        {
            frames.Dispose();
            throw;
        }
    }

    public int FrameCount => frames.Count;
    public uint RepeatCount => frames[0].AnimationIterations;

    public TimeSpan GetFrameDelay(int index)
    {
        var frame = frames[index];
        double delay = frame.AnimationDelay * 1000.0 / Math.Max(1, frame.AnimationTicksPerSecond);
        return TimeSpan.FromMilliseconds(delay <= 0 ? 100 : Math.Max(10, delay));
    }

    public BitmapSource GetFrame(int index)
    {
        var frame = frames[index];
        byte[] pixels = frame.ToByteArray(MagickFormat.Bgra);
        var bitmap = BitmapSource.Create((int)frame.Width, (int)frame.Height, 96, 96, PixelFormats.Bgra32,
            null, pixels, checked((int)frame.Width * 4));
        bitmap.Freeze();
        return bitmap;
    }

    public void Dispose() => frames.Dispose();
}
