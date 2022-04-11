using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Utility
{
	internal class DescriptItemModel
	{
		internal enum DescriptType
		{
			String,
			Select
		}

		public string Property { get; set; }
		public string Description { get; set; }
		public string Help { get; set; }
		public string Default { get; set; }
		public bool Required { get; set; }
		public DescriptType Type { get; set; }
		public DescriptSelectItem[] Items { get; set; }
	}

	internal class DescriptSelectItem
	{
		public static readonly string DefaultLabel = "デフォルト";
		private string label;
		
		public string Label
		{
			get => label ?? Value;
			set
			{
				label = value;
			}
		}
		public string Value
		{
			get;
			set;
		}
	}


	internal class DataModelManager
	{
		public const string DescriptPath = "data/descript.json";
		public const string InstallPath = "data/install.json";

		public static DescriptItemModel[] DescriptItems { get; private set; }
		public static DescriptItemModel[] InstallItems { get; private set; }

		public static void Load()
		{
			DescriptItems = LoadDescript(DescriptPath);
			InstallItems = LoadDescript(InstallPath);
		}

		private static DescriptItemModel[] LoadDescript(string path)
		{
			var descriptItems = (JArray)JsonUtility.DeserializeFromFile(path);
			var descriptData = new List<DescriptItemModel>();
			foreach(JObject item in descriptItems)
			{
				var d = new DescriptItemModel();
				d.Property = item[nameof(DescriptItemModel.Property)]?.ToString() ?? string.Empty;
				d.Description = item[nameof(DescriptItemModel.Description)]?.ToString() ?? string.Empty;
				d.Required = item[nameof(DescriptItemModel.Required)]?.Value<bool>() ?? false;  //TODO: 配列改行対応
				d.Type = ((DescriptItemModel.DescriptType)Enum.Parse(typeof(DescriptItemModel.DescriptType), item[nameof(DescriptItemModel.Type)].ToString()));

				var help = item[nameof(DescriptItemModel.Help)];
				if (help != null)
				{
					if (help is JArray helpArray)
						d.Help = string.Join(Constants.NewLine, helpArray.Select(o => o.ToString()));
					else
						d.Help = help.ToString();
				}

				List<DescriptSelectItem> items = new List<DescriptSelectItem>();
				items.AddRange(item[nameof(DescriptItemModel.Items)]?.ToObject<DescriptSelectItem[]>() ?? Array.Empty<DescriptSelectItem>());

				var defaultItem = new DescriptSelectItem() { Label = DescriptSelectItem.DefaultLabel, Value = string.Empty };
				var def = item[nameof(DescriptItemModel.Default)]?.ToString();
				if(def != null)
				{
					var defaultValue = items.FirstOrDefault(o => o.Value == def);
					if(defaultValue != null)
					{
						defaultItem.Label = string.Concat(defaultValue.Label, " ", "(", defaultItem.Label, ")");
					}
				}
				items.Insert(0, defaultItem);
				d.Items = items.ToArray();
				d.Default = def ?? string.Empty;

				descriptData.Add(d);
			}
			return descriptData.ToArray();
		}
	

	}
}
