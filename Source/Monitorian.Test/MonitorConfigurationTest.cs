using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Monitorian.Models.Monitor;

namespace Monitorian.Test
{
	[TestClass]
	public class MonitorConfigurationTest
	{
		[TestMethod]
		public void TestIsLowLevelSupported1()
		{
			// Dell U2415
			var source = @"(prot(monitor)type(LCD)model(U2415)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 08 10 12 14(04 0B 05 06 08 09 0C) 16 18 1A 52 60(0F 10 11 12) AA(01 02 04) AC AE B2 B6 C6 C8 C9 D6(01 04 05) DC(00 02 03 05) DF E0 E1 E2(00 01 02 04 14 19 0C 0D 0F 10 11 13) F0(00 08) F1(01 02) F2 FD)mswhql(1)asset_eep(40)mccs_ver(2.1))";
			Assert.IsTrue(TestIsLowLevelSupportedBase(source));
		}

		[TestMethod]
		public void TestIsLowLevelSupported2()
		{
			// LG MP57
			var source = @"(prot(monitor)type(lcd)model(MP57)cmds(01 02 03 0C E3 F3)vcp(02030405080B0C101214(01 05 06 07 08 0B) 15(10 11 20 30 40 0B)16181A5260(01 03 04)6C6E7087ACAEB6C0C6C8C9D6(01 04)DFE0E1E3(00 01 02 03 04 10 11 12 13 14)ECEFFD(00 01)FE(00 01 02)FF)mswhql(1)mccs_ver(2.1))";
			Assert.IsTrue(TestIsLowLevelSupportedBase(source));
		}

		private bool TestIsLowLevelSupportedBase(string source)
		{
			var @class = new PrivateType(typeof(MonitorConfiguration));
			return (bool)@class.InvokeStatic("IsLowLevelSupported", source);
		}
	}
}