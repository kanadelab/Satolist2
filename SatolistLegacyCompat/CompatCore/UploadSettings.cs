using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace SatolistLegacyCompat.CompatCore
{
	public class UploadSettings
	{
		private const String UPLOAD_SETTING = "setting,";
		private const String UPLOAD_HOST = "host,";
		private const String UPLOAD_DIRECTORY = "directory,";
		private const String UPLOAD_USER = "user,";
		private const String UPLOAD_PASSWORD = "pass,";
		private const String UPLOAD_USE_PASSWORD = "use_password,";
		private const String UPLOAD_NAR_NAME = "nar,";
		private const String UPLOAD_UPLOADER_SERVICE = "*LOGIN_GHOST_UPLOADER*";
		private const String UPLOAD_USE_SSL = "use_ssl,";
		private const String UPLOAD_USE_PASSIVE = "use_passive,";
		private const String UPLOAD_MODE = "mode,";

		public static IEnumerable<UploadListItem> LoadUploadSettins(string filename, Func<string,string> passwordEncryptor)
		{
			List<UploadListItem> uploadSettings = new List<UploadListItem>();
			if (!File.Exists(filename))
				throw new FileNotFoundException();

			using (StreamReader read = new StreamReader(filename, Encoding.GetEncoding("Shift_JIS")))
			{
				String line;
				UploadListItem item = null;
				while (true)
				{
					line = read.ReadLine();
					if (line == null)
						break;

					if (line.IndexOf(UPLOAD_SETTING) == 0)
					{
						//ヘッド
						if (item != null)
						{
							uploadSettings.Add(item);
						}
						item = new UploadListItem(line.Substring(UPLOAD_SETTING.Length));
					}
					else if (line.IndexOf(UPLOAD_HOST) == 0)
					{
						//ホスト名
						item.mHostName = line.Substring(UPLOAD_HOST.Length);
					}
					else if (line.IndexOf(UPLOAD_USER) == 0)
					{
						//ユーザ名
						item.mUserName = line.Substring(UPLOAD_USER.Length);
					}
					else if (line.IndexOf(UPLOAD_PASSWORD) == 0)
					{
						//pass
						item.mPassword = passwordEncryptor(line.Substring(UPLOAD_PASSWORD.Length));
					}
					else if (line.IndexOf(UPLOAD_DIRECTORY) == 0)
					{
						//dir
						item.mDirectory = line.Substring(UPLOAD_DIRECTORY.Length);
					}
					else if (line.IndexOf(UPLOAD_USE_PASSWORD) == 0)
					{
						item.mIsEveryInputPassword = bool.Parse(line.Substring(UPLOAD_USE_PASSWORD.Length));
					}
					else if (line.IndexOf(UPLOAD_NAR_NAME) == 0)
					{
						item.mNarName = line.Substring(UPLOAD_NAR_NAME.Length);
					}
					else if (line.IndexOf(UPLOAD_USE_SSL) == 0)
					{
						item.mIsUseSSL = bool.Parse(line.Substring(UPLOAD_USE_SSL.Length));
					}
					else if (line.IndexOf(UPLOAD_USE_PASSIVE) == 0)
					{
						item.mIsUsePassive = bool.Parse(line.Substring(UPLOAD_USE_PASSIVE.Length));
					}
					else if (line.IndexOf(UPLOAD_MODE) == 0)
					{
						item.mUploadMode = (UploadListItem.UploadMode)int.Parse(line.Substring(UPLOAD_MODE.Length));
					}

				}

				if (item != null)
					uploadSettings.Add(item);
			}
			return uploadSettings;
		}


		public class UploadListItem
		{
			public String mSettingName { get; set; }
			public String mPassword = "";
			public bool mIsEveryInputPassword = false;
			public String mHostName = "";
			public String mUserName = "";
			public String mDirectory = "";
			public String mNarName = "";
			public bool mIsUseSSL = false;
			public bool mIsUsePassive = true;
			public UploadMode mUploadMode = UploadMode.Ftp;

			public UploadListItem(String name)
			{
				mSettingName = name;
			}
			public override string ToString()
			{
				return mSettingName;
			}

			public enum UploadMode
			{
				Ftp,
				NarNaLoader,
				NarNaLoader2
			}
		}
	}
}
