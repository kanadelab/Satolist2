using FluentFTP.Servers.Handlers;
using ICSharpCode.AvalonEdit.Editing;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Satolist2.Module.TextEditor
{
	public abstract class TextEditorModuleBase : UserControl
	{
		private bool isEnableSyntaxHighlighting;
		private bool isUndogrouping;
		protected MatchCollection CurrentMatches { get; set; }

		//シンタックスハイライトを使うかどうか
		//使わない場合でも背景色と文字色は適用する
		public virtual bool IsEnableSyntaxHighlighting
		{
			get => isEnableSyntaxHighlighting;
			set
			{
				if(isEnableSyntaxHighlighting != value)
				{
					//変更があればハイライタの設定を更新する
					isEnableSyntaxHighlighting = value;
					UpdateHighlighter();
				}
			}
		}

		internal bool IsEventRegistered { get; set; }
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

		//選択中の内容
		public virtual string SelectionString { get; }

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

		//ゴーストに送信機能を有効化
		public abstract bool IsEnableSendToGhost { get; set; }

		//ゴーストに選択範囲を送信する機能を有効化
		public abstract bool IsEnableSendToGhostSelectionRange { get; set; }

		//ゴーストにShioriEcho送信機能を有効化
		public abstract bool IsEnableSendShioriEchoToGhost { get; set; }
		
		//ゴーストに選択範囲でShioriEcho送信機能を有効化
		public abstract bool IsEnableSendShioriEchoToGhostSelectionRange { get; set; }

		//現在行へスクロール
		public abstract void ScrollToCaret();

		//行情報取得
		public abstract LineData GetLineData(int line);

		//オフセットから行情報取得
		public abstract LineData GetLineDataFromCharIndex(int charIndex);

		//テキスト入力
		public abstract void PerformTextInput(string str);

		//テキスト置換
		public abstract void Replace(string text, int position, int length);

		//範囲選択
		public abstract void SetSelection(int anchor, int caret);

		//フォント設定
		public abstract void SetFont(string fontFamilyName, int fontSize);

		//Undoグループ化
		public abstract void BeginUndoGroup();

		//Undoグループ化終了
		public abstract void EndUndoGroup();

		//カレット位置変更
		public abstract event EventHandler OnCaretPositionChanged;

		//テキスト変更
		public abstract event EventHandler OnTextChanged;

		//ゴーストに送信
		public event EventHandler OnSendToGhost;

		//選択範囲をゴーストに送信
		public event EventHandler OnSendToGhostSelectionRange;

		//ゴーストにShioriEchoを送信
		public event EventHandler OnSendShioriEchoToGhost;

		//選択範囲をゴーストにShioriEchoで送信
		public event EventHandler OnSendShioriEchoToGhostSelectionRange;

		//検索ボックス表示リクエスト
		public event EventHandler OnShowSearchBoxRequested;

		//グローバル検索ボックス表示リクエスト
		public event EventHandler OnShowGlobalSearchBox;

		//編集中の項目リクエスト
		public Func<string> RequestEditingEvent;


		//コンストラクタ
		public TextEditorModuleBase()
		{
			Loaded += TextEditorModuleBase_Loaded;
			Unloaded += TextEditorModuleBase_Unloaded;
		}

		private void TextEditorModuleBase_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			MainWindow.Instance.OnTextEditorSettingsChanged += MainWindow_OnTextEditorSettingsChanged;
			MainWindow.Instance.OnInsertPaletteChanged += MainWindow_OnInsertPaletteChanged;

			//初期データ設定
			UpdateSettings();
			UpdateInsertPalete();
		}

		private void TextEditorModuleBase_Unloaded(object sender, System.Windows.RoutedEventArgs e)
		{
			MainWindow.Instance.OnTextEditorSettingsChanged -= MainWindow_OnTextEditorSettingsChanged;
			MainWindow.Instance.OnInsertPaletteChanged += MainWindow_OnInsertPaletteChanged;
		}

		//テキストエディタ設定の変更時
		private void MainWindow_OnTextEditorSettingsChanged(object sender, EventArgs e)
		{
			UpdateSettings();
		}

		//挿入パレットの設定変更時
		private void MainWindow_OnInsertPaletteChanged(object sender, EventArgs e)
		{
			UpdateInsertPalete();
		}

		//設定の更新
		public void UpdateSettings()
		{
			//設定
			ShowLineNumbers = MainViewModel.EditorSettings.GeneralSettings.IsShowLineNumber;
			WordWrap = MainViewModel.EditorSettings.GeneralSettings.IsWardWrap;
			ShowEndOfLine = MainViewModel.EditorSettings.GeneralSettings.IsShowEndOfLine;
			HighlightCurrentLine = MainViewModel.EditorSettings.GeneralSettings.IsHilightCurrentLine;
			AutoIndent = MainViewModel.EditorSettings.GeneralSettings.IsIndent;

			//ハイライト
			UpdateHighlighter();

			//フォント
			if (!string.IsNullOrEmpty(MainViewModel.EditorSettings.GeneralSettings.TextEditorFontName) && MainViewModel.EditorSettings.GeneralSettings.TextEditorFontSize > 0)
			{
				//フォントサイズが小数点付きだと正しくタブサイズが計算できない
				SetFont(
					MainViewModel.EditorSettings.GeneralSettings.TextEditorFontName,
					(int)MainViewModel.EditorSettings.GeneralSettings.TextEditorFontSize
					);

			}
		}

		//ハイライタの更新
		public abstract void UpdateHighlighter();

		//挿入パレットの更新
		public abstract void UpdateInsertPalete();


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
		internal virtual void RequestCompletion() {}

		//エディタへのフォーカス
		public abstract void RequestFocusToEditor();

		//検索ボックスの表示
		public void ShowSearchBox()
		{
			//TODO: 検索内容の表示
			OnShowSearchBoxRequested?.Invoke(this, new EventArgs());
		}

		//グローバル検索ボックスの表示
		public void ShowGlobalSearchBox()
		{
			OnShowGlobalSearchBox?.Invoke(this, new EventArgs());
		}

		//ゴーストに送信
		public void SendToGhost()
		{
			OnSendToGhost?.Invoke(this, new EventArgs());
		}

		//ゴーストに送信(選択範囲)
		public void SendToGhostSelectionRange()
		{
			OnSendToGhostSelectionRange?.Invoke(this, new EventArgs());
		}

		//ゴーストにShioriEchoを送信
		public void SendShioriEchoToGhost()
		{
			OnSendShioriEchoToGhost?.Invoke(this, new EventArgs());
		}

		//ゴーストにShioriEchoを送信（選択範囲）
		public void SendShioriEchoToGhostSelectionRange()
		{
			OnSendShioriEchoToGhostSelectionRange?.Invoke(this, new EventArgs());
		}

		//コメントアウト
		public virtual void CommentoutSelectionRange()
		{
			try
			{
				using (new UndoGroupScope(this))
				{
					//endとbeginでindexが逆な場合もある
					int beginIndex = SelectionBegin;
					int endIndex = SelectionEnd;
					if (beginIndex > endIndex)
					{
						int swp = beginIndex;
						beginIndex = endIndex;
						endIndex = swp;
					}

					int begin = beginIndex;
					int end = endIndex;

					if (begin == end)
					{
						//一文字
						Replace("＃", begin, 0);
					}
					else
					{
						//複数
						Replace("＃", begin, 0);
						begin = beginIndex;

						int lineIndex = GetLineDataFromCharIndex(begin).LineIndex;
						lineIndex++;
						if (LineCount <= lineIndex)
						{
							//最終行
							return;
						}
						else
						{
							int lineHead;
							while (true)
							{
								end = endIndex;

								if (LineCount <= lineIndex)
									break;

								lineHead = GetLineData(lineIndex).Offset;

								if (lineHead > end)
								{
									break;
								}
								Replace("＃", lineHead, 0);
								lineIndex++;
							}
						}
					}
				}
			}
			catch { }	//インデックス関係で落ちると嫌なので念のため握りつぶす感じ
		}

		//コメントアウト解除
		public virtual void RemoveCommentoutSelectionRange()
		{
			try
			{
				using (new UndoGroupScope(this))
				{
					//コメントアウト解除

					//endとbeginでindexが逆な場合もある
					int beginIndex = SelectionBegin;
					int endIndex = SelectionEnd;
					if (beginIndex > endIndex)
					{
						int swp = beginIndex;
						beginIndex = endIndex;
						endIndex = swp;
					}

					int begin = beginIndex;
					int end = endIndex;

					while (true)
					{
						int pos = Text.IndexOf('＃', begin);
						if (pos == -1)
							return;

						//範囲外
						if (pos > end)
							return;
						int lineIndex = GetLineDataFromCharIndex(pos).LineIndex + 1;
						Replace("", pos, 1);
						if (LineCount <= lineIndex)
						{
							//最終行
							break;
						}
						begin = GetLineData(lineIndex).Offset;
					}
				}
			}
			catch { }
		}

		//カレット位置の項目名を取得
		public string FindEditingEventName()
		{
			return FindEditingEventName(CaretLine - 1);	//0オリジンにするために1引く
		}

		//選択行の＠または＊ではじまる項目名を取得
		public string FindEditingEventName(int lineIndex)
		{
			//開始行の検索(＠または＊を含まない範囲)
			for (int i = lineIndex; i >= 0; i--)
			{
				//ヘッダ行の上がエスケープされてないことが必要
				if (i > 0)
				{
					var nextLineData = GetLineData(i - 1);
					var nextLine = Text.Substring(nextLineData.Offset, nextLineData.Length);
					if (DictionaryUtility.IsLineEndEscaped(nextLine))
						continue;
				}

				var lineData = GetLineData(i);
				var lineString = Text.Substring(lineData.Offset, lineData.Length);
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

			//見つからなかった場合はエディタ側にリクエストする
			//イベントエディタの場合、項目名がテキストに存在してないため
			return RequestEditingEvent?.Invoke();
		}

		//Undoグループ
		protected class UndoGroupScope : IDisposable
		{
			private TextEditorModuleBase module;
			public UndoGroupScope(TextEditorModuleBase module)
			{
				this.module = module;
				if (module.isUndogrouping)
					throw new InvalidOperationException();
				module.isUndogrouping = true;
				module.BeginUndoGroup();
			}

			public void Dispose()
			{
				module.isUndogrouping = false;
				module.EndUndoGroup();
			}
		}

	}

	public class LineData
	{
		public int Offset { get; }
		public int Length { get; }
		public int LineIndex { get; }

		public LineData(int offset, int length, int lineIndex)
		{
			Offset = offset;
			Length = length;
			LineIndex = lineIndex;
		}
	}
}
