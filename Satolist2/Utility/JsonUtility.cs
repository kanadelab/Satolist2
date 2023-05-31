using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Utility
{
	public class JsonUtility
	{
		public static JToken DeserializeFromFile(string path)
		{
			using(var stream = new System.IO.StreamReader(path))
			{
				using(var textReader = new JsonTextReader(stream))
				{
					var jsonSerializer = new JsonSerializer();
					return (JToken)jsonSerializer.Deserialize(textReader);
				}
			}
		}

		public static T DeserializeFromFile<T>(string path)
		{
			using(var stream = new System.IO.StreamReader(path))
			{
				using(var textReader = new JsonTextReader(stream))
				{
					var jsonSerializer = new JsonSerializer();
					jsonSerializer.Formatting = Formatting.Indented;
					return jsonSerializer.Deserialize<T>(textReader);
				}
			}
		}

		public static T DeserializeFromFileWithBackup<T>(string path)
		{
			if (System.IO.File.Exists(path))
			{
				try
				{
					return DeserializeFromFile<T>(path);
				}
				catch
				{
					return DeserializeFromFile<T>(MakeBackupPath(path));
				}
			}
			else
			{
				return DeserializeFromFile<T>(MakeBackupPath(path));
			}
		}

		public static void SerializeToFile(string path, object obj)
		{
			using (var writer = new System.IO.StreamWriter(path))
			{
				var jsonSerializer = new JsonSerializer();
				jsonSerializer.Formatting = Formatting.Indented;
				jsonSerializer.Serialize(writer, obj);
			}
		}

		public static void SerializeToFileWithBackup(string path, object obj)
		{
			//バックアップをとってから保存
			try
			{
				System.IO.File.Copy(path, MakeBackupPath(path), true);
			}
			catch { }	//バックアップ失敗自体は許容
			SerializeToFile(path, obj);
		}

		//バックアップか正本が存在しているか
		public static bool ExistsFileWithBackup(string path)
		{
			return System.IO.File.Exists(path) || System.IO.File.Exists(MakeBackupPath(path));
		}

		public static bool SerializableObjectEquals(object a, object b)
		{
			return JObject.FromObject(a).ToString() == JObject.FromObject(b).ToString();
		}

		public static T CloneObject<T>(T obj) { return JObject.FromObject(obj).ToObject<T>(); }


		private static string MakeBackupPath(string path)
		{
			return path + ".bak";
		}
	}
}
