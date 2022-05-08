using Satolist2.Model;
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
		private const string MutexName = "SakuraFMO";
		private const string FMOName = "Sakura";
		private const string SSPFMOHeader = "ssp_fmo_header_";
		private Dictionary<string, SakuraFMORecord> records;

		public ReadOnlyDictionary<string, SakuraFMORecord> Records
		{
			get => new ReadOnlyDictionary<string, SakuraFMORecord>(records);
		}

		public SakuraFMOReader()
		{
			records = new Dictionary<string, SakuraFMORecord>();
		}

		//場合によって同期待ちするので注意
		public void Read()
		{
			records.Clear();
			var lines = new List<string>();

			using (var mutex = new Mutex(false, MutexName))
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
		}

		public SakuraFMORecord Find(GhostModel ghost)
		{
			return records.FirstOrDefault(o => o.Value.GhostPath == ghost.FullPath).Value;
		}

		public SakuraFMORecord Find(GhostModel ghost, string executablePath)
		{
			return records.FirstOrDefault(o =>
				o.Value.GhostPath == ghost.FullPath &&
				o.Value.ExecutablePath == executablePath
				).Value;
		}

		//ヘルパ
		public static SakuraFMORecord Read(GhostModel ghost)
		{
			var reader = new SakuraFMOReader();
			reader.Read();
			return reader.Find(ghost);
		}

		public static SakuraFMORecord Read(GhostModel ghost, string executablePath)
		{
			var reader = new SakuraFMOReader();
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
		

		public SakuraFMORecord(string id)
		{
			ID = id;
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
			}
		}
	}

}
