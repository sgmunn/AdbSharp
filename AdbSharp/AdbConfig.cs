//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="AdbConfig.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

using System;
using System.Net;

namespace AdbSharp
{
	public class AdbConfig
	{
		public const int BridgeServerPort = 5037;

		public AdbConfig (string adbExecutable)
		{
			this.AdbExecutable = adbExecutable;
			this.Address = IPAddress.Loopback;
			this.Port = BridgeServerPort;
		}

		public AdbConfig (string adbExecutable, IPAddress address, int port)
		{
			this.AdbExecutable = adbExecutable;
			this.Address = address;
			this.Port = port;
		}

		public string AdbExecutable { get; private set; }
		public IPAddress Address { get; private set; }
		public int Port { get; private set; }
	}
}