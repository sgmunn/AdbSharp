//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="DeviceMonitor.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using AdbSharp.Adb;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdbSharp.Adb
{
	public sealed class DeviceMonitor : IDisposable
	{
		private readonly object locker = new object ();
		private readonly Client client;
		private Action<IList<IDevice>> devicesChanged;
		private Action<Exception> stopped;
		private bool disposed;
		private IList<IDevice> lastFoundDeviceList;

		public DeviceMonitor (Client client, Action<IList<IDevice>> devicesChanged, Action<Exception> stopped)
		{
			this.client = client;
			this.devicesChanged = devicesChanged;
			this.stopped = stopped;
			this.Start ();
		}

		public static IList<IDevice> ParseDeviceOutput (AndroidDeviceBridge adb, string deviceList)
		{
			var result = new List<IDevice> ();

			var devices = deviceList.Split (new [] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var device in devices) {
				var deviceInfo = device.Split (new [] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
				if (deviceInfo.Length == 2) {
					var d = new Device (adb, deviceInfo [0], deviceInfo [1]);
					result.Add (d);
				} else {
					// TODO: log that the info was wrong
				}
			}

			return result;
		}

		public void Dispose ()
		{
			lock (this.locker) {
				if (!this.disposed) {
					this.disposed = true;
					this.devicesChanged = null;
					this.stopped = null;
					this.client.Dispose ();
				}
			}
		}

		private void Start ()
		{
			ThreadPool.QueueUserWorkItem (this.Monitor);
		}

		private static void NotifyDevices (Action<IList<IDevice>> devicesChanged, IList<IDevice> devices)
		{
			if (devicesChanged != null)
				devicesChanged (devices);
		}

		private void NotifyStopped (Exception ex)
		{
			var handler = this.stopped;
			if (handler != null)
				handler (ex);
		}

		private async void Monitor (object state)
		{
			if (!(await this.ConnectAndStartAsync ()))
				return;
			
			while (!this.disposed) {
				try {
					var r = await this.client.ReadCommandResponseAsync ().ConfigureAwait (false);
					Debug.WriteLine ("{0}{1}", "devices: ", r);
					if (r == null) {
						Debug.WriteLine ("returned null");
						// most likely because adb server disappeared, restarted or network issue
						// we can try to reconnect
						this.NotifyStopped (new AdbException ("Server stopped tracking"));
						return;
					}

					if (this.disposed) {
						Debug.WriteLine ("done monitor, disposed");
						this.NotifyStopped (null);
						return;
					}

					var handler = this.devicesChanged;
					if (handler != null) {
						lastFoundDeviceList = DeviceMonitor.ParseDeviceOutput (this.client.Adb, r);
						NotifyDevices (handler, lastFoundDeviceList);
					}
				}
				catch (Exception ex) {
					Debug.WriteLine (ex);
					this.NotifyStopped (ex);
					return;
				}
			}

			Debug.WriteLine ("done monitor");
			this.NotifyStopped (null);
		}

		private async Task<bool> ConnectAndStartAsync ()
		{
			try {
				await client.ConnectAsync ().ConfigureAwait (false);

				// start the monitoring process
				var cmdResult = await this.client.ExecuteCommandAsync (Commands.Host.TrackDevices).ConfigureAwait (false);
				if (!cmdResult) {
					throw new AdbException ("Failed to start tracking service");
				}

				return true;
			} 
			catch (Exception ex) {
				this.NotifyStopped (ex);
			}

			return false;
		}
	}
}