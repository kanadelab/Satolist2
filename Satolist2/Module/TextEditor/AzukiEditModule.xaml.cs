using Satolist2.Control;
using Satolist2.Utility;
using Sgry.Azuki;
using Sgry.Azuki.Highlighter;
using Sgry.Azuki.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Color = System.Drawing.Color;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;

namespace Satolist2.Module.TextEditor
{
	/// <summary>
	/// AzukiEditModule.xaml の相互作用ロジック
	/// </summary>
	public partial class AzukiEditModule : TextEditorModuleBase
	{
		private AzukiControl MainTextEditor;

		public AzukiEditModule()
		{
			InitializeComponent();
			MainTextEditor = new AzukiControl();

			//コンテキストメニューセットアップ
			var contextmenu = new ContextMenu();
			contextmenu.MenuItems.Add(new MenuItem("コピー", RequestCopy, Shortcut.CtrlC));
			contextmenu.MenuItems.Add(new MenuItem("切り取り", RequestCut, Shortcut.CtrlX));
			contextmenu.MenuItems.Add(new MenuItem("貼り付け", RequestPaste, Shortcut.CtrlV));
			//contextmenu.MenuItems.Add(new MenuItem("ゴーストに送信", null, (Shortcut)(Keys.Alt | Keys.Q)));
			MainTextEditor.ContextMenu = contextmenu;
			MainTextEditor.ShowsDirtBar = false;
			MainTextEditor.ShowsHRuler = true;

			FormsHost.Child = MainTextEditor;
		}

		private void RequestCopy(object sender, EventArgs args)
		{
			MainTextEditor.Copy();
		}

		private void RequestCut(object sender, EventArgs args)
		{
			MainTextEditor.Cut();
		}

		private void RequestPaste(object sender, EventArgs args)
		{
			MainTextEditor.Paste();
		}

		/*
		private void RequestSendToGhost(object sender, EventArgs args)
		{
		}
		*/

		public override int LineCount => MainTextEditor.LineCount;

		public override int CaretOffset
		{
			get => MainTextEditor.CaretIndex;
			set
			{
				MainTextEditor.SetSelection(value, value);
			}
		}

		public override int CaretLine
		{
			get => MainTextEditor.GetLineIndexFromCharIndex(CaretOffset);
		}

		public override string Text
		{
			get => MainTextEditor.Text;
			set
			{
				MainTextEditor.Text = value;
				MainTextEditor.ClearHistory();	//通常設定がクリアされないように
			}
		}

		public override bool ShowLineNumbers
		{
			get => MainTextEditor.ShowsLineNumber;
			set => MainTextEditor.ShowsLineNumber = value;
		}

		//TODO: 
		public override bool WordWrap
		{
			get { return false; }
			set { }
		}

		public override bool ShowEndOfLine
		{
			get { return false; }
			set { }
		}
		public override bool HighlightCurrentLine
		{
			get { return false; }
			set { }
		}

		public override bool AutoIndent
		{
			get { return false; }
			set { }
		}

		public override event EventHandler CaretPositionChanged
		{
			add => MainTextEditor.CaretMoved += value;
			remove => MainTextEditor.CaretMoved -= value;
		}

		public override event EventHandler TextChanged
		{
			add => MainTextEditor.TextChanged += value;
			remove => MainTextEditor.TextChanged -= value;
		}

		public override LineData GetLineData(int line)
		{
			return new LineData(
				MainTextEditor.GetLineHeadIndex(line),
				MainTextEditor.GetLineLength(line)
				);
		}

		public override void PerformTextInput(string str)
		{
			MainTextEditor.HandleTextInput(str);
		}

		public override void ScrollToCaret()
		{
			MainTextEditor.ScrollToCaret();
		}

		public override void SetFont(string fontFamilyName, int fontSize)
		{
			MainTextEditor.Font = new System.Drawing.Font(fontFamilyName, fontSize);
			//MainTextEditor.ColorScheme
		}

		public override void UpdateHighlighter()
		{
			var kh = new KeywordHighlighter();
			MainTextEditor.Highlighter = kh;
			MainTextEditor.BackColor = SatoriSyntaxDictionary.GetHilightDrawingColor(ScriptSyntax.Background);
			MainTextEditor.ForeColor = SatoriSyntaxDictionary.GetHilightDrawingColor(ScriptSyntax.Default);
			var colors = MainTextEditor.ColorScheme;
			foreach (var def in SatoriSyntaxDictionary.Definitions)
			{
				var klass = (CharClass)(def.syntaxType + (int)CharClass.IndexLine + 1);
				kh.AddRegex(def.pattern, klass);
				colors.SetColor(klass, SatoriSyntaxDictionary.GetHilightDrawingColor(def.syntaxType), Color.Transparent);
			}
		}

	
	}
}
