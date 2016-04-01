using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Drawing2D;
namespace WallpaperSetter
{
    public partial class Form1 : Form
    {
        private int ScreenWidth
        {
            get
            {
                return Screen.PrimaryScreen.Bounds.Width;
            }
        }

        private int ScreenHeight
        {
            get
            {
                return Screen.PrimaryScreen.Bounds.Height;
            }
        }
        private Size ScreenSize
        {
            get { return new Size(this.ScreenWidth, this.ScreenHeight); }
        }
        private Size CanvasSize
        {
            get { return this.pictureBox1.Size; }
        }

        private bool isl = false;

        private Point start;

        private int start_x = 0;
        private int start_y = 0;

        private Size ScreenSizeInCanvas;

        bool m_MouseDownInEyedropper = false;
        Bitmap m_ScreenImage;
        Cursor m_eyeDropperCursor = Utility.CreateColorCursorFromResourceFile("WallpaperSetter.EyeDrop.cur");
        Point m_cursorLocation;

        public Form1()
        {
            InitializeComponent();

            ScreenSizeInCanvas = ScreenSize.ApplyAspect(CanvasSize);
            label2.Text = string.Format("Your screen resolution: {0}x{1}", ScreenSize.Width, ScreenSize.Height);
            this.pictureBox1.MouseDown += (s, e) =>
            {
                isl = true;
                start = e.Location;
                this.pictureBox1.Cursor = Cursors.Cross;
            };
            this.pictureBox1.MouseMove += (s, e) =>
            {
                if (isl)
                {
                    start_x += e.X - start.X;
                    start_y += e.Y - start.Y;
                    start = e.Location;
                    draw_result();
                }
            };
            this.pictureBox1.MouseUp += (s, e) =>
            {
                isl = false;
                start = e.Location;
                this.pictureBox1.Cursor = Cursors.Arrow;
                draw_result();
            };

            this.button4.MouseDown += (s, e) =>
            {
                m_MouseDownInEyedropper = true;
                this.Cursor = m_eyeDropperCursor;

                m_ScreenImage = Utility.CaptureScreen();

                m_cursorLocation = Utility.GetCursorPostionOnScreen();
                this.domColor.BackColor = m_ScreenImage.GetPixel(m_cursorLocation.X, m_cursorLocation.Y);
            };
            this.button4.MouseMove += (s, e) =>
            {
                if (m_MouseDownInEyedropper)
                {
                    m_cursorLocation = Utility.GetCursorPostionOnScreen();
                    this.domColor.BackColor = m_ScreenImage.GetPixel(m_cursorLocation.X, m_cursorLocation.Y);
                    draw_result();
                }
            };
            this.button4.MouseUp += (s, e) =>
            {
                m_MouseDownInEyedropper = false;
                m_ScreenImage.Dispose();
                this.Cursor = Cursors.Default;
                draw_result();
            };
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var o = new OpenFileDialog())
            {
                o.Filter = "Image files|*.jpg;*.bmp;*.png;*.jpeg;*.gif";
                o.CheckFileExists = true;
                if (o.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    load_image(File.ReadAllBytes(o.FileName));
                }
            }
        }

        private Bitmap raw_wallpaper = null;

        private Color dominant_color = Color.Black;

        private void load_image(byte[] data)
        {
            if (raw_wallpaper != null)
            {
                raw_wallpaper.Dispose();
            }

            raw_wallpaper = new Bitmap(new MemoryStream(data));

            label3.Text = string.Format("Wallpaper size: {0}x{1}", raw_wallpaper.Width, raw_wallpaper.Height);

            long r = 0;
            long g = 0;
            long b = 0;
            long count = 0;

            for (int x = 0; x < raw_wallpaper.Width; x++)
            {
                for (int y = 0; y < raw_wallpaper.Height; y++)
                {
                    Color w = raw_wallpaper.GetPixel(x, y);
                    r += w.R;
                    g += w.G;
                    b += w.B;
                    count++;

                    Application.DoEvents();
                }
            }

            dominant_color = Color.FromArgb(Convert.ToInt32(r / count), Convert.ToInt32(g / count), Convert.ToInt32(b / count));

            this.panel1.BackColor = dominant_color;
            this.domColor.BackColor = dominant_color;

            draw_result();
        }


        private void draw_result()
        {
            if (raw_wallpaper != null)
            {
                if (pictureBox1.Image != null)
                {
                    Image a = pictureBox1.Image;
                    pictureBox1.Image = null;
                    a.Dispose();
                }

                Bitmap bi = new Bitmap(ScreenSize.Width, ScreenSize.Height);

                Graphics gg = Graphics.FromImage(bi);

                gg.Clear(this.domColor.BackColor);

                int x = Convert.ToInt32((Convert.ToDouble(start_x) / Convert.ToDouble(CanvasSize.Width)) * ScreenSize.Width);
                int y = 0;

                if (checkBox1.Checked)
                {
                    y = Convert.ToInt32((Convert.ToDouble(start_y) / Convert.ToDouble(CanvasSize.Height)) * ScreenSize.Height);
                }

                if (checkBox2.Checked)
                {
                    gg.DrawImage(raw_wallpaper, new Rectangle(x, y, ScreenSize.Width, ScreenSize.Height));
                }
                else
                {
                    // Ignore image DPI 
                    gg.DrawImage(raw_wallpaper, x, y, raw_wallpaper.Width, raw_wallpaper.Height);
                }

                gg.Dispose();

                pictureBox1.Image = bi;
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                string file_name = Path.Combine(Path.GetTempPath(), "wallsetterimage.bmp");
                MemoryStream mem = new MemoryStream();
                pictureBox1.Image.Save(mem, System.Drawing.Imaging.ImageFormat.Bmp);
                File.WriteAllBytes(file_name, mem.ToArray());
                mem.Dispose();
                Wallpaper.SetFromFile(file_name, Wallpaper.Style.Stretched);
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked)
            {
                start_y = 0;
                draw_result();
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            draw_result();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                using (SaveFileDialog d = new SaveFileDialog())
                {
                    d.Filter = "PNG Images|*.png|JPEG Images|*.jpg";

                    if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        MemoryStream mem = new MemoryStream();
                        pictureBox1.Image.Save(mem, d.FileName.EndsWith(".jpg") ? System.Drawing.Imaging.ImageFormat.Jpeg : System.Drawing.Imaging.ImageFormat.Png);
                        File.WriteAllBytes(d.FileName, mem.ToArray());
                        mem.Dispose();
                    }
                }
            }
            else
            {
                MessageBox.Show("No image loaded");
            }
        }

        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            domColor.BackColor = dominant_color;
            draw_result();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            start_x = 0;
            start_y = 0;
            draw_result();
        }
    }


    public static class Extensions
    {
        public static Size ApplyAspect(this Size original, Size target)
        {
            int newImageWidth = target.Width;
            int newImageHeight = target.Height;

            double ratioX = Convert.ToDouble(target.Width) / Convert.ToDouble(original.Width);
            double ratioY = Convert.ToDouble(target.Height) / Convert.ToDouble(original.Height);

            double ratio = ratioX < ratioY ? ratioX : ratioY;

            newImageHeight = Convert.ToInt32(original.Height * ratio);

            newImageWidth = Convert.ToInt32(original.Width * ratio);

            return new Size(newImageWidth, newImageHeight);

        }

        public static bool is_smaller_than(this System.Drawing.Size a1, System.Drawing.Size a2)
        {
            return a1.Height < a2.Height && a1.Width < a2.Width;
        }


    }
}
