using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
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

namespace Satolist2.Control
{
	/// <summary>
	/// TextEditorSearchBox.xaml の相互作用ロジック
	/// </summary>
	public partial class TextEditorSearchBox : UserControl
	{
		public static readonly DependencyProperty AttachEditorProperty = DependencyProperty.Register(nameof(AttachEditor), typeof(ICSharpCode.AvalonEdit.TextEditor), typeof(TextEditorSearchBox),
			new PropertyMetadata( (d,e) =>
			{
				if(e.OldValue is ICSharpCode.AvalonEdit.TextEditor oldEditor)
				{
					oldEditor.TextChanged -= ((TextEditorSearchBox)d).TextChanged;
				}
				if(e.NewValue is ICSharpCode.AvalonEdit.TextEditor newEditor)
				{
					newEditor.TextChanged += ((TextEditorSearchBox)d).TextChanged;
				}
			}));

		private TextEditorSearchBoxViewModel viewModel;

		private void TextChanged(object sender, EventArgs e)
		{
			viewModel.RefleshSearch();
		}

		public ICSharpCode.AvalonEdit.TextEditor AttachEditor
		{
			get => (ICSharpCode.AvalonEdit.TextEditor)GetValue(AttachEditorProperty);
			set => SetValue(AttachEditorProperty, value);
		}

		public TextEditorSearchBox()
		{
			InitializeComponent();
			viewModel = new TextEditorSearchBoxViewModel(this);
			MainPanel.DataContext = viewModel;
			IsVisibleChanged += TextEditorSearchBox_IsVisibleChanged;
		}

		private void TextEditorSearchBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue)
			{
				Dispatcher.BeginInvoke(new Action(() => {MainTextBox.Focus(); }), DispatcherPriority.Render);
			}
			else
			{
				//閉じられたら、エディタを有効化する
				Dispatcher.BeginInvoke(new Action(() => { AttachEditor.Focus(); }), DispatcherPriority.Render);
			}
		}
	}

	internal class TextEditorSearchBoxViewModel : NotificationObject
	{
		private TextEditorSearchBox control;
		private string searchString;
		private string searchInformation;
		private MatchCollection currentMatches;
		private SearchHilighter searchHilighter_;

		public ActionCommand CloseSearchBoxCommand { get; }
		public ActionCommand SearchNextCommand { get; }
		public ActionCommand SearchPrevCommand { get; }

		public string SearchString
		{
			get => searchString ?? string.Empty;
			set
			{
				if (searchString != value)
				{
					searchString = value;
					searchHilighter_.SearchString = searchString;
					NotifyChanged();
					UpdateSearchString();
				}
			}
		}

		public string SearchInformation
		{
			get => searchInformation ?? string.Empty;
			set
			{
				searchInformation = value;
				NotifyChanged();
			}
		}

		private void SetSearchInformation(int selectionIndex = -1)
		{
			if (currentMatches != null)
			{
				if (currentMatches.Count == 0)
				{
					SearchInformation = "一致なし";
				}
				else
				{
					SearchInformation = string.Format("{0}/{1}", selectionIndex + 1, currentMatches.Count);
				}
			}
			else
			{
				SearchInformation = string.Empty;
			}
		}

		//検索文字列が更新された
		private void UpdateSearchString()
		{
			//検索
			var pattern = new Regex(Regex.Escape(SearchString));
			if (!string.IsNullOrEmpty(pattern.ToString()))
			{
				currentMatches = pattern.Matches(control.AttachEditor.Text);
			}
			else
			{
				currentMatches = null;
			}

			//結果表示
			SetSearchInformation();

			//検索ハイライタを再設定
			control.AttachEditor.TextArea.TextView.LineTransformers.Remove(searchHilighter_);
			control.AttachEditor.TextArea.TextView.LineTransformers.Add(searchHilighter_);
		}

		//検索対象の更新
		public void RefleshSearch()
		{
			var pattern = new Regex(Regex.Escape(SearchString));
			if (!string.IsNullOrEmpty(pattern.ToString()))
			{
				currentMatches = pattern.Matches(control.AttachEditor.Text);
			}
			else
			{
				currentMatches = null;
			}
		}

		public void MoveSearch(bool directionIsNext)
		{
			if ((currentMatches?.Count ?? 0) == 0)
				return;

			//現在のカレットの位置から次の一致を取りに行く
			var currentCaret = control.AttachEditor.CaretOffset;
			if (directionIsNext && !control.AttachEditor.TextArea.Selection.IsEmpty)
			{
				//選択中の内容があればそちらを優先
				currentCaret = control.AttachEditor.Document.GetOffset(control.AttachEditor.TextArea.Selection.EndPosition.Location);
			}

			//次検索処理
			bool found = false;
			if (directionIsNext)
			{
				for (int i = 0; i < currentMatches.Count; i++)
				{
					var item = currentMatches[i];
					if (item.Index >= currentCaret)
					{
						//決定
						control.AttachEditor.TextArea.Selection = Selection.Create(control.AttachEditor.TextArea, item.Index, item.Index + item.Length);
						control.AttachEditor.CaretOffset = item.Index;
						control.AttachEditor.ScrollTo(control.AttachEditor.TextArea.Caret.Line, control.AttachEditor.TextArea.Caret.Column);
						SetSearchInformation(i);
						found = true;
						break;
					}
				}
			}
			else
			{
				for (int i = currentMatches.Count - 1; i >= 0; i--)
				{
					var item = currentMatches[i];
					if (item.Index < currentCaret)
					{
						//決定
						control.AttachEditor.TextArea.Selection = Selection.Create(control.AttachEditor.TextArea, item.Index, item.Index + item.Length);
						control.AttachEditor.CaretOffset = item.Index;
						control.AttachEditor.ScrollTo(control.AttachEditor.TextArea.Caret.Line, control.AttachEditor.TextArea.Caret.Column);
						SetSearchInformation(i);
						found = true;
						break;
					}
				}
			}

			if (!found)
			{
				var targetIndex = directionIsNext ? 0 : currentMatches.Count - 1;
				SetSearchInformation(targetIndex);

				//先頭に戻る
				Match item = currentMatches[targetIndex];
				control.AttachEditor.TextArea.Selection = Selection.Create(control.AttachEditor.TextArea, item.Index, item.Index + item.Length);
				control.AttachEditor.CaretOffset = item.Index;
				control.AttachEditor.ScrollTo(control.AttachEditor.TextArea.Caret.Line, control.AttachEditor.TextArea.Caret.Column);
			}
		}

		public TextEditorSearchBoxViewModel(TextEditorSearchBox control)
		{
			this.control = control;
			searchString = string.Empty;
			searchHilighter_ = new SearchHilighter();

			CloseSearchBoxCommand = new ActionCommand(
				o =>
				{
					control.Visibility = Visibility.Collapsed;
					SearchString = string.Empty;
				}
				);

			SearchNextCommand = new ActionCommand(
				o =>
				{
					MoveSearch(true);	
				}
				);

			SearchPrevCommand = new ActionCommand(
				o =>
				{
					MoveSearch(false);
				}
				);
		}
	}

	//検索色付け用の機能
	internal class SearchHilighter : DocumentColorizingTransformer
	{
		public string SearchString { get; set; }

		protected override void ColorizeLine(DocumentLine line)
		{
			var lineStr = CurrentContext.Document.GetText(line.Offset, line.Length);
			var pattern = new Regex(Regex.Escape(SearchString));
			if (!string.IsNullOrEmpty(pattern.ToString()))
			{
				var matches = pattern.Matches(lineStr);
				foreach(Match m in matches)
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
