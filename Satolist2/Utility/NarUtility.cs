using Satolist2.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Utility
{
	//なーをつくる
	//更新ファイルもつくる
	public class NarUtility
	{
		//組み込み無視ファイル名
		private static readonly HashSet<string> IgnoreFileNames = new HashSet<string>()
		{
			"desktop.ini",
			"thumbs.db",
			"folder.htt",
			"msccreate.dir",
			".ds_store",
			"_catalog.vix",
			"satori_savedata.txt",
			"satori_savebackup.txt"
		};

		//組み込み無視フォルダ名
		private static readonly string[] IgnoreDirectoryNames =
		{
			"profile",
			"var",
			"_macosx",
			"xtrastuf.mac",
			".git",
			".vs",
			".svn"
		};

		private static readonly System.Security.Cryptography.MD5CryptoServiceProvider Md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();

		//更新ファイルを作成
		//shellでもghostでも同様のはず？
		public static void CreateUpdateFile(string path)
		{
			var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
			var pathHashSize = new List<Tuple<string, string, int>>();
			var rootPathUri = new Uri(path + "/");

			var pathUpdates2Dau = path + "/updates2.dau";
			var pathUpdatesTxt = path + "/updates.txt";

			//古い更新ファイルを削除
			File.Delete(pathUpdates2Dau);
			File.Delete(pathUpdatesTxt);

			//ファイルをリストアップ、md5計算
			foreach(var filePath in files)
			{
				//相対パスを用意
				var relativePath = rootPathUri.MakeRelativeUri(new Uri(filePath)).ToString();

				if (IsIgnoreFile(relativePath, false))
					continue;

				//ハッシュ計算
				var (hash, size) = CalcMd5(filePath);
				pathHashSize.Add(new Tuple<string, string, int>(relativePath, hash, size));
			}

			//出力
			File.WriteAllText(pathUpdates2Dau, BuildUpdates2Dau(pathHashSize), Constants.EncodingShiftJis);
			File.WriteAllText(pathUpdatesTxt, BuildUpdatesTxt(pathHashSize), Constants.EncodingShiftJis);
		}

		private static (string, int) CalcMd5(string path)
		{
			var data = File.ReadAllBytes(path);
			var hash = Md5.ComputeHash(data);
			var stringBuilder = new StringBuilder();
			foreach (var b in hash)
				stringBuilder.Append(b.ToString("x2"));
			return (stringBuilder.ToString(), data.Length);
		}

		//updates2.dau のフォーマットで出力
		private static string BuildUpdates2Dau(IEnumerable<Tuple<string,string,int>> pathHashSize)
		{
			var builder = new StringBuilder();
			foreach(var item in pathHashSize)
				builder.AppendLine(string.Concat(item.Item1, (char)1, item.Item2, (char)1, "size=", item.Item3, (char)1 ));
			return builder.ToString();
		}

		//updates.txt のフォーマットで出力
		private static string BuildUpdatesTxt(IEnumerable<Tuple<string,string,int>> pathHashSize)
		{
			var builder = new StringBuilder();
			foreach (var item in pathHashSize)
				builder.AppendLine(string.Concat("file,", item.Item1, (char)1, item.Item2, (char)1, "size=", item.Item3, (char)1));
			return builder.ToString();
		}

		public static bool IsIgnoreFile(string relativePath, bool isCreateNar)
		{
			//TODO: developer_optionsをチェックすること
			var lowerPath = relativePath.ToLower();
			if (IgnoreFileNames.Contains(Path.GetFileName(lowerPath)))
				return true;
			if (IgnoreDirectoryNames.FirstOrDefault(o => lowerPath.Contains(o + "/")) != null)
				return true;
			return false;
		}

		public static void CreateNar(string path, string outputPath)
		{
			//TODO: テンポラリファイルはまともなかんじにしたい
			string temporaryDirectory = Path.GetTempPath() + "/satolist/" + Guid.NewGuid().ToString();
			var rootPathUri = new Uri(path + "/");

			var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
			foreach(var item in files)
			{
				var relativePath = rootPathUri.MakeRelativeUri(new Uri(item)).ToString();
				if (IsIgnoreFile(relativePath, true))
					continue;
				Directory.CreateDirectory(Directory.GetParent(temporaryDirectory + "/" + relativePath).FullName);
				File.Copy(item, temporaryDirectory + "/" + relativePath, true);
			}

			//zipに固める
			ZipFile.CreateFromDirectory(temporaryDirectory, outputPath, CompressionLevel.Optimal, false, Constants.EncodingShiftJis);
		}

	}
}
