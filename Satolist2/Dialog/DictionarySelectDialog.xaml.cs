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
using System.Windows.Shapes;

namespace Satolist2.Dialog
{
	//辞書フォルダをえらぶ
	public partial class DictionarySelectDialog : Window
	{
		private MainViewModel main;

		//移動先とかなので無条件にリストアップしないような感じでどうかな
		public IEnumerable<Model.DictionaryModel> Items
		{
			get
			{
				return main.Ghost.Dictionaries.Where(o => !o.IsSerialized);
			}
		}

		public Model.DictionaryModel SelectedItem
		{
			get; private set;
		}

		public ActionCommand OkCommand { get; }
		public ActionCommand CancelCommand { get; }

		internal DictionarySelectDialog(MainViewModel main)
		{
			InitializeComponent();
			this.main = main;
			Owner = main.MainWindow;

			OkCommand = new ActionCommand(
				o =>
				{
					DialogResult = true;
					Close();
				},
				o =>
				{
					return MainList.SelectedItem != null;
				}
				);

			CancelCommand = new ActionCommand(
				o => Close()
				);

			DataContext = this;
		}

		private void MainList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			OkCommand.NotifyCanExecuteChanged();
			SelectedItem = (Model.DictionaryModel)MainList.SelectedItem;
		}

		private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if(OkCommand.CanExecute(null))
			{
				OkCommand.Execute(null);
			}
		}
	}

}
