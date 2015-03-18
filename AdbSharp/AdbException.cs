//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="AdbException.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

using System;

namespace AdbSharp
{
	public class AdbException : Exception
	{
		public AdbException (string message) : base (message)
		{
		}	
	}

	public class AdbDeviceMonitorException : AdbException
	{
		public AdbDeviceMonitorException (string message) : base (message)
		{
		}	
	}

	public class AdbNotFoundException : AdbException
	{
		public AdbNotFoundException (string message) : base (message)
		{
		}	
	}

	public class InvalidAdbResponseException : AdbException
	{
		public InvalidAdbResponseException (string message) : base (message)
		{
		}	
	}

	public class AdbServerException : AdbException
	{
		public AdbServerException (string message) : base (message)
		{
		}	
	}

	public class TransportConnectFailedException : AdbException
	{
		public TransportConnectFailedException (string message) : base (message)
		{
		}	
	}

	public class AdbConnectionException : AdbException
	{
		public AdbConnectionException (string message) : base (message)
		{
		}	
	}

	public class DeviceNotFoundException : AdbException
	{
		public DeviceNotFoundException (string message) : base (message)
		{
		}	
	}

	public class UnexpectedDeviceCountException : AdbException
	{
		public UnexpectedDeviceCountException (string message) : base (message)
		{
		}	
	}
}