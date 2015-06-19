// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LogLevel.cs" company="(c) Greg Munn">
//   (c) 2015 (c) Greg Munn  All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace AdbSharp.Utils
{
	public enum LogLevel
	{
		Error = 1,
		Warn  = 2,
		Info  = 4,
		Debug = 8,
	}

	[Flags]
	public enum LoggingLevel
	{
		Error = 1,
		Warn  = 2,
		Info  = 4,
		Debug = 8,
		None  = 0,

		UpToError = Error,
		UpToWarn  = Warn  | UpToError,
		UpToInfo  = Info  | UpToWarn,
		UpToDebug = Debug | UpToInfo,

		All = UpToDebug
	}
}