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

namespace AdbSharpAddin
{
	internal class DevicesWidget : Gtk.VBox
	{
		private IDisposable deviceMonitor;
		private AndroidDeviceBridge adb;
		private IList<IDevice> devices;

		private IDevice currentDevice;
		private DockToolButton unlockButton;
		private DockToolButton screenshotButton;
		private Xwt.ImageView screenshot;
		private Xwt.ComboBox deviceDropDown;
		private Xwt.ScrollView scrollView;

		private int viewerWidth;
		private int viewerHeight;
		private Xwt.Drawing.Image lastImage;
		private double lastImageScale;

		public DevicesWidget (IPadWindow container) 
		{
			this.Setup ();
			this.Build (container);
			this.ShowAll ();
		}

		protected override void OnDestroyed ()
		{
			this.deviceMonitor.Dispose ();
			base.OnDestroyed ();
		}

		private void Setup ()
		{
			// TODO: pass in the adb path from ?
			this.adb = AndroidDeviceBridge.Create ();
			this.deviceMonitor = this.adb.TrackDevices (this.DevicesChanged, this.MonitorStopped);
		}

		private void Build (IPadWindow container)
		{
			var toolbar = container.GetToolbar (Gtk.PositionType.Top);

			deviceDropDown = new Xwt.ComboBox ();
			deviceDropDown.WidthRequest =  160;
			toolbar.Add (deviceDropDown.ToGtkWidget (), false);
			this.deviceDropDown.Items.Add ("Select Device");
			this.deviceDropDown.SelectedIndex = 0;
			this.deviceDropDown.SelectionChanged += this.DeviceDropDownSelectionChanged;

			this.unlockButton = new DockToolButton (null, "Unlock");
			this.screenshotButton = new DockToolButton (null, "Screenshot");
			toolbar.Add (this.unlockButton);
			toolbar.Add (this.screenshotButton);

			this.screenshot = new Xwt.ImageView ();
			this.scrollView = new Xwt.ScrollView ();
			this.scrollView.HeightRequest = 600;
			this.scrollView.Content = this.screenshot;
			this.PackStart (this.scrollView.ToGtkWidget (), false, true, 0);

			this.screenshot.ButtonPressed += this.ScreenshotButtonPressed;

			this.unlockButton.Clicked += (sender, e) => {
				this.UnlockDevice ();
			};

			this.screenshotButton.Clicked += (sender, e) => {
				this.TakeScreenshot ();
			};

			toolbar.ShowAll ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			this.viewerWidth = (int)this.screenshot.Size.Width;
			this.viewerHeight = (int)this.screenshot.Size.Height;
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
			lock (this.adb) {
				var ix = this.deviceDropDown.SelectedIndex;
				if (ix <= 0) {
					this.currentDevice = null;
				} else {
					this.currentDevice = this.devices [ix - 1];
				}

				this.unlockButton.Sensitive = ix != 0;
				this.screenshotButton.Sensitive = ix != 0;
				this.screenshot.Image = null;
			}
		}

		private void DevicesChanged (IList<IDevice> newDeviceList)
		{
			Xwt.Application.Invoke (() => {
				lock (this.adb) {
					var current = this.currentDevice;

					this.devices = newDeviceList;
					this.deviceDropDown.Items.Clear ();
					this.deviceDropDown.Items.Add ("Select Device");
					foreach (var d in newDeviceList) {
						this.deviceDropDown.Items.Add (d.DeviceId);
					}

					if (current != null) {
						var d = newDeviceList.FirstOrDefault (x => x.DeviceId == this.currentDevice.DeviceId);
						if (d == null) {
							this.deviceDropDown.SelectedIndex = 0;
							this.currentDevice = null;
						} else {
							this.deviceDropDown.SelectedIndex = newDeviceList.IndexOf (d) + 1;
						}
					} else {
						this.deviceDropDown.SelectedIndex = 0;
					}
				}
			});
		}

		private void MonitorStopped (Exception ex)
		{
			LoggingService.Log (MonoDevelop.Core.Logging.LogLevel.Warn, "Restarting Device Monitor");
			if (ex != null) 
				LoggingService.Log (MonoDevelop.Core.Logging.LogLevel.Warn, ex.ToString ());

			this.deviceMonitor = this.adb.TrackDevices (this.DevicesChanged, this.MonitorStopped);
		}

		private async void UnlockDevice ()
		{
			var device = this.currentDevice;
			if (device != null) {
				this.unlockButton.Sensitive = false;

				await device.UnlockAsync ();

				this.unlockButton.Sensitive = true;
			}
		}

		private async void TakeScreenshot ()
		{
			// TODO: resize the image
			var device = this.currentDevice;
			if (device != null) {
				this.screenshotButton.Sensitive = false;
				var buffer = await device.GetFramebufferAsync ();
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

				this.screenshotButton.Sensitive = true;
			}
		}

		private bool sendingTap;
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
					await device.SendTapAsync (screenX, screenY);
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
