using AvalonDock.Layout;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Satolist2.Utility
{
	public class DockingWindow : LayoutAnchorable
	{
		public object ViewModel
		{
			get => ((System.Windows.Controls.Control)Content)?.DataContext;
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

				//Ctrl+Wのクローズ対応
				//バインディングの再設定
				var inputBindings = ((System.Windows.Controls.Control)Content).InputBindings;
				var removeItems = new List<InternalCloseKeyBinding>();
				foreach(var item in inputBindings)
				{
					if (item is InternalCloseKeyBinding bind)
						removeItems.Add(bind);
				}

				foreach (var item in removeItems)
					inputBindings.Remove(item);
				inputBindings.Add(new InternalCloseKeyBinding(this));
			}
		}


		public DockingWindow(System.Windows.Controls.Control control, INotifyPropertyChanged viewModel = null):this()
		{
			Content = control;
			ViewModel = viewModel;
		}

		//デシリアライズ用
		public DockingWindow()
		{
			//ツールウインドウは非表示のみ可能、ドキュメントは外部から逆の設定が入る
			CanClose = false;
			CanHide = true;
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

		// Ctrl+W による閉じるコマンド
		private class InternalCloseKeyBinding : KeyBinding
		{
			public InternalCloseKeyBinding(DockingWindow window):base()
			{
				Key = Key.W;
				Modifiers = ModifierKeys.Control;
				Command = new ActionCommand(
					o =>
					{
						//クローズできるもののみ
						if (window.CanClose)
							window.Close();
					});
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
