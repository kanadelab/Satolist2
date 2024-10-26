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
using ICSharpCode.AvalonEdit;
using Satolist2.Module.TextEditor;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace Satolist2.Control
{
	/// <summary>
	/// EventEditor.xaml の相互作用ロジック
	/// </summary>
	public partial class EventEditor : UserControl
	{
		private System.Windows.Controls.Control lastForcusTextBox;

		public EventEditor()
		{
			InitializeComponent();

			//フォーカスを記憶
			MainTextEditor.GotFocus += (s,e) => {
				lastForcusTextBox = (System.Windows.Controls.Control)s;
			};
			ConditionTextBox.GotFocus += (s, e) => {
				lastForcusTextBox = (System.Windows.Controls.Control)s;
			};
			EventNameTextBox.GotFocus += (s, e) => {
				lastForcusTextBox = (System.Windows.Controls.Control)s;
			};
		}

		public void RequestFocus()
		{
			Dispatcher.Invoke(new Action(() => {

				//最後にフォーカスがあったものを基準
				if (lastForcusTextBox != null )
					lastForcusTextBox.Focus();
				else
					MainTextEditor.MainTextEditor.RequestFocusToEditor();

			}), System.Windows.Threading.DispatcherPriority.Render);
		}

		public void PerformTextInput(string str)
		{
			//最後にフォーカスがあったものを基準
			if (lastForcusTextBox != null)
			{
				if (lastForcusTextBox is TextBox tb)
				{
					if(tb.SelectionLength > 0)
					{
						//選択削除
						var caretIndex = tb.SelectionStart;
						var result = tb.Text.Remove(caretIndex, tb.SelectionLength);
						tb.Text = result.Insert(caretIndex, str);
						tb.CaretIndex = caretIndex + str.Length;
					}
					else
					{
						var caretIndex = tb.CaretIndex;
						tb.Text = tb.Text.Insert(caretIndex, str);
						tb.CaretIndex = caretIndex + str.Length;
					}
					
				}
				else if (lastForcusTextBox is TextEditorModuleSelector tms)
				{
					tms.MainTextEditor.PerformTextInput(str);
				}
			}
			else
			{
				MainTextEditor.MainTextEditor.PerformTextInput(str);
			}
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
		private bool isActiveTextEditor;
		private Color textEditorBackgroundColor;
		private int caretLine;

		//エディタエンジンの指定
		public string TextEditorEngineName => MainViewModel.EditorSettings.GeneralSettings.OverrideTextEditorEngine;

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

		//ボックス背景色
		public Color TextEditorBackgroundColor
		{
			get => textEditorBackgroundColor;
			set
			{
				textEditorBackgroundColor = value;
				NotifyChanged();
			}
		}

		//編集ウインドウのタイトル
		public abstract string DocumentTitle { get; }

		public ActionCommand ShowGlobalSearchCommand { get; }

		public abstract TextEditorModuleBase MainTextEditor { get; }

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

			//全体検索
			ShowGlobalSearchCommand = new ActionCommand(
				o =>
				{
					if(o is TextBox tb)
						Main.ShowSearchBoxCommand.Execute(tb?.SelectedText);
				});
		}

		protected void NotifyDocumentTitleChanged()
		{
			NotifyChanged(nameof(DocumentTitle));
			NotifyChanged(nameof(DockingTitle));
		}


		public void Caret_PositionChanged(object sender, EventArgs e)
		{
			if (caretLine != MainTextEditor.CaretLine)
			{
				TrySelectEventOnUkadocViewer();
				caretLine = MainTextEditor.CaretLine;
			}
		}

		protected void TrySelectEventOnUkadocViewer()
		{
			var eventName = MainTextEditor.FindEditingEventName();
			if (!string.IsNullOrEmpty(eventName))
				Main.UkadocEventReferenceViewModel.TrySelectEvent(eventName);
		}

		//選択中の内容をゴーストに送信
		public void SendToGhostSelectionRange(bool useShioriEcho)
		{
			if (!string.IsNullOrEmpty(MainTextEditor.SelectionString))
			{
				try
				{
					if (useShioriEcho)
					{
						Satorite.SendShioriEcho(Main.Ghost, MainTextEditor.SelectionString);
						Core.LogMessage.AddLog("ゴーストにShioriEchoを送信しました。");
					}
					else
					{
						Satorite.SendSatori(Main.Ghost, MainTextEditor.SelectionString, EventType.Sentence, MainViewModel.EditorSettings.GeneralSettings.UseOwnedSSTP);
						Core.LogMessage.AddLog("ゴーストにトークを送信しました。");
					}
				}
				catch (GhostNotFoundException ex)
				{
					ex.PrintErrorLog();
				}
			}
		}
	}

	internal class EventEditorViewModel : TextEditorViewModelBase, IDisposable, IControlBindedReceiver
	{
		private bool disableBodyPropertyChanged;
		private EventEditor control;
		private readonly ActionCommand sendShioriEchoToGhostCommand;
		private readonly ActionCommand sendShioriEchoToGhostSelectionRangeCommand;

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

		public ActionCommand SendToGhostCommand {get;}
		public ActionCommand SendToGhostSelectionRangeCommand { get; }
		public ActionCommand InsertCommand { get; }

		//設定してある場合に限りコマンドが有効
		public ActionCommand SendShioriEchoToGhostCommand => MainViewModel.EditorSettings.GeneralSettings.IsShowTextEditorShioriEcho ? sendShioriEchoToGhostCommand : null;
		public ActionCommand SendShioriEchoToGhostSelectionRangeCommand => MainViewModel.EditorSettings.GeneralSettings.IsShowTextEditorShioriEcho ? sendShioriEchoToGhostSelectionRangeCommand : null;

		public override TextEditorModuleBase MainTextEditor => control.MainTextEditor.MainTextEditor;
		public object HilightColors { get; private set; }
		public bool EnableHeaderEdit => Event.Type != EventType.Header;

		public EventEditorViewModel(MainViewModel main, EventModel ev) : base(main)
		{
			randomizedContentId = Guid.NewGuid().ToString();	//複数出現するのでユニークなIDを振る
			Event = ev;
			Event.PropertyChanged += Event_PropertyChanged;
			Main.MainWindow.OnTextEditorSettingsChanged += MainWindow_OnTextEditorSettingsChanged;

			//コマンド
			SendToGhostCommand = new ActionCommand(
				o =>
				{
					SendToGhost(false);
				}
				);

			SendToGhostSelectionRangeCommand = new ActionCommand(
				o =>
				{
					SendToGhostSelectionRange(false);
				});

			sendShioriEchoToGhostCommand = new ActionCommand(
				o =>
				{
					SendToGhost(true);
				});

			sendShioriEchoToGhostSelectionRangeCommand = new ActionCommand(
				o =>
				{
					SendToGhostSelectionRange(true);
				});

			InsertCommand = new ActionCommand(
				o =>
				{
					MainTextEditor.PerformTextInput(((InsertItemPaletteModel)o).Body);
				}
				);
		}

		private void MainWindow_OnTextEditorSettingsChanged(object sender, EventArgs e)
		{
			//ShioriEchoを使うかどうかの設定が切り替わった可能性があるので通知
			NotifyChanged(nameof(SendShioriEchoToGhostCommand));
			NotifyChanged(nameof(SendShioriEchoToGhostSelectionRangeCommand));
		}

		private void Document_TextChanged(object sender, EventArgs e)
		{
			//メインのテキストが変更された場合にModelに伝える
			//その際PropertyChangedは呼ばないようにしておく
			disableBodyPropertyChanged = true;
			Event.Body = MainTextEditor.Text;
			disableBodyPropertyChanged = false;
		}
		private void Event_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(EventModel.Name))
				NotifyChanged(nameof(DockingTitle));
			else if (e.PropertyName == nameof(EventModel.Body) && !disableBodyPropertyChanged)
				MainTextEditor.Text = Event.Body;
		}

		public void MoveCaretToLine(int lineIndex)
		{
			if (lineIndex < MainTextEditor.LineCount)
			{
				MainTextEditor.CaretOffset = MainTextEditor.GetLineData(lineIndex).Offset;
				MainTextEditor.ScrollToCaret();
			}
		}

		//ゴーストにトークを送信
		public void SendToGhost(bool useShioriEcho)
		{
			var currentLine = MainTextEditor.CaretLine - 1;	//indexにするので-1
			var beginLine = 0;
			var endLine = MainTextEditor.LineCount-1;
			EventType type = EventType.Header;

			//開始行の検索(＠または＊を含まない範囲)
			for (int i = currentLine; i >= 0; i--)
			{
				//ヘッダ行の上がエスケープされてないことが必要
				if (i > 0)
				{
					var nextLineData = MainTextEditor.GetLineData(i - 1);
					var nextLine = MainTextEditor.Text.Substring(nextLineData.Offset, nextLineData.Length);
					if (DictionaryUtility.IsLineEndEscaped(nextLine))
						continue;
				}

				var lineData = MainTextEditor.GetLineData(i);
				var lineString = MainTextEditor.Text.Substring(lineData.Offset, lineData.Length);

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
			for (int i = currentLine + 1; i < MainTextEditor.LineCount; i++)
			{
				//ヘッダ行の上がエスケープされてないことが必要
				if (i > 0)
				{
					var nextLineData = MainTextEditor.GetLineData(i - 1);
					var nextLine = MainTextEditor.Text.Substring(nextLineData.Offset, nextLineData.Length);
					if (DictionaryUtility.IsLineEndEscaped(nextLine))
						continue;
				}

				var lineData = MainTextEditor.GetLineData(i);
				var lineString = MainTextEditor.Text.Substring(lineData.Offset, lineData.Length);
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
				var lineData = MainTextEditor.GetLineData(i);
				var lineString = MainTextEditor.Text.Substring(lineData.Offset, lineData.Length);
				builder.AppendLine(lineString);
			}

			try
			{
				if(useShioriEcho)
				{
					Satorite.SendShioriEcho(Main.Ghost, builder.ToString());
					Core.LogMessage.AddLog("ゴーストにShioriEchoを送信しました。");
				}
				else
				{
					Satorite.SendSatori(Main.Ghost, builder.ToString(), type, MainViewModel.EditorSettings.GeneralSettings.UseOwnedSSTP);
					Core.LogMessage.AddLog("ゴーストにトークを送信しました。");
				}
			}
			catch(GhostNotFoundException ex)
			{
				ex.PrintErrorLog();
			}
		}

		public void Dispose()
		{
			Event.PropertyChanged -= Event_PropertyChanged;
			MainTextEditor.OnTextChanged -= Document_TextChanged;
			MainTextEditor.OnCaretPositionChanged -= Caret_PositionChanged;
			MainTextEditor.RequestEditingEvent = null;
			Main.MainWindow.OnTextEditorSettingsChanged -= MainWindow_OnTextEditorSettingsChanged;
		}

		public void ControlBind(System.Windows.Controls.Control ctrl)
		{
			if(ctrl is EventEditor eventEditor)
			{
				control = eventEditor;
				MainTextEditor.Text = Event.Body;
				MainTextEditor.OnTextChanged += Document_TextChanged;
				MainTextEditor.OnCaretPositionChanged += Caret_PositionChanged;
				MainTextEditor.RequestEditingEvent = GetEventName;
			}
		}

		private string GetEventName()
		{
			return Event.Name;
		}
	}
}
