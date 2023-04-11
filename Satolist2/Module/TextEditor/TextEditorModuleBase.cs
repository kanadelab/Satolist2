using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Satolist2.Module.TextEditor
{
	public abstract class TextEditorModuleBase : UserControl
	{
		//行数
		public abstract int LineCount { get; }

		//カレット位置
		public abstract int CaretOffset { get; set; }

		//カレットの行数位置
		public abstract int CaretLine { get; }

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

		//フォント設定
		public abstract void SetFont(string fontFamilyName, int fontSize);

		//カレット位置変更
		public abstract event EventHandler CaretPositionChanged;

		//テキスト変更
		public abstract event EventHandler TextChanged;

		//ハイライタの更新
		public abstract void UpdateHighlighter();

		//入力候補の表示
		internal virtual void RequestCompletion(MainViewModel main)
		{
		}
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
