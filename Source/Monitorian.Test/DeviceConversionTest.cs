using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static Monitorian.Core.Models.Monitor.DeviceConversion;

namespace Monitorian.Test
{
	[TestClass]
	public class DeviceConversionTest
	{
		#region ConvertToDeviceInstanceId

		[TestMethod]
		public void TestConvertToDeviceInstanceId1()
		{
			// Surface Display
			var source = @"\\?\DISPLAY#SDC3853#4&3b1d693f&0&UID265988#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}";
			var expected = @"DISPLAY\SDC3853\4&3b1d693f&0&UID265988";

			Assert.AreEqual(expected, ConvertToDeviceInstanceId(source));
		}

		[TestMethod]
		public void TestConvertToDeviceInstanceId2()
		{
			// Dell U2415
			var source = @"\\?\DISPLAY#DELA0B8#4&3b1d693f&0&UID224795#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}";
			var expected = @"DISPLAY\DELA0B8\4&3b1d693f&0&UID224795";

			Assert.AreEqual(expected, ConvertToDeviceInstanceId(source));
		}

		#endregion

		#region TryParseToDeviceInstanceId

		[TestMethod]
		public void TestTryParseToDeviceInstanceId1()
		{
			// Surface Display
			var source = @"DISPLAY\\LGD06B1\\4&8bc03bf&0&UID8388688"; // Escaped backslashes
			var expected = @"DISPLAY\LGD06B1\4&8bc03bf&0&UID8388688";

			var result = TryParseToDeviceInstanceId(source, out string actual);
			Assert.IsTrue(result);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestTryParseToDeviceInstanceId2()
		{
			// DELL S2721QS
			var source = @"DISPLAY\\DELA198\\4&8bc03bf&0&UID57670"; // Escaped backslashes
			var expected = @"DISPLAY\DELA198\4&8bc03bf&0&UID57670";

			var result = TryParseToDeviceInstanceId(source, out string actual);
			Assert.IsTrue(result);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestTryParseToDeviceInstanceId3()
		{
			var source = @"DISPLAY\LGD06B1";
			Assert.IsFalse(TryParseToDeviceInstanceId(source, out _));
		}

		[TestMethod]
		public void TestTryParseToDeviceInstanceId4()
		{
			var source = @"DISPLAY#LGD06B1#4&8bc03bf&0&UID8388688";
			Assert.IsFalse(TryParseToDeviceInstanceId(source, out _));
		}

		[TestMethod]
		public void TestTryParseToDeviceInstanceId5()
		{
			var source = @"\DISPLAY\LGD06B1\4&8bc03bf&0&UID8388688";
			Assert.IsFalse(TryParseToDeviceInstanceId(source, out _));
		}

		[TestMethod]
		public void TestTryParseToDeviceInstanceId6()
		{
			var source = @"USB\LGD06B1\4&8bc03bf&0&UID8388688";
			Assert.IsFalse(TryParseToDeviceInstanceId(source, out _));
		}

		#endregion
	}
}