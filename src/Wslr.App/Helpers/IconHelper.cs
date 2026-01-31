using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfPoint = System.Windows.Point;

namespace Wslr.App.Helpers;

/// <summary>
/// Helper class for creating application icons.
/// </summary>
public static class IconHelper
{
    /// <summary>
    /// Creates the WSLR application icon as a System.Drawing.Icon for the system tray.
    /// </summary>
    /// <param name="size">The size of the icon in pixels.</param>
    /// <returns>The icon as a System.Drawing.Icon.</returns>
    public static Icon CreateTrayIcon(int size = 32)
    {
        using var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);

        // Enable anti-aliasing
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

        // Fill background with Windows blue (#0078D4)
        using var backgroundBrush = new SolidBrush(System.Drawing.Color.FromArgb(0, 120, 212));
        graphics.FillEllipse(backgroundBrush, 1, 1, size - 2, size - 2);

        // Draw "W" in white
        using var font = new Font("Segoe UI", size * 0.45f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
        using var textBrush = new SolidBrush(System.Drawing.Color.White);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        var rect = new RectangleF(0, 0, size, size);
        graphics.DrawString("W", font, textBrush, rect, format);

        // Convert bitmap to icon by saving to memory stream
        // This ensures the icon owns its data and survives bitmap disposal
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;

        // Create icon from the PNG data via a new bitmap
        using var iconBitmap = new Bitmap(stream);
        var iconHandle = iconBitmap.GetHicon();

        // Clone the icon so it owns its data
        using var tempIcon = Icon.FromHandle(iconHandle);
        return (Icon)tempIcon.Clone();
    }

    /// <summary>
    /// Creates the WSLR application icon as an ImageSource for WPF windows.
    /// </summary>
    /// <param name="size">The size of the icon in pixels.</param>
    /// <returns>The icon as an ImageSource.</returns>
    public static ImageSource CreateAppIcon(int size = 256)
    {
        var radius = size / 2.0;

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            // Draw blue circular background (#0078D4 - Windows blue)
            var backgroundBrush = new SolidColorBrush(WpfColor.FromRgb(0, 120, 212));
            backgroundBrush.Freeze();
            context.DrawEllipse(backgroundBrush, null, new WpfPoint(radius, radius), radius - 2, radius - 2);

            // Draw "W" in white
            var typeface = new Typeface(new System.Windows.Media.FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            var formattedText = new FormattedText(
                "W",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                size * 0.55,
                WpfBrushes.White,
                96);

            // Center the text
            var textX = (size - formattedText.Width) / 2;
            var textY = (size - formattedText.Height) / 2;
            context.DrawText(formattedText, new WpfPoint(textX, textY));
        }

        // Render to bitmap
        var renderBitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        renderBitmap.Render(visual);

        // Convert RenderTargetBitmap to BitmapImage for WPF Window.Icon
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        stream.Position = 0;

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = stream;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        return bitmapImage;
    }
}
