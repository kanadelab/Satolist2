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

namespace Satolist2.Module.TextEditor
{
	/// <summary>
	/// AvalonEditModule.xaml の相互作用ロジック
	/// </summary>
	public partial class AvalonEditModule : TextEditorModuleBase
	{
		private SearchHilighter searchHilighter;

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
			DataContext = new AvalonEditModuleViewModel(this);

			//
			IsEnableSendToGhostSelectionRange = false;
			IsEnableSendToGhost = false;	//デフォルトでオフ
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
