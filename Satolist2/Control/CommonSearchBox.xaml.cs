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
	/// CommonSearchBox.xaml の相互作用ロジック
	/// </summary>
	public partial class CommonSearchBox : UserControl
	{
		public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(CommonSearchBox),
			new PropertyMetadata(string.Empty));

		//テキスト本体
		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		public CommonSearchBox()
		{
			InitializeComponent();
		}

		//リセット
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Text = string.Empty;
		}
	}
}
