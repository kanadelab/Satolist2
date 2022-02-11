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
	/// DebugMainMenu.xaml の相互作用ロジック
	/// </summary>
	public partial class DebugMainMenu : UserControl
	{
		public DebugMainMenu()
		{
			InitializeComponent();
		}
	}

	internal class DebugMainMenuViewModel : NotificationObject, IDockingWindowContent
	{
		private string loadGhostPath;
		private string loadShellDirectoryName;
		private MainViewModel Main { get; set; }

		public string LoadGhostPath
		{
			get => loadGhostPath;
			set
			{
				loadGhostPath = value;
				NotifyChanged();
			}
		}

		public string LoadShellDirectoryName
		{
			get => loadShellDirectoryName;
			set
			{
				loadShellDirectoryName = value;
				NotifyChanged();
			}
		}

		public ActionCommand LoadCommand { get; }

		public DebugMainMenuViewModel(MainViewModel mainViewModel, string loadGhostPath = "", string loadShellDirectoryName = "")
		{
			Main = mainViewModel;
			LoadGhostPath = loadGhostPath;
			LoadShellDirectoryName = loadShellDirectoryName;

			LoadCommand = new ActionCommand(
				o => mainViewModel.MainWindow.OpenGhost(LoadGhostPath, LoadShellDirectoryName)
				);
		}

		public string DockingTitle => "DebugMainMenu";

		public string DockingContentId => "DebugMainMenu";
	}
}
