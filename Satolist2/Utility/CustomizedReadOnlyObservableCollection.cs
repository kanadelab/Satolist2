using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Utility
{
	/// <summary>
	/// ビルトイン版だと完全なプロキシではなくイベントのインスタンス自体は内部に保持しており、
	/// イベントハンドラの追加削除をしたい場合にその場でインスタンスを返すプロパティの記法ではつかいずらいので完全なるプロキシとして別途用意したクラス
	/// </summary>
	public class CustomizedReadOnlyObservableCollection<T> : System.Collections.ObjectModel.ReadOnlyCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
	{
		protected ObservableCollection<T> internalList;

		event NotifyCollectionChangedEventHandler INotifyCollectionChanged.CollectionChanged
		{
			add
			{
				internalList.CollectionChanged += value;
			}
			remove
			{
				internalList.CollectionChanged -= value;
			}
		}

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add
			{
				((INotifyPropertyChanged)internalList).PropertyChanged += value;
			}
			remove
			{
				((INotifyPropertyChanged)internalList).PropertyChanged -= value;
			}
		}

		public CustomizedReadOnlyObservableCollection(ObservableCollection<T> list)
			: base(list)
		{
			internalList = list;
		}
	}
}
