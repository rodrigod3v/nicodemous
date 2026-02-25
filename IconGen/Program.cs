using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        string baseDir = args.Length > 0 ? args[0] : Path.Combine(Directory.GetCurrentDirectory(), "..", "backend", "Assets");
        if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

        GenerateMultipleIcons(baseDir);
    }

    static void GenerateMultipleIcons(string dir)
    {
        // App Icon (Multiple sizes)
        CreateIcon(Path.Combine(dir, "app_icon.ico"), new[] { 16, 24, 32, 48, 64, 128, 256 });
        
        // Tray Icons
        CreateIcon(Path.Combine(dir, "tray_idle.ico"), new[] { 16, 32 });
        CreateIcon(Path.Combine(dir, "tray_connected.ico"), new[] { 16, 32 }, Color.LightGreen);
        
        for (int i = 1; i <= 6; i++)
        {
            CreateIcon(Path.Combine(dir, $"tray_active_{i}.ico"), new[] { 16, 32 }, Color.Violet);
        }

        Console.WriteLine($"Icons generated in: {dir}");
    }

    static void CreateIcon(string path, int[] sizes, Color? overlayColor = null)
    {
        using var fs = new FileStream(path, FileMode.Create);
        using var writer = new BinaryWriter(fs);

        // ICO Header
        writer.Write((short)0);    // Reserved
        writer.Write((short)1);    // Type 1 = Icon
        writer.Write((short)sizes.Length);

        var imageData = new List<byte[]>();
        long offset = 6 + (sizes.Length * 16);

        foreach (var size in sizes)
        {
            using var bitmap = GenerateNLogo(size, overlayColor);
            byte[] data;
            
            using (var ms = new MemoryStream())
            {
                // BMP format for smaller icons, PNG for 256
                if (size >= 256)
                {
                    bitmap.Save(ms, ImageFormat.Png);
                }
                else
                {
                    // For ICO, we need DIB (no BMP file header)
                    // But Bitmap.Save(ms, ImageFormat.Png) is actually very compatible for all sizes in modern Windows
                    // Let's stick to PNG as it's cleaner and usually works if the header is correct
                    bitmap.Save(ms, ImageFormat.Png);
                }
                data = ms.ToArray();
            }

            imageData.Add(data);

            // Directory Entry
            writer.Write((byte)(size >= 256 ? 0 : size));
            writer.Write((byte)(size >= 256 ? 0 : size));
            writer.Write((byte)0); // Color count
            writer.Write((byte)0); // Reserved
            writer.Write((short)1); // Planes
            writer.Write((short)32); // Bit count
            writer.Write(data.Length);
            writer.Write((int)offset);

            offset += data.Length;
        }

        foreach (var data in imageData)
        {
            writer.Write(data);
        }
    }

    static Bitmap GenerateNLogo(int size, Color? overlayColor = null)
    {
        var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        Color colorStart = overlayColor ?? ColorTranslator.FromHtml("#8B5CF6");
        Color colorEnd = overlayColor ?? ColorTranslator.FromHtml("#D946EF");

        using var brush = new LinearGradientBrush(
            new Rectangle(0, 0, size, size),
            colorStart,
            colorEnd,
            LinearGradientMode.ForwardDiagonal);

        float margin = size * 0.15f;
        float strokeWidth = size * 0.20f;
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
