using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
using System.Windows.Shapes;

namespace Satolist2.Dialog
{
	/// <summary>
	/// UploadDialog.xaml の相互作用ロジック
	/// </summary>
	public partial class UploadDialog : DialogContentBase
	{
		private bool isUploading = false;
		public Model.GhostModel Ghost { get; }
		public CancellationTokenSource Cancellation { get; }
		//アップロードするところまで進んだか（設定更新フラグ）
		public bool IsUploadStarted { get; private set; }
		public new DialogViewModel DataContext
		{
			get => (DialogViewModel)base.DataContext;
			set => base.DataContext = value;
		}

		internal UploadDialog(Model.UploadServerSettingModelBase[] servers, MainViewModel main, Model.GhostLocalSettings ghostSettings)
		{
			Ghost = main.Ghost;
			Cancellation = new CancellationTokenSource();
			InitializeComponent();
			Owner = main.MainWindow.RootWindow;
			DataContext = new DialogViewModel();
			Closing += UploadDialog_Closing;

			//viewmodel作成
			DataContext.Items = servers.Select<Model.UploadServerSettingModelBase, object>(
				o =>
				{
					if (o is Model.FtpServerSettingModel ftp)
						return new FtpServerViewModel(ftp);
					else if (o is Model.NarnaloaderV2ServerSettingModel nnl)
						return new NarnaloaderV2ServerViewModel(nnl);
					Debug.Assert(false);
					return null;
				}
				).ToArray();

			if (ghostSettings != null)
			{
				DataContext.IsDiffUpload = ghostSettings.LastUploadUseDiff;
				DataContext.IsUploadFiles = ghostSettings.LastUploadUseFiles;
				DataContext.IsUploadNar = ghostSettings.LastUploadUseNar;

				//選択中アイテムの検出
				foreach(var item in DataContext.Items)
				{
					if(item is FtpServerViewModel ftp)
					{
						var selectedItem = ftp.Items.FirstOrDefault(o => o.SettingId == ghostSettings.LastUploadSettingId);
						if (selectedItem != null)
						{
							selectedItem.IsSelected = true;
						}
					}
					else if(item is NarnaloaderV2ServerViewModel nnl)
					{
						var selectedItem = nnl.Items.FirstOrDefault(o => o.SettingId == ghostSettings.LastUploadSettingId);
						if (selectedItem != null)
						{
							selectedItem.IsSelected = true;
						}
					}
				}
				
			}
			ServerList.Focus();
			SelectUploadTargetPage.Visibility = Visibility.Visible;
		}

		private void UploadDialog_Closing(object sender, CancelEventArgs e)
		{
			//アップロード中は閉じられない
			if (isUploading)
			{
				e.Cancel = true;
			}
		}

		public class DialogViewModel : Utility.NotificationObject
		{
			private string password;
			private string logs;
			private double progressValue;
			private bool useProgressBar;
			private bool isUploadFiles;
			private bool isUploadNar;
			private bool isDiffUpload;

			public object[] Items { get; set; }
			public string Logs
			{
				get => logs;
				set
				{
					logs = value;
					NotifyChanged();
				}
			}

			public double ProgressValue
			{
				get => progressValue;
				set
				{
					progressValue = value;
					NotifyChanged();
				}
			}

			public bool UseProgressBar
			{
				get => useProgressBar;
				set
				{
					useProgressBar = value;
					NotifyChanged();
				}
			}

			public bool IsUploadFiles
			{
				get => isUploadFiles;
				set
				{
					isUploadFiles = value;
					NotifyChanged();
					NotifyChanged(nameof(IsEnableUpload));
				}
			}

			public bool IsUploadNar
			{
				get => isUploadNar;
				set
				{
					isUploadNar = value;
					NotifyChanged();
					NotifyChanged(nameof(IsEnableUpload));
				}
			}

			public bool IsDiffUpload
			{
				get => isDiffUpload;
				set
				{
					isDiffUpload = value;
					NotifyChanged();
				}
			}

			//アップロードに進めるか(narと更新ファイルいずれかの選択が必要)
			public bool IsEnableUpload
			{
				get => IsUploadFiles || isUploadNar;
			}

			public string Password
			{
				get => password;
				set
				{
					password = value;
					NotifyChanged();
				}
			}

			//使用した設定ID
			public string SelectedSettingId { get; set; }
		}

		//ツリーアイテム共通
		private class TreeItemViewModel : NotificationObject
		{
			private bool isSelected;

			public bool IsSelected
			{
				get => isSelected;
				set
				{
					isSelected = value;
					NotifyChanged();
				}
			}
		}

		private class FtpServerViewModel : TreeItemViewModel
		{
			private Model.FtpServerSettingModel model;
			private FtpItemViewModel[] items;

			public IEnumerable<FtpItemViewModel> Items => items;

			public string Label => model.Label;
			public string Url => model.Url;
			public bool AlwaysPasswordInput => model.AlwaysPasswordInput;
			public string ItemType => model.ItemType;
			public string Username => model.UserName;
			public string Password => model.Password;

			public FtpServerViewModel(Model.FtpServerSettingModel model)
			{
				this.model = model;
				items = model.Items.Select(o => new FtpItemViewModel(o, this)).ToArray();
			}
		}

		private class NarnaloaderV2ServerViewModel : TreeItemViewModel
		{
			private Model.NarnaloaderV2ServerSettingModel model;
			private NarnaloaderV2ItemViewModel[] items;
			public IEnumerable<NarnaloaderV2ItemViewModel> Items => items;
			public string Label => model.Label;
			public string Url => model.Url;
			public bool AlwaysPasswordInput => model.AlwaysPasswordInput;
			public string ItemType => model.ItemType;
			public string Username => model.UserName;
			public string Password => model.Password;

			public NarnaloaderV2ServerViewModel(Model.NarnaloaderV2ServerSettingModel model)
			{
				this.model = model;
				items = model.Items.Select(o => new NarnaloaderV2ItemViewModel(o, this)).ToArray();
			}
		}
		
		private class FtpItemViewModel : TreeItemViewModel
		{
			private Model.FtpItemSettingModel model;

			public FtpServerViewModel Parent { get; }
			public string Label => model.Label;
			public string UpdatePath => model.UpdatePath;
			public string NarPath => model.NarPath;
			public string ItemType => model.ItemType;
			public string SettingId => model.SettingId;

			public FtpItemViewModel(Model.FtpItemSettingModel model, FtpServerViewModel parent)
			{
				this.model = model;
				Parent = parent;
			}
		}

		private class NarnaloaderV2ItemViewModel: TreeItemViewModel
		{
			private Model.NarnaloaderV2ItemSettingModel model;
			public NarnaloaderV2ServerViewModel Parent { get; }
			public string Label => model.Label;
			public string ItemId => model.ItemId;
			public string ItemType => model.ItemType;
			public string SettingId => model.SettingID;

			public NarnaloaderV2ItemViewModel(Model.NarnaloaderV2ItemSettingModel model, NarnaloaderV2ServerViewModel parent)
			{
				this.model = model;
				Parent = parent;
			}
		}

		//-- キャンセル(アップロード中を除く)
		private void Cancel(object sender, RoutedEventArgs e)
		{
			Close();
		}

		//アップロード中のキャンセルリクエスト
		private void CancelUploading(object sender, RoutedEventArgs e)
		{
			DataContext.Logs += "キャンセルのリクエスト中..." + "\r\n";
			Cancellation?.Cancel();
		}

		//アップロード完了からの閉じる
		private void UploadEnd(object sender, RoutedEventArgs e)
		{
			Close();
		}
		

		//-- アップロード対象選択
		private void SelectUploadTargetPage_Next(object sender, RoutedEventArgs e)
		{
			if(ServerList.SelectedItem is NarnaloaderV2ItemViewModel nnl)
			{
				if(!nnl.Parent.AlwaysPasswordInput)
				{
					//パスワード入力しない場合は、アップロード確認画面まで進める
					SelectUploadTargetPage.Visibility = Visibility.Collapsed;
					UploadConfirmPage.Visibility = Visibility.Visible;
					return;
				}
			}

			//次のページ
			SelectUploadTargetPage.Visibility = Visibility.Collapsed;
			SetupOptionsPage.Visibility = Visibility.Visible;
		}

		private void ServerList_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			//アップロード対象として有効なアイテムが選択されてるときだけ
			if (ServerList.SelectedItem is FtpItemViewModel ftp)
			{
				SelectUploadTargetPage_NextButton.IsEnabled = true;
				DataContext.SelectedSettingId = ftp.SettingId;
			}
			else if(ServerList.SelectedItem is NarnaloaderV2ItemViewModel nnl)
			{
				SelectUploadTargetPage_NextButton.IsEnabled = true;
				DataContext.SelectedSettingId = nnl.SettingId;
			}
			else
			{
				SelectUploadTargetPage_NextButton.IsEnabled = false;
			}
		}

		//--アップロードセットアップ
		private void SetupOptionsPage_Back(object sender, RoutedEventArgs e)
		{
			SetupOptionsPage.Visibility = Visibility.Collapsed;
			SelectUploadTargetPage.Visibility = Visibility.Visible;
		}

		private void SetupOptionsPage_Next(object sender, RoutedEventArgs e)
		{
			SetupOptionsPage.Visibility = Visibility.Collapsed;
			UploadConfirmPage.Visibility = Visibility.Visible;
		}

		//--アップロード確認
		private void UploadConfirm_Back(object sender, RoutedEventArgs e)
		{
			//ななろだでパスワード入力済みの場合は、最初に戻る
			if (ServerList.SelectedItem is NarnaloaderV2ItemViewModel nnl)
			{
				if (!nnl.Parent.AlwaysPasswordInput)
				{
					UploadConfirmPage.Visibility = Visibility.Collapsed;
					SelectUploadTargetPage.Visibility = Visibility.Visible;
				}
			}
			else
			{
				SetupOptionsPage.Visibility = Visibility.Visible;
			}

			UploadConfirmPage.Visibility = Visibility.Collapsed;
		}

		private void UploadConfirm_Next(object sender, RoutedEventArgs e)
		{
			isUploading = true;
			UploadingPage.Visibility = Visibility.Visible;
			UploadConfirmPage.Visibility = Visibility.Collapsed;
			IsUploadStarted = true;

			if (ServerList.SelectedItem is FtpItemViewModel ftp)
			{
				DoUploadFtp(ftp, DataContext.Password, DataContext.IsUploadFiles, DataContext.IsUploadNar, DataContext.IsDiffUpload);
			}
			else if(ServerList.SelectedItem is NarnaloaderV2ItemViewModel nnl)
			{
				DoUploadNarnaloaderV2(nnl, DataContext.Password);
			}
			else
			{
				Debug.Assert(false);	//おかしい
			}
		}

		private void DoUploadFtp(FtpItemViewModel ftp, string inputPassword, bool updateFiles, bool updateNar, bool useDiff)
		{
			Task.Run(() =>
		   {
			   try
			   {
				   using (var ftpClient = new Core.FtpClient(new Core.FtpAccount() { url = ftp.Parent.Url, username = ftp.Parent.Username, password = ftp.Parent.AlwaysPasswordInput ? inputPassword : ftp.Parent.Password }))
				   {
					   Cancellation.Token.ThrowIfCancellationRequested();
					   try
					   {
						   ftpClient.Connect();
						   AddLogFromTask("Ftp接続に成功しました。");
						   
					   }
					   catch
					   {
						   AddLogFromTask("Ftp接続に失敗しました。");
						   FinishedFromTask();
						   return;
					   }
					   Cancellation.Token.ThrowIfCancellationRequested();

					   if (updateFiles)
					   {
						   try
						   {
							   AddLogFromTask("更新のアップロードを開始します。");
							   //更新ファイルのアップロード
							   Utility.NarUtility.UploadUpdates(ftpClient, Ghost.FullPath, ftp.UpdatePath, useDiff, AddProgressFromTask, Cancellation.Token);
							   AddLogFromTask("更新のアップロードが完了しました。");
						   }
						   catch(OperationCanceledException)
						   {
							   throw;
						   }
						   catch
						   {
							   AddLogFromTask("更新のアップロードに失敗しました。");
							   FinishedFromTask();
							   return;
						   }
					   }

					   if (updateNar)
					   {
						   try
						   {
							   AddLogFromTask("narのアップロードを開始します。");
							   //narのアップロード
							   Utility.NarUtility.UploadNar(Ghost.FullPath, ftp.NarPath, ftpClient, Cancellation.Token);
							   AddLogFromTask("narのアップロードが完了しました。");
						   }
						   catch (OperationCanceledException)
						   {
							   throw;
						   }
						   catch
						   {
							   AddLogFromTask("narアップロードに失敗しました。");
							   FinishedFromTask();
							   return;
						   }
					   }
					   AddLogFromTask("すべてのアップロードに成功しました。");
				   }
			   }
			   catch(OperationCanceledException)
			   {
				   AddLogFromTask("キャンセルしました。");
			   }
			   catch
			   {
				   AddLogFromTask("失敗しました。");
			   }
			   FinishedFromTask();
		   }
			);
		}

		private void DoUploadNarnaloaderV2(NarnaloaderV2ItemViewModel nnl, string inputPassword)
		{
			Task.Run(() =>
			{
				try
				{
					var account = new Core.NarnaloaderAccount() { url = nnl.Parent.Url, id = nnl.Parent.Username, password = nnl.Parent.AlwaysPasswordInput ? inputPassword : nnl.Parent.Password };
					AddLogFromTask("ななろだにアップロードします。");

					try
					{
						var result = Utility.NarUtility.UploadNar(Ghost.FullPath, account, nnl.ItemId, Cancellation.Token);
						if(result.success)
						{
							AddLogFromTask("アップロードに成功しました。");
							AddLogFromTask("アップローダのメッセージ: " + result.message);
						}
						else
						{
							AddLogFromTask("アップロードに失敗しました。");
							AddLogFromTask("アップローダのメッセージ: " + result.message);
						}
					}
					catch (OperationCanceledException)
					{
						AddLogFromTask("キャンセルしました。");
					}
					catch (Exception ex)
					{
						AddLogFromTask(ex.ToString());
						AddLogFromTask("アップロードを試みましたが、エラーが発生しました。");
					}
				}
				catch { }
				FinishedFromTask();
			});
		}

		private void AddLogFromTask(string log)
		{
			Dispatcher.Invoke(() =>
			{
				DataContext.Logs += log + "\r\n";
				UploadLogBox.ScrollToEnd();
			});
		}

		private	void AddProgressFromTask(Utility.Progress progress)
		{
			Dispatcher.Invoke(() =>
			{
				DataContext.Logs += progress.Message + "\r\n";
				DataContext.ProgressValue = progress.Value * 100.0;
				DataContext.UseProgressBar = progress.UseProgress;
				UploadLogBox.ScrollToEnd();
			});
		}

		private void FinishedFromTask()
		{
			Dispatcher.Invoke(() =>
		   {
			   isUploading = false;
			   UploadCancelButton.IsEnabled = false;
			   DataContext.ProgressValue = 100.0;
			   DataContext.UseProgressBar = true;
			   UploadEndButton.IsEnabled = true;
		   });
		}
	}

}
