using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Satolist.GhostBackup;
using Satolist.Utility;
using Satolist2.Utility;
using SatolistGhostBackup;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

namespace Satolist2.Dialog
{
	/// <summary>
	/// BackupList.xaml の相互作用ロジック
	/// </summary>
	public partial class BackupList : DialogContentBase
	{
		public BackupList()
		{
			InitializeComponent();
		}
	}

	//メインViewModel
	internal class BackupListViewModel : NotificationObject
	{
		private BackupList dialog;
		private MainViewModel main;

		private BackupDatabase db;
		private List<BackupSourceViewModel> sources;
		private Dictionary<string, BackupFileViewModel[]> fileListCache;
		private List<BackupFileViewModel> files;
		private BackupItemViewModel selectedItem;
		private bool showCurrentGhostOnly;

		public ICollectionView Items { get; }
		public ICollectionView Files { get; }
		public string BackupSettingSummary
		{
			get
			{
				if(MainViewModel.EditorSettings.GeneralSettings.GhostBackupExpiryDays > 0)
				{
					return $"さとりすとが作成したゴーストのバックアップデータの一覧です。任意のフォルダにバックアップを書き出すことができます。\r\nバックアップは{MainViewModel.EditorSettings.GeneralSettings.GhostBackupExpiryDays}日経過後、次のバックアップ時に自動で削除されます。保持期限はオプションの基本設定から変更できます。";
				}
				else
				{
					return string.Empty;
				}
			}
		}

		public BackupItemViewModel SelectedItem
		{
			get => selectedItem;
			set
			{
				selectedItem = value;
				NotifyChanged();
				ExtractBackupCommand.NotifyCanExecuteChanged();
			}
		}

		public bool ShowCurrentGhostOnly
		{
			get => showCurrentGhostOnly;
			set
			{
				if(showCurrentGhostOnly != value)
				{
					showCurrentGhostOnly = value;
					LoadBackup();
					NotifyChanged();
				}
			}
		}

		public bool HasCurrentGhost => main.Ghost != null;

		public ActionCommand ExtractBackupCommand { get; }
		public ActionCommand RemoveBackupCommand { get; }
		public ActionCommand ExportFileCommand { get; }
		public ActionCommand CreateBackupCommand { get; }

		public BackupListViewModel(BackupList dialog, MainViewModel main)
		{
			this.main = main;
			this.dialog = dialog;

			db = new BackupDatabase(BackgroundGhostBackup.BackupDatabasePath);
			sources = new List<BackupSourceViewModel>();
			Items = CollectionViewSource.GetDefaultView(sources);

			fileListCache = new Dictionary<string, BackupFileViewModel[]>();
			files = new List<BackupFileViewModel>();
			Files = CollectionViewSource.GetDefaultView(files);
			showCurrentGhostOnly = main.Ghost != null;

			ExtractBackupCommand = new ActionCommand(
				o =>
				{
					ExtractBackup((BackupItemViewModel)o);
				},
				o =>
				{
					return o != null;
				});

			RemoveBackupCommand = new ActionCommand(
				o =>
				{
					RemoveBackup((BackupItemViewModel)o);
				},
				o =>
				{
					return o != null;
				});

			ExportFileCommand = new ActionCommand(
				o =>
				{
					ExportFile((BackupFileViewModel)o);
				});

			CreateBackupCommand = new ActionCommand(
				o =>
				{
					CreateBackup();
				},
				o => main.Ghost != null);

			LoadBackup();
		}

		//手動バックアップ
		private void CreateBackup()
		{
			if (main.Ghost == null)
				return;

			if(MessageBox.Show(dialog.Host, $"ゴースト「{main.Ghost.GhostDescriptName}」のバックアップ作成しますか？", "バックアップ", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
			{
				//キャンセル
				return;
			}

			var progressDialog = new ProgressDialog();
			progressDialog.Owner = dialog.Host;
			progressDialog.DataContext.Title = "バックアップの作成";
			progressDialog.DataContext.SetProgress(new Progress() { Message = "バックアップを作成中です。\nゴーストのフォルダサイズによって時間がかかる場合があります。", UseProgress = false });
			var task = BackgroundGhostBackup.Backup(main.Ghost.FullPath, (e) =>
			{
				dialog.Dispatcher.Invoke(() =>
				{ 
					if (e.IsError)
					{
						progressDialog.DataContext.SetMessage(e.ErrorMessage);
						progressDialog.DataContext.SetMessage("バックアップ作成に失敗しました。", 0.0);
					}
					else
					{
						progressDialog.DataContext.SetMessage("バックアップ作成が完了しました。", 100.0);
					}
				});
			});
			progressDialog.SetTask(task);
			progressDialog.ShowDialog();

			//バックアップ終了後、リストを更新する
			LoadBackup();
		}

		//単体ファイルを出力
		private void ExportFile(BackupFileViewModel file)
		{
			//出力先フォルダ指定
			var fileDialog = new SaveFileDialog();
			var exportExtension = Path.GetExtension(file.Path);
			fileDialog.Filter = $"出力ファイル形式(*.{exportExtension})|*.{exportExtension}|すべてのファイル|*.*";
			fileDialog.AddExtension = true;
			fileDialog.OverwritePrompt = true;
			fileDialog.FileName = Path.GetFileName(file.Path);

			if(fileDialog.ShowDialog() == true)
			{
				var targetPath = fileDialog.FileName;
				db.ExportFile(file.BackupId, file.Path, targetPath, true);

				//出力先をオープン
				WindowsExplorer.OpenDirectoryFileSelect(targetPath, true);
			}
		}

		//バックアップを削除
		private void RemoveBackup(BackupItemViewModel item)
		{
			if (MessageBox.Show(dialog.Host, "このバックアップを本当に削除しますか？", "バックアップの削除", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
			{
				db.RemoveBackup(item.BackupId);
				LoadBackup();
			}
		}

		//バックアップアーカイブをフォルダに展開
		private void ExtractBackup(BackupItemViewModel item)
		{
			//出力先フォルダ指定
			var dirDialog = new CommonOpenFileDialog();
			dirDialog.IsFolderPicker = true;
			
			if(dirDialog.ShowDialog(dialog.Host) == CommonFileDialogResult.Ok)
			{
				var targetPath = dirDialog.FileName;

				//フォルダが空かどうかチェック
				if(Directory.EnumerateFileSystemEntries(targetPath).Any())
				{
					if(MessageBox.Show(dialog.Host, "選択したフォルダが空ではないようです。\r\n解凍データを上書きしてもいいですか？", "上書き確認", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
					{
						//上書きキャンセル
						return;
					}
				}

				//何かしらの進捗表示
				var progressDialog = new ProgressDialog();
				progressDialog.Owner = dialog.Host;
				progressDialog.DataContext.Title = "バックアップの書き出し";
				progressDialog.DataContext.SetProgress(new Progress() { Message = "バックアップの書き出しを開始", UseProgress = false });
				var task = System.Threading.Tasks.Task.Run(() =>
				{
					try
					{
						//展開実行
						db.ExtractToDirectory(item.BackupId, targetPath, true);
						progressDialog.Dispatcher.Invoke(() =>
						{
							progressDialog.DataContext.SetMessage("作成完了しました。", 100.0);
						});

						//出力先をオープン
						WindowsExplorer.OpenDirectory(targetPath, true);
					}
					catch
					{
						progressDialog.Dispatcher.Invoke(() =>
						{
							progressDialog.DataContext.SetMessage("エラーが発生しました。");
						});
					}
				});
				progressDialog.SetTask(task);
				progressDialog.ShowDialog();
			}
		}

		public void LoadBackup()
		{
			//DB問い合わせ
			BackupData[] backupItems;
			if(showCurrentGhostOnly)
			{
				backupItems = db.QueryBackups(main.Ghost.FullPath);
			}
			else
			{
				backupItems = db.QueryBackups();
			}

			//アイテム追加
			sources.Clear();
			foreach(var source in backupItems.GroupBy(o => o.SourcePath))
			{
				var item = new BackupSourceViewModel(source.Key, source.Select(o => new BackupItemViewModel(o.BackupId, o.CreatedAt, o.SourcePath, o.SizeBytes, this)), this);
				sources.Add(item);
			}

			//アイテムが１個しかない場合は、デフォルトで展開にしておく
			if(sources.Count == 1)
			{
				sources.First().IsExpanded = true;
			}
			Items.Refresh();

			//ファイルビューを一旦削除
			files.Clear();
			Files.Refresh();
		}

		//バックアップを選択した際にファイルリストを表示
		public void SelectedItemChanged(BackupItemViewModel item)
		{
			//アイテム選択
			SelectedItem = item;

			//バックアップIDとファイルリストの交換をキャッシュする
			if(fileListCache.TryGetValue(item.BackupId, out var items))
			{
				files.Clear();
				files.AddRange(items);
				Files.Refresh();
				return;
			}

			//データベース問い合わせ
			var fileList = db.QueryFiles(item.BackupId);
			items = fileList.Select(o => new BackupFileViewModel(o)).ToArray();
			fileListCache.Add(item.BackupId, items);
			files.Clear();
			files.AddRange(items);
			Files.Refresh();
		}
	}

	//バックアップソース単位の情報
	internal class BackupSourceViewModel : NotificationObject
	{
		private ObservableCollection<BackupItemViewModel> items;
		private bool isSelected;
		private bool isExpanded;
		private BackupListViewModel root;

		public ReadOnlyObservableCollection<BackupItemViewModel> Items { get; }
		public string SourcePath { get; }

		public bool IsSelected
		{
			get => isSelected;
			set
			{
				isSelected = value;
				NotifyChanged();
			}
		}

		public bool IsExpanded
		{
			get => isExpanded;
			set
			{
				isExpanded = value;
				NotifyChanged();
			}
		}

		public string Type => "BackupSource";
		

		public BackupSourceViewModel(string sourcePath, IEnumerable<BackupItemViewModel> backups, BackupListViewModel root)
		{
			this.root = root;
			SourcePath = sourcePath;
			items = new ObservableCollection<BackupItemViewModel>(backups);
			Items = new ReadOnlyObservableCollection<BackupItemViewModel>(items);
		}
	}

	//バックアップ単位の情報
	internal class BackupItemViewModel : NotificationObject
	{
		private bool isSelected;
		private bool isExpanded;
		private BackupListViewModel root;

		public DateTime CreatedAt { get; }
		public string BackupId { get; }
		public string SourcePath { get; }
		public long ArchiveSizeMegaBytes { get; }

		public string CreatedAtStr => CreatedAt.ToString();

		public bool IsSelected
		{
			get => isSelected;
			set
			{
				isSelected = value;
				NotifyChanged();

				if(isSelected)
				{
					//選択されたらファイルリストを表示する
					root.SelectedItemChanged(this);
				}
			}
		}

		public bool IsExpanded
		{
			get => isExpanded;
			set
			{
				isExpanded = value;
				NotifyChanged();
			}
		}

		public object[] Items => Array.Empty<object>();
		public string Type => "BackupItem";

		public BackupItemViewModel(string backupId, DateTime createdAt, string sourcePath, long archiveSizeBytes, BackupListViewModel root)
		{
			this.root = root;
			CreatedAt = createdAt.ToLocalTime();
			BackupId = backupId;
			SourcePath = sourcePath;
			ArchiveSizeMegaBytes = archiveSizeBytes / (1024*1024);
		}
	}

	//バックアップ内ファイルの情報
	public class BackupFileViewModel
	{
		private BackupFile model;

		public string Path => model.Path;
		public long SizeBytes => model.SizeBytes;
		public DateTime UpdatedAt => model.UpdatedAt;
		public string BackupId => model.BackupId;

		public string UpdatedAtStr => UpdatedAt.ToString();

		public BackupFileViewModel(BackupFile backup)
		{
			model = backup;
		}
	}
}
