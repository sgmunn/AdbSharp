//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="IVirtualDevice.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

using System;

namespace AdbSharp
{
	public interface IVirtualDevice
	{
		string Name { get; }
	}

	public interface IVirtualDeviceProvider
	{
		// list of virtual devices, eg: google emulators, geny motion etc
	}
}