// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConsolePad.cs" company="(c) Greg Munn">
//   (c) 2015 (c) Greg Munn  All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using Gtk;
using MonoDevelop.Ide.Gui;
using AdbSharpTools.Widgets;

namespace AdbSharpTools
{
	internal class ConsolePad : AbstractPadContent
	{
		private ConsoleWidget widget;

		public override void Initialize (IPadWindow container)
		{
			base.Initialize (container);
			this.widget = new ConsoleWidget (container);
		}

		public override Widget Control {
			get { 
				return this.widget; 
			}
		}
	}
}