using ICSharpCode.AvalonEdit.Document;
using Satolist2.Module.TextEditor;
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
	/// TemporaryTextEditor.xaml の相互作用ロジック
	/// </summary>
	public partial class TemporaryTextEditor : UserControl
	{
		public TemporaryTextEditor()
		{
			InitializeComponent();
			MainTextEditor.MainTextEditor.UpdateHighlighter();
		}

		public void RequestFocus()
		{
			Dispatcher.BeginInvoke(new Action(() => { MainTextEditor.Focus(); }), System.Windows.Threading.DispatcherPriority.Render);
		}
	}

	internal class TemporaryTextEditorViewModel : TextEditorViewModelBase, IControlBindedReceiver
	{
		private TemporaryTextEditor control;
		private string text;
		private string title;

		public string Text
		{
			get => text;
			set
			{
				text = value;
				NotifyChanged();
			}
		}

		public string Title
		{
			get => title;
			set
			{
				title = value;
				NotifyDocumentTitleChanged();
				NotifyChanged();
			}
		}

		public override string DocumentTitle => Title;

		public override string DockingContentId { get; } = Guid.NewGuid().ToString();

		public override TextEditorModuleBase MainTextEditor => control.MainTextEditor.MainTextEditor;

		public TemporaryTextEditorViewModel(MainViewModel main):base(main)
		{
			title = "無題";
			Text = string.Empty;
		}

		public void ControlBind(System.Windows.Controls.Control control)
		{
			this.control = (TemporaryTextEditor)control;
			this.control.MainTextEditor.MainTextEditor.Text = Text;
		}
	}
}
