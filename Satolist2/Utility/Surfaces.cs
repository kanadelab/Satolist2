using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Satolist2.Utility
{
	//surfaces.txt解析
	public class ShellAnalyzer
	{
		private const string SurfaceHeader = "surface";
		private const string SurfaceAppendHeader = "surface.append";
		private static readonly Regex ElementRegexPattern = new Regex("^element([0-9])+");
		private static readonly Regex AnimationRegexPattern = new Regex("^animation[0-9]+");
		private static readonly Regex SurfaceRecordRegexPattern = new Regex("^surface(\\.append)?([0-9,]+)");
		private const string DescriptUseSelfAplha = "seriko.use_self_alpha";

		private static readonly Regex SatolistPaletteOffsetRegiexPattern = new Regex("^//satolist.palette.offset,([0-9]+),([0-9]+)");
		private static readonly Regex SatolistPaletteVisibleRegexPattern = new Regex( "^//satolist.palette.visible,([0-9]+)");
		private static readonly Regex SatolistViewerVisibleRegiexPattern = new Regex( "^//satolist.viewer.visible,([0-9]+)");
		private static readonly Regex SatolistSurfaceVisibleRegexPattern = new Regex( "^//satolist.surface.visible,([0-9]+)");
		private static readonly Regex SatolistSurfaceDefaultRegexPattern = new Regex("^//satolist.surface.default,([0-9]+),([0-9]+)");

		private static readonly Regex BindGroupDefaultRegexPatterh = new Regex("^(sakura|kero)\\.bindgroup([0-9]+)\\.default");


		private Dictionary<long, SurfaceRecord> records;

		public string ShellDirectoryPath { get; private set; }
		public bool UseSelfAlpha { get; private set; }
		public int GlobalPaletteOffsetX { get; private set; }
		public int GlobalPaletteOffsetY { get; private set; }
		public bool GlobalPaletteVisible { get; private set; }
		public bool GlobalViewerVisible { get; private set; }

		public IEnumerable<long> SurfaceIDList
		{
			get => records.Keys.OrderBy(o=>o);
		}

		public ReadOnlyDictionary<long, SurfaceRecord> Records
		{
			get => new ReadOnlyDictionary<long, SurfaceRecord>(records);
		}

		public ShellAnalyzer()
		{
			records = new Dictionary<long, SurfaceRecord>();
			GlobalPaletteVisible = true;
			GlobalViewerVisible = true;
		}

		public void Load(string shellDirectory)
		{
			ShellDirectoryPath = Path.GetFullPath(shellDirectory);

			var files = Directory.GetFiles(shellDirectory, "*.png");
			foreach(var file in files)
			{
				var fileName = Path.GetFileName(file);
				var match = Regex.Match(Path.GetFileName(file), "^surface([0-9]+)\\.png$");
				if(match.Success)
				{
					//element0扱いでロードする
					var elementFromFile = new ElementRecord();
					elementFromFile.FileName = fileName;
					elementFromFile.RenderingMethod = RenderingMethod.Overlay;
					elementFromFile.ID = 0;

					var surface = new SurfaceRecord();
					surface.IsImageFileOnly = true;
					surface.Elements.Add(elementFromFile);
					var surfaceId = long.Parse(match.Groups[1].Value);

					if(!records.ContainsKey(surfaceId))
						records.Add(surfaceId, surface);
				}
			}

			LoadSurfaces(shellDirectory + "/surfaces.txt");

			//shell-descriptをロード
			CsvBuilder descriptReader = new CsvBuilder();
			descriptReader.DeserializeFromFile(shellDirectory + "/descript.txt");

			//seriko.use_self_alpha
			var useSelfAlpha = descriptReader.GetValue(DescriptUseSelfAplha);
			int useSelfAlphaValue;
			if(int.TryParse(useSelfAlpha, out useSelfAlphaValue))
				UseSelfAlpha = (useSelfAlphaValue != 0);

			//きせかえでデフォルト表示になっているモノは表示するようする
			foreach (var r in descriptReader.Records)
			{
				if (r.Key == null)
					continue;

				var match = BindGroupDefaultRegexPatterh.Match(r.Key);
				if (!match.Success)
					continue;

				int val;
				long animationId = 0;
				if (!int.TryParse(r.Value, out val) || !long.TryParse(match.Groups[2].Value, out animationId))
					val = 0;

				if (val != 0)
				{
					foreach (var surface in records)
					{
						var anim = surface.Value.Animations.FirstOrDefault(o => o.ID == animationId);
						if (anim != null && anim.Patterns.Count > 0)
						{
							//TODO: 複数intervalを持てるのを忘れていたので、Invalidになっていて解析出来てない場合はrunonceとかがついてると仮定して1パターン目だけ読む
							if (anim.Internval != AnimationInterval.Bind)
							{
								surface.Value.AddSatolistSurfaceDefault(animationId, anim.Patterns.First().ID, false);
							}
							else
							{
								foreach (var pat in anim.Patterns)
									surface.Value.AddSatolistSurfaceDefault(animationId, pat.ID, false);
							}
						}
					}
				}
			}


			//グローバル設定を書き込む
			foreach (var record in records)
			{
				if(!record.Value.UseSatolistPaletteOffset)
				{
					record.Value.SatolistPaletteOffsetX = GlobalPaletteOffsetX;
					record.Value.SatolistPaletteOffsetY = GlobalPaletteOffsetY;
				}
				if(!record.Value.UseSatolistPaletteVisible)
				{
					record.Value.SatolistPaletteVisible = GlobalPaletteVisible;
				}
				if(!record.Value.UseSatolistViewerVisible)
				{
					record.Value.SatolistViewerVisible = GlobalViewerVisible;
				}
			}
		}

		private void LoadSurfaces(string path)
		{
			var text = File.ReadAllText(path);
			text = text.Replace("\t", "");
			text = text.Replace(" ", "");

			var lines = text.Split(Constants.NewLineSeparator, StringSplitOptions.None);

			SurfaceRecord currentRecord = null;
			SurfaceRangeCollection currentRange = null;
			bool isAppend = false;

			for(int i = 0; i < lines.Length; i++)
			{
				var line = lines[i];
				var surfaceMatch = SurfaceRecordRegexPattern.Match(line);
				if (surfaceMatch.Success)
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
					currentRecord = new SurfaceRecord();
					continue;
				}
				

				//各設定をチェック
				if (ElementRegexPattern.IsMatch(line))
				{
					//element
					currentRecord.ParseElement(line);
				}
				else if (AnimationRegexPattern.IsMatch(line))
				{
					//animation
					currentRecord.ParseAnimation(line);
				}

				//さとりすと用設定
				var paletteOffsetMatch = SatolistPaletteOffsetRegiexPattern.Match(line);
				if(paletteOffsetMatch.Success)
				{
					int x = int.Parse(paletteOffsetMatch.Groups[1].Value);
					int y = int.Parse(paletteOffsetMatch.Groups[2].Value);
					
					if(currentRecord != null)
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
					continue;
				}

				//可視性
				var viewerVisibleMatch = SatolistViewerVisibleRegiexPattern.Match(line);
				if(viewerVisibleMatch.Success)
				{
					int visible = int.Parse(viewerVisibleMatch.Groups[1].Value);
					if (currentRecord != null)
					{
						currentRecord.SatolistViewerVisible = (visible != 0);
						currentRecord.UseSatolistViewerVisible = true;
					}
					else
						GlobalViewerVisible = (visible != 0);
					continue;
				}

				var paletteVisibleMatch = SatolistPaletteVisibleRegexPattern.Match(line);
				if(paletteVisibleMatch.Success)
				{
					int visible = int.Parse(paletteVisibleMatch.Groups[1].Value);
					if (currentRecord != null)
					{
						currentRecord.SatolistPaletteVisible = (visible != 0);
						currentRecord.UseSatolistPaletteVisible = true;
					}
					else
						GlobalPaletteVisible = (visible != 0);
					continue;
				}

				var surfaceVisibleMatch = SatolistSurfaceVisibleRegexPattern.Match(line);
				if(surfaceVisibleMatch.Success)
				{
					int visible = int.Parse(surfaceVisibleMatch.Groups[1].Value);
					if(currentRecord != null)
					{
						currentRecord.SatolistPaletteVisible = (visible != 0);
						currentRecord.SatolistViewerVisible = (visible != 0);
						currentRecord.UseSatolistPaletteVisible = true;
						currentRecord.UseSatolistViewerVisible = true;
					}
					else
					{
						currentRecord.SatolistPaletteVisible = (visible != 0);
						currentRecord.SatolistViewerVisible = (visible != 0);
					}
					continue;
				}

				if (currentRecord != null)
				{
					var surfaceDefaultMatch = SatolistSurfaceDefaultRegexPattern.Match(line);
					if (surfaceDefaultMatch.Success)
					{
						int animation = int.Parse(surfaceDefaultMatch.Groups[1].Value);
						int pattern = int.Parse(surfaceDefaultMatch.Groups[2].Value);
						currentRecord.AddSatolistSurfaceDefault(animation, pattern, true);
						continue;
					}
				}
			}

			//残っている情報をフラッシュして終了
			if (currentRange != null)
				MergeSurfaceRecord(currentRecord, currentRange, isAppend);
		}

		//宣言ブレスの情報と既存の情報とをマージする
		public void MergeSurfaceRecord(SurfaceRecord record, SurfaceRangeCollection range, bool isAppend)
		{
			//NOTE: 広範囲指定のappendレコードとかは真面目にループすると重いかも？
			foreach(var surfaceId in range.Range)
			{
				SurfaceRecord surface = null;
				records.TryGetValue(surfaceId, out surface);

				//レコードが存在しない場合新規作成
				if (surface == null)
				{
					if (isAppend)
						continue;
					surface = new SurfaceRecord();
					records.Add(surfaceId, surface);
				}

				//レコードを追加
				surface.Merge(record);
			}
		}

		public static string AnimationIntervalToString(AnimationInterval interval)
		{
			return AnimationIntervals.First(o => o.Value == interval).Key;
		}

		public static AnimationInterval StringToAnimationInterval(string interval)
		{
			return AnimationIntervals.FirstOrDefault(o => o.Key == interval).Value;
		}

		public static string RenderingMethodToString(RenderingMethod renderingMethod)
		{
			return RenderingMethods.First(o => o.Value == RenderingMethod.Replace).Key;
		}

		public static RenderingMethod StringToRenderingMethod(string renderingMethod)
		{
			return RenderingMethods.FirstOrDefault(o => o.Key == renderingMethod).Value;
		}

		public static readonly ReadOnlyDictionary<string, RenderingMethod> RenderingMethods = new ReadOnlyDictionary<string, RenderingMethod>
			(
				new Dictionary<string, RenderingMethod>()
				{
					{ "base", RenderingMethod.Base },
					{ "overlay", RenderingMethod.Overlay },
					{ "overlayfast", RenderingMethod.OverlayFast },
					{ "replace", RenderingMethod.Reduce },
					{ "interpolate", RenderingMethod.Interpolate },
					{ "asis", RenderingMethod.Asis },
					{ "move", RenderingMethod.Move },
					{ "bind", RenderingMethod.Bind },
					{ "add", RenderingMethod.Add },
					{ "reduce", RenderingMethod.Reduce },
					{ "insert", RenderingMethod.Insert },
					{ "start", RenderingMethod.Start },
					{ "stop", RenderingMethod.Stop },
					{ "alternativestart", RenderingMethod.AlternativeStart },
					{ "alternativestop", RenderingMethod.AlternativeStop },
					{ "parallelstart", RenderingMethod.ParallelStart },
					{ "parallelstop", RenderingMethod.ParallelStop }
				}
			);

		public static readonly ReadOnlyDictionary<string, AnimationInterval> AnimationIntervals = new ReadOnlyDictionary<string, AnimationInterval>
			(
				new Dictionary<string, AnimationInterval>()
				{
					{ "sometimes", AnimationInterval.Sometimes },
					{ "rarely", AnimationInterval.Rarely },
					{ "random", AnimationInterval.Random },
					{ "periodic", AnimationInterval.Periodic },
					{ "always", AnimationInterval.Always },
					{ "runonce", AnimationInterval.Runonce },
					{ "never", AnimationInterval.Never },
					{ "yen-e", AnimationInterval.YenE },
					{ "talk", AnimationInterval.Talk },
					{ "bind", AnimationInterval.Bind }
				}
			);
	}

	public enum AnimationInterval
	{
		Invalid,
		Sometimes,
		Rarely,
		Random,
		Periodic,
		Always,
		Runonce,
		Never,
		YenE,
		Talk,
		Bind
	}

	public enum RenderingMethod
	{
		Invalid,
		Base,
		Overlay,
		OverlayFast,
		Replace,
		Interpolate,
		Asis,
		Move,
		Bind,
		Add,
		Reduce,
		Insert,
		Start,
		Stop,
		AlternativeStart,
		AlternativeStop,
		ParallelStart,
		ParallelStop
	}

	public enum AnimationRecordType
	{
		Invalid,
		Interval,
		Pattern,
		Collision,
		Option
	}

	public class SurfaceDefaultRecord
	{
		public long AnimationID { get; set; }
		public long PatternID { get; set; }
	}


	//1surfaceあたりの情報
	public class SurfaceRecord
	{
		//public long ID { get; }
		public List<ElementRecord> Elements { get; }
		public List<AnimationRecord> Animations { get; }
		public List<SurfaceDefaultRecord> SatolistSurfaceDefault { get; set; }

		//画像ファイルからのみ存在するサーフェス
		public bool IsImageFileOnly { get; set; }
		public bool UseSatolistPaletteOffset { get; set; }
		public int SatolistPaletteOffsetX { get; set; }
		public int SatolistPaletteOffsetY { get; set; }

		public bool UseSatolistPaletteVisible { get; set; }
		public bool SatolistPaletteVisible { get; set; }
		public bool UseSatolistViewerVisible { get; set; }
		public bool SatolistViewerVisible { get; set; }

		public SurfaceRecord()
		{
			Elements = new List<ElementRecord>();
			Animations = new List<AnimationRecord>();
			SatolistSurfaceDefault = new List<SurfaceDefaultRecord>();
		}

		public void ParseElement(string line)
		{
			Elements.Add(new ElementRecord(line));
		}

		public void ParseAnimation(string line)
		{
			long animationId;
			AnimationRecordType recordType;
			if( AnimationRecord.ParseIDAndType(line, out animationId, out recordType) )
			{
				var record = AddAnimation(animationId);
				switch(recordType)
				{
					case AnimationRecordType.Pattern:
						record.ParsePattern(line);
						break;
					case AnimationRecordType.Interval:
						record.ParseInterval(line);
						break;
				}
			}
		}

		public AnimationRecord AddAnimation(long animationId)
		{
			//アニメーションがあるか探す
			var found = Animations.FirstOrDefault(o => o.ID == animationId);
			if (found != null)
				return found;
			var newRecord = new AnimationRecord();
			newRecord.ID = animationId;
			Animations.Add(newRecord);
			return newRecord;
		}

		public void AddSatolistSurfaceDefault(long animationId, long patternId, bool isOverwrite)
		{
			var found = SatolistSurfaceDefault.FirstOrDefault(o => o.PatternID == patternId && o.AnimationID == animationId);

			if (found == null || isOverwrite)
			{
				SatolistSurfaceDefault.Add(new SurfaceDefaultRecord()
				{
					AnimationID = animationId,
					PatternID = patternId
				}
				);
			}
		}

		public bool IsSatolistSurfaceDefault(long animationId, long patternId)
		{
			return SatolistSurfaceDefault.FirstOrDefault(o => o.AnimationID == animationId && o.PatternID == patternId) != null;
		}

		//別のサーフェスブレスの設定をとりこむ
		public void Merge(SurfaceRecord record)
		{
			foreach (var element in record.Elements)
			{
				//同じIDなら上書きにする(pngで設定されてるelementを上書きしたいので)
				//二重定義はそれはそれで検出しないといけないかも
				var found = Elements.FirstOrDefault(o => o.ID == element.ID);
				if (found != null)
					Elements.Remove(found);
				Elements.Add(element);
			}

			foreach(var animation in record.Animations)
			{
				//アニメーションも二重定義を検出しないといけない
				//あとアニメーションオーダーを考えないといけない…
				Animations.Add(animation);
			}

			if(record.UseSatolistPaletteOffset)
			{
				UseSatolistPaletteOffset = true;
				SatolistPaletteOffsetX = record.SatolistPaletteOffsetX;
				SatolistPaletteOffsetY = record.SatolistPaletteOffsetY;
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

			foreach(var item in record.SatolistSurfaceDefault)
			{
				SatolistSurfaceDefault.Add(item);
			}

			if (!record.IsImageFileOnly)
				IsImageFileOnly = false;
		}
	}

	public class ElementRecord
	{
		public long ID { get; set; }
		public RenderingMethod RenderingMethod { get; set; }
		public string FileName { get; set; }
		public int PositionX { get; set; }
		public int PositionY { get; set; }

		public string PnaFileName
		{
			get
			{
				return Path.GetDirectoryName(FileName) + "/" + Path.GetFileNameWithoutExtension(FileName) + ".pna";
			}
		}

		public ElementRecord()
		{
		}

		public ElementRecord(string parseLine)
		{
			Parse(parseLine);
		}

		public void Parse(string line)
		{
			var match = Regex.Match(line, "element([0-9]+),(.+),(.+),(.+),(.+)");
			ID = long.Parse(match.Groups[1].Value);
			RenderingMethod = ShellAnalyzer.StringToRenderingMethod(match.Groups[2].Value);
			FileName = match.Groups[3].Value;
			PositionX = int.Parse(match.Groups[4].Value);
			PositionY = int.Parse(match.Groups[5].Value);
		}
	}

	public class AnimationRecord
	{
		public long ID { get; set; }
		public AnimationInterval Internval { get; set; }
		public List<AnimationPatternRecord> Patterns { get; set; }

		public AnimationRecord()
		{
			Patterns = new List<AnimationPatternRecord>();
		}

		public static bool ParseIDAndType(string line, out long id, out AnimationRecordType type)
		{
			var match = Regex.Match(line, "^animation([0-9]+)\\.([a-zA-Z]+)[0-9]*,(.+)");

			if (!match.Success)
			{
				id = 0;
				type = AnimationRecordType.Invalid;
				return false;
			}

			id = long.Parse(match.Groups[1].Value);
			type = AnimationRecordType.Interval;
			switch(match.Groups[2].Value)
			{
				case "interval":
					type = AnimationRecordType.Interval;
					break;
				case "pattern":
					type = AnimationRecordType.Pattern;
					break;
				case "collision":
					type = AnimationRecordType.Collision;
					break;
				case "option":
					type = AnimationRecordType.Option;
					break;
			}

			return true;
		}

		public void ParseInterval(string line)
		{
			var match = Regex.Match(line, "animation[0-9]+\\.interval,(.+),?");
			Internval = ShellAnalyzer.StringToAnimationInterval(match.Groups[1].Value);
		}

		public void ParsePattern(string line)
		{
			//TODO: 単純合成でないものもあるので注意
			var match = Regex.Match(line, "animation[0-9]+\\.pattern([0-9]+),(.+),(.+),(.+),(.+),(.+)");
			if(!match.Success)
			{
				//start系、フォーマットが一致しないモノは現状無視
				return;
			}

			var pattern = new AnimationPatternRecord(
				long.Parse(match.Groups[1].Value),
				ShellAnalyzer.StringToRenderingMethod(match.Groups[2].Value),
				long.Parse(match.Groups[3].Value),
				int.Parse(match.Groups[5].Value),
				int.Parse(match.Groups[6].Value)
				);
			Patterns.Add(pattern);
		}
	}

	public class AnimationPatternRecord
	{
		public long ID { get; set; }
		public RenderingMethod RenderingMethod { get; set; }
		public long SurfaceID { get; set; }
		//public int Wait { get; set; }	//ウェイトはランダムがありえる。そもそも本システムでは必要にならないので読み飛ばす
		public int PositionX { get; set; }
		public int PositionY { get; set; }

		public AnimationPatternRecord(long id, RenderingMethod renderingMethod, long surfaceId/*, string wait*/, int x, int y)
		{
			ID = id;
			RenderingMethod = renderingMethod;
			SurfaceID = surfaceId;
			PositionX = x;
			PositionY = y;
		}
	}

	public class CollisionRecordBase
	{

	}

	//サーフェスブレスの範囲指定情報
	public class SurfaceRangeCollection
	{
		private List<SurfaceRange> rangeList;

		public SurfaceRangeCollection()
		{
			rangeList = new List<SurfaceRange>();
		}

		public static SurfaceRangeCollection MakeRange(string rangeString)
		{
			var result = new SurfaceRangeCollection();

			//まず単純に各レコードを生成する
			var addRecords = new List<SurfaceRange>();
			var ignoreRecords = new List<SurfaceRange>();

			var items = rangeString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var item in items)
			{
				var record = new SurfaceRange();
				var current = item;
				var targetList = addRecords;

				if (current[0] == '!')
				{
					//先頭が '!' ならレコードの除外指定
					current = current.Substring(1);
					targetList = ignoreRecords;
				}

				if (current.IndexOf('-') >= 0)
				{
					//範囲指定
					var range = current.Split(new char[] { '-' }, StringSplitOptions.None);
					record.Begin = long.Parse(range[0]);
					record.End = long.Parse(range[1]);

					//逆だったら入れ替える
					if (record.Begin > record.End)
					{
						var temp = record.Begin;
						record.Begin = record.End;
						record.End = temp;
					}
					targetList.Add(record);
				}
				else
				{
					//単発
					record.Begin = long.Parse(current);
					record.End = record.Begin;
					targetList.Add(record);
				}
			}

			//生成済みレコードを最適化する
			//ignoreレコードを取り除く、重複レコードを処理する、連番を範囲化する

			//無効レコードを後ろに移動する、開始位置順にソートする
			addRecords.Sort(new Comparison<SurfaceRange>(
				(o1, o2) =>
				{
					if (o1.Begin == o2.Begin)
						return 0;
					else if (o1.Begin < o2.Begin)
						return -1;
					else
						return 1;
				}
				)
			);

			ignoreRecords.Sort(new Comparison<SurfaceRange>(
				(o1, o2) =>
				{
					if (o1.Begin == o2.Begin)
						return 0;
					else if (o1.Begin < o2.Begin)
						return -1;
					else
						return 1;
				}
				)
			);

			var optimizedRecords = new List<SurfaceRange>();

			//レコード追加
			foreach (var range in addRecords)
			{
				result.AddRange(range);
			}

			//レコード削除
			foreach (var range in ignoreRecords)
			{
				result.RemoveRange(range);
			}
			return result;
		}

		private void RemoveRange(SurfaceRange range)
		{
			//被っているレコードを拾う
			var duplicatedBeginRecord = rangeList.FirstOrDefault(o => range.Begin >= o.Begin && range.Begin <= o.End);
			var duplicatedEndRecord = rangeList.FirstOrDefault(o => range.End >= o.Begin && range.End <= o.End);

			if(duplicatedBeginRecord == null && duplicatedEndRecord == null)
			{
				//指定されていない範囲か、広範囲を指定している
				//含まれるレコードをすべて除去
				var removeList = rangeList.Where(o => range.Begin <= o.Begin && range.End >= o.End).ToArray();
				foreach (var item in removeList)
					rangeList.Remove(item);
			}
			else if (duplicatedBeginRecord == duplicatedEndRecord)
			{
				//範囲が重複しているので分割する
				rangeList.Remove(duplicatedEndRecord);

				if (duplicatedBeginRecord.Begin != range.Begin)
				{
					var range1 = new SurfaceRange(duplicatedBeginRecord.Begin, range.Begin - 1);
					AddRange(range1);
				}
				if(duplicatedEndRecord.End != range.End)
				{
					var range2 = new SurfaceRange(range.End + 1, duplicatedEndRecord.End);
					AddRange(range2);
				}
			}
			
			if(duplicatedBeginRecord != null)
			{
				duplicatedBeginRecord.End = range.Begin;
			}

			if(duplicatedEndRecord != null)
			{
				duplicatedEndRecord.Begin = range.End;
			}
		}

		private void AddRange(SurfaceRange range)
		{
			//開始と終了がそれぞれ被っていたら結合する
			var duplicatedBeginRecord = rangeList.FirstOrDefault(o => range.Begin >= o.Begin && range.Begin <= o.End);
			var duplicatedEndRecord = rangeList.FirstOrDefault(o => range.End >= o.Begin && range.End <= o.End);

			if (duplicatedBeginRecord == null && duplicatedEndRecord == null)
			{
				rangeList.Add(range);
			}
			else if (duplicatedBeginRecord != null && duplicatedEndRecord == null)
			{
				//開始位置が被っているレコードがあった
				if (duplicatedBeginRecord.End < range.End)
					duplicatedBeginRecord.End = range.End;
			}
			else if (duplicatedEndRecord != null && duplicatedBeginRecord == null)
			{
				if (duplicatedEndRecord.Begin > range.Begin)
					duplicatedEndRecord.Begin = range.Begin;
			}
			else if (duplicatedBeginRecord != duplicatedEndRecord)
			{
				//２レコードを接続する
				duplicatedBeginRecord.End = duplicatedEndRecord.End;
				rangeList.Remove(duplicatedEndRecord);
			}
		}

		//ふくまれるサーフェス範囲をすべて返す
		public IEnumerable<long> Range
		{
			get
			{
				foreach(var r in rangeList)
				{
					foreach (var rr in r.Surfaces)
						yield return rr;
				}
			}
		}

		//文字列化
		public override string ToString()
		{
			return string.Join(",", rangeList.Select(o => o.ToString()));
		}
	}

	//surfaceID範囲
	public class SurfaceRange
	{
		public long Begin { get; set; }
		public long End { get; set; }

		public SurfaceRange(long id)
		{
			Begin = id;
			End = id;
		}

		public SurfaceRange(long begin, long end)
		{
			Begin = begin;
			End = end;
		}

		public SurfaceRange()
		{ }

		
		//appendの場合、一旦ソートしたほうがいいのかも？
		public IEnumerable<long> Surfaces
		{
			get
			{
				for (long i = Begin; i <= End; i++)
					yield return i;
			}
		}

		public override string ToString()
		{
			return string.Format("{0}-{1}", Begin, End);
		}
	}

	//画像データ
	public class ShellImageSource
	{
		public bool LoadFailed { get; set; }
		public Bitmap Image { get; private set; }
		
		public ShellImageSource(string path)
		{
			LoadFailed = false;
			try
			{
				//Bitmapの引数でロードするとロックしちゃうので、一旦byteに展開して読む
				byte[] bitmapBytes = System.IO.File.ReadAllBytes(path);
				var stream = new MemoryStream(bitmapBytes);
				Image = new Bitmap(stream, false);
			}
			catch
			{
				LoadFailed = true;
			}
		}
	}

	//画像データキャッシュ
	public class ShellImageCache
	{
		private string directoryPath;
		private Dictionary<string, ShellImageSource> images;
		public ShellImageCache(string shellDirectoryPath)
		{
			images = new Dictionary<string, ShellImageSource>();
			directoryPath = shellDirectoryPath;
		}
		public ShellImageSource LoadImage(string filePath)
		{
			ShellImageSource result;
			images.TryGetValue(filePath, out result);
			if(result == null)
			{
				result = new ShellImageSource(directoryPath + "/" + filePath);
				images.Add(filePath, result);
			}
			return result;
		}
	}

	//シェル描画周り
	public class SurfaceRenderer
	{
		private Bitmap renderTarget;

		public Bitmap Image
		{
			get => renderTarget;
		}

		public SurfaceRenderer()
		{
		}

		public void Rendering(ShellAnalyzer shell, long id, ShellImageCache cache, int sizeX = 0, int sizeY = 0, int offsetX = 0,  int offsetY = 0)
		{
			var surface = shell.Records[id];

			//elementがなければカラということにしておく(animationのみで形成されるサーフェスもある？)
			if(surface.Elements.Count == 0)
			{
				renderTarget = new Bitmap(1, 1);
				return;
			}

			//elementを描画する
			renderTarget = RenderingElements(surface, shell, cache, sizeX, sizeY, offsetX, offsetY);

			//animationを描画する 描画登録されてるものだけ
			foreach(var animation in surface.Animations)
			{
				foreach(var pattern in animation.Patterns)
				{
					if(surface.IsSatolistSurfaceDefault(animation.ID, pattern.ID) && shell.Records.ContainsKey(pattern.SurfaceID))
					{
						//サーフェスに所属するelementをビルド
						//TODO: ここはキャッシュしたい
						var patternSurface = shell.Records[pattern.SurfaceID];
						var surfaceElements = RenderingElements(patternSurface, shell, cache);

						RenderingLayer(surfaceElements, null, renderTarget, pattern.PositionX - offsetX, pattern.PositionY - offsetY, pattern.RenderingMethod);
					}
				}
			}
		}

		//surfaceのelementを１枚にまとめる
		public Bitmap RenderingElements(SurfaceRecord surface, ShellAnalyzer shell, ShellImageCache cache, int sizeX = 0, int sizeY = 0, int offsetX = 0, int offsetY = 0)
		{
			//描画先
			var firstElement = surface.Elements.First();

			//最初のelementが基準
			var firstElementImage = cache.LoadImage(firstElement.FileName);
			if (sizeX == 0 || sizeY == 0)
			{
				sizeX = firstElementImage.Image.Width;
				sizeY = firstElementImage.Image.Height;
			}

			Bitmap target = new Bitmap(sizeX, sizeY);

			foreach (var element in surface.Elements)
			{
				ShellImageSource pnaImage = null;
				if (shell.UseSelfAlpha)
					pnaImage = cache.LoadImage(element.PnaFileName);

				var image = cache.LoadImage(element.FileName);
				if (!image.LoadFailed)
					RenderingLayer(image.Image, pnaImage?.Image, target, element.PositionX - offsetX, element.PositionY - offsetY, element.RenderingMethod);
			}

			return target;
		}

		public void RenderingLayer(Bitmap source, Bitmap sourcePna, Bitmap dest, int targetX, int targetY, RenderingMethod renderingMethod)
		{
			//使用不可能な合成メソッドはなにもせず終了
			switch(renderingMethod)
			{
				case RenderingMethod.Base:
				case RenderingMethod.Add:
				case RenderingMethod.Overlay:
				case RenderingMethod.Bind:
				case RenderingMethod.Asis:
				case RenderingMethod.Interpolate:
				case RenderingMethod.OverlayFast:
				case RenderingMethod.Reduce:
				case RenderingMethod.Replace:
					break;
				default:
					return;
			}

			//element0は全部描画するので
			var targetBits = dest.LockBits(new Rectangle(0, 0, dest.Width, dest.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
			var sourceBits = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			var sourcePnaBits = sourcePna?.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

			//透過色
			FloatColor leftTopColor = GetPixel(sourceBits, 0, 0);

			//ピクセルごとの合成
			for (int x = 0; x < sourceBits.Width; x++)
			{
				for(int y = 0; y < sourceBits.Height; y++)
				{
					int destX = targetX + x;
					int destY = targetY + y;

					//範囲外ならパス(ループ条件側で対応したほうがよさそう)
					if (destX < 0 || destY < 0 || destX >= targetBits.Width || destY >= targetBits.Height)
						continue;

					//ビットマップを合成
					var sourcePixel = GetPixel(sourceBits, x, y);
					if (sourcePna != null)
						sourcePixel.A = GetPixel(sourcePnaBits, x, y).R;

					//左上カラーと同一の場合discardする
					if (leftTopColor.Equals(sourcePixel))
						continue;

					var destPixel = GetPixel(targetBits, destX, destY);
					var resultPixel = Blend(destPixel, sourcePixel, renderingMethod);
			
					SetPixel(resultPixel, targetBits, destX, destY);
				}
			}

			//unlock
			dest.UnlockBits(targetBits);
			source.UnlockBits(sourceBits);

			//dest.Save(@"D:\data\Satolist2\test.png", ImageFormat.Png);
		}

		public static FloatColor GetPixel(BitmapData bitmapData, int x, int y)
		{
			if (x < 0 || y < 0 || x >= bitmapData.Width || y >= bitmapData.Height)
				return FloatColor.FromArgb(0);
			return FloatColor.FromArgb(Marshal.ReadInt32(bitmapData.Scan0, bitmapData.Stride * y + x * sizeof(uint)));
		}

		public static void SetPixel(FloatColor color, BitmapData bitmapData, int x, int y)
		{
			if (x < 0 || y < 0 || x >= bitmapData.Width || y >= bitmapData.Height)
				return;

			Marshal.WriteInt32(
				bitmapData.Scan0,
				bitmapData.Stride * y + x * sizeof(uint),
				color.ToArgb()
				);
		}

		//ブレンドモードに従う各種ブレンド
		//baseのようなブレンドとは異なる動きをするものは対応してないのでここ以前で処理を行う必要がある
		public static FloatColor Blend(FloatColor destColor, FloatColor sourceColor, RenderingMethod renderingMethod)
		{
			var resultColor = new FloatColor();

			//各ブレンドを実装
			switch(renderingMethod)
			{
				case RenderingMethod.Add:
				case RenderingMethod.Overlay:
				case RenderingMethod.Bind:
				case RenderingMethod.Base:
					//普通のアルファブレンド
					resultColor = AlphaBlend(destColor, sourceColor);
					break;
				case RenderingMethod.Reduce:
					resultColor = destColor;
					resultColor.A = sourceColor.A;
					break;
				case RenderingMethod.OverlayFast:
					//ベースが濃いほど濃く合成。source側のalphaにdest側のalphaにかける
					sourceColor.A = sourceColor.A * destColor.A;
					resultColor = AlphaBlend(sourceColor, sourceColor);
					break;
				case RenderingMethod.Interpolate:
					//ベースが薄いほど濃く合成。source側のalphaにdest側の1.0-alphaをかける
					sourceColor.A = sourceColor.A * (1.0f - destColor.A);
					resultColor = AlphaBlend(sourceColor, destColor);
					break;
				case RenderingMethod.Replace:
					resultColor = sourceColor;
					break;
				case RenderingMethod.Asis:
					sourceColor.A = 1.0f;
					resultColor = AlphaBlend(sourceColor, destColor);
					break;
				default:
					throw new NotImplementedException();

			}
			return resultColor;
		}

		//アルファ合成
		public static FloatColor AlphaBlend(FloatColor destColor, FloatColor sourceColor)
		{
			var resultColor = new FloatColor();
			resultColor.A = sourceColor.A + destColor.A * (1.0f - sourceColor.A);
			if (resultColor.A == 0.0f)
			{
				//ゼロ除算を回避するためにalphaがゼロの場合は全部ゼロにしてしまう
				resultColor.R = 0.0f;
				resultColor.G = 0.0f;
				resultColor.B = 0.0f;
			}
			else
			{
				resultColor.R = (sourceColor.R * sourceColor.A + destColor.R * destColor.A * (1.0f - sourceColor.A)) / resultColor.A;
				resultColor.G = (sourceColor.G * sourceColor.A + destColor.G * destColor.A * (1.0f - sourceColor.A)) / resultColor.A;
				resultColor.B = (sourceColor.B * sourceColor.A + destColor.B * destColor.A * (1.0f - sourceColor.A)) / resultColor.A;
			}
			return resultColor;
		}
	}

	//乗算計算用カラー
	public struct FloatColor
	{
		public float A { get; set; }
		public float R { get; set; }
		public float G { get; set; }
		public float B { get; set; }

		public static FloatColor FromArgb(int argb)
		{
			var byteColor = Color.FromArgb(argb);
			var result = new FloatColor();
			result.R = byteColor.R / 255.0f;
			result.G = byteColor.G / 255.0f;
			result.B = byteColor.B / 255.0f;
			result.A = byteColor.A / 255.0f;
			return result;
		}

		public int ToArgb()
		{
			var byteColor = Color.FromArgb((int)(Clamp(A) * 255.0f), (int)(Clamp(R) * 255.0f), (int)(Clamp(G) * 255.0f), (int)(Clamp(B) * 255.0f));
			return byteColor.ToArgb();
		}

		public bool Equals(FloatColor data)
		{
			return A == data.A && R == data.R && G == data.G && B == data.B;
		}

		private static float Clamp(float val)
		{
			return (float)Math.Min(1.0, Math.Max(0.0, val));
		}

	}
}
