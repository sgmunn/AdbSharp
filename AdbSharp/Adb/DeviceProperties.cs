// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeviceProperties.cs" company="(c) Greg Munn">
//   (c) 2015 (c) Greg Munn  All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace AdbSharp.Adb
{
	public static class DeviceProperties
	{
		public static string[] ParsePropertyNames (string properties)
		{
			var result = new List<string> ();

			var props = properties.Split (new [] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var prop in props) {
				var first = prop.Split (new [] { "[" }, StringSplitOptions.RemoveEmptyEntries);
				if (first.Length > 0) {
					var second = first [0].Split (new [] { "]:" }, StringSplitOptions.RemoveEmptyEntries);
					if (second.Length > 0) {
						result.Add (second [0]);
					}
				}
			}

			return result.ToArray ();
		}

		public static DeviceProperty[] ParseProperties (string properties)
		{
			var result = new List<DeviceProperty> ();

			var props = properties.Split (new [] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var prop in props) {
				var first = prop.Split (new [] { "[" }, StringSplitOptions.RemoveEmptyEntries);
				if (first.Length > 0) {
					var second = first [0].Split (new [] { "]:" }, StringSplitOptions.RemoveEmptyEntries);
					if (second.Length > 0) {

						var third = first [1].Split (new [] { "]" }, StringSplitOptions.RemoveEmptyEntries);
						if (third.Length > 0) {
							result.Add (new DeviceProperty (second [0], third [0]));
						} else {
							result.Add (new DeviceProperty (second [0]));
						}
					}
				}
			}

			return result.ToArray ();
		}
	}

	public sealed class DeviceProperty
	{
		public DeviceProperty (string name)
		{
			this.Name = name;
		}

		public DeviceProperty (string name, string value)
		{
			this.Name = name;
			this.Value = value;
		}

		public string Name { get; private set; }

		public string Value { get; private set; }
	}
}