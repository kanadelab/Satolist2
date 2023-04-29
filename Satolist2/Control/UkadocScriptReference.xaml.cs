using AngleSharp.Dom.Events;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
	/// UkadocScriptReference.xaml の相互作用ロジック
	/// </summary>
	public partial class UkadocScriptReference : UserControl
	{
		public UkadocScriptReference()
		{
			InitializeComponent();
		}

		//スクリプトツリーアイテムのダブルクリック
		private void ScriptTreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (sender is FrameworkElement elem)
			{
				if (elem.DataContext is UkadocSakuraScriptViewModel vm)
				{
					vm.InsertCommand?.Execute(null);
					e.Handled = true;
				}
			}
		}
	}

	public class UkadocScriptReferenceViewModel : NotificationObject, IDockingWindowContent
	{
		public const string ContentId = "UkadocScriptReference";
		public string DockingTitle => "Ukadocスクリプトリファレンス";
		public string DockingContentId => ContentId;

		private bool isLoaded_;
		private UkadocSakuraScriptCategoryViewModel[] scriptItems;

		public ReadOnlyCollection<UkadocSakuraScriptCategoryViewModel> ScriptItems => new ReadOnlyCollection<UkadocSakuraScriptCategoryViewModel>(scriptItems);

		//ukadocデータが読み込めているか
		public bool IsLoaded
		{
			get => isLoaded_;
			set
			{
				isLoaded_ = value;
				NotifyChanged();
			}
		}

		public UkadocScriptReferenceViewModel()
		{
			RefleshView();
		}

		public void RefleshView()
		{
			if (MainViewModel.EditorSettings?.TemporarySettings?.UkadocCache != null)
			{
				var categories = MainViewModel.EditorSettings.TemporarySettings.UkadocCache.EventReferenceCategories.ToArray();
				scriptItems = MainViewModel.EditorSettings.TemporarySettings.UkadocCache.SakuraScriptReferenceCategories.Select(o => new UkadocSakuraScriptCategoryViewModel(o)).ToArray();
				IsLoaded = true;
			}
			else
			{
				scriptItems = Array.Empty<UkadocSakuraScriptCategoryViewModel>();
				IsLoaded = false;
			}
			NotifyChanged(nameof(ScriptItems));
		}
	}

	//Ukadocさくらスクリプトのカテゴリビューモデル
	public class UkadocSakuraScriptCategoryViewModel : NotificationObject
	{
		private UkadocSakuraScriptCategoryModel model;
		private bool isExpanded;

		public string Name => model.Name;
		public UkadocSakuraScriptViewModel[] Scripts { get; private set; }

		public bool IsExpanded
		{
			get => isExpanded;
			set
			{
				isExpanded = value;
				NotifyChanged();
			}
		}

		public UkadocSakuraScriptCategoryViewModel(UkadocSakuraScriptCategoryModel model)
		{
			this.model = model;
			Scripts = model.Scripts.Select(o => new UkadocSakuraScriptViewModel(o)).ToArray();
		}
	}

	//さくらスクリプトのビューモデル
	public class UkadocSakuraScriptViewModel : NotificationObject
	{
		private UkadocSakuraScriptModel model;

		public bool IsExpanded
		{
			get => false;
			set { }
		}

		public bool IsScriptNode => true;
		public string Name => model.Name;
		public string Detail => string.Join("\r\n", model.Details);

		//さくらスクリプトの挿入
		public ICommand InsertCommand;

		public UkadocSakuraScriptViewModel(UkadocSakuraScriptModel model)
		{
			this.model = model;

			InsertCommand = new ActionCommand(
				o =>
				{
					//開いているやつに挿入
					MainWindow.Instance.InsertToActiveEditor(Name);
				});
		}
	}
}
