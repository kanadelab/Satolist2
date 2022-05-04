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
using System.Windows.Shapes;

namespace Satolist2.Dialog
{
	/// <summary>
	/// AddEventDialog.xaml の相互作用ロジック
	/// </summary>
	public partial class AddEventDialog : Window
	{
		internal new AddEventDialogViewModel DataContext
		{
			get => base.DataContext as AddEventDialogViewModel;
			set
			{
				base.DataContext = value;
			}
		}

		internal AddEventDialog(MainViewModel main)
		{
			InitializeComponent();
			Owner = main.MainWindow;
			DataContext = new AddEventDialogViewModel(main);
			DataContext.AddTarget = DataContext.Main.Ghost.Dictionaries.FirstOrDefault();
		}

		private void OkButtonClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void CancelButtonClick(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}

	internal class AddEventDialogViewModel : NotificationObject
	{
		private string name;
		private string condition;
		private EventType type;
		private DictionaryModel addTarget;

		public MainViewModel Main { get; }

		public string Name
		{
			get => name;
			set
			{
				name = value;
				NotifyChanged();
			}
		}

		public string Condition
		{
			get => condition;
			set
			{
				condition = value;
				NotifyChanged();
			}
		}

		public DictionaryModel AddTarget
		{
			get => addTarget;
			set
			{
				addTarget = value;
				NotifyChanged();
			}
		}

		public bool IsWordSelected
		{
			get => type == EventType.Word;
			set
			{
				if (value)
					type = EventType.Word;
				NotifyChanged();
				NotifyChanged(nameof(IsSentenceSelected));
				NotifyChanged(nameof(Type));
			}
		}

		public bool IsSentenceSelected
		{
			get => type == EventType.Sentence;
			set
			{
				if (value)
					type = EventType.Sentence;
				NotifyChanged();
				NotifyChanged(nameof(IsWordSelected));
				NotifyChanged(nameof(Type));
			}
		}

		public EventType Type
		{
			get => type;
			set
			{
				type = value;
				NotifyChanged();
				NotifyChanged(nameof(IsWordSelected));
				NotifyChanged(nameof(IsSentenceSelected));
			}
		}

		public AddEventDialogViewModel(MainViewModel main)
		{
			Main = main;
		}
	}
}
