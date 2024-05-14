using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Diagnostics.Tracing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Satolist.GhostBackup
{
	internal class Backup
	{
		private const string OptionRemoveOldBackupDays = "removeOldBackupDays";
		private const string OptionMaxFileSizeKb = "maxFileSizeKb";
		private const string OptionOverwrite = "overwrite";

		static int Main(string[] args)
		{
			return (new Backup()).MainProcess(args);
		}


		int MainProcess(string[] args)
		{
			//オプションは key-value 形式で表示
			var options = new Dictionary<string, string>();
			var commands = new List<string>();

			//オプション解析
			//あんまりきれいじゃないけど…
			for(int i = 0; i < args.Length; i++)
			{
				string item = args[i];
				if(item.StartsWith("--"))
				{
					var optionName = item.Substring(2);
					// --からはじまるのはオプションということにする
					switch (optionName)
					{
						case OptionRemoveOldBackupDays:
						case OptionMaxFileSizeKb:
							//引数をとるタイプ
							i++;
							options.Add(optionName, args[i]);
							break;
						default:
							//それ以外は引数なし
							options.Add(optionName, string.Empty);
							break;
					}
				}
				else
				{
					//オプション無しはコマンド引数扱い
					commands.Add(item);
				}

			}

			if(!commands.Any())
			{
				//一切指定されてないので、コマンド例を出して終わり
				Console.WriteLine("SatolistGhostBackup - さとりすとの簡易ゴーストバックアップツール");
				Console.WriteLine("");
				Console.WriteLine("backup (dbfile) (sourceDir)");
				Console.WriteLine("\tバックアップ実行");
				Console.WriteLine(" --removeOldBackupDays (days)");
				Console.WriteLine("\tバックアップ完了後に、days日を過ぎた古いバックアップを削除");
				Console.WriteLine(" --maxFileSizeKb (size)");
				Console.WriteLine("\t指定sizeキロバイトを超える大きなファイルをバックアップしない");
				Console.WriteLine("list backup (dbfile)");
				Console.WriteLine("\tバックアップデータを一覧表示");
				Console.WriteLine("list backup (dbfile) (sourceDir)");
				Console.WriteLine("\ttargetDir から作成したバックアップデータを一覧表示");
				Console.WriteLine("list file (dbfile) (backupId)");
				Console.WriteLine("\tbackupId の内容ファイルを一覧表示");
				Console.WriteLine("removeOldBackupDays (days) (dbfile) (sourceDir)");
				Console.WriteLine("\tdays 経過した sourceDir のバックアップをすべて削除");
				Console.WriteLine("extract (dbfile) (backupId) (targetDir)");
				Console.WriteLine("\ttargetDir にバックアップを書き出します。フォルダがから出ない場合は --overwrite で上書きにしないと失敗します。");
				Console.WriteLine(" --overwrite");
				Console.WriteLine("\t同名ファイルを上書き");
				Console.WriteLine("export (dbfile) (backupId) (filePath) (targetPath)");
				Console.WriteLine("\tバックアップ内の filePath のファイルを単体で targetPath に書き出します。ファイルが既に存在する場合 --overwrite で上書きにしないと失敗します。");
				Console.WriteLine(" --overwrite");
				Console.WriteLine("\t同名ファイルを上書き");
				return 0;
			}

			//コマンド実行
			var mainCommand = commands.First();
			commands = commands.Skip(1).ToList();
			switch(mainCommand)
			{
				case "backup":
					//バックアップ実行
					if(commands.Count == 2)
					{
						int removeOldDays = 0;
						int maxFileSizeKb = 0;

						//オプション確認
						if(options.TryGetValue(OptionRemoveOldBackupDays, out string daysStr))
						{
							if(int.TryParse(daysStr, out removeOldDays))
							{
								if(removeOldDays <= 0)
								{
									throw new Exception(OptionRemoveOldBackupDays + "は1以上の必要があります。");
								}
								//有効なオプションは外す
								options.Remove(OptionRemoveOldBackupDays);
							}
							else
							{
								Console.WriteLine(OptionRemoveOldBackupDays + "は数値指定が必要です。");
							}
						}

						if(options.TryGetValue(OptionMaxFileSizeKb, out string maxKbStr))
						{
							if(int.TryParse(maxKbStr, out maxFileSizeKb))
							{
								if(maxFileSizeKb <= 0)
								{
									throw new Exception(OptionMaxFileSizeKb + "は1以上の必要があります。");
								}
								//有効なオプションは外す
								options.Remove(OptionMaxFileSizeKb);
							}
							else
							{
								Console.WriteLine(OptionMaxFileSizeKb + "は数値指定が必要です。");
							}
						}

						//オプションが残ってたらエラー
						ThrowIfInvalidOptions(options);

						//バックアップ実行
						var dbPath = commands[0];
						var sourcePath = commands[1];
						BackupGhost(sourcePath, dbPath, maxFileSizeKb);

						//古いバックアップの消去
						if(removeOldDays > 0)
						{
							using (var connection = ConnectToDatabase(dbPath))
							{
								ExecuteRemoveOldItems(removeOldDays, sourcePath, connection);
							}
						}
					}
					else
					{
						ThrowInvalidArgs();
					}
					break;
				case "list":
					//表示
					{
						//オプションが残ってたらエラー
						ThrowIfInvalidOptions(options);

						var subCommand = commands[0];
						commands = commands.Skip(1).ToList();
						if(subCommand == "backup")
						{
							//パスは任意
							if(commands.Count == 1)
							{
								//パス指定無し
								var dbPath = commands[0];
								BackupData[] backup;
								using(var connection = ConnectToDatabase(dbPath))
								{
									backup = QueryBackupList(connection);
								}

								foreach(var item in backup)
								{
									Console.WriteLine(item);
								}
							}
							else if(commands.Count == 2)
							{
								//パス指定あり
								var dbPath = commands[0];
								var sourcePath = commands[1];
								BackupData[] backup;
								using(var connection = ConnectToDatabase(dbPath))
								{
									backup = QueryBackupList(sourcePath, connection);
								}

								foreach (var item in backup)
								{
									Console.WriteLine(item);
								}
							}
							else
							{
								ThrowInvalidArgs();
							}
						}
						else if(subCommand == "file")
						{
							//IDは必須
							if(commands.Count == 2)
							{
								var dbPath = commands[0];
								var backupId = commands[1];
								BackupFile[] files;
								using (var connection = ConnectToDatabase(dbPath))
								{
									files = QueryBackupFile(backupId, connection);
								}

								//ファイルリストの出力
								foreach(var item in files)
								{
									Console.WriteLine(item.ToString());
								}
							}
							else
							{
								ThrowInvalidArgs();
							}
						}
						else
						{
							ThrowInvalidArgs();
						}
					}
					

					break;
				case "removeOldBackupDays":
					//古いバックアップの削除
					if (commands.Count == 3)
					{
						var days = commands[0];
						var dbPath = commands[1];
						var sourcePath = commands[2];
						ThrowIfInvalidOptions(options);

						//オプション確認
						int removeDays;
						int.TryParse(days, out removeDays);

						if (removeDays > 0)
						{
							using (var connection = ConnectToDatabase(dbPath))
							{
								//削除実行
								ExecuteRemoveOldItems(removeDays, sourcePath, connection);
							}
						}
						else
						{
							throw new Exception("日数指定は1以上の必要があります。");
						}
					}
					else
					{
						ThrowInvalidArgs();
					}

					break;
				case "extract":

					//アーカイブの解凍
					if (commands.Count == 3)
					{
						bool isOverWrite = false;
						if (options.ContainsKey(OptionOverwrite))
						{
							isOverWrite = true;
							options.Remove(OptionOverwrite);
						}
						ThrowIfInvalidOptions(options);

						var dbPath = commands[0];
						var backupId = commands[1];
						var targetPath = commands[2];

						ZipArchive archive;
						using (var connection = ConnectToDatabase(dbPath))
						{
							archive = OpenBackupArchive(backupId, connection);
						}
						if (archive == null)
						{
							throw new Exception("backupIdが見つかりません。");
						}

						if (isOverWrite)
						{
							//ExtractToDirectoryに上書き機能がないので１エントリずつ処理する
							foreach (var entry in archive.Entries)
							{
								var path = Path.Combine(targetPath, entry.FullName);
								Directory.CreateDirectory(Path.GetDirectoryName(path));
								entry.ExtractToFile(path, true);
							}
						}
						else
						{
							if (Directory.EnumerateFileSystemEntries(targetPath).Any())
							{
								throw new Exception("出力先が空ではありません。空のフォルダを指定するか --overwrite で上書きにしてください。");
							}
							archive.ExtractToDirectory(targetPath);
						}
					}
					else
					{
						ThrowInvalidArgs();
					}
					break;

				case "export":
					//アーカイブから単体ファイルを取り出す
					if(commands.Count == 4)
					{
						var dbPath = commands[0];
						var backupId = commands[1];
						var sourcePath = NormalizeArchivePath(commands[2]);
						var targetPath = commands[3];

						bool isOverWrite = false;
						if (options.ContainsKey(OptionOverwrite))
						{
							isOverWrite = true;
							options.Remove(OptionOverwrite);
						}
						ThrowIfInvalidOptions(options);

						ZipArchive archive;
						using(var connection = ConnectToDatabase(dbPath))
						{
							archive = OpenBackupArchive(backupId, connection);
						}

						if(archive == null)
						{
							throw new Exception("backupIdが見つかりません。");
						}

						if(!isOverWrite)
						{
							if(File.Exists(targetPath))
							{
								throw new Exception("出力先にファイルが存在しています。上書きするには --overwrite オプションを指定してください。");
							}
						}

						//出力: ややこしいけどアーカイブエントリは / 区切りなので今度はそちらに統一
						var entry = archive.GetEntry(sourcePath.Replace("\\", "/"));
						if (entry == null)
						{
							throw new Exception("指定ファイルがバックアップ内に見つかりません。");
						}

						entry.ExtractToFile(targetPath, isOverWrite);
					}
					else
					{
						ThrowInvalidArgs();
					}
					break;
				default:
					ThrowInvalidArgs();
					break;
			}
			return 0;	
		}

		public SQLiteConnection ConnectToDatabase(string databasePath)
		{
			//初期化用クエリ
			string[] initializeSql =
			{
				"create table if not exists backup(backupId text primary key, sourcePath text not null, archiveBinary blob not null, archiveSizeBytes integer not null, createdAt text not null default CURRENT_TIMESTAMP);",
				"create table if not exists files(idx integer primary key autoincrement, backupId text not null, path text not null, sizeBytes integer not null, updatedAt text not null);",
				"create index if not exists filesBackupId on files(backupId);",
				"create index if not exists backupSourcePath on backup(sourcePath, createdAt);",
				"create index if not exists backupCreatedAt on backup(createdAt);",
			};

			var builder = new SQLiteConnectionStringBuilder() { DataSource = databasePath };
			var connection = new SQLiteConnection(builder.ToString());
			connection.Open();

			//初期化クエリの実行
			foreach(var sql in initializeSql)
			{
				using(var command = new SQLiteCommand(sql, connection))
				{
					command.ExecuteNonQuery();
				}
			}

			return connection;
		}

		//オプション不正時のエラー
		public static void ThrowInvalidArgs()
		{
			throw new Exception("コマンド形式が正しくありません。オプション無しでSatolistGhostBackupを実行するとヘルプを確認できます。");
		}

		//余計なオプション指定を指摘してエラーにする
		public static void ThrowIfInvalidOptions(Dictionary<string, string> options)
		{
			if(options.Any())
			{
				throw new Exception("不明なオプション: " + string.Join(",", options.Keys));
			}
		}

		//パスの形式を統一化
		public static string NormalizeFullPath(string path)
		{
			//さとりすとにあわせて / でセパレート
			var result = Path.GetFullPath(path);
			result = result.Replace("\\", "/");
			result = result.TrimEnd('/');
			return result;
		}

		//アーカイブ内のパスの形式統一
		public static string NormalizeArchivePath(string path)
		{
			return path.Replace("\\", "/");
		}

		//ゴーストバックアップの作成
		//nar作成と同じようにできるとよさそう
		public void BackupGhost(string ghostDirectory, string dbPath, int maxFileSizeKb)
		{
			ghostDirectory = NormalizeFullPath(ghostDirectory);

			using (var temporaryDirectory = new TemporaryDirectory())
			{
				//再帰的なファイルリストアップが要る
				var ghostDirectoryUri = new Uri(ghostDirectory + "\\");
				var backupFileFullPathList = new List<string>();
				var backupItems = new List<BackupFile>();
				var backupFilesDirectory = Path.Combine(temporaryDirectory.FullPath, "files");
				var backupZipPath = Path.Combine(temporaryDirectory.FullPath, "archive.zip");

				//バックアップ対象をリストアップ
				ListupFiles(ghostDirectory, backupFileFullPathList, maxFileSizeKb);

				//ファイルをコピー
				foreach (var item in backupFileFullPathList)
				{
					//相対パスを作成
					var relativePath = NormalizeArchivePath(ghostDirectoryUri.MakeRelativeUri(new Uri(item)).ToString());
					var backupPath = Path.Combine(backupFilesDirectory, relativePath);

					//一時フォルダにコピー
					Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
					File.Copy(item, backupPath);

					//相対パスと更新日時を記憶
					var updatedAt = File.GetLastWriteTimeUtc(item);
					var fileInfo = new FileInfo(item);
					backupItems.Add(new BackupFile(null, fileInfo.Length, updatedAt, relativePath));
				}

				//zip圧縮
				ZipFile.CreateFromDirectory(backupFilesDirectory, backupZipPath, CompressionLevel.Optimal, false);

				//バックアップサイズ
				var archiveInfo = new FileInfo(backupZipPath);

				//zipをメモリに展開
				var archiveData = File.ReadAllBytes(backupZipPath);

				//バックアップIDを決定
				var backupId = Guid.NewGuid().ToString();

				//データベースにバイナリブロブを書き込み
				using (var connection = ConnectToDatabase(dbPath))
				{
					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							//バックアップID重複を確認
							while (true)
							{
								var sql = "select backupId from backup where backupId = @backupId";
								using (var command = new SQLiteCommand(sql, connection))
								{
									var param = new SQLiteParameter();
									param.DbType = System.Data.DbType.String;
									param.Value = backupId;
									param.ParameterName = "@backupId";
									command.Parameters.Add(param);
									using (var reader = command.ExecuteReader())
									{
										if (reader.Read())
										{
											//レコードがあったら重複
											backupId = Guid.NewGuid().ToString();
											continue;
										}
									}
								}

								//なにごともなく終わればOK
								break;
							}

							//データベースに登録
							{
								var sql = "insert into backup(backupId, sourcePath, archiveBinary, archiveSizeBytes) values(@backupId, @sourcePath, @archiveBinary, @archiveSizeBytes)";
								using (var command = new SQLiteCommand(sql, connection))
								{
									{
										var param = new SQLiteParameter();
										param.DbType = System.Data.DbType.String;
										param.Value = backupId;
										param.ParameterName = "@backupId";
										command.Parameters.Add(param);
									}
									{
										var param = new SQLiteParameter();
										param.DbType = System.Data.DbType.String;
										param.Value = ghostDirectory;
										param.ParameterName = "@sourcePath";
										command.Parameters.Add(param);
									}
									{
										var param = new SQLiteParameter();
										param.DbType = System.Data.DbType.Binary;
										param.Value = archiveData;
										param.ParameterName = "@archiveBinary";
										command.Parameters.Add(param);
									}
									{
										var param = new SQLiteParameter();
										param.DbType = System.Data.DbType.Int64;
										param.Value = archiveInfo.Length;
										param.ParameterName = "@archiveSizeBytes";
										command.Parameters.Add(param);
									}
									command.ExecuteNonQuery();
								}
							}

							//ファイルリストを記録
							{
								var sql = "insert into files(backupId, path, sizeBytes, updatedAt) values(@backupId, @path, @sizeBytes, @updatedAt)";
								foreach (var item in backupItems)
								{
									using (var command = new SQLiteCommand(sql, connection))
									{
										{
											var param = new SQLiteParameter();
											param.DbType = System.Data.DbType.String;
											param.Value = backupId;
											param.ParameterName = "@backupId";
											command.Parameters.Add(param);
										}
										{
											var param = new SQLiteParameter();
											param.DbType = System.Data.DbType.DateTime;
											param.Value = item.UpdatedAt;
											param.ParameterName = "@updatedAt";
											command.Parameters.Add(param);
										}
										{
											var param = new SQLiteParameter();
											param.DbType = System.Data.DbType.String;
											param.Value = item.Path;
											param.ParameterName = "@path";
											command.Parameters.Add(param);
										}
										{
											var param = new SQLiteParameter();
											param.DbType = System.Data.DbType.Int64;
											param.Value = item.SizeBytes;
											param.ParameterName = "@sizeBytes";
											command.Parameters.Add(param);
										}
										command.ExecuteNonQuery();
									}
								}
							}
							transaction.Commit();
						}
						catch
						{
							transaction.Rollback();
							throw;
						}
					}
				}
			}
		}

		public void ListupFiles(string directoryPath, List<string> backupFiles, int maxFileSizeKb)
		{
			var items = Directory.GetFileSystemEntries(directoryPath, "*", SearchOption.TopDirectoryOnly);
			foreach(var item in items)
			{
				var attr = File.GetAttributes(item);
				if((attr & FileAttributes.Hidden) != 0)
				{
					//隠しファイルは無視
					continue;
				}

				if (Directory.Exists(item))
				{
					ListupFiles(item, backupFiles, maxFileSizeKb);
				}
				else
				{
					//無駄に肥大化するSSPログは無視
					if(Path.GetFileName(item) == "ssp_shiori_log.txt")
					{
						continue;
					}

					if (maxFileSizeKb > 0)
					{
						//ファイルサイズ上限指定あり
						var info = new FileInfo(item);
						if(info.Length <= maxFileSizeKb * 1024)
						{
							backupFiles.Add(item);
						}
					}
					else
					{
						backupFiles.Add(item);
					}
				}
			}
		}

		//バックアップリストの取得
		public BackupData[] QueryBackupList(SQLiteConnection connection)
		{
			var items = new List<BackupData>();
			var sql = "select backupId, createdAt, sourcePath, archiveSizeBytes from backup order by createdAt desc";
			using(var command = new SQLiteCommand(sql, connection))
			{
				using(var reader = command.ExecuteReader())
				{
					while(reader.Read())
					{
						var backupId = reader.GetString(0);
						var createdAt = reader.GetDateTime(1);
						var sourcePath = reader.GetString(2);
						var archiveSizeBytes = reader.GetInt64(3);
						items.Add(new BackupData(backupId, archiveSizeBytes, createdAt, sourcePath));
					}
				}
			}
			return items.ToArray();
		}

		//ディレクトリに関するバックアップリストの取得
		public BackupData[] QueryBackupList(string sourcePath, SQLiteConnection connection)
		{
			sourcePath = NormalizeFullPath(sourcePath);

			var items = new List<BackupData>();
			var sql = "select backupId, createdAt, archiveSizeBytes from backup where sourcePath = @sourcePath order by createdAt desc";
			using (var command = new SQLiteCommand(sql, connection))
			{
				var param = new SQLiteParameter();
				param.DbType = System.Data.DbType.String;
				param.Value = sourcePath;
				param.ParameterName = "@sourcePath";
				command.Parameters.Add(param);

				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						var backupId = reader.GetString(0);
						var createdAt = reader.GetDateTime(1);
						var archiveSizeBytes = reader.GetInt64(2);
						items.Add(new BackupData(backupId, archiveSizeBytes, createdAt, sourcePath));
					}
				}
			}
			return items.ToArray();
		}


		//バックアップファイルリストの取得
		public BackupFile[] QueryBackupFile(string backupId, SQLiteConnection connection)
		{
			var items = new List<BackupFile>();
			var sql = "select updatedAt, path, sizeBytes from files where backupId = @targetId";
			using(var command = new SQLiteCommand(sql, connection))
			{
				var param = new SQLiteParameter();
				param.DbType = System.Data.DbType.String;
				param.Value = backupId;
				param.ParameterName = "@targetId";
				command.Parameters.Add(param);

				using(var reader = command.ExecuteReader())
				{
					while(reader.Read())
					{
						var updatedAt = reader.GetDateTime(0);
						var path = reader.GetString(1);
						var sizeBytes = reader.GetInt64(2);
						items.Add(new BackupFile(backupId, sizeBytes, updatedAt, path));
					}
				}
			}
			return items.ToArray();
		}

		//バックアップデータの展開
		public ZipArchive OpenBackupArchive(string backupId, SQLiteConnection connection)
		{
			byte[] archiveBlob = null;
			var sql = "select archiveBinary from backup where backupId = @backupId";
			using (var command = new SQLiteCommand(sql, connection))
			{
				var param = new SQLiteParameter();
				param.ParameterName = "@backupId";
				param.Value = backupId;
				param.DbType = System.Data.DbType.String;
				command.Parameters.Add(param);

				using(var reader = command.ExecuteReader())
				{
					if(reader.Read())
					{
						archiveBlob = (byte[])reader.GetValue(0);
					}
					else
					{
						//見つからない
						return null;
					}
				}
			}

			//読み出しはファイル単体とかできると便利が良いのでZipArchiveを使ってオンメモリでやる
			return new ZipArchive(new MemoryStream(archiveBlob));
		}

		//保存期間をすぎたデータの削除
		public void ExecuteRemoveOldItems(int removeDays, string sourcePath, SQLiteConnection connection)
		{
			sourcePath = NormalizeFullPath(sourcePath);
			Debug.Assert(removeDays > 0);

			//不要データの削除
			var sql = $"delete from backup where sourcePath = @sourcePath and createdAt < date('now', '-{removeDays} days')";
			using (var command = new SQLiteCommand(sql, connection))
			{
				var param = new SQLiteParameter();
				param.DbType = System.Data.DbType.String;
				param.Value = sourcePath;
				param.ParameterName = "@sourcePath";
				command.Parameters.Add(param);

				command.ExecuteNonQuery();
			}

			Vacuum(connection);
		}

		//バックアップデータの削除
		public void RemoveBackup(string backupId, SQLiteConnection connection)
		{
			using (var transaction = connection.BeginTransaction())
			{
				try
				{
					//それぞれのテーブルから削除
					{
						var sql = "delete from backup where backupId = @backupId";
						using (var command = new SQLiteCommand(sql, connection))
						{
							var param = new SQLiteParameter();
							param.DbType = System.Data.DbType.String;
							param.Value = backupId;
							param.ParameterName = "@backupId";
							command.Parameters.Add(param);

							command.ExecuteNonQuery();
						}
					}

					{
						var sql = "delete from files where backupId = @backupId";
						using (var command = new SQLiteCommand(sql, connection))
						{
							var param = new SQLiteParameter();
							param.DbType = System.Data.DbType.String;
							param.Value = backupId;
							param.ParameterName = "@backupId";
							command.Parameters.Add(param);

							command.ExecuteNonQuery();
						}
					}
					transaction.Commit();
				}
				catch
				{
					transaction.Rollback();
					throw;
				}
			}

			Vacuum(connection);
		}

		//不要領域の整理
		public void Vacuum(SQLiteConnection connection)
		{
			var sql = "vacuum";
			using(var command = new SQLiteCommand(sql, connection))
			{
				command.ExecuteNonQuery();
			}
		}
	}

	public class BackupData
	{
		public string BackupId { get; }
		public DateTime CreatedAt { get; }
		public string SourcePath { get; }
		public long SizeBytes { get; }

		public BackupData(string backupId, long sizeBytes, DateTime createdAt, string sourcePath)
		{
			BackupId = backupId;
			CreatedAt = createdAt;
			SourcePath = sourcePath;
			SizeBytes = sizeBytes;
		}

		public override string ToString()
		{
			return $"{BackupId} {CreatedAt.ToLocalTime()} {SourcePath}";
		}
	}

	public class BackupFile
	{
		public string BackupId { get; }
		public string Path { get; }
		public long SizeBytes { get; }
		public DateTime UpdatedAt { get; }

		public BackupFile(string backupId, long sizeBytes, DateTime updatedAt, string path)
		{
			BackupId = backupId;
			SizeBytes = sizeBytes;
			UpdatedAt = updatedAt;
			Path = path;
		}

		public override string ToString()
		{
			return $"{UpdatedAt.ToLocalTime()} {SizeBytes} {Path}";
		}
	}
}
