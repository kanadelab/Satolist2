using AngleSharp.Dom.Events;
using AngleSharp.Io;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using MahApps.Metro.Controls;
using Satolist2.Control;
using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Windows.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace Satolist2.Module.TextEditor
{
	/// <summary>
	/// AvalonEditModule.xaml の相互作用ロジック
	/// </summary>
	public partial class AvalonEditModule : TextEditorModuleBase
	{
		private SearchHilighter searchHilighter;
		private DispatcherTimer tooltipTimer;
		private string mouseOverWord;

		internal new AvalonEditModuleViewModel DataContext
		{
			get => (AvalonEditModuleViewModel)base.DataContext;
			set => base.DataContext = value;
		}

		public AvalonEditModule()
		{
			InitializeComponent();
			MainTextEditor.Document = new ICSharpCode.AvalonEdit.Document.TextDocument();
			searchHilighter = new SearchHilighter();
			tooltipTimer = new DispatcherTimer();
			tooltipTimer.Interval = TimeSpan.FromMilliseconds(200.0);
			tooltipTimer.Tick += TooltipTimer_Tick;
			DataContext = new AvalonEditModuleViewModel(this);

			//
			IsEnableSendToGhostSelectionRange = false;
			IsEnableSendToGhost = false;    //デフォルトでオフ

			MouseMove += AvalonEditModule_MouseMove;
			MouseLeave += AvalonEditModule_MouseLeave;
		}

		private void AvalonEditModule_MouseLeave(object sender, MouseEventArgs e)
		{
			//リセット
			mouseOverWord = null;
			tooltipTimer.Stop();
			TextEditorToolTip.IsOpen = false;
		}

		private void TooltipTimer_Tick(object sender, EventArgs e)
		{
			//タイマー停止
			tooltipTimer.Stop();

			//閉じている場合のみ
			if (TextEditorToolTip.IsOpen)
				return;

			RequestOpenTooltip(mouseOverWord);
		}

		//選択内容からワードを取得
		private string FindWord(int lineIndex, int colmnIndex)
		{
			var line = MainTextEditor.Document.Lines[lineIndex];

			//カッコを検索して、その間の文字列を取得する
			var offset = line.Offset + colmnIndex;
			var endIndex = MainTextEditor.Document.Text.IndexOf("）", offset);
			var startIndex = MainTextEditor.Document.Text.LastIndexOf("（", offset, offset + 1);

			//見つからない
			if (startIndex < 0 || endIndex < 0)
				return null;

			//内容を取得
			return MainTextEditor.Document.GetText(startIndex + 1, endIndex - (startIndex + 1));
		}

		private void AvalonEditModule_MouseMove(object sender, MouseEventArgs e)
		{
			var point = Mouse.GetPosition(MainTextEditor);
			var item = MainTextEditor.GetPositionFromPoint(point);
			if (item.HasValue)
			{
				//それぞれ1オリジンなので１つ減らす
				var lineIndex = item.Value.Line - 1;
				var colmnIndex = item.Value.Column - 1;
				var word = FindWord(lineIndex, colmnIndex);

				if (!string.IsNullOrEmpty(word))
				{
					//変更されたら一旦タイマーとめる
					if (word != mouseOverWord)
					{
						mouseOverWord = word;
						tooltipTimer.Stop();
						RequestCloseTooltip();
					}
					tooltipTimer.Start();
				}
				else
				{
					mouseOverWord = null;
					tooltipTimer.Stop();
					RequestCloseTooltip();
				}
			}
			else
			{
				mouseOverWord = null;
				tooltipTimer.Stop();
				RequestCloseTooltip();
			}
		}

		private void RequestCloseTooltip()
		{
			if (DataContext is AvalonEditModuleViewModel vm)
			{
				vm.CloseToolTip();
			}
		}

		private void RequestOpenTooltip(string word)
		{
			if (DataContext is AvalonEditModuleViewModel vm)
			{
				vm.RequestTooltip(word);
			}
		}

		public override int LineCount => MainTextEditor.LineCount;

		public override int CaretOffset
		{
			get => MainTextEditor.CaretOffset;
			set => MainTextEditor.CaretOffset = value;
		}

		public override int CaretLine
		{
			get => MainTextEditor.TextArea.Caret.Line;
		}

		//選択範囲
		public override string SelectionString
		{
			get => MainTextEditor.Document.GetText(MainTextEditor.SelectionStart, MainTextEditor.SelectionLength);
		}

		//選択位置
		public override int SelectionBegin
		{
			get => MainTextEditor.TextArea.Selection.IsEmpty ?
				CaretOffset : 
				MainTextEditor.Document.GetOffset(MainTextEditor.TextArea.Selection.StartPosition.Location);
		}

		//選択終了位置
		public override int SelectionEnd
		{
			get => MainTextEditor.TextArea.Selection.IsEmpty ?
				CaretOffset :
				MainTextEditor.Document.GetOffset(MainTextEditor.TextArea.Selection.EndPosition.Location);
		}

		public override string Text
		{
			get => MainTextEditor.Text;
			set => MainTextEditor.Text = value;
		}
		public override bool ShowLineNumbers
		{
			get => MainTextEditor.ShowLineNumbers;
			set => MainTextEditor.ShowLineNumbers = value;
		}

		public override bool WordWrap
		{
			get => MainTextEditor.WordWrap;
			set => MainTextEditor.WordWrap = value;
		}
		public override bool ShowEndOfLine
		{
			get => MainTextEditor.Options.ShowEndOfLine;
			set => MainTextEditor.Options.ShowEndOfLine = value;
		}

		public override bool HighlightCurrentLine
		{
			get => MainTextEditor.Options.HighlightCurrentLine;
			set => MainTextEditor.Options.HighlightCurrentLine = value;
		}

		public override bool AutoIndent
		{
			get => MainTextEditor.TextArea.IndentationStrategy != null;
			set
			{
				if (MainViewModel.EditorSettings.GeneralSettings.IsIndent)
					MainTextEditor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.DefaultIndentationStrategy();
				else
					MainTextEditor.TextArea.IndentationStrategy = null;
			}
		}

		public override bool IsEnableSendToGhost
		{
			get => SendToGhostMenuItem.IsEnabled;
			set
			{
				SendToGhostMenuItem.IsEnabled = value;
				SendToGhostMenuItem.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		public override bool IsEnableSendToGhostSelectionRange
		{
			get => SelectedRangeSendToGhostMenuItem.IsEnabled;
			set
			{
				SelectedRangeSendToGhostMenuItem.IsEnabled = value;
				SelectedRangeSendToGhostMenuItem.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		public override event EventHandler OnCaretPositionChanged
		{
			add => MainTextEditor.TextArea.Caret.PositionChanged += value;
			remove => MainTextEditor.TextArea.Caret.PositionChanged -= value;
		}

		public override event EventHandler OnTextChanged
		{
			add => MainTextEditor.TextChanged += value;
			remove => MainTextEditor.TextChanged -= value;
		}

		public override LineData GetLineData(int line)
		{
			var lineData = MainTextEditor.Document.Lines[line];
			return new LineData(lineData.Offset, lineData.Length, line);
		}

		public override LineData GetLineDataFromCharIndex(int charIndex)
		{
			return GetLineData(MainTextEditor.Document.GetLineByOffset(charIndex).LineNumber-1);
		}

		public override void PerformTextInput(string str)
		{
			MainTextEditor.TextArea.PerformTextInput(str);
		}

		public override void Replace(string text, int position, int length)
		{
			MainTextEditor.TextArea.Document.Replace(position, length, text);
		}

		public override void ScrollToCaret()
		{
			MainTextEditor.ScrollToLine(CaretLine);
		}

		public override void SetSelection(int anchor, int caret)
		{
			MainTextEditor.TextArea.Selection = Selection.Create(MainTextEditor.TextArea, anchor, caret);
			MainTextEditor.CaretOffset = caret;
		}

		public override void SetFont(string fontFamilyName, int fontSize)
		{
			MainTextEditor.TextArea.FontFamily = new FontFamily(fontFamilyName);
			MainTextEditor.TextArea.FontSize = fontSize;
		}

		//Undoグループ化
		public override void BeginUndoGroup()
		{
			MainTextEditor.Document.UndoStack.StartUndoGroup();
		}

		//Undoグループ化終了
		public override void EndUndoGroup()
		{
			MainTextEditor.Document.UndoStack.EndUndoGroup();
		}

		public override void UpdateHighlighter()
		{
			MainTextEditor.SyntaxHighlighting = null;
			MainTextEditor.Dispatcher.BeginInvoke(new Action(() =>
			{
				var highlighter = new SatoriSyntaxHilighter();
				if (IsEnableSyntaxHighlighting)
				{
					MainTextEditor.SyntaxHighlighting = highlighter;
				}
				MainTextEditor.Background = Brushes.Transparent;
				MainTextEditor.Foreground = highlighter.MainForegroundColor;
				DataContext.TextEditorBackgroundColor = highlighter.MainBackgroundColor;
				DataContext.BackgroundImagePath = MainViewModel.EditorSettings.GeneralSettings.TextEditorBackgroundImagePath;
				DataContext.TextEditorMargin = new Thickness(MainViewModel.EditorSettings.GeneralSettings.TextEditorOffsetX, MainViewModel.EditorSettings.GeneralSettings.TextEditorOffsetY, 0.0, 0.0);
			}
			), DispatcherPriority.Render);
		}

		public override int UpdateSearchString(string searchString)
		{
			//検索ハイライタを再設定
			searchHilighter.SearchString = searchString;
			MainTextEditor.TextArea.TextView.LineTransformers.Remove(searchHilighter);
			MainTextEditor.TextArea.TextView.LineTransformers.Add(searchHilighter);

			return base.UpdateSearchString(searchString);
		}

		public override void RequestFocusToEditor()
		{
			MainTextEditor.Focus();
		}

		//入力候補の表示
		internal override void RequestCompletion()
		{
			CompletionManager p = new CompletionManager();
			p.RequestCompletion(MainTextEditor, MainWindow.Instance.DataContext);
		}

		//検索色付け用の機能
		private class SearchHilighter : DocumentColorizingTransformer
		{
			public string SearchString { get; set; }

			protected override void ColorizeLine(DocumentLine line)
			{
				var lineStr = CurrentContext.Document.GetText(line.Offset, line.Length);
				var pattern = new Regex(Regex.Escape(SearchString));
				if (!string.IsNullOrEmpty(pattern.ToString()))
				{
					var matches = pattern.Matches(lineStr);
					foreach (Match m in matches)
					{
						ChangeLinePart(line.Offset + m.Index, line.Offset + m.Index + SearchString.Length,
							(elem) =>
							{
								elem.BackgroundBrush = Brushes.Yellow;
							});
					}
				}
			}
		}

		private void ShowSearchBox_Click(object sender, RoutedEventArgs e)
		{
			ShowSearchBox();
		}

		//InputBindingの更新
		public override void UpdateInsertPalete()
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
			var items = MainWindow.Instance.DataContext.InsertPalette?.AllItems() ?? Array.Empty<InsertItemPaletteModel>();
			foreach (var item in items)
			{
				var gesture = InsertItemPaletteShortCutGestureConverter.ConvertToGesture(item);
				if (gesture != null)
				{
					MainTextEditor.InputBindings.Add(new InsertPaletteKeyBinding(DataContext.InsertCommand, gesture, item));
				}
			}
		}
	}

	/// <summary>
	/// 内部ViewModel
	/// </summary>
	internal class AvalonEditModuleViewModel : NotificationObject
	{
		private AvalonEditModule Module { get; }
		private Brush backgroundColor;
		private string backgroundImagePath;
		private Thickness textEditorMargin;
		private bool isToolTipOpen;
		private SurfacePaletteItemViewModel toolTipSurface;
		private string toolTipDictionaryEvent;

		public ICommand SendToGhostSelectionRangeCommand { get; }
		public ICommand SendToGhostCommand { get; }
		public ICommand ShowSearchBoxCommand { get; }
		public ICommand ShowGlobalSearchCommand { get; }
		public ICommand CompletionCommand { get; }
		public ICommand InsertCommand { get; }
		public MainViewModel Main => MainWindow.Instance.DataContext;

		//背景画像パス
		public string BackgroundImagePath
		{
			get => backgroundImagePath;
			set
			{
				backgroundImagePath = value;
				NotifyChanged();
				NotifyChanged(nameof(IsEnableBackgroundImage));
			}
		}

		//背景画像
		public bool IsEnableBackgroundImage
		{
			get
			{
				return !string.IsNullOrEmpty(backgroundImagePath);
			}
		}

		//マージン
		public Thickness TextEditorMargin
		{
			get
			{
				return textEditorMargin;
			}

			set
			{
				textEditorMargin = value;
				NotifyChanged();
			}
		}

		//背景色
		public Brush TextEditorBackgroundColor
		{
			get => backgroundColor;
			set
			{
				backgroundColor = value;
				NotifyChanged();
			}
		}

		//ツールチップ開いているか
		public bool IsToolTipOpen
		{
			get => isToolTipOpen;
			set
			{
				isToolTipOpen = value;
				NotifyChanged();
			}
		}

		//ツールチップに表示するサーフェスパレット画像
		public SurfacePaletteItemViewModel ToolTipSurface
		{
			get => toolTipSurface;
			set
			{
				toolTipSurface = value;
				NotifyChanged();
			}
		}

		//ツールチップに表示する辞書情報
		public string DictionaryEvent
		{
			get => toolTipDictionaryEvent;
			set
			{
				toolTipDictionaryEvent = value;
				NotifyChanged();
			}
		}

		//ツールチップを開く
		public void CloseToolTip()
		{
			IsToolTipOpen = false;
		}

		//wordに対応する内容をゴーストから検索してツールチップを表示
		public void RequestTooltip(string word)
		{
			//リセット
			DictionaryEvent = null;
			ToolTipSurface = null;

			if (!MainViewModel.EditorSettings.GeneralSettings.IsShowTextEditorToolTip)
				return;
			
			//サーフェスパレットのプレビュー表示
			var surfaceId = DictionaryUtility.NumberZen2Han(word);
			if (int.TryParse(surfaceId, out int _))
			{
				var image = Main.SurfacePaletteViewModel.Items.FirstOrDefault(o => o.Id.ToString() == surfaceId);
				if (image != null)
				{
					IsToolTipOpen = true;
					ToolTipSurface = image;
					return;
				}
			}
		}

		public AvalonEditModuleViewModel(AvalonEditModule module)
		{
			Module = module;
			textEditorMargin = new Thickness();

			SendToGhostCommand = new ActionCommand(o =>
			{
				module.SendToGhost();
			});

			SendToGhostSelectionRangeCommand = new ActionCommand(o =>
			{
				module.SendToGhostSelectionRange();
			});

			ShowSearchBoxCommand = new ActionCommand(o =>
			{
				module.ShowSearchBox();
			});

			ShowGlobalSearchCommand = new ActionCommand(o =>
			{
				module.ShowGlobalSearchBox();
			});

			CompletionCommand = new ActionCommand( o =>
			{
				module.RequestCompletion();
			});

			InsertCommand = new ActionCommand(o =>
			{
				if (o is InsertItemPaletteModel item)
				{
					if (!string.IsNullOrEmpty(item.Body))
					{
						module.PerformTextInput(item.Body);
					}
				}
			});
		}
	}

}
