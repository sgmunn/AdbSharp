// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AdbCompletionDataList.cs" company="(c) Greg Munn">
//   (c) 2015 (c) Greg Munn  All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using MonoDevelop.Ide.CodeCompletion;
using System.Linq;
using AdbSharp;
using System.Threading.Tasks;
using AdbSharp.Adb;

namespace AdbSharpTools.CommandSupport
{
	class AdbCompletionDataList: List<ICSharpCode.NRefactory.Completion.ICompletionData>, ICompletionDataList
	{
		private static readonly List<ICompletionKeyHandler> keyHandler = new List<ICompletionKeyHandler> ();
		private readonly string[] currentTokens;
		private readonly IDevice device;

		public AdbCompletionDataList (string[] currentTokens, IDevice device)
		{
			this.IsSorted = false;
			this.AutoSelect = true;
			this.currentTokens = currentTokens;
			this.device = device;
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

		public async Task GetCompletionItemsAsync ()
		{
			var firstToken = this.currentTokens.Take (1).FirstOrDefault ();

			if (firstToken == "-s") {
				// we're targetting a device
				var secondToken = this.currentTokens.Skip (1).Take (1).FirstOrDefault ();

				if (string.IsNullOrEmpty (secondToken)) {
					// no device
					this.Add (new AdbCompletionData ("device1"));
					this.Add (new AdbCompletionData ("device2"));

				} else {
					// we have a device choosen, assume shell
					//this.hasDeviceContext = true;
				}

				//				switch (secondToken) {
				//				case "shell":
				//					// http://developer.android.com/tools/help/adb.html#shellcommands
				//					this.Add (new AdbCompletionData ("am")); //adb shell am start -a android.intent.action.VIEW, 
				//					this.Add (new AdbCompletionData ("pm"));
				//					this.Add (new AdbCompletionData ("ls"));
				//					this.Add (new AdbCompletionData ("start"));
				//					this.Add (new AdbCompletionData ("stop"));
				//
				//					this.Add (new AdbCompletionData ("getprop")); 
				//					this.Add (new AdbCompletionData ("setprop")); 
				//					this.Add (new AdbCompletionData ("reboot")); 
				//					break;
				//				default:
				//					this.Add (new AdbCompletionData ("shell")); 
				//					this.Add (new AdbCompletionData ("devices")); 
				//					this.Add (new AdbCompletionData ("version")); 
				//					this.Add (new AdbCompletionData ("install")); 
				//					this.Add (new AdbCompletionData ("uninstall")); 
				//					break;
				//				}

			}

			if (this.device != null) {
				// asumme we want to send a shell command
				switch (firstToken) {
				case "getprop":
					await this.GetPropertiesCompletionListAsync ();
					break;





				//				case "shell":
				//					// http://developer.android.com/tools/help/adb.html#shellcommands
				//					this.Add (new AdbCompletionData ("am")); //adb shell am start -a android.intent.action.VIEW, 
				//					this.Add (new AdbCompletionData ("pm"));
				//					this.Add (new AdbCompletionData ("ls"));
				//					this.Add (new AdbCompletionData ("start"));
				//					this.Add (new AdbCompletionData ("stop"));
				//
				//					this.Add (new AdbCompletionData ("getprop")); 
				//					this.Add (new AdbCompletionData ("setprop")); 
				//					this.Add (new AdbCompletionData ("reboot")); 
				//					break;
				default:
					// shell commands
					this.Add (new AdbCompletionData ("am")); //adb shell am start -a android.intent.action.VIEW, 
					this.Add (new AdbCompletionData ("pm"));
					this.Add (new AdbCompletionData ("ls"));
					this.Add (new AdbCompletionData ("start"));
					this.Add (new AdbCompletionData ("stop"));

					this.Add (new AdbCompletionData ("getprop")); 
					this.Add (new AdbCompletionData ("setprop")); 
					this.Add (new AdbCompletionData ("reboot")); 
					//					this.Add (new AdbCompletionData ("shell")); 
					//					this.Add (new AdbCompletionData ("devices")); 
					//					this.Add (new AdbCompletionData ("version")); 
					//					this.Add (new AdbCompletionData ("install")); 
					//					this.Add (new AdbCompletionData ("uninstall")); 
					break;
				}
			} else {
				// we're not directly addressing a device so assume host commands
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

		public void OnCompletionListClosed (EventArgs e)
		{
			var handler = CompletionListClosed;

			if (handler != null)
				handler (this, e);
		}

		private async Task GetPropertiesCompletionListAsync ()
		{
			var properties = await device.GetPropertyAsync (null, new System.Threading.CancellationToken ());
			foreach (var property in DeviceProperties.ParseProperties (properties)) {
				this.Add (new AdbCompletionData (property.Name, property.Name + " = " + (property.Value ?? "NULL")));
			}
		}
	}
}
