//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="DevicesWidget.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

using System;
using MonoDevelop.Core;
using Gtk;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Docking;
using System.Threading;
using MonoDevelop.Components;
using System.Collections.Generic;
using System.Globalization;
using MonoDevelop.Ide.TypeSystem;
using System.Text;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using AdbSharp;
using Mono.TextEditor.PopupWindow;
using MonoDevelop.Ide.Tasks;
using AdbSharp.Adb;
using Xwt;
using System.IO;
using System.Drawing.Imaging;

namespace AdbSharpAddin
{
	internal class DevicesWidget : Gtk.VBox, DropDownBoxListWindow.IListDataProvider
	{
		private IDisposable deviceMonitor;

		public DevicesWidget (IPadWindow container) 
		{
			this.Setup ();
			this.Build (container);
			this.ShowAll ();
		}

		AndroidDeviceBridge adb;

		public void Shutdown ()
		{

		}

		private void Setup ()
		{
			this.adb = AndroidDeviceBridge.Create ();
			this.deviceMonitor = this.adb.TrackDevices (this.DevicesChanged, this.MonitorStopped);

			//deviceMonitor
		}

		private IList<IDevice> devices;

		private void DevicesChanged (IList<IDevice> devices)
		{
			this.devices = devices;
			foreach (var d in devices) {
				Console.WriteLine ("{0} - {1}", d.DeviceId, d.State);
			}

		}

		private void MonitorStopped (Exception ex)
		{
			
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
			//deviceDropDown.SetItem (n);
		}

		public int IconCount {
			get {
				return this.devices != null ? this.devices.Count + 1: 1;
			}
		}

		private IDevice currentDevice;

		private Gtk.Button unlockButton;

		private Gtk.Button screenshotButton;

		private Xwt.ImageView screenshot;

		private MonoDevelop.Components.DropDownBox deviceDropDown;

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
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

		}

		private async void UnlockDevice ()
		{
			var device = this.currentDevice;
			if (device != null) {
				await device.UnlockAsync ();
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
					this.screenshot.Image = i;
				}

				this.screenshotButton.Sensitive = true;
			}
		}
	}
}
