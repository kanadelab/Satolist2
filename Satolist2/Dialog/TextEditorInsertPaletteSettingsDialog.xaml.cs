using Satolist2.Model;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Satolist2.Dialog
{
	/// <summary>
	/// TextEditorInsertPaletteSettingsDialog.xaml の相互作用ロジック
	/// </summary>
	public partial class TextEditorInsertPaletteSettingsDialog : DialogContentBase
	{
		internal TextEditorInsertPaletteSettingsDialog(MainViewModel main)
		{
			InitializeComponent();
			Owner = main.MainWindow.RootWindow;
		}

		private void TreeViewItem_DragOver(object sender, DragEventArgs e)
		{
			if(sender is FrameworkElement elem)
			{
				if (e.Data.GetDataPresent(typeof(TextEditorInsertPaletteSettingsDialogItemViewModel)))
				{
					if (elem.DataContext is TextEditorInsertPaletteSettingsDialogItemViewModel itemVm)
					{
						if (itemVm.Type != InsertItemPaletteModel.NodeType.Item && !ReferenceEquals(itemVm, e.Data.GetData(typeof(TextEditorInsertPaletteSettingsDialogItemViewModel))))
						{
							e.Effects = DragDropEffects.Move;
							e.Handled = true;
						}
						else
						{
							e.Effects = DragDropEffects.None;
							e.Handled = true;
						}
					}
				}
			}
		}

		private void TreeViewItem_Drop(object sender, DragEventArgs e)
		{
			if(sender is FrameworkElement elem)
			{
				if(elem.DataContext is TextEditorInsertPaletteSettingsDialogItemViewModel itemVm)
				{
					var data = (TextEditorInsertPaletteSettingsDialogItemViewModel)e.Data.GetData(typeof(TextEditorInsertPaletteSettingsDialogItemViewModel));

					if (itemVm.Type != InsertItemPaletteModel.NodeType.Item && !ReferenceEquals(itemVm, e.Data.GetData(typeof(TextEditorInsertPaletteSettingsDialogItemViewModel))))
					{
						//移動処理
						itemVm.AddItem(data);

						//アイテムを投入したらexpandする
						itemVm.IsExpanded = true;
						e.Handled = true;
					}
				}
			}
		}

		private void TreeViewItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (sender is FrameworkElement elem)
			{
				if (elem.DataContext is TextEditorInsertPaletteSettingsDialogItemViewModel itemVm)
				{
					if (itemVm.Type != InsertItemPaletteModel.NodeType.Root)
					{
						DragDrop.DoDragDrop((DependencyObject)sender, itemVm, DragDropEffects.Move);
					}
				}
			}
		}
	}

	internal class TextEditorInsertPaletteSettingsDialogViewModel : NotificationObject, IDisposable
	{
		private TextEditorInsertPaletteSettingsDialog control;
		private ObservableCollection<TextEditorInsertPaletteSettingsDialogItemViewModel> items;
		private TextEditorInsertPaletteSettingsDialogItemViewModel selectedItem;
		private bool isChanged;

		public ReadOnlyObservableCollection<TextEditorInsertPaletteSettingsDialogItemViewModel> Items => new ReadOnlyObservableCollection<TextEditorInsertPaletteSettingsDialogItemViewModel>(items);
		public TextEditorInsertPaletteSettingsDialogItemViewModel SelectedItem
		{
			get => selectedItem;
			set
			{
				selectedItem = value;
				NotifyChanged();
				AddItemCommand.NotifyCanExecuteChanged();
				AddGroupCommand.NotifyCanExecuteChanged();
			}
		}

		public bool IsChanged
		{
			get => isChanged;
			private set
			{
				isChanged = value;
				NotifyChanged();
			}
		}

		public ActionCommand AddItemCommand { get; }
		public ActionCommand AddGroupCommand { get; }
		public ActionCommand OkCommand { get; }
		public ActionCommand CancelCommand { get; }
		 
		public TextEditorInsertPaletteSettingsDialogViewModel(TextEditorInsertPaletteSettingsDialog control, InsertItemPaletteModel model)
		{
			this.control = control;
			this.control.MainList.SelectedItemChanged += MainList_SelectedItemChanged;
			control.Closing += Control_Closing;

			items = new ObservableCollection<TextEditorInsertPaletteSettingsDialogItemViewModel>();

			//ルートアイテムの生成
			var rootItem = new TextEditorInsertPaletteSettingsDialogItemViewModel(this, model)
			{
				Type = InsertItemPaletteModel.NodeType.Root,
				Label = "(挿入メニュートップ)",
				IsSelected = true,
				IsExpanded = true
			};

			items.Add(rootItem);
			

			//コマンド生成
			AddItemCommand = new ActionCommand(
				o =>
				{
					var newItem = new TextEditorInsertPaletteSettingsDialogItemViewModel(this)
					{
						Type = InsertItemPaletteModel.NodeType.Item,
						Label = "新しいアイテム",
						Body = "挿入内容"
					};
					selectedItem.AddItem(newItem);
					selectedItem.IsExpanded = true; //追加先を展開する
					Changed();
				},
				o =>
				{
					if (selectedItem == null)
						return false;
					if (selectedItem.Type == InsertItemPaletteModel.NodeType.Item)
						return false;
					return true;
				}
				);

			AddGroupCommand = new ActionCommand(
				o =>
				{
					var newItem = new TextEditorInsertPaletteSettingsDialogItemViewModel(this)
					{
						Type = InsertItemPaletteModel.NodeType.Group,
						Label = "新しいグループ"
					};
					selectedItem.AddItem(newItem);
					selectedItem.IsExpanded = true; //追加先を展開する
					Changed();
				},
				o =>
				{
					if (selectedItem == null)
						return false;
					if (selectedItem.Type == InsertItemPaletteModel.NodeType.Item)
						return false;
					return true;
				}
				);

			OkCommand = new ActionCommand(
				o =>
				{
					if(IsChanged)
						control.DialogResult = true;
					control.Close();
				}
				);

			CancelCommand = new ActionCommand(
				o =>
				{
					control.Close();
				}
				);
		}

		private void Control_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (control.DialogResult == null)
			{
				if (IsChanged)
				{
					//破棄警告
					var result = MessageBox.Show("変更を保存せずに閉じてもいいですか？", "挿入メニューのカスタマイズ", MessageBoxButton.YesNo, MessageBoxImage.Question);
					if (result != MessageBoxResult.Yes)
					{
						e.Cancel = true;
						return;
					}
				}
				control.DialogResult = false;
			}
		}

		public void Dispose()
		{
			this.control.MainList.SelectedItemChanged -= MainList_SelectedItemChanged;
		}

		private void MainList_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			SelectedItem = (TextEditorInsertPaletteSettingsDialogItemViewModel)control.MainList.SelectedItem;
		}

		//変更フラグを立てる
		public void Changed()
		{
			IsChanged = true;
		}
	}

	

	//ViewModel
	internal class TextEditorInsertPaletteSettingsDialogItemViewModel : NotificationObject
	{
		private string label;
		private string body;
		private int shortcutKeyNumber;
		private bool shortcutKeyCtrl;
		private bool shortcutKeyAlt;
		private bool shortcutKeyShift;
		private bool isSelected;
		private bool isExpanded;
		private ObservableCollection<TextEditorInsertPaletteSettingsDialogItemViewModel> items;
		private TextEditorInsertPaletteSettingsDialogItemViewModel parent;
		private TextEditorInsertPaletteSettingsDialogViewModel dialogViewModel;

		public ActionCommand UpItemCommand { get; }
		public ActionCommand DownItemCommand { get; }
		public ActionCommand RemoveItemCommand { get; }

		public InsertItemPaletteModel.NodeType Type { get; set; }
		public string Label
		{
			get => label;
			set
			{
				if (label != value)
				{
					label = value;
					NotifyChanged();
					NotifyChanged(nameof(ListLabel));
					dialogViewModel.Changed();
				}
			}
		}

		public string Body
		{
			get => body;
			set
			{
				if (body != value)
				{
					body = value;
					NotifyChanged();
					dialogViewModel.Changed();
				}
			}
		}

		public int ShortcutKeyNumber
		{
			get => shortcutKeyNumber;
			set
			{
				if (shortcutKeyNumber != value)
				{
					shortcutKeyNumber = value;
					NotifyChanged();
					NotifyChanged(nameof(ListLabel));
					dialogViewModel.Changed();
				}
			}
		}

		public bool ShortcutKeyCtrl
		{
			get => shortcutKeyCtrl;
			set
			{
				if (shortcutKeyCtrl != value)
				{
					shortcutKeyCtrl = value;
					NotifyChanged();
					NotifyChanged(nameof(ListLabel));
					dialogViewModel.Changed();
				}
			}
		}

		public bool ShortcutKeyAlt
		{
			get => shortcutKeyAlt;
			set
			{
				if (shortcutKeyAlt != value)
				{
					shortcutKeyAlt = value;
					NotifyChanged();
					NotifyChanged(nameof(ListLabel));
					dialogViewModel.Changed();
				}
			}
		}

		public bool ShortcutKeyShift
		{
			get => shortcutKeyShift;
			set
			{
				if (shortcutKeyShift != value)
				{
					shortcutKeyShift = value;
					NotifyChanged();
					NotifyChanged(nameof(ListLabel));
					dialogViewModel.Changed();
				}
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

		public bool IsExpanded
		{
			get => isExpanded;
			set
			{
				isExpanded = value;
				NotifyChanged();
			}
		}

		public string ListLabel
		{
			get
			{
				var result = Label;
				if (shortcutKeyCtrl || shortcutKeyAlt)
				{
					var items = new List<string>();
					items.Add(Label);
					if (shortcutKeyCtrl)
						items.Add("Ctrl");
					if (shortcutKeyAlt)
						items.Add("Alt");
					if (shortcutKeyShift)
						items.Add("Shift");
					result += string.Format(" [{0}]", string.Join(" + ", items));
				}
				return result;
			}
		}

		public void AddItem(TextEditorInsertPaletteSettingsDialogItemViewModel addItem)
		{
			//自分自身と移動したいのはだめ
			if (ReferenceEquals(addItem.parent, this))
				return;

			//現在の親があれば解除
			addItem.parent?.items?.Remove(addItem);

			//自分に追加
			items.Add(addItem);
			addItem.parent = this;
		}


		public ReadOnlyObservableCollection<TextEditorInsertPaletteSettingsDialogItemViewModel> Items => new ReadOnlyObservableCollection<TextEditorInsertPaletteSettingsDialogItemViewModel>(items);

		public TextEditorInsertPaletteSettingsDialogItemViewModel Parent => parent;


		public TextEditorInsertPaletteSettingsDialogItemViewModel(TextEditorInsertPaletteSettingsDialogViewModel dialogViewModel, InsertItemPaletteModel fromModel = null, TextEditorInsertPaletteSettingsDialogItemViewModel parentViewModel = null)
		{
			this.dialogViewModel = dialogViewModel;

			if (fromModel == null)
			{
				items = new ObservableCollection<TextEditorInsertPaletteSettingsDialogItemViewModel>();
			}
			else
			{
				body = fromModel.Body;
				label = fromModel.Label;
				shortcutKeyAlt = fromModel.ShortCutKeyAlt;
				shortcutKeyCtrl = fromModel.ShortCutKeyCtrl;
				shortcutKeyShift = fromModel.ShortCutKeyShift;
				shortcutKeyNumber = fromModel.ShortCutKeyNumber;
				parent = parentViewModel;
				Type = fromModel.Type;
				items = new ObservableCollection<TextEditorInsertPaletteSettingsDialogItemViewModel>(
					fromModel.Items?.Select(o => new TextEditorInsertPaletteSettingsDialogItemViewModel(dialogViewModel, o, this)) ?? Array.Empty<TextEditorInsertPaletteSettingsDialogItemViewModel>()
					);
			}

			UpItemCommand = new ActionCommand(o =>
			{
				var currentIndex = parent.items.IndexOf(this);
				if(currentIndex > 0)
				{
					parent.items.Remove(this);
					parent.items.Insert(currentIndex - 1, this);
					dialogViewModel.Changed();
				}
			});

			DownItemCommand = new ActionCommand(
				o =>
				{
					var currentIndex = parent.items.IndexOf(this);
					if(currentIndex < parent.items.Count-1)
					{
						parent.items.Remove(this);
						parent.items.Insert(currentIndex + 1, this);
						dialogViewModel.Changed();
					}
				}
				);

			RemoveItemCommand = new ActionCommand(
				o =>
				{
					parent.items.Remove(this);
					dialogViewModel.Changed();
				}
				);
		}

		public InsertItemPaletteModel ToModel()
		{
			return new InsertItemPaletteModel()
			{
				Body = body,
				Label = label,
				ShortCutKeyAlt = shortcutKeyAlt,
				ShortCutKeyCtrl = shortcutKeyCtrl,
				ShortCutKeyShift = shortcutKeyShift,
				ShortCutKeyNumber = shortcutKeyNumber,
				Type = Type,
				Items = items.Select(o => o.ToModel()).ToArray()
			};
		}

	}
}
