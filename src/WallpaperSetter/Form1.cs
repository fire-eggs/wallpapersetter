using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

// ReSharper disable SuggestUseVarKeywordEvident

namespace WallpaperSetter
{
    public partial class Form1 : Form
    {
        private static Size ScreenSize
        {
            get { return new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height); }
        }
        private Size CanvasSize
        {
            get { return pictureBox1.Size; }
        }

        private bool amDragging;

        private Point start;

        private int start_x;
        private int start_y;

        bool m_MouseDownInEyedropper;
        Bitmap m_ScreenImage;
        readonly Cursor m_eyeDropperCursor = Utility.CreateColorCursorFromResourceFile("WallpaperSetter.EyeDrop.cur");

        private Color _backColor;

        public Form1()
        {
            Point mCursorLocation;
            InitializeComponent();

            label2.Text = string.Format("Your screen resolution: {0}x{1}", ScreenSize.Width, ScreenSize.Height);
            label3.Text = " ";

            pictureBox1.MouseDown += (s, e) =>
            {
                amDragging = true;  // KBR TODO: dragging should be true ONLY if the initial mouse position is within the raw_wallpaper!!!
                start = e.Location; // KBR TODO: calc the delta relative to the raw_wallpaper ULC 
                pictureBox1.Cursor = Cursors.Cross;
            };
            pictureBox1.MouseMove += (s, e) =>
            {
                if (amDragging)
                {
                    start_x += e.X - start.X;
                    start_y += e.Y - start.Y;
                    start = e.Location;
                    draw_result();
                }
            };
            pictureBox1.MouseUp += (s, e) =>
            {
                amDragging = false;
                start = e.Location;
                pictureBox1.Cursor = Cursors.Arrow;
                draw_result();
            };

            btnDropper.MouseDown += (s, e) =>
            {
                m_MouseDownInEyedropper = true;
                Cursor = m_eyeDropperCursor;

                m_ScreenImage = Utility.CaptureScreen();

                mCursorLocation = Utility.GetCursorPostionOnScreen();
                _backColor = m_ScreenImage.GetPixel(mCursorLocation.X, mCursorLocation.Y);
            };
            btnDropper.MouseMove += (s, e) =>
            {
                if (m_MouseDownInEyedropper)
                {
                    mCursorLocation = Utility.GetCursorPostionOnScreen();
                    _backColor = m_ScreenImage.GetPixel(mCursorLocation.X, mCursorLocation.Y);
                    draw_result();
                }
            };
            btnDropper.MouseUp += (s, e) =>
            {
                m_MouseDownInEyedropper = false;
                m_ScreenImage.Dispose();
                Cursor = Cursors.Default;
                draw_result();
            };
        }

        private void btnLoadImage_Click(object sender, EventArgs e)
        {
            using (var o = new OpenFileDialog())
            {
                o.Filter = "Image files|*.jpg;*.bmp;*.png;*.jpeg;*.gif";
                o.CheckFileExists = true;
                if (o.ShowDialog() == DialogResult.OK)
                {
                    load_image(File.ReadAllBytes(o.FileName));
                }
            }
        }

        private Bitmap raw_wallpaper;

        private Color averageColor = Color.Black;

        private void load_image(byte[] data)
        {
            if (raw_wallpaper != null)
            {
                raw_wallpaper.Dispose();
            }

            raw_wallpaper = new Bitmap(new MemoryStream(data));

            label3.Text = string.Format("Wallpaper size: {0}x{1}", raw_wallpaper.Width, raw_wallpaper.Height);

            //PictureAnalysis.GetMostUsedColor(raw_wallpaper);
            PictureAnalysis.Analyze(raw_wallpaper);
            averageColor = PictureAnalysis.ColorAverage;

            comboBox1.Items.Clear();
            foreach (var top10Color in PictureAnalysis.Top10Colors)
            {
                comboBox1.Items.Add(top10Color.ToArgb());
            }

            panel1.BackColor = averageColor;
            _backColor = averageColor;

            draw_result();
        }

        private Bitmap bi;

        private void drawImage(Graphics gg, int in_x, int in_y, bool flip)
        {
            int x = Convert.ToInt32((Convert.ToDouble(in_x) / Convert.ToDouble(CanvasSize.Width)) * ScreenSize.Width);
            int y = Convert.ToInt32((Convert.ToDouble(in_y) / Convert.ToDouble(CanvasSize.Height)) * ScreenSize.Height);

            if (checkBox2.Checked)
            {
                // ignore flip!
                gg.DrawImage(raw_wallpaper, new Rectangle(x, y, ScreenSize.Width, ScreenSize.Height));
            }
            else if (chkScaleImage.Checked)
            {
                var dSize = raw_wallpaper.Size.ApplyAspect(ScreenSize);
                var w = flip ? -dSize.Width : dSize.Width;
                gg.DrawImage(raw_wallpaper, x, y, w, dSize.Height);
            }
            else
            {
                var w = flip ? -raw_wallpaper.Width : raw_wallpaper.Width;
                // Ignore image DPI by specifying output dimensions
                gg.DrawImage(raw_wallpaper, x, y, w, raw_wallpaper.Height);
            }
        }

        private double scale_factorW; // ratio of canvas to screen

        private void draw_result()
        {
            if (raw_wallpaper == null) return;
            //if (pictureBox1.Image != null)
            //{
            //    Image a = pictureBox1.Image;
            //    pictureBox1.Image = null;
            //    a.Dispose();
            //}

            if (bi == null)
            {
                bi = new Bitmap(ScreenSize.Width, ScreenSize.Height);
            }

            scale_factorW = (double)CanvasSize.Width/(double)ScreenSize.Width;

            using (Graphics gg = Graphics.FromImage(bi))
            {
                gg.Clear(_backColor);

                int y = checkBox1.Checked ? start_y : 0;
                drawImage(gg, start_x, y, false);
                if (chkMirror.Checked)
                {
                    int x2 = CanvasSize.Width - start_x; // necessary if NOT flipping: - (int)(raw_wallpaper.Width * scale_factorW);
                    drawImage(gg, x2, y, true);
                }
            }
            pictureBox1.Image = bi;
        }

        private void btnSetPaper_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                string file_name = Path.Combine(Path.GetTempPath(), "wallsetterimage.bmp");
                MemoryStream mem = new MemoryStream();
                pictureBox1.Image.Save(mem, ImageFormat.Bmp);
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

        private void btnSaveFile_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                using (SaveFileDialog d = new SaveFileDialog())
                {
                    d.Filter = "PNG Images|*.png|JPEG Images|*.jpg";

                    if (d.ShowDialog() == DialogResult.OK)
                    {
                        MemoryStream mem = new MemoryStream();
                        pictureBox1.Image.Save(mem, d.FileName.EndsWith(".jpg") ? ImageFormat.Jpeg : ImageFormat.Png);
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
            _backColor = averageColor;
            draw_result();
            comboBox1.SelectedIndex = -1;
        }

        private void btnResetPos_Click(object sender, EventArgs e)
        {
            start_x = 0;
            start_y = 0;
            draw_result();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int dex = comboBox1.SelectedIndex;
            if (dex < 0)
                return;
            Color c = Color.FromArgb((int) (comboBox1.Items[dex]));
            _backColor = c;
            draw_result();
        }

        private void comboBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;

            Graphics g = e.Graphics;
            Rectangle r = e.Bounds;
            int argb = (int)(((ComboBox) sender).Items[e.Index]);
            Color c = Color.FromArgb(argb);
            Brush b = new SolidBrush(c);
            g.FillRectangle(b, r);
        }

        private void chkScaleImage_CheckedChanged(object sender, EventArgs e)
        {
            if (chkScaleImage.Checked)
                checkBox2.Checked = false;
            draw_result();
        }

        private void chkMirror_CheckedChanged(object sender, EventArgs e)
        {
            if (chkMirror.Checked)
                checkBox2.Checked = false;
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

        public static bool is_smaller_than(this Size a1, Size a2)
        {
            return a1.Height < a2.Height && a1.Width < a2.Width;
        }


    }
}
