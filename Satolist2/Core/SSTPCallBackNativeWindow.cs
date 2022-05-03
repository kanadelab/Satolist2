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
		private static readonly object LockObject = new object();

		private Win32Import.WndProcDelegate wndProc;
		private Dictionary<int, Action<int, IntPtr, IntPtr>> callbackDictionary;

		public const int OperationProgressMessage = 0x0401;
		public static SSTPCallBackNativeWindow Instance { get; private set; }
		public IntPtr HWnd { get; private set; }
		public static void Create(IntPtr parent)
		{
			lock (LockObject)
			{
				if (Instance != null)
					throw new InvalidOperationException();
				Instance = new SSTPCallBackNativeWindow(parent);
			}
		}

		public static void Destory()
		{
			lock (LockObject)
			{
				if (Instance == null)
					throw new InvalidOperationException();
				Instance.Dispose();
				Instance = null;
			}
		}

		public static bool IsCreated
		{
			get
			{
				lock (LockObject)
				{
					return Instance != null;
				}
			}
		}

		private SSTPCallBackNativeWindow(IntPtr parent)
		{
			callbackDictionary = new Dictionary<int, Action<int, IntPtr, IntPtr>>();

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
			HWnd = Win32Import.CreateWindow(0, WindowClassName, WindowName, 0, 0, 0, 100, 100, parent, IntPtr.Zero, windowClass.hInstance, IntPtr.Zero);
			if (Marshal.GetLastWin32Error() != 0)
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		public RegisteredCallbackToken RegisterCallback(int msg, Action<int, IntPtr, IntPtr> action)
		{
			lock (LockObject)
			{
				if (callbackDictionary.ContainsKey(msg))
					throw new InvalidOperationException();
				callbackDictionary.Add(msg, action);
				return new RegisteredCallbackToken(msg);
			}
		}

		public void UnregisterCallback(int msg)
		{
			lock (LockObject)
			{
				if (!callbackDictionary.ContainsKey(msg))
					throw new InvalidOperationException();
				callbackDictionary.Remove(msg);
			}
		}

		private void Dispose()
		{
			Win32Import.DestroyWindow(HWnd);
			Win32Import.UnregisterClass(WindowClassName, Win32Import.GetModuleHandle(IntPtr.Zero));
			wndProc = null;
		}

		private static IntPtr WindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
		{
			//Instance登録前の呼び出しは無効
			lock (LockObject)
			{
				if (Instance != null)
				{
					//登録したコールバックを呼び出し
					if (Instance.callbackDictionary.ContainsKey(msg))
					{
						Instance.callbackDictionary[msg]?.Invoke(msg, wParam, lParam);
					}
				}
			}
			return Win32Import.DefWindowProc(hWnd, msg, wParam, lParam);
		}

		//usingでコールバックを消せるようにするためのもの
		public class RegisteredCallbackToken : IDisposable
		{
			private int msg;

			public RegisteredCallbackToken(int msg)
			{
				this.msg = msg;
			}

			public void Dispose()
			{
				Instance.UnregisterCallback(msg);
			}
		}
	}
}
