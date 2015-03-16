//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="AdbException.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

using System;

namespace AdbSharp
{
	public class AdbException : Exception
	{
		public AdbException (string message) : base (message)
		{
		}	
	}
}