using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Utility
{
	//汎用進捗データ
	public class Progress
	{
		//0-1.0 進捗データ
		public double Value { get; set; }
		//お好きなメッセージをどうぞ
		public string Message { get; set; }
		//falseなら具体的進捗を表示しない
		public bool UseProgress { get; set; }

		public Progress()
		{
			Value = 0.0;
			Message = string.Empty;
			UseProgress = false;
		}
	}
}
