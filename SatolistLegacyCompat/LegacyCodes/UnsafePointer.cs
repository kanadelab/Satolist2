using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Satolist
{
	class UnsafePointer : IDisposable
	{
		private GCHandle handle;
		private IntPtr ptr;

		internal UnsafePointer(GCHandle handle, IntPtr ptr)
		{
			this.handle = handle;
			this.ptr = ptr;
		}

		~UnsafePointer()
		{
			Dispose();
		}

		public IntPtr IntPtr
		{
			get { return ptr; }
		}

		#region IDisposable メンバ

		public void Dispose()
		{
			lock (this)
			{
				if (this.handle.IsAllocated)
				{
					this.handle.Free();
				}
			}
		}

		#endregion
	}
}
