// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AdbConsoleView.cs" company="(c) Greg Munn">
//   (c) 2015 (c) Greg Munn  All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using AdbSharp;
using MonoDevelop.Components;
using System.Threading.Tasks;
using System.Threading;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide;
using AdbSharpTools.CommandSupport;

namespace AdbSharpTools.Widgets
{
	public class AdbConsoleView : ConsoleView, ICompletionWidget
	{
		#pragma warning disable 0414 // it is used, XS just can't tell
		private bool busy;
		#pragma warning restore 0414
		private Gtk.TextMark tokenBeginMark;
		private CodeCompletionContext completionContext;
		private string[] currentTokens;

		private bool showingCompletionWindow;

		public AdbConsoleView ()
		{
			this.TextView.KeyReleaseEvent += this.OnEditKeyRelease;

			IdeApp.Preferences.CustomOutputPadFontChanged += OnCustomOutputPadFontChanged;

			CompletionWindowManager.WindowClosed -= this.OnCompletionWindowClosed;
			CompletionWindowManager.WindowClosed += this.OnCompletionWindowClosed;
		}

		public AndroidDeviceBridge Adb { get; set; }

		public IDevice Device { get; set; }

		public CodeCompletionContext CurrentCodeCompletionContext {
			get {
				return ((ICompletionWidget)this).CreateCodeCompletionContext (this.Position);
			}
		}

		public int CaretOffset {
			get {
				return this.Position;
			}
		}

		public int TextLength {
			get {
				return this.TokenText.Length;
			}
		}

		public int SelectedLength {
			get {
				return 0;
			}
		}

		public Gtk.Style GtkStyle {
			get {
				return this.Style;
			}
		}

		private int Position {
			get { 
				return this.Cursor.Offset - this.TokenBegin.Offset; 
			}
		}

		private Gtk.TextIter TokenBegin {
			get { 
				return this.Buffer.GetIterAtMark (this.tokenBeginMark); 
			}
		}

		private Gtk.TextIter TokenEnd {
			get { 
				return this.Cursor; 
			}
		}

		private string TokenText {
			get { 
				return this.Buffer.GetText (this.TokenBegin, TokenEnd, false); 
			}

			set {
				var start = this.TokenBegin;
				var end = this.TokenEnd;

				this.Buffer.Delete (ref start, ref end);
				start = this.TokenBegin;
				this.Buffer.Insert (ref start, value);
			}
		}

		#pragma warning disable 067 // it is used, part of the interface requirements
		public event EventHandler CompletionContextChanged; 
		#pragma warning restore 067

		public string GetText (int startOffset, int endOffset)
		{
			var text = this.TokenText;

			if (startOffset < 0 || startOffset > text.Length) startOffset = 0;
			if (endOffset > text.Length) endOffset = text.Length;

			return text.Substring (startOffset, endOffset - startOffset);
		}

		public char GetChar (int offset)
		{
			string text = this.TokenText;

			if (offset >= text.Length)
				return (char) 0;

			return text[offset];
		}

		public void Replace (int offset, int count, string text)
		{
			if (count > 0)
				this.TokenText = this.TokenText.Remove (offset, count);
			
			if (!string.IsNullOrEmpty (text))
				this.TokenText = this.TokenText.Insert (offset, text);
		}

		public CodeCompletionContext CreateCodeCompletionContext (int triggerOffset)
		{
			string expr = this.Buffer.GetText (TokenBegin, Cursor, false);

			var c = new CodeCompletionContext ();
			c.TriggerLine = 0;
			c.TriggerOffset = triggerOffset;
			c.TriggerLineOffset = c.TriggerOffset;
			c.TriggerWordLength = expr.Length;

			int height, lineY, x, y;
			TextView.GdkWindow.GetOrigin (out x, out y);
			TextView.GetLineYrange (Cursor, out lineY, out height);

			var rect = GetIterLocation (Cursor);

			c.TriggerYCoord = y + lineY + height - (int)Vadjustment.Value;
			c.TriggerXCoord = x + rect.X;
			c.TriggerTextHeight = height;

			return c;
		}

		public string GetCompletionText (CodeCompletionContext ctx)
		{
			return this.TokenText.Substring (ctx.TriggerOffset, ctx.TriggerWordLength);
		}

		public void SetCompletionText (CodeCompletionContext ctx, string partialWord, string completeWord)
		{
			int sp = this.Position - partialWord.Length;

			var start = Buffer.GetIterAtOffset (this.TokenBegin.Offset + sp);

			// FIXME: if we're replacing a token, this ends up inserting the token into the middle of a word rather than replacing it
			// we need to know if we're in the middle of a token and if so, find it's length and use that instead
			// this.Token only returns up to the cursor, which doesn't help
			var end = Buffer.GetIterAtOffset (start.Offset + partialWord.Length);

			Buffer.Delete (ref start, ref end);
			Buffer.Insert (ref start, completeWord);
			Buffer.PlaceCursor (start);
		}

		public void SetCompletionText (CodeCompletionContext ctx, string partialWord, string completeWord, int completeWordOffset)
		{
			int sp = this.Position - partialWord.Length;

			var start = this.Buffer.GetIterAtOffset (this.TokenBegin.Offset + sp);
			var end = this.Buffer.GetIterAtOffset (start.Offset + partialWord.Length);
			Buffer.Delete (ref start, ref end);
			Buffer.Insert (ref start, completeWord);

			var cursor = this.Buffer.GetIterAtOffset (start.Offset + completeWordOffset);
			this.Buffer.PlaceCursor (cursor);
		}

		protected async override void ProcessInput (string line)
		{
			this.WriteOutput ("\n");
			await this.ProcessCommand (line);
			this.Prompt (true, false);
		}

		protected override bool ProcessKeyPressEvent (Gtk.KeyPressEventArgs args)
		{
			if (showingCompletionWindow) {
				if ((CompletionWindowManager.PreProcessKeyEvent (args.Event.Key, (char) args.Event.Key, args.Event.State)))
					return true;
			}

			return base.ProcessKeyPressEvent (args);
		}

		protected override void UpdateInputLineBegin ()
		{
			if (this.tokenBeginMark == null)
				this.tokenBeginMark = Buffer.CreateMark (null, Buffer.EndIter, true);
			else
				Buffer.MoveMark (this.tokenBeginMark, Buffer.EndIter);
			
			base.UpdateInputLineBegin ();
		}

		private async Task ProcessCommand (string command)
		{
			this.busy = true;
			try {
				var adb = this.Adb;
				if (adb != null) {
					var result = await CommandProcessor.ProcessCommand (adb, this.Device, command, CancellationToken.None);
					this.WriteOutput (result);
				}
			} catch (Exception ex) {
				this.WriteOutput ("Error - " + ex.ToString ());
			} finally {
				this.busy = false;
			}
		}

		private void OnCustomOutputPadFontChanged (object sender, EventArgs e)
		{
			this.SetFont (IdeApp.Preferences.CustomOutputPadFont);
		}

		private void OnCompletionWindowClosed (object sender, EventArgs e)
		{
			showingCompletionWindow = false;
		}

		bool IsCompletionChar (char c)
		{
			return (char.IsLetterOrDigit (c) || char.IsPunctuation (c) || char.IsSymbol (c) || char.IsWhiteSpace (c));
		}

		private void PopupCompletion (uint keyValue)
		{
			Gtk.Application.Invoke (delegate {
				char c = (char) Gdk.Keyval.ToUnicode (keyValue);
				if (!showingCompletionWindow && IsCompletionChar (c)) {
					var dataList = new AdbCompletionDataList (this.currentTokens);
					completionContext = this.CreateCodeCompletionContext (0);

					showingCompletionWindow = true;

					CompletionWindowManager.ShowWindow (null, c, dataList, this, completionContext);
				}
			});
		}

		void OnEditKeyRelease (object sender, Gtk.KeyReleaseEventArgs args)
		{
			UpdateTokenBeginMarker ();

			var keyChar = (char) args.Event.Key;
			var keyValue = args.Event.KeyValue;
			var modifier = args.Event.State;
			var key = args.Event.Key;

			string text = TokenText;

			if (showingCompletionWindow) {
				// ?? what does this actually do?
				text = text.Substring (Math.Max (0, Math.Min (completionContext.TriggerOffset, text.Length)));
			}

			CompletionWindowManager.UpdateWordSelection (text);
			CompletionWindowManager.PostProcessKeyEvent (key, keyChar, modifier);

			PopupCompletion (keyValue);
		}

		private void UpdateTokenBeginMarker ()
		{
			var text = Buffer.GetText (InputLineBegin, Cursor, false);
			int index = 0;
			this.currentTokens = CommandProcessor.TokeniseCommand (text, out index);

			var iter = Buffer.GetIterAtOffset (InputLineBegin.Offset + index);
			Buffer.MoveMark (tokenBeginMark, iter);
		}
	}
}
