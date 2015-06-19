// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ILoggingService.cs" company="(c) Greg Munn">
//   (c) 2015 (c) Greg Munn  All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace AdbSharp.Utils
{
	/// <summary>
	/// Logs a messages
	/// </summary>
	public interface ILoggingService
	{
		/// <summary>
		/// Logs a message to the log
		/// </summary>
		void Log (DateTime timestamp, int threadId, LogLevel level, string message);
	}
}