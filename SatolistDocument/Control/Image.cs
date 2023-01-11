using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SatolistDocument.Control
{
	internal class Image : System.Windows.Controls.Image
	{
		public Image()
		{
			StretchDirection = System.Windows.Controls.StretchDirection.DownOnly;
			HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
			Margin = new System.Windows.Thickness(0.0, 0.0, 3.0, 3.0);
		}
	}
}
