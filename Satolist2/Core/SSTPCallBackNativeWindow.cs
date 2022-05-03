using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Core
{
	internal class SSTPCallBackNativeWindow
	{
		private const string WindowName = "SatolistSSTPCallback";
		private const string WindowClassName = "SatolistSSTPCallback";

		private IntPtr hWnd;
		private Action<string> onReceiveCallback;
		private Win32Import.WndProcDelegate wndProc;
		public static SSTPCallBackNativeWindow Instance { get; private set; }
		public static void Create(IntPtr parent, Action<string> onReceive)
		{
			if (Instance != null)
				throw new InvalidOperationException();
			Instance = new SSTPCallBackNativeWindow(parent);
			Instance.onReceiveCallback = onReceive;
		}

		public static void Destory()
		{
			if (Instance == null)
				throw new InvalidOperationException();
			Instance.Dispose();
			Instance = null;
		}

		public static bool IsCreated
		{
			get => Instance != null;
		}

		private SSTPCallBackNativeWindow(IntPtr parent)
		{
			//ウインドウプロシージャ関数をデリゲート化して保持
			wndProc = new Win32Import.WndProcDelegate(WindowProc);

			//ウインドウクラスの登録
			var windowClass = new Win32Import.NativeWindowClassEx()
			{
				cbSize = (uint)Marshal.SizeOf(typeof(Win32Import.NativeWindowClassEx)),
				style = 0,
				lpfnWndProc = wndProc,
				cbClsExtra = 0,
				cbWndExtra = 0,
				hInstance = Win32Import.GetModuleHandle(IntPtr.Zero),
				hIcon = IntPtr.Zero,
				hCursor = IntPtr.Zero,
				hbrBackGround = IntPtr.Zero,
				lpszMenuName = "",
				lpszClassName = WindowClassName,
				hIconSm = IntPtr.Zero
			};
			Win32Import.RegisterClassEx(ref windowClass);
			if (Marshal.GetLastWin32Error() != 0)
				throw new Win32Exception(Marshal.GetLastWin32Error());

			//ウインドウの生成
			hWnd = Win32Import.CreateWindow(0, WindowClassName, WindowName, 0, 0, 0, 100, 100, parent, IntPtr.Zero, windowClass.hInstance, IntPtr.Zero);
			if (Marshal.GetLastWin32Error() != 0)
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		private void Dispose()
		{
			Win32Import.DestroyWindow(hWnd);
			Win32Import.UnregisterClass(WindowClassName, Win32Import.GetModuleHandle(IntPtr.Zero));
			wndProc = null;
		}

		private static IntPtr WindowProc(IntPtr hWnd, int msg, UIntPtr wParam, IntPtr lParam)
		{
			//ここで対応

			return Win32Import.DefWindowProc(hWnd, msg, wParam, lParam);
		}
	}
}
