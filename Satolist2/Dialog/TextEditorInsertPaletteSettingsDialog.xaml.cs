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
	public partial class TextEditorInsertPaletteSettingsDialog : Window
	{
		public TextEditorInsertPaletteSettingsDialog()
		{
			InitializeComponent();
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
		public ActionCommand AddItemCommand { get; }
		public ActionCommand AddGroupCommand { get; }
		public ActionCommand OkCommand { get; }
		public ActionCommand CancelCommand { get; }
		 
		public TextEditorInsertPaletteSettingsDialogViewModel(TextEditorInsertPaletteSettingsDialog control, InsertItemPaletteModel model)
		{
			this.control = control;
			this.control.MainList.SelectedItemChanged += MainList_SelectedItemChanged;
			items = new ObservableCollection<TextEditorInsertPaletteSettingsDialogItemViewModel>();

			//ルートアイテムの生成
			var rootItem = new TextEditorInsertPaletteSettingsDialogItemViewModel(model)
			{
				Type = InsertItemPaletteModel.NodeType.Root,
				Label = "(挿入メニュートップ)"
			};

			items.Add(rootItem);
			

			//コマンド生成
			AddItemCommand = new ActionCommand(
				o =>
				{
					var newItem = new TextEditorInsertPaletteSettingsDialogItemViewModel()
					{
						Type = InsertItemPaletteModel.NodeType.Item,
						Label = "新しいアイテム",
						Body = "挿入内容"
					};
					selectedItem.AddItem(newItem);
					selectedItem.IsExpanded = true;	//追加先を展開する
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
					var newItem = new TextEditorInsertPaletteSettingsDialogItemViewModel()
					{
						Type = InsertItemPaletteModel.NodeType.Group,
						Label = "新しいグループ"
					};
					selectedItem.AddItem(newItem);
					selectedItem.IsExpanded = true; //追加先を展開する
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

		public void Dispose()
		{
			this.control.MainList.SelectedItemChanged -= MainList_SelectedItemChanged;
		}

		private void MainList_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			SelectedItem = (TextEditorInsertPaletteSettingsDialogItemViewModel)control.MainList.SelectedItem;
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

		public InsertItemPaletteModel.NodeType Type { get; set; }
		public string Label
		{
			get => label;
			set
			{
				label = value;
				NotifyChanged();
				NotifyChanged(nameof(ListLabel));
			}
		}

		public string Body
		{
			get => body;
			set
			{
				body = value;
				NotifyChanged();
			}
		}

		public int ShortcutKeyNumber
		{
			get => shortcutKeyNumber;
			set
			{
				shortcutKeyNumber = value;
				NotifyChanged();
				NotifyChanged(nameof(ListLabel));
			}
		}

		public bool ShortcutKeyCtrl
		{
			get => shortcutKeyCtrl;
			set
			{
				shortcutKeyCtrl = value;
				NotifyChanged();
				NotifyChanged(nameof(ListLabel));
			}
		}

		public bool ShortcutKeyAlt
		{
			get => shortcutKeyAlt;
			set
			{
				shortcutKeyAlt = value;
				NotifyChanged();
				NotifyChanged(nameof(ListLabel));
			}
		}

		public bool ShortcutKeyShift
		{
			get => shortcutKeyShift;
			set
			{
				shortcutKeyShift = value;
				NotifyChanged();
				NotifyChanged(nameof(ListLabel));
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


		public TextEditorInsertPaletteSettingsDialogItemViewModel(InsertItemPaletteModel fromModel = null, TextEditorInsertPaletteSettingsDialogItemViewModel parentViewModel = null)
		{
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
					fromModel.Items?.Select(o => new TextEditorInsertPaletteSettingsDialogItemViewModel(o, this)) ?? Array.Empty<TextEditorInsertPaletteSettingsDialogItemViewModel>()
					);
			}
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
