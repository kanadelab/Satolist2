using FluentFTP.Servers.Handlers;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Satolist2.Module.TextEditor
{
	public abstract class TextEditorModuleBase : UserControl
	{
		protected MatchCollection CurrentMatches { get; set; }

		public virtual int CurrentMatchCount => CurrentMatches?.Count ?? 0;

		//行数
		public abstract int LineCount { get; }

		//カレット位置
		public abstract int CaretOffset { get; set; }

		//カレットの行数位置
		public abstract int CaretLine { get; }

		//選択位置
		public abstract int SelectionBegin { get; }

		//選択終了位置
		public abstract int SelectionEnd { get; }

		//テキストボディ
		public abstract string Text { get; set; }

		//行数の表示
		public abstract bool ShowLineNumbers { get; set; }

		//自動改行
		public abstract bool WordWrap { get; set; }

		//改行の表示
		public abstract bool ShowEndOfLine { get; set; }

		//現在行の表示
		public abstract bool HighlightCurrentLine { get; set; }

		//オートインデント
		public abstract bool AutoIndent { get; set; }

		//現在行へスクロール
		public abstract void ScrollToCaret();

		//行情報取得
		public abstract LineData GetLineData(int line);

		//テキスト入力
		public abstract void PerformTextInput(string str);

		//範囲選択
		public abstract void SetSelection(int anchor, int caret);

		//フォント設定
		public abstract void SetFont(string fontFamilyName, int fontSize);

		//カレット位置変更
		public abstract event EventHandler CaretPositionChanged;

		//テキスト変更
		public abstract event EventHandler TextChanged;

		public TextEditorModuleBase()
		{
			Debug.WriteLine("Test");
		}

		//ハイライタの更新
		public abstract void UpdateHighlighter();

		//検索要求
		public virtual int UpdateSearchString(string searchString)
		{
			//検索
			var pattern = new Regex(Regex.Escape(searchString));
			if (!string.IsNullOrEmpty(pattern.ToString()))
			{
				CurrentMatches = pattern.Matches(Text);
			}
			else
			{
				CurrentMatches = null;
			}

			return CurrentMatches?.Count ?? 0;
		}

		//次検索要求
		public virtual int MoveSearch(bool isSearchDirectionNext)
		{
			int currentIndex = -1;
			if ((CurrentMatches?.Count ?? 0) == 0)
				return currentIndex;

			//現在のカレットの位置から次の一致を取りに行く
			var currentCaret = CaretOffset;
			if (SelectionBegin != SelectionEnd)
			{
				//選択中の内容があればそちらを優先
				if (isSearchDirectionNext)
				{
					currentCaret = SelectionEnd;
				}
				else
				{
					currentCaret = SelectionBegin;
				}
			}

			//次検索処理
			bool found = false;
			if (isSearchDirectionNext)
			{
				for (int i = 0; i < CurrentMatches.Count; i++)
				{
					var item = CurrentMatches[i];
					if (item.Index >= currentCaret)
					{
						//決定
						SetSelection(item.Index, item.Index + item.Length);
						ScrollToCaret();
						currentIndex = i;
						found = true;
						break;
					}
				}
			}
			else
			{
				for (int i = CurrentMatches.Count - 1; i >= 0; i--)
				{
					var item = CurrentMatches[i];
					if (item.Index < currentCaret)
					{
						//決定
						SetSelection(item.Index, item.Index + item.Length);
						ScrollToCaret();
						currentIndex = i;
						found = true;
						break;
					}
				}
			}

			if (!found)
			{
				var targetIndex = isSearchDirectionNext ? 0 : CurrentMatches.Count - 1;
				currentIndex = targetIndex;

				//先頭に戻る
				Match item = CurrentMatches[targetIndex];
				SetSelection(item.Index, item.Index + item.Length);
				ScrollToCaret();
			}

			return currentIndex;
		}

		//入力候補の表示
		internal virtual void RequestCompletion(MainViewModel main)
		{
		}

		//エディタへのフォーカス
		public abstract void RequestFocusToEditor();
	}

	public class LineData
	{
		public int Offset { get; }
		public int Length { get; }

		public LineData(int offset, int length)
		{
			Offset = offset;
			Length = length;
		}
	}
}
