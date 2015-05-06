//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="DevicesWidget.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

using System;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Docking;
using MonoDevelop.Components;
using System.Collections.Generic;
using AdbSharp;
using AdbSharp.Adb;
using System.IO;
using System.Drawing.Imaging;
using System.Linq;
using MonoDevelop.Core;
using System.Threading;
using System.Reflection;

namespace AdbSharpTools
{
	internal class DevicesWidget : Gtk.VBox
	{
		private IDisposable deviceMonitor;
		private AndroidDeviceBridge adb;
		private IList<IDevice> devices;

		private IDevice currentDevice;
		private DockToolButton connectButton;
		private DockToolButton unlockButton;
		private DockToolButton screenshotButton;
		private Xwt.ImageView screenshot;
		private Xwt.ComboBox deviceDropDown;
		private Xwt.ScrollView scrollView;

		private int viewerWidth;
		private int viewerHeight;
		private Xwt.Drawing.Image lastImage;
		private double lastImageScale;
		private bool sendingTap;

		// TODO: cancellation of running tasks
		// TODO: log errors from AdbSharp

		public DevicesWidget (IPadWindow container) 
		{
			this.Build (container);
			this.ShowAll ();
		}

		protected override void OnDestroyed ()
		{
			if (this.deviceMonitor != null)
				this.deviceMonitor.Dispose ();
			base.OnDestroyed ();
		}

		private void Build (IPadWindow container)
		{
			var toolbar = container.GetToolbar (Gtk.PositionType.Top);

			deviceDropDown = new Xwt.ComboBox ();
			deviceDropDown.WidthRequest =  160;
			this.deviceDropDown.Items.Add ("Select Device");
			this.deviceDropDown.SelectedIndex = 0;
			this.deviceDropDown.SelectionChanged += this.DeviceDropDownSelectionChanged;

			this.connectButton = new DockToolButton (null, "C") { TooltipText = "Connect to ADB" };
			this.unlockButton = new DockToolButton (null, "Unlock") { TooltipText = "Unlock the device" };
			this.screenshotButton = new DockToolButton (null, "Screenshot") { TooltipText = "Take a screenshot" };

			toolbar.Add (this.connectButton);
			toolbar.Add (deviceDropDown.ToGtkWidget (), false);
			toolbar.Add (this.unlockButton);
			toolbar.Add (this.screenshotButton);

			this.screenshot = new Xwt.ImageView ();
			this.scrollView = new Xwt.ScrollView ();
			this.scrollView.HeightRequest = 600;
			this.scrollView.Content = this.screenshot;
			this.PackStart (this.scrollView.ToGtkWidget (), false, true, 0);

			this.connectButton.Clicked += (sender, e) => this.ConnectDisconnect ();
			this.screenshot.ButtonPressed += this.ScreenshotButtonPressed;
			this.unlockButton.Clicked += (sender, e) => this.UnlockDevice ();
			this.screenshotButton.Clicked += (sender, e) => this.TakeScreenshot ();

			this.SetButtonStates ();

			toolbar.ShowAll ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			this.viewerWidth = (int)this.screenshot.Size.Width;
			this.viewerHeight = (int)this.screenshot.Size.Height;
		}

		private static string GetMonoDroidSdk ()
		{
			try {
				var androidTools = AppDomain.CurrentDomain.GetAssemblies ().FirstOrDefault (a => a.GetType ("Xamarin.AndroidTools.AndroidSdk") != null);
				if (androidTools != null) {
					var androidSdk = androidTools.GetType ("Xamarin.AndroidTools.AndroidSdk");
					var adbProperty = androidSdk.GetProperty ("AdbExe");
					var adbExe = (string)adbProperty.GetValue (null);

					// sometimes this won't have been initialised and we expect it to exist, or be blank.
					if (File.Exists (adbExe))
						return adbExe;
				}

			} catch (Exception ex) {
				return string.Empty;
			}

			return string.Empty;
		}

		private void UpdateImage ()
		{
			lock (this.adb) {
				
				if (this.lastImage != null) {
					var scale = this.CalculateScale ();
					this.screenshot.Image = this.lastImage.Scale (scale);
				}
			}
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

				// TODO: show all devices in a list with their current state
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
				LoggingService.Log (MonoDevelop.Core.Logging.LogLevel.Error, string.Format ("Android Device Monitor Stopped\n{0}", ex));
			else
				LoggingService.Log (MonoDevelop.Core.Logging.LogLevel.Error, "Android Device Monitor Stopped");
			
			Xwt.Application.Invoke (() => {
				this.Disconnect ();
				this.SetButtonStates ();
			});
		}

		private void SetButtonStates ()
		{
			this.deviceDropDown.Sensitive = this.adb != null && this.devices != null && this.devices.Count > 0;
			this.unlockButton.Sensitive = this.adb != null && this.currentDevice != null;
			this.screenshotButton.Sensitive = this.adb != null && this.currentDevice != null;
			if (this.adb == null) {
				this.screenshot.Image = null;
			}
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
			if (device != this.currentDevice)
				this.screenshot.Image = null;

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

		private void Connect ()
		{
			this.Disconnect ();

			// TODO: test the connection and put into status bar if we failed to connect, or something like that
			// TODO: log failure to start

			var config = new AdbConfig (GetMonoDroidSdk ());//Xamarin.AndroidTools.AndroidSdk.AdbExe);
			this.adb = AndroidDeviceBridge.Create (config);
			this.deviceMonitor = this.adb.TrackDevices (this.DevicesChanged, this.MonitorStopped);
			this.SetButtonStates ();
			this.connectButton.TooltipText = "Disconnect from ADB";
			this.connectButton.Label = "D";
		}

		private void Disconnect ()
		{
			if (this.deviceMonitor != null) {
				this.deviceMonitor.Dispose ();
				this.deviceMonitor = null;
			}

			this.adb = null;
			this.ClearDeviceDropDown ();
			this.SetButtonStates ();
			this.connectButton.TooltipText = "Connect to ADB";
			this.connectButton.Label = "C";
		}

		private async void UnlockDevice ()
		{
			var device = this.currentDevice;
			if (device != null) {
				this.unlockButton.Sensitive = false;

				await device.UnlockAsync (CancellationToken.None);

				this.unlockButton.Sensitive = true;
			}
		}

		private async void TakeScreenshot ()
		{
			var device = this.currentDevice;
			if (device != null) {
				this.screenshotButton.Sensitive = false;
				var buffer = await device.GetFramebufferAsync (CancellationToken.None);
				if (buffer != null) {
					var img = await buffer.ToImageAsync ();

					using (var ms = new MemoryStream ()) {
						img.Save (ms, ImageFormat.Bmp);
						ms.Position = 0;

						lock (this.adb) {
							this.lastImage = Xwt.Drawing.Image.FromStream (ms);
							this.lastImageScale = this.CalculateScale ();
							this.screenshot.Image = this.lastImage.Scale (this.lastImageScale);
						}
					}
				}

				this.screenshotButton.Sensitive = true;
			}
		}

		private async void ScreenshotButtonPressed (object sender, Xwt.ButtonEventArgs e)
		{
			if (this.sendingTap)
				return;
			
			var device = this.currentDevice;
			if (this.lastImage != null && device != null && e.Button == Xwt.PointerButton.Left) {
				var screenX = (int)(e.X / this.lastImageScale);
				var screenY = (int)(e.Y / this.lastImageScale);

				this.sendingTap = true;
				try {
					await device.SendTapAsync (screenX, screenY, CancellationToken.None);
					// trigger a screenshot
					this.TakeScreenshot ();
				}
				finally {
					this.sendingTap = false;
				}
			}
		}

		private double CalculateScale ()
		{
			var w1 = this.lastImage.Width;
			var w2 = this.viewerWidth;
			var h1 = this.lastImage.Height;
			var h2 = this.viewerHeight;

			var s1 = w2 / w1;
			var s2 = h2 / h1;
			if (s1 > s2)
				return s2;

			return s1;
		}
	}
}
