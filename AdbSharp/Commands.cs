//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Commands.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

using System;
using System.Text;

namespace AdbSharp
{
	public static class Commands
	{
		private readonly static Encoding TextEncoding = new UTF8Encoding (false); // ISO-8859-1

		public static class Host
		{
			public const string Version = "host:version";
			public const string TrackDevices = "host:track-devices";
			public const string Transport = "host:transport:";
			public const string Devices = "host:devices";
		}

		public static class Device
		{
			public const string Framebuffer = "framebuffer:";
			public const string Unlock = "shell:input keyevent 82";
			public const string InputTap = "shell:input tap";
			public const string GetProp = "shell:getprop";

			public static string GetInputTap (int x, int y)
			{
				return InputTap + string.Format (" {0} {1}", x, y);
			}

			public static string GetGetProp (string property)
			{
				return GetProp + string.Format (" {0}", property);
			}
		}

		public static byte[] GetCommand (string command)
		{
			if (string.IsNullOrEmpty (command))
				throw new ArgumentException ("command cannot be null or empty");
			
			string commandStr = string.Format("{0}{1}", command.Length.ToString("X4"), command);
			return TextEncoding.GetBytes (commandStr);
		}

		public static string GetCommandResponse (byte[] reponseData, int offset, int count)
		{
			return TextEncoding.GetString (reponseData, offset, count);
		}
	}
}