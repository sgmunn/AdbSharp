// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AdbConsoleViewWidget.cs" company="(c) Greg Munn">
//   (c) 2015 (c) Greg Munn  All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Docking;
using MonoDevelop.Components;

namespace AdbSharpTools.Widgets
{
	internal class AdbConsoleViewWidget : Gtk.VBox, IDeviceMonitorTarget
	{
		private DeviceMonitorWidget deviceMonitor;
		private DockToolButton connectButton;
		private Xwt.ComboBox deviceDropDown;
		private AdbConsoleView consoleView;

		public AdbConsoleViewWidget (IPadWindow container) 
		{
			this.Build (container);
			this.ShowAll ();
		}

		public Xwt.ComboBox DeviceDropDown {
			get {
				return this.deviceDropDown;
			}
		}

		public void SetButtonStates (DeviceMonitorWidget monitor)
		{
			this.consoleView.Sensitive = monitor.Connected;
			this.consoleView.Adb = monitor.Adb;
			this.consoleView.Device = monitor.CurrentDevice;
		}

		protected override void OnDestroyed ()
		{
			this.deviceMonitor.Dispose ();
			base.OnDestroyed ();
		}

		private void Build (IPadWindow container)
		{
			var toolbar = container.GetToolbar (Gtk.PositionType.Top);

			deviceDropDown = new Xwt.ComboBox ();
			deviceDropDown.WidthRequest =  160;

			this.connectButton = new DockToolButton (null, "C") { TooltipText = "Connect to ADB" };

			toolbar.Add (this.connectButton);
			toolbar.Add (deviceDropDown.ToGtkWidget (), false);

			this.connectButton.Clicked += (sender, e) => this.ConnectDisconnect ();

			toolbar.ShowAll ();

			this.consoleView = new AdbConsoleView ();
			this.PackStart (this.consoleView, true, true, 0);

			this.deviceMonitor = new DeviceMonitorWidget ("ConsolePad", this);
		}

		private void ConnectDisconnect ()
		{
			if (this.deviceMonitor.Connected) {
				this.Disconnect ();
			} else {
				this.Connect ();
			}
		}

		private void Connect ()
		{
			this.Disconnect ();
			this.deviceMonitor.Connect ();

			this.connectButton.TooltipText = "Disconnect from ADB";
			this.connectButton.Label = "D";
		}

		private void Disconnect ()
		{
			this.deviceMonitor.Disconnect ();

			this.connectButton.TooltipText = "Connect to ADB";
			this.connectButton.Label = "C";
		}
	}
}