using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Utility
{
	//リスト系エディタのシリアライズ中、Changedのフラグが立たないようにするためのオブジェクト
	internal class ListEditorSerializingScope
	{
		private bool isSerializing;

		//シリアライズかどうか
		public bool IsSerializing => isSerializing;

		//シリアライズ中通知
		public ScopeObject NotifySerialize()
		{
			return new ScopeObject(this);
		}

		public class ScopeObject : IDisposable
		{
			private ListEditorSerializingScope item;
			public ScopeObject(ListEditorSerializingScope obj)
			{
				item = obj;
				Debug.Assert(!obj.isSerializing);
				obj.isSerializing = true;
			}

			public void Dispose()
			{
				item.isSerializing = false;
			}
		}
	}
}
