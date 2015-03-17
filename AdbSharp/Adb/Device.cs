//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Device.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace AdbSharp.Adb
{
	public class Device : IDevice
	{
		public Device (AndroidDeviceBridge adb, string deviceId, string deviceState)
		{
			this.Adb = adb;
			this.DeviceId = deviceId;
			this.State = deviceState;
		}

		public AndroidDeviceBridge Adb { get; private set; }

		public string DeviceId { get; private set; }

		public string State { get; private set; }

		public Task UnlockAsync ()
		{
			return UnlockAsync (CancellationToken.None);
		}

		public async Task UnlockAsync (CancellationToken cancelToken)
		{
			using (var client = await this.CreateAndConnectAsync (cancelToken).ConfigureAwait (false)) {
				await client.ExecuteCommandAsync (Commands.Device.Unlock).ConfigureAwait (false);
				await client.ReadCommandResponseAsync ();
			}
		}

		public async Task SendTapAsync (int x, int y)
		{
			using (var client = await this.CreateAndConnectAsync (CancellationToken.None).ConfigureAwait (false)) {
				await client.ExecuteCommandAsync (Commands.Device.GetInputTap (x, y)).ConfigureAwait (false);
				await client.ReadCommandResponseAsync ();
			}
		}

		public Task<Framebuffer> GetFramebufferAsync () 
		{
			return this.GetFramebufferAsync (CancellationToken.None);
		}

		public async Task<Framebuffer> GetFramebufferAsync (CancellationToken cancelToken) 
		{
			using (var client = await this.CreateAndConnectAsync (cancelToken)) {

				if (await client.ExecuteCommandAsync (Commands.Device.Framebuffer)) {

					var fm = new Framebuffer ();
					await fm.ReadFramebufferAsync (client.Stream);

					return fm;
				}
			}

			return null;
		}

		private async Task<Client> CreateAndConnectAsync (CancellationToken cancelToken)
		{
			var client = new Client (this.Adb);
			if (cancelToken.CanBeCanceled)
				cancelToken.Register (client.Dispose);
			
			await client.ConnectAsync ();

			var connected = await client.ExecuteCommandAsync (Commands.Host.Transport + this.DeviceId).ConfigureAwait (false);
			if (!connected)
				throw new Exception ("not connected");
			return client;
		}
	}
}