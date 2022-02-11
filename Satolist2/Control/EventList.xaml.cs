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
				}
			}
		}
	}

	internal class EventListViewModel : NotificationObject, IDockingWindowContent
	{
		private ObservableCollection<EventListItemViewModel> items;
		private INotifyCollectionChanged itemsSource;
		private string dockingTitle;

		public MainViewModel Main { get; }

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

		public string DockingContentId => "EventList";

		public EventListViewModel(MainViewModel main)
		{
			dockingTitle = "単語群・文リスト";
			Main = main;
			items = new ObservableCollection<EventListItemViewModel>();
		}

		//表示するアイテムを投入
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

			DockingTitle = dictionary.Name + "/" + eventLabel;
			NotifyChanged(nameof(DockingTitle));
		}

		private void SourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch(e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach(object item in e.NewItems)
					{
						if(item is EventModel ev)
						{
							items.Add(new EventListItemViewModel(this, ev));
						}
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					foreach(object item in e.OldItems)
					{
						if(item is EventModel ev)
						{
							items.Remove(items.First(o => o.Event == ev));
						}
					}
					break;
			}
		}

	}

	internal class EventListItemViewModel : NotificationObject
	{
		//参照しているアイテム
		public EventModel Event { get; }
		public EventListViewModel Parent { get; }

		public EventListItemViewModel(EventListViewModel parent, EventModel ev)
		{
			Parent = parent;
			Event = ev;
		}

		public void OpenEditor()
		{
			Parent.Main.OpenEventEditor(Event);
		}

	}
}
