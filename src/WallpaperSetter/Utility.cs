using System;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing.Imaging;

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
			Stream inputStream = System.Reflection.Assembly.GetExecutingAssembly().
				GetManifestResourceStream(cursorResourceName);
			byte[] buffer = new byte[inputStream.Length];
			inputStream.Read(buffer, 0, buffer.Length);
			inputStream.Close();

			// create temporary cursor file
			String tmpFileName = System.IO.Path.GetRandomFileName();
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
}
