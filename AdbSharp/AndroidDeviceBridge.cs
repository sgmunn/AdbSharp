//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="AndroidDeviceBridge.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using System.Diagnostics;
using AdbSharp.Adb;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using AdbSharp.Utils;

namespace AdbSharp
{
	// TODO: more specific exceptions, audit AdbException
	// TODO: add cancellable overloads to AndroidDeviceBridge
	// TODO: add logging for client libraries to hook into
	// TODO: debug log command execution and responses, with response length

	// TODO: find and reconnect tcp devices if they are not connected
	// - find the ports 5555, 5557 etc, issue the connect command ??




	/*
	 * Monitor is cancelled via dispose
	 * other function calls are cancelled in the call itself, we create a client for each call, 
	 * if the cancellation token is cancelled, we just dispose the client. passing the cancellation token to the async tcp / stream
	 * methods doesn't help because they dont monitor the token - we need to check disposed ourselves
	 * 
	 */

	public sealed class AndroidDeviceBridge
	{
		public static AdbConfig DefaultConfig;

		public AdbConfig Config { get; private set; }

		static AndroidDeviceBridge ()
		{
			// TODO: determine if windows and add .exe, assumes in the path
			DefaultConfig = new AdbConfig ("adb");
		}

		private AndroidDeviceBridge (AdbConfig config)
		{
			this.Config = config;
		}

		public static AndroidDeviceBridge Create ()
		{
			return new AndroidDeviceBridge (DefaultConfig);
		}

		public static AndroidDeviceBridge Create (AdbConfig config)
		{
			return new AndroidDeviceBridge (config);
		}

		public Task<int> StartServerAsync ()
		{
			var task = Task.Factory.StartNew<int> (() => {
				try {
					return ProcessUtils.Start (this.Config.AdbExecutable, "start-server");
				}
				catch (Win32Exception ex) {
					if (ex.NativeErrorCode == 2) {
						throw new AdbException ("Adb was not found, invalid configuration");
					}
					else {
						throw;
					}
				}
			});

			return task;
		}

		public async Task<string> GetServerVersionAsync ()
		{
			using (var client = new Client (this)) {
				await client.ConnectAsync ();
				await client.ExecuteCommandAsync (Commands.Host.Version);
				var data = await client.ReadCommandResponseAsync ();
				return data;
			}
		}

		public async Task<IList<IDevice>> GetDevicesAsync ()
		{
			using (var client = new Client (this)) {
				await client.ConnectAsync ();
				await client.ExecuteCommandAsync (Commands.Host.Devices);

				var data = await client.ReadCommandResponseAsync ();
				return DeviceMonitor.ParseDeviceOutput (this, data);
			}
		}

		public DeviceMonitor TrackDevices(Action<IList<IDevice>> devicesChanged, Action<Exception> stopped)
		{
			var client = new Client (this);
			var monitor = new DeviceMonitor (client, devicesChanged, stopped);
			return monitor;
		}

		public async Task<IDevice> GetDefaultDeviceAsync ()
		{
			var devices = await this.GetDevicesAsync ();
			if (devices.Count < 1)
				throw new AdbException ("no devices connected");

			if (devices.Count > 1)
				throw new AdbException ("more than one device connected, specify device id");

			return devices [0];
		}

		public async Task<IDevice> GetDeviceByIdAsync (string deviceId)
		{
			var devices = await this.GetDevicesAsync ();
			var device = devices.FirstOrDefault (d => d.DeviceId == deviceId);
			if (device == null)
				throw new AdbException ("device not found");

			return device;
		}

		public async Task UnlockAsync ()
		{
			var device = await this.GetDefaultDeviceAsync ();
			await device.UnlockAsync ();
		}

		public async Task UnlockAsync (string deviceId)
		{
			var device = await GetDeviceByIdAsync (deviceId);
			await device.UnlockAsync ();
		}

		public async Task<Framebuffer> GetFramebufferAsync ()
		{
			var device = await this.GetDefaultDeviceAsync ();
			return await device.GetFramebufferAsync ();
		}

		public async Task<Framebuffer> GetFramebufferAsync (string deviceId)
		{
			var device = await GetDeviceByIdAsync (deviceId);
			return await device.GetFramebufferAsync ();
		}
	}
}