using ICSharpCode.AvalonEdit.Document;
using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Satolist2.Dialog;
using ICSharpCode.AvalonEdit.Highlighting;

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

		internal void UpdateInsertPaletteKeyBindings(InsertItemPaletteModel palette, ICommand command)
		{
			//古いバインディング削除
			List<InputBinding> removes = new List<InputBinding>();
			foreach(var item in MainTextEditor.InputBindings)
			{
				if (item is InsertPaletteKeyBinding b)
					removes.Add(b);
			}

			foreach(var item in removes)
			{
				MainTextEditor.InputBindings.Remove(item);
			}

			//バインディングの作成
			var items = palette?.AllItems() ?? Array.Empty<InsertItemPaletteModel>();
			foreach(var item in items)
			{
				var gesture = InsertItemPaletteShortCutGestureConverter.ConvertToGesture(item);
				if (gesture != null)
				{
					MainTextEditor.InputBindings.Add(new InsertPaletteKeyBinding(command, gesture, item));
				}
			}
		}

		public void RequestFocus()
		{
			Dispatcher.Invoke(new Action(() => { MainTextEditor.Focus(); }), System.Windows.Threading.DispatcherPriority.Render);
		}
	}

	//クラス識別用
	internal class InsertPaletteKeyBinding : KeyBinding
	{
		public InsertPaletteKeyBinding(ICommand command, KeyGesture gesture, object commandParam):
			base(command,gesture)
		{
			base.CommandParameter = commandParam;
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

	internal class TextEditorViewModelBase : NotificationObject
	{
		private bool isShowSearchBox;

		public bool IsShowSearchBox
		{
			get => isShowSearchBox;
			set
			{
				isShowSearchBox = value;
				NotifyChanged();
			}
		}

		public ActionCommand ShowSearchBoxCommand { get; }

		public TextEditorViewModelBase()
		{
			//検索
			ShowSearchBoxCommand = new ActionCommand(
				o =>
				{
					IsShowSearchBox = true;
				}
				);
		}
	}

	internal class EventEditorViewModel : TextEditorViewModelBase, IDockingWindowContent, IDisposable, IControlBindedReceiver
	{
		private bool disableBodyPropertyChanged;
		private EventEditor control;
		

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

		public string DockingTitle => Event.Identifier;

		public string DockingContentId => randomizedContentId;

		public TextDocument Document { get; }

		public ActionCommand SendToGhostCommand {get;}
		public ActionCommand InsertCommand { get; }
		public MainViewModel Main { get; }
		public object HilightColors { get; private set; }

		public HighlightingRule searchRule = new HighlightingRule();

		public EventEditorViewModel(MainViewModel main, EventModel ev)
		{
			Main = main;
			Main.PropertyChanged += Main_PropertyChanged;

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

			InsertCommand = new ActionCommand(
				o =>
				{
					control.MainTextEditor.Document.Insert(control.MainTextEditor.CaretOffset, ((InsertItemPaletteModel)o).Body);
				}
				);
		}

		private void Main_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if(e.PropertyName == nameof(MainViewModel.InsertPalette))
			{
				//insertPalette更新
				control.UpdateInsertPaletteKeyBindings(Main.InsertPalette, InsertCommand);
			}
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

		public void MoveCaretToLine(int lineIndex)
		{
			if (lineIndex < control.MainTextEditor.LineCount)
			{
				control.MainTextEditor.ScrollToLine(lineIndex);
				control.MainTextEditor.CaretOffset = Document.Lines[lineIndex].Offset;
			}
		}

		public void Dispose()
		{
			Event.PropertyChanged -= Event_PropertyChanged;
			Document.TextChanged -= Document_TextChanged;
			Main.PropertyChanged -= Main_PropertyChanged;
		}

		public void ControlBind(System.Windows.Controls.Control ctrl)
		{
			if(ctrl is EventEditor eventEditor)
			{
				control = eventEditor;
				control.UpdateInsertPaletteKeyBindings(Main.InsertPalette, InsertCommand);
			}
		}
	}

	

	
}
