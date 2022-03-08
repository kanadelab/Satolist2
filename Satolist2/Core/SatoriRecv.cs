using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Satolist2.Core
{
	class SatoriRecv
	{
	}

	//れしばのウインドウメッセージを受け取るウインドウ
	//ネイティブwin32APIを使ってウインドウを生成する
	public class SatoriRecvNativeWindow
	{
		private const string WindowName = "れしば";
		private const string WindowClassName = "れしば";

		//ウインドウハンドル
		private IntPtr hWnd;

		//ウインドウプロシージャがGCに回収されないように参照しておく
		private WndProcDelegate wndProc;

		public static SatoriRecvNativeWindow Instance { get; private set; }

		//シングルトン開放生成
		public static void Create(IntPtr parent)
		{
			if (Instance != null)
				throw new InvalidOperationException();
			Instance = new SatoriRecvNativeWindow(parent);
		}

		private static void Destroy()
		{
			if (Instance == null)
				throw new InvalidOperationException();
			Instance.Dispose();
			Instance = null;
		}

		private SatoriRecvNativeWindow(IntPtr parent)
		{
			//ウインドウプロシージャ関数のデリゲート化
			wndProc = new WndProcDelegate(WindowProc);

			//ウインドウクラスの登録
			NativeWindowClassEx windowClass = new NativeWindowClassEx()
			{
				cbSize = (uint)Marshal.SizeOf(typeof(NativeWindowClassEx)),
				style = 0,
				lpfnWndProc = wndProc,
				cbClsExtra = 0,
				cbWndExtra = 0,
				hInstance = GetModuleHandle(IntPtr.Zero),
				hIcon = IntPtr.Zero,
				hCursor = IntPtr.Zero,
				hbrBackGround = IntPtr.Zero,
				lpszMenuName = "",
				lpszClassName = WindowClassName,
				hIconSm = IntPtr.Zero
			};
			RegisterClassEx(ref windowClass);
			if (Marshal.GetLastWin32Error() != 0)
				throw new Win32Exception(Marshal.GetLastWin32Error());

			//ウインドウの生成
			//TODO: parentっているんだっけ…
			hWnd = CreateWindow(0, WindowClassName, WindowName, 0, 0, 0, 100, 100, parent, IntPtr.Zero, GetModuleHandle(IntPtr.Zero), IntPtr.Zero);
			if (Marshal.GetLastWin32Error() != 0)
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		private void Dispose()
		{
			DestroyWindow(hWnd);
			UnregisterClass(WindowClassName, GetModuleHandle(IntPtr.Zero));
			wndProc = null;
		}

		//ウインドウプロシージャ
		private static IntPtr WindowProc(IntPtr hWnd, int msg, UIntPtr wParam, IntPtr lParam)
		{
			if(msg == 0x004a)	//WM_COPYDATA
			{
				CopyDataStruct cds = (CopyDataStruct)Marshal.PtrToStructure<CopyDataStruct>(lParam);
				byte[] data = new byte[cds.cbData];
				Marshal.Copy(cds.lpData, data, 0, data.Length);
				var body = Constants.EncodingShiftJis.GetString(data);
				Data(body);	//TODO: 実際のれしば解析処理
			}
			return DefWindowProc(hWnd, msg, wParam, lParam);
		}

		private static void Data(string data)
		{
			Console.WriteLine(data);
		}

		private delegate IntPtr WndProcDelegate(IntPtr hwnd, int msg, UIntPtr wParam, IntPtr lParam);
		private struct NativeWindowClassEx
		{
			public uint cbSize;
			public uint style;
			[MarshalAs(UnmanagedType.FunctionPtr)]
			public WndProcDelegate lpfnWndProc;
			public int cbClsExtra;
			public int cbWndExtra;
			public IntPtr hInstance;
			public IntPtr hIcon;
			public IntPtr hCursor;
			public IntPtr hbrBackGround;
			[MarshalAs(UnmanagedType.LPStr)]
			public string lpszMenuName;
			[MarshalAs(UnmanagedType.LPStr)]
			public string lpszClassName;
			public IntPtr hIconSm;
		}

		//れしばとしてやりとりするためのウインドウ操作系API
		[DllImport("user32.dll", EntryPoint = "RegisterClassExA", SetLastError = true)]
		private static extern ushort RegisterClassEx(ref NativeWindowClassEx classEx);
		[DllImport("kernel32.dll", EntryPoint = "GetModuleHandle", SetLastError = true)]
		private static extern IntPtr GetModuleHandle(IntPtr name);
		[DllImport("user32.dll", EntryPoint = "CreateWindowExA", SetLastError = true)]
		private static extern IntPtr CreateWindow(int exstyle, [MarshalAs(UnmanagedType.LPStr)] string className, [MarshalAs(UnmanagedType.LPStr)] string windowName, int style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr hInstance, IntPtr param);
		[DllImport("user32.dll", EntryPoint = "DefWindowProc", SetLastError = true)]
		private static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, UIntPtr wParam, IntPtr lParam);
		[DllImport("user32.dll", EntryPoint = "UnregisterClassA", SetLastError = true)]
		private static extern int UnregisterClass([MarshalAs(UnmanagedType.LPStr)] string className, IntPtr hInstance);
		[DllImport("user32.dll", EntryPoint = "DestroyWindow", SetLastError = true)]
		private static extern int DestroyWindow(IntPtr hWnd);
	}
}
