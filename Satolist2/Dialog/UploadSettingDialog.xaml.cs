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
	public partial class UploadSettingDialog : DialogContentBase
	{
		internal new UploadSettingDialogViewModel DataContext
		{
			get => (UploadSettingDialogViewModel)base.DataContext;
			set
			{
				base.DataContext = value;
			}
		}

		internal UploadSettingDialog(Model.UploadServerSettingModelBase[] uploadSettings, MainViewModel main)
		{
			InitializeComponent();
			Owner = main.MainWindow.RootWindow;
			Closing += UploadSettingDialog_Closing;
			DataContext = new UploadSettingDialogViewModel(uploadSettings, this);
		}

		private void UploadSettingDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if(!DataContext.CloseAccepted)
			{
				if (!DataContext.AskDiscard())
					e.Cancel = true;
			}
		}

		public class UploadSettingDialogViewModel : NotificationObject
		{
			private ObservableCollection<UploadSettingItemViewModelBase> items;
			private Model.UploadServerSettingModelBase[] originalModel;

			public ReadOnlyObservableCollection<UploadSettingItemViewModelBase> Items
			{
				get => new ReadOnlyObservableCollection<UploadSettingItemViewModelBase>(items);
			}

			public UploadSettingDialog Dialog { get; private set; }

			public bool CloseAccepted { get; private set; }

			public ActionCommand AddFtpAccountCommand { get; }
			public ActionCommand AddNarnaloaderAccountCommand { get; }
			public ActionCommand CancelCommand { get; }
			public ActionCommand OkCommand { get; }

			internal UploadSettingDialogViewModel(Model.UploadServerSettingModelBase[] uploadSettings, UploadSettingDialog dialog)
			{
				Dialog = dialog;
				originalModel = uploadSettings ?? Array.Empty<Model.UploadServerSettingModelBase>();
				items = new ObservableCollection<UploadSettingItemViewModelBase>();
				var models = uploadSettings?.Select(o => o.Clone()) ?? Array.Empty<Model.UploadServerSettingModelBase>();

				AddFtpAccountCommand = new ActionCommand(
					o =>
					{
						var itemModel = new Model.FtpServerSettingModel();
						var newItem = new FtpAccountUploadSettingViewModel(itemModel, this);
						newItem.Label = "新しいサーバ設定";
						newItem.IsSelected = true;
						newItem.IsExpanded = true;
						items.Add(newItem);
					}
					);

				AddNarnaloaderAccountCommand = new ActionCommand(
					o =>
					{
						var itemModel = new Model.NarnaloaderV2ServerSettingModel();
						var newItem = new NarnaloaderSettingViewModel(itemModel, this);
						newItem.Label = "新しいサーバ設定";
						newItem.IsSelected = true;
						newItem.IsExpanded = true;
						items.Add(newItem);
					}
					);

				OkCommand = new ActionCommand(
					o =>
					{
						CloseAccepted = true;
						dialog.DialogResult = IsChanged();	//変更がないなら保存しない
						dialog.Close();
					}
					);

				CancelCommand = new ActionCommand(
					o =>
					{
						if (AskDiscard())
						{
							CloseAccepted = true;
							dialog.DialogResult = false;
							dialog.Close();
						}
					}
					);

				//モデルの読込
				foreach (var item in models)
				{
					if (item.ItemType == Model.FtpServerSettingModel.Type)
					{
						//ftp
						items.Add(new FtpAccountUploadSettingViewModel((Model.FtpServerSettingModel)item, this));

					}
					else if (item.ItemType == Model.NarnaloaderV2ServerSettingModel.Type)
					{
						//nnlv2
						items.Add(new NarnaloaderSettingViewModel((Model.NarnaloaderV2ServerSettingModel)item, this));
					}
					else
					{
						//?
						throw new Exception("アップロード設定に不明なレコードが存在するため開けませんでした。");
					}
				}
			}

			public bool AskDiscard()
			{
				if (IsChanged())
				{
					var result = MessageBox.Show("変更を破棄してもよろしいですか？", "アップロード設定", MessageBoxButton.YesNo, MessageBoxImage.Question);
					if (result != MessageBoxResult.Yes)
						return false;
				}
				return true;
			}

			public void RemoveItem(UploadSettingItemViewModelBase item)
			{
				items.Remove(item);
			}

			internal Model.UploadServerSettingModelBase[] GetItems()
			{
				return items.Select<UploadSettingItemViewModelBase, Model.UploadServerSettingModelBase>(o =>
				{
					if (o is NarnaloaderSettingViewModel nnl)
					{
						//パスワード毎回入力の場合はパスワードのレコードを除去する
						var item = (Model.NarnaloaderV2ServerSettingModel)nnl.Model.Clone();
						if (item.AlwaysPasswordInput)
							item.Password = string.Empty;
						return item;
					}
					if (o is FtpAccountUploadSettingViewModel ftp)
					{
						var item = (Model.FtpServerSettingModel)ftp.Model.Clone();
						if (item.AlwaysPasswordInput)
							item.Password = string.Empty;
						return item;
					}
					return null;
				}).ToArray();
			}

			public bool IsChanged()
			{
				if (originalModel.Length != Items.Count)
					return true;

				for (int i = 0; i < items.Count; i++)
				{
					if (Items[i] is FtpAccountUploadSettingViewModel ftp)
					{
						if (!ftp.Model.IsEquals(originalModel[i]))
							return true;
					}
					else if (Items[i] is NarnaloaderSettingViewModel nnl)
					{
						if (!nnl.Model.IsEquals(originalModel[i]))
							return true;
					}
				}
				return false;
			}
		}

		public abstract class UploadSettingItemViewModelBase : NotificationObject
		{
			public enum ItemType
			{
				//サーバアイテム
				FtpServer,                //FTP
				NarnaloaderV2Server,       //ななろだv2

				//ゴーストアップロードアイテム
				FtpGhost,
				NarnaloaderV2Ghost,
			}

			public bool isExpanded;
			public bool isSelected;

			public bool IsExpanded
			{
				get => isExpanded;
				set
				{
					isExpanded = value;
					NotifyChanged();
				}
			}

			public bool IsSelected
			{
				get => isSelected;
				set
				{
					isSelected = value;
					NotifyChanged();
				}
			}

			public abstract ItemType Type { get; }
		}

		private class FtpAccountUploadSettingViewModel : UploadSettingItemViewModelBase
		{
			private ObservableCollection<FtpUploadGhostSettingViewModel> items;
			private Model.FtpServerSettingModel model;

			public override ItemType Type => ItemType.FtpServer;

			public UploadSettingDialogViewModel Parent { get; private set; }

			public Model.FtpServerSettingModel Model
			{
				get => model;
			}

			public string Label
			{
				get => model.Label;
				set
				{
					model.Label = value;
					NotifyChanged();
				}
			}

			public string UserName
			{
				get => model.UserName;
				set
				{
					model.UserName = value;
					NotifyChanged();
				}
			}

			public string Password
			{
				get => model.Password;
				set
				{
					model.Password = value;
					NotifyChanged();
				}
			}

			public string Url
			{
				get => model.Url;
				set
				{
					model.Url = value;
					NotifyChanged();
				}
			}

			public bool AlwaysPasswordInput
			{
				get => model.AlwaysPasswordInput;
				set
				{
					model.AlwaysPasswordInput = value;
					NotifyChanged();
				}
			}


			public ReadOnlyObservableCollection<FtpUploadGhostSettingViewModel> Items => new ReadOnlyObservableCollection<FtpUploadGhostSettingViewModel>(items);
			public ActionCommand AddItemCommand { get; }
			public ActionCommand RemoveServerCommand { get; }

			public FtpAccountUploadSettingViewModel(Model.FtpServerSettingModel model, UploadSettingDialogViewModel parent)
			{
				Parent = parent;
				this.model = model;
				items = new ObservableCollection<FtpUploadGhostSettingViewModel>();

				AddItemCommand = new ActionCommand(
					o =>
					{
						var itemModel = new Model.FtpItemSettingModel();
						model.Items.Add(itemModel);

						var item = new FtpUploadGhostSettingViewModel(itemModel, this);
						item.Label = "新しいアップロード設定";
						item.IsSelected = true;
						IsExpanded = true;                          //追加が分かるように開く
					items.Add(item);
					}
					);
				RemoveServerCommand = new ActionCommand(
					o =>
					{
						parent.RemoveItem(this);
					}
					);

				foreach (var item in model.Items)
				{
					var itemViewModel = new FtpUploadGhostSettingViewModel(item, this);
					items.Add(itemViewModel);
				}
			}

			public void RemoveItem(FtpUploadGhostSettingViewModel item)
			{
				model.Items.Remove(item.Model);
				items.Remove(item);
			}
		}

		private class FtpUploadGhostSettingViewModel : UploadSettingItemViewModelBase
		{
			public override ItemType Type => ItemType.FtpGhost;
			private FtpAccountUploadSettingViewModel parent;
			private Model.FtpItemSettingModel model;

			public Model.FtpItemSettingModel Model
			{
				get => model;
			}
			public string UpdatePath
			{
				get => model.UpdatePath;
				set
				{
					model.UpdatePath = value;
					NotifyChanged();
				}
			}

			public string NarPath
			{
				get => model.NarPath;
				set
				{
					model.NarPath = value;
					NotifyChanged();
				}
			}

			public string Label
			{
				get => model.Label;
				set
				{
					model.Label = value;
					NotifyChanged();
				}
			}

			public ActionCommand RemoveItemCommand { get; }
			public ActionCommand UpdatePathBrowseCommand { get; }
			public ActionCommand NarPathBrowseCommand { get; }

			public object Items => Array.Empty<object>();

			public FtpUploadGhostSettingViewModel(Model.FtpItemSettingModel model, FtpAccountUploadSettingViewModel parent)
			{
				this.parent = parent;
				this.model = model;

				RemoveItemCommand = new ActionCommand(
					o =>
					{
						parent.RemoveItem(this);
					}
					);

				UpdatePathBrowseCommand = new ActionCommand(
					o =>
					{
						var account = new Core.FtpAccount()
						{
							url = parent.Url,
							password = parent.Password,
							username = parent.UserName
						};

						RemoteFileSelectDialog dialog = new RemoteFileSelectDialog(parent.Parent.Dialog.Host);
						var vm = new RemoteFileSelectDialogViewModel(dialog, account);
						vm.ShowFileNameInput = false;
						dialog.DataContext = vm;
						if (dialog.ShowDialog() == true)
						{
							UpdatePath = vm.ResultPath;
						}
					}
					);

				NarPathBrowseCommand = new ActionCommand(
					o =>
					{
						var account = new Core.FtpAccount()
						{
							url = parent.Url,
							password = parent.Password,
							username = parent.UserName
						};

						RemoteFileSelectDialog dialog = new RemoteFileSelectDialog(parent.Parent.Dialog.Host);
						var vm = new RemoteFileSelectDialogViewModel(dialog, account);
						vm.FileName = "ghost.nar";  //default
					vm.ShowFileNameInput = true;
						if (!string.IsNullOrEmpty(NarPath) && !string.IsNullOrEmpty(System.IO.Path.GetFileName(NarPath)))
						{
							vm.FileName = System.IO.Path.GetFileName(NarPath);
						}

						dialog.DataContext = vm;
						if (dialog.ShowDialog() == true)
						{
							NarPath = vm.ResultPath;
						}
					}
					);
			}
		}

		private class NarnaloaderSettingViewModel : UploadSettingItemViewModelBase
		{
			private ObservableCollection<NarnaloaderUploadGhostViewModel> items;
			private UploadSettingDialogViewModel parent;
			private Model.NarnaloaderV2ServerSettingModel model;

			public override ItemType Type => ItemType.NarnaloaderV2Server;

			public Model.NarnaloaderV2ServerSettingModel Model
			{
				get => model;
			}

			public string Label
			{
				get => model.Label;
				set
				{
					model.Label = value;
					NotifyChanged();
				}
			}

			public string UserName
			{
				get => model.UserName;
				set
				{
					model.UserName = value;
					NotifyChanged();
				}
			}

			public string Password
			{
				get => model.Password;
				set
				{
					model.Password = value;
					NotifyChanged();
				}
			}

			public string Url
			{
				get => model.Url;
				set
				{
					model.Url = value;
					NotifyChanged();
				}
			}

			public bool AlwaysPasswordInput
			{
				get => model.AlwaysPasswordInput;
				set
				{
					model.AlwaysPasswordInput = value;
					NotifyChanged();
				}
			}

			public ReadOnlyObservableCollection<NarnaloaderUploadGhostViewModel> Items => new ReadOnlyObservableCollection<NarnaloaderUploadGhostViewModel>(items);

			public ActionCommand AddItemCommand { get; }
			public ActionCommand RemoveServerCommand { get; }

			public NarnaloaderSettingViewModel(Model.NarnaloaderV2ServerSettingModel model, UploadSettingDialogViewModel parent)
			{
				this.parent = parent;
				this.model = model;
				items = new ObservableCollection<NarnaloaderUploadGhostViewModel>();

				AddItemCommand = new ActionCommand(
					o =>
					{
					//modelの追加
					var itemModel = new Model.NarnaloaderV2ItemSettingModel();
						model.Items.Add(itemModel);

					//viewModelの追加
					var item = new NarnaloaderUploadGhostViewModel(itemModel, this);
						item.IsSelected = true;
						item.IsExpanded = true;
						item.Label = "新しいアップロード設定";
						items.Add(item);
					}
					);
				RemoveServerCommand = new ActionCommand(
					o =>
					{
						parent.RemoveItem(this);
					}
					);

				foreach (var item in model.Items)
				{
					var itemViewModel = new NarnaloaderUploadGhostViewModel(item, this);
					items.Add(itemViewModel);
				}
			}

			public void RemoveItem(NarnaloaderUploadGhostViewModel item)
			{
				model.Items.Remove(item.Model);
				items.Remove(item);
			}
		}

		private class NarnaloaderUploadGhostViewModel : UploadSettingItemViewModelBase
		{
			public override ItemType Type => ItemType.NarnaloaderV2Ghost;

			private NarnaloaderSettingViewModel parent;
			private Model.NarnaloaderV2ItemSettingModel model;

			public Model.NarnaloaderV2ItemSettingModel Model
			{
				get => model;
			}

			public string Label
			{
				get => model.Label;
				set
				{
					model.Label = value;
					NotifyChanged();
				}
			}

			public string GhostId
			{
				get => model.ItemId;
				set
				{
					model.ItemId = value;
					NotifyChanged();
				}
			}

			public ActionCommand RemoveItemCommand { get; }
			public object Items => Array.Empty<object>();

			public NarnaloaderUploadGhostViewModel(Model.NarnaloaderV2ItemSettingModel model, NarnaloaderSettingViewModel parent)
			{
				this.model = model;
				this.parent = parent;

				RemoveItemCommand = new ActionCommand(
					o =>
					{
						parent.RemoveItem(this);
					}
					);
			}

		}

	}
}
