using System.Windows.Threading;

namespace ClassifyImage;

public partial class MainWindow
{
    private ImageAnimation? imageAnimation;
    private DispatcherTimer? animationTimer;
    private int animationFrameIndex;
    private int animationLoops;

    private void LoadDisplayImage()
    {
        if (!Tools.IsGifOrWebP(now_img_path))
        {
            now_display_img.Source = Tools.LoadBitmapImage(now_img_path);
            return;
        }

        imageAnimation = new ImageAnimation(now_img_path);
        now_display_img.Source = imageAnimation.GetFrame(0);
        if (imageAnimation.FrameCount <= 1) return;

        animationTimer = new DispatcherTimer(DispatcherPriority.Render, Dispatcher);
        animationTimer.Tick += AnimationTimer_Tick;
        animationTimer.Interval = imageAnimation.GetFrameDelay(0);
        animationTimer.Start();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        if (imageAnimation == null || animationTimer == null) return;
        int nextFrame = animationFrameIndex + 1;
        if (nextFrame == imageAnimation.FrameCount)
        {
            animationLoops++;
            if (imageAnimation.RepeatCount > 0 && animationLoops >= imageAnimation.RepeatCount)
            {
                animationTimer.Stop();
                return;
            }
            nextFrame = 0;
        }
        now_display_img.Source = imageAnimation.GetFrame(nextFrame);
        animationFrameIndex = nextFrame;
        animationTimer.Interval = imageAnimation.GetFrameDelay(nextFrame);
    }

    private void StopImageAnimation()
    {
        if (animationTimer != null)
        {
            animationTimer.Stop();
            animationTimer.Tick -= AnimationTimer_Tick;
            animationTimer = null;
        }
        imageAnimation?.Dispose();
        imageAnimation = null;
        animationFrameIndex = 0;
        animationLoops = 0;
    }
}
