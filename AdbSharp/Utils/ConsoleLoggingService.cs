// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConsoleLoggingService.cs" company="(c) Greg Munn">
//   (c) 2015 (c) Greg Munn  All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace AdbSharp.Utils
{
	sealed class ConsoleLoggingService : ILoggingService
	{
		public static readonly ILoggingService Instance = new ConsoleLoggingService ();

		private ConsoleLoggingService ()
		{
		}

		public void Log (DateTime timestamp, int threadId, LogLevel level, string message)
		{
			Console.WriteLine (Logging.DefaultLogFormatStr (timestamp, threadId, level, message));
		}
	}
}