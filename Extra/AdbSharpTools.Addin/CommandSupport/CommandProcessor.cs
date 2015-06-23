// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandProcessor.cs" company="(c) Greg Munn">
//   (c) 2015 (c) Greg Munn  All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Threading.Tasks;
using System.Threading;
using AdbSharp;
using System.Collections.Generic;
using System.Text;

namespace AdbSharpTools.CommandSupport
{
	/// <summary>
	/// Provides support for tokenising and processing commands
	/// </summary>
	public static class CommandProcessor
	{
		public static Task<string> ProcessCommand (AndroidDeviceBridge adb, IDevice device, string command, CancellationToken token)
		{
			var commandParts = TokeniseCommand (command);
			// based on the initial token we will either route the command to the bridge or to the device, throw if device is null
			if (commandParts.Length < 1) {
				return Task.FromResult<string> (null);
			}

			switch (commandParts [0]) {
			case "shell": 
				if (device == null) {
					return Task.FromResult ("! No device connected");
				}

				return ProcessShellCommand (adb, device, commandParts, token);

			case "version":
				return adb.GetServerVersionAsync (token);
			case "devices":
				return GetDeviceListAsync (adb, token);
			default:
				return Task.FromResult ("! Unrecognised command");
			}
		}

		public static Task<string> ProcessShellCommand (AndroidDeviceBridge adb, IDevice device, string[] commandParts, CancellationToken token)
		{
			if (commandParts.Length < 2) {
				return Task.FromResult<string> (null);
			}

			switch (commandParts [1]) {
			case "getprop": 

				return GetPropertiesAsync (adb, device, "", token);
			default:
				return Task.FromResult ("! Unrecognised command");
			}
		}

		public async static Task<string> GetPropertiesAsync (AndroidDeviceBridge adb, IDevice device, string propertyName, CancellationToken token)
		{
			var sb = new StringBuilder ();
			var devices = await device.GetPropertyAsync (propertyName, token);

			return devices;
		}

		public async static Task<string> GetDeviceListAsync (AndroidDeviceBridge adb, CancellationToken token)
		{
			var sb = new StringBuilder ();
			var devices = await adb.GetDevicesAsync (token);
			if (devices.Count < 1) {
				return "No devices found";
			}

			var first = true;
			foreach (var device in devices) {
				if (!first) {
					sb.AppendLine ();
				}

				sb.Append (device.ToString ());
				first = false;
			}

			return sb.ToString ();
		}

		/// <summary>
		/// Takes the command and parses it into tokens
		/// </summary>
		public static string[] TokeniseCommand (string command)
		{
			int lastTokenIndex;
			return TokeniseCommand (command, out lastTokenIndex);
		}

		/// <summary>
		/// Takes the command and parses it into tokens and returns the index of he beginning of the last token
		/// </summary>
		public static string[] TokeniseCommand (string command, out int lastTokenIndex)
		{
			lastTokenIndex = 0;
			var result =  Parse (command, out lastTokenIndex);

			if (!string.IsNullOrEmpty (command)) {
				if (command.EndsWith (" ", StringComparison.InvariantCulture)) {
					// if we end with a space, we want the last token index to be on the space
					lastTokenIndex = command.Length;
				}
			}

			return result;
		}

		private static string GetArgument (StringBuilder builder, string buf, int startIndex, out int endIndex, out Exception ex)
		{
			bool escaped = false;
			char qchar, c = '\0';
			int i = startIndex;

			builder.Clear ();
			switch (buf[startIndex]) {
			case '\'': qchar = '\''; i++; break;
			case '"': qchar = '"'; i++; break;
			default: qchar = '\0'; break;
			}

			while (i < buf.Length) {
				c = buf[i];

				if (c == qchar && !escaped) {
					// unescaped qchar means we've reached the end of the argument
					i++;
					break;
				}

				if (c == '\\') {
					escaped = true;
				} else if (escaped) {
					builder.Append (c);
					escaped = false;
				} else if (qchar == '\0' && (c == ' ' || c == '\t')) {
					break;
				} else if (qchar == '\0' && (c == '\'' || c == '"')) {
					string sofar = builder.ToString ();
					string embedded;

					if ((embedded = GetArgument (builder, buf, i, out endIndex, out ex)) == null)
						return null;

					i = endIndex;
					builder.Clear ();
					builder.Append (sofar);
					builder.Append (embedded);
					continue;
				} else {
					builder.Append (c);
				}

				i++;
			}

			if (escaped || (qchar != '\0' && c != qchar)) {
				ex = new FormatException (escaped ? "Incomplete escape sequence." : "No matching quote found.");
				endIndex = -1;
				return null;
			}

			endIndex = i;
			ex = null;

			return builder.ToString ();
		}

		private static bool TryParse (string commandline, out string[] argv, out Exception ex, out int lastIndex)
		{
			lastIndex = 0;

			StringBuilder builder = new StringBuilder ();
			List<string> args = new List<string> ();
			string argument;
			int i = 0, j;
			char c;

			while (i < commandline.Length) {
				c = commandline[i];
				if (c != ' ' && c != '\t') {
					if ((argument = GetArgument (builder, commandline, i, out j, out ex)) == null) {
						argv =  null;
						return false;
					}

					lastIndex = i;

					args.Add (argument);
					i = j;
				}

				i++;
			}

			argv = args.ToArray ();
			ex = null;

			return true;
		}

		private static bool TryParse (string commandline, out string[] argv, out int lastIndex)
		{
			Exception ex;

			return TryParse (commandline, out argv, out ex, out lastIndex);
		}

		private static string[] Parse (string commandline, out int lastIndex)
		{
			string[] argv;
			Exception ex;

			if (!TryParse (commandline, out argv, out ex, out lastIndex))
				throw ex;

			return argv;
		}

//		/// <summary>
//		/// Returns true if there are more characters to process and updates index to the first non-whitespace char
//		/// </summary>
//		private static bool EatWhitespace (string text, ref int index)
//		{
//			while (index < text.Length && char.IsWhiteSpace (text[index]))
//				index++;
//
//			return index < text.Length;
//		}
//
//		/// <summary>
//		/// Returns true if the index was moved past an idenitifer / command
//		/// </summary>
//		private static bool EatIdentifier (string text, ref int index)
//		{
//			int startIndex = index;
//
//			if (index >= text.Length)
//				return false;
//
////			while (index < text.Length && (char.IsLetterOrDigit (text[index]) || text[index] == '_'))
////				index++;
//
//			// eat everything that is not a space - TODO - quoted spaces !
//			while (index < text.Length && (!char.IsWhiteSpace (text[index])))
//				index++;
//
//			return index > startIndex;
//		}
//
//		private static void UpdateTokenBeginMarker (string command)
//		{
//			// TODO: the way we generate tokens needs work. this fails if the last char is a space and we never get any completions
//			var stack = new Stack<int> ();
//			int index = 0;
//
//			// remove leading whitespace
//			if (!EatWhitespace (command, ref index))
//				return;
//
//			// push the start of the tokens
//			stack.Push (index);
//
//			// we should be positioned at the start of a token
//
//
//			while (EatIdentifier (command, ref index) &&  EatWhitespace (command, ref index)) {
//				stack.Push (index);
//			}
//
//			index = stack.Peek ();
//
//			var iter = Buffer.GetIterAtOffset (InputLineBegin.Offset + index);
//			Buffer.MoveMark (tokenBeginMark, iter);
//		}
//
	}
}

