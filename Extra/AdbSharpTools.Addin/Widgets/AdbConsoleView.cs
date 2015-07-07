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
using AdbSharp.Utils;

namespace AdbSharpTools.Widgets
{
	public class AdbConsoleView : ConsoleView, ICompletionWidget
	{
		private readonly Gtk.TextTag outputTag;
		private readonly Gtk.TextTag errorTag;

		// it is used, XS just can't tell
		#pragma warning disable 0414 
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

			outputTag = new Gtk.TextTag ("output");
			outputTag.ForegroundGdk = new Gdk.Color (100, 100, 100);

			errorTag = new Gtk.TextTag ("error");
			errorTag.ForegroundGdk = new Gdk.Color (100, 0, 0);

			this.Buffer.TagTable.Add (outputTag);
			this.Buffer.TagTable.Add (errorTag);
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

		// it is used, part of the interface requirements
		#pragma warning disable 067 
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

		private static bool IsCompletionChar (char c)
		{
			return (char.IsLetterOrDigit (c) || char.IsPunctuation (c) || char.IsSymbol (c) || char.IsWhiteSpace (c));
		}

		private async Task ProcessCommand (string command)
		{
			this.busy = true;
			try {
				var adb = this.Adb;
				if (adb != null) {
					var result = await CommandProcessor.ProcessCommand (adb, this.Device, command, CancellationToken.None);
					this.WriteOutputWithColor (result);
				}
			} catch (Exception ex) {
				Logging.LogError (ex);
				this.WriteOutputWithColor ("Error - " + ex.Message, errorTag);
			} finally {
				this.busy = false;
			}
		}

		private void WriteOutputWithColor (string line, Gtk.TextTag tag = null)
		{
			Gtk.TextIter endIter = this.Buffer.EndIter;

			this.Buffer.InsertWithTags (ref endIter, line, new [] { (tag ?? outputTag) });
			this.Buffer.PlaceCursor (this.Buffer.EndIter);
			this.TextView.ScrollMarkOnscreen (this.Buffer.InsertMark);
		}

		private void OnCustomOutputPadFontChanged (object sender, EventArgs e)
		{
			this.SetFont (IdeApp.Preferences.CustomOutputPadFont);
		}

		private void OnCompletionWindowClosed (object sender, EventArgs e)
		{
			showingCompletionWindow = false;
		}

		private async void PopupCompletion (uint keyValue)
		{
			AdbCompletionDataList dataList = null;
			char c = (char) Gdk.Keyval.ToUnicode (keyValue);
			if (!showingCompletionWindow && IsCompletionChar (c)) {
				dataList = new AdbCompletionDataList (this.currentTokens, this.Device);
				await dataList.GetCompletionItemsAsync ();
			}

			if (dataList != null) {
				Gtk.Application.Invoke (delegate {
					completionContext = this.CreateCodeCompletionContext (0);
					showingCompletionWindow = true;
					CompletionWindowManager.ShowWindow (null, c, dataList, this, completionContext);
				});
			}
		}

		private void OnEditKeyRelease (object sender, Gtk.KeyReleaseEventArgs args)
		{
			UpdateTokenBeginMarker ();

			var keyChar = (char) args.Event.Key;
			var keyValue = args.Event.KeyValue;
			var modifier = args.Event.State;
			var key = args.Event.Key;

			// allow the user to navigate up and down the list
			if (showingCompletionWindow && (key == Gdk.Key.Down || key == Gdk.Key.Up)) {
				return;
			}

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
