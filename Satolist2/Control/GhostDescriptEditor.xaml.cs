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
	/// GhostDescriptEditor.xaml の相互作用ロジック
	/// </summary>
	public partial class GhostDescriptEditor : UserControl
	{
		public GhostDescriptEditor()
		{
			InitializeComponent();
		}

		//入力欄のフォーカスを取得した場合、そのListItemをSelectedにする
		private void StringInput_GotFocus(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement elem)
			{
				if (elem.DataContext is DescriptItemViewModel itemViewModel)
				{
					itemViewModel.IsSelected = true;
				}
			}
		}

		//ListViewItemがFocusを得た場合、StringInputをフォーカスする。
		//Selected担った場合も同様
		private void ListViewItem_GotFocus(object sender, RoutedEventArgs e)
		{    
			if (sender is FrameworkElement elem)
			{
				var item = FindChildItem("StringInput", elem);
				if(item is TextBox textBox)
				{
					textBox.Focus();
				}
			}
		}

		private FrameworkElement FindChildItem(string Name, DependencyObject obj)
		{
			var count = VisualTreeHelper.GetChildrenCount(obj);
			for(int i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(obj, i);
				if (child is FrameworkElement elem && elem.Name == Name)
					return elem;

				var childResult = FindChildItem(Name, child);
				if (childResult is FrameworkElement childElem)
					return childElem;
			}
			return null;
		}

		//キーの押下による移動をサポート
		private void StringInput_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			//タブを押して移動をサポートする
			if(e.Key == Key.Tab)
			{
				SettingsList.SelectedIndex = (SettingsList.SelectedIndex + 1) % SettingsList.Items.Count;
				e.Handled = true;
			}
		}
	}

	internal class GhostDescriptEditorViewModel : NotificationObject
	{ 
		private int selectedIndex;

		public IEnumerable<DescriptItemViewModel> Items { get; }

		public int SelectedIndex
		{
			get => selectedIndex;
			set
			{
				selectedIndex = value;
				NotifyChanged();
			}
		}

		public GhostDescriptEditorViewModel()
		{
			Items = new DescriptItemViewModel[]
			{
				new DescriptItemViewModel()
				{
					Label = "name",
					Description = "(必須)ゴースト名",
					Help = "例：奏でる日常の旋律",
					Type = DescriptType.String,
					Required = true
				},

				new DescriptItemViewModel()
				{
					Label = "sakura.name",
					Description = "(必須)メインキャラの名前",
					Help = "例: かなで",
					Type = DescriptType.String,
					Required = true
				},

				new DescriptItemViewModel()
				{
					Label = "craftman",
					Description = "(必須)半角英数の作者名",
					Help = "例：yunosora",
					Type = DescriptType.String,
					Required = true
				},

				new DescriptItemViewModel()
				{
					Label = "craftmanw",
					Description = "(必須)作者名",
					Help = "例: 湯空さとり",
					Type = DescriptType.String,
					Required = true
				},

				new DescriptItemViewModel()
				{
					Label = "craftmanurl",
					Description = "作者サイトのURL",
					Help = "例: https://nanachi.sakura.ne.jp",
					Type = DescriptType.String
				},

				new DescriptItemViewModel()
				{
					Label = "sakura.seriko.defaultsurface",
					Description = "本体側のゴーストのデフォルトサーフェス番号",
					Help = "既定値: 0"
				},

				new DescriptItemViewModel()
				{
					Label = "kero.seriko.defaultsurface",
					Description = "相方側のゴーストのデフォルトサーフェス番号",
					Help = "既定値: 0"
				}




				/*
				new DescriptItemViewModel()
				{
					Label = "name",
					Description = "そのゴーストの名前",
					Type = DescriptType.Bool
				}
				*/
			};
		}
	
	}

	internal enum DescriptType
	{
		String,
		Bool
	}

	internal enum DescriptBool
	{
		Default,
		False,
		True
	}

	internal class DescriptItemViewModel :NotificationObject
	{
		private object val;
		private bool isSelected;

		public string Label { get; set; }
		public string Description { get; set; }
		public string Help { get; set; }
		public DescriptType Type { get; set; }
		public bool Required { get; set; }

		public object Value
		{
			get => val;
			set
			{
				val = value;
				NotifyChanged();
				NotifyChanged(nameof(IsDefault));
			}
		}

		public bool IsDefault
		{
			get
			{
				switch(Type)
				{
					case DescriptType.String:
						return string.IsNullOrEmpty(val as string);
					case DescriptType.Bool:
						return val as DescriptBool? == DescriptBool.Default;
						
				}
				return false;
			}
		}

		public bool IsSelected
		{
			get => isSelected;
			set
			{
				isSelected = value;
				NotifyChanged();
			}
		}
	}
}
