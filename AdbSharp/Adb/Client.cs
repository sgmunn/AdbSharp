//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Client.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using System.Threading;
using AdbSharp.Utils;

namespace AdbSharp.Adb
{
	/// <summary>
	/// Provides connectivity with an adb server instance
	/// </summary>
	public sealed class Client : IDisposable
	{
		private readonly object locker = new object ();
		private readonly CancellationTokenSource cancel;
		private TcpClient tcpClient;
		private Stream clientStream;
		private bool disposed;

		public Client (AndroidDeviceBridge adb)
		{
			this.Adb = adb;
			this.cancel = new CancellationTokenSource ();
		}

		public AndroidDeviceBridge Adb { get; private set; }

		public Stream Stream {
			get {
				return this.clientStream;
			}
		}

		public Task ConnectAsync ()
		{
			this.CheckDisposed ();
			if (this.tcpClient != null)
				throw new InvalidOperationException ("Adb Client is already connected.");
			
			return InternalConnectAsync (true);
		}

		public void Dispose ()
		{
			lock (this.locker) {
				if (!this.disposed) {
					this.disposed = true;
					this.cancel.Cancel ();
					this.tcpClient.Close ();
				}
			}
		}

		public async Task<bool> ExecuteCommandAsync (string command)
		{
			this.CheckDisposed ();
			var cmd = Commands.GetCommand (command);
			Logging.LogDebug ("Client: Executing command: {0}", command);

			await this.clientStream.WriteAsync (cmd, 0, cmd.Length, this.cancel.Token).ConfigureAwait (false);
			if (await this.CheckCommandStatusAsync ().ConfigureAwait (false)) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Reads the command response where the response is pre-pended with the length of the response
		/// </summary>
		public async Task<string> ReadCommandResponseWithLengthAsync ()
		{
			this.CheckDisposed ();

			try {
				Logging.LogDebug ("Client: Reading command response");
				var buffer = new byte[4096];
				var bytesRead = await clientStream.ReadAsync (buffer, 0, 4, this.cancel.Token).ConfigureAwait (false);

				if (bytesRead == 0) {
					Logging.LogDebug ("Client: 0 bytes returned, cannot continue reading command response");
					return null;
				}

				if (bytesRead == 4) {
					var responseLengthStr = Commands.GetCommandResponse (buffer, 0, 4);
					int responseLength = Int32.Parse (responseLengthStr, NumberStyles.HexNumber);
					Logging.LogDebug ("Client: command response is {0} bytes", responseLength);

					// now read the response
					int totalCount = 0;
					while (totalCount < responseLength) {
						int bytesToRead = responseLength - totalCount;
						if (bytesToRead > 4096)
							bytesToRead = 4096;
						
						bytesRead = await clientStream.ReadAsync (buffer, totalCount, bytesToRead, this.cancel.Token).ConfigureAwait (false);
						totalCount += bytesRead;
					}

					if (totalCount == responseLength) {
						var response = Commands.GetCommandResponse (buffer, 0, responseLength);

						return response;
					}

					throw new InvalidAdbResponseException ("Incomplete response received.");
				}

				throw new InvalidAdbResponseException ("Incorrect response length returned.");
			}
			catch (Exception ex) {
				if (this.cancel.IsCancellationRequested) {
					Logging.LogDebug ("Client: Command was cancelled");
					throw new TaskCanceledException ();
				}

				Logging.LogError ("Client: ", ex);
				throw;
			}
		}

		/// <summary>
		/// Reads the command response. The response is not pre-pended with the length of the result.
		/// </summary>
		public async Task<string> ReadCommandResponseAsync ()
		{
			this.CheckDisposed ();

			try {
				Logging.LogDebug ("Client: Reading command response");

				var sw = new StringWriter ();

				int totalCount = 0;
				var buffer = new byte[4096];
				var bytesRead = await clientStream.ReadAsync (buffer, 0, 4096, this.cancel.Token).ConfigureAwait (false);

				if (bytesRead == 0) {
					Logging.LogDebug ("Client: 0 bytes read");
					return null;
				}

				while (bytesRead > 0) {
					totalCount += bytesRead;
					var responseStr = Commands.GetCommandResponse (buffer, 0, bytesRead);
					sw.Write (responseStr);

					bytesRead = await clientStream.ReadAsync (buffer, 0, 4096, this.cancel.Token).ConfigureAwait (false);
				}

				Logging.LogDebug ("Client: {0} bytes read", totalCount);
				return sw.ToString ();
			}
			catch (Exception ex) {
				if (this.cancel.IsCancellationRequested) {
					Logging.LogDebug ("Client: Command was cancelled");
					throw new TaskCanceledException ();
				}

				Logging.LogError ("Client: ", ex);
				throw;
			}
		}

		private void CheckConnected ()
		{
			if (this.tcpClient == null || !this.tcpClient.Connected)
				throw new AdbConnectionException ("Client is not connected.");
		}

		private void CheckDisposed ()
		{
			if (this.disposed)
				throw new ObjectDisposedException ("Adb client has been disposed.");
		}

		private async Task InternalConnectAsync (bool allowStartServer)
		{
			var needsServerStart = false;

			// create a new client for each connection attempt
			this.tcpClient = new TcpClient ();
			try {
				await this.tcpClient.ConnectAsync (this.Adb.Config.Address, this.Adb.Config.Port).ConfigureAwait (false);
				if (!this.tcpClient.Connected)
					throw new AdbConnectionException ("Client did not connect.");
			}
			catch (SocketException ex) {
				if (ex.ErrorCode == 10061) {
					// server was not running, start it up
					needsServerStart = true;
				}
				else {
					throw;
				}
			}

			if (allowStartServer && needsServerStart) {
				// start up the adb server
				Logging.LogInfo ("Client: Starting the adb server process");
				var started = await this.Adb.StartServerAsync ().ConfigureAwait (false);
				if (started == 0) {
					await this.InternalConnectAsync (false);
				} else {
					throw new AdbServerException ("Adb server not started and failed to start.");
				}
			} else {
				// we should be connected now
				if (!this.tcpClient.Connected)
					throw new AdbConnectionException ("Client did not connect.");

				this.clientStream = tcpClient.GetStream ();
			}
		}

		private async Task<bool> CheckCommandStatusAsync ()
		{
			this.CheckDisposed ();

			var response = new byte[4];
			var bytesRead = await this.clientStream.ReadAsync (response, 0, 4).ConfigureAwait (false);
			if (bytesRead == 4) {
				var responseStr = Commands.GetCommandResponse (response, 0, bytesRead);

				if (responseStr == "OKAY") {
					return true;
				}

				if (responseStr == "FAIL") {
					Logging.LogDebug ("Client: Command responsed: {0}", responseStr);
					return false;
				}
			}

			throw new InvalidAdbResponseException ("Adb returned an invalid response.");
		}
	}
}