// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DebugLoggingService.cs" company="(c) Greg Munn">
//   (c) 2015 (c) Greg Munn  All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace AdbSharp.Utils
{
	sealed class DebugLoggingService : ILoggingService
	{
		public static readonly ILoggingService Instance = new DebugLoggingService ();

		private DebugLoggingService ()
		{
		}

		public void Log (DateTime timestamp, int threadId, LogLevel level, string message)
		{
			System.Diagnostics.Debug.WriteLine (Logging.DefaultLogFormatStr (timestamp, threadId, level, message));
		}
	}
}