using Satolist2.Dialog;
using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GhostDeploy
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		public new MainViewModel DataContext
		{
			get => (MainViewModel)base.DataContext;
			set => base.DataContext = value;
		}

		public MainWindow()
		{
			InitializeComponent();
			AllowDrop = true;
			DragEnter += MainWindow_DragEnter;
			Drop += MainWindow_Drop;
			DataContext = new MainViewModel(this);
		}

		private void MainWindow_Drop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				if (e.Data.GetData(DataFormats.FileDrop) is string[] items)
				{
					DataContext.DirectoryPath = items.FirstOrDefault();
					e.Effects = DragDropEffects.Copy;
				}
			}
		}

		private void MainWindow_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effects = DragDropEffects.Copy;
			}
		}
	}

	public class MainViewModel : NotificationObject
	{
		private string directoryPath;
		private MainWindow window;

		//アップロード元のフルパス
		public string DirectoryPath
		{
			get => directoryPath;
			set
			{
				directoryPath = value;
				NotifyChanged();
				UploadCommand.NotifyCanExecuteChanged();
			}
		}

		public ActionCommand UploadCommand { get; }
		public ActionCommand UploadSettingCommand { get; }

		public MainViewModel(MainWindow mainWindow)
		{
			window = mainWindow;

			UploadCommand = new ActionCommand(
				o =>
				{
					UploadServerSettingModelBase[] settings = null;
					try
					{
						settings = UploadSetting.Load();
					}
					catch
					{
						MessageBox.Show("アップロードのロードに失敗しました。\r\n「設定」→「アップロード設定」でアップロード設定を構成してください。", "GhostDeploy");
						return;
					}

					if (!(settings?.Any() ?? false))
					{
						MessageBox.Show("アップロード設定がありません。\r\n「設定」→「アップロード設定」でアップロード設定を構成してください。", "GhostDeploy");
						return;
					}

					if(!System.IO.Directory.Exists(DirectoryPath))
					{
						MessageBox.Show("アップロードするフォルダがありません。", "GhostDeploy", MessageBoxButton.OK, MessageBoxImage.Exclamation);
						return;
					}

					//ローカル設定を持ってくる
					GhostLocalSettings ghostLocal = null;
					var settingPath = MakeSettingsPath(DirectoryPath);
					try
					{
						ghostLocal = GhostLocalSettings.Load(settingPath);
					}
					catch
					{
					}

					if(ghostLocal == null)
					{
						ghostLocal = new GhostLocalSettings();
					}

					//設定値のロード
					var dialog = new UploadDialog(settings, DirectoryPath, ghostLocal);
					dialog.Owner = window;
					dialog.ShowDialog();

					//profile更新
					if(dialog.IsUploadStarted)
					{
						ghostLocal.LastUploadSettingId = dialog.DataContext.SelectedSettingId;
						ghostLocal.LastUploadUseDiff = dialog.DataContext.IsDiffUpload;
						ghostLocal.LastUploadUseFiles = dialog.DataContext.IsUploadFiles;
						ghostLocal.LastUploadUseNar = dialog.DataContext.IsUploadNar;

						GhostLocalSettings.Save(settingPath, ghostLocal);
					}
				},
				o =>
				{
					return !string.IsNullOrEmpty(DirectoryPath);
				});

			UploadSettingCommand = new ActionCommand(
				o =>
				{
					UploadServerSettingModelBase[] settings = Array.Empty<UploadServerSettingModelBase>();
					bool isContinue = true;
					try
					{
						settings = UploadSetting.Load();
					}
					catch
					{
						if (MessageBox.Show("アップロードのロードに失敗しました。\r\n設定をクリアして再設定するようにしてもいいですか？", "さとりすと", MessageBoxButton.YesNo, MessageBoxImage.Warning)
							== MessageBoxResult.No)
						{
							isContinue = false;
						}
					}

					if (!isContinue)
						return;

					var d = new Satolist2.Dialog.UploadSettingDialog(settings);
					d.Owner = window;
					if (d.ShowDialog() == true)
					{
						settings = d.DataContext.GetItems();
						UploadSetting.Save(settings);
					}
				});
		}

		//profileを置くフォルダを検出
		//ゴーストならghost/masterに、それ以外ならその場に配置
		private string MakeSettingsPath(string path)
		{
			var ghostMasterPath = Satolist2.Utility.DictionaryUtility.ConbinePath(path, "ghost/master");
			if (System.IO.Directory.Exists(ghostMasterPath))
				return ghostMasterPath;
			else
				return path;
		}
	}
}
