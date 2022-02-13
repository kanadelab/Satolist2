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
	}

	internal class TextEditorViewModel : NotificationObject, IDockingWindowContent, IDisposable
	{
		private bool disableBodyPropertyChanged;
		private string randomizedContetnId;
		public TextFileModel TextFile { get; }
		public TextDocument Document { get; }

		public string DockingTitle => TextFile.RelativeName;

		public string DockingContentId => randomizedContetnId;

		public TextEditorViewModel(TextFileModel textFile)
		{
			randomizedContetnId = Guid.NewGuid().ToString();    //複数出現するのでユニークなIDを振る
			TextFile = textFile;
			TextFile.PropertyChanged += TextFile_PropertyChanged;

			Document = new TextDocument(TextFile.Body);
			Document.TextChanged += Document_TextChanged;
		}

		private void Document_TextChanged(object sender, EventArgs e)
		{
			disableBodyPropertyChanged = true;
			TextFile.Body = Document.Text;
			disableBodyPropertyChanged = false;
		}

		private void TextFile_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(TextFileModel.Body) && !disableBodyPropertyChanged)
				Document.Text = TextFile.Body;
		}

		public void Dispose()
		{
			TextFile.PropertyChanged -= TextFile_PropertyChanged;
			Document.TextChanged -= Document_TextChanged;
		}
	}
}
