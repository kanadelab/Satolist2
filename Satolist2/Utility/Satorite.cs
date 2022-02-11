using Satolist2.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Utility
{
	//さとりて機能
	static public class Satorite
	{
		//里々にロードさせるために、一時的に生成されるテキストファイル
		private const string TemporaryDictionaryFileName = "dic_made_by_satolist.txt";
		private const string SatoriProxyPath = "data/SatoriProxy.exe";
		private const string SatoriteEventName = "Satolist2_Satorite_Event";


		[DllImport("data/sstp.dll", EntryPoint = "SendSSTP", CallingConvention = CallingConvention.Cdecl)]
		private extern static void SendSSTP(IntPtr ptr);

		[DllImport("data/sstp.dll", EntryPoint = "NotifySSTP", CallingConvention = CallingConvention.Cdecl)]
		private extern static void NotifySSTP(IntPtr ptr);

		//SEND SSTPによるスクリプトの送信
		public static void SendSSTP(string request)
		{
			byte[] requestBytes = Constants.EncodingShiftJis.GetBytes(request);
			IntPtr requestPtr = Marshal.AllocHGlobal(requestBytes.Length);
			Marshal.Copy(requestBytes, 0, requestPtr, requestBytes.Length);
			SendSSTP(requestPtr);
			Marshal.FreeHGlobal(requestPtr);
		}

		//NOTIFY SSTP によるイベントの起動
		public static void NotifySSTP(string request)
		{
			byte[] requestBytes = Constants.EncodingShiftJis.GetBytes(request);
			IntPtr requestPtr = Marshal.AllocHGlobal(requestBytes.Length);
			Marshal.Copy(requestBytes, 0, requestPtr, requestBytes.Length);
			NotifySSTP(requestPtr);
			Marshal.FreeHGlobal(requestPtr);
		}

		//スクリプトを里々にかけて、出力したさくらスクリプトを戻す
		public static void SendSatori(GhostModel ghost, string script, EventType type)
		{
			throw new NotImplementedException();
			var saveData = new SaveDataBuilder(ghost);
			var dictionaryDirectory = ".";

			if(saveData.Loaded && saveData.SaveData.ContainsKey(Constants.VariableDictionaryDirectory))
			{
				dictionaryDirectory = null;

				//ロードするよう指定されているフォルダの１つを取り出してそこに配置するようする
				var sp = saveData.SaveData[Constants.VariableDictionaryDirectory].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach(var dir in sp)
				{
					//有効なフォルダを選択していたらそれを使用する
					if(Directory.Exists(ghost.FullDictionaryPath + "/" + dir))
					{
						dictionaryDirectory = dir;
						break;
					}
				}
			}

			//このタイミングで起動できなかったらゴーストがそもそも辞書をロードしようとしてない？
			if (string.IsNullOrEmpty(dictionaryDirectory))
				throw new Exception("辞書フォルダが無効");

			//さとりてに読ませる辞書を作成
			string filePath = ghost.FullDictionaryPath + "/" + dictionaryDirectory + "/" + TemporaryDictionaryFileName;
			var writer = new StreamWriter(filePath, false, Constants.EncodingShiftJis);
			writer.WriteLine(Constants.GetEventHead(type) + SatoriteEventName);
			writer.WriteLine(script);
			writer.Close();

			//里々にリクエストを投げてイベントを実行
			var result = ExecuteSatori(ghost.FullDictionaryPath, SatoriteEventName);

			//実行を終えたので一時的な辞書を終了
			File.Delete(filePath);

			//出力されたさくらスクリプトをSSTPでゴーストに投げる
			var parsedResult = new ProtocolBuilder();
			parsedResult.Deserialize(result);
			
			if(parsedResult.Parameters.ContainsKey("Value"))
			{
				string resultScript = parsedResult.Parameters["Value"];

				var sstpBuilder = new ProtocolBuilder();
				sstpBuilder.Command = "SEND SSTP/1.0";
				sstpBuilder.Parameters["Script"] = resultScript;
				sstpBuilder.Parameters["Sender"] = "さとりすと";
				
				//ifghostを設定
				if(!string.IsNullOrEmpty(ghost.GhostDescriptSakuraName))
				{
					sstpBuilder.Parameters["IfGhost"] = ghost.GhostDescriptSakuraName;
				}
				
				if(true)//use Owned SSTP
				{
					var fmoReader = new SakuraFMOReader();
					fmoReader.Read();
					var fmoRecord = fmoReader.Find(ghost);

					if(fmoRecord != null)
					{
						sstpBuilder.Parameters["ID"] = fmoRecord.ID;
					}
				}

				SendSSTP(sstpBuilder.Serialize());
			}
		}

		//里々を起動
		//さとりすと側のプロセスに依存せず別プロセスで里々を起動するのでさとりすとが64bitでも起動できるしくみ
		public static string ExecuteSatori(string satoriDirectory, string eventName)
		{
			try
			{
				var startInfo = new ProcessStartInfo();
				startInfo.WorkingDirectory = Environment.CurrentDirectory;
				startInfo.FileName = startInfo.WorkingDirectory + "/" + SatoriProxyPath;
				startInfo.UseShellExecute = false;
				startInfo.RedirectStandardOutput = true;
				startInfo.Arguments = string.Format("SendSatori \"{0}\" \"{1}\"", eventName, satoriDirectory);
				startInfo.CreateNoWindow = true;

				using(var process = Process.Start(startInfo))
				{
					var standardOutput = process.StandardOutput.ReadToEnd();
					process.WaitForExit();

					if (process.ExitCode != 0)
						return string.Empty;
					return standardOutput;
				}
			}
			catch
			{
				return string.Empty;
			}
		}

		//FMOからOwned SSTP用のIDを取得する
		private static string GetOwnedSSTPId(GhostModel ghost)
		{
			throw new NotImplementedException();
		}


		//SSPへの単純データ送信
		public static void RaiseSSTP(ProtocolBuilder data, SakuraFMORecord target)
		{
			var serializedData = data.Serialize();
			var dataBytes = Constants.EncodingShiftJis.GetBytes(serializedData);
			var dataPtr = Marshal.AllocHGlobal(dataBytes.Length);
			Marshal.Copy(dataBytes, 0, dataPtr, dataBytes.Length);

			var copydata = new CopyDataStruct()
			{
				dwData = (UIntPtr)9801,
				cbData = (uint)dataBytes.Length,
				lpData = dataPtr
			};
			var h = GCHandle.Alloc(copydata, GCHandleType.Pinned);

			//TODO: マジックナンバーフラグの定数化
			SendMessageTimeoutA(target.HWnd, 0x004A, UIntPtr.Zero, h.AddrOfPinnedObject(), 2, 5000, IntPtr.Zero);

			Marshal.FreeHGlobal(dataPtr);
		}

		[DllImport("USER32.dll")]
		private static extern IntPtr SendMessageTimeoutA(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam, uint flags, uint timeout, IntPtr lpdwResult);
	}


	internal struct CopyDataStruct
	{
		public UIntPtr dwData;
		public uint cbData;
		public IntPtr lpData;
	}

}
