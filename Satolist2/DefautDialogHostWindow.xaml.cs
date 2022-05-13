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
using System.Windows.Shapes;

namespace Satolist2
{
	/// <summary>
	/// DefautDialogHostWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class DefautDialogHostWindow : Window
	{
		public DefautDialogHostWindow(Dialog.DialogContentBase content)
		{
			InitializeComponent();
			Content = content;
			Owner = content.Owner;
			WindowStartupLocation = content.WindowStartupLocation;
			Title = content.Title ?? string.Empty;
			Width = content.Width;
			Height = content.Height;

			//コントロール側は未指定の戻す
			content.Width = double.NaN;
			content.Height = double.NaN;
			content.Setup(this);
		}
	}
}
