using AvalonDock.Converters;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration.Internal;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;

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

	//boolを反転するコンバータ
	public class InvertBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return !(bool)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return !(bool)value;
		}
	}

	//boolを反転した上でVisibilityに変換するコンバータ
	public class InvertBoolToVisibilityConverter : IValueConverter
	{
		private static BoolToVisibilityConverter boolToVisibility = new BoolToVisibilityConverter();

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return boolToVisibility.Convert(!(bool)value, targetType, parameter, culture);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return !(bool)boolToVisibility.ConvertBack(value, targetType, parameter, culture);
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

	//アンダースコアをエスケープしてアンダースコア２個にするコンバータ
	internal class EscapeUnderScoreConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value.ToString().Replace("_", "__");
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	//ColorをSolidColorBrushにするコンバータ
	internal class ColorToBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return new SolidColorBrush((Color)value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	//検索フィルタのヒット有無でVisibilityを切り替えるコンバータ
	//[0] フィルタ対象
	//[1] 検索ボックス側の文字列
	internal class SearchFilterConverter : IMultiValueConverter
	{
		public interface IFilter
		{
			bool Filter(string filterString);
		}

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values[0] is IFilter filter && values[1] is string filterString)
			{
				if(string.IsNullOrWhiteSpace(filterString))
				{
					//空白なら強制的に許容される
					return Visibility.Visible;
				}
				return filter.Filter(filterString) ? Visibility.Visible : Visibility.Collapsed;
			}
			else
			{
				return Visibility.Visible;
			}
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
