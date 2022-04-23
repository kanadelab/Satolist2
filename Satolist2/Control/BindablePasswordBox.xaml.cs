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
	/// BindablePasswordBox.xaml の相互作用ロジック
	/// </summary>
	public partial class BindablePasswordBox : UserControl
	{
		public static readonly DependencyProperty BindablePasswordProperty = DependencyProperty.Register(nameof(BindablePassword), typeof(string), typeof(BindablePasswordBox),
			new PropertyMetadata( (o,d) => {
				//逆方向反映は無し
				//((BindablePasswordBox)o).PasswordBox.Password = (string)d.NewValue;
			} ));

		public string BindablePassword
		{
			get => (string)GetValue(BindablePasswordProperty);
			set
			{
				SetValue(BindablePasswordProperty, value);
			}
		}

		public BindablePasswordBox()
		{
			InitializeComponent();
		}

		private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
		{
			BindablePassword = Utility.EncryptString.Encrypt(PasswordBox.Password);
		}
	}
}
