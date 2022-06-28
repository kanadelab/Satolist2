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

namespace Satolist2.Dialog
{
	/// <summary>
	/// SatoriUpdateDialog.xaml の相互作用ロジック
	/// </summary>
	public partial class SatoriUpdateDialog : DialogContentBase
	{
		internal new SatoriUpdateDialogViewModel DataContext
		{
			get => (SatoriUpdateDialogViewModel)base.DataContext;
			set => base.DataContext = value;
		}

		public SatoriUpdateDialog(GhostModel ghost, string currentVersion)
		{
			InitializeComponent();

			//バージョン情報を取得
			DataContext = new SatoriUpdateDialogViewModel(this, ghost, currentVersion);
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//里々のリリースを開く
				System.Diagnostics.Process.Start("https://github.com/ponapalt/satoriya-shiori/releases");
			}
			catch { }
		}
	}

	internal class SatoriUpdateDialogViewModel : NotificationObject
	{
		public enum DialogStatus
		{
			Loading,
			LoadFailed,
			Ready
		}

		private SatoriUpdateDialogItemViewModel selectedItem_;
		private DialogStatus dialogStatus_;

		public SatoriUpdateDialog Dialog { get; }
		public GhostModel Ghost { get; }
		public string CurrentSatoriVersion { get; }

		public List<SatoriUpdateDialogItemViewModel> Items { get; }
		public SatoriUpdateDialogItemViewModel SelectedItem
		{
			get => selectedItem_;
			set
			{
				selectedItem_ = value;
				NotifyChanged();
			}
		}

		public DialogStatus Status
		{
			get => dialogStatus_;
			set
			{
				dialogStatus_ = value;
				NotifyChanged();
			}
		}

		public ActionCommand OkCommand { get; }
		public ActionCommand CancelCommand { get; }

		public SatoriUpdateDialogViewModel(SatoriUpdateDialog dialog, GhostModel ghost, string currentVersion)
		{
			Status = DialogStatus.Loading;
			Dialog = dialog;
			Ghost = ghost;
			CurrentSatoriVersion = currentVersion;
			Items = new List<SatoriUpdateDialogItemViewModel>();

			//里々バージョン文字列を解析して一致部分を取り出す
			var sp = currentVersion.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			var versionElement = sp[sp.Length - 1];

			//リリースを取得
			Task.Run(() =>
		   {
			   try
			   {
				   var releases = SatoriUpdator.GetRelease();
				   dialog.Dispatcher.Invoke(() =>
				  {
					  foreach (var item in releases)
					  {
						  var viewmodel = new SatoriUpdateDialogItemViewModel(item);
						  viewmodel.IsCurrentVersion = item.Label.Equals(versionElement, StringComparison.OrdinalIgnoreCase);
						  Items.Add(viewmodel);
					  }
					  Status = DialogStatus.Ready;
				  });
			   }
			   catch
			   {
				   dialog.Dispatcher.Invoke(() => { Status = DialogStatus.LoadFailed; });
			   }
		   });

			OkCommand = new ActionCommand(
				o =>
				{
					if(MessageBox.Show(string.Format("{0} に更新します。よろしいですか？", SelectedItem.Title), "里々のアップデート", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						dialog.DialogResult = true;
						dialog.Close();
					}
				}
				);

			CancelCommand = new ActionCommand(
				o =>
				{
					dialog.Close();
				}
				);
		}
	}

	internal class SatoriUpdateDialogItemViewModel : NotificationObject
	{
		public SatoriReleaseInfo Release { get; }
		public string Title => Release.Label;
		public string Description => Release.Description;
		public bool IsCurrentVersion { get; set; }

		public SatoriUpdateDialogItemViewModel(SatoriReleaseInfo release)
		{
			Release = release;
		}
	}
}
