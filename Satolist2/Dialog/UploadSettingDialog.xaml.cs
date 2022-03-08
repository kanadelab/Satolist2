using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace Satolist2.Dialog
{
	/// <summary>
	/// UploadSettingDialog.xaml の相互作用ロジック
	/// </summary>
	public partial class UploadSettingDialog : Window
	{
		public UploadSettingDialog()
		{
			InitializeComponent();
		}

	}

	public class UploadSettingDialogViewModel : NotificationObject
	{
		private ObservableCollection<UploadSettingItemViewModelBase> items;

		public ReadOnlyObservableCollection<UploadSettingItemViewModelBase> Items
		{
			get => new ReadOnlyObservableCollection<UploadSettingItemViewModelBase>(items);
		}

		public UploadSettingDialogViewModel()
		{
			items = new ObservableCollection<UploadSettingItemViewModelBase>();

			var item = new FtpAccountUploadSettingViewModel()
			{
			};

			items.Add(item);
			items.Add(new NarnaloaderSettingViewModel());
		}
	}

	

	public abstract class UploadSettingItemViewModelBase : NotificationObject
	{
		public enum ItemType
		{
			//サーバアイテム
			FtpServer,                //FTP
			NarNaLoaderV2Server,       //ななろだv2

			//ゴーストアップロードアイテム
			FtpGhost,
			NarnaloaderV2Ghost,
		}

		public abstract ItemType Type { get; }
	}

	public class FtpAccountUploadSettingViewModel : UploadSettingItemViewModelBase
	{
		private string label;
		private string username;
		private string password;
		private string url;
		private ObservableCollection<FtpUploadGhostSettingViewModel> items;

		public override ItemType Type => ItemType.FtpServer;

		public string Label
		{
			get => label;
			set
			{
				label = value;
				NotifyChanged();
			}
		}

		public string Description
		{
			get => string.Concat( "FTP: ", url);
		}

		public string UserName
		{
			get => username;
			set
			{
				username = value;
				NotifyChanged();
			}
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

		public string Url
		{
			get => url;
			set
			{
				url = value;
				NotifyChanged();
				NotifyChanged(nameof(Description));
			}
		}

		public ReadOnlyObservableCollection<FtpUploadGhostSettingViewModel> Items => new ReadOnlyObservableCollection<FtpUploadGhostSettingViewModel>(items);

		public FtpAccountUploadSettingViewModel()
		{
			items = new ObservableCollection<FtpUploadGhostSettingViewModel>();

			items.Add(new FtpUploadGhostSettingViewModel()
			{
				Label = "奏でる日常の旋律"
			});
		}
	}

	public class FtpUploadGhostSettingViewModel : UploadSettingItemViewModelBase
	{
		public override ItemType Type => ItemType.FtpGhost;
		private string updatePath;
		private string narPath;
		private string label;

		public string UpdatePath
		{
			get => updatePath;
			set
			{
				updatePath = value;
				NotifyChanged();
			}
		}

		public string NarPath
		{
			get => narPath;
			set
			{
				narPath = value;
				NotifyChanged();
			}
		}

		public string Label
		{
			get => label;
			set
			{
				label = value;
				NotifyChanged();
			}
		}

		public ActionCommand UpdatePathBrowseCommand { get; }
		public ActionCommand NarPathBrowseCommand { get; }

		public object Items => Array.Empty<object>();

		public FtpUploadGhostSettingViewModel()
		{
			UpdatePathBrowseCommand = new ActionCommand(
				o =>
				{
					var account = new Core.FtpAccount()
					{
					};
					RemoteFileSelectDialog dialog = new RemoteFileSelectDialog();
					var vm = new RemoteFileSelectDialogViewModel(dialog, account);
					dialog.DataContext = vm;
					if( dialog.ShowDialog() == true)
					{
						UpdatePath = vm.ResultPath;
					}
				}
				);
		}
	}
	 
	public class NarnaloaderSettingViewModel : UploadSettingItemViewModelBase
	{
		public override ItemType Type => ItemType.NarNaLoaderV2Server;

		public string Label
		{
			get => "ななろだ";
		}

		public string Description
		{
			get => "ななろだv2: https://nanachi.sakura.ne.jp/narnaloader/upload.php";
		}
	}

	
}
