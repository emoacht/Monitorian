using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Models.Monitor
{
	internal static class Error
	{
		#region Win32

		[DllImport("Kernel32.dll", SetLastError = true)]
		private static extern uint FormatMessage(
			uint dwFlags,
			IntPtr lpSource,
			uint dwMessageId,
			uint dwLanguageId,
			StringBuilder lpBuffer,
			int nSize,
			IntPtr Arguments);

		private const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;

		#endregion

		public static string CreateMessage() =>
			 CreateMessage(Marshal.GetLastWin32Error());

		public static string CreateMessage(int errorCode)
		{
			var message = new StringBuilder($"Code: {errorCode}");

			var buffer = new StringBuilder(512); // This 512 capacity is arbitrary.

			var messageLength = FormatMessage(
			  FORMAT_MESSAGE_FROM_SYSTEM,
			  IntPtr.Zero,
			  (uint)errorCode,
			  0x0409, // US (English)
			  buffer,
			  buffer.Capacity,
			  IntPtr.Zero);

			if (0 < messageLength)
				message.Append($", Message: {buffer}");

			return message.ToString();
		}
	}
}