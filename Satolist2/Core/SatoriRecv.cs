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
	//れしばのウインドウメッセージを受け取るウインドウ
	//ネイティブwin32APIを使ってウインドウを生成する
	public class SatoriRecvNativeWindow
	{
		private const string WindowName = "れしば";
		private const string WindowClassName = "れしば";

		//ウインドウハンドル
		private IntPtr hWnd;
		//受信コールバック
		private Action<string> onReceiveCallback;

		//ウインドウプロシージャがGCに回収されないように参照しておく
		private Win32Import.WndProcDelegate wndProc;

		public static SatoriRecvNativeWindow Instance { get; private set; }

		//シングルトン開放生成
		public static void Create(IntPtr parent, Action<string> onReceive)
		{
			if (Instance != null)
				throw new InvalidOperationException();
			Instance = new SatoriRecvNativeWindow(parent);
			Instance.onReceiveCallback = onReceive;
		}

		public static void Destroy()
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

		private SatoriRecvNativeWindow(IntPtr parent)
		{
			//ウインドウプロシージャ関数のデリゲート化
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
			hWnd = Win32Import.CreateWindow(0, WindowClassName, WindowName, 0, 0, 0, 100, 100, parent, IntPtr.Zero, Win32Import.GetModuleHandle(IntPtr.Zero), IntPtr.Zero);
			if (Marshal.GetLastWin32Error() != 0)
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		private void Dispose()
		{
			Win32Import.DestroyWindow(hWnd);
			Win32Import.UnregisterClass(WindowClassName, Win32Import.GetModuleHandle(IntPtr.Zero));
			wndProc = null;
		}

		//ウインドウプロシージャ
		private static IntPtr WindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
		{
			if(msg == Win32Import.WM_COPYDATA)	//WM_COPYDATA
			{
				var body = Win32Import.CopyDataStructToString(lParam);
				Data(body);
			}
			return Win32Import.DefWindowProc(hWnd, msg, wParam, lParam);
		}

		private static void Data(string data)
		{
			Instance.onReceiveCallback?.Invoke(data);
		}

		

		
	}

	public static class Win32Import
	{
		public const int WM_COPYDATA = 0x004a;

		public static readonly UIntPtr SSTP_DWDATA = (UIntPtr)9801;

		public delegate IntPtr WndProcDelegate(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);
		public struct NativeWindowClassEx
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

		public struct CopyDataStruct
		{
			public UIntPtr dwData;
			public uint cbData;
			public IntPtr lpData;
		}

		public static string CopyDataStructToString(IntPtr lParam)
		{
			CopyDataStruct cds = (CopyDataStruct)Marshal.PtrToStructure<CopyDataStruct>(lParam);
			if (cds.dwData != SSTP_DWDATA)
				return null;	//想定外

			byte[] data = new byte[cds.cbData];
			Marshal.Copy(cds.lpData, data, 0, data.Length);
			var body = Constants.EncodingShiftJis.GetString(data);
			return body;
		}

		//れしばとしてやりとりするためのウインドウ操作系API
		[DllImport("user32.dll", EntryPoint = "RegisterClassExA", SetLastError = true)]
		public static extern ushort RegisterClassEx(ref NativeWindowClassEx classEx);
		[DllImport("kernel32.dll", EntryPoint = "GetModuleHandle", SetLastError = true)]
		public static extern IntPtr GetModuleHandle(IntPtr name);
		[DllImport("user32.dll", EntryPoint = "CreateWindowExA", SetLastError = true)]
		public static extern IntPtr CreateWindow(int exstyle, [MarshalAs(UnmanagedType.LPStr)] string className, [MarshalAs(UnmanagedType.LPStr)] string windowName, int style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr hInstance, IntPtr param);
		[DllImport("user32.dll", EntryPoint = "DefWindowProc", SetLastError = true)]
		public static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
		[DllImport("user32.dll", EntryPoint = "UnregisterClassA", SetLastError = true)]
		public static extern int UnregisterClass([MarshalAs(UnmanagedType.LPStr)] string className, IntPtr hInstance);
		[DllImport("user32.dll", EntryPoint = "DestroyWindow", SetLastError = true)]
		public static extern int DestroyWindow(IntPtr hWnd);
		[DllImport("user32.dll")]
		public static extern IntPtr SendMessageTimeoutA(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, IntPtr lpdwResult);
	}
}
