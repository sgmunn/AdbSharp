//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Device.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------
using System;
using System.Threading.Tasks;
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

		public async Task UnlockAsync (CancellationToken cancelToken)
		{
			var client = await this.CreateAndConnectToTransportAsync (cancelToken).ConfigureAwait (false);

			using (client) {
				await client.ExecuteCommandAsync (Commands.Device.Unlock).ConfigureAwait (false);
				await client.ReadCommandResponseAsync ().ConfigureAwait (false);
			}
		}

		public async Task SendTapAsync (int x, int y, CancellationToken cancelToken)
		{
			var client = await this.CreateAndConnectToTransportAsync (cancelToken).ConfigureAwait (false);
			using (client) {
				await client.ExecuteCommandAsync (Commands.Device.GetInputTap (x, y)).ConfigureAwait (false);
				await client.ReadCommandResponseAsync ().ConfigureAwait (false);
			}
		}

		public async Task<Framebuffer> GetFramebufferAsync (CancellationToken cancelToken) 
		{
			var client = await this.CreateAndConnectToTransportAsync (cancelToken).ConfigureAwait (false);
			using (client) {
				if (await client.ExecuteCommandAsync (Commands.Device.Framebuffer).ConfigureAwait (false)) {
					var fm = new Framebuffer ();
					await fm.ReadFramebufferAsync (client.Stream).ConfigureAwait (false);

					return fm;
				}
			}

			return null;
		}

		private async Task<Client> CreateAndConnectToTransportAsync (CancellationToken cancelToken)
		{
			var client = await this.Adb.CreateAndConnectAsync (cancelToken).ConfigureAwait (false);

			var connected = await client.ExecuteCommandAsync (Commands.Host.Transport + this.DeviceId).ConfigureAwait (false);
			if (!connected)
				throw new TransportConnectFailedException ("Did not connect to device transport.");
			return client;
		}
	}
}