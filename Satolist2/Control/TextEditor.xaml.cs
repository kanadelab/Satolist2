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

	internal class TextEditorViewModel : NotificationObject, IDockingWindowContent
	{
		private string randomizedContetnId;
		public TextFileModel TextFile { get; }

		public string DockingTitle => TextFile.RelativeName;

		public string DockingContentId => randomizedContetnId;

		public TextEditorViewModel(TextFileModel textFile)
		{
			randomizedContetnId = Guid.NewGuid().ToString();    //複数出現するのでユニークなIDを振る
			TextFile = textFile;
		}
	}
}
