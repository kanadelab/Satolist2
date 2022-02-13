using ICSharpCode.AvalonEdit.Document;
using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
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
	/// EventEditor.xaml の相互作用ロジック
	/// </summary>
	public partial class EventEditor : UserControl
	{
		public EventEditor()
		{
			InitializeComponent();
			MainTextEditor.SyntaxHighlighting = new SatoriSyntaxHilighter();
		}
	}

	internal struct EventTypeComboBoxItem
	{
		public string Name
		{
			get;set;
		}

		public EventType Type
		{
			get;set;
		}
	}

	internal class EventEditorViewModel : NotificationObject, IDockingWindowContent, IDisposable
	{
		private bool disableBodyPropertyChanged;

		public EventModel Event { get; }
		public string randomizedContentId;
		public static readonly EventTypeComboBoxItem[] eventTypeList =
		{
			new EventTypeComboBoxItem(){Name="文(" + Constants.SentenceHead +")", Type=EventType.Sentence  },
			new EventTypeComboBoxItem(){Name="単語群(" + Constants.WordHead+")", Type=EventType.Word }
		};

		public EventTypeComboBoxItem[] EventTypeList
		{
			get => eventTypeList;
		}

		public string DockingTitle => Event.Name;

		public string DockingContentId => randomizedContentId;

		public TextDocument Document { get; }

		public ActionCommand SendToGhostCommand {get;}

		public EventEditorViewModel(MainViewModel main, EventModel ev)
		{
			//TODO: 閉じるときにイベントを取り除く必要がありそう
			randomizedContentId = Guid.NewGuid().ToString();	//複数出現するのでユニークなIDを振る
			Event = ev;
			Event.PropertyChanged += Event_PropertyChanged;

			Document = new TextDocument(Event.Body);
			Document.TextChanged += Document_TextChanged;

			//コマンド
			SendToGhostCommand = new ActionCommand(
				o =>
				{
					Satorite.SendSatori(main.Ghost, Document.Text, Event.Type);
				}
				);
		}

		private void Document_TextChanged(object sender, EventArgs e)
		{
			//メインのテキストが変更された場合にModelに伝える
			//その際PropertyChangedは呼ばないようにしておく
			disableBodyPropertyChanged = true;
			Event.Body = Document.Text;
			disableBodyPropertyChanged = false;
		}

		private void Event_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(EventModel.Name))
				NotifyChanged(nameof(DockingTitle));
			else if (e.PropertyName == nameof(EventModel.Body) && !disableBodyPropertyChanged)
				Document.Text = Event.Body;
		}

		public void Dispose()
		{
			Event.PropertyChanged -= Event_PropertyChanged;
			Document.TextChanged -= Document_TextChanged;
		}
	}

	
}
