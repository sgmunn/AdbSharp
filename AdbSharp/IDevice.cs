//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="IDevice.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------
using System;
using AdbSharp.Adb;
using System.Threading.Tasks;
using System.Threading;

namespace AdbSharp
{
	// TODO: shell command, sync, reboot, logcat



	public interface IDevice
	{
		string DeviceId { get; }
		string State { get; }

		Task UnlockAsync (CancellationToken cancelToken);
		Task SendTapAsync (int x, int y, CancellationToken cancelToken);
		Task<Framebuffer> GetFramebufferAsync (CancellationToken cancelToken);
		Task<string> ExecuteShellCommand (string command, CancellationToken cancelToken);
	}
}