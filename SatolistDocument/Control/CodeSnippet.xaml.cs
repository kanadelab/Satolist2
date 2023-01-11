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

namespace SatolistDocument.Control
{
	/// <summary>
	/// CodeSnippet.xaml の相互作用ロジック
	/// </summary>
	public partial class CodeSnippet : UserControl
	{
		public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(CodeSnippet));
		public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(CodeSnippet));

		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		public string Description
		{
			get => (string)GetValue(DescriptionProperty);
			set => SetValue(DescriptionProperty, value);
		}

		public ActionCommand CopyCommand { get; }

		public CodeSnippet()
		{
			InitializeComponent();
			CopyCommand = new ActionCommand(
				o =>
				{
					try
					{
						Clipboard.SetText(Text);
					}
					catch { }
				});
		}
	}
}
