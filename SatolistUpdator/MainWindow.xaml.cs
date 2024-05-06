using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace SatolistUpdator
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		internal new MainWindowViewModel DataContext
		{
			get => (MainWindowViewModel)base.DataContext;
			set
			{
				base.DataContext = value;
			}
		}

		public MainWindow()
		{
			InitializeComponent();
			DataContext = new MainWindowViewModel(this);
			Closing += MainWindow_Closing;
		}

		private void MainWindow_Closing(object sender, CancelEventArgs e)
		{
			//閉じて良いタイミングだけ許容
			if (!DataContext.ShowCloseButton)
				e.Cancel = true;
			DataContext.Cleanup();
		}
	}


	internal class MainWindowViewModel : NotificationObject
	{
		private const string FileListPath = "data/files";
		private const string ZipFileName = "downloaded.zip";
		private const string BackupDirectoryName = "backup";
		private const string ExtractDirectoryName = "extract";

		private MainWindow MainWindow;
		private string message;
		private bool showUpdateButton;
		private bool showCloseButton;
		private bool showBootSatolistButton;
		private bool showProgressBar;

		public List<string> CurrentFileList;
		public string SatolistDirectory;
		public string BackupDirectory;
		public string ExtractDirectory;
		public string DownloadedZipPath;
		public string currentTemporaryDirectory;

		public string TargetUrl;				//リリースzipのURL
		public string RequestedImagePath;       //更新をリクエストしてきたさとりすとのURL

		public CancellationTokenSource canceller;

		public ActionCommand CancelCommand { get; }
		public ActionCommand UpdateButtonCommand { get; }
		public ActionCommand CloseButtonCommand { get; }
		public ActionCommand BootSatolistButtonCommand { get; }

		//更新メッセージの表示
		public string Message
		{
			get => message;
			private set
			{
				message = value;
				NotifyChanged();
			}
		}

		//更新開始ボタンの表示
		public bool ShowUpdateButton
		{
			get => showUpdateButton;
			set
			{
				showUpdateButton = value;
				NotifyChanged();
			}
		}

		//閉じるボタンの表示
		public bool ShowCloseButton
		{
			get => showCloseButton;
			set
			{
				showCloseButton = value;
				NotifyChanged();
			}
		}

		//さとりすとを起動(閉じる)ボタンの表示
		public bool ShowBootSatolistButton
		{
			get => showBootSatolistButton;
			set
			{
				showBootSatolistButton = value;
				NotifyChanged();
			}
		}

		//キャンセルボタンの表示
		public bool ShowCancelButton
		{
			get => canceller != null;
		}

		//プログレスバーの表示
		public bool ShowProgressBar
		{
			get => showProgressBar;
			set
			{
				showProgressBar = value;
				NotifyChanged();
			}
		}


		public MainWindowViewModel(MainWindow main)
		{
			MainWindow = main;
			showProgressBar = true;

			if(IsRunningDuplecatedUpdatorProcess())
			{
				//自分以外が実行されていたら強制終了
				Environment.Exit(1);
			}

			var commands = Environment.GetCommandLineArgs();
			if(commands.Length < 3)
				Environment.Exit(1);

			//パス関連の設定 ワーキングディレクトリがさとりすとの位置になり、アセンブリ位置がテンポラリになるのでそれぞれ設定する
			RequestedImagePath = commands[1];
			TargetUrl = commands[2];

			var executableDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			currentTemporaryDirectory = Path.Combine(executableDirectory, Path.GetRandomFileName());
			DownloadedZipPath = Path.Combine(currentTemporaryDirectory, ZipFileName);
			BackupDirectory = Path.Combine(currentTemporaryDirectory, BackupDirectoryName);
			ExtractDirectory = Path.Combine(currentTemporaryDirectory, ExtractDirectoryName);
			SatolistDirectory = Environment.CurrentDirectory;

			//クリーンアップ
			Cleanup();
			Directory.CreateDirectory(currentTemporaryDirectory);
			Directory.CreateDirectory(BackupDirectory);
			Directory.CreateDirectory(ExtractDirectory);

			CancelCommand = new ActionCommand(
				o =>
				{
					canceller?.Cancel();
				});

			UpdateButtonCommand = new ActionCommand(
				o =>
				{
					Step2_UpdateFiles();
				}
				);
			CloseButtonCommand = new ActionCommand(
				o =>
				{
					MainWindow.Close();
				}
				);
			BootSatolistButtonCommand = new ActionCommand(
				o =>
				{
					Process.Start("Satolist2.exe");
					MainWindow.Close();
				});

			//まずはDL開始
			Step1_DownloadZip();
		}

		private void NotifyRetry()
		{
			//初期の表示に戻す
			Message = "さとりすとを更新します。よろしいですか？";
			ShowUpdateButton = true;
			ShowCloseButton = true;
			ShowProgressBar = false;

			canceller?.Dispose();
			canceller = null;
			NotifyChanged(nameof(ShowCancelButton));
		}

		private void NotifyFailed()
		{
			//失敗の表示系に切り替える
			ShowUpdateButton = false;
			ShowBootSatolistButton = false;
			ShowCloseButton = true;
			ShowProgressBar = false;
		}

		//zipをダウンロード
		private void Step1_DownloadZip()
		{
			Message = "更新ファイルをダウンロード中...";

			Task.Run(() =>
		   {
			   try
			   {
				   WebClient client = new WebClient();
				   client.DownloadFile(TargetUrl, DownloadedZipPath);

				   MainWindow.Dispatcher.Invoke(() =>
				   {
					   //完了を通知
					   Message = "さとりすとを更新します。よろしいですか？";
					   ShowUpdateButton = true;
					   ShowCloseButton = true;
					   ShowProgressBar = false;
				   }
				   );
			   }
			   catch
			   {
				   MainWindow.Dispatcher.Invoke(() =>
				   {
					   //失敗を通知
					   Message = "更新ファイルのダウンロードに失敗しました。";
					   NotifyFailed();
				   }
				   );
			   }
		   }
			);
		}

		//実際のアップデート
		private void Step2_UpdateFiles()
		{
			ShowCloseButton = false;
			ShowUpdateButton = false;
			ShowProgressBar = true;
			Message = "さとりすとが終了していることを確認中...";

			canceller = new CancellationTokenSource();
			NotifyChanged(nameof(ShowCancelButton));
			var cancellationToken = canceller.Token;

			Task.Run(() =>
			{
				try
				{
					while (true)
					{
						cancellationToken.WaitHandle.WaitOne(3000);
						var editorProcessName = FindRunningDuplecatedEditorProcess();
						var fileName = Path.GetFileName(editorProcessName);

						if(cancellationToken.IsCancellationRequested)
						{
							//キャンセル
							NotifyRetry();
							return;
						}

						if (fileName != null)
						{
							if (fileName == "SatolistGhostBackup.exe")
							{
								MainWindow.Dispatcher.Invoke(() =>
								{
									Message = "ゴーストバックアップの完了を待機中...";
								});
								continue;
							}
							else
							{
								MainWindow.Dispatcher.Invoke(() =>
								{
									MessageBox.Show("アップデートするには起動中の「さとりすと」と「GhostDeploy」をすべて終了してください。", "さとりすと ネットワーク更新", MessageBoxButton.OK, MessageBoxImage.Information);
									NotifyRetry();
								});
								return;
							}
						}
					}
				}
				catch
				{
				}

				MainWindow.Dispatcher.Invoke(() =>
				{
					Message = "アップデート中...";
				});

				try
				{
					//バックアップをとる
					Step2_1_BackupFiles();

					try
					{
						Step2_2_ExtractZip();
					}
					catch
					{
						Step2_Ex_RestoreBackup();
						throw;
					}

					MainWindow.Dispatcher.Invoke(() =>
				   {
					   Message = "アップデートが完了しました。";
					   ShowProgressBar = false;
					   ShowCloseButton = true;
					   ShowBootSatolistButton = true;
				   }
					);
				}
				catch
				{
					MainWindow.Dispatcher.Invoke(() =>
					{
						Message = "アップデートに失敗しました。PCの再起動や手動の上書き更新を試すと良いかもしれません。";
						NotifyFailed();
					}
					);
				}
			}
			);
		}

		//ファイルリストを設定
		private string[] ReadFileList(string path)
		{
			return File.ReadAllLines(path);
		}

		//さとりすとのプロセスが実行されているかをチェック
		public string FindRunningDuplecatedEditorProcess()
		{
			var executableDirectory = Path.GetDirectoryName(RequestedImagePath);
			var process = Process.GetProcesses();
			var editorProcess = process.FirstOrDefault(o =>
			{
				try
				{
					return o.MainModule.FileName.StartsWith(executableDirectory);
				}
				catch
				{
					return false;
				}
			});

			if (editorProcess != null)
			{
				return editorProcess.MainModule.FileName;
			}
			return null;
		}

		//自身のプロセスが他に実行されているかチェック
		public bool IsRunningDuplecatedUpdatorProcess()
		{
			var selfExecutable = Assembly.GetExecutingAssembly().Location;

			var process = Process.GetProcesses();
			if (process.Where(o =>
			{
				try
				{
					return o.MainModule.FileName == selfExecutable;
				}
				catch
				{
					return false;
				}
			}).Count() > 1)
			{
				return true;
			}

			return false;
		}

		public void Step2_1_BackupFiles()
		{
			CurrentFileList = new List<string>();
			CurrentFileList.Add(FileListPath);  //ファイルリストそのものは対象外になっているので追加
			CurrentFileList.AddRange(ReadFileList(FileListPath));

			foreach (var file in CurrentFileList)
			{
				if (string.IsNullOrEmpty(file))
					continue;
				var filePath = Path.Combine(SatolistDirectory, file);

				//バックアップ先にコピー
				var backupFilePath = Path.Combine(BackupDirectory, file);

				//できる限り
				try
				{
					Directory.CreateDirectory(Path.GetDirectoryName(backupFilePath));
					File.Copy(filePath, backupFilePath, true);
				}
				catch { }
			}
		}

		//zipを解凍
		public void Step2_2_ExtractZip()
		{
			//ファイルを解凍
			ZipFile.ExtractToDirectory(DownloadedZipPath, ExtractDirectory);

			//ファイルリストをロード
			List<string> updateFileList = new List<string>();
			updateFileList.Add(FileListPath);
			updateFileList.AddRange(ReadFileList(Path.Combine(ExtractDirectory, FileListPath)));

			foreach(var file in updateFileList)
			{
				if (string.IsNullOrEmpty(file))
					continue;

				//ファイルを対象にコピー
				var targetFilePath = Path.Combine(SatolistDirectory, file);
				var extractFilePath = Path.Combine(ExtractDirectory, file);
				Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath));
				File.Copy(extractFilePath, targetFilePath, true);
			}
		}

		//失敗時にバックアップから復元
		public void Step2_Ex_RestoreBackup()
		{
			foreach (var file in CurrentFileList)
			{
				if (string.IsNullOrEmpty(file))
					continue;
				var filePath = Path.Combine(SatolistDirectory, file);

				//バックアップ先にコピー
				var backupFilePath = Path.Combine(BackupDirectory, file);
				try
				{
					Directory.CreateDirectory(Path.GetDirectoryName(backupFilePath));
					File.Copy(backupFilePath, filePath, true);
				}
				catch { }	//できるだけ復元する
			}
		}

		//バックアップ完了後、バックアップ元のファイルを削除
		public void Cleanup()
		{
			try
			{
				Directory.Delete(currentTemporaryDirectory, true);
			}
			catch { }
		}

	}

	//汎用機能をさとりすとからベタ移植

	public class NotificationObject : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		protected void NotifyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class ActionCommand : ICommand
	{
		public event EventHandler CanExecuteChanged;
		private Action<object> executeAction;
		private Func<object, bool> canExecuteFunc;

		public ActionCommand(Action<object> execute, bool canExecute = true)
		{
			executeAction = execute;
			canExecuteFunc = (o) => canExecute;
		}

		public ActionCommand(Action<object> execute, Func<object, bool> canExecute)
		{
			executeAction = execute;
			canExecuteFunc = canExecute;
		}


		public bool CanExecute(object parameter)
		{
			if (canExecuteFunc != null)
			{
				return canExecuteFunc.Invoke(parameter);
			}
			return true;
		}

		public void Execute(object parameter)
		{
			executeAction?.Invoke(parameter);
		}

		public void NotifyCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, new EventArgs());
		}
	}


}
