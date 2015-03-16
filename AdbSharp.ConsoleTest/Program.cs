using System;
using AdbSharp.Adb;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AdbSharp.ConsoleTest
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var config = new AdbConfig ("/Users/sgm/Library/Developer/Xamarin/android-sdk-macosx/platform-tools/adb");

			AndroidDeviceBridge.DefaultConfig = config;
			Console.WriteLine ("Hello World!");


			IDisposable monitor = null;

			var adb = AndroidDeviceBridge.Create ();
			Console.WriteLine (adb.GetServerVersionAsync ().Result);
//
//			var devices = adb.GetDevicesAsync ().Result;
//			foreach (var d in devices) {
//				Console.WriteLine (d.DeviceId);
//			};
//
//			Console.WriteLine ("Start tracking");
//
			monitor = adb.TrackDevices ((l) => {
				foreach (var d in l) {
					Console.WriteLine ("{0} - {1}", d.DeviceId, d.State);
				}
			}, (ex) => {
				Console.WriteLine ("monitor stopped - {0}", ex);

			});

//			adb.UnlockAsync ().Wait ();

			var sw = new Stopwatch ();
			sw.Start ();
			var fm = adb.GetFramebufferAsync ().Result;
			sw.Stop ();
			Console.WriteLine ("got framebuffer {0}", sw.ElapsedMilliseconds);
			sw.Reset ();
			sw.Start ();
			var img = fm.ToImage ();
			img.Save ("/Users/sgm/Desktop/Image1.png");
			sw.Stop ();
			Console.WriteLine ("got image 1" +
				" {0}", sw.ElapsedMilliseconds);
			sw.Reset ();

			Console.WriteLine ("Done");
			Console.ReadLine ();

//			monitor.Dispose ();

			Console.WriteLine ("Close");
			Console.ReadLine ();
		}
	}
}
