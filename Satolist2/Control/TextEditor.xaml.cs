using ICSharpCode.AvalonEdit.Document;
using Satolist2.Model;
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

		public void RequestFocus()
		{
			Dispatcher.BeginInvoke(new Action(() => { MainTextEditor.Focus(); }), System.Windows.Threading.DispatcherPriority.Render);
		}
	}

	internal class TextEditorViewModel : NotificationObject, IDockingWindowContent, IDisposable, IControlBindedReceiver
	{
		private TextEditor control;
		private bool disableBodyPropertyChanged;
		private string randomizedContetnId;
		
		public string DockingTitle => TextFile.RelativeName;

		public string DockingContentId => randomizedContetnId;

		
		public TextFileModel TextFile { get; }
		public TextDocument Document { get; }
		public MainViewModel Main { get; }
		public ActionCommand SendToGhostCommand { get; }
		public ActionCommand InsertCommand { get; }


		public TextEditorViewModel(MainViewModel main, TextFileModel textFile)
		{
			randomizedContetnId = Guid.NewGuid().ToString();    //複数出現するのでユニークなIDを振る
			TextFile = textFile;

			Main = main;
			Main.PropertyChanged += Main_PropertyChanged;

			Document = new TextDocument(TextFile.Body);
			Document.TextChanged += Document_TextChanged;

			//コマンド
			SendToGhostCommand = new ActionCommand(
				o =>
				{
					//イベントのスコープを検出して送信する必要がある
					throw new NotImplementedException();
				}
				);

			InsertCommand = new ActionCommand(
				o =>
				{
					control.MainTextEditor.Document.Insert(control.MainTextEditor.CaretOffset, ((InsertItemPaletteModel)o).Body);
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
			disableBodyPropertyChanged = true;
			TextFile.Body = Document.Text;
			disableBodyPropertyChanged = false;
		}

		public void MoveCaretToLine(int lineIndex)
		{
			if (lineIndex < control.MainTextEditor.LineCount)
			{
				control.MainTextEditor.ScrollToLine(lineIndex);
				control.MainTextEditor.CaretOffset = Document.Lines[lineIndex].Offset;
			}
		}

		public void Dispose()
		{
			//TextFile.PropertyChanged -= TextFile_PropertyChanged;
			Document.TextChanged -= Document_TextChanged;
		}

		public void ControlBind(System.Windows.Controls.Control ctrl)
		{
			if (ctrl is TextEditor textEditor)
			{
				control = textEditor;
				control.UpdateInsertPaletteKeyBindings(Main.InsertPalette, InsertCommand);
			}
		}
	}
}
