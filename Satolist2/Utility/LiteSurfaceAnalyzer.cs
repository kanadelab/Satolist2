using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Satolist2.Utility
{
	//ShellAnalyzer より単純な解析のみを行うもの。
	//さとりすとでサーフェスのレンダリングを行わないようになったため。
	public class LiteSurfaceAnalyzer
	{
		private static readonly Regex SurfaceRecordRegexPattern = new Regex("^surface(\\.append)?([0-9,-]+)");
		private static readonly Regex SatolistPaletteOffsetRegiexPattern = new Regex("^//satolist.palette.offset,([0-9]+),([0-9]+)");
		private static readonly Regex SatolistPaletteExpandRegiexPattern = new Regex("^//satolist.palette.expand,([0-9\\.]+)");
		private static readonly Regex SatolistPaletteVisibleRegexPattern = new Regex("^//satolist.palette.visible,([0-9]+)");
		private static readonly Regex SatolistViewerVisibleRegiexPattern = new Regex("^//satolist.viewer.visible,([0-9]+)");
		private static readonly Regex SatolistSurfaceVisibleRegexPattern = new Regex("^//satolist.surface.visible,([0-9]+)");
		private static readonly Regex SatolistScopeVisibleRegexPattern = new Regex("^//satolist.surface.scope,([0-9]+)");
		private static readonly Regex SurfaceFileRegexPattern = new Regex("^surface([0-9]+)\\.png$");

		private Dictionary<long, LiteSurfaceRecord> records;
		public string ShellDirectoryPath { get; private set; }
		public int GlobalPaletteOffsetX { get; private set; }
		public int GlobalPaletteOffsetY { get; private set; }
		public double GlobalPaletteExpand { get; private set; }
		public bool GlobalPaletteVisible { get; private set; }
		public bool GlobalViewerVisible { get; private set; }

		//すべてのレコード
		public ReadOnlyDictionary<long, LiteSurfaceRecord> Records
		{
			get => new ReadOnlyDictionary<long, LiteSurfaceRecord>(records);
		}

		//存在しているサーフェスIDの一覧
		public IEnumerable<long> SurfaceIDList
		{
			get => records.Keys.OrderBy(o => o);
		}

		public LiteSurfaceAnalyzer()
		{
			records = new Dictionary<long, LiteSurfaceRecord>();
			GlobalPaletteExpand = 1.0;
		}

		public void Load(string shellDirectory)
		{
			ShellDirectoryPath = DictionaryUtility.NormalizeFullPath(shellDirectory);
			GlobalPaletteVisible = true;
			GlobalViewerVisible = true;

			//ファイルとして存在するサーフェスを列挙
			var files = Directory.GetFiles(shellDirectory, "*.png");
			foreach(var file in files)
			{
				var fileName = Path.GetFileName(file);
				var match = SurfaceFileRegexPattern.Match(fileName);
				if(match.Success)
				{
					var surface = new LiteSurfaceRecord();
					surface.IsImageFileOnly = true;
					var surfaceId = long.Parse(match.Groups[1].Value);

					if (!records.ContainsKey(surfaceId))
						records.Add(surfaceId, surface);
				}
			}

			//surfaces.txtをロード
			var surfaceFiles = Directory.GetFiles(shellDirectory, "surfaces*.txt");
			foreach (var f in surfaceFiles)
			{
				var filename = Path.GetFileName(f);
				LoadSurfaces(DictionaryUtility.ConbinePath(shellDirectory, filename));
			}

			//グローバル設定を書き込む
			foreach (var record in records)
			{
				if (!record.Value.UseSatolistPaletteOffset)
				{
					record.Value.SatolistPaletteOffsetX = GlobalPaletteOffsetX;
					record.Value.SatolistPaletteOffsetY = GlobalPaletteOffsetY;
				}
				if (!record.Value.UseSatolistPaletteVisible)
				{
					record.Value.SatolistPaletteVisible = GlobalPaletteVisible;
				}
				if (!record.Value.UseSatolistViewerVisible)
				{
					record.Value.SatolistViewerVisible = GlobalViewerVisible;
				}
				if(!record.Value.UseSatolistPaletteExpand)
				{
					record.Value.SatolistPaletteExpand = GlobalPaletteExpand;
				}
			}

			//surfacetable.txtをロード
			var surfaceTablePath = DictionaryUtility.ConbinePath(shellDirectory, "surfacetable.txt");
			if(File.Exists(surfaceTablePath))
			{
				LoadSurfaceTable(surfaceTablePath);
			}
		}

		//１つのsurfaces.txtをロード
		private void LoadSurfaces(string path)
		{
			var text = File.ReadAllText(path);
			text = text.Replace("\t", "");
			text = text.Replace(" ", "");

			var lines = DictionaryUtility.SplitLines(text);
			LiteSurfaceRecord currentRecord = null;
			SurfaceRangeCollection currentRange = null;
			bool isAppend = false;

			for(int i = 0; i < lines.Length; i++)
			{
				var line = lines[i];
				var surfaceMatch = SurfaceRecordRegexPattern.Match(line);
				if(surfaceMatch.Success)
				{
					//別のサーフェスブレスなのでマージ
					if(currentRange != null)
					{
						MergeSurfaceRecord(currentRecord, currentRange, isAppend);
					}

					//surface
					isAppend = false;
					if (!string.IsNullOrEmpty(surfaceMatch.Groups[1].Value))
						isAppend = true;
					currentRange = SurfaceRangeCollection.MakeRange(surfaceMatch.Groups[2].Value);
					currentRecord = new LiteSurfaceRecord();
					continue;
				}

				//各種設定をチェック
				var paletteOffsetMatch = SatolistPaletteOffsetRegiexPattern.Match(line);
				if (paletteOffsetMatch.Success)
				{
					int x, y;
					if (
						int.TryParse(paletteOffsetMatch.Groups[1].Value, out x) &&
						int.TryParse(paletteOffsetMatch.Groups[2].Value, out y)
						)
					{
						if (currentRecord != null)
						{
							currentRecord.SatolistPaletteOffsetX = x;
							currentRecord.SatolistPaletteOffsetY = y;
							currentRecord.UseSatolistPaletteOffset = true;
						}
						else
						{
							GlobalPaletteOffsetX = x;
							GlobalPaletteOffsetY = y;
						}
					}
					continue;
				}

				//拡大
				var paletteExpandMatch = SatolistPaletteExpandRegiexPattern.Match(line);
				if(paletteExpandMatch.Success)
				{
					double ex;
					if(
						double.TryParse(paletteExpandMatch.Groups[1].Value, out ex)
						)
					{
						if (currentRecord != null)
						{
							currentRecord.SatolistPaletteExpand = ex;
							currentRecord.UseSatolistPaletteExpand = true;
						}
						else
						{
							GlobalPaletteExpand = ex;
						}
					}
				}

				//可視性
				var viewerVisibleMatch = SatolistViewerVisibleRegiexPattern.Match(line);
				if (viewerVisibleMatch.Success)
				{
					int visible;
					if (int.TryParse(viewerVisibleMatch.Groups[1].Value, out visible))
					{
						if (currentRecord != null)
						{
							currentRecord.SatolistViewerVisible = (visible != 0);
							currentRecord.UseSatolistViewerVisible = true;
						}
						else
							GlobalViewerVisible = (visible != 0);
					}
					continue;
				}

				var paletteVisibleMatch = SatolistPaletteVisibleRegexPattern.Match(line);
				if (paletteVisibleMatch.Success)
				{
					int visible;
					if (int.TryParse(paletteVisibleMatch.Groups[1].Value, out visible))
					{
						if (currentRecord != null)
						{
							currentRecord.SatolistPaletteVisible = (visible != 0);
							currentRecord.UseSatolistPaletteVisible = true;
						}
						else
							GlobalPaletteVisible = (visible != 0);
					}
					continue;
				}

				var surfaceVisibleMatch = SatolistSurfaceVisibleRegexPattern.Match(line);
				if (surfaceVisibleMatch.Success)
				{
					int visible;
					if (int.TryParse(surfaceVisibleMatch.Groups[1].Value, out visible))
					{
						if (currentRecord != null)
						{
							currentRecord.SatolistPaletteVisible = (visible != 0);
							currentRecord.SatolistViewerVisible = (visible != 0);
							currentRecord.UseSatolistPaletteVisible = true;
							currentRecord.UseSatolistViewerVisible = true;
						}
						else
						{
							GlobalPaletteVisible = (visible != 0);
							GlobalViewerVisible = (visible != 0);
						}
					}
					continue;
				}

				var surfaceScopeMatch = SatolistScopeVisibleRegexPattern.Match(line);
				if(surfaceScopeMatch.Success)
				{
					int scope;
					if (int.TryParse(surfaceScopeMatch.Groups[1].Value, out scope))
					{
						if (currentRecord != null)
						{
							currentRecord.SatolistScope = scope;
							currentRecord.UseSatolistScope = true;
						}
					}
				}
			}

			//残っている情報をフラッシュして終了
			if (currentRange != null)
				MergeSurfaceRecord(currentRecord, currentRange, isAppend);

		}

		public void MergeSurfaceRecord(LiteSurfaceRecord record, SurfaceRangeCollection range, bool isAppend)
		{
			foreach(var surfaceId in range.Range)
			{
				LiteSurfaceRecord surface;
				records.TryGetValue(surfaceId, out surface);

				//レコードが存在しない場合新規作成
				if(surface == null)
				{
					if (isAppend) continue;
					surface = new LiteSurfaceRecord();
					records.Add(surfaceId, surface);
				}

				//レコードを結合
				surface.Merge(record);
			}
		}

		//surfacetable.txtのロード
		public void LoadSurfaceTable(string path)
		{
			var text = File.ReadAllText(path, Constants.EncodingShiftJis);
			text = text.Replace("\t", "");
			text = text.Replace(" ", "");

			var csvBuilder = new CsvBuilder();
			csvBuilder.Deserialize(text);

			//簡易的解析で終了
			foreach(var item in csvBuilder.Records)
			{
				long surfaceId;
				if(long.TryParse(item.Key, out surfaceId) && !string.IsNullOrEmpty(item.Value))
				{
					if(records.ContainsKey(surfaceId))
					{
						records[surfaceId].SurfaceTableLabel = item.Value;
					}
				}
			}
		}
	}

	public class LiteSurfaceRecord
	{
		public bool IsImageFileOnly { get; set; }
		public bool UseSatolistPaletteOffset { get; set; }
		public int SatolistPaletteOffsetX { get; set; }
		public int SatolistPaletteOffsetY { get; set; }
		public bool UseSatolistPaletteExpand { get; set; }
		public double SatolistPaletteExpand { get; set; }
		public bool UseSatolistPaletteVisible { get; set; }
		public bool SatolistPaletteVisible { get; set; }
		public bool UseSatolistViewerVisible { get; set; }
		public bool SatolistViewerVisible { get; set; }
		public bool UseSatolistScope { get; set; }
		public int SatolistScope { get; set; }
		public string SurfaceTableLabel { get; set; }

		public void Merge(LiteSurfaceRecord record)
		{
			if(record.UseSatolistPaletteOffset)
			{
				UseSatolistPaletteOffset = true;
				SatolistPaletteOffsetX = record.SatolistPaletteOffsetX;
				SatolistPaletteOffsetY = record.SatolistPaletteOffsetY;
			}

			if(record.UseSatolistPaletteExpand)
			{
				UseSatolistPaletteExpand = true;
				SatolistPaletteExpand = record.SatolistPaletteExpand;
			}

			if(record.UseSatolistPaletteVisible)
			{
				UseSatolistPaletteVisible = true;
				SatolistPaletteVisible = record.SatolistPaletteVisible;
			}

			if(record.UseSatolistViewerVisible)
			{
				UseSatolistViewerVisible = true;
				SatolistViewerVisible = record.SatolistViewerVisible;
			}

			if(record.UseSatolistScope)
			{
				UseSatolistScope = true;
				SatolistScope = record.SatolistScope;
			}

			if (!record.IsImageFileOnly)
				IsImageFileOnly = false;
		}
	}
}
