using Satolist2.Dialog;
using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
	/// EventList.xaml の相互作用ロジック
	/// </summary>
	public partial class EventList : UserControl
	{
		public EventList()
		{
			InitializeComponent();
		}

		private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if(sender is ListViewItem item)
			{
				if(item.DataContext is EventListItemViewModel itemViewModel)
				{
					itemViewModel.OpenEditor();
					e.Handled = true;
				}
			}
		}

		private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//サイズ更新
			//いい感じのサイズ感にしておく
			EventBodyColumn.Width = MainListView.ActualWidth * 0.7;
			EventConditionColumn.Width = MainListView.ActualWidth * 0.3;
		}
	}

	internal class EventListViewModel : NotificationObject, IDockingWindowContent
	{
		public const string ContentId = "EventList";
		private ObservableCollection<EventListItemViewModel> items;
		private INotifyCollectionChanged itemsSource;
		private string eventTitleLabel;
		private string dockingTitle;

		public MainViewModel Main { get; }

		//NOTE: ファイルイベントツリーのViewModelから直接受け取っているので、自動で削除が反映される
		//分離したほうがいいのかもしれないけど・・・
		public ReadOnlyObservableCollection<EventListItemViewModel> Items
		{
			get => new ReadOnlyObservableCollection<EventListItemViewModel>(items);
		}

		public string DockingTitle
		{
			get => dockingTitle;
			private set
			{
				dockingTitle = value;
				NotifyChanged();
			}
		}

		public string DockingContentId => ContentId;

		public EventListViewModel(MainViewModel main)
		{
			dockingTitle = "単語群・文リスト";
			Main = main;
			items = new ObservableCollection<EventListItemViewModel>();
		}

		//選択中のアイテムのエディタをオープン
		public void OpenSelectedItemEditor()
		{
			foreach(var item in Items.Where(o=> o.IsSelected))
			{
				if (item.InlineEvent != null)
				{
					Main.OpenEventEditor(item.InlineEvent);
				}
				else
				{
					Main.OpenEventEditor(item.Event);
				}
			}
		}

		//選択中のアイテムを削除
		public void RemoveSelectedItem()
		{
			//コレクションが変化するのでToArray()でフリーズする
			foreach(var item in Items.Where(o => o.IsSelected).ToArray())
			{
				item.Event.Dictionary.RemoveEvent(item.Event);
			}
		}

		//選択中のアイテムを別のファイルに移動
		public void MoveSelectedItem()
		{
			var dialog = new DictionarySelectDialog(Main);
			if (dialog.ShowDialog() == true)
			{
				foreach (var item in Items.Where(o => o.IsSelected).ToArray())
				{
					if (item.Event.Dictionary != dialog.SelectedItem)
					{
						item.Event.MoveTo(dialog.SelectedItem);
					}
				}
			}
		}

		//新規アイテムの追加
		public void AddSameNameItem(EventModel ev, bool sameCondition)
		{
			Main.OpenAddEventDialog(
				name: ev.Name,
				condition: sameCondition ? ev.Condition : null,
				addTarget: ev.Dictionary,
				type: ev.Type
				);
		}

		//表示するアイテムを投入
		//ファイルイベントツリーがソースになっているので、そちらがわから変更は伝播してくる
		public void SetItems(ReadOnlyObservableCollection<EventModel> events, string eventLabel, DictionaryModel dictionary)
		{
			//アイテムソースを保持
			if(itemsSource != null)
			{
				//不要な増減のハンドラを取り除く
				itemsSource.CollectionChanged -= SourceCollectionChanged;
			}
			itemsSource = events;
			itemsSource.CollectionChanged += SourceCollectionChanged;

			//ビューモデルを投入
			items.Clear();
			foreach(var ev in events)
			{
				var newItem = new EventListItemViewModel(this, ev);
				items.Add(newItem);
			}

			eventTitleLabel = string.Format("{0}/{1}", dictionary.Name, eventLabel);
			DockingTitle = string.Format("{0} ({1})", eventTitleLabel, items.Count);
		}

		public void SetItems(ReadOnlyObservableCollection<InlineEventModel> events, string eventLabel, DictionaryModel dictionary)
		{
			//アイテムソースを保持
			if(itemsSource != null)
			{
				//不要な増減のハンドラを取り除く
				itemsSource.CollectionChanged -= SourceCollectionChanged;
			}
			itemsSource = events;
			itemsSource.CollectionChanged += SourceCollectionChanged;

			//ビューモデルを投入
			items.Clear();
			foreach(var ev in events)
			{
				var newItem = new EventListItemViewModel(this, ev);
				items.Add(newItem);
			}

			eventTitleLabel = string.Format("{0}/{1}", dictionary.Name, eventLabel);
			DockingTitle = string.Format("{0} ({1})", eventTitleLabel, items.Count);
		}

		private void SourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null)
			{
				foreach (object item in e.NewItems)
				{
					if (item is EventModel ev)
						items.Add(new EventListItemViewModel(this, ev));
					else if (item is InlineEventModel inlineEv)
						items.Add(new EventListItemViewModel(this, inlineEv));
				}
			}

			if (e.OldItems != null)
			{
				foreach (object item in e.OldItems)
				{
					if (item is EventModel ev)
						items.Remove(items.First(o => o.Event == ev));
					else if (item is InlineEventModel inlineEv)
						items.Remove(items.First(o => o.InlineEvent == inlineEv));
				}
			}

			if(e.Action == NotifyCollectionChangedAction.Reset)
			{
				items.Clear();
			}
			DockingTitle = string.Format("{0} ({1})", eventTitleLabel, items.Count);
		}

	}

	internal class EventListItemViewModel : NotificationObject
	{
		private bool isSelected;

		//参照しているアイテム
		public EventModel Event { get; }
		public InlineEventModel InlineEvent { get; }
		public EventListViewModel Parent { get; }

		public ActionCommand OpenCommand { get; }
		public ActionCommand AddItemCommand { get; }
		public ActionCommand AddSameConditionItemCommand { get; }
		public ActionCommand MoveItemCommand { get; }
		public ActionCommand RemoveItemCommand { get; }

		public bool IsSelected
		{
			get => isSelected;
			set
			{
				isSelected = value;
				NotifyChanged();
			}
		}

		public EventListItemViewModel(EventListViewModel parent, EventModel ev)
		{
			Parent = parent;
			Event = ev;
			InlineEvent = null;

			//コンテキストメニューコマンド
			OpenCommand = new ActionCommand(
				o => Parent.OpenSelectedItemEditor()
				);
			RemoveItemCommand = new ActionCommand(
				o =>
				{
					var message = "選択した項目を削除します。よろしいですか？";
					var result = MessageBox.Show(message, "項目の削除", MessageBoxButton.YesNo, MessageBoxImage.Question);
					if (result != MessageBoxResult.Yes)
						return;
					Parent.RemoveSelectedItem();
				}
				);
			AddItemCommand = new ActionCommand(
				o => Parent.AddSameNameItem(Event, false)
				);
			MoveItemCommand = new ActionCommand(
				o =>
				{
					Parent.MoveSelectedItem();
				}
				);
			AddSameConditionItemCommand = new ActionCommand(
				o => parent.AddSameNameItem(Event, true)
				);
		}

		//インラインイベントの表示
		public EventListItemViewModel(EventListViewModel parent, InlineEventModel ev)
		{
			Parent = parent;
			Event = ev.InlineEvent;
			InlineEvent = ev;

			OpenCommand = new ActionCommand(
				o => Parent.OpenSelectedItemEditor()
				);
			MoveItemCommand = new ActionCommand(
				null, false
				);
			RemoveItemCommand = new ActionCommand(
				null, false
				);
			AddItemCommand = new ActionCommand(
				null, false
				);
			AddSameConditionItemCommand = new ActionCommand(
				null, false
				);
		}

		public void OpenEditor()
		{
			if (InlineEvent != null)
				Parent.Main.OpenEventEditor(InlineEvent);
			else
				Parent.Main.OpenEventEditor(Event);
		}

	}
}
