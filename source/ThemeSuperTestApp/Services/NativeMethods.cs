using System;
using System.Runtime.InteropServices;

namespace ThemeSuperTestApp.Services
{
	/// <summary>
	/// Win32 P/Invoke declarations isolated per <c>CA1060</c> and <c>CA5392</c>.
	/// </summary>
	internal static class NativeMethods
	{
		internal const int WmSysColorChange = 0x0015;
		internal const int WmSettingChange = 0x001A;
		internal const int WmThemeChanged = 0x031A;
		internal const int WmDwmColorizationColorChanged = 0x0320;

		internal const int DwmwaUseImmersiveDarkModeLegacy = 19;
		internal const int DwmwaUseImmersiveDarkMode = 20;

		internal const uint PwRenderFullContent = 0x00000002;

		[DllImport("ntdll.dll", ExactSpelling = true)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		internal static extern int RtlGetVersion(ref OsVersionInfoEx versionInfo);

		[DllImport("dwmapi.dll", ExactSpelling = true)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

		[DllImport("user32.dll", ExactSpelling = true)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetWindowRect(IntPtr hWnd, out Win32Rect lpRect);

		[DllImport("user32.dll", ExactSpelling = true)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool IsIconic(IntPtr hWnd);

		[DllImport("user32.dll", ExactSpelling = true)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

		[DllImport("user32.dll", ExactSpelling = true)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		internal static extern IntPtr GetWindowDC(IntPtr hWnd);

		[DllImport("user32.dll", ExactSpelling = true)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		internal static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

		[DllImport("gdi32.dll", ExactSpelling = true)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		internal static extern IntPtr CreateCompatibleDC(IntPtr hdc);

		[DllImport("gdi32.dll", ExactSpelling = true)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		internal static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

		[DllImport("gdi32.dll", ExactSpelling = true)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		internal static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

		[DllImport("gdi32.dll", ExactSpelling = true)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool DeleteObject(IntPtr ho);

		[DllImport("gdi32.dll", ExactSpelling = true)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool DeleteDC(IntPtr hdc);

		[StructLayout(LayoutKind.Sequential)]
		internal struct OsVersionInfoEx
		{
			internal int OsVersionInfoSize;
			internal int MajorVersion;
			internal int MinorVersion;
			internal int BuildNumber;
			internal int PlatformId;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
			internal short[]? CsVersion;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct Win32Rect
		{
			internal int Left;
			internal int Top;
			internal int Right;
			internal int Bottom;
		}
	}
}