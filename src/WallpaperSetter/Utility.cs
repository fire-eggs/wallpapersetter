using System;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;

namespace WallpaperSetter
{
	class Utility
	{
		/// <summary>
		/// The method just below is the work of Bingzhe Quan which I found in an article titled
		/// 'A scrollable, zoomable, and scalable picture box' at The Code Project 
		///     http://www.codeproject.com/cs/miscctrl/ScalablePictureBox.asp
		/// 
		/// Mr. Quan requested that the following text be included when distributing his work: 
		/// This is public domain software - that is, you can do whatever you want
		/// with it, and include it software that is licensed under the GNU or the
		/// BSD license, or whatever other licence you choose, including proprietary
		/// closed source licenses.  I do ask that you leave this lcHeader intact.
		///
		/// QAlbum.NET makes use of this control to display pictures.
		/// Please visit <a href="http://www.qalbum.net/en/">http://www.qalbum.net/en/</a>
		/// </summary>
		
		/// <summary>
		/// Create a cursor from an embedded cursor file
		/// 
		/// To set up the embedded cursor you must:
		/// 1. Create a cursor and save it in the same location as the .sln file for the solution.
		///    A free cursor editor that seems to work is 'RealWorld Cursor Editor 2006.1' This can 
		///    be found at http://www.rw-designer.com/cursor-maker
		/// 2. Open the app in VS2005
		/// 3. Click Project/ Add Existing Item and add the cursor file.
		/// 4. In the Solution Explorer, click the cursor and change Build Action to Embedded Resource.
		/// 5. Create the cursor by calling this method passing the cursar name as the argument. The name
		///    must include the namespace in which the cursar resides. Example call:
		///      Cursor mycursor = ColorCursor.CreateColorCursorFromResourceFile("Eyedropper.EyeDrop.cur");
		/// </summary>
		/// <param name="cursorResourceName">embedded cursor resource name</param>
		/// <returns>cursor</returns>
		public static Cursor CreateColorCursorFromResourceFile(String cursorResourceName)
		{
			// read cursor resource binary data
			Stream inputStream = Assembly.GetExecutingAssembly().
				GetManifestResourceStream(cursorResourceName);
			byte[] buffer = new byte[inputStream.Length];
			inputStream.Read(buffer, 0, buffer.Length);
			inputStream.Close();

			// create temporary cursor file
			String tmpFileName = Path.GetRandomFileName();
			FileInfo tempFileInfo = new FileInfo(tmpFileName);
			FileStream outputStream = tempFileInfo.Create();
			outputStream.Write(buffer, 0, buffer.Length);
			outputStream.Close();

			// create cursor
			IntPtr cursorHandle = LoadCursorFromFile(tmpFileName);
			Cursor cursor = new Cursor(cursorHandle);

			tempFileInfo.Delete();  // delete temporary cursor file
			return cursor;
		}

		/// <summary>
		/// Load colored cursor handle from an embedded resource
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		[DllImport("user32.dll", EntryPoint = "LoadCursorFromFileW", CharSet = CharSet.Unicode)]
		public static extern IntPtr LoadCursorFromFile(String str);

		/// <summary>
		/// Gets the position of the cursor in screen coordinates
		/// </summary>
		/// <param name="lpPoint"></param>
		/// <returns></returns>
		[DllImport("user32.dll")]
		static extern bool GetCursorPos(ref Point lpPoint);

		/// <summary>
		/// Returns a point containing the cursor location in screen coordinates
		/// </summary>
		public static Point GetCursorPostionOnScreen()
		{
			Point cursorLoc = new Point();
			GetCursorPos(ref cursorLoc);
			return cursorLoc;
		}

		/// <summary>
		/// Captures the entire screen and returns it as a bitmap.
		/// </summary>
		/// <returns></returns>
		public static Bitmap CaptureScreen()
		{
			// Set up a bitmap of the correct size
			Bitmap CapturedImage = new Bitmap(Screen.PrimaryScreen.Bounds.Width, 
				Screen.PrimaryScreen.Bounds.Height, PixelFormat.Format32bppArgb);
			// Create a graphics object from it
			using (Graphics g = Graphics.FromImage(CapturedImage))
			{
				// copy the entire screen to the bitmap
				g.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0,
					Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);
			}
			return CapturedImage;
		}

	}

    public static class PictureAnalysis
    {
        public static List<Color> Top10Colors { get; private set; }
        public static List<Color> TenMostUsedColors {get; private set;}
        public static List<int> TenMostUsedColorIncidences {get; private set;}
 
        public static Color MostUsedColor {get; private set;}
        public static int MostUsedColorIncidence {get; private set;}

        public static Color ColorAverage { get; private set; }
 
        private static int pixelColor;
 
        private static Dictionary<int, int> dctColorIncidence;

        public static void Analyze(Bitmap theImage)
        {
            int thumbSize = 32;
            Dictionary<Color, int> colors = new Dictionary<Color, int>();

            Bitmap thumbBmp =
                new Bitmap(theImage.GetThumbnailImage(
                    thumbSize, thumbSize, ThumbnailCallback, IntPtr.Zero));

            long r = 0;
            long g = 0;
            long b = 0;

            for (int i = 0; i < thumbSize; i++)
            {
                for (int j = 0; j < thumbSize; j++)
                {
                    Color col = thumbBmp.GetPixel(i, j);
                    r += col.R;
                    g += col.G;
                    b += col.B;
                    if (colors.ContainsKey(col))
                        colors[col]++;
                    else
                        colors.Add(col, 1);
                }
            }

            int count = thumbSize*thumbSize;
            ColorAverage = Color.FromArgb(Convert.ToInt32(r / count), Convert.ToInt32(g / count), Convert.ToInt32(b / count));

            List<KeyValuePair<Color, int>> keyValueList =
                new List<KeyValuePair<Color, int>>(colors);

            keyValueList.Sort(
                delegate(KeyValuePair<Color, int> firstPair,
                KeyValuePair<Color, int> nextPair)
                {
                    return nextPair.Value.CompareTo(firstPair.Value);
                });

            Top10Colors = new List<Color>();
            for (int i = 0; i < 10; i++)
                Top10Colors.Add(keyValueList[i].Key);

            //string top10Colors = "";
            //for (int i = 0; i < 10; i++)
            //{
            //    top10Colors += string.Format("\n {0}. {1} > {2}",
            //        i, keyValueList[i].Key.ToString(), keyValueList[i].Value);
            //    flowLayoutPanel1.Controls[i].BackColor = keyValueList[i].Key;
            //}
            //MessageBox.Show("Top 10 Colors: " + top10Colors);
        }

        public static bool ThumbnailCallback() { return false; }

        public static void GetMostUsedColor(Bitmap theBitMap)
        {
            TenMostUsedColors = new List<Color>();
            TenMostUsedColorIncidences = new List<int>();
 
            MostUsedColor = Color.Empty;
            MostUsedColorIncidence = 0;

            ColorAverage = Color.Empty;

            // does using Dictionary<int,int> here
            // really pay-off compared to using
            // Dictionary<Color, int> ?

            // would using a SortedDictionary be much slower, or ?

            dctColorIncidence = new Dictionary<int, int>();
            long r = 0;
            long g = 0;
            long b = 0;
            // this is what you want to speed up with unmanaged code
            for (int row = 0; row < theBitMap.Size.Width; row++)
            {
                for (int col = 0; col < theBitMap.Size.Height; col++)
                {
                    Color w = theBitMap.GetPixel(row, col);
                    pixelColor = w.ToArgb();
                    r += w.R;
                    g += w.G;
                    b += w.B;
                    if (dctColorIncidence.Keys.Contains(pixelColor))
                    {
                        dctColorIncidence[pixelColor]++;
                    }
                    else
                    {
                        dctColorIncidence.Add(pixelColor, 1);
                    }
                }
            }
            long count = theBitMap.Size.Width*theBitMap.Size.Height;
            ColorAverage = Color.FromArgb(Convert.ToInt32(r / count), Convert.ToInt32(g / count), Convert.ToInt32(b / count));

            // note that there are those who argue that a
            // .NET Generic Dictionary is never guaranteed
            // to be sorted by methods like this
            var dctSortedByValueHighToLow = dctColorIncidence.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
 
            // this should be replaced with some elegant Linq ?
            foreach (KeyValuePair<int, int> kvp in dctSortedByValueHighToLow.Take(10))
            {
                TenMostUsedColors.Add(Color.FromArgb(kvp.Key));
                TenMostUsedColorIncidences.Add(kvp.Value);
            }
 
            MostUsedColor = Color.FromArgb(dctSortedByValueHighToLow.First().Key);
            MostUsedColorIncidence = dctSortedByValueHighToLow.First().Value;
        }
 
    }
}
