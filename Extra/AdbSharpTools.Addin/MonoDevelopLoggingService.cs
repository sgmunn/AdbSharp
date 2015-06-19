// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MonoDevelopLoggingService.cs" company="(c) Greg Munn">
//   (c) 2015 (c) Greg Munn  All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using MonoDevelop.Core;
using AdbSharp.Utils;

namespace AdbSharpTools
{
	/// <summary>
	/// Logging service for writing to a specific log file owned and managed by MonoDevelop
	/// </summary>
	internal class MonoDevelopLoggingService : ILoggingService
	{
		public static readonly ILoggingService Instance = new MonoDevelopLoggingService ();

		private static TextWriter logWriter;

		private MonoDevelopLoggingService ()
		{
		}

		private static TextWriter LogWriter { 
			get {
				if (logWriter == null) {
					logWriter = LoggingService.CreateLogFile ("AdbSharpTools");
				}

				return logWriter;
			}
		}

		public void Log (DateTime timestamp, int threadId, LogLevel level, string message)
		{
			LogWriter.WriteLine (Logging.DefaultLogFormatStr (timestamp, threadId, level, message));
		}
	}
}
