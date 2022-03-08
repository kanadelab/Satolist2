using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
	/// SaoriList.xaml の相互作用ロジック
	/// </summary>
	public partial class SaoriList : UserControl
	{
		public SaoriList()
		{
			InitializeComponent();
		}
	}

	internal class SaoriListViewModel : NotificationObject, IDockingWindowContent
	{
		private ObservableCollection<SaoriListModuleViewModel> items;

		public ReadOnlyObservableCollection<SaoriListModuleViewModel> Items => new ReadOnlyObservableCollection<SaoriListModuleViewModel>(items);

		public string DockingTitle => "SAORIリスト";

		public string DockingContentId => "SaoriList";

		public SaoriListViewModel()
		{
			items = new ObservableCollection<SaoriListModuleViewModel>();

			items.Add(new SaoriListModuleViewModel() { ModulePath = "saori/ssu.dll" });
			items.Add(new SaoriListModuleViewModel() { ModulePath = "saori/ssu.dll" });
		}
	}

	internal class SaoriListModuleViewModel : NotificationObject
	{
		private ObservableCollection<SaoriListFunctionViewModel> items;
		private string modulePath;

		public ReadOnlyObservableCollection<SaoriListFunctionViewModel> Items => new ReadOnlyObservableCollection<SaoriListFunctionViewModel>(items);
		public string Type => "SaoriModule";

		public string ModulePath
		{
			get => modulePath;
			set
			{
				modulePath = value;
				NotifyChanged();
			}
		}

		public SaoriListModuleViewModel()
		{
			items = new ObservableCollection<SaoriListFunctionViewModel>();

			items.Add(new SaoriListFunctionViewModel()
			{
				Command = "calc",
				Name = "calc"
			});

			items.Add(new SaoriListFunctionViewModel()
			{
				Command = "calc",
				Name = "calc"
			});


		}

	}

	internal class SaoriListFunctionViewModel : NotificationObject
	{
		private string name;
		private string command;

		public object Items => Array.Empty<object>();
		public string Type => "SaoriFunction";

		public string Name
		{
			get => name;
			set
			{
				name = value;
				NotifyChanged();
			}
		}
		public string Command
		{
			get => command;
			set
			{
				command = value;
				NotifyChanged();
			}
		}
	}
}
