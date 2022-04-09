using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Satolist2.Model
{
	//Model
	internal class InsertItemPaletteModel
	{
		public enum NodeType
		{
			Item,
			Group,
			Root
		}

		public InsertItemPaletteModel[] Items { get; set; }
		public string Label { get; set; }
		public string Body { get; set; }
		public int ShortCutKeyNumber { get; set; }
		public bool ShortCutKeyCtrl { get; set; }
		public bool ShortCutKeyAlt { get; set; }
		public bool ShortCutKeyShift { get; set; }
		public NodeType Type { get; set; }

		//全アイテムの取得
		public IEnumerable<InsertItemPaletteModel> AllItems()
		{
			yield return this;
			foreach (var i in Items ?? Array.Empty<InsertItemPaletteModel>())
				foreach (var r in i.AllItems())
					yield return r;
		}
	}

	internal class InsertItemPaletteShortCutGestureConverter : IValueConverter
	{
		//InputBindingも生成が必要なのでここでできるようにする
		public static System.Windows.Input.KeyGesture ConvertToGesture(InsertItemPaletteModel model)
		{
			if ((model.ShortCutKeyAlt || model.ShortCutKeyCtrl) && model.ShortCutKeyNumber >= 0 && model.ShortCutKeyNumber <= 9)
			{
				System.Windows.Input.Key[] keys =
				{
					System.Windows.Input.Key.D0,
					System.Windows.Input.Key.D1,
					System.Windows.Input.Key.D2,
					System.Windows.Input.Key.D3,
					System.Windows.Input.Key.D4,
					System.Windows.Input.Key.D5,
					System.Windows.Input.Key.D6,
					System.Windows.Input.Key.D7,
					System.Windows.Input.Key.D8,
					System.Windows.Input.Key.D9,
				};

				System.Windows.Input.ModifierKeys mod = System.Windows.Input.ModifierKeys.None;
				if (model.ShortCutKeyCtrl)
					mod |= System.Windows.Input.ModifierKeys.Control;
				if (model.ShortCutKeyAlt)
					mod |= System.Windows.Input.ModifierKeys.Alt;
				if (model.ShortCutKeyShift)
					mod |= System.Windows.Input.ModifierKeys.Shift;

				return new System.Windows.Input.KeyGesture(keys[model.ShortCutKeyNumber], mod);
			}

			return null;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value is InsertItemPaletteModel model)
			{
				if((model.ShortCutKeyAlt || model.ShortCutKeyCtrl) && model.ShortCutKeyNumber >= 0 && model.ShortCutKeyNumber <= 9 )
				{
					List<string> items = new List<string>();

					if (model.ShortCutKeyCtrl)
						items.Add("Ctrl");
					if (model.ShortCutKeyAlt)
						items.Add("Alt");
					if (model.ShortCutKeyShift)
						items.Add("Shift");
					items.Add(model.ShortCutKeyNumber.ToString());
					return string.Join("+", items);
				}
			}
			return string.Empty;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
