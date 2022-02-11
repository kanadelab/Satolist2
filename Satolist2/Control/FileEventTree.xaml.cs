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


namespace Satolist2.Control
{
	/// <summary>
	/// FileEventTree.xaml の相互作用ロジック
	/// </summary>
	public partial class FileEventTree : UserControl
	{
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
			}
		}
	}

	internal class FileEventTreeViewModel : NotificationObject, IDockingWindowContent
	{
		private ObservableCollection<FileEventTreeItemDictionaryViewModel> dictionaries;

		public MainViewModel Main { get; }
		public GhostModel Ghost
		{
			get => Main.Ghost;
		}

		public ReadOnlyObservableCollection<FileEventTreeItemDictionaryViewModel> Dictionaries
		{
			get => new ReadOnlyObservableCollection<FileEventTreeItemDictionaryViewModel>(dictionaries);
		}

		public string DockingTitle => "ファイルイベントツリー";

		public string DockingContentId => "FileEventTree";

		public FileEventTreeViewModel(MainViewModel main)
		{
			Main = main;
			dictionaries = new ObservableCollection<FileEventTreeItemDictionaryViewModel>();

			if (Ghost != null)
			{
				//辞書生成
				foreach (var dic in Ghost.Dictionaries)
				{
					var dictViewModel = new FileEventTreeItemDictionaryViewModel(this, dic);
					dictionaries.Add(dictViewModel);
				}
			}
		}
	}

	//辞書ファイルにあたる
	internal class FileEventTreeItemDictionaryViewModel : NotificationObject
	{
		private ObservableCollection<FileEventTreeItemEventViewModel> items;
		private Dictionary<string, FileEventTreeItemEventViewModel> itemsDictionary;
		private Dictionary<EventModel, FileEventTreeItemEventViewModel> reverseDictionary;  //逆引き
		private ObservableCollection<System.Windows.Controls.Control> contextMenuItems;

		public FileEventTreeViewModel FileEventTree { get; }
		public DictionaryModel Dictionary { get; }

		public ReadOnlyObservableCollection<FileEventTreeItemEventViewModel> ChildItems
		{
			get => new ReadOnlyObservableCollection<FileEventTreeItemEventViewModel>(items);
		}

		//ファイルイベントツリー上で表示する名前
		public string Label
		{
			get => Dictionary.RelativeName;
		}

		public ReadOnlyObservableCollection<System.Windows.Controls.Control> ContextMenuItems
		{
			get => new ReadOnlyObservableCollection<System.Windows.Controls.Control>(contextMenuItems);
		}

		public FileEventTreeItemDictionaryViewModel(FileEventTreeViewModel parent, DictionaryModel dictionary)
		{
			FileEventTree = parent;
			Dictionary = dictionary;
			contextMenuItems = new ObservableCollection<System.Windows.Controls.Control>();

			items = new ObservableCollection<FileEventTreeItemEventViewModel>();
			itemsDictionary = new Dictionary<string, FileEventTreeItemEventViewModel>();
			reverseDictionary = new Dictionary<EventModel, FileEventTreeItemEventViewModel>();

			//コンテキストメニュー
			//TODO: コンテキストメニュー実装
			{
				var addItem = new MenuItem();
				addItem.Header = "項目の追加";
				addItem.Command = new ActionCommand(o =>
				{
					var dialog = new AddEventDialog(parent.Main);
					if(dialog.ShowDialog()==true)
					{
						//新規で追加
						dialog.DataContext.AddTarget.AddEvent(new EventModel(dialog.DataContext.Type, dialog.DataContext.Name, dialog.DataContext.Condition, string.Empty));
					}
				});
				contextMenuItems.Add(addItem);
				contextMenuItems.Add(new Separator());
			}

			//イベントハンドラの用意
			Dictionary.PropertyChanged += OnDictionaryPropertyChanged;
			INotifyCollectionChanged collectionChanged = Dictionary.Events;
			collectionChanged.CollectionChanged += EventCollectionChanged;

			//dictionaryから同名のeventsを引っ張ってくる
			foreach (var ev in Dictionary.Events)
			{
				ev.PropertyChanged += OnEventPropertyChanged;

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
			if (itemsDictionary.ContainsKey(ev.Identifier))
			{
				itemsDictionary[ev.Identifier].AddEvent(ev);
			}
			else
			{
				var newItem = new FileEventTreeItemEventViewModel(this);
				newItem.AddEvent(ev);
				itemsDictionary.Add(ev.Identifier, newItem);
				items.Add(newItem);
				reverseDictionary[ev] = newItem;
			}
		}

		//項目の削除
		private void RemoveEvent(EventModel ev)
		{
			var removeTarget = reverseDictionary[ev];
			if (removeTarget.ItemCount == 1)
			{
				itemsDictionary.Remove(ev.Identifier);
				items.Remove(removeTarget);
				reverseDictionary.Remove(ev);
			}
			else
			{
				removeTarget.RemoveEvent(ev);
			}
		}

		//項目のリネーム
		private void RenameEvent(EventModel ev)
		{
			//名前が変更になった場合
			FileEventTreeItemEventViewModel eventViewModel = reverseDictionary[ev];
			string newLabel = ev.Identifier;
			string oldLabel = eventViewModel.Label;

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
				}
				else
				{
					//アイテムが１個しかなく、移動先がある場合は項目を破棄し、既存の項目に合流する
					itemsDictionary.Remove(oldLabel);
					items.Remove(eventViewModel);
					reverseDictionary[ev] = moveTarget;
				}
			}
			else
			{
				if (moveTarget == null)
				{
					//他の項目が存在し、移動先がない場合は項目を新設する
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

		private void OnEventPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (object item in e.NewItems)
					{
						if(item is EventModel ev)
							AddEvent(ev);
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					foreach(object item in e.OldItems)
					{
						if(item is EventModel ev)
							RemoveEvent(ev);
					}
					break;
			}
		}
	}

	//イベントにあたる
	internal class FileEventTreeItemEventViewModel : NotificationObject
	{
		//ノードに所属するイベント
		private ObservableCollection<EventModel> events;
		private string identifier;

		public FileEventTreeItemDictionaryViewModel Dictionary { get; }

		public string Label
		{
			get => identifier;
			private set
			{
				identifier = value;
				NotifyChanged();
			}
		}

		public int ItemCount
		{
			get => events.Count;
		}

		public ReadOnlyObservableCollection<EventModel> Items
		{
			get => new ReadOnlyObservableCollection<EventModel>(events);
		}

		public IList<FileEventTreeItemEventViewModel> ChildEvent
		{
			get => null;
		}


		public FileEventTreeItemEventViewModel(FileEventTreeItemDictionaryViewModel dict)
		{
			events = new ObservableCollection<EventModel>();
			Dictionary = dict;
		}

		//インスタンス生成中のイベントの追加
		public void AddEvent(EventModel ev)
		{
			events.Add(ev);

			if (identifier == null)
			{
				identifier = ev.Identifier;
			}
		}

		//イベントを取り除く
		public void RemoveEvent(EventModel ev)
		{
			Debug.Assert(ItemCount > 1);    //最後のアイテムを除去することは考えてない
			events.Remove(ev);
		}

		//イベント名の更新
		public void UpdateLabel()
		{
			Label = events.First().Identifier;
		}

	}
}