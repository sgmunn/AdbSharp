//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Logging.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace AdbSharp.Utils
{
	public static class Logging
	{
		/// <summary>
		/// The default formatter for log output.
		/// </summary>
		public static Func<DateTime, int, LogLevel, string, string> DefaultLogFormatStr = LogFormat;

		static readonly BlockingCollection<LogItem> queue = new BlockingCollection<LogItem> (new ConcurrentQueue<LogItem> ());
		static readonly Dictionary<ILoggingService,LoggingLevel> logs = new Dictionary<ILoggingService, LoggingLevel> ();
		static readonly List<ILoggingService> filteredLogs = new List<ILoggingService> ();

		static Logging ()
		{
			// TODO: remove
			InitConsoleLogging ();

			Task.Factory.StartNew (ProcessLogs);
		}

		public static void RegisterLog (ILoggingService log, LoggingLevel level = LoggingLevel.UpToInfo)
		{
			lock (logs) {
				if (!logs.ContainsKey (log))
					logs.Add (log, level);
			}
		}

		public static void UnregisterLog (ILoggingService log)
		{
			lock (logs) {
				if (logs.ContainsKey (log))
					logs.Remove (log);
			}
		}

		public static void InitDebugLogging ()
		{
			RegisterLog (DebugLoggingService.Instance);
		}

		public static void InitConsoleLogging ()
		{
			RegisterLog (ConsoleLoggingService.Instance, LoggingLevel.All);
		}

		public static void Log (LogLevel level, string message)
		{
			queue.Add (new LogItem { ThreadId = Thread.CurrentThread.ManagedThreadId, Timestamp = DateTime.Now, Level = level, Message = message});
		}

		public static void LogDebug (string message, params object[] args)
		{
			Log (LogLevel.Debug, string.Format (message, args));
		}

		public static void LogInfo (string message, params object[] args)
		{
			Log (LogLevel.Info, string.Format (message, args));
		}

		public static void LogWarning (string message, params object[] args)
		{
			Log (LogLevel.Warn, string.Format (message, args));
		}

		public static void LogError (string message)
		{
			Log (LogLevel.Error, message);
		}

		public static void LogError (Exception ex)
		{
			if (ex != null) {
				Log (LogLevel.Error, ex.ToString ());
			} else {
				Log (LogLevel.Error, null);
			}
		}

		public static void LogError (string message, Exception ex)
		{
			if (ex != null) {
				Log (LogLevel.Error, (message ?? ex.Message) + "\n" + ex.ToString ());
			} else {
				Log (LogLevel.Error, message);
			}
		}

		static ILoggingService[] GetLogs (LogLevel level)
		{
			// TODO: optimise if required - check if the logs have changed since last time
			lock (logs) {
				return logs.Where (kv => ((int)kv.Value & (int)level) != 0).Select (kv => kv.Key).ToArray ();
			}
		}

		static void ProcessLogs ()
		{
			foreach (var item in queue.GetConsumingEnumerable ()) {
				LogItem currentItem = item;

				foreach (var log in GetLogs (currentItem.Level)) {
					log.Log (currentItem.Timestamp, currentItem.ThreadId, currentItem.Level, currentItem.Message);
				}
			}
		}

		static string LogFormat (DateTime timestamp, int threadId, LogLevel level, string message)
		{
			return string.Format ("{0:yyyy-MM-dd HH:mm:ss} [{1:00}]: {2} - {3}", timestamp, threadId, level, message);			
		}

		class LogItem
		{
			public DateTime Timestamp { get; set; }
			public LogLevel Level { get; set; }
			public string Message { get; set; }
			public int ThreadId { get; set; }
		}
	}
}