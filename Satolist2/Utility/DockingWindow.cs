using AvalonDock.Layout;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Utility
{
	internal class DockingWindow : LayoutAnchorable
	{
		public object ViewModel
		{
			get => ((System.Windows.Controls.Control)Content).DataContext;
			set
			{
				if(((System.Windows.Controls.Control)Content).DataContext is INotifyPropertyChanged oldVm)
				{
					oldVm.PropertyChanged -= ViewModel_PropertyChanged;
				}

				((System.Windows.Controls.Control)Content).DataContext = value;

				if (value is INotifyPropertyChanged newVm)
				{
					newVm.PropertyChanged += ViewModel_PropertyChanged;
				}

				if (value is IDockingWindowContent dockingContent)
				{
					Title = dockingContent.DockingTitle;
					ContentId = dockingContent.DockingContentId;
				}
				else
				{
					Title = "No Title";
					ContentId = string.Empty;
				}
			}
		}

		public DockingWindow(System.Windows.Controls.Control control, INotifyPropertyChanged viewModel = null)
		{
			Content = control;
			ViewModel = viewModel;
		}

		private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if(sender is IDockingWindowContent content)
			{
				if(e.PropertyName == nameof(IDockingWindowContent.DockingTitle))
				{
					Title = content.DockingTitle;
				}
				else if(e.PropertyName == nameof(IDockingWindowContent.DockingContentId))
				{
					ContentId = content.DockingContentId;
				}
			}
		}

		protected override void OnClosing(CancelEventArgs args)
		{
			base.OnClosing(args);
			
			//ViewModelを破棄
			if(ViewModel is IDisposable d)
			{
				d.Dispose();
			}
		}
	}

	internal interface IDockingWindowContent
	{
		string DockingTitle { get; }
		string DockingContentId { get; }
	}

}
