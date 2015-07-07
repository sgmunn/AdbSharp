// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeviceMonitorWidget.cs" company="(c) Greg Munn">
//   (c) 2015 (c) Greg Munn  All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using AdbSharp;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using AdbSharp.Utils;

namespace AdbSharpTools.Widgets
{
	/// <summary>
	/// Widget that manages a ComboBox that represents the devices attached to ADB
	/// </summary>
	internal sealed class DeviceMonitorWidget : IDisposable
	{
		private readonly string padName;
		private readonly IDeviceMonitorTarget target;
		private IDisposable deviceMonitor;
		private AndroidDeviceBridge adb;
		private IList<IDevice> devices;

		private IDevice currentDevice;
		private Xwt.ComboBox deviceDropDown;

		public DeviceMonitorWidget (string padName, IDeviceMonitorTarget target) 
		{
			this.padName = padName;
			this.target = target;
			this.Build ();
		}

		public AndroidDeviceBridge Adb {
			get {
				return this.adb;
			}
		}

		public bool Connected {
			get {
				return this.adb != null;
			}
		}

		public bool ConnectedToDevice {
			get {
				return this.adb != null && this.currentDevice != null;
			}
		}

		public IDevice CurrentDevice { 
			get {
				return this.currentDevice;
			}
		}

		public void Dispose ()
		{
			if (this.deviceMonitor != null)
				this.deviceMonitor.Dispose ();
		}

		public void Connect ()
		{
			this.Disconnect ();

			var config = new AdbConfig (GetMonoDroidSdk ());
			this.adb = AndroidDeviceBridge.Create (config);
			this.deviceMonitor = this.adb.TrackDevices (this.DevicesChanged, this.MonitorStopped);
			this.SetButtonStates ();
		}

		public void Disconnect ()
		{
			if (this.deviceMonitor != null) {
				this.deviceMonitor.Dispose ();
				this.deviceMonitor = null;
			}

			this.adb = null;
			this.ClearDeviceDropDown ();
			this.SetButtonStates ();
		}

		private void Build ()
		{
			this.deviceDropDown = this.target.DeviceDropDown;
			if (this.deviceDropDown != null) {
				this.deviceDropDown.Items.Add ("Select Device");
				this.deviceDropDown.SelectedIndex = 0;
				this.deviceDropDown.SelectionChanged += this.DeviceDropDownSelectionChanged;
			}

			this.SetButtonStates ();
		}

		private void DeviceDropDownSelectionChanged (object sender, EventArgs e)
		{
			var ix = this.deviceDropDown.SelectedIndex;

			var device = ix <= 0 ? null : this.devices [ix - 1];
			this.SetCurrentDevice (device);
		}

		private void DevicesChanged (IList<IDevice> newDeviceList)
		{
			Xwt.Application.Invoke (() => {
				var current = this.currentDevice;
				this.devices = newDeviceList;
				this.ClearDeviceDropDown ();

				var onlineDevices = newDeviceList.Where (x => x.State == "device").ToList ();

				foreach (var d in onlineDevices) {
					this.deviceDropDown.Items.Add (d.DeviceId);
				}

				if (current != null) {
					var d = onlineDevices.FirstOrDefault (x => x.DeviceId == current.DeviceId);
					if (d != null) {
						this.deviceDropDown.SelectedIndex = onlineDevices.IndexOf (d) + 1;
						this.SetCurrentDevice (onlineDevices [0]);
					}
				}

				this.SetButtonStates ();

				// default if only one device
				if (this.deviceDropDown.SelectedIndex <= 0 && onlineDevices.Count == 1) {
					this.deviceDropDown.SelectedIndex = 1;
					this.SetCurrentDevice (onlineDevices [0]);
				}
			});
		}

		private void MonitorStopped (Exception ex)
		{
			if (ex != null)
				Logging.LogWarning ("{0}: Android Device Monitor threw an exception", padName);
			else
				Logging.LogWarning ("{0}: Android Device Monitor stopped", padName);

			Xwt.Application.Invoke (() => {
				this.Disconnect ();
				this.SetButtonStates ();
			});
		}

		private void SetButtonStates ()
		{
			this.deviceDropDown.Sensitive = this.adb != null && this.devices != null && this.devices.Count > 0;
			this.target.SetButtonStates (this);
		}

		private void ClearDeviceDropDown ()
		{
			this.deviceDropDown.SelectedIndex = 0;

			while (this.deviceDropDown.Items.Count > 1) {
				this.deviceDropDown.Items.RemoveAt (1);
			}
		}

		private void SetCurrentDevice (IDevice device)
		{
			this.currentDevice = device;
			this.SetButtonStates ();
		}

		private void ConnectDisconnect ()
		{
			if (this.adb == null) {
				this.Connect ();
			} else {
				this.Disconnect ();
			}
		}

		private string GetMonoDroidSdk ()
		{
			try {
				var androidTools = AppDomain.CurrentDomain.GetAssemblies ().FirstOrDefault (a => a.GetType ("Xamarin.AndroidTools.AndroidSdk") != null);
				if (androidTools != null) {
					var androidSdk = androidTools.GetType ("Xamarin.AndroidTools.AndroidSdk");
					var adbProperty = androidSdk.GetProperty ("AdbExe");
					var adbExe = (string)adbProperty.GetValue (null);

					// sometimes this won't have been initialised and we expect it to exist, or be blank.
					if (File.Exists (adbExe)) {
						Logging.LogInfo ("{0}: Found adb - '{1}'", this.padName, adbExe);
						return adbExe;
					}

					Logging.LogWarning ("{0}: Located MonoDroidSdk but adb was not found, assuming adb is in the path", this.padName);
				}
			} catch (Exception ex) {
				Logging.LogError (ex);
			}

			Logging.LogWarning ("{0}: Could not locate ADB, assuming it is in the path", this.padName);
			return string.Empty;
		}
	}
}