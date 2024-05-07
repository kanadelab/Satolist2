using SatolistGhostBackup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;

namespace Satolist2.Utility
{
	//データ保存時のバックグラウンド ゴーストバックアップ
	internal static class BackgroundGhostBackup
	{
		public const string BackupDatabasePath = "backup.db";

		//バックアップソース-プロセスの対応表
		//さとりすとプロセス内では同一のソースから同時にバックアップをとらないようにしておく
		private static List<BackupProcess> requestedProcessList = new List<BackupProcess>();

		//バックアップが必要であれば開始する
		public static bool BackupIfNeed(string sourcePath, Action<FinishedCallbackArgs> finishedCallback)
		{
			//既に実行中であればキャンセル
			lock(requestedProcessList)
			{
				if (requestedProcessList.Exists(o => o.sourcePath == sourcePath))
				{
					return false;
				}
			}

			//バックアップ起動要件チェックのために同ソースのバックアップデータを検索
			var db = new BackupDatabase(BackupDatabasePath);
			var backupList = db.QueryBackups(sourcePath);
			var latestBackup = backupList.OrderBy(o => o.CreatedAt).FirstOrDefault();

			if(latestBackup == null)
			{
				//バックアップが無いので実施
				Backup(sourcePath, finishedCallback);
				return true;
			}

			if(latestBackup.CreatedAt + TimeSpan.FromMinutes(MainViewModel.EditorSettings.GeneralSettings.GhostBackupWithSaveIntervalMinutes) < DateTime.UtcNow)
			{
				//必要時間経過している
				Backup(sourcePath, finishedCallback);
				return true;
			}
			return false;
		}
		
		//バックアップの実施
		public static Task Backup(string sourcePath, Action<FinishedCallbackArgs> finishedCallback)
		{
			Process process;
			lock (requestedProcessList)
			{
				//別プロセスでGhostBackupを起動して、さとりすとが終了しても続行する形とする
				var psi = new ProcessStartInfo();
#if DEPLOY
				psi.FileName = "data/assembly/SatolistGhostBackup.exe";
#else
				psi.FileName = "SatolistGhostBackup.exe";
#endif
				psi.CreateNoWindow = true;
				psi.UseShellExecute = false;
				psi.Arguments = "backup";

				if(MainViewModel.EditorSettings.GeneralSettings.GhostBackupExpiryDays > 0)
				{
					psi.Arguments += $" --removeOldBackupDays {MainViewModel.EditorSettings.GeneralSettings.GhostBackupExpiryDays}";
				}
				if(MainViewModel.EditorSettings.GeneralSettings.GhostBackupMaxFileSizeKb > 0)
				{
					psi.Arguments += $" --maxFileSizeKb {MainViewModel.EditorSettings.GeneralSettings.GhostBackupMaxFileSizeKb}";
				}
				psi.Arguments += string.Format(@" ""backup.db"" ""{0}""", sourcePath);
				psi.RedirectStandardError = true;

				//プロセス開始
				process = Process.Start(psi);
				requestedProcessList.Add(new BackupProcess(process, finishedCallback, sourcePath));
			}

			//プロセス終了まち
			return Task.Run(() =>
			{
				process.WaitForExit();
				string errorMessage = process.StandardError.ReadToEnd();
				bool isError = process.ExitCode != 0;

				lock(requestedProcessList)
				{
					var item = requestedProcessList.FirstOrDefault(o => ReferenceEquals(o.process, process));
					if(item.process != null)
					{
						FinishedCallbackArgs args;
						if(isError)
						{
							args = new FinishedCallbackArgs(true, sourcePath);
							args.ErrorMessage = errorMessage;
						}
						else
						{
							args = new FinishedCallbackArgs(false, sourcePath);
						}

						//コールバックよびだし
						item.callback?.Invoke(args);
						item.process.Dispose();
						requestedProcessList.Remove(item);
					}
				}
			});
		}

		//バックアップ終了通知オブジェクト
		public class FinishedCallbackArgs
		{
			public bool IsError { get; }
			public string SourcePath { get; }
			public string ErrorMessage { get; set; }

			public FinishedCallbackArgs(bool isError, string sourcePath)
			{
				IsError = isError;
				SourcePath = sourcePath;
				ErrorMessage = string.Empty;
			}
		}

		//バックアッププロセスアイテム
		public class BackupProcess
		{
			public Process process;
			public Action<FinishedCallbackArgs> callback;
			public string sourcePath;

			public BackupProcess(Process process, Action<FinishedCallbackArgs> callback, string sourcePath)
			{
				this.process = process;
				this.callback = callback;
				this.sourcePath = sourcePath;
			}
		}
	}
}
