using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using Satolist2.Module.TextEditor;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Windows.Threading;

namespace Satolist2.Control
{
	/// <summary>
	/// TextEditorSearchBox.xaml の相互作用ロジック
	/// </summary>
	public partial class TextEditorSearchBox : UserControl
	{
		public static readonly DependencyProperty AttachEditorProperty = DependencyProperty.Register(nameof(AttachEditor), typeof(TextEditorModuleBase), typeof(TextEditorSearchBox));

		//フォーカストリガー。trueするとフォーカスする
		public static readonly DependencyProperty FocusTriggerProperty = DependencyProperty.Register(nameof(FocusTrigger), typeof(bool), typeof(TextEditorSearchBox),
			new PropertyMetadata((d,e) =>
			{
				if(d is TextEditorSearchBox ctl)
				{
					if(e.NewValue is bool trigger && trigger)
					{
						ctl.MainTextBox.Focus();
					}
				}
			}));

		private void MainTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			//フォーカスを失った
			FocusTrigger = false;
		}

		public TextEditorModuleBase AttachEditor
		{
			get => (TextEditorModuleBase)GetValue(AttachEditorProperty);
			set => SetValue(AttachEditorProperty, value);
		}

		public bool FocusTrigger
		{
			get => (bool)GetValue(FocusTriggerProperty);
			set => SetValue(FocusTriggerProperty, value);
		}

		public TextEditorSearchBox()
		{
			InitializeComponent();
			IsVisibleChanged += TextEditorSearchBox_IsVisibleChanged;
			MainTextBox.LostFocus += MainTextBox_LostFocus;
		}

		private void TextEditorSearchBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue)
			{
				Dispatcher.BeginInvoke(new Action(() => {MainTextBox.Focus(); }), DispatcherPriority.Render);
			}
			else
			{
				//閉じられたら、エディタを有効化する
				Dispatcher.BeginInvoke(new Action(() => { AttachEditor.RequestFocusToEditor(); }), DispatcherPriority.Render);
			}
		}
	}

	internal class TextEditorSearchBoxViewModel : NotificationObject
	{
		private TextEditorSearchBox control;
		private string searchString;
		private string searchInformation;
		private bool isShowSearchBox;
		private bool searchBoxFocusTrigger;

		public ActionCommand CloseSearchBoxCommand { get; }
		public ActionCommand SearchNextCommand { get; }
		public ActionCommand SearchPrevCommand { get; }

		public string SearchString
		{
			get => searchString ?? string.Empty;
			set
			{
				if (searchString != value)
				{
					searchString = value;
					NotifyChanged();
					UpdateSearchString();
				}
			}
		}

		public string SearchInformation
		{
			get => searchInformation ?? string.Empty;
			set
			{
				searchInformation = value;
				NotifyChanged();
			}
		}

		//検索ボックスの表示
		public bool IsShowSearchBox
		{
			get => isShowSearchBox;
			set
			{
				isShowSearchBox = value;
				NotifyChanged();
			}
		}

		//検索ボックス表示用のトリガー
		public bool SearchBoxFocusTrigger
		{
			get => searchBoxFocusTrigger;
			set
			{
				searchBoxFocusTrigger = value;
				NotifyChanged();
			}
		}

		private void SetSearchInformation(int selectionIndex = -1)
		{
			if (!string.IsNullOrEmpty(SearchString))
			{
				if (control.AttachEditor.CurrentMatchCount == 0)
				{
					SearchInformation = "一致なし";
				}
				else
				{
					SearchInformation = string.Format("{0}/{1}", selectionIndex + 1, control.AttachEditor.CurrentMatchCount);
				}
			}
			else
			{
				SearchInformation = string.Empty;
			}
		}

		//検索文字列が更新された
		private void UpdateSearchString()
		{
			control.AttachEditor.UpdateSearchString(SearchString);

			//結果をリセット
			SetSearchInformation();
		}

		//検索対象の更新
		public void RefleshSearch()
		{
			//TODO: ハイライタの更新など不要な処理は省略できるとよい
			UpdateSearchString();
		}

		//次検索
		public void MoveSearch(bool directionIsNext)
		{
			var selectionIndex = control.AttachEditor.MoveSearch(directionIsNext);
			SetSearchInformation(selectionIndex);
			
		}

		public TextEditorSearchBoxViewModel(TextEditorSearchBox control)
		{
			this.control = control;
			searchString = string.Empty;

			CloseSearchBoxCommand = new ActionCommand(
				o =>
				{
					control.Visibility = Visibility.Collapsed;
					SearchString = string.Empty;
				}
				);

			SearchNextCommand = new ActionCommand(
				o =>
				{
					MoveSearch(true);	
				}
				);

			SearchPrevCommand = new ActionCommand(
				o =>
				{
					MoveSearch(false);
				}
				);
		}
	}

	
}
