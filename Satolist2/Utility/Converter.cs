using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Satolist2.Utility
{
	//Imageコントロールに表示するためのBitmapからのコンバータ
	public class BitmapImageSourceConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Bitmap bitmap)
			{
				using (Stream stream = new MemoryStream())
				{
					bitmap.Save(stream, ImageFormat.Png);
					stream.Seek(0, SeekOrigin.Begin);
					BitmapImage result = new BitmapImage();
					result.BeginInit();
					result.CacheOption = BitmapCacheOption.OnLoad;
					result.StreamSource = stream;
					result.EndInit();
					return result;
				}
			}
			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	//NullだとFalseになるコンバータ
	public class ReferenceToBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value != null);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	//ステートをVisibilityに変換するもの
	internal class EditorLoadStateToVisibilityConverter : DependencyObject, IValueConverter
	{
		public static readonly DependencyProperty StateProperty = DependencyProperty.Register(nameof(StateProperty), typeof(EditorLoadState), typeof(EditorLoadStateToVisibilityConverter));
		public EditorLoadState State
		{
			get => (EditorLoadState)GetValue(StateProperty);
			set
			{
				SetValue(StateProperty, value);
			}
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if((EditorLoadState)value == State)
			{
				return Visibility.Visible;
			}
			else
			{
				return Visibility.Collapsed;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
