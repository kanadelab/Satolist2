using MahApps.Metro.Converters;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel.Configuration;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
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
			hWnd = Win32Import.CreateWindowEx(0, WindowClassName, WindowName, 0, 0, 0, 100, 100, parent, IntPtr.Zero, Win32Import.GetModuleHandle(IntPtr.Zero), IntPtr.Zero);
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
				var body = Win32Import.CopyDataStructToString(lParam, Win32Import.RECV_DWDATA);
				if(body != null)
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
		public const int TRUE = 1;
		public const int FALSE = 0;

		public static readonly UIntPtr SSTP_DWDATA = (UIntPtr)9801;
		public static readonly UIntPtr RECV_DWDATA = (UIntPtr)0;

		public const int CS_HREDRAW = 0x0002;
		public const int CS_VREDRAW = 0x0001;

		public const int GWL_STYLE = -16;
		public const int GWL_EXSTYLE = -20;

		public const int SWP_NOZORDER = 0x0004;
		public const int SWP_NOSIZE = 0x0001;

		public const int WS_EX_LAYERED = 0x00080000;
		public const int WS_CHILD = 0x40000000;

		public const int JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;
		public const int JobObjectExtendedLimitInformation = 9;

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

		public struct WindowPlacement
		{
			public int length;
			public int flags;
			public SW chowCmd;
			public POINT minPosition;
			public POINT maxPosition;
			public RECT normalPosition;
		}

		public enum SW
		{
			HIDE = 0,
			SHOWNORMAL = 1,
			SHOWMINIMIZED = 2,
			SHOWMAXIMIZED = 3,
			SHOWNOACTIVATE = 4,
			SHOW = 5,
			MINIMIZE = 6,
			SHOWMINNOACTIVE = 7,
			SHOWNA = 8,
			RESTORE = 9,
			SHOWDEFAULT = 10,
		}

		public struct POINT
		{
			public int x;
			public int y;

			public int X
			{
				get => x;
				set => x = value;
			}

			public int Y
			{
				get => y;
				set => y = value;
			}
		}

		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;

			public int Left
			{
				get => left;
				set => left = value;
			}

			public int Top
			{
				get => top;
				set => top = value;
			}

			public int Right
			{
				get => right;
				set => right = value;
			}

			public int Bottom
			{
				get => bottom;
				set => bottom = value;
			}
		}

		public static string CopyDataStructToString(IntPtr lParam, UIntPtr targetDwData )
		{
			CopyDataStruct cds = (CopyDataStruct)Marshal.PtrToStructure<CopyDataStruct>(lParam);

			if (cds.dwData != targetDwData)
				return null;	//想定外
			
			byte[] data = new byte[cds.cbData];
			Marshal.Copy(cds.lpData, data, 0, data.Length);
			var body = Constants.EncodingShiftJis.GetString(data);
			return body;
		}

		public struct JOBOBJECT_BASIC_LIMIT_INFORMATION
		{
			public long PerProcessUserTimeLimit;
			public long PerJobUserTimeLimit;
			public uint LimitFlags;
			public ulong MinimumWorkingSetSize;
			public ulong MaximumWorkingSetSize;
			public uint ActiveProcessLimit;
			public UIntPtr Affinity;
			public uint PriorityClass;
			public uint SchedulingClass;
		}

		public struct IO_COUNTERS
		{
			public ulong ReadOperationCount;
			public ulong WriteOperationCount;
			public ulong OtherOperationCount;
			public ulong ReadTransferCount;
			public ulong WriteTransferCount;
			public ulong OtherTransferCount;
		}

		public struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
		{
			public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
			public IO_COUNTERS IoInfo;
			public ulong ProcessMemoryLimit;
			public ulong JobMemoryLimit;
			public ulong PeakProcessMemoryUsed;
			public ulong PeakJobMemoryUsed;
		}

		//れしばとしてやりとりするためのウインドウ操作系API
		[DllImport("user32.dll", EntryPoint = "RegisterClassExA", SetLastError = true)]
		public static extern ushort RegisterClassEx(ref NativeWindowClassEx classEx);
		[DllImport("kernel32.dll", EntryPoint = "GetModuleHandle", SetLastError = true)]
		public static extern IntPtr GetModuleHandle(IntPtr name);
		[DllImport("user32.dll", EntryPoint = "CreateWindowExA", SetLastError = true)]
		public static extern IntPtr CreateWindowEx(int exstyle, [MarshalAs(UnmanagedType.LPStr)] string className, [MarshalAs(UnmanagedType.LPStr)] string windowName, int style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr hInstance, IntPtr param);
		[DllImport("user32.dll", EntryPoint = "DefWindowProc", SetLastError = true)]
		public static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
		[DllImport("user32.dll", EntryPoint = "UnregisterClassA", SetLastError = true)]
		public static extern int UnregisterClass([MarshalAs(UnmanagedType.LPStr)] string className, IntPtr hInstance);
		[DllImport("user32.dll", EntryPoint = "DestroyWindow", SetLastError = true)]
		public static extern int DestroyWindow(IntPtr hWnd);
		[DllImport("user32.dll")]
		public static extern IntPtr SendMessageTimeoutA(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, IntPtr lpdwResult);
		[DllImport("user32.dll")]
		public static extern int IsWindow(IntPtr hwnd);
		[DllImport("user32.dll")]
		public static extern int SetWindowPlacement(IntPtr hwnd, ref WindowPlacement placement);
		[DllImport("user32.dll")]
		public static extern int GetWindowPlacement(IntPtr hwnd, ref WindowPlacement placement);

		[DllImport("user32.dll")]
		public static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

		[DllImport("user32.dll")]
		public static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
		[DllImport("user32.dll")]
		public static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr newLong);
		[DllImport("user32.dll")]
		public static extern IntPtr SetParent(IntPtr child, IntPtr parent);
		[DllImport("user32.dll")]
		public static extern int GetClientRect(IntPtr hwnd, ref RECT rect);
		[DllImport("user32.dll")]
		public static extern int SetWindowPos(IntPtr hwmd, IntPtr hwndInsertAfter, int x, int y, int cx, int cy, uint flags);

		public delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lparam);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool EnumWindows(EnumWindowsDelegate lpEnumFunc,
			IntPtr lparam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int GetWindowText(IntPtr hWnd,
			StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int GetWindowTextLength(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int GetClassName(IntPtr hWnd,
			StringBuilder lpClassName, int nMaxCount);

		[DllImport("kernel32.dll", EntryPoint = "CreateJobObjectA")]
		public static extern IntPtr CreateJobObject(IntPtr jobAttributes, string name);

		[DllImport("kernel32.dll")]
		public static extern int SetInformationJobObject(IntPtr hJob, int jobInfoClass, ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION info, uint jobInfoLength);

		[DllImport("kernel32.dll")]
		public static extern int AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

		[DllImport("kernel32.dll")]
		public static extern int CloseHandle(IntPtr handle);
	}
}
