using Satolist2.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
			"satori_savebackup.txt",
			"updates.txt",
			"updates2.dau"
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
			var pathHashSize = new List<Tuple<string, string, int>>();
			var developerOptions = new DeveloperOptionsParser();

			var pathUpdates2Dau = path + "/updates2.dau";
			var pathUpdatesTxt = path + "/updates.txt";

			//developerOptionsの検出
			var pathDeveloperOptions = path + "/developer_options.txt";
			if(File.Exists(pathDeveloperOptions))
			{
				developerOptions = new DeveloperOptionsParser(File.ReadAllText(pathDeveloperOptions, Constants.EncodingShiftJis));
			}

			//古い更新ファイルを削除
			File.Delete(pathUpdates2Dau);
			File.Delete(pathUpdatesTxt);

			//ファイルをリストアップ、md5計算
			var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
			foreach (var filePath in files)
			{
				//相対パスを用意
				var relativePath = DictionaryUtility.MakeRelativePath(path, filePath);

				if (IsIgnoreFile(relativePath, false))
					continue;
				if (developerOptions.IsIgnored(relativePath, false))
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
			builder.AppendLine("charset,Shift_JIS");
			foreach (var item in pathHashSize)
				builder.AppendLine(string.Concat("file,", item.Item1, (char)1, item.Item2, (char)1, "size=", item.Item3, (char)1));
			return builder.ToString();
		}

		//updates2.dauのフォーマットを解析
		private static IEnumerable<Tuple<string,string,int>> ParseUpdates2Dau(string fileBody)
		{
			var lines = DictionaryUtility.SplitLines(fileBody);
			foreach (var line in lines)
			{
				if (string.IsNullOrEmpty(line))
					continue;
				var items = line.Split((char)1);
				if (items.Length < 3)
					continue;

				var size = int.Parse(items[2].Substring("size=".Length));
				yield return new Tuple<string, string, int>(items[0], items[1], size);
			}
		}

		//updates.txt のフォーマット解析
		private static IEnumerable<Tuple<string,string,int>> ParseUpdatesTxt(string fileBody)
		{
			var lines = DictionaryUtility.SplitLines(fileBody);
			foreach(var line in lines)
			{
				if (string.IsNullOrEmpty(line))
					continue;
				var items = line.Substring("file,".Length).Split((char)1);
				if (items.Length < 3)
					continue;

				var size = int.Parse(items[2].Substring("size=".Length));
				yield return new Tuple<string, string, int>(items[0], items[1], size);
			}
		}


		public static bool IsIgnoreFile(string relativePath, bool isCreateNar)
		{
			var lowerPath = relativePath.ToLower();
			if (IgnoreFileNames.Contains(Path.GetFileName(lowerPath)))
				return true;
			if (IgnoreDirectoryNames.FirstOrDefault(o => lowerPath.Contains(o + "/")) != null)
				return true;
			return false;
		}

		public static void CreateNar(string sourceDirectory, string outputPath)
		{
			using (var temporaryDirectory = new TemporaryDirectory())
			{
				var rootPathUri = new Uri(sourceDirectory + "/");

				//developerOptionsの検出
				var developerOptions = new DeveloperOptionsParser();
				var pathDeveloperOptions = sourceDirectory + "/developer_options.txt";
				if (File.Exists(pathDeveloperOptions))
				{
					developerOptions = new DeveloperOptionsParser(File.ReadAllText(pathDeveloperOptions, Constants.EncodingShiftJis));
				}

				var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
				foreach (var item in files)
				{
					var relativePath = rootPathUri.MakeRelativeUri(new Uri(item)).ToString();
					if (IsIgnoreFile(relativePath, true))
						continue;
					if (developerOptions.IsIgnored(relativePath, true))
						continue;

					Directory.CreateDirectory(Directory.GetParent(temporaryDirectory + "/" + relativePath).FullName);
					File.Copy(item, temporaryDirectory + "/" + relativePath, true);
				}

				//zipに固める
				ZipFile.CreateFromDirectory(temporaryDirectory.ToString(), outputPath, CompressionLevel.Optimal, false, Constants.EncodingShiftJis);
			}
		}

		//ftpで更新ファイルをアップロードする
		public static void UploadUpdates(Core.FtpClient ftpClient, string sourceDirectory, string remotePath, bool usediff, Action<Progress> progressHandler, CancellationToken? cancellationToken)
		{
			var diffFiles = new Dictionary<string, Tuple<string, int>>();   //file,hash,size

			//更新ファイルを生成、解析
			CreateUpdateFile(sourceDirectory);
			progressHandler?.Invoke(new Progress() { UseProgress = false, Message = "更新ファイルを作成しました。" });
			cancellationToken?.ThrowIfCancellationRequested();

			var localUpdates2Dau = System.IO.File.ReadAllText(DictionaryUtility.ConbinePath(sourceDirectory, "updates2.dau"), Constants.EncodingShiftJis);
			var localFileList = ParseUpdates2Dau(localUpdates2Dau);
			var uploadFiles = new List<Tuple<string, string, int>>();
			uploadFiles.AddRange(localFileList);
			uploadFiles.Add(new Tuple<string, string, int>("updates2.dau", string.Empty, 0));
			uploadFiles.Add(new Tuple<string, string, int>("updates.txt", string.Empty, 0));
			cancellationToken?.ThrowIfCancellationRequested();

			//差分アップロードのためdiffを取得する
			if (usediff)
			{
				progressHandler?.Invoke(new Progress() { UseProgress = false, Message = "差分アップロードのためサーバのupdates2.dauを取得します。" });
				IEnumerable<Tuple<string, string, int>> updateData = null;
				//updates2.dau
				try
				{
					var updates2dauPath = DictionaryUtility.ConbinePath(remotePath, "updates2.dau");
					var downloadedFile = ftpClient.DownloadFile(updates2dauPath);
					var fileBody = Constants.EncodingShiftJis.GetString(downloadedFile);
					updateData = ParseUpdates2Dau(fileBody);
					progressHandler?.Invoke(new Progress() { UseProgress = false, Message = "updates2.dau を取得しました。" });
					cancellationToken?.ThrowIfCancellationRequested();
				}
				catch
				{
					//見つからなかったらupdate.txtにフォールバック
					progressHandler?.Invoke(new Progress() { UseProgress = false, Message = "updates2.dau の取得に失敗したため updates.txt からの差分に切替えます。" });
					try
					{
						var updateTxtPath = DictionaryUtility.ConbinePath(remotePath, "updates.txt");
						var downloadedFile = ftpClient.DownloadFile(updateTxtPath);
						var fileBody = Constants.EncodingShiftJis.GetString(downloadedFile);
						updateData = ParseUpdatesTxt(fileBody);
						progressHandler?.Invoke(new Progress() { UseProgress = false, Message = "updates.txt を取得しました。" });
						cancellationToken?.ThrowIfCancellationRequested();
					}
					catch
					{
						progressHandler?.Invoke(new Progress() { UseProgress = false, Message = "updates.txt の取得に失敗しました。フルアップロードに切替えます。" });
					}
				}

				//更新差分の収集
				if(updateData != null)
				{
					foreach (var item in updateData)
						diffFiles.Add(item.Item1, new Tuple<string, int>(item.Item2, item.Item3));
				}
			}

			//ここからアップロード
			cancellationToken?.ThrowIfCancellationRequested();
			progressHandler?.Invoke(new Progress() { UseProgress = true, Message="ファイルアップロードを開始します。" });

			//更新ファイルベースで更新を行う
			for(int i = 0; i < uploadFiles.Count; i++)
			{
				cancellationToken?.ThrowIfCancellationRequested();
				var item = uploadFiles[i];
				var progressValue = (float)i / (float)uploadFiles.Count;
				var fileName = DictionaryUtility.ConbinePath(sourceDirectory, item.Item1);
				var remoteFileName = DictionaryUtility.ConbinePath(remotePath, item.Item1);
				var hash = item.Item2;
				var size = item.Item3;

				//差分チェック
				if(diffFiles.ContainsKey(item.Item1))
				{
					var diff = diffFiles[item.Item1];
					if(hash == diff.Item1 && size == diff.Item2)
					{
						//差分一致につきスキップ
						var skipMessage = string.Format("{1}/{2} skip: {0}", item.Item1, i, uploadFiles.Count);
						progressHandler?.Invoke(new Progress() { UseProgress = true, Value = progressValue, Message = skipMessage });
						continue;
					}
				}

				//アップロード
				var uploadMessage = string.Format("{1}/{2} upload: {0}", item.Item1, i, uploadFiles.Count);
				progressHandler?.Invoke(new Progress() { UseProgress = true, Value = progressValue, Message = uploadMessage });
				ftpClient.UploadFile(fileName, remoteFileName);
			}
		}

		//ftpでnarをアップロードする
		public static void UploadNar(string sourceDirectory, string remotePath, Core.FtpClient ftpClient, CancellationToken? cancellationToken)
		{
			using (var narFile = new TemporaryFile(true))
			{
				//nar作成
				cancellationToken?.ThrowIfCancellationRequested();
				CreateNar(sourceDirectory, narFile.FullPath);

				//FTPアップロード
				cancellationToken?.ThrowIfCancellationRequested();
				ftpClient.UploadFile(narFile.FullPath, remotePath);
			}
		}

		//ななろだにnarをアップロードする
		public static Core.NarnaloaderUploadResult UploadNar(string sourceDirectory, Core.NarnaloaderAccount account, string ghostId, CancellationToken? cancellationToken)
		{
			using(var narFile = new TemporaryFile(true))
			{
				//nar作成
				cancellationToken?.ThrowIfCancellationRequested();
				CreateNar(sourceDirectory, narFile.FullPath);

				//NNLアップロード
				cancellationToken?.ThrowIfCancellationRequested();
				var task = Core.NarNaLoaderClient.UploadFile(narFile.FullPath, ghostId, account);
				task.Wait();
				return task.Result;
			}
		}
	}

	public class DeveloperOptionsParser
	{
		//有効レコードかどうかを判定するためのもの
		private static readonly Regex NoNarNoUpdateRegex = new Regex("(nonar|noupdate)\\s*,?\\s*(nonar|noupdate)?");

		public ReadOnlyCollection<string> CommonLines { get; }
		public ReadOnlyCollection<Record> Records { get; }

		public DeveloperOptionsParser()
		{
			CommonLines = new ReadOnlyCollection<string>(Array.Empty<string>());
			Records = new ReadOnlyCollection<Record>(Array.Empty<Record>());
		}

		public DeveloperOptionsParser(string fileBody)
		{
			var commonLines = new List<string>();
			var records = new List<Record>();

			//csvをロード
			var csvBuilder = new CsvBuilder();
			csvBuilder.Deserialize(fileBody);

			//フォーマットに沿っているデータを検索
			foreach (var item in csvBuilder.Records)
			{
				//ファイルパスではないレコードは無視。"//"で始まる行もコメントとしてみなしておく
				bool isInvalid = false;
				if ((item.Key?.IndexOfAny(System.IO.Path.GetInvalidPathChars()) ?? -1) >= 0)
				{
					isInvalid = true;
				}
				else if ((item.Key?.IndexOf("//") ?? -1) == 0)
				{
					isInvalid = true;
				}

				if (!isInvalid && NoNarNoUpdateRegex.IsMatch(item.Value) && !string.IsNullOrEmpty(item.Key))
				{
					var record = new Record(item.Key, item.Value);
					var existsRecord = records.FirstOrDefault(o => o.Path == item.Key);
					if(existsRecord != null)
					{
						existsRecord.Merge(record);
					}
					else
					{
						records.Add(record);
					}
					continue;
				}

				if (!string.IsNullOrEmpty(item.Key))
				{
					commonLines.Add(string.Concat(item.Key, ",", item.Value));
				}
				else
				{
					commonLines.Add(item.Value);
				}
			}

			CommonLines = new ReadOnlyCollection<string>(commonLines);
			Records = new ReadOnlyCollection<Record>(records);
		}

		public bool IsIgnored(string path, bool isCreateNar)
		{
			return Records.Any(o =>
				{
					var isMatch = o.WildCardRegex.IsMatch(path);
					if (isMatch)
					{
						if (isCreateNar)
							return o.IsNonar;
						else
							return o.IsNoUpdate;
					}
					return false;
				});
		}

		public class Record
		{
			public string Path { get; private set; }
			public Regex WildCardRegex { get; private set; }
			public bool IsNonar { get; private set; }
			public bool IsNoUpdate { get; private set; }

			public Record(string path, string value)
			{
				Path = path;
				IsNonar = value.IndexOf("nonar") >= 0;
				IsNoUpdate = value.IndexOf("noupdate") >= 0;

				//ワイルドカードを正規表現に置換するのでそれ以外の正規表現をエスケープする
				var wildCard = Regex.Escape(path);
				//エスケープされた?と*をもとにもどす
				wildCard = wildCard.Replace(@"\*", @"*");
				wildCard = wildCard.Replace(@"\?", @"?");
				WildCardRegex = new Regex(wildCard);
			}

			public void Merge(Record other)
			{
				Debug.Assert(Path == other.Path);
				IsNonar = IsNonar || other.IsNonar;
				IsNoUpdate = IsNoUpdate || other.IsNoUpdate;
			}
		}
	}
}
