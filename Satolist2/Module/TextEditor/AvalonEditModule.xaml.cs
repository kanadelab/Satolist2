using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using MahApps.Metro.Controls;
using Satolist2.Control;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
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

		public AvalonEditModule()
		{
			InitializeComponent();
			MainTextEditor.Document = new ICSharpCode.AvalonEdit.Document.TextDocument();
			searchHilighter = new SearchHilighter();
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

		public override event EventHandler CaretPositionChanged
		{
			add => MainTextEditor.TextArea.Caret.PositionChanged += value;
			remove => MainTextEditor.TextArea.Caret.PositionChanged -= value;
		}

		public override event EventHandler TextChanged
		{
			add => MainTextEditor.TextChanged += value;
			remove => MainTextEditor.TextChanged -= value;
		}

		public override LineData GetLineData(int line)
		{
			var lineData = MainTextEditor.Document.Lines[line];
			return new LineData(lineData.Offset, lineData.Length);
		}

		public override void PerformTextInput(string str)
		{
			MainTextEditor.TextArea.PerformTextInput(str);
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
			MainTextEditor.FontFamily = new FontFamily(fontFamilyName);
			MainTextEditor.FontSize = fontSize;
		}

		public override void UpdateHighlighter()
		{
			MainTextEditor.Background = new SolidColorBrush(SatoriSyntaxDictionary.GetHilightColor(ScriptSyntax.Background));
			MainTextEditor.SyntaxHighlighting = null;
			MainTextEditor.Dispatcher.BeginInvoke(new Action(() =>
			{
				var hilighter = new SatoriSyntaxHilighter();
				MainTextEditor.SyntaxHighlighting = hilighter;
				MainTextEditor.Foreground = hilighter.MainForegroundColor;
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
		internal override void RequestCompletion(MainViewModel main)
		{
			CompletionManager p = new CompletionManager();
			p.RequestCompletion(MainTextEditor, main);
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
	}

}
