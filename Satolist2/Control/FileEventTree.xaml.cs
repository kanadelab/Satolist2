using ICSharpCode.AvalonEdit.Utils;
using Microsoft.Win32;
using Satolist2.Dialog;
using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
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
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;


namespace Satolist2.Control
{
	/// <summary>
	/// FileEventTree.xaml の相互作用ロジック
	/// </summary>
	public partial class FileEventTree : UserControl
	{
		public bool isMouseDown;

		public FileEventTree()
		{
			InitializeComponent();
			
		}

		private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if(DataContext is FileEventTreeViewModel treeViewModel)
			{
				if(e.NewValue is FileEventTreeItemEventViewModel eventViewModel)
				{
					treeViewModel.Main.EventListViewModel.SetItems(eventViewModel.Items, eventViewModel.Label, eventViewModel.Dictionary.Dictionary);
				}
				else if(e.NewValue is FileEventTreeItemInlineEventViewModel inlineEventViewModel)
				{
					treeViewModel.Main.EventListViewModel.SetItems(inlineEventViewModel.EventList, inlineEventViewModel.Label, inlineEventViewModel.Dictionary);
				}
			}
		}

		private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			//ハンドル済でもイベントをバブルする問題があるので蹴る
			if (e.Handled)
				return;

			if(sender is TreeViewItem item)
			{
				if (item.DataContext is FileEventTreeItemDictionaryViewModel dic)
				{
					e.Handled = dic.Open();
				}
				else if(item.DataContext is FileEventTreeItemEventViewModel ev)
				{
					if(ev.Items.Count==1)
					{
						//Handledを立てても最後までイベントがバブルするせいでウインドウをアクティブにしてしまい、イベントエディタからフォーカスを奪ってしまうので
						//BeginInvokeをここでもかけて多重にすることでイベントエディタ側のフォーカシングを遅延させる
						Dispatcher.BeginInvoke(
							new Action(() =>
							{
								ev.OpenFirstEventEditor();
							}), System.Windows.Threading.DispatcherPriority.Render);
						e.Handled = true;
					}
					else
					{
						//イベントリストをアクティブ化
						Dispatcher.BeginInvoke(
							new Action(() =>
							{
								MainWindow.Instance.EventList.IsActive = true;
							}), System.Windows.Threading.DispatcherPriority.Render);
						e.Handled = true;
					}
				}
				else if(item.DataContext is FileEventTreeItemInlineEventViewModel inlineEv)
				{
					Dispatcher.BeginInvoke(
						new Action(() =>
						{
							inlineEv.OpenFirstEventEditor();
						}), System.Windows.Threading.DispatcherPriority.Render);
					e.Handled = true;
				}
			}
		}

		//勝手な横スクロールを抑制
		private void TreeViewItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
		{
			e.Handled = true;
		}

		private void TreeViewItem_MouseDown(object sender, MouseButtonEventArgs e)
		{
			//マウス押下でドラッグ開始を許可
			isMouseDown = true;
		}

		//ドロップ処理
		private void TreeViewItem_Drop(object sender, DragEventArgs e)
		{
			TreeViewPopup.IsOpen = false;

			if (e.Handled)
				return;

			if (!e.Data.GetDataPresent(typeof(FileEventTreeItemEventViewModel)))
				return;

			if (sender is FrameworkElement obj)
			{ 
				if (obj.DataContext is FileEventTreeItemEventViewModel ev)
				{
					var dropEv = (FileEventTreeItemEventViewModel)e.Data.GetData(typeof(FileEventTreeItemEventViewModel));

					//自分どうし
					if (ev == dropEv)
						return;

					if(ev.Dictionary == dropEv.Dictionary)
					{
						//同じ辞書ならインデックス上の移動のみを行う
						ev.Dictionary.Dictionary.MoveIndexEvent(dropEv.Identifier, ev.Identifier);
					}
					else
					{
						//異なる辞書なら、同じシグネチャのイベントが有る場合は合成になる
						if(ev.Dictionary.Dictionary.Containts(dropEv.Identifier))
						{
							//同じ内容があるなら移動(合成)するだけ
							if(MessageBox.Show("移動先の辞書に同じ項目が存在しているため、ひとつにまとめます。\r\nよろしいですか？", "さとりすと", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
							{
								dropEv.MoveTo(ev.Dictionary);
							}
						}
						else
						{
							//異なる内容があれば移動したうえで位置指定
							var identifier = dropEv.Identifier;
							dropEv.MoveTo(ev.Dictionary);
							ev.Dictionary.Dictionary.MoveIndexEvent(identifier, ev.Identifier);
						}
					}
					
					//辞書違い
					e.Handled = true;
				}
			}
		}

		//マウス移動: 押下されていて、動いたらドラッグ開始扱い
		private void TreeViewItem_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton != MouseButtonState.Pressed)
			{
				isMouseDown = false;
				return;
			}

			if(!isMouseDown)
			{
				return;
			}

			if (e.Handled)
				return;

			if (sender is FrameworkElement obj)
			{
				if (obj.DataContext is FileEventTreeItemEventViewModel ev)
				{
					//里々辞書出ない場合やリスト化されてなければNG
					if (ev.Dictionary.Dictionary.IsSerialized || !ev.Dictionary.Dictionary.IsSatoriDictionary)
						return;


					//ヘッダの移動不可
					if (ev.Items.First().Type == EventType.Sentence || ev.Items.First().Type == EventType.Word)
					{
						Dispatcher.BeginInvoke(new Action(() =>
						{
							DragDrop.DoDragDrop(obj, ev, DragDropEffects.Move);
						}), DispatcherPriority.Render);
						e.Handled = true;
					}
				}
			}
		}

		//マウスが上がった
		private void TreeViewItem_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			isMouseDown = false;
		}

		private void TreeViewItem_MouseLeave(object sender, MouseEventArgs e)
		{
			isMouseDown = false;
		}

		private void TreeViewItem_DragLeave(object sender, DragEventArgs e)
		{
			//ポップアップをキャンセル
			TreeViewPopup.IsOpen = false;
		}

		private void TreeViewItem_DragEnter(object sender, DragEventArgs e)
		{
			//ドラッグの入りなので位置を動かす
			if (e.Handled)
				return;

			if (e.Data.GetDataPresent(typeof(FileEventTreeItemEventViewModel)))
			{
				if (sender is TreeViewItem obj)
				{
					if (obj.DataContext is FileEventTreeItemEventViewModel ev)
					{
						//リスト化された里々辞書が条件
						if (!ev.Dictionary.Dictionary.IsSerialized && ev.Dictionary.Dictionary.IsSatoriDictionary)
						{
							var dropEv = (FileEventTreeItemEventViewModel)e.Data.GetData(typeof(FileEventTreeItemEventViewModel));
							var point = obj.PointToScreen(default);

							//MVVMではないけどポップアップを処理
							TreeViewPopup.IsOpen = true;
							TreeViewPopup.PlacementTarget = obj;
							TreeViewPopup.Placement = System.Windows.Controls.Primitives.PlacementMode.Absolute;
							TreeViewPopup.UpdateLayout();
							TreeViewPopup.HorizontalOffset = point.X;
							TreeViewPopup.VerticalOffset = point.Y;

							if (ev.Dictionary != dropEv.Dictionary && ev.Dictionary.Dictionary.Containts(dropEv.Identifier))
							{
								//移動先が異なる辞書でかつ、同名が存在しているのでマージを示す
								ArrowIcon.Visibility = Visibility.Collapsed;
								MergeIcon.Visibility = Visibility.Visible;
							}
							else
							{
								ArrowIcon.Visibility = Visibility.Visible;
								MergeIcon.Visibility = Visibility.Collapsed;
							}
							e.Handled = true;
							return;
						}
					}
				}
			}

			TreeViewPopup.IsOpen = false;
			e.Effects = DragDropEffects.None;
		}
	}

	internal class FileEventTreeViewModel : NotificationObject, IDockingWindowContent, IDisposable
	{
		public const string ContentId = "FileEventTree";
		private ObservableCollection<FileEventTreeItemDirectoryViewModel> directories;
		private string searchString;
		private bool isEventItemSearch;

		public MainViewModel Main { get; }
		public ActionCommand ToggleSearchModeCommand { get; }

		public GhostModel Ghost
		{
			get => Main.Ghost;
		}

		public ReadOnlyObservableCollection<FileEventTreeItemDirectoryViewModel> Directories
		{
			get => new ReadOnlyObservableCollection<FileEventTreeItemDirectoryViewModel>(directories);
		}

		//検索文字列
		public string SearchString
		{
			get => searchString;
			set
			{
				searchString = value;
				NotifyChanged();
			}
		}

		//検索モード:項目検索かどうか
		public bool IsEventItemSearch
		{
			get => isEventItemSearch;
			set
			{
				isEventItemSearch = value;
				NotifyChanged();
			}
		}

		public string DockingTitle => "ファイルイベントツリー";
		public string DockingContentId => ContentId;

		public FileEventTreeViewModel(MainViewModel main)
		{
			Main = main;
			directories = new ObservableCollection<FileEventTreeItemDirectoryViewModel>();

			ToggleSearchModeCommand = new ActionCommand(
				o =>
				{
					//検索モードの切り替え
					IsEventItemSearch = !isEventItemSearch;
				});

			if (Ghost != null)
			{
				//辞書生成
				foreach (var dic in Ghost.Dictionaries)
				{
					dic.PropertyChanged += Dic_PropertyChanged;
					var dictViewModel = new FileEventTreeItemDictionaryViewModel(this, dic);
					//dictionaries.Add(dictViewModel);

					var dicDir = DictionaryUtility.NormalizePath(System.IO.Path.GetDirectoryName(dic.RelativeName));
					var dir = directories.FirstOrDefault(o => o.RelativeName == dicDir);
					if(dir == null)
					{
						dir = new FileEventTreeItemDirectoryViewModel(dicDir);
						directories.Add(dir);
					}
					dir.AddDictionary(dictViewModel);
				}

				//辞書のコレクション変更ハンドラ
				INotifyCollectionChanged dictionaryCollection = Ghost.Dictionaries;
				dictionaryCollection.CollectionChanged += DictionaryCollection_CollectionChanged;
			}
		}

		private void Dic_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (sender is DictionaryModel dic)
			{
				if (e.PropertyName == nameof(DictionaryModel.RelativeName))
				{
					var newDicDir = DictionaryUtility.NormalizePath(System.IO.Path.GetDirectoryName(dic.RelativeName));

					//すでに存在してるレコードを一旦消す
					FileEventTreeItemDictionaryViewModel viewmodel = null;
					foreach(var dirVm in directories)
					{
						var item = dirVm.ChildItems.FirstOrDefault(o => o.Dictionary == dic);
						if (item != null)
						{
							if (dirVm.RelativeName == dic.RelativeName)
							{
								//前後で位置が変わらないので打ち切り
								return;
							}
							else
							{
								dirVm.RemoveDictionary(item);
								viewmodel = item;
								break;
							}
						}
					}

					if (viewmodel != null)
					{
						var dir = directories.FirstOrDefault(o => o.RelativeName == newDicDir);
						if (dir == null)
						{
							dir = new FileEventTreeItemDirectoryViewModel(newDicDir);
							directories.Add(dir);
						}
						dir.AddDictionary(viewmodel);
					}
				}
			}
		}

		//辞書の追加削除への対応 今のところ削除は保存操作で消えないといけないので、Collapsedにしているだけ
		private void DictionaryCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{

			if (e.OldItems != null)
			{
				foreach (Model.DictionaryModel item in e.OldItems)
				{
					foreach (var dir in directories)
					{
						var vm = dir.ChildItems.FirstOrDefault(o => o.Dictionary == item);
						if (vm != null)
						{
							dir.RemoveDictionary(vm);
							break;
						}
					}
				}
			}

			if (e.NewItems != null)
			{
				foreach (Model.DictionaryModel dic in e.NewItems)
				{
					var dicViewModel = new FileEventTreeItemDictionaryViewModel(this, dic);
					var dicDir = DictionaryUtility.NormalizePath(System.IO.Path.GetDirectoryName(dic.RelativeName));
					var dir = directories.FirstOrDefault(o => o.RelativeName == dicDir);
					if (dir == null)
					{
						dir = new FileEventTreeItemDirectoryViewModel(dicDir);
						directories.Add(dir);
					}
					dir.AddDictionary(dicViewModel);
				}
			}
		}

		public void Dispose()
		{
			/*
			foreach(var dic in dictionaries)
			{
				dic.Dictionary.PropertyChanged -= Dic_PropertyChanged;
			}
			*/

			foreach(var dir in directories)
			{
				foreach(var dic in dir.ChildItems)
				{
					dic.Dictionary.PropertyChanged -= Dic_PropertyChanged;
				}
			}

			if (Main.Ghost != null)
			{
				INotifyCollectionChanged dictionaryCollection = Ghost.Dictionaries;
				dictionaryCollection.CollectionChanged -= DictionaryCollection_CollectionChanged;
			}
		}
	}

	//辞書ファイルディレクトリのViewModel
	internal class FileEventTreeItemDirectoryViewModel : NotificationObject, Utility.SearchFilterConverter.IFilter
	{
		private ObservableCollection<FileEventTreeItemDictionaryViewModel> items;

		public ReadOnlyObservableCollection<FileEventTreeItemDictionaryViewModel> ChildItems
		{
			get => new ReadOnlyObservableCollection<FileEventTreeItemDictionaryViewModel>(items);
		}

		public string RelativeName { get; set; }

		public string Label => string.IsNullOrEmpty(RelativeName) ? "<里々ルートフォルダ>" : RelativeName;

		public FileEventTreeItemDirectoryViewModel(string relativePath)
		{
			items = new ObservableCollection<FileEventTreeItemDictionaryViewModel>();
			RelativeName = relativePath;
		}

		public string NodeType => "Directory";

		public void AddDictionary(FileEventTreeItemDictionaryViewModel dic)
		{
			items.Add(dic);
		}

		public void RemoveDictionary(FileEventTreeItemDictionaryViewModel dic)
		{
			items.Remove(dic);
		}

		public bool Filter(string filterString, object[] args)
		{
			//ヒットする子があるもののみ表示
			return items.Any(o => o.Filter(filterString, args));
		}
	}

	//辞書ファイルにあたるViewModel
	internal class FileEventTreeItemDictionaryViewModel : NotificationObject, Utility.SearchFilterConverter.IFilter
	{
		private ObservableCollection<FileEventTreeItemEventViewModel> items;
		private Dictionary<string, FileEventTreeItemEventViewModel> itemsDictionary;
		private Dictionary<EventModel, FileEventTreeItemEventViewModel> reverseDictionary;  //逆引き

		public string NodeType => "Dictionary";
		public FileEventTreeViewModel FileEventTree { get; }
		public DictionaryModel Dictionary { get; }

		public ReadOnlyObservableCollection<FileEventTreeItemEventViewModel> ChildItems
		{
			get => new ReadOnlyObservableCollection<FileEventTreeItemEventViewModel>(items);
		}

		//ファイルイベントツリー上で表示する名前
		public string Label => Dictionary.Name;

		//アイテムを追加
		public ActionCommand AddItemCommand { get; }
		public ActionCommand ChangeSerializeStatusCommand { get; }
		public ActionCommand DeleteFileCommand { get; }
		public ActionCommand MoveFileCommand { get; }

		public FileEventTreeItemDictionaryViewModel(FileEventTreeViewModel parent, DictionaryModel dictionary)
		{
			FileEventTree = parent;
			Dictionary = dictionary;

			items = new ObservableCollection<FileEventTreeItemEventViewModel>();
			itemsDictionary = new Dictionary<string, FileEventTreeItemEventViewModel>();
			reverseDictionary = new Dictionary<EventModel, FileEventTreeItemEventViewModel>();

			//コマンド
			AddItemCommand = new ActionCommand(
				o => FileEventTree.Main.OpenAddEventDialog(addTarget: Dictionary)
				);

			//リスト化
			ChangeSerializeStatusCommand = new ActionCommand(
				o =>
				{
					Dictionary.IsSerialized = !Dictionary.IsSerialized;
				},
				o =>
				{
					//里々辞書のみリストモード変更可能
					return Dictionary.IsSatoriDictionary;
				}
				);

			//削除
			DeleteFileCommand = new ActionCommand(
				o =>
				{
					var result = MessageBox.Show(string.Format("ファイル「{0}」を削除します。よろしいですか？\r\n※削除したファイルはゴミ箱へ送られます。", Label), "ファイルの削除", MessageBoxButton.YesNo, MessageBoxImage.Question);
					if(result == MessageBoxResult.Yes)
					{
						Dictionary.Ghost.DeleteDictionary(Dictionary);
					}
				}
				);

			//移動
			MoveFileCommand = new ActionCommand(
				o =>
				{
					var filePath = DictionaryUtility.NormalizeWindowsPath(dictionary.FullPath);
					var dialog = new SaveFileDialog();
					dialog.Filter = "里々辞書ファイル(dic*.txt)|dic*.txt|すべてのファイル(*.*)|*.*";
					dialog.InitialDirectory = System.IO.Path.GetDirectoryName(filePath);
					dialog.FileName = System.IO.Path.GetFileName(filePath);
					dialog.OverwritePrompt = false;
					dialog.Title = "移動先を指定";
					dialog.FileOk += (s, e) =>
					{
						if(!parent.Main.ValidateNewDictionaryPath(dialog.FileName, false))
						{
							e.Cancel = true;
							return;
						}

						bool result = true;
						bool showDialog = false;
						if(DictionaryUtility.IsSatoriDictionaryName(dictionary.Name))
						{
							if (!DictionaryUtility.IsSatoriDictionaryName(System.IO.Path.GetFileName(dialog.FileName)))
							{
								showDialog = true;
								//里々の形式からそれ以外の形式
								if (
									MessageBoxResult.No == MessageBox.Show("ファイルをここに移動します。よろしいですか？\r\n\r\n※ファイル名を里々の辞書(dic*.txt)からそうではない名前に変更しようとしています。里々の辞書とは認識されなくなりますがよろしいですか？", "ファイルを移動・名前変更", MessageBoxButton.YesNo, MessageBoxImage.Warning)
								)
								{
									result = false;
								}
							}
						}
						else
						{
							if (DictionaryUtility.IsSatoriDictionaryName(System.IO.Path.GetFileName(dialog.FileName)))
							{
								showDialog = true;
								//それ以外から里々の辞書形式
								if(
									MessageBoxResult.No == MessageBox.Show("ファイルをここに移動します。よろしいですか？\r\n\r\n※ファイル名を里々の辞書(dic*.txt)に変更しようとしています。新たに里々の辞書として認識されるようになりますがよろしいですか？", "ファイルを移動・名前変更", MessageBoxButton.YesNo, MessageBoxImage.Warning)
								)
								{
									result = false;
								}
							}
						}

						if(!showDialog)
						{
							 if(
								MessageBoxResult.No == MessageBox.Show("ファイルをここに移動します。よろしいですか？", "ファイルを移動・名前変更", MessageBoxButton.YesNo, MessageBoxImage.Question)
								)
							{
								result = false;
							}
						}

						e.Cancel = !result;
					};

					//リネームの実行
					if (dialog.ShowDialog() == true)
					{
						//辞書リネーム
						dictionary.Rename(dialog.FileName);
					}
				}
				);


			//イベントハンドラの用意
			Dictionary.PropertyChanged += OnDictionaryPropertyChanged;
			INotifyCollectionChanged collectionChanged = Dictionary.Events;
			collectionChanged.CollectionChanged += EventCollectionChanged;

			//dictionaryから同名のeventsを引っ張ってくる
			foreach (var ev in Dictionary.Events)
			{
				if (!itemsDictionary.ContainsKey(ev.Identifier))
				{
					var newItem = new FileEventTreeItemEventViewModel(this);
					itemsDictionary.Add(ev.Identifier, newItem);
					items.Add(newItem);
				}
				itemsDictionary[ev.Identifier].AddEvent(ev);
				reverseDictionary.Add(ev, itemsDictionary[ev.Identifier]);
			}
		}

		//項目の追加
		private void AddEvent(EventModel ev)
		{
			FileEventTreeItemEventViewModel item;
			if(!itemsDictionary.TryGetValue(ev.Identifier, out item))
			{
				item = new FileEventTreeItemEventViewModel(this);
				itemsDictionary.Add(ev.Identifier, item);

				//手前のアイテムを探す
				var eventIndex = Dictionary.Events.IndexOf(ev);
				var beforeIdentifierItem = Dictionary.Events.Take(eventIndex).Reverse().FirstOrDefault(o => o.Identifier != ev.Identifier);

				//Model側の追加位置に基づいて設定する
				if(beforeIdentifierItem != null)
					items.Insert(items.IndexOf(itemsDictionary[beforeIdentifierItem.Identifier]) + 1,item);
				else
					items.Insert(0, item);
			}

			item.AddEvent(ev);
			reverseDictionary[ev] = item;
		}

		//項目の削除
		private void RemoveEvent(EventModel ev)
		{
			var removeTarget = reverseDictionary[ev];
			if (removeTarget.ItemCount == 1)
			{
				itemsDictionary.Remove(ev.Identifier);
				items.Remove(removeTarget);
			}
			removeTarget.RemoveEvent(ev);
			reverseDictionary.Remove(ev);
		}

		//全項目の削除
		private void ClearEvents()
		{
			foreach(var ev in reverseDictionary.Keys.ToArray())
			{
				RemoveEvent(ev);
			}
		}

		//項目のリネーム
		private void RenameEvent(EventModel ev)
		{
			//名前が変更になった場合
			FileEventTreeItemEventViewModel eventViewModel = reverseDictionary[ev];
			string newLabel = ev.Identifier;
			string oldLabel = eventViewModel.Label;

			//名前が変更されてなければなにもしない
			if (newLabel == oldLabel)
				return;

			//移動先があるかを探す
			FileEventTreeItemEventViewModel moveTarget = null;
			if (itemsDictionary.ContainsKey(newLabel))
			{
				moveTarget = itemsDictionary[newLabel];
			}

			if (eventViewModel.ItemCount == 1)
			{
				if (moveTarget == null)
				{
					//アイテムが１個しかなく、移動先もない場合はその項目自体をリネームする
					eventViewModel.UpdateLabel();
					itemsDictionary.Remove(oldLabel);
					itemsDictionary.Add(newLabel, eventViewModel);
				}
				else
				{
					//アイテムが１個しかなく、移動先がある場合は項目を破棄し、既存の項目に合流する
					itemsDictionary.Remove(oldLabel);
					items.Remove(eventViewModel);
					eventViewModel.RemoveEvent(ev);
					moveTarget.AddEvent(ev);
					reverseDictionary[ev] = moveTarget;
				}
			}
			else
			{
				if (moveTarget == null)
				{
					//他の項目が存在し、移動先がない場合は項目を新設する
					eventViewModel.RemoveEvent(ev);

					var newItem = new FileEventTreeItemEventViewModel(this);
					newItem.AddEvent(ev);
					itemsDictionary.Add(newLabel, newItem);
					items.Add(newItem);
					reverseDictionary[ev] = newItem;
				}
				else
				{
					//他の項目が存在し、移動先がある場合はビューモデル間を移動させる
					eventViewModel.RemoveEvent(ev);
					moveTarget.AddEvent(ev);
					reverseDictionary[ev] = moveTarget;
				}
			}
		}

		internal void OnEventPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (sender is EventModel ev)
			{
				switch (e.PropertyName)
				{
					case nameof(EventModel.Identifier):
						RenameEvent(ev);
						break;
				}
			}
		}

		private void OnDictionaryPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(DictionaryModel.RelativeName):
					NotifyChanged(nameof(Label));
					break;
			}
		}

		//イベントの追加削除
		private void EventCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (object item in e.OldItems)
				{
					if (item is EventModel ev)
						RemoveEvent(ev);
				}
			}

			if (e.NewItems != null)
			{
				foreach (object item in e.NewItems)
				{
					if (item is EventModel ev)
						AddEvent(ev);
				}
			}

			if(e.Action == NotifyCollectionChangedAction.Reset)
			{
				ClearEvents();
			}
		}

		//開く
		public bool Open()
		{
			if (Dictionary.IsSerialized)
			{
				//シリアライズされてない版をエディタで開いく
				FileEventTree.Main.MainWindow.OpenTextEditor(Dictionary);
				return true;
			}
			return false;
		}

		//表示するかどうかのフィルタ関数
		public bool Filter(string filterString, object[] args)
		{
			if ((bool)args[0])
			{
				//イベント検索モード。ヒットする子があれば表示。
				return items.Any(o => o.Filter(filterString, args));
			}
			else
			{
				//ファイル名検索モード
				return Label.ToLower().Contains(filterString.ToLower());
			}
		}
	}

	//イベントにあたるViewModel
	internal class FileEventTreeItemEventViewModel : NotificationObject, Utility.SearchFilterConverter.IFilter
	{
		//ノードに所属するイベント
		private ObservableCollection<EventModel> events;
		private Dictionary<string, ObservableCollection<InlineEventModel>> inlineEvents;
		private ObservableCollection<FileEventTreeItemInlineEventViewModel> inlineEventViewModels;
		private string identifier;

		public string NodeType => "Event";
		public FileEventTreeItemDictionaryViewModel Dictionary { get; }
		public ActionCommand AddItemCommand { get; }
		public ActionCommand MoveItemCommand { get; }
		public ActionCommand RenameItemCommand { get; }
		public ActionCommand DeleteItemCommand { get; }

		public string Label
		{
			get => identifier;
			private set
			{
				identifier = value;
				NotifyChanged();
			}
		}

		public string Identifier
		{
			get => identifier;
		}

		public bool IsHeader
		{
			get => (events.FirstOrDefault()?.Type ?? EventType.Sentence) == EventType.Header;
		}

		public int ItemCount
		{
			get => events.Count;
		}

		public ReadOnlyObservableCollection<EventModel> Items
		{
			get => new ReadOnlyObservableCollection<EventModel>(events);
		}

		public IList<FileEventTreeItemInlineEventViewModel> ChildItems
		{
			get => new ReadOnlyObservableCollection<FileEventTreeItemInlineEventViewModel>(inlineEventViewModels);
		}

		public FileEventTreeItemEventViewModel(FileEventTreeItemDictionaryViewModel dict)
		{
			events = new ObservableCollection<EventModel>();
			inlineEventViewModels = new ObservableCollection<FileEventTreeItemInlineEventViewModel>();
			inlineEvents = new Dictionary<string, ObservableCollection<InlineEventModel>>();
			Dictionary = dict;

			//項目の追加
			AddItemCommand = new ActionCommand(
				o => Dictionary.FileEventTree.Main.OpenAddEventDialog(addTarget: Dictionary.Dictionary, name: events.First().Name, type: events.First().Type)
				);

			//項目の移動
			MoveItemCommand = new ActionCommand(
				o =>
				{
					var dialog = new DictionarySelectDialog(Dictionary.FileEventTree.Main);
					if(dialog.ShowDialog() == true && Dictionary.Dictionary != dialog.SelectedItem)
					{
						foreach(var item in events.ToArray())
						{
							item.MoveTo(dialog.SelectedItem);
						}
					}
				}
				);

			//項目のリネーム
			RenameItemCommand = new ActionCommand(
				o =>
				{
					var item = Items.First();
					var dialog = new AddEventDialog(Dictionary.FileEventTree.Main);
					dialog.DataContext.Name = item.Name;
					dialog.DataContext.Type = item.Type;
					dialog.DataContext.AddTarget = Dictionary.Dictionary;
					dialog.DataContext.IsFileSelectEnabled = false;

					//決定されたら名前をいじる
					if(dialog.ShowDialog() == true && ( item.Name != dialog.DataContext.Name || item.Type != dialog.DataContext.Type ))
					{
						foreach(var i in Items.ToArray())
						{
							i.Name = dialog.DataContext.Name;
							i.Type = dialog.DataContext.Type;
						}
					}
				}
				);

			//項目の削除
			DeleteItemCommand = new ActionCommand(
				o =>
				{
					var message = string.Format("{0}の「{1}」をまとめて削除します。よろしいですか？", Dictionary.Label, Label);
					var result = MessageBox.Show(message, "項目の削除", MessageBoxButton.YesNo, MessageBoxImage.Question);
					if (result != MessageBoxResult.Yes)
						return;

					foreach(var item in events.ToArray())
					{
						item.Remove();
					}
				}
				);

		}

		//インスタンス生成中のイベントの追加
		public void AddEvent(EventModel ev)
		{
			ev.PropertyChanged += Dictionary.OnEventPropertyChanged;
			events.Add(ev);
			foreach (var inlineEv in ev.InlineEvents)
				AddInlineEvent(inlineEv);

			INotifyCollectionChanged inlineEvents = ev.InlineEvents;
			inlineEvents.CollectionChanged += InlineEvents_CollectionChanged;

			if (identifier == null)
			{
				identifier = ev.Identifier;
			}
		}

		//イベントを取り除く
		public void RemoveEvent(EventModel ev)
		{
			ev.PropertyChanged -= Dictionary.OnEventPropertyChanged;
			INotifyCollectionChanged inlineEvents = ev.InlineEvents;
			inlineEvents.CollectionChanged -= InlineEvents_CollectionChanged;

			foreach (var inlineEv in ev.InlineEvents)
				RemoveInlineEvent(inlineEv);
			events.Remove(ev);
		}

		//イベントを移動
		public void MoveTo(FileEventTreeItemDictionaryViewModel targetDic)
		{
			foreach (var item in events.ToArray())
			{
				item.MoveTo(targetDic.Dictionary);
			}
		}

		//イベント名の更新
		public void UpdateLabel()
		{
			Label = events.First().Identifier;
		}

		//インラインイベント処理
		private void InlineEvents_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (var item in e.OldItems)
					RemoveInlineEvent((InlineEventModel)item);
			}
			if (e.NewItems != null)
			{
				foreach (var item in e.NewItems)
					AddInlineEvent((InlineEventModel)item);
			}

			if(e.Action == NotifyCollectionChangedAction.Reset)
			{
				ClearInlineEvent();
			}
		}

		public void AddInlineEvent(InlineEventModel ev)
		{
			//追加。必要に応じてviewModelも足す
			ObservableCollection<InlineEventModel> inlineEventList;
			if (!inlineEvents.TryGetValue(ev.Identifier, out inlineEventList))
			{
				inlineEventList = new ObservableCollection<InlineEventModel>();
				inlineEvents.Add(ev.Identifier, inlineEventList);
				inlineEventList.Add(ev);
				var newViewModel = new FileEventTreeItemInlineEventViewModel(this, inlineEventList);
				inlineEventViewModels.Add(newViewModel);
			}
			else
			{
				inlineEventList.Add(ev);
			}
		}

		public void RemoveInlineEvent(InlineEventModel ev)
		{
			//Viewmodelを削除
			ObservableCollection<InlineEventModel> inlineEventList = inlineEvents[ev.Identifier];
			inlineEventList.Remove(ev); ;

			//同名のものがなくなったらViewModelを取り除く
			if(inlineEventList.Count == 0)
			{
				inlineEvents.Remove(ev.Identifier);

				//同じイベントをもつものを取り除く
				inlineEventViewModels.Remove(inlineEventViewModels.First(o => o.EventList.Count == 0));
			}
		}

		public void ClearInlineEvent()
		{
			foreach(var evList in inlineEvents.ToArray())
			{
				foreach(var ev in evList.Value.ToArray())
				{
					RemoveInlineEvent(ev);
				}
			}
		}

		//最初の１つをイベントエディタで開く。１つしかアイテムがない場合にダブルクリック時エディタを開くためのもの
		public void OpenFirstEventEditor()
		{
			if(Items.Count > 0)
				Dictionary.FileEventTree.Main.OpenEventEditor(Items.First());
		}

		public bool Filter(string filterString, object[] args)
		{
			if ((bool)args[0])
			{
				return Label.ToLower().Contains(filterString.ToLower()) || inlineEventViewModels.Any(o => o.Filter(filterString, args));
			}
			else
			{
				return true;
			}
		}
	}

	//インラインイベントにあたる
	//末端なので簡易なつくり
	internal class FileEventTreeItemInlineEventViewModel : NotificationObject, Utility.SearchFilterConverter.IFilter
	{
		private FileEventTreeItemEventViewModel parentViewModel;
		public string NodeType => "InlineEvent";
		public DictionaryModel Dictionary => parentViewModel.Dictionary.Dictionary;
		public IEnumerable<object> ChildItems => Array.Empty<object>();
		public ReadOnlyObservableCollection<InlineEventModel> EventList { get; }
		public string Label { get; }

		public FileEventTreeItemInlineEventViewModel(FileEventTreeItemEventViewModel parent, ObservableCollection<InlineEventModel> events)
		{
			parentViewModel = parent;
			EventList = new ReadOnlyObservableCollection<InlineEventModel>(events);
			Label = events.First().Identifier;
		}

		public void OpenFirstEventEditor()
		{
			if (EventList.Count > 0)
				parentViewModel.Dictionary.FileEventTree.Main.OpenEventEditor(EventList.First());
		}

		public bool Filter(string filterString, object[] args)
		{
			if ((bool)args[0])
			{
				return Label.ToLower().Contains(filterString.ToLower());
			}
			else
			{
				return true;	//イベント検索モードではないので全ヒット
			}
		}
	}
}