using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace TimeShots.Core.Overlay;

public static class TimestampOverlayRenderer
{
    public static byte[] RenderOverlay(
        byte[] inputImageBytes,
        string utcIso,
        string localFormatted,
        long sequence,
        string truncatedHash,
        string style = "default")
    {
        using var inputStream = new MemoryStream(inputImageBytes);
        using var originalBitmap = new Bitmap(inputStream);

        using var outputBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(outputBitmap))
        {
            g.DrawImage(originalBitmap, 0, 0, originalBitmap.Width, originalBitmap.Height);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            DrawOverlayLines(g, outputBitmap.Width, outputBitmap.Height, utcIso, localFormatted, sequence, truncatedHash, style);
        }

        using var outputStream = new MemoryStream();
        outputBitmap.Save(outputStream, ImageFormat.Png);
        return outputStream.ToArray();
    }

    private static void DrawOverlayLines(
        Graphics g,
        int imgWidth,
        int imgHeight,
        string utcIso,
        string localFormatted,
        long sequence,
        string truncatedHash,
        string style)
    {
        using var font = new Font("Consolas", 10.5f, FontStyle.Regular, GraphicsUnit.Pixel);

        var lines = new List<string>();
        if (style == "minimal")
        {
            lines.Add($"{localFormatted} | #{sequence:D5}");
        }
        else if (style == "compact")
        {
            lines.Add($"{localFormatted}");
            lines.Add($"#{sequence:D5} | sha256:{truncatedHash}");
        }
        else // default
        {
            lines.Add($"{utcIso} (UTC)");
            lines.Add($"{localFormatted} (L)");
            lines.Add($"#{sequence:D5} | sha256:{truncatedHash}");
        }

        // Measure text bounding box
        float maxTextWidth = 0;
        float totalTextHeight = 0;
        float lineHeight = font.GetHeight(g) + 2;

        foreach (var line in lines)
        {
            var size = g.MeasureString(line, font);
            if (size.Width > maxTextWidth) maxTextWidth = size.Width;
            totalTextHeight += lineHeight;
        }

        int paddingX = 10;
        int paddingY = 8;
        int margin = 12;

        int boxWidth = Math.Min(imgWidth, (int)Math.Ceiling(maxTextWidth) + (paddingX * 2));
        int boxHeight = Math.Min(imgHeight, (int)Math.Ceiling(totalTextHeight) + (paddingY * 2));

        int boxX = Math.Max(0, imgWidth - boxWidth - margin);
        int boxY = Math.Max(0, imgHeight - boxHeight - margin);

        // Draw semi-transparent dark rounded badge
        using (var brush = new SolidBrush(Color.FromArgb(215, 15, 23, 42))) // dark slate
        using (var borderPen = new Pen(Color.FromArgb(160, 56, 189, 248), 1)) // accent cyan border
        {
            var rect = new Rectangle(boxX, boxY, boxWidth, boxHeight);
            g.FillRectangle(brush, rect);
            g.DrawRectangle(borderPen, rect);
        }

        // Render text lines
        using var textBrush = new SolidBrush(Color.FromArgb(248, 250, 252));
        using var accentBrush = new SolidBrush(Color.FromArgb(56, 189, 248));

        float textY = boxY + paddingY;
        for (int i = 0; i < lines.Count; i++)
        {
            var brush = (i == lines.Count - 1 && style != "minimal") ? accentBrush : textBrush;
            g.DrawString(lines[i], font, brush, boxX + paddingX, textY);
            textY += lineHeight;
        }
    }
}
