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
		string command;

		public AdbCompletionData (string command)
		{
			this.command = command;
		}

		public override string DisplayText {
			get {
				return command;
			}
		}

		public override string CompletionText {
			get {
				return command;
			}
		}

		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
		{
			base.InsertCompletionText (window, ref ka, closeChar, keyChar, modifier);
		}
	}
}
