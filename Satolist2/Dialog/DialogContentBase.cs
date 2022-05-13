using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Satolist2.Dialog
{
	//じつはウインドウのなかにあるコントロールだけど、ダイアログのホストがテーマによって違うウインドウ実態でも同じように扱えるようにする機構
	public class DialogContentBase : UserControl
	{
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(DialogContentBase));
		public static readonly DependencyProperty WindowStartupLocationProperty = DependencyProperty.Register(nameof(WindowStartupLocation), typeof(WindowStartupLocation), typeof(DialogContentBase));

		public event CancelEventHandler Closing;
		public event EventHandler Closed;
		public event EventHandler Activated;

		public string Title
		{
			get => (string)GetValue(TitleProperty);
			set => SetValue(TitleProperty, value);
		}

		public WindowStartupLocation WindowStartupLocation
		{
			get => (WindowStartupLocation)GetValue(WindowStartupLocationProperty);
			set => SetValue(WindowStartupLocationProperty, value);
		}

		public Window Host { get; private set; }
		public Window Owner { get; set; }
		public bool? DialogResult
		{
			get => Host.DialogResult;
			set => Host.DialogResult = value;
		}

		public void Setup(Window host)
		{
			Host = host;
			Host.Closing += Host_Closing;
			Host.Closed += Host_Closed;
			Host.Activated += Host_Activated;
		}

		private void Host_Activated(object sender, EventArgs e)
		{
			Activated?.Invoke(this, e);
		}

		private void Host_Closed(object sender, EventArgs e)
		{
			Host.Closing += Host_Closing;
			Host.Closed += Host_Closed;
			Closed?.Invoke(this, e);
		}

		private void Host_Closing(object sender, CancelEventArgs e)
		{
			Closing?.Invoke(this, e);
		}

		public void Close()
		{
			Host.Close();
		}

		public bool? ShowDialog()
		{
			return Themes.ApplicationTheme.ShowDialog(this);
		}
	}
}
