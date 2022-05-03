using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
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
	/// SurfaceViewer.xaml の相互作用ロジック
	/// </summary>
	public partial class SurfaceViewer : UserControl
	{
		public SurfaceViewer()
		{
			InitializeComponent();
		}
	}

	internal class SurfaceViewerViewModel : NotificationObject, IDockingWindowContent
	{
		public const string ContentId = "SurfaceViewer";
		private MainViewModel main;
		private ShellImageCache cache;
		private bool isPreviewDataEnable;
		private Core.SurfacePreviewMetaData previewData;
		private ICollectionView surfaceList;
		private Core.SurfacePreviewMetaDataRecord selectedSurface;

		public SurfaceViewerViewModel(MainViewModel main)
		{
			this.main = main;
			CollectionViewSource.GetDefaultView(main.SurfacePreviewData);
			if (main.Ghost != null)
			{
				UpdateSurfacePreviewData();
			}
		}

		public void UpdateSurfacePreviewData()
		{
			if (main.SurfacePreviewData != null)
			{
				cache = new ShellImageCache(DictionaryUtility.ConbinePath(main.Ghost.FullDictionaryPath, MainViewModel.SurfacePreviewPath));
				IsPreviewDataEnable = true;
				previewData = main.SurfacePreviewData;
				SurfaceList = CollectionViewSource.GetDefaultView(previewData.Items);
				SurfaceList.Filter = (o) =>
				{
					var item = (Core.SurfacePreviewMetaDataRecord)o;
					return item.IsEnableSurfaceViewer;
				};
				SurfaceList.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
			}
			else
			{
				//プレビューデータが正しくない
				IsPreviewDataEnable = false;
				SurfaceList = CollectionViewSource.GetDefaultView(Array.Empty<Core.SurfacePreviewMetaDataRecord>());
			}
		}

		public Core.SurfacePreviewMetaDataRecord SelectedSurface
		{
			get => selectedSurface;
			set
			{
				selectedSurface = value;
				NotifyChanged();
				NotifyChanged(nameof(SelectedSurfaceBitmap));
			}
		}

		public Bitmap SelectedSurfaceBitmap
		{
			get
			{
				if (SelectedSurface != null)
				{
					var path = SelectedSurface.FileName;
					var loadedImage = cache.LoadImage(path);
					return loadedImage.Image;
				}
				return null;
			}
		}

		public bool IsPreviewDataEnable
		{
			get => isPreviewDataEnable;
			private set
			{
				isPreviewDataEnable = value;
				NotifyChanged();
			}
		}

		public ICollectionView SurfaceList
		{
			get => surfaceList;
			private set
			{
				surfaceList = value;
				NotifyChanged();
			}
		}

		public string DockingTitle => "サーフェスビューワ";

		public string DockingContentId => ContentId;

	}
	

}
