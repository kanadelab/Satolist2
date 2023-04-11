using Satolist2.Utility;
using SatolistLegacyCompat.CompatCore;
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
	//過去互換コントロールのホスト
	public partial class LegacyControlHost : UserControl
	{
		public LegacyControlHost()
		{
			InitializeComponent();
		}
	}

	internal class LegacyControlViewModel : IDockingWindowContent, IControlBindedReceiver
	{
		private CompatContol compatControl { get; set; }

		public string DockingTitle => compatControl.Label;

		public string DockingContentId => string.Concat("LegacyCompat.", compatControl.Id);

		public LegacyControlViewModel(SatolistLegacyCompat.CompatCore.CompatContol compatControl)
		{
			this.compatControl = compatControl;
		}

		public void ControlBind(System.Windows.Controls.Control ctrl)
		{
			if(ctrl is LegacyControlHost control)
			{
				//コントロールを追加する
				if (compatControl.WpfControl != null)
				{
					control.MainGrid.Children.Clear();
					control.MainGrid.Children.Add(compatControl.WpfControl);
				}
			}
		}
	}

}
