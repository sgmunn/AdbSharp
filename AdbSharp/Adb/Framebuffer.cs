//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Framebuffer.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------
using System;
using System.Threading.Tasks;
using System.IO;

namespace AdbSharp.Adb
{
	public sealed class Framebuffer
	{
		public struct FramebufferHeader
		{
			public int Version;
			public int Bpp;
			public int Size;
			public int Width;
			public int Height;
			public int RedOffset;
			public int RedLength;
			public int BlueOffset;
			public int BlueLength;
			public int GreenOffset;
			public int GreenLength;
			public int AlphaOffset;
			public int AlphaLength;
		}

		public FramebufferHeader Header { get; private set; }

		public byte [] Data { get; private set; }

		public async Task ReadFramebufferAsync (Stream stream)
		{
			var version = await ReadHeaderVersionAsync (stream).ConfigureAwait (false);
			if (version != 1)
				throw new Exception ("not supported");

			var header = await ReadHeaderAsync (stream, version).ConfigureAwait (false);

			// nudge ??

			var buffer = new byte [header.Size];
			int totalCount = 0;

			while (totalCount < (int)header.Size) {
				var bytesRead = await stream.ReadAsync (buffer, (int)totalCount, (int)header.Size - (int)totalCount);
				totalCount += bytesRead;
			}

			if (totalCount == (int)header.Size) {
				this.Header = header;
				this.Data = buffer;

				return;
			}

			throw new Exception ("failed to read");
		}

		private static async Task<int> ReadHeaderVersionAsync (Stream stream)
		{
			var buffer = new byte[4];
			var bytesRead = await stream.ReadAsync (buffer, 0, 4);

			if (bytesRead == 4) {
				int version;

				using(var ms = new MemoryStream(buffer)) {
					using (var buf = new BinaryReader(ms)) {
						version = buf.ReadInt32();
					}
				}

				return version;
			}

			return 0;
		}

		private static async Task<FramebufferHeader> ReadHeaderAsync (Stream stream, int version)
		{
			var header = new FramebufferHeader ();
			header.Version = version;

			// buffer is 12 * 4 bytes
			var buffer = new byte[12 * 4]; 
			var bytesRead = await stream.ReadAsync (buffer, 0, buffer.Length);

			if (bytesRead == buffer.Length) {
				using(var ms = new MemoryStream(buffer)) {
					using (var buf = new BinaryReader(ms)) {
						header.Bpp = buf.ReadInt32();
						header.Size = buf.ReadInt32();
						header.Width = buf.ReadInt32();
						header.Height = buf.ReadInt32();
						header.RedOffset = buf.ReadInt32();
						header.RedLength = buf.ReadInt32();
						header.BlueOffset = buf.ReadInt32();
						header.BlueLength = buf.ReadInt32();
						header.GreenOffset = buf.ReadInt32();
						header.GreenLength = buf.ReadInt32();
						header.AlphaOffset = buf.ReadInt32();
						header.AlphaLength = buf.ReadInt32();
					}
				}

				return header;
			}

			throw new Exception ("no header read");
		}
	}
}