// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AdbCompletionDataList.cs" company="(c) Greg Munn">
//   (c) 2015 (c) Greg Munn  All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using MonoDevelop.Ide.CodeCompletion;
using System.Linq;

namespace AdbSharpTools.CommandSupport
{
	class AdbCompletionDataList: List<ICSharpCode.NRefactory.Completion.ICompletionData>, ICompletionDataList
	{
		private static readonly List<ICompletionKeyHandler> keyHandler = new List<ICompletionKeyHandler> ();

		public AdbCompletionDataList (string[] currentTokens)
		{
			this.SetCompletionItems (currentTokens);
			this.IsSorted = false;
			this.AutoSelect = true;
		}

		public bool IsSorted { get; set; }

		public bool AutoSelect { get; set; }

		public string DefaultCompletionString {
			get {
				return string.Empty;
			}
		}

		public bool AutoCompleteUniqueMatch {
			get { 
				return false; 
			}
		}

		public bool AutoCompleteEmptyMatch {
			get { 
				return false; 
			}
		}

		public bool AutoCompleteEmptyMatchOnCurlyBrace {
			get { 
				return false; 
			}
		}

		public bool CloseOnSquareBrackets {
			get {
				return false;
			}
		}

		public CompletionSelectionMode CompletionSelectionMode { get; set; }

		public IEnumerable<ICompletionKeyHandler> KeyHandler { get { return keyHandler;} }

		public event EventHandler CompletionListClosed;

		public void OnCompletionListClosed (EventArgs e)
		{
			var handler = CompletionListClosed;

			if (handler != null)
				handler (this, e);
		}

		private void SetCompletionItems (string[] currentTokens)
		{
			// mmm, do we really need to say shell?, why not just assume we're talking to the device?
			var firstToken = currentTokens.Take (1).FirstOrDefault ();

			switch (firstToken) {
			case "shell":
				// http://developer.android.com/tools/help/adb.html#shellcommands
				this.Add (new AdbCompletionData ("am")); //adb shell am start -a android.intent.action.VIEW, 
				this.Add (new AdbCompletionData ("pm"));
				this.Add (new AdbCompletionData ("ls"));
				this.Add (new AdbCompletionData ("start"));
				this.Add (new AdbCompletionData ("stop"));

				this.Add (new AdbCompletionData ("getprop")); 
				this.Add (new AdbCompletionData ("setprop")); 
				this.Add (new AdbCompletionData ("reboot")); 
				break;
			default:
				this.Add (new AdbCompletionData ("shell")); 
				this.Add (new AdbCompletionData ("devices")); 
				this.Add (new AdbCompletionData ("version")); 
				this.Add (new AdbCompletionData ("install")); 
				this.Add (new AdbCompletionData ("uninstall")); 
				break;
			}
		}
	}
}
