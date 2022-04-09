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
					return jsonSerializer.Deserialize<T>(textReader);
				}
			}
		}

		public static void SerializeToFile(object obj, string path)
		{
			using (var writer = new System.IO.StreamWriter(path))
			{
				var jsonSerializer = new JsonSerializer();
				jsonSerializer.Serialize(writer, obj);
			}
		}

	}
}
