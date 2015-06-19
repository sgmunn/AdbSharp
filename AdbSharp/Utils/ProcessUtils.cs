//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ProcessUtils.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

namespace AdbSharp.Utils
{
	public static class ProcessUtils
	{
		// TODO: provide a way to capture output
		public static int Start (string cmd, string args, string workingDirectory = null)
		{
			var psi = new ProcessStartInfo (cmd, args);
			if (!string.IsNullOrEmpty (workingDirectory))
				psi.WorkingDirectory = workingDirectory;
			
			psi.WindowStyle = ProcessWindowStyle.Hidden;
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;

			psi.RedirectStandardOutput = true;
			psi.RedirectStandardError = true;

			var process = new Process ();
			process.StartInfo = psi;

			process.OutputDataReceived += (sender, e) => {
				Logging.LogDebug (e.Data);
			};
			process.EnableRaisingEvents = true;

			process.Start ();
			process.BeginOutputReadLine ();

			process.WaitForExit ();
			return process.ExitCode;
		}
	}
}