using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SatoriProxy
{
	/// <summary>
	/// satori.dll を呼び出すためのもの。
	/// Satolist.exe は64bitでも起動できるようにしたいので、
	/// 里々にあわせて32bitで動作させないといけないものを別プロセスに切り離したプロジェクト。
	/// </summary>
	public class Program
	{
		private static MethodInfo SatoriRequestMethodInfo { get; set; }
		private static MethodInfo SatoriLoadMethodInfo { get; set; }
		private static MethodInfo SatoriUnloadMethodInfo { get; set; }
		private static readonly Encoding SatoriEncoding = Encoding.GetEncoding("Shift_JIS");
		private static readonly Encoding CSharpEncoding = Encoding.UTF8;

		public static int Main(string[] args)
		{
			string mode = args[0];

			if(mode == "SendSatori")
			{
				var eventName = args[1];
				var dir = args[2];

				CreateMethodInfo(dir);
				Initialize(dir);
				var result = CallEvent(eventName);
				Destroy();
				Console.Write(result);
				return 0;
			}
			return 1;
		}


		//里々の初期化
		static void Initialize(string directory)
		{
			//SHIORIプロトコルは、渡された側に開放の責任がある
			string dictionaryPath = Path.GetFullPath(directory);
			byte[] pathBytes = SatoriEncoding.GetBytes(dictionaryPath);
			IntPtr pathPtr = Marshal.AllocHGlobal(pathBytes.Length);
			Marshal.Copy(pathBytes, 0, pathPtr, pathBytes.Length);

			//loadの実行
			SatoriLoad(pathPtr, pathBytes.Length);

			//SHIORI GET Versionを実行
			string request = "GET Version SHIORI/3.0\r\nSender: Satolist\r\nCharset: Shift_JIS";
			byte[] requestBytes = SatoriEncoding.GetBytes(request);
			IntPtr requestPtr = Marshal.AllocHGlobal(requestBytes.Length);
			Marshal.Copy(requestBytes, 0, requestPtr, requestBytes.Length);
			IntPtr sizePtr = Marshal.AllocHGlobal(sizeof(int));
			Marshal.Copy(BitConverter.GetBytes(requestBytes.LongLength), 0, sizePtr, sizeof(int));

			IntPtr resultPtr = SatoriRequest(requestPtr, sizePtr);
			int[] resultSize = new int[1];
			Marshal.Copy(sizePtr, resultSize, 0, 1);
			string result = Marshal.PtrToStringAnsi(resultPtr, resultSize[0]);

			//TODO: ちゃんと検証するのがいいかも

			Marshal.FreeHGlobal(sizePtr);
			Marshal.FreeHGlobal(resultPtr);
		}

		//里々の実呼び出し
		static string CallEvent(string eventName)
		{
			string request = "GET SHIORI/3.0\r\nSender: Satolist\r\nSecurityLevel: local\r\nID: " + eventName + "\r\nCharset: Shift_JIS";
			byte[] requestBytes = SatoriEncoding.GetBytes(request);
			IntPtr requestPtr = Marshal.AllocHGlobal(requestBytes.Length);
			Marshal.Copy(requestBytes, 0, requestPtr, requestBytes.Length);

			IntPtr sizePtr = Marshal.AllocHGlobal(sizeof(int));
			Marshal.Copy(BitConverter.GetBytes(requestBytes.LongLength), 0, sizePtr, sizeof(int));

			IntPtr resultPtr = SatoriRequest(requestPtr, sizePtr);
			int[] resultSize = new int[1];
			Marshal.Copy(sizePtr, resultSize, 0, 1);
			string result = Marshal.PtrToStringAnsi(resultPtr, resultSize[0]);

			Marshal.FreeHGlobal(sizePtr);
			Marshal.FreeHGlobal(resultPtr);

			return result;
		}

		//里々の開放
		static void Destroy()
		{
			SatoriUnload();
		}

		static IntPtr SatoriRequest(IntPtr h, IntPtr len)
		{
			return (IntPtr)SatoriRequestMethodInfo.Invoke(null, new object[] { h, len });
		}

		static int SatoriLoad(IntPtr h, int len)
		{
			return (int)SatoriLoadMethodInfo.Invoke(null, new object[] { h, len });
		}

		static int SatoriUnload()
		{
			return (int)SatoriUnloadMethodInfo.Invoke(null, Array.Empty<object>());
		}


		public static void CreateMethodInfo(string dictionaryPath)
		{
			AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("satori"), AssemblyBuilderAccess.Run);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("satori");
			TypeBuilder typeBuilder = moduleBuilder.DefineType("satori", TypeAttributes.Public | TypeAttributes.Class);

			MethodBuilder requestMethodBuilder = typeBuilder.DefinePInvokeMethod(
				"request",
				dictionaryPath + "/satori.dll",
				MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl | MethodAttributes.HideBySig,
				CallingConventions.Standard,
				typeof(IntPtr),
				new Type[] { typeof(IntPtr), typeof(IntPtr) },
				CallingConvention.Cdecl,
				CharSet.Ansi
				);
			requestMethodBuilder.SetImplementationFlags(MethodImplAttributes.PreserveSig);

			MethodBuilder loadMethodBuilder = typeBuilder.DefinePInvokeMethod(
				"load",
				dictionaryPath + "/satori.dll",
				MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl | MethodAttributes.HideBySig,
				CallingConventions.Standard,
				typeof(int),
				new Type[] { typeof(IntPtr), typeof(int) },
				CallingConvention.Cdecl,
				CharSet.Ansi
				);
			loadMethodBuilder.SetImplementationFlags(MethodImplAttributes.PreserveSig);

			MethodBuilder unloadMethodBuilder = typeBuilder.DefinePInvokeMethod(
				"unload",
				dictionaryPath + "/satori.dll",
				MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl | MethodAttributes.HideBySig,
				CallingConventions.Standard,
				typeof(int),
				Array.Empty<Type>(),
				CallingConvention.Cdecl,
				CharSet.Ansi
				);
			unloadMethodBuilder.SetImplementationFlags(MethodImplAttributes.PreserveSig);

			var createdType = typeBuilder.CreateType();
			SatoriRequestMethodInfo = createdType.GetMethod("request");
			SatoriLoadMethodInfo = createdType.GetMethod("load");
			SatoriUnloadMethodInfo = createdType.GetMethod("unload");
		}
	}
}
