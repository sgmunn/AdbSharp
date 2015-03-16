//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="DevicesWidget.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

using System;
using Gtk;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Docking;
using MonoDevelop.Components;
using System.Collections.Generic;
using AdbSharp;
using AdbSharp.Adb;
using Xwt;
using System.IO;
using System.Drawing.Imaging;
using System.Linq;

namespace AdbSharpAddin
{
	internal class DevicesWidget : Gtk.VBox, DropDownBoxListWindow.IListDataProvider
	{
		private IDisposable deviceMonitor;
		private AndroidDeviceBridge adb;
		private IList<IDevice> devices;

		private IDevice currentDevice;
		private Gtk.Button unlockButton;
		private Gtk.Button screenshotButton;
		private Xwt.ImageView screenshot;
		private DropDownBox deviceDropDown;

		public DevicesWidget (IPadWindow container) 
		{
			this.Setup ();
			this.Build (container);
			this.ShowAll ();
		}

		public void Reset ()
		{
			deviceDropDown.SetItem (0);
		}

		public string GetMarkup (int n)
		{
			return n == 0 ? "Select Device" : this.devices [n - 1].DeviceId;
		}

		public Xwt.Drawing.Image GetIcon (int n)
		{
			return null;
		}

		public object GetTag (int n)
		{
			return n == 0 ? "Select Device" : this.devices [n - 1].DeviceId;

		}

		public void ActivateItem (int n)
		{
			this.currentDevice = n == 0 ? null : this.devices [n - 1];
			this.unlockButton.Sensitive = n != 0;
			this.screenshotButton.Sensitive = n != 0;
			this.screenshot.Image = null;
		}

		public int IconCount {
			get {
				return this.devices != null ? this.devices.Count + 1: 1;
			}
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

		private void DevicesChanged (IList<IDevice> newDeviceList)
		{
			lock (this.adb) {
				this.devices = newDeviceList;
				if (this.currentDevice != null) {
					if (newDeviceList.All (d => d.DeviceId != this.currentDevice.DeviceId)) {
						this.currentDevice = null;
						this.deviceDropDown.SetItem (0);
						this.ActivateItem (0);
					}
				}
			}
		}

		private void MonitorStopped (Exception ex)
		{
		}

		private void Build (IPadWindow container)
		{
			var toolbar = container.GetToolbar (PositionType.Top);

			deviceDropDown = new DropDownBox ();
			deviceDropDown.DrawButtonShape = false;
			deviceDropDown.SetSizeRequest (160, 20);
			deviceDropDown.DataProvider = this;


			var filterVBox = new Gtk.VBox ();
			filterVBox.PackStart (deviceDropDown, true, false, 0); 
			toolbar.Add (filterVBox, false);


			this.unlockButton = new Gtk.Button () { Label = "Unlock" };
			this.screenshotButton = new Gtk.Button () { Label = "Screenshot" };
			toolbar.Add (this.unlockButton);
			toolbar.Add (this.screenshotButton);

			this.screenshot = new Xwt.ImageView ();

			var scrollView = new ScrollView ();
			scrollView.Content = screenshot;
			this.Add (scrollView.ToGtkWidget ());

			this.unlockButton.Clicked += (sender, e) => {
				this.UnlockDevice ();
			};

			this.screenshotButton.Clicked += (sender, e) => {
				this.TakeScreenshot ();
			};

			toolbar.ShowAll ();
			this.deviceDropDown.SetItem (0);
			this.ActivateItem (0);
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
