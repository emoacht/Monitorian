using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Monitorian.Core.Models.Monitor;

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
			// Dell E1715S
			var source = @"(prot(monitor)type(LCD)model(E1715S)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 06 08 10 12 14(05 08 0B 0C) 16 18 1A 52 60(01 0F) AA AC AE B2 B6 C6 C8 C9 D6(01 04 05) DC(00 02 03 05) DF E0 E1 E2(00 01 02 04 06 0E 12 14) F0(00 01) F1(01) F2(00 01 02) FD)mswhql(1)asset_eep(40)mccs_ver(2.1))";
			Assert.IsTrue(TestIsLowLevelSupportedBase(source));
		}

		[TestMethod]
		public void TestIsLowLevelSupported3()
		{
			// Dell 2717H
			var source = @"(prot(monitor)type(LCD)model(SE2717H)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 08 10 12 14(04 05 08 0B 0C) 16 18 1A 52 60(01 11 ) AC AE B2 B6 C6 C8 C9 CC(02 0A 03 04 08 09 0D 06 ) D6(01 04 05) DC(00 02 03 05 ) DF E0 E1 E2(00 1D 01 02 22 20 21 0E 12 14) E3 F0(0C 0F 10 11 ) F1 F2 FD)mswhql(1)asset_eep(40)mccs_ver(2.1))";
			Assert.IsTrue(TestIsLowLevelSupportedBase(source));
		}

		[TestMethod]
		public void TestIsLowLevelSupported4()
		{
			// Dell U2720QM
			var source = @"(prot(monitor)type(lcd)model(U2720QM)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 08 10 12 14(01 04 05 06 08 09 0B 0C) 16 18 1A 52 60(11 1B 0F) AA(01 02 03 04) AC AE B2 B6 C6 C8 C9 CC(02 03 04 06 09 0A 0D 0E) D6(01 04 05) DC(00 03 05) DF E0 E1 E2(00 02 04 0B 0C 0D 0F 10 11 13 14 1B 1D 23 24 27 3A) EA F0(00 05 06 0A 0C 31 32 34 36) F1 F2 FD)mccs_ver(2.1)mswhql(1))";
			Assert.IsTrue(TestIsLowLevelSupportedBase(source));
		}

		[TestMethod]
		public void TestIsLowLevelSupported5()
		{
			// Dell S2721QS
			var source = @"(prot(monitor)type(lcd)model(S2721QS)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 08 10 12 14(05 08 0B 0C) 16 18 1A 52 60( 0F 11 12) 62 AC AE B2 B6 C6 C8 C9 CC(02 03 04 06 09 0A 0D 0E) D6(01 04 05) DC(00 03 ) DF E0 E1 E2(00 1D 02 20 21 22 0E 12 14 23 24 27 )E3 E5 E8 E9(00 01 02 21 22 24) EA F0(00 0C 0F 10 11 31 32 34 35) F1 F2 FD)mccs_ver(2.1)mswhql(1))";
			Assert.IsTrue(TestIsLowLevelSupportedBase(source));
			Assert.IsTrue(TestMakeCapabilitiesReportBase(source));
		}

		[TestMethod]
		public void TestIsLowLevelSupported6()
		{
			// LG MP57
			var source = @"(prot(monitor)type(lcd)model(MP57)cmds(01 02 03 0C E3 F3)vcp(02030405080B0C101214(01 05 06 07 08 0B) 15(10 11 20 30 40 0B)16181A5260(01 03 04)6C6E7087ACAEB6C0C6C8C9D6(01 04)DFE0E1E3(00 01 02 03 04 10 11 12 13 14)ECEFFD(00 01)FE(00 01 02)FF)mswhql(1)mccs_ver(2.1))";
			Assert.IsTrue(TestIsLowLevelSupportedBase(source));
		}

		[TestMethod]
		public void TestIsLowLevelSupported7()
		{
			// LG 27UL550-W
			var source = @"(prot(monitor)type(lcd)UL550_500cmds(01 02 03 0C E3 F3)vcp(02 04 05 08 10 12 14(05 08 0B) 16 18 1A 52 60(11 12 0F 10) AC AE B2 B6 C0 C6 C8 C9 D6(01 04) DF 62 8D F4 F5(00 01 02) F6(00 01 02) 4D 4E 4F 15(01 06 09 10 11 13 14 28 29 32  44 48) F7(00 01 02 03) F8(00 01) F9 EF FD(00 01) FE(00 01 02) FF)mccs_ver(2.1)mswhql(1))";
			Assert.IsTrue(TestIsLowLevelSupportedBase(source));
			Assert.IsTrue(TestMakeCapabilitiesReportBase(source));
		}

		[TestMethod]
		public void TestIsLowLevelSupported8()
		{
			// HP LE1711
			var source = @"(prot(monitor)type(lcd)model(HP LE1711)cmds(01 02 03 07 0C 4E F3 E3)vcp(02 04 05 06 08 0B 0C 0E 10 12 14(01 05 08 0B) 16 18 1A 1E 1F 20 30 3E 52 60(01) 6C 6E 70 AC AE B6 C0 C6 C8 C9 CA CC(01 02 03 04 05 06 08 0A 0D 14) D6(01 04 05) DF FA(00 01 02) FB FC FD FE(00 01 02 04) )mswhql(1)mccs_ver(2.1)asset_eep(32)mpu_ver(01))";
			Assert.IsTrue(TestIsLowLevelSupportedBase(source));
		}

		[TestMethod]
		public void TestIsLowLevelSupported9()
		{
			// NEC L220W
			var source = @"(vcp(02 04 05 06 08 0E 10 12 14(01 02 06 08 0B 0E) 16 18 1A 1E 20 30 3E 68(01 02 03 04 05 06 07 09 0D) B0 B6 DF E3 F4 F5(01 02 03 04 05 06 07 09 0D) F9 FA FC FF)vcp_p02(33 37 47 52 64 65 DA EA FF)vcp_p10(10 11 26 27 28 29 2A 2B 2C 2D)prot(monitor)type(LCD)cmds(01 02 03 07 0C C2 C4 C6 C8 F3)mccs_ver(2.0)asset_eep(20)mpu_ver(1.02)model(L220W)mswhql(1))";
			Assert.IsTrue(TestIsLowLevelSupportedBase(source));
		}

		private static bool TestIsLowLevelSupportedBase(string source)
		{
			var @class = new PrivateType(typeof(MonitorConfiguration));
			return (bool)@class.InvokeStatic("IsLowLevelSupported", source);
		}

		private static bool TestMakeCapabilitiesReportBase(string source)
		{
			var @class = new PrivateType(typeof(MonitorConfiguration));
			var report = (string)@class.InvokeStatic("MakeCapabilitiesReport", source);

			bool IsSupported(string name)
			{
				var index = report.IndexOf(name);
				if (index < 0)
					return false;

				var buffer = report.Substring(index + name.Length)
					.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
					.First();

				return bool.TryParse(buffer, out bool value) && value;
			}

			return IsSupported("Contrast:")
				&& IsSupported("Speaker Volume:");
		}
	}
}