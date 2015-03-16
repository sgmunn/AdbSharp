//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="FramebufferImageExtensions.cs" company="(c) Ryan Conrad">
//  http://madb.codeplex.com
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdbSharp.Adb
{
	/// <summary>
	/// ToImage extension from Managed Android Debug Bridge (MadBee)
	/// http://madb.codeplex.com
	/// </summary>
	public static class FramebufferImageExtensions
	{
		public static Task<Image> ToImageAsync (this Framebuffer framebuffer) 
		{
			var tcs = new TaskCompletionSource<Image> ();

			Task.Factory.StartNew (() => {
				var img = ToImage (framebuffer, framebuffer.Header.Bpp == 32 ? PixelFormat.Format32bppArgb : PixelFormat.Format16bppRgb565 );
				tcs.SetResult (img);
			});

			return tcs.Task;
		}

		public static Image ToImage (this Framebuffer framebuffer) 
		{
			return ToImage (framebuffer, framebuffer.Header.Bpp == 32 ? PixelFormat.Format32bppArgb : PixelFormat.Format16bppRgb565 );
		}

		public static Image ToImage (this Framebuffer framebuffer, PixelFormat format) 
		{
			Bitmap bitmap = null;
			Bitmap image = null;
			BitmapData bitmapdata = null;

			bitmap = new Bitmap ( framebuffer.Header.Width, framebuffer.Header.Height, format );
			bitmapdata = bitmap.LockBits ( new Rectangle ( 0, 0, framebuffer.Header.Width, framebuffer.Header.Height ), ImageLockMode.WriteOnly, format );
			image = new Bitmap ( framebuffer.Header.Width, framebuffer.Header.Height, format );
			var tdata = framebuffer.Data;
			if ( framebuffer.Header.Bpp == 32 ) {
				tdata = Swap ( tdata );
			}
			Marshal.Copy ( tdata, 0, bitmapdata.Scan0, framebuffer.Header.Size );
			bitmap.UnlockBits ( bitmapdata );
			using ( Graphics g = Graphics.FromImage ( image ) ) {
				g.DrawImage ( bitmap, new Point ( 0, 0 ) );
				return image;
			}
		}

		private static void IntReverseForRawImage ( this byte[] source, Action<byte[]> action ) {
			const int step = 4;
			for ( int i = 0; i < source.Length; i += step ) {
				var b = new byte[step];
				for ( int x = b.Length - 1; x >= 0; --x ) {
					b[( step - 1 ) - x] = source[i + x];
				}

				b[2] = source[i + 0];
				b[1] = source[i + 1];
				b[0] = source[i + 2];
				b[3] = source[i + 3];

				action ( b );
			}
		}

		private static byte[] Swap ( byte[] b ) {
			var clone = new List<byte> ( );
			b.IntReverseForRawImage ( bitem => {
				clone.AddRange ( bitem );
			} );
			return clone.ToArray ( );
		}
	}
}
