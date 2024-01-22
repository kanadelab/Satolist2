using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Core
{
	internal class SSTPCallBackNativeWindow : IDisposable
	{
		private const string WindowName = "SatolistSSTPCallback";
		private const string WindowClassName = "SatolistSSTPCallback";

		private static readonly object staticLock = new object();
		private static readonly Dictionary<IntPtr, SSTPCallBackNativeWindow> hWndDictionary = new Dictionary<IntPtr, SSTPCallBackNativeWindow>();
		private static Win32Import.WndProcDelegate wndProc;

		private Dictionary<int, Action<int, IntPtr, IntPtr>> callbackDictionary;
		public IntPtr HWnd { get; private set; }

		public SSTPCallBackNativeWindow(IntPtr parent)
		{
			lock (staticLock)
			{
				var hInstance = Win32Import.GetModuleHandle(IntPtr.Zero);

				//ウインドウクラスの登録は一度だけ行う
				if (wndProc == null)
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
						hInstance = hInstance,
						hIcon = IntPtr.Zero,
						hCursor = IntPtr.Zero,
						hbrBackGround = IntPtr.Zero,
						lpszMenuName = "",
						lpszClassName = WindowClassName,
						hIconSm = IntPtr.Zero
					};

					if (Win32Import.RegisterClassEx(ref windowClass) == 0)
						throw new Win32Exception(Marshal.GetLastWin32Error());
				}

				callbackDictionary = new Dictionary<int, Action<int, IntPtr, IntPtr>>();

				//ウインドウの生成
				HWnd = Win32Import.CreateWindowEx(0, WindowClassName, WindowName, 0, 0, 0, 100, 100, parent, IntPtr.Zero, hInstance, IntPtr.Zero);
				if (HWnd == IntPtr.Zero)
					throw new Win32Exception(Marshal.GetLastWin32Error());

				//クラスの登録
				hWndDictionary.Add(HWnd, this);
			}
		}

		public RegisteredCallbackToken RegisterCallback(int msg, Action<int, IntPtr, IntPtr> action)
		{
			lock (this)
			{
				if (callbackDictionary.ContainsKey(msg))
					throw new InvalidOperationException();
				callbackDictionary.Add(msg, action);
				return new RegisteredCallbackToken(msg, this);
			}
		}

		public void UnregisterCallback(int msg)
		{
			lock (this)
			{
				if (!callbackDictionary.ContainsKey(msg))
					throw new InvalidOperationException();
				callbackDictionary.Remove(msg);
			}
		}

		public void Dispose()
		{
			lock (staticLock)
			{
				Win32Import.DestroyWindow(HWnd);
				hWndDictionary.Remove(HWnd);

				//すべての登録が解除されたら、ウインドウクラスを解放する
				if (hWndDictionary.Count == 0)
				{
					Win32Import.UnregisterClass(WindowClassName, Win32Import.GetModuleHandle(IntPtr.Zero));
					wndProc = null;
				}
			}
		}

		private static IntPtr WindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
		{
			//Instance登録前の呼び出しは無効
			SSTPCallBackNativeWindow callTarget;
			lock (staticLock)
			{
				hWndDictionary.TryGetValue(hWnd, out callTarget);
			}

			if (callTarget != null)
			{
				//登録したコールバックを呼び出し
				if (callTarget.callbackDictionary.TryGetValue(msg, out var func))
				{
					func.Invoke(msg, wParam, lParam);
				}
			}

			return Win32Import.DefWindowProc(hWnd, msg, wParam, lParam);
		}

		//usingでコールバックを消せるようにするためのもの
		public class RegisteredCallbackToken : IDisposable
		{
			private int msg;
			private SSTPCallBackNativeWindow target;

			public RegisteredCallbackToken(int msg, SSTPCallBackNativeWindow target)
			{
				this.msg = msg;
				this.target = target;
			}

			public void Dispose()
			{
				target.UnregisterCallback(msg);
			}
		}
	}
}
