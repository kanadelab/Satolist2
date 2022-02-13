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
	/// <summary>
	/// SaveFileListDialog.xaml の相互作用ロジック
	/// </summary>
	public partial class SaveFileListDialog : Window
	{
		public new SaveFileListViewModel DataContext
		{
			get => (SaveFileListViewModel)base.DataContext;
			set => base.DataContext = value;
		}

		public SaveFileListDialog()
		{
			InitializeComponent();
		}
	}

	public class SaveFileListViewModel
	{
		public ISaveFileObject[] Items { get; }

		public SaveFileListViewModel(IEnumerable<ISaveFileObject> saveFiles)
		{
			//変更があったものだけフィルタしてリストアップ
			Items = saveFiles.Where( o=> o.IsChanged).ToArray();
		}
	}

	//変更検出と保存実行
	public interface ISaveFileObject
	{
		bool IsChanged { get; }
		string SaveFilePath { get; }
		void Save();
	}
}
