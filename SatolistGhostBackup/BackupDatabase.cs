using Satolist.GhostBackup;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SatolistGhostBackup
{
	public class BackupDatabase
	{
		private Backup implement_;
		public string DatabasePath { get; }

		public BackupDatabase(string path)
		{
			DatabasePath = path;
			implement_ = new Backup();
		}

		//全部のバックアップを取得
		public BackupData[] QueryBackups()
		{
			using(var connection = implement_.ConnectToDatabase(DatabasePath))
			{
				return implement_.QueryBackupList(connection);
			}
		}

		//バックアップ元を指定して取得
		public BackupData[] QueryBackups(string sourcePath)
		{
			using(var connection = implement_.ConnectToDatabase(DatabasePath))
			{
				return implement_.QueryBackupList(sourcePath, connection);
			}
		}

		//ファイルリストを取得
		public BackupFile[] QueryFiles(string backupId)
		{
			using(var connection = implement_.ConnectToDatabase(DatabasePath))
			{
				return implement_.QueryBackupFile(backupId, connection);
			}
		}

		//バックアップをフォルダにエクスポート
		public void ExtractToDirectory(string backupId, string targetPath, bool isOverWrite)
		{
			if(!Directory.Exists(targetPath))
			{
				//対象フォルダが無ければやめる
				throw new DirectoryNotFoundException();
			}

			ZipArchive archive;
			using (var connection = implement_.ConnectToDatabase(DatabasePath))
			{
				archive = implement_.OpenBackupArchive(backupId, connection);
			}

			//ExtractToDirectoryに上書き機能がないので１エントリずつ処理する
			foreach(var entry in archive.Entries)
			{
				var path = Path.Combine(targetPath, entry.FullName);
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				entry.ExtractToFile(path, isOverWrite);
			}
		}

		//ファイル単体をエクスポート
		public void ExportFile(string backupId, string path, string targetPath, bool isOverWrite)
		{
			ZipArchive archive;
			using(var connection = implement_.ConnectToDatabase(DatabasePath))
			{
				archive = implement_.OpenBackupArchive(backupId, connection);
			}
			var entry = archive.GetEntry(path);
			if (entry == null)
				return;

			entry.ExtractToFile(targetPath, isOverWrite);
		}

		//バックアップを削除
		public void RemoveBackup(string backupId)
		{
			using(var connection = implement_.ConnectToDatabase(DatabasePath))
			{
				implement_.RemoveBackup(backupId, connection);
			}
		}
	}
}
