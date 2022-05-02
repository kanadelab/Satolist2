using ICSharpCode.AvalonEdit.Document;
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
			MainTextEditor.SyntaxHighlighting = new SatoriSyntaxHilighter();
		}

		public void RequestFocus()
		{
			Dispatcher.BeginInvoke(new Action(() => { MainTextEditor.Focus(); }), System.Windows.Threading.DispatcherPriority.Render);
		}
	}

	internal class TemporaryTextEditorViewModel : TextEditorViewModelBase, IDockingWindowContent
	{
		private TextDocument document;
		private string title;

		public TextDocument Document
		{
			get => document;
			set
			{
				Document = value;
				NotifyChanged();
				NotifyChanged(nameof(Text));
			}
		}

		public string Text
		{
			get => document.Text;
			set
			{
				document.Text = value;
				NotifyChanged();
				NotifyChanged(nameof(Document));
			}
		}

		public string Title
		{
			get => title;
			set
			{
				title = value;
				NotifyChanged();
			}
		}

		public string DockingTitle => Title;

		public string DockingContentId { get; } = Guid.NewGuid().ToString();

		public TemporaryTextEditorViewModel()
		{
			document = new TextDocument();
			title = "無題";
			document.Text = string.Empty;
		}
	}
}
