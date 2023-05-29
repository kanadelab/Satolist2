using Satolist2.Control;
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

namespace Satolist2.Module.TextEditor
{
	/// <summary>
	/// TextEditorModuleSelector.xaml の相互作用ロジック
	/// </summary>
	public partial class TextEditorModuleSelector : UserControl
	{
		public static readonly DependencyProperty IsEnableSyntaxHighlightingProperty = DependencyProperty.Register(nameof(IsEnableSyntaxHighlighting), typeof(bool), typeof(TextEditorModuleSelector),
			new PropertyMetadata((d, e) =>
			{
				if (d is TextEditorModuleSelector ctrl)
				{
					ctrl.MainTextEditor.IsEnableSyntaxHighlighting = (bool)e.NewValue;
				}
			}));

		public static readonly DependencyProperty SendToGhostCommandProperty = DependencyProperty.Register(nameof(SendToGhostCommand), typeof(ICommand), typeof(TextEditorModuleSelector),
			new PropertyMetadata((d, e) =>
			{
				if (d is TextEditorModuleSelector ctrl)
				{
					ctrl.MainTextEditor.IsEnableSendToGhost = e.NewValue != null;
				}
			}));

		public static readonly DependencyProperty SendToGhostSelectionRangeCommandProperty = DependencyProperty.Register(nameof(SendToGhostSelectionRangeCommand), typeof(ICommand), typeof(TextEditorModuleSelector),
			new PropertyMetadata((d, e) =>
			{
				if (d is TextEditorModuleSelector ctrl)
				{
					ctrl.MainTextEditor.IsEnableSendToGhostSelectionRange = e.NewValue != null;
				}
			}));
		
		private TextEditorSearchBoxViewModel searchBoxViewModel;
		public TextEditorModuleBase MainTextEditor { get; }

		public bool IsEnableSyntaxHighlighting
		{
			get => (bool)GetValue(IsEnableSyntaxHighlightingProperty);
			set => SetValue(IsEnableSyntaxHighlightingProperty, value);
		}

		public ICommand SendToGhostCommand
		{
			get => (ICommand)GetValue(SendToGhostCommandProperty);
			set => SetValue(SendToGhostCommandProperty, value);
		}

		public ICommand SendToGhostSelectionRangeCommand
		{
			get => (ICommand)GetValue(SendToGhostSelectionRangeCommandProperty);
			set => SetValue(SendToGhostSelectionRangeCommandProperty, value);
		}

		public TextEditorModuleSelector()
		{
			InitializeComponent();
			MainTextEditor = CreateEditor();
			ModuleContainer.Children.Add(MainTextEditor);

			//検索ボックス
			searchBoxViewModel = new TextEditorSearchBoxViewModel(SearchBox);
			SearchBox.DataContext = searchBoxViewModel;
			SearchBox.AttachEditor = MainTextEditor;

			//内部イベント
			Unloaded += TextEditorModuleSelector_Unloaded;
			Loaded += TextEditorModuleSelector_Loaded;
		}

		private void TextEditorModuleSelector_Loaded(object sender, RoutedEventArgs e)
		{
			if (!MainTextEditor.IsEventRegistered)
			{
				MainTextEditor.OnShowSearchBoxRequested += MainTextEditor_OnShowSearchBoxRequested;
				MainTextEditor.OnTextChanged += MainTextEditor_OnTextChanged;
				MainTextEditor.OnShowGlobalSearchBox += MainTextEditor_OnShowGlobalSearchBox;
				MainTextEditor.OnSendToGhost += MainTextEditor_OnSendToGhost;
				MainTextEditor.OnSendToGhostSelectionRange += MainTextEditor_OnSendToGhostSelectionRange;

				//多重に発生することがあるようなので対策として
				MainTextEditor.IsEventRegistered = true;
			}
		}

		private void TextEditorModuleSelector_Unloaded(object sender, RoutedEventArgs e)
		{
			if (MainTextEditor.IsEventRegistered)
			{
				MainTextEditor.OnShowSearchBoxRequested -= MainTextEditor_OnShowSearchBoxRequested;
				MainTextEditor.OnTextChanged -= MainTextEditor_OnTextChanged;
				MainTextEditor.OnShowGlobalSearchBox -= MainTextEditor_OnShowGlobalSearchBox;
				MainTextEditor.OnSendToGhost -= MainTextEditor_OnSendToGhost;
				MainTextEditor.OnSendToGhostSelectionRange -= MainTextEditor_OnSendToGhostSelectionRange;
				MainTextEditor.IsEventRegistered = false;
			}
		}

		private void MainTextEditor_OnSendToGhost(object sender, EventArgs e)
		{
			SendToGhostCommand?.Execute(null);
		}

		private void MainTextEditor_OnSendToGhostSelectionRange(object sender, EventArgs e)
		{
			SendToGhostSelectionRangeCommand?.Execute(null);
		}

		private void MainTextEditor_OnShowGlobalSearchBox(object sender, EventArgs e)
		{
			MainWindow.Instance.DataContext.ShowSearchBoxCommand.Execute(MainTextEditor.SelectionString);
		}

		private void MainTextEditor_OnTextChanged(object sender, EventArgs e)
		{
			searchBoxViewModel.RefleshSearch();
		}

		private void MainTextEditor_OnShowSearchBoxRequested(object sender, EventArgs e)
		{
			searchBoxViewModel.SearchString = MainTextEditor.SelectionString;
			searchBoxViewModel.IsShowSearchBox = true;
			searchBoxViewModel.SearchBoxFocusTrigger = true;

			//検索ボックスを全選択にする
			SearchBox.MainTextBox.SelectAll();
		}

		public TextEditorModuleBase CreateEditor()
		{
			//モジュールの作成。
			//ControlTemplateとかでやりたいけど、うまくコントロールの取得タイミングに合わないので仕方なく…
			if (MainViewModel.EditorSettings.GeneralSettings.OverrideTextEditorEngine == "Azuki")
			{
				AzukiEditModule module = new AzukiEditModule();
				return module;
			}
			else
			{
				return new AvalonEditModule();
			}
		}
	}
}
