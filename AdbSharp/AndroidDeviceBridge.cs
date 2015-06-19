//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="AndroidDeviceBridge.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using AdbSharp.Adb;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using AdbSharp.Utils;
using System.Threading;

namespace AdbSharp
{
	// TODO: debug log command execution and responses, with response length

	// TODO: find and reconnect tcp devices if they are not connected
	// - find the ports 5555, 5557 etc, issue the connect command ??

	/// <summary>
	/// Android Device Bridge ('Adb')
	/// </summary>
	public sealed class AndroidDeviceBridge
	{
		/// <summary>
		/// The default configuration of adb. By default we will assume that adb is in the path, if adb is not in the 
		/// path, supply a new AdbConfig with the correct path.
		/// </summary>
		public static AdbConfig DefaultConfig;

		public AdbConfig Config { get; private set; }

		static AndroidDeviceBridge ()
		{
			DefaultConfig = Platform.IsWindows ? new AdbConfig ("adb.exe") : new AdbConfig ("adb");
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
						throw new AdbNotFoundException ("Adb was not found, invalid or missing configuration.");
					}
					else {
						throw;
					}
				}
			});

			return task;
		}

		public async Task<string> GetServerVersionAsync (CancellationToken cancelToken)
		{
			var client = await this.CreateAndConnectAsync (cancelToken).ConfigureAwait (false);
			using (client) {
				await client.ConnectAsync ().ConfigureAwait (false);
				await client.ExecuteCommandAsync (Commands.Host.Version).ConfigureAwait (false);

				var data = await client.ReadCommandResponseAsync ().ConfigureAwait (false);
				return data;
			}
		}

		public async Task<IList<IDevice>> GetDevicesAsync (CancellationToken cancelToken)
		{
			var client = await this.CreateAndConnectAsync (cancelToken).ConfigureAwait (false);
			using (client) {
				await client.ConnectAsync ().ConfigureAwait (false);
				await client.ExecuteCommandAsync (Commands.Host.Devices).ConfigureAwait (false);

				var data = await client.ReadCommandResponseAsync ().ConfigureAwait (false);
				return DeviceMonitor.ParseDeviceOutput (this, data);
			}
		}

		public DeviceMonitor TrackDevices(Action<IList<IDevice>> devicesChanged, Action<Exception> stopped)
		{
			var client = new Client (this);
			var monitor = new DeviceMonitor (client, devicesChanged, stopped);
			return monitor;
		}


		/// <summary>
		/// Gets the device that is connected as long as there is only one device connected
		/// </summary>
		public async Task<IDevice> GetDefaultDeviceAsync (CancellationToken cancelToken)
		{
			var devices = await this.GetDevicesAsync (cancelToken).ConfigureAwait (false);
			if (devices.Count < 1)
				throw new UnexpectedDeviceCountException ("There are no devices connected.");

			if (devices.Count > 1)
				throw new UnexpectedDeviceCountException ("There is more than one device connected, specify the device id instead.");

			return devices [0];
		}

		public async Task<IDevice> GetDeviceByIdAsync (string deviceId, CancellationToken cancelToken)
		{
			var devices = await this.GetDevicesAsync (cancelToken).ConfigureAwait (false);
			var device = devices.FirstOrDefault (d => d.DeviceId == deviceId);
			if (device == null)
				throw new DeviceNotFoundException ("The specified device was not found.");

			return device;
		}

		internal async Task<Client> CreateAndConnectAsync (CancellationToken cancelToken)
		{
			var client = new Client (this);
			if (cancelToken.CanBeCanceled)
				cancelToken.Register (client.Dispose);

			await client.ConnectAsync ().ConfigureAwait (false);
			return client;
		}

	}
}