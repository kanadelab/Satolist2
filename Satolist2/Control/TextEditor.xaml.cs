using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Satolist2.Model;
using Satolist2.Module.TextEditor;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Satolist2.Control
{
	/// <summary>
	/// TextEditor.xaml の相互作用ロジック
	/// </summary>
	public partial class TextEditor : UserControl
	{
		public TextEditor()
		{
			InitializeComponent();
		}

		internal void UpdateInsertPaletteKeyBindings(InsertItemPaletteModel palette, ICommand command)
		{
			//古いバインディング削除
			List<InputBinding> removes = new List<InputBinding>();
			foreach (var item in MainTextEditor.InputBindings)
			{
				if (item is InsertPaletteKeyBinding b)
					removes.Add(b);
			}

			foreach (var item in removes)
			{
				MainTextEditor.InputBindings.Remove(item);
			}

			//バインディングの作成
			var items = palette?.AllItems() ?? Array.Empty<InsertItemPaletteModel>();
			foreach (var item in items)
			{
				var gesture = InsertItemPaletteShortCutGestureConverter.ConvertToGesture(item);
				if (gesture != null)
				{
					MainTextEditor.InputBindings.Add(new InsertPaletteKeyBinding(command, gesture, item));
				}
			}
		}

		//フォーカスリクエスト
		public void RequestFocus()
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				MainTextEditor.MainTextEditor.RequestFocusToEditor();
			}), System.Windows.Threading.DispatcherPriority.Render);
		}

		//文字列挿入リクエスト
		public void PerformTextInput(string str)
		{
			MainTextEditor.MainTextEditor.PerformTextInput(str);
		}
	}

	internal class TextEditorViewModel : TextEditorViewModelBase, IDisposable, IControlBindedReceiver
	{
		private TextEditor control;
		private string randomizedContetnId;
		
		public override string DocumentTitle => TextFile.RelativeName;
		public override string DockingContentId => randomizedContetnId;
		
		public DictionaryModel TextFile { get; }
		public ActionCommand SendToGhostCommand { get; }
		public ActionCommand InsertCommand { get; }

		public override TextEditorModuleBase MainTextEditor => control.MainTextEditor.MainTextEditor;

		public TextEditorViewModel(MainViewModel main, DictionaryModel textFile) : base(main)
		{
			randomizedContetnId = Guid.NewGuid().ToString();    //複数出現するのでユニークなIDを振る
			TextFile = textFile;

			Main.PropertyChanged += Main_PropertyChanged;

			//コマンド
			SendToGhostCommand = new ActionCommand(
				o =>
				{
					//イベントのスコープを検出して送信する必要がある
					SendToGhost();
				}
				);

			InsertCommand = new ActionCommand(
				o =>
				{
					MainTextEditor.PerformTextInput(((InsertItemPaletteModel)o).Body);
				}
				);
		}

		private void Main_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if(e.PropertyName == nameof(MainViewModel.InsertPalette))
			{
				control.UpdateInsertPaletteKeyBindings(Main.InsertPalette, InsertCommand);
			}
		}

		private void Document_TextChanged(object sender, EventArgs e)
		{
			TextFile.Body = MainTextEditor.Text;
		}

		public void MoveCaretToLine(int lineIndex)
		{
			if (lineIndex < MainTextEditor.LineCount)
			{
				MainTextEditor.CaretOffset = MainTextEditor.GetLineData(lineIndex).Offset;
				MainTextEditor.ScrollToCaret();
			}
		}

		//カレットの位置からトークを特定して送信する
		public void SendToGhost()
		{
			var currentLine = MainTextEditor.CaretLine - 1;	//indexにするので-1
			var beginLine = 0;
			var endLine = MainTextEditor.LineCount - 1;
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
				else if(lineString.IndexOf(Constants.WordHead) == 0)
				{
					beginLine = i + 1;
					type = EventType.Word;
					break;
				}
			}

			//最終的に文か単語群が見つからなければそれはヘッダなのでキャンセル
			if (type == EventType.Header)
				return;

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
			for(int i = beginLine; i <= endLine; i++)
			{
				var lineData = MainTextEditor.GetLineData(i);
				var lineString = MainTextEditor.Text.Substring(lineData.Offset, lineData.Length);
				builder.AppendLine(lineString);
			}

			try
			{
				Satorite.SendSatori(Main.Ghost, builder.ToString(), type);
				Core.LogMessage.AddLog("ゴーストにトークを送信しました。");
			}
			catch(GhostNotFoundException ex)
			{
				ex.PrintErrorLog();
			}
		}

		public void Dispose()
		{
			MainTextEditor.OnTextChanged -= Document_TextChanged;
			MainTextEditor.OnCaretPositionChanged -= Caret_PositionChanged;
		}

		public void ControlBind(System.Windows.Controls.Control ctrl)
		{
			if (ctrl is TextEditor textEditor)
			{
				control = textEditor;
				MainTextEditor.Text = TextFile.Body;
				MainTextEditor.OnTextChanged += Document_TextChanged;
				MainTextEditor.OnCaretPositionChanged += Caret_PositionChanged;
				control.UpdateInsertPaletteKeyBindings(Main.InsertPalette, InsertCommand);
			}
		}	
	}
}
