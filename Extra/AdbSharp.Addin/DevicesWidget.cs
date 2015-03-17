//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="DevicesWidget.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

using System;
//using Gtk;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Docking;
using MonoDevelop.Components;
using System.Collections.Generic;
using AdbSharp;
using AdbSharp.Adb;
//using Xwt;
using System.IO;
using System.Drawing.Imaging;
using System.Linq;
using Gtk;

namespace AdbSharpAddin
{
	internal class DevicesWidget : Gtk.VBox
	{
		private IDisposable deviceMonitor;
		private AndroidDeviceBridge adb;
		private IList<IDevice> devices;

		private IDevice currentDevice;
		private Gtk.Button unlockButton;
		private Gtk.Button screenshotButton;
		private Xwt.ImageView screenshot;
		private Xwt.ComboBox deviceDropDown;

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

			this.unlockButton = new Gtk.Button () { Label = "Unlock" };
			this.screenshotButton = new Gtk.Button () { Label = "Screenshot" };
			toolbar.Add (this.unlockButton);
			toolbar.Add (this.screenshotButton);

			this.screenshot = new Xwt.ImageView ();

			var scrollView = new Xwt.ScrollView ();
			scrollView.Content = screenshot;
			this.Add (scrollView.ToGtkWidget ());

			this.unlockButton.Clicked += (sender, e) => {
				this.UnlockDevice ();
			};

			this.screenshotButton.Clicked += (sender, e) => {
				this.TakeScreenshot ();
			};

			toolbar.ShowAll ();
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

					var i = Xwt.Drawing.Image.FromStream (ms);

					this.screenshot.Image = i.Scale (0.5);
				}

				this.screenshotButton.Sensitive = true;
			}
		}
	}
}
