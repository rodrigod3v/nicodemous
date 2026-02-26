using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace IconGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string baseDir = @"C:\Users\777\Desktop\nicodemous\backend\Assets";
                if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

                GenerateIcon(Path.Combine(baseDir, "tray_idle.ico"), Color.FromArgb(248, 250, 252), Color.FromArgb(26, 28, 46));
                GenerateIcon(Path.Combine(baseDir, "tray_connected.ico"), Color.FromArgb(139, 92, 246), Color.FromArgb(217, 70, 239));

                Console.WriteLine("Icons generated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        static void GenerateIcon(string path, Color fillColor, Color outlineColor)
        {
            int size = 64;
            using (Bitmap bmp = new Bitmap(size, size))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);

                    // Draw a mouse pointer
                    PointF[] points = {
                        new PointF(20, 10),
                        new PointF(20, 50),
                        new PointF(32, 38),
                        new PointF(52, 38)
                    };

                    using (GraphicsPath gp = new GraphicsPath())
                    {
                        gp.AddPolygon(points);
                        using (SolidBrush brush = new SolidBrush(fillColor))
                        {
                            g.FillPath(brush, gp);
                        }
                        using (Pen pen = new Pen(outlineColor, 3f))
                        {
                            pen.LineJoin = LineJoin.Round;
                            g.DrawPath(pen, gp);
                        }
                    }
                }

                // Simple conversion to ICO format handling 1 image
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        // ICO header
                        bw.Write((short)0); // reserved
                        bw.Write((short)1); // type (1 = ico)
                        bw.Write((short)1); // count

                        using (MemoryStream ms = new MemoryStream())
                        {
                            bmp.Save(ms, ImageFormat.Png);
                            byte[] pngBytes = ms.ToArray();

                            // Directory entry
                            bw.Write((byte)size);     // width
                            bw.Write((byte)size);     // height
                            bw.Write((byte)0);        // colors
                            bw.Write((byte)0);        // reserved
                            bw.Write((short)1);       // color planes
                            bw.Write((short)32);      // BPP
                            bw.Write((int)pngBytes.Length); // size of data
                            bw.Write((int)22);        // offset to data

                            // Image data
                            bw.Write(pngBytes);
                        }
                    }
                }
            }
        }
    }
}
