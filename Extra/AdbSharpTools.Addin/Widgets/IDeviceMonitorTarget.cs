// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDeviceMonitorTarget.cs" company="(c) Greg Munn">
//   (c) 2015 (c) Greg Munn  All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace AdbSharpTools.Widgets
{
	internal interface IDeviceMonitorTarget
	{
		Xwt.ComboBox DeviceDropDown { get; }
		void SetButtonStates (DeviceMonitorWidget monitor);
	}
}
