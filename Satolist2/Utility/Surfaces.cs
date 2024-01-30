using Satolist2.Model;
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
				if (System.IO.File.Exists(path))
				{
					//Bitmapの引数でロードするとロックしちゃうので、一旦byteに展開して読む
					byte[] bitmapBytes = System.IO.File.ReadAllBytes(path);
					var stream = new MemoryStream(bitmapBytes);
					Image = new Bitmap(stream, false);
				}
				else
				{
					//存在しない
					LoadFailed = true;
				}
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
		private object lockObject;

		public ShellImageCache(string shellDirectoryPath)
		{
			lockObject = new object();
			images = new Dictionary<string, ShellImageSource>();
			directoryPath = shellDirectoryPath;
		}
		public ShellImageSource LoadImage(string filePath)
		{
			lock (lockObject)
			{
				ShellImageSource result;
				images.TryGetValue(filePath, out result);
				if (result == null)
				{
					result = new ShellImageSource(directoryPath + "/" + filePath);
					images.Add(filePath, result);
				}
				return result;
			}
		}
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

			if (duplicatedBeginRecord == null && duplicatedEndRecord == null)
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
				if (duplicatedEndRecord.End != range.End)
				{
					var range2 = new SurfaceRange(range.End + 1, duplicatedEndRecord.End);
					AddRange(range2);
				}
			}

			if (duplicatedBeginRecord != null)
			{
				duplicatedBeginRecord.End = range.Begin;
			}

			if (duplicatedEndRecord != null)
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
				foreach (var r in rangeList)
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
}
