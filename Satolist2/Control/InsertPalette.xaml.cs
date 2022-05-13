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

namespace Satolist2.Control
{
	/// <summary>
	/// InsertPalette.xaml の相互作用ロジック
	/// </summary>
	public partial class InsertPalette : UserControl
	{
		internal new InsertPaletteViewModel DataContext
		{
			get => (InsertPaletteViewModel)base.DataContext;
			set => base.DataContext = value;
		}

		public InsertPalette()
		{
			InitializeComponent();
			
		}

		private void InsertButton_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (sender is Button b)
			{
				if (b.DataContext is InsertItemPaletteModel model)
				{
					if(DataContext.InsertCommand.CanExecute(model))
					{
						Dispatcher.BeginInvoke(new Action(() => DataContext.InsertCommand.Execute(model)), System.Windows.Threading.DispatcherPriority.Render);
					}
				}
			}
			e.Handled = true;
		}
	}

	internal class InsertPaletteViewModel : NotificationObject, IDockingWindowContent
	{
		public const string ContentId = "InsertPalette";
		public MainViewModel Main { get; }
		public string DockingTitle => "挿入パレット";

		public string DockingContentId => ContentId;
		public ActionCommand InsertCommand { get; }

		public IEnumerable<InsertItemPaletteModel> Items
		{
			get
			{
				return MainViewModel.EditorSettings.InsertPalette?.Items;
			}
		}

		public InsertPaletteViewModel(MainViewModel main)
		{
			Main = main;

			InsertCommand = new ActionCommand(
				o =>
				{
					if (o is InsertItemPaletteModel model)
					{
						Main.InsertToActiveEditor(model.Body);
					}
				},
				o =>
				{
					if(o is InsertItemPaletteModel model)
					{
						return model.Type == InsertItemPaletteModel.NodeType.Item;
					}
					return false;
				}
				);
		}

		public void NotifyModelUpdated()
		{
			NotifyChanged(nameof(Items));
		}
	}
}
