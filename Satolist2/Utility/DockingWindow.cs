using AvalonDock.Layout;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Satolist2.Utility
{
	public class DockingWindow : LayoutAnchorable
	{
		//public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(nameof(Content), typeof(System.Windows.Controls.Control), typeof(DockingWindow));
		/*
		public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel", typeof(object), typeof(DockingWindow),
			new PropertyMetadata(null, (d,e) =>
			{
				((System.Windows.Controls.Control)((DockingWindow)d).Content).DataContext = e.NewValue;
			}
			));
		*/

		public object ViewModel
		{
			//get => GetValue(ViewModelProperty);
			get => ((System.Windows.Controls.Control)Content).DataContext;
			set
			{
				//古い方のvmはdisposeする
				if(ViewModel is IDisposable d)
				{
					d.Dispose();
				}

				//イベントの変更
				if(ViewModel is INotifyPropertyChanged oldVm)
				{
					oldVm.PropertyChanged -= ViewModel_PropertyChanged;
				}

				//TODO: set
				//SetValue(ViewModelProperty, value);
				((System.Windows.Controls.Control)Content).DataContext = value;

				if( value is IControlBindedReceiver recv)
				{
					recv.ControlBind((System.Windows.Controls.Control)Content);
				}

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

		//??
		public DockingWindow()
		{ }

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

	//ビューモデル側にコントロールを渡す処理
	//あんまりお行儀よくないかもだけどね
	internal interface IControlBindedReceiver
	{
		void ControlBind(System.Windows.Controls.Control control);
	}

	//変更検出と保存実行
	internal interface ISaveFileObject
	{
		bool IsChanged { get; }
		string SaveFilePath { get; }
		bool Save();
		void Changed();
		EditorLoadState LoadState { get; }
	}

	public enum EditorLoadState
	{
		Initialized,    //まだロードしてない
		Loaded,         //ロード完了(ファイルが存在しないので新規になっていることを含む)
		LoadFailed      //ロード失敗
	}

}
