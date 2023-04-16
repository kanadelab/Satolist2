using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
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
	/// AddEventDialog.xaml の相互作用ロジック
	/// </summary>
	public partial class AddEventDialog : DialogContentBase
	{
		internal new AddEventDialogViewModel DataContext
		{
			get => base.DataContext as AddEventDialogViewModel;
			set
			{
				base.DataContext = value;
			}
		}

		internal AddEventDialog(MainViewModel main)
		{
			InitializeComponent();
			Owner = main.MainWindow.RootWindow;
			DataContext = new AddEventDialogViewModel(main);
			DataContext.AddTarget = DataContext.Main.Ghost.Dictionaries.FirstOrDefault();
			InitialFocus = NameInputBox;

			//入力ボックスを全選択にして直接編集しやすいように
			Dispatcher.BeginInvoke(new Action(()=>
			{
				NameInputBox.SelectAll();
			}), System.Windows.Threading.DispatcherPriority.Render);
		}

		private void OkButtonClick(object sender, RoutedEventArgs e)
		{
			//追加確定時、通知を送る "EVENT", "WORD" は過去仕様から
			Satorite.NotifySSTPBroadcast("OnSatolistEventAdded", DataContext.Name, DataContext.AddTarget.Name, DataContext.Type == EventType.Sentence ? "EVENT" : "WORD", DataContext.Condition);

			DialogResult = true;
			Close();
		}

		private void CancelButtonClick(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}

	//項目の追加ダイアログ
	internal class AddEventDialogViewModel : NotificationObject
	{
		private string name;
		private string condition;
		private EventType type;
		private DictionaryModel addTarget;
		private bool isFileSelectEnabled;

		public MainViewModel Main { get; }

		//項目名
		public string Name
		{
			get => name ?? string.Empty;
			set
			{
				name = value;
				NotifyChanged();
			}
		}

		//条件 ウインドウには出さないけど追加項目の内容として保持
		public string Condition
		{
			get => condition ?? string.Empty;
			set
			{
				condition = value;
				NotifyChanged();
			}
		}
		
		//追加先の辞書
		public DictionaryModel AddTarget
		{
			get => addTarget;
			set
			{
				addTarget = value;
				NotifyChanged();
			}
		}

		//追加するのは「単語群」か
		public bool IsWordSelected
		{
			get => type == EventType.Word;
			set
			{
				if (value)
					type = EventType.Word;
				NotifyChanged();
				NotifyChanged(nameof(IsSentenceSelected));
				NotifyChanged(nameof(Type));
			}
		}

		//追加するのは「文」か
		public bool IsSentenceSelected
		{
			get => type == EventType.Sentence;
			set
			{
				if (value)
					type = EventType.Sentence;
				NotifyChanged();
				NotifyChanged(nameof(IsWordSelected));
				NotifyChanged(nameof(Type));
			}
		}

		//enumで追加するものの選択
		//headerは想定してない
		public EventType Type
		{
			get => type;
			set
			{
				type = value;
				NotifyChanged();
				NotifyChanged(nameof(IsWordSelected));
				NotifyChanged(nameof(IsSentenceSelected));
			}
		}

		public bool IsFileSelectEnabled
		{
			get => isFileSelectEnabled;
			set
			{
				isFileSelectEnabled = value;
				NotifyChanged(); 
			}
		}

		public AddEventDialogViewModel(MainViewModel main)
		{
			Main = main;
			isFileSelectEnabled = true;
		}
	}
}
