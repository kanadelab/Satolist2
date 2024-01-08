﻿using Satolist2.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Satolist2.Utility
{
	public class SakuraFMOReader
	{
		private const int MutexTimeoutMs = 1000;
		public const string DefaultFMOName = "Sakura";
		//private const string MutexName = "SakuraFMO";
		//private const string FMOName = "Sakura";

		private const string SSPFMOHeader = "ssp_fmo_header_";
		private Dictionary<string, SakuraFMORecord> records;

		public string FMOName { get; }
		//public string MutexName => FMOName + "FMO";

		public ReadOnlyDictionary<string, SakuraFMORecord> Records
		{
			get => new ReadOnlyDictionary<string, SakuraFMORecord>(records);
		}

		public SakuraFMOReader(string fmoName = DefaultFMOName)
		{
			FMOName = fmoName;
			records = new Dictionary<string, SakuraFMORecord>();
		}

		public static void DumpToFile(string fileName, string fmoName = DefaultFMOName)
		{
			using (var mutex = new Mutex(false, MakeMutexName(DefaultFMOName)))
			{
				if (mutex.WaitOne(MutexTimeoutMs))
				{
					try
					{
						using (var fmo = MemoryMappedFile.OpenExisting(fmoName))
						{
							using (var fmoStream = fmo.CreateViewStream())
							{
								using(var reader = new BinaryReader(fmoStream))
								{
									byte[] data = new byte[fmoStream.Length];
									reader.Read(data, 0, (int)fmoStream.Length);

									File.WriteAllBytes(fileName, data);
								}
							}
						}
					}
					catch(FileNotFoundException)
					{
						//fmoうまく読めなかった…
						throw;
					}
					finally
					{
						mutex.ReleaseMutex();
					}
				}
			}
		}

		private static string MakeMutexName(string fmoName)
		{
			return fmoName + "FMO";
		}

		//場合によって同期待ちするので注意
		public void Read()
		{
			records.Clear();
			var lines = new List<string>();

			using (var mutex = new Mutex(false, MakeMutexName(FMOName)))
			{
				if (mutex.WaitOne(MutexTimeoutMs))
				{
					try
					{
						using (var fmo = MemoryMappedFile.OpenExisting(FMOName))
						{
							using (var fmoStream = fmo.CreateViewStream())
							{
								using (var reader = new StreamReader(fmoStream, Constants.EncodingShiftJis))
								{
									while (true)
									{
										var line = reader.ReadLine();
										if (line != null)
											lines.Add(line);
										else
											break;
									}
								}
							}
						}
					}
					catch
					{
						//fmoうまく読めなかった…
					}
					finally
					{
						mutex.ReleaseMutex();
					}
				}
			}

			//ここまででFMOを読めたので、解析する
			//http://ssp.shillest.net/docs/fmo.html
			foreach (var line in lines)
			{
				int headerPos = line.IndexOf(SSPFMOHeader);
				if (headerPos < 0)
					continue;

				var rawData = line.Substring(headerPos);

				//fmoの識別子の後データ本体とは . で区切られている
				var data = rawData.Split(new char[] { '.' }, 2, StringSplitOptions.None);

				var fmoId = data[0];		//ゴーストごとの識別子
				var dataBody = data[1];

				//データのkey,valueはバイト値1で区切られている
				var dataKeyValue = dataBody.Split(new char[] { (char)1 }, 2, StringSplitOptions.None);
				var key = dataKeyValue[0];
				var value = dataKeyValue[1];

				//データを追加
				if (!records.ContainsKey(fmoId))
					records.Add(fmoId, new SakuraFMORecord(fmoId));
				records[fmoId].Parse(key, value);
			}

			//基準を満たしていない情報を削除
			foreach(var item in records.ToArray())
			{
				if (string.IsNullOrEmpty(item.Value.GhostPath) ||
					string.IsNullOrEmpty(item.Value.GhostName) ||
					string.IsNullOrEmpty(item.Value.ExecutablePath) ||
					Core.Win32Import.IsWindow(item.Value.HWnd) == Core.Win32Import.FALSE
					)
					records.Remove(item.Key);
			}
		}

		public SakuraFMORecord Find(GhostModel ghost)
		{
			//末尾を最新として取り扱う
			return records.LastOrDefault(o => o.Value.GhostPath == ghost.FullPath).Value;
		}

		public SakuraFMORecord Find(GhostModel ghost, string executablePath)
		{
			return records.LastOrDefault(o =>
				o.Value.GhostPath == ghost.FullPath &&
				o.Value.ExecutablePath == executablePath
				).Value;
		}

		//サーフェスプレビュー作成に使用するSSPがあれば除去
		public void RemoveSurfacePreviewGeneratorRuntime()
		{
			foreach(var r in Records.ToArray())
			{
				if (r.Value.GhostName == "サーフェスプレビューの準備中")//descript.txtで設定してある
					records.Remove(r.Key);
			}
		}

		//ヘルパ
		public static SakuraFMORecord Read(GhostModel ghost, string fmoName = DefaultFMOName)
		{
			var reader = new SakuraFMOReader(fmoName);
			reader.Read();
			return reader.Find(ghost);
		}

		public static SakuraFMORecord Read(GhostModel ghost, string executablePath, string fmoName = DefaultFMOName)
		{
			var reader = new SakuraFMOReader(fmoName);
			reader.Read();
			return reader.Find(ghost, executablePath);
		}

		public static bool Exists(GhostModel ghost)
		{
			return Read(ghost) != null;
		}

		public static bool Exists(GhostModel ghost, string executablePath)
		{
			return Read(ghost, executablePath) != null;
		}
	}

	public class SakuraFMORecord
	{
		public string ID { get; private set; }
		public string SakuraName { get; private set; }
		public string KeroName { get; private set; }
		public string GhostName { get; private set; }
		public string GhostPath { get; private set; }
		public string ExecutablePath { get; private set; }
		public IntPtr HWnd { get; private set; }
		public IntPtr[] HWndList { get; private set; }
		

		public SakuraFMORecord(string id)
		{
			ID = id;
			HWndList = Array.Empty<IntPtr>();
		}

		public void Parse(string key, string value)
		{
			switch(key)
			{
				case "hwnd":
					HWnd = (IntPtr)ulong.Parse(value);
					break;
				case "name":
					SakuraName = value;
					break;
				case "ghostpath":
					GhostPath = DictionaryUtility.RemoveLastSlash(DictionaryUtility.NormalizeFullPath(value));
					break;
				case "keroname":
					KeroName = value;
					break;
				case "fullname":
					GhostName = value;
					break;
				case "path":
					ExecutablePath = DictionaryUtility.ConbinePath(value, "ssp.exe");
					break;
				case "hwndlist":
					HWndList = value.Split(Constants.CommaSeparator, StringSplitOptions.RemoveEmptyEntries).Select(o => (IntPtr)ulong.Parse(o)).ToArray();
					break;
			}
		}
	}

}
