using ControlzEx.Standard;
using Satolist2.Core;
using Satolist2.Utility;
using Sgry.Azuki.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Satolist2.Core.Win32Import;

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
		public const string ContentId = "DebugMainMenu";
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
		public ActionCommand RecvStartCommand { get; }
		public ActionCommand UITestCommand { get; }

		public DebugMainMenuViewModel(MainViewModel mainViewModel, string loadGhostPath = "", string loadShellDirectoryName = "")
		{
			Main = mainViewModel;
			LoadGhostPath = loadGhostPath;
			LoadShellDirectoryName = loadShellDirectoryName;

			LoadCommand = new ActionCommand(
				o => mainViewModel.MainWindow.OpenGhost(LoadGhostPath, LoadShellDirectoryName)
				);

			/*
			RecvStartCommand = new ActionCommand(
				o => Satolist2.Core.SatoriRecvNativeWindow.Create((new System.Windows.Interop.WindowInteropHelper(Main.MainWindow)).Handle)
				);
			*/
			UITestCommand = new ActionCommand(
				 o =>
				 {
					 
					 /*
					 var d = new Dialog.UploadSettingDialog();
					 d.DataContext = new Dialog.UploadSettingDialogViewModel();
					 d.ShowDialog();
					 */

				 }
				);
		}

		public string DockingTitle => "DebugMainMenu";

		public string DockingContentId => ContentId;
	}
}
