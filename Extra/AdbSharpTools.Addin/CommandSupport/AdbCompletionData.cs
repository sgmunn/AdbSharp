// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AdbCompletionData.cs" company="(c) Greg Munn">
//   (c) 2015 (c) Greg Munn  All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using MonoDevelop.Ide.CodeCompletion;

namespace AdbSharpTools.CommandSupport
{
	class AdbCompletionData : CompletionData
	{
		private readonly string command;
		private readonly string displayValue;

		public AdbCompletionData (string command)
		{
			this.command = command;
			this.displayValue = command;
		}

		public AdbCompletionData (string command, string displayValue)
		{
			this.command = command;
			this.displayValue = displayValue;
		}

		public override string DisplayText {
			get {
				return this.displayValue;
			}
		}

		public override string CompletionText {
			get {
				return this.command;
			}
		}

		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
		{
			base.InsertCompletionText (window, ref ka, closeChar, keyChar, modifier);
		}
	}
}
