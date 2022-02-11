using AvalonDock.Layout;
using Satolist2.Control;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Satolist2
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private List<DockingWindow> EventEditors { get; }

		public LayoutDocumentPane DocumentPane { get; }

		private DockingWindow FileEventTree { get; set; }
		private DockingWindow EventList { get; set; }
		private DockingWindow SurfaceViewer { get; set; }
		private DockingWindow SurfacePalette { get; set; }
		private DockingWindow DebugMainMenu { get; set; }

		public MainWindow()
		{
			InitializeComponent();

			EventEditors = new List<DockingWindow>();

			//DockingWindowの作成
			FileEventTree = new DockingWindow(new FileEventTree());
			EventList = new DockingWindow(new EventList());
			SurfaceViewer = new DockingWindow(new SurfaceViewer());
			SurfacePalette = new DockingWindow(new SurfacePalette());
			DebugMainMenu = new DockingWindow(new DebugMainMenu());

			//カラのviewModelを設定
			var mainVM = new MainViewModel(this);
			DataContext = mainVM;
			ReflectControlViewModel(mainVM);

			//パネルの生成
			var horizontalPanel = new LayoutPanel() { Orientation = Orientation.Horizontal };
			var topPane = new LayoutAnchorablePane();
			var bottomPane = new LayoutAnchorablePane() { DockHeight = new GridLength(250.0) };

			bottomPane.Children.Add(EventList);

			var leftPane = new LayoutAnchorablePane() { DockWidth = new GridLength(200.0) };
			var rightPane = new LayoutAnchorablePane() { DockWidth = new GridLength(300.0) };
			leftPane.Children.Add(FileEventTree);
			rightPane.Children.Add(SurfaceViewer);
			rightPane.Children.Add(SurfacePalette);
			DocumentPane = new LayoutDocumentPane();
			DocumentPane.Children.Add(DebugMainMenu);

			horizontalPanel.Children.Add(leftPane);
			horizontalPanel.Children.Add(DocumentPane);
			horizontalPanel.Children.Add(rightPane);

			//VerticalPanel.Children.Add(topPane);
			VerticalPanel.Children.Add(horizontalPanel);
			VerticalPanel.Children.Add(bottomPane);


			//PaneB.Children.Add( new DockingWindow(new FileEventTree(), mainVM.FileEventTreeViewModel));
			//LeftPane.InsertChildAt(0, new DockingWindow(new EventList(), mainVM.EventListViewModel));
			//RightPane.InsertChildAt(0, new DockingWindow(new FileEventTree(), mainVM.FileEventTreeViewModel));
		}

		internal void OpenEventEditor(EventModel ev)
		{
			var currentWindow = EventEditors.FirstOrDefault(o => ((EventEditorViewModel)o.ViewModel).Event == ev);
			if (currentWindow == null)
			{
				var newWindow = new DockingWindow(new EventEditor(), new EventEditorViewModel(ev));
				EventEditors.Add(newWindow);
				DocumentPane.Children.Add(newWindow);
				currentWindow = newWindow;
			}

			//アクティベート
			currentWindow.IsActive = true;
		}

		internal void OpenTextEditor(TextFileModel text)
		{

		}

		internal void OpenGhost(string ghostPath, string shellDirectoryName)
		{
			var ghost = new GhostModel(ghostPath);
			var mainVM = new MainViewModel(this, ghost, shellDirectoryName);
			DataContext = mainVM;
			ReflectControlViewModel(mainVM);
		}

		//各コントロールのViewModelの再バインド
		private void ReflectControlViewModel(MainViewModel mainVm)
		{
			FileEventTree.ViewModel = mainVm.FileEventTreeViewModel;
			EventList.ViewModel = mainVm.EventListViewModel;
			SurfacePalette.ViewModel = mainVm.SurfacePaletteViewModel;
			SurfaceViewer.ViewModel = mainVm.SurfaceViewerViewModel;
			DebugMainMenu.ViewModel = mainVm.DebugMainMenuViewModel;
		}
	}

	//ワークスペースは切り離せるのが望ましそう。Dockのビューモデルとかもくっついてそうだし
	internal class MainViewModel : NotificationObject
	{
		public MainWindow MainWindow { get; }
		public GhostModel Ghost { get; private set; }

		public FileEventTreeViewModel FileEventTreeViewModel { get; }
		public EventListViewModel EventListViewModel { get; }
		public SurfaceViewerViewModel SurfaceViewerViewModel { get; }
		public SurfacePaletteViewModel SurfacePaletteViewModel { get; }
		public DebugMainMenuViewModel DebugMainMenuViewModel { get; }

		public List<EventEditorViewModel> EventEditors { get; }

		public MainViewModel(MainWindow mainWindow, GhostModel ghost = null, string shellDirectoryName = null)
		{
			MainWindow = mainWindow;
			Ghost = ghost;
			string shellPath = null;

			if(ghost != null && !string.IsNullOrEmpty(shellDirectoryName))
			{
				shellPath = ghost.FullPath + "/shell/" + shellDirectoryName;
			}

			EventEditors = new List<EventEditorViewModel>();
			FileEventTreeViewModel = new FileEventTreeViewModel(this);
			EventListViewModel = new EventListViewModel(this);
			SurfaceViewerViewModel = new SurfaceViewerViewModel(shellPath);
			SurfacePaletteViewModel = new SurfacePaletteViewModel(shellPath);
			DebugMainMenuViewModel = new DebugMainMenuViewModel(this,
				System.IO.Path.GetFullPath("../../../TestSampleGhost"),
				"master"
				);
		}

		public void OpenEventEditor(EventModel ev)
		{
			MainWindow.OpenEventEditor(ev);
		}
	}
}
