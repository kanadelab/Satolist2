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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SatolistDocument.Control
{
	/// <summary>
	/// Text.xaml の相互作用ロジック
	/// </summary>
	public partial class Paragraph : UserControl
	{
		public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(Paragraph));

		//String[] を指定するとIntelliSenseがエラーを吐くのでobjectにしておく…
		public static readonly DependencyProperty LinesProperty = DependencyProperty.Register(nameof(Lines), typeof(object), typeof(Paragraph));

		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		public object Lines
		{
			get => GetValue(LinesProperty);
			set => SetValue(LinesProperty, value);
		}

		public string ContentText
		{
			get
			{
				if(Lines != null)
				{
					return string.Join(Environment.NewLine, (IEnumerable<string>)Lines);
				}
				else
				{
					return Text ?? string.Empty;
				}
			}
		}

		public Paragraph()
		{
			InitializeComponent();
		}
	}
}
