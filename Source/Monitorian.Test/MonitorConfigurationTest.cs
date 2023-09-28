using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Monitorian.Core.Models.Monitor;

namespace Monitorian.Test;

[TestClass]
public class MonitorConfigurationTest
{
	[TestMethod]
	public void TestMonitorCapability_D1_1()
	{
		// Dell U2415
		var source = @"(prot(monitor)type(LCD)model(U2415)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 08 10 12 14(04 0B 05 06 08 09 0C) 16 18 1A 52 60(0F 10 11 12) AA(01 02 04) AC AE B2 B6 C6 C8 C9 D6(01 04 05) DC(00 02 03 05) DF E0 E1 E2(00 01 02 04 14 19 0C 0D 0F 10 11 13) F0(00 08) F1(01 02) F2 FD)mswhql(1)asset_eep(40)mccs_ver(2.1))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsFalse(AreIncluded(vcpCodes, VcpCode.SpeakerVolume));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 4, 11, 5, 6, 8, 9));
	}

	[TestMethod]
	public void TestMonitorCapability_D1_2()
	{
		// Dell E1715S
		var source = @"(prot(monitor)type(LCD)model(E1715S)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 06 08 10 12 14(05 08 0B 0C) 16 18 1A 52 60(01 0F) AA AC AE B2 B6 C6 C8 C9 D6(01 04 05) DC(00 02 03 05) DF E0 E1 E2(00 01 02 04 06 0E 12 14) F0(00 01) F1(01) F2(00 01 02) FD)mswhql(1)asset_eep(40)mccs_ver(2.1))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsFalse(AreIncluded(vcpCodes, VcpCode.SpeakerVolume));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 5, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_D1_3()
	{
		// Dell SE2717H
		var source = @"(prot(monitor)type(LCD)model(SE2717H)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 08 10 12 14(04 05 08 0B 0C) 16 18 1A 52 60(01 11 ) AC AE B2 B6 C6 C8 C9 CC(02 0A 03 04 08 09 0D 06 ) D6(01 04 05) DC(00 02 03 05 ) DF E0 E1 E2(00 1D 01 02 22 20 21 0E 12 14) E3 F0(0C 0F 10 11 ) F1 F2 FD)mswhql(1)asset_eep(40)mccs_ver(2.1))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsFalse(AreIncluded(vcpCodes, VcpCode.SpeakerVolume));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 4, 5, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_D1_4()
	{
		// Dell U2720QM
		var source = @"(prot(monitor)type(lcd)model(U2720QM)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 08 10 12 14(01 04 05 06 08 09 0B 0C) 16 18 1A 52 60(11 1B 0F) AA(01 02 03 04) AC AE B2 B6 C6 C8 C9 CC(02 03 04 06 09 0A 0D 0E) D6(01 04 05) DC(00 03 05) DF E0 E1 E2(00 02 04 0B 0C 0D 0F 10 11 13 14 1B 1D 23 24 27 3A) EA F0(00 05 06 0A 0C 31 32 34 36) F1 F2 FD)mccs_ver(2.1)mswhql(1))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsFalse(AreIncluded(vcpCodes, VcpCode.SpeakerVolume));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 4, 5, 6, 8, 9));
	}

	[TestMethod]
	public void TestMonitorCapability_D1_5()
	{
		// Dell S2721QS
		var source = @"(prot(monitor)type(lcd)model(S2721QS)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 08 10 12 14(05 08 0B 0C) 16 18 1A 52 60( 0F 11 12) 62 AC AE B2 B6 C6 C8 C9 CC(02 03 04 06 09 0A 0D 0E) D6(01 04 05) DC(00 03 ) DF E0 E1 E2(00 1D 02 20 21 22 0E 12 14 23 24 27 )E3 E5 E8 E9(00 01 02 21 22 24) EA F0(00 0C 0F 10 11 31 32 34 35) F1 F2 FD)mccs_ver(2.1)mswhql(1))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.SpeakerVolume)); // True
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 5, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_D1_6()
	{
		// DELL U2419H
		var source = @"(prot(monitor)type(LCD)model(U2419H)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 08 10 12 14(04 05 06 08 09 0B 0C) 16 18 1A 52 60(0F 11 ) AA(01 02 03 04 ) AC AE B2 B6 C6 C8 C9 CC(02 0A 03 04 08 09 0D 06 ) D6(01 04 05) DC(00 03 05 ) DF E0 E1 E2(00 1D 29 02 04 0C 0D 0F 10 11 13 14 ) E3(16 17 18 19 1A) F0(0C 12 ) F1 F2 FD)mswhql(1)asset_eep(40)mccs_ver(2.1))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsFalse(AreIncluded(vcpCodes, VcpCode.SpeakerVolume)); // True
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 4, 5, 6, 8, 9));
	}

	[TestMethod]
	public void TestMonitorCapability_D1_7()
	{
		// DELL P2719H
		var source = @"(prot(monitor)type(lcd)model(P2719H)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 08 10 12 14(05 08 0B 0C) 16 18 1A 52 60(01 0F 11) AA(01 02 04) AC AE B2 B6 C6 C8 C9 CC(02 03 04 06 09 0a 0d 0e) D6(01 04 05) DC(00 03 05 ) DF E0 E1 E2(00 02 04 0E 12 14 1D) F0(00 0C) F1 F2 FD)mccs_ver(2.1)mswhql(1))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsFalse(AreIncluded(vcpCodes, VcpCode.SpeakerVolume));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 5, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_D1_8()
	{
		// DELL P2717H
		var source = @"(prot(monitor)type(LCD)model(P2717H)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 08 10 12 14(05 08 0B 0C) 16 18 1A 52 60(01 0F 11) AA(01 02) AC AE B2 B6 C6 C8 C9 D6(01 04 05) DC(00 02 03 05) DF E0 E1 E2(00 1D 01 02 04 0E 12 14) F0(0C) F1 F2 FD)mswhql(1)asset_eep(40)mccs_ver(2.1))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsFalse(AreIncluded(vcpCodes, VcpCode.SpeakerVolume));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 5, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_H1_1()
	{
		// HP LE1711
		var source = @"(prot(monitor)type(lcd)model(HP LE1711)cmds(01 02 03 07 0C 4E F3 E3)vcp(02 04 05 06 08 0B 0C 0E 10 12 14(01 05 08 0B) 16 18 1A 1E 1F 20 30 3E 52 60(01) 6C 6E 70 AC AE B6 C0 C6 C8 C9 CA CC(01 02 03 04 05 06 08 0A 0D 14) D6(01 04 05) DF FA(00 01 02) FB FC FD FE(00 01 02 04) )mswhql(1)mccs_ver(2.1)asset_eep(32)mpu_ver(01))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsFalse(AreIncluded(vcpCodes, VcpCode.SpeakerVolume));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 5, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_N1_1()
	{
		// NEC L220W
		var source = @"(vcp(02 04 05 06 08 0E 10 12 14(01 02 06 08 0B 0E) 16 18 1A 1E 20 30 3E 68(01 02 03 04 05 06 07 09 0D) B0 B6 DF E3 F4 F5(01 02 03 04 05 06 07 09 0D) F9 FA FC FF)vcp_p02(33 37 47 52 64 65 DA EA FF)vcp_p10(10 11 26 27 28 29 2A 2B 2C 2D)prot(monitor)type(LCD)cmds(01 02 03 07 0C C2 C4 C6 C8 F3)mccs_ver(2.0)asset_eep(20)mpu_ver(1.02)model(L220W)mswhql(1))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsFalse(AreIncluded(vcpCodes, VcpCode.SpeakerVolume));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 2, 6, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_L1_1()
	{
		// LG MP57
		var source = @"(prot(monitor)type(lcd)model(MP57)cmds(01 02 03 0C E3 F3)vcp(02030405080B0C101214(01 05 06 07 08 0B) 15(10 11 20 30 40 0B)16181A5260(01 03 04)6C6E7087ACAEB6C0C6C8C9D6(01 04)DFE0E1E3(00 01 02 03 04 10 11 12 13 14)ECEFFD(00 01)FE(00 01 02)FF)mswhql(1)mccs_ver(2.1))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsFalse(AreIncluded(vcpCodes, VcpCode.SpeakerVolume));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 5, 6, 7, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_L1_2()
	{
		// LG 27UL550-W
		var source = @"(prot(monitor)type(lcd)UL550_500cmds(01 02 03 0C E3 F3)vcp(02 04 05 08 10 12 14(05 08 0B) 16 18 1A 52 60(11 12 0F 10) AC AE B2 B6 C0 C6 C8 C9 D6(01 04) DF 62 8D F4 F5(00 01 02) F6(00 01 02) 4D 4E 4F 15(01 06 09 10 11 13 14 28 29 32  44 48) F7(00 01 02 03) F8(00 01) F9 EF FD(00 01) FE(00 01 02) FF)mccs_ver(2.1)mswhql(1))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.SpeakerVolume)); // True
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 5, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_L1_3()
	{
		// LG GP850
		var source = @"(prot(monitor)type(lcd)model(GP850)cmds(01 02 03 0C E3 F3)vcp(02 04 05 08 10 12 14(05 08 0B ) 16 18 1A 52 60(11 12 0F 10 ) AC AE B2 B6 C0 C6 C8 C9 D6(01 04) DF 62 8D F4 F5(01 02 03 04) F6(00 01 02) 4D 4E 4F 15(01 06 11 13 14 15 18 19 20 22 23 24 28 29 32 48) F7(00 01 02 03) F8(00 01) F9 EF FA(00 01) FD(00 01) FE(00 01 02) FF)mccs_ver(2.1)mswhql(1))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.SpeakerVolume)); // True
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 5, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_L2_1()
	{
		// Lenovo P27u-10
		var source = @"(prot(monitor)type(LCD)model(P27u_10)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 08 10 12 14(01 02 05 06 08 0B 0C 0D 0E 0F) 16 18 1A 52 60(0F 11 12 13) 86(02 05) AC AE B2 B6 C6 C8 C9 CA(01 02) CC(02 03 04 05 06 09 0A 0D) D6(01 04 05) DC(00 01 02 03 04 05 06) DF E0(00 01 02) EA(00 01) EB(00 01) )mswhql(1)asset_eep(40)mccs_ver(2.2))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsFalse(AreIncluded(vcpCodes, VcpCode.SpeakerVolume));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 2, 5, 6, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_L2_2()
	{
		// Lenovo P27h-10
		var source = @"(prot(monitor)type(LCD)model(P27h_10)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 06 08 10 12 14(01 05 06 08 0B) 16 18 1A 52 60(0F 10 11 12) AC AE B2 B6 C6 C8 CA CC(02 03 04 05 06 09 0A 0D) D6(01 04 05) DF FD)mswhql(1)asset_eep(40)mccs_ver(2.2))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsFalse(AreIncluded(vcpCodes, VcpCode.SpeakerVolume));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 5, 6, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_L2_3()
	{
		// Lenovo Q24i-10
		var source = @"(prot(monitor)type(LCD)model(Q24i-10)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 06 08 10 12 14(01 05 06 08 0B) 16 18 1A 52 60(01 11) 62 AC AE B2 B6 C6 C8 C9 CA CC(02 03 04 05 06 09 0A 0D) D6(01 04 05) DF)mswhql(1)asset_eep(40)mccs_ver(2.2))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.SpeakerVolume)); // True
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 5, 6, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_B1_1()
	{
		// BenQ GW2270H
		var source = @"(prot(monitor)type(LCD)model(GW2270H)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 08 0B 0C 10 12 14(04 05 08 0B) 16 18 1A 52 60(01 11 12) 62 72(50 64 78 8C A0) 86(02 05) 87 8A 8D(01 02) 90 AA(01 02 FF) AC AE B2 B5(00 01) B6 C0 C6 C8 C9 CA(01 02) CC(01 02 03 04 05 06 09 0A 0B 0D 0E 12 14 1A 1E 1F 20) DA(00 02) D6(01 05) DC(00 03 05 0B 0C 0E 12 13) DF EA(00 01 02 03 04 05) EE(00 01) EF(00 01) F0(00 01 02) F1(00 01) F2(14 28 3C 50 64) F4(00 01) F5(00 01) F7(00 01) F8(00 0A 14 1E) FC(00 01))mswhql(1)asset_eep(40)mccs_ver(2.2)mpu(1.02))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.SpeakerVolume)); // True
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 4, 5, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_B1_2()
	{
		// BenQ XL2411P
		var source = @"(prot(monitor)type(lcd)model(XL2411P)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 08 0B 0C 10 12 14(04 05 08 0B) 16 18 1A 52 60(03 0F 11) 62 8D(01 02) AC AE B2 B6 C0 C6 C8 C9 CA(01 02) CC(01 02 03 04 05 06 07 08 09 0A 0B 0D 0F 12 14 1A 1E 1F 20) D6(01 05) DF 86(01 02 05 0B 0C 0D 0E 0F 10 11 12 13) F7(00 01) DA(00 02) DC(00 03 0B 0C 0E 15 16 17 18 19 1A) EA(00 01 02 03 04 05) F4(00 01) 87 90 8A 72(50 64 78 8C A0) F8(00 0A 14 1E) EF(00 01) F0(00 01 02) )mswhql(1)mccs_ver(2.2))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.SpeakerVolume)); // True
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 4, 5, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_A1_1()
	{
		// ASUS PB277Q
		var source = @"(prot(monitor)type(lcd)model(PB277Q)cmds(01 02 03 07 0C 4E F3 E3)vcp(02 04 05 08 0B 0C 10 12 14(05 06 08 0B) 16 18 1A 6C 6E 70 AC AE B6 C0 C6 C8 C9 CC(00 01 02 03 04 05 06 07 08 09 0A 0C 0D 11 12 14 1A 1E 1F 72 23 73) D6(01 05) DF 60(01 03 11 0F) 62 8D )mswhql(1)mccs_ver(2.1)asset_eep(32)mpu_ver(01))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.SpeakerVolume)); // True
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 5, 6, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_A1_2()
	{
		// ASUS VG279Q
		var source = @"(prot(monitor) type(LCD)model(VG279Q) cmds(01 02 03 07 0C F3) vcp(02 04 05 08 10 12 14(05 06 08 0B) 16 18 1A 52 60(03 0F 11) 62 86(02 0B) 87(00 0A 14 1E 28 32 3C 46 50 5A 64) 8A 8D(01 02) AC AE B6 C6 C8 CC(01 02 03 04 05 06 07 08 09 0A 0C 0D 11 12 14 1A 1E 1F 23 30 31) D6(01 05) DF DC(03 0B 0D 0E 11 12 13 14) E0(01 02 03) E1(00 01) E2(00 14 28 3C 50 64) E3(00 19 32 4B 64) E4(00 01) E6(00 01 02 03 04) E7(00 01) E9(00 01) EA(00 01) EB(00 01))mccs_ver(2.2)asset_eep(32)mpu(01)mswhql(1))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.SpeakerVolume)); // True
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 5, 6, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_A2_1()
	{
		// Acer XB271HU
		var source = @"(prot(monitor)type(lcd)model(XB271HU)cmds(01 02 03 06 07 0C E3 F3)vcp(02 03(01) 04 05 08 10 12 14(03 05 09 0B) 16 18 1A 2E 52 59 5A 5B 5C 5D 5E 72(00 78 FF) 8A AC AE B6 C0 C8 C9 CA DF)mccs_ver(2.2)vcpname(10(Brightness))mswhql(1))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsFalse(AreIncluded(vcpCodes, VcpCode.SpeakerVolume));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 3, 5, 9));
	}

	[TestMethod]
	public void TestMonitorCapability_A2_2()
	{
		// Acer XV272U KV
		var source = @"(prot(monitor)type(LCD)model(ACER XV272UKV)cmds(01 02 03 07 0C E3 F3)vcp(04 10 12 14(05 06 08 0B) 16 18 1A 59 5A 5B 5C 5D 5E 60(0F 11 12) 62 6C 6E 70 8D 9B 9C 9D 9E 9F A0 D6 E0(00 04 05) E1(00 01 02) E2(00 01 02 03 05 06 07 0B 20 21 22) E3 E4 E5 E7(00 01 02) E8(00 01 02 03 04)) mswhql(1)asset_eep(40)mccs_ver(2.2))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.SpeakerVolume));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 5, 6, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_A3_1()
	{
		// AOC G2460PG
		var source = @"(prot(monitor)type(lcd)model(G2460PG)cmds(01 02 03 06 07 0C E3 F3)vcp(02 03(01) 04 05 08 10 12 14(03 05 09 0B) 16 18 1A 2E 52 59 5A 5B 5C 5D 5E 72(00 78 FF) 8A AC AE B6 C0 C8 C9 CA DF)mccs_ver(2.2)vcpname(10(Brightness))mswhql(1))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsFalse(AreIncluded(vcpCodes, VcpCode.SpeakerVolume));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 3, 5, 9));
	}

	[TestMethod]
	public void TestMonitorCapability_A3_2()
	{
		// AOC AG271QG
		var source = @"(prot(monitor)type(lcd)model(AG271QG)cmds(01 02 03 06 07 0C E3 F3)vcp(02 03(01) 04 05 08 10 12 14(03 05 09 0B) 16 18 1A 2E 52 59 5A 5B 5C 5D 5E 72(00 78 FF) 8A AC AE B6 C0 C8 C9 CA DF)mccs_ver(2.2)vcpname(10(Brightness))mswhql(1))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsFalse(AreIncluded(vcpCodes, VcpCode.SpeakerVolume));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 3, 5, 9));
	}

	[TestMethod]
	public void TestMonitorCapability_A3_3()
	{
		// AOC G2590FX
		var source = @"(prot(monitor)type(lcd)model(G2590FX)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 08 10 12 14(01 05 06 08 0B) 16 18 1A 52 60(01 11 12 0F) 62 8D(01 02) AC AE B6 C0 C6 C8 C9 CA(01 02) CC(02 03 04 05 07 08 09 0A 0D 01 06 0B 12 14 16 1E) 86(01 02 05 0B 0C 0F 10 11) D6(01 05) DC(00 0B 0C 0D 0E 0F 10) DF FF)mswhql(1)asset_eep(40)mccs_ver(2.2)))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.SpeakerVolume)); // True
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature, 5, 6, 8));
	}

	[TestMethod]
	public void TestMonitorCapability_A4_1()
	{
		// Apple Cinema Display
		var source = @"prot(monitor) type(LCD) model(LED Cinema Display) cmds(01 02 03 E3 F3) VCP(02 05 10 52 62 66 8D 93 B6 C0 C8 C9 CA D6(00 01 02 03 04) DF) mccs_ver(2.2)";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsFalse(AreIncluded(vcpCodes, VcpCode.Contrast)); // False
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.SpeakerVolume)); // True
		Assert.IsFalse(AreIncluded(vcpCodes, VcpCode.Temperature)); // False
	}

	[TestMethod]
	public void TestMonitorCapability_V1_1()
	{
		// ViewSonic VX2370
		var source = @"(prot(monitor)type(LCD)model(VSC)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 08 0B 0C 10 12 14(01 08 06 05 04 0B) 16 18 1A 52 60(01 03 0F) 62 6C 6E 70 87 8D AC AE B2 B6 C6 C8 C9 CA CC(02 03 04 0a 05 16 09 06 07 0d 01) D6(01 04) DF E1)mswhql(1)asset_eep(40)mccs_ver(2.0))";
		var (success, vcpCodes) = TestExtractVcpCodes(source);
		Assert.IsTrue(success);
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Luminance));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Contrast));
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.SpeakerVolume)); // True
		Assert.IsTrue(AreIncluded(vcpCodes, VcpCode.Temperature));
	}

	private enum VcpCode : byte
	{
		None = 0x0,
		Luminance = 0x10,
		Contrast = 0x12,
		Temperature = 0x14,
		InputSource = 0x60,
		SpeakerVolume = 0x62,
		PowerMode = 0xD6,
	}

	private static (bool success, Dictionary<byte, byte[]> vcpCodes) TestExtractVcpCodes(string source)
	{
		var @class = new PrivateType(typeof(MonitorConfiguration));
		var dic = @class.InvokeStatic("ExtractVcpCodes", source) as Dictionary<byte, byte[]>;
		var enumerator = @class.InvokeStatic("EnumerateVcpCodes", source) as IEnumerable<byte>;

		bool success = (dic is { Count: > 0 }) && dic.Keys.SequenceEqual(enumerator?.ToArray());
		return (success, dic);
	}

	private static bool AreIncluded(Dictionary<byte, byte[]> source, VcpCode key, params byte[] elements)
	{
		if (!source.TryGetValue((byte)key, out byte[] values))
			return false;

		return (elements.Length is 0)
			|| (values.Intersect(elements).Count() == elements.Length);
	}
}