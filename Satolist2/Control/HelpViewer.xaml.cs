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
	/// HelpViewer.xaml の相互作用ロジック
	/// </summary>
	public partial class HelpViewer : UserControl
	{
		public HelpViewer()
		{
			InitializeComponent();
		}
	}

	public class HelpViewerViewModel : NotificationObject, IDockingWindowContent
	{
		public const string ContentId = "HelpViewer";
		public string DockingTitle => "ヘルプ(試験的)";

		public string DockingContentId => ContentId;
	}
}
