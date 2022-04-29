using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using ICSharpCode.AvalonEdit.Document;
using Satolist2.Utility;

namespace Satolist2.Control
{
	/// <summary>
	/// RecvEventLog.xaml の相互作用ロジック
	/// </summary>
	public partial class RecvEventLog : UserControl
	{
		public RecvEventLog()
		{
			InitializeComponent();
		}

		private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//サイズ更新
			//いい感じのサイズ感にしておく
			RecvEventLogTypeColumn.Width = MainList.ActualWidth * 0.15;
			RecvEventLogNameColmn.Width = MainList.ActualWidth * 0.15;
			RecvEventLogResponseColmn.Width = MainList.ActualWidth * 0.55;
			RecvEventLogTimestampColumn.Width = MainList.ActualWidth * 0.15;
		}

		private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			//ダブルクリックされたら開く
			if (sender is ListViewItem item)
			{
				if (item.DataContext is RecvEventLogListItemViewModel itemViewModel)
				{
					itemViewModel.OpenTextEditor();
				}
			}
		}
	}

	internal class RecvEventLogViewModel : NotificationObject, IDockingWindowContent, IDisposable, IControlBindedReceiver
	{
		public const string RecvRequestSeparator = "--- Request ---";
		public const string RecvResponseSeparator = "--- Response ---";
		public const string RecvOperationSeparator = "--- Operation ---";

		public const string ContentId = "RecgvEventLog";
		private RecvEventLog control;
		private RecvEventLogListItemViewModel receivingItem;
		private ObservableCollection<RecvEventLogListItemViewModel> items;
		private string searchString;
		private bool isRunning;
		private bool isSuspendLogging;
		private bool isAutoScroll;

		public string DockingTitle => "れしばイベントログ";

		public string DockingContentId => ContentId;
		public MainViewModel Main { get; }
		public TextDocument RealtimeViewDocument { get; }
		public ActionCommand BootRecvCommand { get; }
		public ActionCommand KillRecvCommand { get; }
		public ActionCommand ClearLogCommand { get; }
		public ReadOnlyObservableCollection<RecvEventLogListItemViewModel> Items
		{
			get => new ReadOnlyObservableCollection<RecvEventLogListItemViewModel>(items);
		}
		public ICollectionView FilteredItems
		{
			get;
		}

		//検索
		public string SearchString
		{
			get => searchString;
			set
			{
				searchString = value;
				FilteredItems.Refresh();
				NotifyChanged();
			}
		}

		//れしばが起動しているか
		public bool IsRunning
		{
			get => isRunning;
			set
			{
				isRunning = value;
				NotifyChanged();
			}
		}

		//ロギングの一時停止
		public bool IsSuspendLogging
		{
			get => isSuspendLogging;
			set
			{
				isSuspendLogging = value;
				NotifyChanged();
			}
		}

		//自動スクロールの有効化
		public bool IsAutoScroll
		{
			get => isAutoScroll;
			set
			{
				isAutoScroll = value;
				NotifyChanged();
			}
		}

		public RecvEventLogViewModel(MainViewModel main)
		{
			Main = main;
			items = new ObservableCollection<RecvEventLogListItemViewModel>();
			IsAutoScroll = true;
			FilteredItems = CollectionViewSource.GetDefaultView(items);
			FilteredItems.Filter = (o) =>
			{
				if(o is RecvEventLogListItemViewModel item)
				{
					//検索なし
					if (string.IsNullOrEmpty(SearchString))
						return true;

					if(item.EventName.IndexOf(SearchString) >= 0)
					{
						return true;
					}
				}
				return false;
			};

			RealtimeViewDocument = new TextDocument();

			BootRecvCommand = new ActionCommand(
				//れしばを起動
				o =>
				{
					if (!Core.SatoriRecvNativeWindow.IsCreated)
					{
						//れしばを起動
						Core.SatoriRecvNativeWindow.Create((new System.Windows.Interop.WindowInteropHelper(Main.MainWindow)).Handle, OnReceive);

						//SHIORIを読み直し(里々の再起動によってれしばに接続)
						if(Main.Ghost != null)
						{
							GhostRuntimeRequest.ReloadShiori(Main.Ghost, "れしばに接続するため、ゴーストをリロードしました。");
						}
						IsRunning = true;
					}
				}
				);

			KillRecvCommand = new ActionCommand(
				o =>
				{
					if (Core.SatoriRecvNativeWindow.IsCreated)
					{
						//れしばを終了
						Core.SatoriRecvNativeWindow.Destroy();
						IsRunning = false;
					}
				}
				);

			ClearLogCommand = new ActionCommand(
				//ログをクリア
				o =>
				{
					items.Clear();
					RealtimeViewDocument.Text = String.Empty;
				}
				);
		}

		public void OnReceive(string data)
		{
			//バイト値系の情報は加工する
			data = data.Replace(((char)0).ToString(), "");
			data = data.Replace(((char)1).ToString(), " ");

			if(data == RecvRequestSeparator)
			{
				if (isSuspendLogging)
				{
					//中断中であればそのイベントは受信しない
					receivingItem = null;
				}
				else
				{
					//これが来たら新しいイベントの受信開始
					receivingItem = new RecvEventLogListItemViewModel(this);
				}
			}

			if (receivingItem != null)
			{
				//内容を追加して、末尾へ。
				RealtimeViewDocument.Insert(RealtimeViewDocument.TextLength, data + "\r\n");

				bool eventNameReceived = !string.IsNullOrEmpty(receivingItem.EventName);
				//現在受信中のアイテムにも内容を足す
				receivingItem.OnReceive(data);

				if(!eventNameReceived && !string.IsNullOrEmpty(receivingItem.EventName))
				{
					//イベント名が確定したら、内容を追加する(フィルタに使用しているので、確定してからリストに追加しないとフィルタが正しく適用されない)
					items.Add(receivingItem);
					eventNameReceived = true;
				}

				if (IsAutoScroll)
				{
					//自動スクロールを設定している場合は末尾までスクロール
					control.RealtimeLog.ScrollToEnd();
					if(eventNameReceived)
						control.MainList.ScrollIntoView(receivingItem);
				}
			}
		}

		public void Dispose()
		{
			//れしばを終了
			if(Core.SatoriRecvNativeWindow.IsCreated)
				Core.SatoriRecvNativeWindow.Destroy();
		}

		public void ControlBind(System.Windows.Controls.Control ctrl)
		{
			if(ctrl is RecvEventLog control)
			{
				this.control = control;
			}
		}
	}

	internal class RecvEventLogListItemViewModel : NotificationObject
	{
		private enum LogSection
		{
			Request,
			Operation,
			Response
		}

		private StringBuilder rawEventBody;
		private string eventProtocol;
		private string eventName;
		private string response;
		private DateTime timestamp;
		private LogSection currentSection;

		public RecvEventLogViewModel Parent { get; }

		public string RawEventBody
		{
			get => rawEventBody.ToString();
		}

		public string EventProtocol
		{
			get => eventProtocol ?? string.Empty;
			set
			{
				eventProtocol = value;
				NotifyChanged();
			}
		}

		public string EventName
		{
			get => eventName ?? string.Empty;
			set
			{
				eventName = value;
				NotifyChanged();
			}
		}

		public string Response
		{
			get => response ?? string.Empty;
			set
			{
				response = value;
				NotifyChanged();
			}
		}

		public string Timestamp
		{
			get => timestamp.ToString();
		}

		public RecvEventLogListItemViewModel(RecvEventLogViewModel parent)
		{
			Parent = parent;
			timestamp = DateTime.Now;
			rawEventBody = new StringBuilder();
			currentSection = LogSection.Request;
		}

		public void OpenTextEditor()
		{
			Parent.Main.OpenTemporaryTextEditor(RawEventBody, string.Concat("[log]", EventName));
		}

		public void OnReceive(string data)
		{
			//全容に追加
			rawEventBody.AppendLine(data);
			NotifyChanged(nameof(RawEventBody));

			//先頭レコードなので意味はない
			if (data == RecvEventLogViewModel.RecvRequestSeparator)
				return;

			//-- 各レコードを解析

			//最初に来るのがプロトコルのはず
			if (eventProtocol == null)
			{
				EventProtocol = data;
				return;
			}

			switch (data)
			{
				case RecvEventLogViewModel.RecvRequestSeparator:
					return;
				case RecvEventLogViewModel.RecvOperationSeparator:
					currentSection = LogSection.Operation;
					return;
				case RecvEventLogViewModel.RecvResponseSeparator:
					currentSection = LogSection.Response;
					return;
			}

			if (currentSection == LogSection.Request)
			{
				//リクエストヘッダで必要なものを解析する
				if(data.IndexOf("ID: ") == 0)
				{
					EventName = data.Substring("ID: ".Length);
				}
			}
			else if(currentSection == LogSection.Response)
			{
				if (data.IndexOf("Value=") == 0)
				{
					Response = data.Substring("Value=".Length);
				}
			}
		}

	}
}
