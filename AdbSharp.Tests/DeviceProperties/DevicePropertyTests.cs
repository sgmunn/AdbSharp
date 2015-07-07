//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Test.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------
using NUnit.Framework;
using System;

namespace AdbSharp.Tests.DeviceProperties
{
	[TestFixture ()]
	public class Parsing
	{
		[Test]
		public void ParsePropertyNamesTest ()
		{
			const string properties = "[ro.xapd.caps.scr]: [on]";
			var result = AdbSharp.Adb.DeviceProperties.ParsePropertyNames (properties);

			Assert.AreEqual (1, result.Length);
			Assert.AreEqual ("ro.xapd.caps.scr", result [0]);
		}

		[Test]
		public void ParsePropertiesTest ()
		{
			const string properties = "[ro.xapd.caps.scr]: [on]";
			var result = AdbSharp.Adb.DeviceProperties.ParseProperties (properties);

			Assert.AreEqual (1, result.Length);
			Assert.AreEqual ("ro.xapd.caps.scr", result [0].Name);
			Assert.AreEqual ("on", result [0].Value);
		}
	}
}

