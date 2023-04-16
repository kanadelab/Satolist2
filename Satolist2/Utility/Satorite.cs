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

		//里々のスクリプトをゴーストに送信
		public static void SendSatori(GhostModel ghost, string script, EventType type)
		{
			var sakuraScript = ExecuteSatori(ghost, script, type);
			if (!string.IsNullOrEmpty(sakuraScript))
			{
				SendSSTP(ghost, sakuraScript, true, false);
			}
		}

		//里々のスクリプトを実行してさくらスクリプト出力を返す
		public static string ExecuteSatori(GhostModel ghost, string script, EventType type)
		{
			try
			{
				var saveData = new SaveDataBuilder(ghost);
				var dictionaryDirectory = ".";

				if (saveData.Loaded && saveData.SaveData.ContainsKey(Constants.VariableDictionaryDirectory))
				{
					dictionaryDirectory = null;

					//ロードするよう指定されているフォルダの１つを取り出してそこに配置するようする
					var sp = saveData.SaveData[Constants.VariableDictionaryDirectory].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (var dir in sp)
					{
						//有効なフォルダを選択していたらそれを使用する
						if (Directory.Exists(ghost.FullDictionaryPath + "/" + dir))
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
				var result = ExecuteSatoriDLL(ghost.FullDictionaryPath, SatoriteEventName);

				//実行を終えたので一時的な辞書を終了
				File.Delete(filePath);

				//出力されたさくらスクリプトをSSTPでゴーストに投げる
				var parsedResult = new ProtocolBuilder();
				parsedResult.Deserialize(result);

				if (parsedResult.Parameters.ContainsKey("Value"))
				{
					string resultScript = parsedResult.Parameters["Value"];
					return resultScript;
				}
			}
			catch { }
			return null;
		}

		//SEND SSTPの送信
		public static void SendSSTP(SakuraFMORecord fmoRecord, string script, bool useOwnedSSTP, bool noTranslate, IntPtr hWnd = default(IntPtr))
		{
			var sstpBuilder = new ProtocolBuilder();
			sstpBuilder.Command = "SEND SSTP/1.0";
			sstpBuilder.Parameters["Script"] = script;
			sstpBuilder.Parameters["Charset"] = "Shift_JIS";
			sstpBuilder.Parameters["Sender"] = "さとりすと";

			if (hWnd != default(IntPtr))
			{
				sstpBuilder.Parameters["HWnd"] = hWnd.ToString();
			}

			if (noTranslate)
			{
				sstpBuilder.Parameters["Option"] = "notranslate";
			}

			if (useOwnedSSTP)
			{
				sstpBuilder.Parameters["ID"] = fmoRecord.ID;
			}

			RaiseSSTP(sstpBuilder, fmoRecord);
		}

		//SEND SSTPの送信
		public static void SendSSTP(GhostModel ghost, string script, bool useOwnedSSTP, bool noTranslate, IntPtr hWnd = default(IntPtr))
		{
			var fmoRecord = SakuraFMOReader.Read(ghost);
			if (fmoRecord == null)
			{
				throw new GhostNotFoundException();
			}

			SendSSTP(fmoRecord, script, useOwnedSSTP, noTranslate, hWnd);
		}

		//EXECUTE SSTPの送信
		public static void ExecuteSSTP(GhostModel ghost, string command, IntPtr hWnd = default(IntPtr))
		{
			var sstpBuilder = new ProtocolBuilder();
			sstpBuilder.Command = "EXECUTE SSTP/1.1";
			sstpBuilder.Parameters["Sender"] = "さとりすと";
			sstpBuilder.Parameters["Charset"] = "Shift_JIS";
			sstpBuilder.Parameters["Command"] = command;

			if (hWnd != default(IntPtr))
			{
				sstpBuilder.Parameters["HWnd"] = hWnd.ToString();
			}

			var fmoReader = new SakuraFMOReader();
			fmoReader.Read();

			var fmoRecord = fmoReader.Find(ghost);
			if (fmoRecord == null)
				throw new Exception();  //ゴーストが見つからない。おそらく、起動していない
			RaiseSSTP(sstpBuilder, fmoRecord);
		}

		//NOTIFY SSTPの送信
		public static void NotifySSTP(GhostModel ghost, string eventName, params string[] references)
		{
			//未実装
			throw new NotImplementedException();

			/*
			var fmoRecord = SakuraFMOReader.Read(ghost);
			if (fmoRecord == null)
			{
				Core.LogMessage.AddLog("編集中のゴーストにアクセスできません。SSPでゴーストを起動していますか？", Core.LogMessageType.Error);
				return; //送信できてない
			}

			var sstpBuilder = new ProtocolBuilder();
			sstpBuilder.Command = "NOTIFY SSTP/1.0";
			sstpBuilder.Parameters["Event"] = eventName;
			*/
			
		}

		//NOTIFY SSTPブロードキャスト
		public static void NotifySSTPBroadcast(string eventName, params string[] references)
		{
			var sstpBuilder = new ProtocolBuilder();
			sstpBuilder.Command = "NOTIFY SSTP/1.0";
			sstpBuilder.Parameters["Event"] = eventName;
			sstpBuilder.Parameters["Sender"] = "さとりすと";
			sstpBuilder.Parameters["Charset"] = "Shift_JIS";
			
			for(int i = 0; i < references.Length; i++)
			{
				sstpBuilder.Parameters.Add(string.Format("Reference{0}", i), references[i]);
			}
			RaiseSSTPBroadcast(sstpBuilder);
		}

		//里々を起動
		//さとりすと側のプロセスに依存せず別プロセスで里々を起動するのでさとりすとが64bitでも起動できるしくみ
		private static string ExecuteSatoriDLL(string satoriDirectory, string eventName)
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

		//SSPへの単純データ送信
		public static void RaiseSSTP(ProtocolBuilder data, SakuraFMORecord target)
		{
			var serializedData = data.Serialize();
			var dataBytes = Constants.EncodingShiftJis.GetBytes(serializedData);
			var dataPtr = Marshal.AllocHGlobal(dataBytes.Length);
			Marshal.Copy(dataBytes, 0, dataPtr, dataBytes.Length);

			var copydata = new Core.Win32Import.CopyDataStruct()
			{
				dwData = Core.Win32Import.SSTP_DWDATA,
				cbData = (uint)dataBytes.Length,
				lpData = dataPtr
			};
			var h = GCHandle.Alloc(copydata, GCHandleType.Pinned);

			//TODO: マジックナンバーフラグの定数化
			Core.Win32Import.SendMessageTimeoutA(target.HWnd, Core.Win32Import.WM_COPYDATA, IntPtr.Zero, h.AddrOfPinnedObject(), 2, 5000, IntPtr.Zero);

			Marshal.FreeHGlobal(dataPtr);
		}

		//SSTPブロードキャスト
		public static void RaiseSSTPBroadcast(ProtocolBuilder data)
		{
			var fmoReader = new SakuraFMOReader();
			fmoReader.Read();
			fmoReader.RemoveSurfacePreviewGeneratorRuntime();

			foreach(var item in fmoReader.Records)
			{
				RaiseSSTP(data, item.Value);
			}
		}

		//里々のバージョン取得
		public static string GetSatoriVersion(GhostModel ghost)
		{
			return ExecuteSatori(ghost, "（里々のバージョン）", EventType.Word);
		}
	}

	//ゴーストが見つかりません例外
	public class GhostNotFoundException : Exception
	{
		public void PrintErrorLog()
		{
			Core.LogMessage.AddLog("編集中のゴーストにアクセスできません。SSPでゴーストを起動していますか？", Core.LogMessageType.Error);
		}
	}
}
