using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Model
{
	public class UploadSetting
	{
		public const string UploadSettingPath = "settings/accounts.json";

		public static UploadServerSettingModelBase[] Load()
		{
			if (JsonUtility.ExistsFileWithBackup(UploadSettingPath))
			{
				var itemArray = JsonUtility.DeserializeFromFileWithBackup<JArray>(UploadSettingPath);
				var uploadSettings = new List<UploadServerSettingModelBase>();
				foreach (JObject item in itemArray)
				{
					var itemType = item["ItemType"].ToString();
					switch (itemType)
					{
						case FtpServerSettingModel.Type:
							uploadSettings.Add(item.ToObject<FtpServerSettingModel>());
							break;
						case NarnaloaderV2ServerSettingModel.Type:
							uploadSettings.Add(item.ToObject<NarnaloaderV2ServerSettingModel>());
							break;
						default:
							throw new Exception("アップロード設定に不明なエントリがあります");
					}
				}
				return uploadSettings.ToArray();
			}
			return Array.Empty<UploadServerSettingModelBase>();
		}

		public static void Save(UploadServerSettingModelBase[] items)
		{
			try
			{
				System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(UploadSettingPath));
				JsonUtility.SerializeToFileWithBackup(UploadSettingPath, items);
			}
			catch { }
		}
	}

	[JsonObject]
	public abstract class UploadServerSettingModelBase
	{
		[JsonProperty]
		public string Label { get; set; } = "";
		[JsonProperty]
		public abstract string ItemType { get; }

		public abstract UploadServerSettingModelBase Clone();
		public abstract bool IsEquals(UploadServerSettingModelBase item);
	}

	[JsonObject]
	public class FtpServerSettingModel : UploadServerSettingModelBase
	{
		public const string Type = "FtpServer";
		[JsonProperty]
		public override string ItemType => Type;
		[JsonProperty]
		public string SettingID { get; set; } = Guid.NewGuid().ToString();
		[JsonProperty]
		public string UserName { get; set; } = "";
		[JsonProperty]
		public string Password { get; set; } = "";
		[JsonProperty]
		public string Url { get; set; } = "";
		[JsonProperty]
		public bool AlwaysPasswordInput { get; set; } = false;
		[JsonProperty]
		public List<FtpItemSettingModel> Items { get; set; } = new List<FtpItemSettingModel>();

		public override UploadServerSettingModelBase Clone()
		{
			var obj = new FtpServerSettingModel();
			obj.UserName = UserName;
			obj.Password = Password;
			obj.Url = Url;
			obj.AlwaysPasswordInput = AlwaysPasswordInput;
			obj.Label = Label;
			obj.SettingID = SettingID;
			foreach(var item in Items)
			{
				obj.Items.Add(item.Clone());
			}

			return obj;
		}

		public override bool IsEquals(UploadServerSettingModelBase item)
		{
			if(item is FtpServerSettingModel model)
			{
				if (Label != model.Label)
					return false;
				if (UserName != model.UserName)
					return false;
				if (Password != model.Password)
					return false;
				if (Url != model.Url)
					return false;
				if (AlwaysPasswordInput != model.AlwaysPasswordInput)
					return false;
				if (SettingID != model.SettingID)
					return false;

				if (Items.Count != model.Items.Count)
					return false;
				for (int i = 0; i < Items.Count; i++)
				{
					if (!Items[i].IsEquals(model.Items[i]))
						return false;
				}

				return true;
			}
			return false;
		}

	}

	[JsonObject]
	public class NarnaloaderV2ServerSettingModel : UploadServerSettingModelBase
	{
		public const string Type = "NarnaloaderV2Server";
		[JsonProperty]
		public override string ItemType => Type;
		[JsonProperty]
		public string SettingID { get; set; } = Guid.NewGuid().ToString();
		[JsonProperty]
		public string UserName { get; set; } = "";
		[JsonProperty]
		public string Password { get; set; } = "";
		[JsonProperty]
		public string Url { get; set; } = "";
		[JsonProperty]
		public bool AlwaysPasswordInput { get; set; } = false;
		[JsonProperty]
		public List<NarnaloaderV2ItemSettingModel> Items { get; set; } = new List<NarnaloaderV2ItemSettingModel>();

		public override UploadServerSettingModelBase Clone()
		{
			var obj = new NarnaloaderV2ServerSettingModel();
			obj.UserName = UserName;
			obj.Password = Password;
			obj.Url = Url;
			obj.AlwaysPasswordInput = AlwaysPasswordInput;
			obj.Label = Label;
			obj.SettingID = SettingID;

			foreach(var item in Items)
			{
				obj.Items.Add(item.Clone());
			}

			return obj;
		}

		public override bool IsEquals(UploadServerSettingModelBase item)
		{
			if(item is NarnaloaderV2ServerSettingModel model)
			{
				if (Label != model.Label)
					return false;
				if (UserName != model.UserName)
					return false;
				if (Password != model.Password)
					return false;
				if (Url != model.Url)
					return false;
				if (AlwaysPasswordInput != model.AlwaysPasswordInput)
					return false;
				if (SettingID != model.SettingID)
					return false;

				if (Items.Count != model.Items.Count)
					return false;
				for(int i = 0; i < Items.Count; i++)
				{
					if (!Items[i].IsEquals(model.Items[i]))
						return false;
				}

				return true;
			}
			return false;
		}


	}

	[JsonObject]
	public class FtpItemSettingModel
	{
		public const string Type = "FtpItem";
		[JsonIgnore]
		public string ItemType => Type;
		[JsonProperty]
		public string SettingId { get; set; } = Guid.NewGuid().ToString();
		[JsonProperty]
		public string Label { get; set; } = "";
		[JsonProperty]
		public string UpdatePath { get; set; } = "";
		[JsonProperty]
		public string NarPath { get; set; } = "";

		public FtpItemSettingModel Clone()
		{
			var obj = new FtpItemSettingModel();
			obj.Label = Label;
			obj.UpdatePath = UpdatePath;
			obj.NarPath = NarPath;
			obj.SettingId = SettingId;
			return obj;
		}

		public bool IsEquals(FtpItemSettingModel item)
		{
			if (Label != item.Label)
				return false;
			if (UpdatePath != item.UpdatePath)
				return false;
			if (NarPath != item.NarPath)
				return false;
			if (SettingId != item.SettingId)
				return false;
			return true;
		}
	}

	[JsonObject]
	public class NarnaloaderV2ItemSettingModel
	{
		public const string Type = "NarnaloaderV2Item";
		[JsonIgnore]
		public string ItemType => Type;
		[JsonProperty]
		public string SettingID { get; set; } = Guid.NewGuid().ToString();
		[JsonProperty]
		public string Label { get; set; } = "";
		[JsonProperty]
		public string ItemId { get; set; } = "";

		public NarnaloaderV2ItemSettingModel Clone()
		{
			var obj = new NarnaloaderV2ItemSettingModel();
			obj.Label = Label;
			obj.ItemId = ItemId;
			obj.SettingID = SettingID;
			return obj;
		}

		public bool IsEquals(NarnaloaderV2ItemSettingModel item)
		{
			if (Label != item.Label)
				return false;
			if (ItemId != item.ItemId)
				return false;
			if (SettingID != item.SettingID)
				return false;
			return true;
		}
	}
}
