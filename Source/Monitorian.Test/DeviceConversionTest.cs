using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Monitorian.Core.Models.Monitor;

namespace Monitorian.Test
{
	[TestClass]
	public class DeviceConversionTest
	{
		[TestMethod]
		public void TestGetDeviceInstanceId1()
		{
			// Surface Display
			var source = @"\\?\DISPLAY#SDC3853#4&3b1d693f&0&UID265988#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}";
			var expected = @"DISPLAY\SDC3853\4&3b1d693f&0&UID265988";

			Assert.AreEqual(expected, TestConvertDeviceInstanceIdBase(source));
		}

		[TestMethod]
		public void TestGetDeviceInstanceId2()
		{
			// Dell U2415
			var source = @"\\?\DISPLAY#DELA0B8#4&3b1d693f&0&UID224795#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}";
			var expected = @"DISPLAY\DELA0B8\4&3b1d693f&0&UID224795";

			Assert.AreEqual(expected, TestConvertDeviceInstanceIdBase(source));
		}

		private string TestConvertDeviceInstanceIdBase(string source)
		{
			var @class = new PrivateType(typeof(DeviceConversion));
			return (string)@class.InvokeStatic(nameof(DeviceConversion.ConvertToDeviceInstanceId), source);
		}
	}
}