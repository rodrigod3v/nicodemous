#if WINDOWS
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public class IconGenerator
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    extern static bool DestroyIcon(IntPtr handle);

    public static void GenerateIcons()
    {
        string assetsDir = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
        if (!Directory.Exists(assetsDir)) Directory.CreateDirectory(assetsDir);

        int[] sizes = { 16, 32, 48, 64, 128, 256 };
        string icoPath = Path.Combine(assetsDir, "app_icon.ico");
        string pngPath = Path.Combine(assetsDir, "logo_n.png");

        using (FileStream fs = new FileStream(icoPath, FileMode.Create))
        {
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                // ICO Header
                writer.Write((short)0);    // Reserved
                writer.Write((short)1);    // Type 1 = Icon
                writer.Write((short)sizes.Length); // Number of images

                long offset = 6 + (sizes.Length * 16);
                List<byte[]> imageData = new List<byte[]>();

                foreach (int s in sizes)
                {
                    using var bitmap = GenerateNLogo(s);
                    if (s == 256) bitmap.Save(pngPath, ImageFormat.Png); // Save high-res PNG too

                    using var ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Png); // Save PNG inside ICO (standard for modern)
                    byte[] data = ms.ToArray();
                    imageData.Add(data);

                    // Directory Entry
                    writer.Write((byte)(s == 256 ? 0 : s)); // Width
                    writer.Write((byte)(s == 256 ? 0 : s)); // Height
                    writer.Write((byte)0); // Color count
                    writer.Write((byte)0); // Reserved
                    writer.Write((short)1); // Planes
                    writer.Write((short)32); // Bit count
                    writer.Write(data.Length); // Image size
                    writer.Write((int)offset); // Image offset

                    offset += data.Length;
                }

                foreach (byte[] data in imageData)
                {
                    writer.Write(data);
                }
            }
        }
        
        Console.WriteLine($"Icons generated: {icoPath}");
    }

    private static Bitmap GenerateNLogo(int size)
    {
        var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        Color colorStart = ColorTranslator.FromHtml("#8B5CF6");
        Color colorEnd = ColorTranslator.FromHtml("#D946EF");

        using var brush = new LinearGradientBrush(
            new Rectangle(0, 0, size, size),
            colorStart,
            colorEnd,
            LinearGradientMode.ForwardDiagonal);

        float margin = size * 0.15f;
        float strokeWidth = size * 0.20f; // Thicker for small sizes
        using var pen = new Pen(brush, strokeWidth);
        pen.StartCap = LineCap.Round;
        pen.EndCap = LineCap.Round;
        pen.LineJoin = LineJoin.Round;

        g.DrawLine(pen, margin, size - margin, margin, margin);
        g.DrawLine(pen, margin, margin, size - margin, size - margin);
        g.DrawLine(pen, size - margin, size - margin, size - margin, margin);

        return bitmap;
    }
}
#endif
