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
		public TextEditorModuleBase MainTextEditor { get; }

		public TextEditorModuleSelector()
		{
			InitializeComponent();
			MainTextEditor = CreateEditor();
			ModuleContainer.Children.Add(MainTextEditor);
			SearchBox.AttachEditor = MainTextEditor;
		}

		public TextEditorModuleBase CreateEditor()
		{
			//モジュールの作成。
			//ControlTemplateとかでやりたいけど、うまくコントロールの取得タイミングに合わないので仕方なく…
			if (MainViewModel.EditorSettings.GeneralSettings.OverrideTextEditorEngine == "Azuki")
			{
				AzukiEditModule module = new AzukiEditModule();
				module.SetBinding(AzukiEditModule.InsertPaletteItemsProperty, new Binding("Main.InsertPalette.Items") { FallbackValue = null });
				module.SetBinding(AzukiEditModule.SendToGhostCommandProperty, new Binding("SendToGhostCommand"));
				module.SetBinding(AzukiEditModule.ShowSearchBoxCommandProperty, new Binding("ShowSearchBoxCommand"));
				module.SetBinding(AzukiEditModule.InsertCommandProperty, new Binding("InsertCommand"));
				return module;
			}
			else
			{
				return new AvalonEditModule();
			}
		}
	}
}
