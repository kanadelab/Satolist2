using Satolist2.Control;
using Satolist2.Model;
using Satolist2.Utility;
using Sgry.Azuki;
using Sgry.Azuki.Highlighter;
using Sgry.Azuki.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
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
using Menu = System.Windows.Forms.Menu;
using MenuItem = System.Windows.Forms.MenuItem;

namespace Satolist2.Module.TextEditor
{
	/// <summary>
	/// AzukiEditModule.xaml の相互作用ロジック
	/// </summary>
	public partial class AzukiEditModule : TextEditorModuleBase
	{
		public static readonly DependencyProperty SendToGhostCommandProperty = DependencyProperty.Register(nameof(SendToGhostCommand), typeof(ICommand), typeof(AzukiEditModule));
		public static readonly DependencyProperty InsertCommandProperty = DependencyProperty.Register(nameof(InsertCommand), typeof(ICommand), typeof(AzukiEditModule));
		public static readonly DependencyProperty ShowSearchBoxCommandProperty = DependencyProperty.Register(nameof(ShowSearchBoxCommand), typeof(ICommand), typeof(AzukiEditModule));
		public static readonly DependencyProperty InsertPaletteItemsProperty = DependencyProperty.Register(nameof(InsertPaletteItems), typeof(IEnumerable<InsertItemPaletteModel>), typeof(AzukiEditModule),
			new PropertyMetadata(
				(d, e) =>
				{
					if(d is AzukiEditModule control)
					{
						control.UpdateInsertPalette((IEnumerable<InsertItemPaletteModel>)e.NewValue);
					}
				}));

		private AzukiControl MainTextEditor { get; set; }
		private MenuItem insertPaletteMenuItem;

		public ICommand SendToGhostCommand
		{
			get => (ICommand)GetValue(SendToGhostCommandProperty);
			set => SetValue(SendToGhostCommandProperty, value);
		}

		public ICommand InsertCommand
		{
			get => (ICommand)GetValue(InsertCommandProperty);
			set => SetValue(InsertCommandProperty, value);
		}

		public ICommand ShowSearchBoxCommand
		{
			get => (ICommand)GetValue(ShowSearchBoxCommandProperty);
			set => SetValue(ShowSearchBoxCommandProperty, value);
		}

		internal IEnumerable<InsertItemPaletteModel> InsertPaletteItems
		{
			get => (IEnumerable<InsertItemPaletteModel>)GetValue(InsertPaletteItemsProperty);
			set => SetValue(InsertPaletteItemsProperty, value);
		}

		public AzukiEditModule()
		{
			InitializeComponent();
			MainTextEditor = new AzukiControl();
			MainTextEditor.SizeChanged += MainTextEditor_SizeChanged;

			//コンテキストメニューセットアップ
			var contextmenu = new ContextMenu();
			contextmenu.MenuItems.Add(new MenuItem("コピー", RequestCopy, Shortcut.CtrlC));
			contextmenu.MenuItems.Add(new MenuItem("切り取り", RequestCut, Shortcut.CtrlX));
			contextmenu.MenuItems.Add(new MenuItem("貼り付け", RequestPaste, Shortcut.CtrlV));
			contextmenu.MenuItems.Add(new MenuItem("ゴーストに送信", RequestSendToGhost, (Shortcut)(Keys.Alt | Keys.Q)));
			contextmenu.MenuItems.Add(new MenuItem("検索", RequestShowSearchBox, Shortcut.CtrlF));
			insertPaletteMenuItem = new MenuItem("挿入");
			insertPaletteMenuItem.Enabled = false;	//初期はアイテムが無いので無効
			contextmenu.MenuItems.Add(insertPaletteMenuItem);

			MainTextEditor.ContextMenu = contextmenu;
			MainTextEditor.ShowsDirtBar = false;
			MainTextEditor.ShowsHRuler = true;

			/*
			var panel = new System.Windows.Forms.Panel();
			panel.Controls.Add(MainTextEditor);
			FormsHost.Child = panel;
			*/
			FormsHost.Child = MainTextEditor;
		}

		private void MainTextEditor_SizeChanged(object sender, EventArgs e)
		{
			//改行位置基準を設定する必要あり
			//値はさとりすとv1より
			MainTextEditor.ViewWidth = MainTextEditor.Width - 30;
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

		private void RequestSendToGhost(object sender, EventArgs args)
		{
			SendToGhostCommand?.Execute(null);
		}

		private void RequestShowSearchBox(object sender, EventArgs args)
		{
			ShowSearchBoxCommand?.Execute(null);
		}

		private void RequestInsert(object sender, EventArgs args)
		{
			if(sender is MenuItem item)
			{
				InsertCommand?.Execute(item.Tag);
			}
		}

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
			get
			{
				//折り返し有効時、MainTextEditor.GetLineIndexFromCharIndex()が折り返しベースで計算する問題がある
				//仕方ないので数える
				int sum = 0;
				for(var i = 0; i<LineCount;i++)
				{
					sum += MainTextEditor.GetLineLength(i) + Environment.NewLine.Length;
					if (CaretOffset < sum)
						return i;
				}
				return LineCount - 1;
			}
		}

		public override int SelectionBegin
		{
			get
			{
				int begin, end;
				MainTextEditor.GetSelection(out begin, out end);
				return begin;
			}
		}

		public override int SelectionEnd
		{
			get
			{
				int begin, end;
				MainTextEditor.GetSelection(out begin, out end);
				return end;
			}
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

		public override bool WordWrap
		{
			get
			{
				return MainTextEditor.ViewType == ViewType.WrappedProportional;
			}
			set
			{
				MainTextEditor.ViewType = value ? ViewType.WrappedProportional : ViewType.Proportional;
			}
		}

		public override bool ShowEndOfLine
		{
			get => MainTextEditor.DrawsEolCode;
			set => MainTextEditor.DrawsEolCode = value;
		}
		public override bool HighlightCurrentLine
		{
			get => MainTextEditor.HighlightsCurrentLine;
			set => MainTextEditor.HighlightsCurrentLine = value;
		}

		public override bool AutoIndent
		{
			get => MainTextEditor.AutoIndentHook != null;
			set
			{
				if(value)
				{
					MainTextEditor.AutoIndentHook = (ui, ch) =>
					{
						if (ch == '\r')
						{
							//インデント
							String indent = "";
							int line, cm;
							ui.Document.GetCaretIndex(out line, out cm);
							String data = ui.Document.GetLineContent(line);
							foreach (char item in data)
							{
								if (item == '\t' || item == ' ' || item == '　')
								{
									indent += item;
								}
								else
								{
									break;
								}

							}

							ui.Document.Replace("\r\n" + indent);
							return true;
						}
						return false;
					};
				}
				else
				{
					MainTextEditor.AutoIndentHook = null;
				}
			}
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
			//折り返し有効時、GetLineHeadIndex()が折り返しベースで計算する問題がある
			//仕方ないので効率は悪いが数えてみる
			int sum = 0;
			for(int i = 0; i<line;i++)
			{
				sum += MainTextEditor.GetLineLength(i) + Environment.NewLine.Length;
			}

			return new LineData(
				sum,
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

		public override void SetSelection(int anchor, int caret)
		{
			MainTextEditor.SetSelection(anchor, caret);
		}

		public override void SetFont(string fontFamilyName, int fontSize)
		{
			MainTextEditor.Font = new System.Drawing.Font(fontFamilyName, fontSize);
		}

		public override void RequestFocusToEditor()
		{
			MainTextEditor.Focus();
		}

		public override void UpdateHighlighter()
		{
			var kh = new KeywordHighlighter();
			kh.HighlightsNumericLiterals = false;
			var colors = MainTextEditor.ColorScheme;
			foreach (var def in SatoriSyntaxDictionary.Definitions)
			{
				var klass = (CharClass)(def.syntaxType + (int)CharClass.IndexLine + 1);
				kh.AddRegex(def.pattern, klass);
				switch(def.syntaxType)
				{
					case ScriptSyntax.Tab:
					case ScriptSyntax.Space:
					case ScriptSyntax.WideSpace:
						colors.SetColor(klass, Color.Transparent, SatoriSyntaxDictionary.GetHilightDrawingColor(def.syntaxType));
						break;
					default:
						colors.SetColor(klass, SatoriSyntaxDictionary.GetHilightDrawingColor(def.syntaxType), Color.Transparent);
						break;
				}
			}
			MainTextEditor.Highlighter = kh;
			MainTextEditor.BackColor = SatoriSyntaxDictionary.GetHilightDrawingColor(ScriptSyntax.Background);
			MainTextEditor.ForeColor = SatoriSyntaxDictionary.GetHilightDrawingColor(ScriptSyntax.Default);
		}

		internal void UpdateInsertPalette(IEnumerable<InsertItemPaletteModel> items)
		{
			//挿入パレットの構築
			insertPaletteMenuItem.MenuItems.Clear();
			if (items != null)
			{
				CreateInsertPalette(items, insertPaletteMenuItem);
			}

			//アイテムがなければ無効
			if(insertPaletteMenuItem.MenuItems.Count>0)
			{
				insertPaletteMenuItem.Enabled = true;
			}
			else
			{
				insertPaletteMenuItem.Enabled = false;
			}
		}

		internal void CreateInsertPalette(IEnumerable<Model.InsertItemPaletteModel> items, Menu parent)
		{
			foreach(var item in items)
			{
				if(item.Type == Model.InsertItemPaletteModel.NodeType.Group)
				{
					var menuItem = new MenuItem(item.Label);
					CreateInsertPalette(item.Items, menuItem);
					parent.MenuItems.Add(menuItem);
				}
				else
				{
					var menuItem = new MenuItem(item.Label, RequestInsert, MakeShortcut(item));
					menuItem.Tag = item;
					parent.MenuItems.Add(menuItem);
				}
			}
		}

		//Forms用のショートカットキー組み合わせを作成
		private Shortcut MakeShortcut(Model.InsertItemPaletteModel item)
		{
			if(item.ShortCutKeyAlt || item.ShortCutKeyShift || item.ShortCutKeyCtrl)
			{
				Shortcut result = Shortcut.None;
				if (item.ShortCutKeyAlt)
					result |= (Shortcut)Keys.Alt;
				if (item.ShortCutKeyShift)
					result |= (Shortcut)Keys.Shift;
				if (item.ShortCutKeyCtrl)
					result |= (Shortcut)Keys.Control;

				var keys = new Keys[]
				{
					Keys.D0,
					Keys.D1,
					Keys.D2,
					Keys.D3,
					Keys.D4,
					Keys.D5,
					Keys.D6,
					Keys.D7,
					Keys.D8,
					Keys.D9
				};
				result |= (Shortcut)keys[item.ShortCutKeyNumber];
				return result;
			}
			else
			{
				return Shortcut.None;
			}
		}

	
	}
}
