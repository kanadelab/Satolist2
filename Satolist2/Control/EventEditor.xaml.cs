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
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;

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

	internal abstract class TextEditorViewModelBase : NotificationObject, IDockingWindowContent
	{
		public MainViewModel Main { get; }
		private bool isShowSearchBox;
		private bool isActiveTextEditor;
		private bool searchBoxFocusTrigger;
		private int caretLine;

		//検索ボックスの表示
		public bool IsShowSearchBox
		{
			get => isShowSearchBox;
			set
			{
				isShowSearchBox = value;
				NotifyChanged();
			}
		}

		//アクティブなテキストエディタか（サーフェスパレットダブルクリックで挿入の対象）
		public virtual bool IsActiveTextEditor
		{
			get => isActiveTextEditor;
			set
			{
				if (IsActiveTextEditor != value)
				{
					isActiveTextEditor = value;
					NotifyChanged();
					NotifyChanged(nameof(DockingTitle));

					if(isActiveTextEditor)
					{
						TrySelectEventOnUkadocViewer();
					}
				}
			}
		}

		public bool SearchBoxFocusTrigger
		{
			get => searchBoxFocusTrigger;
			set
			{
				searchBoxFocusTrigger = value;
				NotifyChanged();
			}
		}

		//編集ウインドウのタイトル
		public abstract string DocumentTitle { get; }

		public ActionCommand ShowSearchBoxCommand { get; }
		public ActionCommand CompletionCommand { get; }

		public abstract ICSharpCode.AvalonEdit.TextEditor MainTextEditor { get; }

		//背景画像パス
		public string BackgroundImagePath
		{
			get
			{
				return MainViewModel.EditorSettings.GeneralSettings.TextEditorBackgroundImagePath;
			}
		}

		//背景画像
		public bool IsEnableBackgroundImage
		{
			get
			{
				return !string.IsNullOrEmpty(MainViewModel.EditorSettings.GeneralSettings.TextEditorBackgroundImagePath);
			}
		}

		//マージン
		public Thickness TextEditorMargin
		{
			get
			{
				return new Thickness(MainViewModel.EditorSettings.GeneralSettings.TextEditorOffsetX, MainViewModel.EditorSettings.GeneralSettings.TextEditorOffsetY, 0.0, 0.0);
			}
		}

		public string DockingTitle
		{
			get
			{
				if (isActiveTextEditor)
					return string.Format("<{0}>", DocumentTitle);
				else
					return DocumentTitle;
			}
		}
		public abstract string DockingContentId {get;}

		public TextEditorViewModelBase(MainViewModel main)
		{
			Main = main;

			//検索
			ShowSearchBoxCommand = new ActionCommand(
				o =>
				{
					IsShowSearchBox = true;

					//すでに開いている場合でもフォーカスを移す必要がある
					SearchBoxFocusTrigger = true;
				}
				);

			//入力補助
			CompletionCommand = new ActionCommand(
				o =>
				{
					CompletionManager p = new CompletionManager();
					p.RequestCompletion(MainTextEditor, Main);
				});
		}

		protected void NotifyDocumentTitleChanged()
		{
			NotifyChanged(nameof(DocumentTitle));
			NotifyChanged(nameof(DockingTitle));
		}

		//フォント設定を最新のものを使うようにする
		public void UpdateFontSettings()
		{
			if(!string.IsNullOrEmpty(MainViewModel.EditorSettings.GeneralSettings.TextEditorFontName) && MainViewModel.EditorSettings.GeneralSettings.TextEditorFontSize > 0 )
			{
				MainTextEditor.TextArea.FontFamily = new FontFamily(MainViewModel.EditorSettings.GeneralSettings.TextEditorFontName);

				//フォントサイズが小数点付きだと正しくタブサイズが計算できない
				MainTextEditor.TextArea.FontSize = (int)MainViewModel.EditorSettings.GeneralSettings.TextEditorFontSize;
			}
		}

		//基本設定を最新のものに更新
		public void UpdateGeneralSettings()
		{
			//設定
			MainTextEditor.ShowLineNumbers = MainViewModel.EditorSettings.GeneralSettings.IsShowLineNumber;
			MainTextEditor.WordWrap = MainViewModel.EditorSettings.GeneralSettings.IsWardWrap;
			MainTextEditor.Options.ShowEndOfLine = MainViewModel.EditorSettings.GeneralSettings.IsShowEndOfLine;

			if (MainViewModel.EditorSettings.GeneralSettings.IsIndent)
				MainTextEditor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.DefaultIndentationStrategy();
			else
				MainTextEditor.TextArea.IndentationStrategy = null;

			//ハイライト
			MainTextEditor.SyntaxHighlighting = null;
			MainTextEditor.Dispatcher.BeginInvoke(new Action(() =>
			{
				var hilighter = new SatoriSyntaxHilighter();
				MainTextEditor.SyntaxHighlighting = hilighter;
				MainTextEditor.Foreground = hilighter.MainForegroundColor;
			}
			), DispatcherPriority.Render);
		}

		//カレットの位置から編集中のイベント名を取得する
		public virtual string FindEditingEventName()
		{
			if (MainTextEditor?.Document == null)
				return null;

			var currentLine = MainTextEditor.TextArea.Caret.Line - 1;   //indexにするので-1

			//開始行の検索(＠または＊を含まない範囲)
			for (int i = currentLine; i >= 0; i--)
			{
				//ヘッダ行の上がエスケープされてないことが必要
				if (i > 0)
				{
					var nextLineData = MainTextEditor.Document.Lines[i - 1];
					var nextLine = MainTextEditor.Text.Substring(nextLineData.Offset, nextLineData.Length);
					if (DictionaryUtility.IsLineEndEscaped(nextLine))
						continue;
				}

				var lineData = MainTextEditor.Document.Lines[i];
				var lineString = MainTextEditor.Text.Substring(lineData.Offset, lineData.Length);
				if (lineString.IndexOf(Constants.SentenceHead) == 0)
				{
					//条件部と＠や＊を取り除く
					return lineString.Substring(1).Split(new char[] { '\t' }).FirstOrDefault();
				}
				else if (lineString.IndexOf(Constants.WordHead) == 0)
				{
					return lineString.Substring(1).Split(new char[] { '\t' }).FirstOrDefault();
				}
			}

			//見つからなかった場合はnull
			return null;
		}

		public void Caret_PositionChanged(object sender, EventArgs e)
		{
			if (caretLine != MainTextEditor.TextArea.Caret.Line)
			{
				TrySelectEventOnUkadocViewer();
				caretLine = MainTextEditor.TextArea.Caret.Line;
			}
		}

		protected void TrySelectEventOnUkadocViewer()
		{
			var eventName = FindEditingEventName();
			if (!string.IsNullOrEmpty(eventName))
				Main.UkadocEventReferenceViewModel.TrySelectEvent(eventName);
		}
	}

	internal class EventEditorViewModel : TextEditorViewModelBase, IDisposable, IControlBindedReceiver
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

		public override string DocumentTitle => Event.Identifier;

		public override string DockingContentId => randomizedContentId;

		public TextDocument Document { get; }

		public ActionCommand SendToGhostCommand {get;}
		public ActionCommand InsertCommand { get; }

		public override ICSharpCode.AvalonEdit.TextEditor MainTextEditor => control.MainTextEditor;
		public object HilightColors { get; private set; }
		public bool EnableHeaderEdit => Event.Type != EventType.Header;

		public EventEditorViewModel(MainViewModel main, EventModel ev) : base(main)
		{
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
					//Satorite.SendSatori(main.Ghost, Document.Text, Event.Type);
					SendToGhost();
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

		//ゴーストにトークを送信
		public void SendToGhost()
		{
			var currentLine = control.MainTextEditor.TextArea.Caret.Line - 1;	//indexにするので-1
			var beginLine = 0;
			var endLine = control.MainTextEditor.LineCount-1;
			EventType type = EventType.Header;

			//開始行の検索(＠または＊を含まない範囲)
			for (int i = currentLine; i >= 0; i--)
			{
				//ヘッダ行の上がエスケープされてないことが必要
				if (i > 0)
				{
					var nextLineData = control.MainTextEditor.Document.Lines[i - 1];
					var nextLine = control.MainTextEditor.Text.Substring(nextLineData.Offset, nextLineData.Length);
					if (DictionaryUtility.IsLineEndEscaped(nextLine))
						continue;
				}

				var lineData = control.MainTextEditor.Document.Lines[i];
				var lineString = control.MainTextEditor.Text.Substring(lineData.Offset, lineData.Length);

				if (lineString.IndexOf(Constants.SentenceHead) == 0)
				{
					//決定。ヘッダの次の行までが対象になる
					beginLine = i + 1;
					type = EventType.Sentence;
					break;
				}
				else if (lineString.IndexOf(Constants.WordHead) == 0)
				{
					beginLine = i + 1;
					type = EventType.Word;
					break;
				}
			}

			//最終的に文か単語群が見つからなければ、編集中本体の物なのでそれを使う
			if (type == EventType.Header)
				type = Event.Type;

			//終了行の検索
			for (int i = currentLine + 1; i < control.MainTextEditor.LineCount; i++)
			{
				//ヘッダ行の上がエスケープされてないことが必要
				if (i > 0)
				{
					var nextLineData = control.MainTextEditor.Document.Lines[i - 1];
					var nextLine = control.MainTextEditor.Text.Substring(nextLineData.Offset, nextLineData.Length);
					if (DictionaryUtility.IsLineEndEscaped(nextLine))
						continue;
				}

				var lineData = control.MainTextEditor.Document.Lines[i];
				var lineString = control.MainTextEditor.Text.Substring(lineData.Offset, lineData.Length);
				if (lineString.IndexOfAny(Constants.SentenceOrWordHead) == 0)
				{
					//決定。ヘッダの前の行までが対象になる
					endLine = i - 1;
					break;
				}
			}

			//開始行と終了行が逆になっているようであれば１行に満たない内容なので出力なし
			if (beginLine > endLine)
				return;

			//出力
			StringBuilder builder = new StringBuilder();
			for (int i = beginLine; i <= endLine; i++)
			{
				var lineData = control.MainTextEditor.Document.Lines[i];
				var lineString = control.MainTextEditor.Text.Substring(lineData.Offset, lineData.Length);
				builder.AppendLine(lineString);
			}
			Satorite.SendSatori(Main.Ghost, builder.ToString(), type);
			Core.LogMessage.AddLog("ゴーストにトークを送信しました。");
		}

		//編集中のイベント名を取得
		public override string FindEditingEventName()
		{
			var baseResult = base.FindEditingEventName();
			if(baseResult == null)
				return Event.Name;	//nullの場合はここで編集中のイベントを示す
			else
				return baseResult;
		}

		public void Dispose()
		{
			Event.PropertyChanged -= Event_PropertyChanged;
			Document.TextChanged -= Document_TextChanged;
			Main.PropertyChanged -= Main_PropertyChanged;
			control.MainTextEditor.TextArea.Caret.PositionChanged -= Caret_PositionChanged;
		}

		public void ControlBind(System.Windows.Controls.Control ctrl)
		{
			if(ctrl is EventEditor eventEditor)
			{
				control = eventEditor;
				control.MainTextEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
				control.UpdateInsertPaletteKeyBindings(Main.InsertPalette, InsertCommand);
				UpdateGeneralSettings();
			}
		}
	}

	

	
}
