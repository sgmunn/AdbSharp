// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AddinInitialisation.cs" company="(c) Greg Munn">
//   (c) 2015 (c) Greg Munn  All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using AdbSharp.Utils;

namespace AdbSharpTools
{
	sealed class AddinInitialisation : MonoDevelop.Components.Commands.CommandHandler
	{
		public AddinInitialisation ()
		{
		}

		protected override void Run ()
		{
			Logging.RegisterLog (MonoDevelopLoggingService.Instance);
			Logging.LogInfo ("AdbSharp Tools Initialized");
		}
	}
}