//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="DevicesPad.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

using System;
using Gtk;
using MonoDevelop.Ide.Gui;

namespace AdbSharpTools
{
	internal class DevicesPad : AbstractPadContent
	{
		private DevicesWidget widget;

		public override void Initialize (IPadWindow container)
		{
			base.Initialize (container);
			this.widget = new DevicesWidget (container);
		}

		public override Widget Control {
			get { 
				return this.widget; 
			}
		}
	}
}