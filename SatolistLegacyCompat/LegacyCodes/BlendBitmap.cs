using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

//参考:
//http://d.hatena.ne.jp/melpon/20061128/p1
namespace Satolist
{
	class BlendBitmap
	{
		private Bitmap mBitmap;
		private BitmapData mDestdata;

		public BlendBitmap( Bitmap bitmap)
		{
			mBitmap = bitmap;
			mDestdata = null;
		}

		private void Lock()
		{
			mDestdata = mBitmap.LockBits(GetBitmapRect(mBitmap), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
		}

		private void Unlock()
		{
			mBitmap.UnlockBits(mDestdata);
			mDestdata = null;
		}

		public void Draw(Bitmap source, int drawX, int drawY, Graphics destGraphics, float scale, SurfaceAnimationPattern.Patterns blend, Bitmap baseSurface)
		{
			//データがおかしいものを弾く
			if (source == null || blend == SurfaceAnimationPattern.Patterns.Invalid)
			{
				return;
			}

			//合成不可能なものをはじく
			if (baseSurface == null)
			{
				switch (blend)
				{
					case SurfaceAnimationPattern.Patterns.Interpolate:
					case SurfaceAnimationPattern.Patterns.Overlayfast:
					case SurfaceAnimationPattern.Patterns.Reduce:
						return;
				}

			}

			Lock();
			//try
			//{

			BitmapData elementData = null;
			int elementStride = 0;
			int elementWidth = 0;
			int elementHeight = 0;
			switch (blend)
			{
				case SurfaceAnimationPattern.Patterns.Interpolate:
				case SurfaceAnimationPattern.Patterns.Overlayfast:
				case SurfaceAnimationPattern.Patterns.Reduce:
					elementData = baseSurface.LockBits(GetBitmapRect(baseSurface), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
					elementStride = elementData.Stride;
					elementWidth = elementData.Width;
					elementHeight = elementData.Height;
					break;
			}
				BitmapData sourceData = source.LockBits(GetBitmapRect(source), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
				IntPtr destLine;
				IntPtr srcLine;
				IntPtr elementLine = IntPtr.Zero;
				IntPtr destPixel;
				IntPtr srcPixel;
				IntPtr elementPixel = IntPtr.Zero;
				int dataLine;
				int dataPixel;

				byte[] data = new byte[sourceData.Stride * sourceData.Height];

				int sourceStride = sourceData.Stride;
				int destStride = mDestdata.Stride;
				
				int destWidth = mDestdata.Width;
				int destHeight = mDestdata.Height;
				int srcWidth = sourceData.Width;
				int srcHeight = source.Height;



				//実際にブレンドしていく
				for (int y = 0; y < srcHeight && y + drawY < destHeight; y++)
				{
					destLine = IntPtr.Add(mDestdata.Scan0, destStride * (y + drawY));
					srcLine = IntPtr.Add(sourceData.Scan0, sourceStride * y);
					if (elementData != null)
					{
						elementLine = IntPtr.Add(elementData.Scan0, elementStride * (y + drawY));
					}
					
					dataLine = sourceStride * y;
					for (int x = 0; x < srcWidth && x + drawX < destWidth; x++)
					{
						if (y + drawY < 0 || x + drawX < 0)
						{
							continue;	//マイナス値範囲外アクセス
						}

						//スキャンライン描画
						destPixel = IntPtr.Add(destLine, 4 * (x + drawX));
						srcPixel = IntPtr.Add(srcLine, 4 * x);
						dataPixel = dataLine + 4 * x;
						if (elementData != null)
						{
							elementPixel = IntPtr.Add(elementLine, 4 * ( x + drawX));
						}

						ColorARGB8 srcArgb = new ColorARGB8();
						srcArgb.FromIntARGB8(Marshal.ReadInt32(srcPixel));
						ColorARGB8 destArgb = new ColorARGB8();
						destArgb.FromIntARGB8(Marshal.ReadInt32(destPixel));
						ColorARGB8 elementArgb = new ColorARGB8();
						if (elementData != null)
						{
							elementArgb.FromIntARGB8(Marshal.ReadInt32(elementPixel));
						}


						//ここで合成
						for (int i = 0; i < 4; i++)
						{
							if (blend == SurfaceAnimationPattern.Patterns.Overlayfast)
							{
								//overlayfast-aplhaが存在する場所にのみ描画
								if (elementArgb.mBytes[3] > 0)
								{
									data[dataPixel + i] = srcArgb.mBytes[i];
								}
							}
							else if (blend == SurfaceAnimationPattern.Patterns.Interpolate)
							{
								//アルファリバースで
								if (i == 3)
								{
									float alphaScale = (1.0f - (elementArgb.mBytes[i] / 255.0f));

									data[dataPixel + i] = (byte)( srcArgb.mBytes[i] * alphaScale );
								}
								else
								{
									data[dataPixel + i] = srcArgb.mBytes[i];
								}

							}
							else if (blend == SurfaceAnimationPattern.Patterns.Reduce)
							{/*
								if (i == 3)
								{
									int alpha = byte.MaxValue - srcArgb.mBytes[i];
									alpha = elementArgb.mBytes[i] - alpha;
									if (alpha > byte.MaxValue)
									{
										alpha = byte.MaxValue;
									}
									else if (alpha < 0)
									{
										alpha = 0;
									}
									data[dataPixel + i] = (byte)alpha;
								}
							  */
							}
							else
							{
								//通常合成法
								data[dataPixel + i] = srcArgb.mBytes[i];
							}
							
						}
					}
				}

			//}
			//catch { }

			Unlock();

			GCHandle handle = GCHandle.Alloc(data);
			using (UnsafePointer ptr = new UnsafePointer(handle, Marshal.UnsafeAddrOfPinnedArrayElement(data, 0)))
			{
				Bitmap bitmap = new Bitmap(srcWidth, srcHeight, sourceStride, PixelFormat.Format32bppArgb, ptr.IntPtr);
				Point pos = new Point((int)(drawX * scale), (int)(drawY * scale));
				Size size = new Size((int)(srcWidth * scale), (int)(srcHeight * scale));

				if (blend == SurfaceAnimationPattern.Patterns.Replace)
				{
					destGraphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
				}

				destGraphics.DrawImage(bitmap, new Rectangle(pos, size));
				if (blend == SurfaceAnimationPattern.Patterns.Replace)
				{
					//戻す
					destGraphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
				}
			}

			switch (blend)
			{
				case SurfaceAnimationPattern.Patterns.Interpolate:
				case SurfaceAnimationPattern.Patterns.Overlayfast:
				case SurfaceAnimationPattern.Patterns.Reduce:
					baseSurface.UnlockBits(elementData);
					break;
			}
			source.UnlockBits(sourceData);
			
		}
		



		public static Rectangle GetBitmapRect( Bitmap bitmap)
		{
			return new Rectangle(0, 0, bitmap.Width, bitmap.Height);
		}

		public class ColorARGB8
		{
			public byte[] mBytes;

			public ColorARGB8()
			{
				mBytes = null;
			}

			public void FromIntARGB8( int pixel )
			{
				mBytes = BitConverter.GetBytes(pixel);
				//Array.Reverse(mBytes);
			}

			public int ToInt()
			{
				return BitConverter.ToInt32(mBytes, 0);
			}

		}
		

	}
}
