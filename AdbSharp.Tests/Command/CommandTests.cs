//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Test.cs" company="(c) Greg Munn">
//    (c) 2014 (c) Greg Munn  All Rights Reserved
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------
using NUnit.Framework;
using System;

namespace AdbSharp.Tests.Command
{
	[TestFixture ()]
	public class WhenCreatingCommandBuffer
	{
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void NullCommandThrowsArgumentException ()
		{
			AdbSharp.Commands.GetCommand (null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void EmptyCommandThrowsArgumentException ()
		{
			AdbSharp.Commands.GetCommand (string.Empty);
		}

		[Test]
		public void ValidCommandReturnsCorrectByteArray ()
		{
			const string cmd = "qwertyuiopasdfghjklzxcvbnm";
			var bytes = AdbSharp.Commands.GetCommand (cmd);

			// length
			Assert.AreEqual (bytes.Length, 30);
			// command text size, as text
			Assert.AreEqual (bytes [0], 48);
			Assert.AreEqual (bytes [1], 48);
			Assert.AreEqual (bytes [2], 49);
			Assert.AreEqual (bytes [3], 65);

			// the command, as given
			for (int i = 0; i < 26; i++) {
				Assert.AreEqual (bytes [4 + i], cmd [i]);
			}
		}
	}
}

