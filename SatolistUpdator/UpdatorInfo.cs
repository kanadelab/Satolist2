using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SatolistUpdator
{
	public static class UpdatorInfo
	{
		public static string GetLocation()
		{
			//自分自身のパスを返す
			//さとりすと本体はアセンブリ位置を移動する可能性があるので、正しい位置を確認する
			return System.Reflection.Assembly.GetExecutingAssembly().Location;
		}
	}
}
