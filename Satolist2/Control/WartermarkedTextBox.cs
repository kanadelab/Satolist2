using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Satolist2.Control
{

	/// <summary>
	/// ウォーターマークつきテキストボックス
	/// </summary>
	//参考: https://araramistudio.jimdo.com/2016/12/19/wpf%E3%81%A7watermarkedtextbox-%E3%83%97%E3%83%AC%E3%83%BC%E3%82%B9%E3%83%9B%E3%83%AB%E3%83%80%E3%83%BC-%E3%82%92%E4%BD%9C%E3%82%8B/

	public class WartermarkedAdorner : Adorner
	{
		private TextBlock wartermarkTextBlock;
		private VisualCollection visualChildren;

		public WartermarkedAdorner(UIElement element) : base(element)
		{
			wartermarkTextBlock = new TextBlock();
			wartermarkTextBlock.Margin = new Thickness(5, 5, 5, 0);
			wartermarkTextBlock.Opacity = 0.3;
			wartermarkTextBlock.IsHitTestVisible = false;
			wartermarkTextBlock.Foreground = Themes.ApplicationTheme.GetWartermarkBrush();

			visualChildren = new VisualCollection(this);
			visualChildren.Add(wartermarkTextBlock);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var element = (FrameworkElement)AdornedElement;
			wartermarkTextBlock.Arrange(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
			return finalSize;
		}

		protected override int VisualChildrenCount => visualChildren.Count;
		protected override Visual GetVisualChild(int index)
		{
			return visualChildren[index];
		}

		public void SetWartermark(string text)
		{
			wartermarkTextBlock.Text = text;
		}
	}


	class WartermarkedTextBox : TextBox
	{
		public static readonly DependencyProperty WartermarkProperty = DependencyProperty.Register("Wartermark", typeof(string), typeof(WartermarkedTextBox),
			new UIPropertyMetadata(string.Empty, (d, e) => { ((WartermarkedTextBox)d).wartermarkedAdorner?.SetWartermark(e.NewValue.ToString()); }));

		private WartermarkedAdorner wartermarkedAdorner;

		public string Wartermark
		{
			get { return (string)GetValue(WartermarkProperty); }
			set { SetValue(WartermarkProperty, value); }
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
			this.Loaded += InitializeAdorner;
			this.Unloaded += DestroyAdoener;
			this.IsVisibleChanged += WartermarkedTextBox_IsVisibleChanged;
		}

		private void WartermarkedTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue)
			{
				//表示になった場合は再表示を試みるので、テキスト変更と同じことをする
				OnTextChanged(null);
			}
			else if(wartermarkedAdorner != null)
			{
				//親が非表示なら消す
				wartermarkedAdorner.Visibility = Visibility.Collapsed;
			}
		}

		private void InitializeAdorner(object sender, RoutedEventArgs e)
		{
			var layer = AdornerLayer.GetAdornerLayer(this);
			wartermarkedAdorner = new WartermarkedAdorner(this);
			wartermarkedAdorner.SetWartermark(Wartermark);
			layer?.Add(wartermarkedAdorner);
			OnTextChanged(null);	//初期状態
		}

		private void DestroyAdoener(object sender, RoutedEventArgs e)
		{
			var layer = AdornerLayer.GetAdornerLayer(this);
			layer?.Remove(wartermarkedAdorner);
		}

		protected override void OnTextChanged(TextChangedEventArgs e)
		{
			if(wartermarkedAdorner != null)
			{
				if (string.IsNullOrEmpty(Text) && IsVisible)
					wartermarkedAdorner.Visibility = Visibility.Visible;
				else
					wartermarkedAdorner.Visibility = Visibility.Collapsed;
			}
		}


	}
}
