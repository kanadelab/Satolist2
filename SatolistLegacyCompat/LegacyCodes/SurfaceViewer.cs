using SatolistLegacyCompat.CompatCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Satolist
{
	public partial class SurfaceViewer : Form
	{
		public Surfaces surfaces;
		//private List<PictureBox> layer;
		private Bitmap canvas;
		private Graphics graph;
		private Bitmap surfaceCanvas;
		private Graphics surfaceGraphics;
		public string directory;
		private RootWindow mRoot;
		//private Dictionary<String, Bitmap> mLoadedFiles;
		private Point mMouseDownPoint;
		private Point mMouseUpPoint;
		private List<Point> mMouseClickPoint;
		private SolidBrush mFillBrush = new SolidBrush(Color.FromArgb(200, Color.White));
		private Pen mUsedCollisionPen;
		private Point mMoveBeforeMouse;
		private bool mMoveKeyCtrl = false;
		private int mMoveIndex = -1;
		//private Point mExpandBeforeMouse;
		private Point mEnterPointLT;
		private Point mEnterPointRB;
		private int mCurrentSurfaceId = 0;
		private Dictionary<int, Dictionary<int, bool>> mBindChecked;
		private int mCurrentScopeId = 0;
		//private Dictionary<int, bool> mBindChecked;
		private float mScale = 1.0f;
		private Dictionary<int, AnimationStatus> mAnimationStatus = new Dictionary<int, AnimationStatus>();
		private List<AnimationTimer> mAnimationTimers = new List<AnimationTimer>();
		private Random mRand = new Random();
		private Object mSync = new object();
		private Object mLock = new object();
		private int mDefaultScope = 0;

		public SurfaceViewer(RootWindow root)
		{
			InitializeComponent();
			//layer = new List<PictureBox>();
			surfaces = new Surfaces();
			//mLoadedFiles = new Dictionary<string, Bitmap>();
			//surfaces.LoadSurfaces("");
			

			mUsedCollisionPen = new Pen(Color.DarkGray, 1);
			mUsedCollisionPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
			
			mRoot = root;
			pictureBox1.MouseWheel += pictureBox1_MouseWheel;

			mBindChecked = new Dictionary<int, Dictionary<int, bool>>();

			checkedListBox1.Visible = ProjectData.Instance.isShowSurfaceBind;
			splitter2.Visible = ProjectData.Instance.isShowSurfaceBind;
		
			UpdateOptionStatus();
			ToolStripMenuItem item = (ToolStripMenuItem)menuStrip1.Items["オプションToolStripMenuItem"];
			((ToolStripMenuItem)item.DropDownItems["さわり判定箇所の表示ToolStripMenuItem"]).Checked = ProjectData.Instance.isSurfaceShowCollision;
			

		}

		private void UpdateCanvasSize()
		{
			canvas = new Bitmap(ProjectData.Instance.surfaceViewerSizeX, ProjectData.Instance.surfaceViewerSizeY);
			surfaceCanvas = new Bitmap(ProjectData.Instance.surfaceViewerSizeX, ProjectData.Instance.surfaceViewerSizeY);

			graph = Graphics.FromImage(canvas);
			surfaceGraphics = Graphics.FromImage(surfaceCanvas);

			pictureBox1.Image = canvas;
		}


		void Init()
		{
			UpdateCanvasSize();

			//layer = new List<PictureBox>();
			

			textBox1.Clear();
			listBox1.Items.Clear();

			//mLoadedFiles.Clear();
			ResetLayer();

			foreach (KeyValuePair<int, SurfaceDescript> p in surfaces.surfaces)
			{
				if (!p.Value.isViewerVisible)
				{
					continue;
				}

				if (surfaces.surfaceTable.ContainsKey(p.Key))
				{
					listBox1.Items.Add(p.Key.ToString() + " [" + surfaces.surfaceTable[p.Key] + "]");
				}
				else
				{
					listBox1.Items.Add(p.Key.ToString());
				}
			}


			//このタイミングでサーフェスパレットの設定を行う
			mRoot.surfacePalette.UpdateData();
		}

		private void ResetTimers()
		{
			lock (mLock)
			{

				foreach (AnimationTimer t in mAnimationTimers)
				{
					t.Stop();
					t.Dispose();
				}
				mAnimationTimers.Clear();
				mAnimationStatus.Clear();
			}
		}

		private void CreateAnimationTimers()
		{
			if (!surfaces.surfaces.ContainsKey(mCurrentSurfaceId))
			{
				return;
			}

			lock (mLock)
			{


				//タイマーの作成
				foreach (KeyValuePair<int, SurfaceAnimation> p in surfaces.surfaces[mCurrentSurfaceId].animation)
				{
					//困った 複数インターバルだとどれを採用するのやら。
					//全部採用しなきゃいけないんだけどね。とりあえず、自動発生を全部計算して、最も早かったやつを採用するとかで一つ。
					//自動発生だからね。
					//↑　と思ったけどダメだった。２回めのアニメーションを考えると破綻している。
					//インターバルの数だけアニメーションを用意して、タイマー完了時にアニメーション再生中かどうかを判断し
					//再生中であれば無視、そうでなければアニメーションを開始するのがベスト。

					//アニメーションの数だけアニメーションステータスを作成。
					AnimationStatus st = new AnimationStatus();
					mAnimationStatus.Add(p.Key, st);
					st.mBindType = AnimationStatus.GetBindType(p.Value.interval);

					if (!ProjectData.Instance.isPlaySurfaceAnimation)
					{
						continue;
					}

					//intervalの数だけ必要に応じてタイマーを作成
					foreach (SurfaceAnimationInterval i in p.Value.interval)
					{
						//インターバルだけやる
						switch (i.interval)
						{
							case SurfaceAnimationInterval.Intervals.Always:
							case SurfaceAnimationInterval.Intervals.Random:
							case SurfaceAnimationInterval.Intervals.Rarely:
							case SurfaceAnimationInterval.Intervals.Runonce:
							case SurfaceAnimationInterval.Intervals.Sometimes:
							case SurfaceAnimationInterval.Intervals.Periodic:
								//自発アニメーション。	runonce含む。
								int time = GetTimerInterval(i);
								if (time > 0)
								{
									AnimationTimer at = new AnimationTimer();
									mAnimationTimers.Add(at);
									at.mTarget = st;
									at.mAnimationID = p.Key;
									at.mInterval = i;
									at.mAnimation = p.Value;
									at.Tick += SurfaceViewer_Tick;
									at.Interval = time;
									at.Start();
								}
								break;
						}
					}
				}
			}

		}

		private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			lock (mLock)
			{
				if (listBox1.SelectedItem == null)
					return;

				textBox1.Clear();
				textBox1.AppendText("テスト用の領域です。ユーザさんは気にしないでください。\r\n");
				

				//インデックスの作成
				//タイマーの解放
				ResetTimers();

				String ba = (String)listBox1.SelectedItem;
				int pp = ba.IndexOf(" ");
				if (pp != -1)
				{
					ba = ba.Substring(0, pp);
				}
				mCurrentSurfaceId = int.Parse(ba);
				int beforeScopeId = mCurrentScopeId;
				mCurrentScopeId = surfaces.surfaces[mCurrentSurfaceId].scope;

				if (mCurrentScopeId < 0 || !ProjectData.Instance.isUseSurfacesScope)
				{
					mCurrentScopeId = mDefaultScope;
				}
				//if (beforeScopeId != mCurrentScopeId)
				//{
					checkedListBox1.Items.Clear();
				//}

				//チェックボックスの作成
				foreach (KeyValuePair<int, SurfaceAnimation> p in surfaces.surfaces[mCurrentSurfaceId].animation)
				{
					//デバッグ出力
					/*
					debugOut += "animation" + p.Key + ".interval,";
					foreach (SurfaceAnimationInterval i in p.Value.interval)
					{
						debugOut += i.interval.ToString() + "," + i.value.ToString() + "_";
					}
					debugOut += "\r\n";
					foreach (KeyValuePair<int, SurfaceAnimationPattern> pa in p.Value.pattern)
					{
						debugOut += "pattern" + pa.Value.id.ToString() + ", blend:" + pa.Value.pattern.ToString() + " surface:" + pa.Value.surface.ToString() + " wait:" + pa.Value.wait.ToString()
							+ " pos:" + pa.Value.x + "," + pa.Value.y + "\r\n";
					}
					*/


					if (p.Value.GetIntervals(SurfaceAnimationInterval.Intervals.Bind).Count == 0)
					{
						continue;
					}

					BindDescript bd = null;
					if(!mBindChecked.ContainsKey(mCurrentScopeId))
					{
						mBindChecked.Add(mCurrentScopeId, new Dictionary<int, bool>());
					}
					/*
					if (beforeScopeId == mCurrentScopeId)
					{
						if (!mBindChecked[mCurrentScopeId].ContainsKey(p.Value.id))
						{
							if (p.Value.mBindData.ContainsKey(mCurrentScopeId))
							{
								bd = p.Value.mBindData[mCurrentScopeId];
								mBindChecked[mCurrentScopeId].Add(p.Value.id, bd.mIsDefault);
								String item = p.Value.id.ToString() + " " + bd.mName;
								checkedListBox1.Items.Add(item, mBindChecked[mCurrentScopeId][p.Value.id]);
							}
						}
					}
					else
					 * */
					{
						if (!mBindChecked[mCurrentScopeId].ContainsKey(p.Value.id))
						{
							if (p.Value.mBindData.ContainsKey(mCurrentScopeId))
							{
								bd = p.Value.mBindData[mCurrentScopeId];
								mBindChecked[mCurrentScopeId].Add(p.Value.id, bd.mIsDefault);
								String item = p.Value.id.ToString() + " " + bd.mName;
								checkedListBox1.Items.Add(item, mBindChecked[mCurrentScopeId][p.Value.id]);
							}
						}
						else
						{
							bd = p.Value.mBindData[mCurrentScopeId];
							String item = p.Value.id.ToString() + " " + bd.mName;
							checkedListBox1.Items.Add(item, mBindChecked[mCurrentScopeId][p.Value.id]);
						}
					}

					

				}
				
				CreateAnimationTimers();
				
				DrawImage();
			}
		}

		void SurfaceViewer_Tick(object sender, EventArgs e)
		{
			lock (mLock)
			{
				AnimationTimer timer = (AnimationTimer)sender;
				timer.Stop();
				Stopwatch sw = new Stopwatch();
				sw.Start();
				bool isTimerEnable = true;

				lock (timer.mTarget)
				{
					//タイマーの所持者がnullもしくは自分なら操作を行う
					if (timer.mTarget.mController == null || timer.mTarget.mController == timer)
					{
						if (timer.mTarget.mController == null)
						{
							//はじめてアタッチする場合に、再生インデックスをリセット
							timer.mTarget.mPlayingPatternIndex = -1;
						}

						timer.mTarget.mController = timer;
						//タイマーの再定義

						//次に使用するインデックスを探す
						int before = timer.mTarget.mPlayingPatternIndex;
						bool isFound = false;
						foreach( KeyValuePair<int, SurfaceAnimationPattern> pat in timer.mAnimation.pattern)
						{
							if (pat.Key > timer.mTarget.mPlayingPatternIndex)
							{
								timer.mTarget.mPlayingPatternIndex = pat.Key;
								isFound = true;
								break;
							}
						}
						if (!isFound)
						{
							timer.mTarget.mPlayingPatternIndex = before;
						}


						//インデックスが存在し、かつ前回と違うパターンに移行したならば、アニメーション再生中。
						if (timer.mAnimation.pattern.ContainsKey(timer.mTarget.mPlayingPatternIndex) && before != timer.mTarget.mPlayingPatternIndex)
						{
							if (timer.mAnimation.pattern[timer.mTarget.mPlayingPatternIndex].wait > 0)
							{
								timer.Interval = timer.mAnimation.pattern[timer.mTarget.mPlayingPatternIndex].wait;
							}
							else
							{
								timer.Interval = 1;
							}
						}
						else
						{
							//終了していたので最初から。
							//コントロールを開放
							timer.mTarget.mController = null;
							
							int time = -1;
							switch (timer.mInterval.interval)
							{
								case SurfaceAnimationInterval.Intervals.Sometimes:
								case SurfaceAnimationInterval.Intervals.Rarely:
								case SurfaceAnimationInterval.Intervals.Random:
								case SurfaceAnimationInterval.Intervals.Periodic:
								case SurfaceAnimationInterval.Intervals.Always:
									time = GetTimerInterval(timer.mInterval);
									if (time > 0)
									{
										timer.Interval = time;
									}
									break;
							}
							if (time <= 0)
							{
								//timeが０以下→もう実行しない
								lock (mAnimationTimers)
								{
									mAnimationTimers.Remove(timer);
									timer.Dispose();
									isTimerEnable = false;
								}
							}

						}

						//グラフィック
						DrawImage();
					}
				}

				if (isTimerEnable)
				{
					sw.Stop();
					int newTime = timer.Interval - (int)sw.ElapsedMilliseconds;
					if (newTime > 0)
					{
						timer.Interval = newTime;

					}
					else
					{
						timer.Interval = 1;
					}
					timer.Start();
				}
			}
		}

		private int GetTimerInterval( SurfaceAnimationInterval interval)
		{
			switch (interval.interval)
			{
				case SurfaceAnimationInterval.Intervals.Sometimes:
					return Sometimes(2);
				case SurfaceAnimationInterval.Intervals.Rarely:
					return Sometimes(4);
				case SurfaceAnimationInterval.Intervals.Random:
					return Sometimes(interval.value);
				case SurfaceAnimationInterval.Intervals.Periodic:
					return interval.value * 1000;
				case SurfaceAnimationInterval.Intervals.Always:
					return 1;
				case SurfaceAnimationInterval.Intervals.Runonce:
					return 1;

			}
			return -1;
		}

		private int Sometimes(int count)
		{
			int ret = 1000;
			while( mRand.Next(0, count) != 0)
			{
				ret += 1000;
			}
			return ret;
		}

		private void TakeScreenShot(Point pos, Size size)
		{
			lock(mSync)
			{
				ResetLayer();
				pictureBox1.Image = null;

				using (Graphics g = Graphics.FromImage(surfaceCanvas))
				{
					g.CopyFromScreen(pos, new Point(0, 0), size);
				}
				pictureBox1.Image = surfaceCanvas;
			}
		}

		private void DrawImage()
		{
			//surfaceGraphics.
			lock (mSync)
			{
				ResetLayer();
				pictureBox1.Image = null;

				int id = mCurrentSurfaceId;


				//ベースサーフェス

				//animation-base を喰らっていたらベースが変更になるので確認しておく
				int elementId = id;
				foreach (KeyValuePair<int, AnimationStatus> pat in mAnimationStatus)
				{
					if ( pat.Value.mBindType == AnimationStatus.BindType.NoBind && surfaces.surfaces[id].animation[pat.Key].pattern.ContainsKey(pat.Value.mPlayingPatternIndex))
					{
						SurfaceAnimationPattern anime = surfaces.surfaces[id].animation[pat.Key].pattern[pat.Value.mPlayingPatternIndex];
						if( anime.pattern == SurfaceAnimationPattern.Patterns.Base){
							//ここで変更が発生
							elementId = anime.surface;
						}
					}
				}

				//ここでbackgroundを書く。
				foreach (KeyValuePair<int, AnimationStatus> pat in mAnimationStatus)
				{
					bool judgePattern = false;
					bool judgeBind = false;

					if(surfaces.surfaces[id].animation[pat.Key].option.IndexOf("background") < 0 )
					{
						continue;
					}

					if (surfaces.surfaces[id].animation[pat.Key].pattern.ContainsKey(pat.Value.mPlayingPatternIndex))
					{
						judgePattern = true;
					}
					if (mBindChecked.ContainsKey(mCurrentScopeId))
					{
						if (mBindChecked[mCurrentScopeId].ContainsKey(pat.Key) && mBindChecked[mCurrentScopeId][pat.Key])
						{
							judgeBind = true;
						}
					}

					if (pat.Value.mBindType == AnimationStatus.BindType.BindOnly && judgeBind)
					{

						foreach (KeyValuePair<int, SurfaceAnimationPattern> ap in surfaces.surfaces[id].animation[pat.Key].pattern)
						{
							try//アニメーション定義に-1があれば[]解決失敗で落ちる
							{
								Bitmap b = GetPatternBitmap(id, pat.Key, ap.Key);
								if (b != null)
								{
									AddLayer(b, new Point(ap.Value.x, ap.Value.y), null, ap.Value.pattern);
								}
							}
							catch { }
						}

					}
					else if ((pat.Value.mBindType == AnimationStatus.BindType.NoBind && judgePattern) || (pat.Value.mBindType == AnimationStatus.BindType.BindPlus && judgePattern && judgeBind))
					{
						try
						{
							SurfaceAnimationPattern anime = surfaces.surfaces[id].animation[pat.Key].pattern[pat.Value.mPlayingPatternIndex];
							Bitmap b = GetPatternBitmap(id, pat.Key, pat.Value);
							if (b != null)
							{
								AddLayer(b, new Point(anime.x, anime.y), null, anime.pattern);
							}
						}
						catch
						{
						}
					}
					else if (!ProjectData.Instance.isPlaySurfaceAnimation && surfaces.surfaces[id].animation[pat.Key].pattern.ContainsKey(surfaces.surfaces[id].animation[pat.Key].mDefaultActive))
					{

						try
						{
							SurfaceAnimationPattern anime = surfaces.surfaces[id].animation[pat.Key].pattern[surfaces.surfaces[id].animation[pat.Key].mDefaultActive];
							Bitmap b = GetPatternBitmap(id, pat.Key, surfaces.surfaces[id].animation[pat.Key].mDefaultActive);
							if (b != null)
							{
								AddLayer(b, new Point(anime.x, anime.y), null, anime.pattern);
							}
						}
						catch
						{
						}
					}
				}		

				Bitmap element = null;
				try
				{
					if (surfaces.surfaceFile.ContainsKey(elementId))
					{
						element = surfaces.GetBitmap(directory + "/" + surfaces.surfaceFile[elementId].fileName, elementId);
					}
					else
					{
						element = surfaces.GetBitmap(null, elementId);
					
					}
					if (element != null)
					{
						AddLayer(element, new Point(0, 0), null, SurfaceAnimationPattern.Patterns.Overlay);
					}
				}
				catch { }

				//きせかえが影響する場合の可能性について
				//・着せ替えのみの影響を受ける　bind →チェックリストがはいってたらいい
				//・着せ替えとその他のアニメーションの影響を受ける bind+　→チェックリストに入っている　かつ　アニメーションタイミング中
				//・着せ替えの影響を受けない other　→アニメーションタイミング中
				//まずbindだけかどうかを判別したほうがいい
				//ステータスは再生の有無に関係なく必要だった
				foreach( KeyValuePair<int, AnimationStatus> pat in mAnimationStatus)
				{
					bool judgePattern = false;
					bool judgeBind = false;

					if (surfaces.surfaces[id].animation[pat.Key].option.IndexOf("background") >= 0)
					{
						continue;
					}

					if (surfaces.surfaces[id].animation[pat.Key].pattern.ContainsKey(pat.Value.mPlayingPatternIndex))
					{
						judgePattern = true;
					}
					if (mBindChecked.ContainsKey(mCurrentScopeId))
					{
						if (mBindChecked[mCurrentScopeId].ContainsKey(pat.Key) && mBindChecked[mCurrentScopeId][pat.Key])
						{
							judgeBind = true;
						}
					}

					if( pat.Value.mBindType == AnimationStatus.BindType.BindOnly && judgeBind ){

						foreach (KeyValuePair<int, SurfaceAnimationPattern> ap in surfaces.surfaces[id].animation[pat.Key].pattern)
						{
							try//アニメーション定義に-1があれば[]解決失敗で落ちる
							{
								Bitmap b = GetPatternBitmap(id, pat.Key, ap.Key);
								if (b != null)
								{
									AddLayer(b, new Point(ap.Value.x, ap.Value.y), element, ap.Value.pattern);
								}
							}
							catch { }
						}

					}
					else if ( (pat.Value.mBindType == AnimationStatus.BindType.NoBind && judgePattern ) || (pat.Value.mBindType == AnimationStatus.BindType.BindPlus && judgePattern && judgeBind))
					{
						try
						{
							SurfaceAnimationPattern anime = surfaces.surfaces[id].animation[pat.Key].pattern[pat.Value.mPlayingPatternIndex];
							Bitmap b = GetPatternBitmap(id, pat.Key, pat.Value);
							if (b != null)
							{
								AddLayer(b, new Point(anime.x, anime.y), element, anime.pattern);
							}
						}
						catch
						{
						}
					}
					else if( !ProjectData.Instance.isPlaySurfaceAnimation && surfaces.surfaces[id].animation[pat.Key].pattern.ContainsKey(surfaces.surfaces[id].animation[pat.Key].mDefaultActive) )
					{
						
						try
						{
							SurfaceAnimationPattern anime = surfaces.surfaces[id].animation[pat.Key].pattern[surfaces.surfaces[id].animation[pat.Key].mDefaultActive];
							Bitmap b = GetPatternBitmap(id, pat.Key, surfaces.surfaces[id].animation[pat.Key].mDefaultActive);
							if (b != null)
							{
								AddLayer(b, new Point(anime.x, anime.y), element, anime.pattern);
							}
						}
						catch
						{
						}
					}
				}		

				if (ProjectData.Instance.isSurfaceShowCollision)
				{
					DrawCollisions();
				}
				graph.DrawImage(surfaceCanvas, new Point(0, 0));
				pictureBox1.Image = canvas;
			}
		}

		public Rectangle ScaleRect(Rectangle rect)
		{
			return new Rectangle((int)(rect.X * mScale), (int)(rect.Y * mScale), (int)(rect.Width * mScale), (int)(rect.Height * mScale));
		}

		public static Point[] ScalePolygonPublic(Point[] point, float scale)
		{
			Point[] ret = new Point[point.Length];
			for (int i = 0; i < point.Length; i++)
			{
				ret[i] = new Point((int)(point[i].X * scale), (int)(point[i].Y * scale));
			}
			return ret;
		}

		public static Point[] ReScalePolygonPublic(Point[] point, float scale)
		{
			Point[] ret = new Point[point.Length];
			for (int i = 0; i < point.Length; i++)
			{
				ret[i] = new Point((int)(point[i].X / scale), (int)(point[i].Y / scale));
			}
			return ret;
		}

		public Point[] ScalePolygon(Point[] point)
		{
			return ScalePolygonPublic(point, mScale);
		}

		private void DrawCollisions()
		{
			if (listBox1.SelectedItem == null)
				return;
			String ba = (String)listBox1.SelectedItem;
			int pp = ba.IndexOf(" ");
			if (pp != -1)
			{
				ba = ba.Substring(0, pp);
			}
			int id = int.Parse(ba);
			
			foreach (SurfaceCollision col in surfaces.surfaces[id].collisions)
			{


				DrawCollisionItem(col);
			}

			//ここで表示する
			
			foreach (String p in checkedListBox1.Items)
			{
				String[] sp = (p).Split(' ');
				int animationId = int.Parse(sp[0]);

				if (!mBindChecked[mCurrentScopeId][animationId])
				{
					continue;
				}

				foreach ( SurfaceCollision pat in surfaces.surfaces[id].animation[animationId].collisions)
				{
					DrawCollisionItem(pat);

				}

			}
			

		}

		private void DrawCollisionItem( SurfaceCollision col )
		{
			SizeF size = surfaceGraphics.MeasureString(col.id, mRoot.surfacePalette.mUseFont);
			switch (col.colType)
			{
				case CollisionType.RECT:
					{
						Rectangle rect = ScaleRect(Rectangle.FromLTRB(col.points[0].X, col.points[0].Y, col.points[1].X, col.points[1].Y));
						surfaceGraphics.FillRectangle(mFillBrush, rect);
						surfaceGraphics.DrawRectangle(mUsedCollisionPen, rect);

						surfaceGraphics.DrawString(col.id, mRoot.surfacePalette.mUseFont, mRoot.surfacePalette.mUseBrush, col.center.X * mScale - size.Width * 0.5f * mScale, col.center.Y * mScale - size.Height * 0.5f * mScale);
					}
					break;
				case CollisionType.ELLIPSE:
					{
						Rectangle rect = ScaleRect(Rectangle.FromLTRB(col.points[0].X, col.points[0].Y, col.points[1].X, col.points[1].Y));
						surfaceGraphics.FillEllipse(mFillBrush, rect);
						surfaceGraphics.DrawEllipse(mUsedCollisionPen, rect);
						surfaceGraphics.DrawString(col.id, mRoot.surfacePalette.mUseFont, mRoot.surfacePalette.mUseBrush, col.center.X * mScale - size.Width * 0.5f * mScale, col.center.Y * mScale - size.Height * 0.5f * mScale);
					}
					break;
				case CollisionType.CIRCLE:
					{
						Rectangle rect = ScaleRect(new Rectangle(col.points[0].X - col.circleLength / 2, col.points[0].Y - col.circleLength / 2, col.circleLength, col.circleLength));
						surfaceGraphics.FillEllipse(mFillBrush, rect);
						surfaceGraphics.DrawEllipse(mUsedCollisionPen, rect);
						surfaceGraphics.DrawString(col.id, mRoot.surfacePalette.mUseFont, mRoot.surfacePalette.mUseBrush, col.center.X * mScale - size.Width * 0.5f * mScale, col.center.Y * mScale - size.Height * 0.5f * mScale);
					}
					break;
				case CollisionType.POLYGON:
					{
						surfaceGraphics.FillPolygon(mFillBrush, col.points);
						surfaceGraphics.DrawPolygon(mUsedCollisionPen, col.points);
						surfaceGraphics.DrawString(col.id, mRoot.surfacePalette.mUseFont, mRoot.surfacePalette.mUseBrush, col.center.X * mScale - size.Width * 0.5f * mScale, col.center.Y * mScale - size.Height * 0.5f * mScale);
					}

					break;

			}
		}

		private void ResetLayer()
		{
			lock (mLock)
			{
				//foreach (PictureBox p in layer)
				//{
				//	p.Dispose();
				//}

				//layer = new List<PictureBox>();
				//currentLayer = null;
				graph.Clear(Color.Transparent);
				surfaceGraphics.Clear(Color.Transparent);
			}
		}

		private Bitmap GetPatternBitmap( int id, int animeId, AnimationStatus stat )
		{
			return GetPatternBitmap(id, animeId, stat.mPlayingPatternIndex);

		}

		public Bitmap GetPatternBitmap(int id, int animeId, int patternId)
		{
			if (surfaces.surfaces[id].animation[animeId].pattern.ContainsKey(patternId))
			{
				SurfaceAnimationPattern anime = surfaces.surfaces[id].animation[animeId].pattern[patternId];
				if (surfaces.surfaceFile.ContainsKey(anime.surface))
				{
					Bitmap b = surfaces.GetBitmap(directory + "/" + surfaces.surfaceFile[anime.surface].fileName, anime.surface);
					return b;
				}
				else
				{
					Bitmap b = surfaces.GetBitmap(null, anime.surface);
					return b;
				}
			}
			else
			{
				return null;
			}
		}

		private void AddLayer(Bitmap bitmap, Point pos, Bitmap element, SurfaceAnimationPattern.Patterns blend)
		{
			//Point loc = pos;
			//loc = new Point((int)(loc.X * mScale), (int)(loc.Y * mScale));
			//Size size = new Size((int)(bitmap.Width * mScale), (int)(bitmap.Height * mScale));

			//surfaceGraphics.DrawImage(bitmap, new Rectangle(loc.X, loc.Y, size.Width, size.Height));
			/*
			if (pat.pattern == SurfaceAnimationPattern.Patterns.Base)
			{
				ResetLayer();
			}
			 */

			//合成メソッドメモ
			//base - 不明
			//overlay - 単純合成
			//overlayfast - elementのalphaがあるところに描画
			//replace - 完全に上書き（透明度関係なし）
			//

			BlendBitmap b = new BlendBitmap(surfaceCanvas);
			b.Draw(bitmap, pos.X, pos.Y, surfaceGraphics, mScale, blend, element);

		}

		private void SurfaceViewer_FormClosed(object sender, FormClosedEventArgs e)
		{
			if (graph != null)
				graph.Dispose();
			if (surfaceGraphics != null)
				surfaceGraphics.Dispose();
		}

		private void SurfaceViewer_DragDrop(object sender, DragEventArgs e)
		{
#if LEGACY
			mRoot.RootWindow_DragDrop(sender, e);
#endif
		}

		private void button1_Click(object sender, EventArgs e)
		{
			ResetTimers();
			//directory = textBox2.Text;
			LoadSurfaceInternal();
		}

		public void シェルを選択ToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		public void SelectDefaultShell()
		{
			ResetTimers();

			

			try
			{
				DirectoryInfo d = Directory.GetParent(mRoot.satori.Directory);

				//ゴーストルート
				d = d.Parent;

				if (Directory.Exists(d.FullName + "/" + "shell"))
				{
					String master = d.FullName + "/" + "shell/master";
					if (Directory.Exists(master))
					{
						//マスターシェルフォルダ
						directory = master;
						LoadSurfaceInternal();
					}
					else
					{
						//シェルフォルダが存在する
						String[] s = Directory.GetDirectories(d.FullName + "/" + "shell");
						if (s.Length > 0)
						{
							directory = s[0];
							LoadSurfaceInternal();
						}
					}
				}
				
			}
			catch
			{

			}
		
		
		}

		private void LoadSurfaceInternal()
		{
			bool isAnimeoff = false;
			if (ProjectData.Instance.isPlaySurfaceAnimation)
			{
				アニメーションを再生を有効にするToolStripMenuItem_Click(null, null);
				isAnimeoff = true;
			}
#if LEGACY
			if (GhostSettings.mCurrent != null)
			{
				GhostSettings.mCurrent.mLoadedShellFolder = Path.GetFileName(directory);
			}
#endif
			surfaces.LoadSurfaces(directory);
			
			Init();

			if( isAnimeoff)
			{
				アニメーションを再生を有効にするToolStripMenuItem_Click(null, null);
			}


			UpdateMenuEnable(Directory.Exists(directory));

			/*
			D3DTest.Initalize();
			D3DTest.AddSurfaceElement(0, 0, "overlay", surfaces.surfaces[0].autoElement, 0,0);

			D3DFrom form = new D3DFrom();
			form.Show();
			*/
		}


		private void ベータ版についてToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MessageBox.Show("現在、次の機能と制約があります。\r\n・左上透過のみで、pnaは使用しません。\r\n使用可能なアニメーション合成メソッド:\r\nbind. overlay, base, overlayfast, replace, interpolate, bind, add");
		}

		private void SurfaceViewer_FormClosing(object sender, FormClosingEventArgs e)
		{
			Hide();
			e.Cancel = true;
		}

		private void listBox1_DoubleClick(object sender, EventArgs e)
		{
			SurfaceDoubleClick((String)listBox1.SelectedItem);


		}

		public void SurfaceDoubleClick(string text)
		{
#if LEGACY
			if (!ProjectData.Instance.isUseSurfaceInsert)
				return;


			if (text == null)
				return;

			Sgry.Azuki.WinForms.AzukiControl target = mRoot.searchWindow.GetTarget();
			if (target == null)
				return;

			String surface;
			String[] sp = (text).Split(' ');
			surface = sp[0];

			if (ProjectData.Instance.isUseSurfaceInsertSatori)
			{
				target.Document.Replace("（" + Han2ZenNum(surface) + "）");
			}
			else
			{
				target.Document.Replace("\\s[" + surface + "]");
			}


			target.Select();//アクティブにする
#else
			if (text == null)
				return;

			string surface;
			string[] sp = (text).Split(' ');
			surface = sp[0];
			long surfaceId;
			if (long.TryParse(surface, out surfaceId))
			{
				ProjectCompat.RequestInsertChangeSurface(surfaceId);
			}
#endif
		}

		private void リストをダブルクリックでサーフェス変更を挿入ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ProjectData.Instance.isUseSurfaceInsert = !ProjectData.Instance.isUseSurfaceInsert;
			UpdateOptionStatus();
			mRoot.surfacePalette.UpdateOptionStatus();
		}

		private void 里々形式のサーフェス変更を使用するToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ProjectData.Instance.isUseSurfaceInsertSatori = !ProjectData.Instance.isUseSurfaceInsertSatori;
			UpdateOptionStatus();
			mRoot.surfacePalette.UpdateOptionStatus();
		}

		public static String Han2ZenNum(String num)
		{
			String r = num;
			r = r.Replace('0', '０');
			r = r.Replace('1', '１');
			r = r.Replace('2', '２');
			r = r.Replace('3', '３');
			r = r.Replace('4', '４');
			r = r.Replace('5', '５');
			r = r.Replace('6', '６');
			r = r.Replace('7', '７');
			r = r.Replace('8', '８');
			r = r.Replace('9', '９');

			return r;
		}

		public static String Zen2HanNum(String num)
		{
			String r = num;
			r = r.Replace('０', '0');
			r = r.Replace('１', '1');
			r = r.Replace('２', '2');
			r = r.Replace('３', '3');
			r = r.Replace('４', '4');
			r = r.Replace('５', '5');
			r = r.Replace('６', '6');
			r = r.Replace('７', '7');
			r = r.Replace('８', '8');
			r = r.Replace('９', '9');

			return r;
		}

		private void オプションToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//ToolStripMenuItem item = (ToolStripMenuItem)sender;
			//((ToolStripMenuItem)item.DropDownItems["リストをダブルクリックでサーフェス変更を挿入ToolStripMenuItem"]).Checked = ProjectData.Instance.isUseSurfaceInsert;
			//((ToolStripMenuItem)item.DropDownItems["里々形式のサーフェス変更を使用するToolStripMenuItem"]).Checked = ProjectData.Instance.isUseSurfaceInsertSatori;
		}

		public void UpdateOptionStatus()
		{
			ToolStripMenuItem item = (ToolStripMenuItem)menuStrip1.Items["オプションToolStripMenuItem"];
			((ToolStripMenuItem)item.DropDownItems["リストをダブルクリックでサーフェス変更を挿入ToolStripMenuItem"]).Checked = ProjectData.Instance.isUseSurfaceInsert;
			((ToolStripMenuItem)item.DropDownItems["里々形式のサーフェス変更を使用するToolStripMenuItem"]).Checked = ProjectData.Instance.isUseSurfaceInsertSatori;
			((ToolStripMenuItem)item.DropDownItems["定義のないサーフェスも表示するリロードが必要ToolStripMenuItem"]).Checked = ProjectData.Instance.isSurfaceUseNoSetting;
			((ToolStripMenuItem)item.DropDownItems["着せ替えリストを表示ToolStripMenuItem"]).Checked = ProjectData.Instance.isShowSurfaceBind;
			((ToolStripMenuItem)item.DropDownItems["アニメーションを再生を有効にするToolStripMenuItem"]).Checked = ProjectData.Instance.isPlaySurfaceAnimation;

			splitter2.Visible = ProjectData.Instance.isShowSurfaceBind;
			checkedListBox1.Visible = ProjectData.Instance.isShowSurfaceBind;

			ToolStripMenuItem scope = ((ToolStripMenuItem)item.DropDownItems["スコープToolStripMenuItem"]);
			((ToolStripMenuItem)scope.DropDownItems["surfacestxtによるスコープ指定を優先ToolStripMenuItem"]).Checked = ProjectData.Instance.isUseSurfacesScope;

		}

		public void シェルを読み込むToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ShellSelectDialog sd = new ShellSelectDialog(mRoot.satori?.Directory);
			sd.ShowDialog(this);

			if (sd.mIsEnter && sd.GetDir() != null)
			{
				//ResetTimers();
				directory = sd.GetDir();
				bool enable_directory = Directory.Exists(directory);
				if (enable_directory)
				{
					LoadSurfaceInternal();
				}
				else
				{
					System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, "フォルダがみつかりません。", "エラー");
				}
				



			}
			
		}

		public void UpdateMenuEnable(bool enable_directory)
		{
			{
				ToolStripMenuItem item = (ToolStripMenuItem)menuStrip1.Items["ツールToolStripMenuItem"];
				item.DropDownItems["更新ファイル作成ToolStripMenuItem"].Enabled = enable_directory;
				item.DropDownItems["narファイル作成ToolStripMenuItem"].Enabled = enable_directory;
				item.DropDownItems["更新ファイルnar作成ToolStripMenuItem"].Enabled = enable_directory;
				item.DropDownItems["更新ファイルを作成してアップロードToolStripMenuItem"].Enabled = enable_directory;
			}

			{
				ToolStripMenuItem item = (ToolStripMenuItem)menuStrip1.Items["シェルを選択ToolStripMenuItem"];
				item.DropDownItems["シェルをエクスプローラで開くToolStripMenuItem"].Enabled = enable_directory;

			}

			mRoot.surfacePalette.UpdateMenuEnable(enable_directory);
		}

		public void シェルをリロードToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//ResetTimers();
			LoadSurfaceInternal();
		}

		public void LoadShell(String dir)
		{
			DirectoryInfo d = Directory.GetParent(mRoot.satori.Directory);

				//ゴーストルート
				d = d.Parent;

				if (File.Exists(d.FullName + "/" + "shell/" + dir + "/surfaces.txt"))
				{
					try
					{
						directory = d.FullName + "/" + "shell/" + dir;
	
						LoadSurfaceInternal();
					}
					catch
					{
						SelectDefaultShell();
					}
				}
				else
				{
					SelectDefaultShell();
				}
		}

		private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
		{
#if LEGACY
			try
			{
				mMoveKeyCtrl = false;
				if (e.Button == System.Windows.Forms.MouseButtons.Left)
				{
					if (mRoot.collision.GetCollisionType() != CollisionType.POLYGON && mRoot.collision.GetCollisionType() != CollisionType.DISABLED)
					{
						mMouseDownPoint = e.Location;
					}
					mMoveBeforeMouse = e.Location;

					if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
					{
						mMoveKeyCtrl = true;

						//アンカーポイントピックアップ
						if (mRoot.collision.GetCollisionType() == CollisionType.POLYGON)
						{
							int target_index = -1;
							float dist = 100.0f;	//最低条件
							for (int i = 0; i < mMouseClickPoint.Count; i++)
							{
								//設定されているアンカーポイントから一番近いやつを参照する
								Point p = mMouseClickPoint[i];
								float d = (p.X - e.Location.X) * (p.X - e.Location.X) + (p.Y - e.Location.Y) * (p.Y - e.Location.Y);
								if (d < dist)
								{
									target_index = i;
									dist = d;
								}
							}
							mMoveIndex = target_index;
						}
					}
					
				}

				
			}
			catch { ResetCollisions(); }
#endif
		}

		private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
		{
#if LEGACY
			try
			{
				if (e.Button == System.Windows.Forms.MouseButtons.Left)
				{
					if (mRoot.collision.GetCollisionType() != CollisionType.POLYGON && mRoot.collision.GetCollisionType() != CollisionType.DISABLED)
					{

						mMouseUpPoint = e.Location;
						UpdateCollision();


					}
					else if (mRoot.collision.GetCollisionType() == CollisionType.POLYGON && mMoveKeyCtrl && mMoveIndex != -1)
					{
						mMouseClickPoint[mMoveIndex] = e.Location;
						UpdatePolygonCollision();
					}
				}
				else if (e.Button == System.Windows.Forms.MouseButtons.Right)
				{
					if (mRoot.collision.GetCollisionType() != CollisionType.POLYGON && mRoot.collision.GetCollisionType() != CollisionType.DISABLED)
					{
						if (mMouseDownPoint != null && mMouseUpPoint != null)
						{
							mEnterPointLT = new Point(mEnterPointLT.X - mMoveBeforeMouse.X + e.Location.X, mEnterPointLT.Y - mMoveBeforeMouse.Y + e.Location.Y);
							mEnterPointRB = new Point(mEnterPointRB.X - mMoveBeforeMouse.X + e.Location.X, mEnterPointRB.Y - mMoveBeforeMouse.Y + e.Location.Y);
							UpdateEnterCollision();
						}
					}
					
				}
			
			}
			catch { ResetCollisions(); }

			//リセットしとく
			mMoveKeyCtrl = false;
#endif
		}

		private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
		{
#if LEGACY
			try
			{
                mRoot.collision.UpdateMouseLocation(e.Location);

				if (e.Button == System.Windows.Forms.MouseButtons.Left)
				{
					if (mRoot.collision.GetCollisionType() != CollisionType.POLYGON && mRoot.collision.GetCollisionType() != CollisionType.DISABLED)
					{

						mMouseUpPoint = e.Location;
						UpdateCollision();
					}
					else if (mRoot.collision.GetCollisionType() == CollisionType.POLYGON && mMoveKeyCtrl && mMoveIndex != -1)
					{
						mMouseClickPoint[mMoveIndex] = e.Location;
						UpdatePolygonCollision();
					}
				}
				else if (e.Button == System.Windows.Forms.MouseButtons.Right)
				{
					if (mRoot.collision.GetCollisionType() != CollisionType.POLYGON && mRoot.collision.GetCollisionType() != CollisionType.DISABLED)
					{
						if (mMouseDownPoint != null && mMouseUpPoint != null)
						{
							mEnterPointLT = new Point(mEnterPointLT.X - mMoveBeforeMouse.X + e.Location.X, mEnterPointLT.Y - mMoveBeforeMouse.Y + e.Location.Y);
							mEnterPointRB = new Point(mEnterPointRB.X - mMoveBeforeMouse.X + e.Location.X, mEnterPointRB.Y - mMoveBeforeMouse.Y + e.Location.Y);
							mMoveBeforeMouse = e.Location;
							UpdateEnterCollision();
						}
					}
				}

                
			
			}
			catch { ResetCollisions(); }
#endif
		}

		private void pictureBox1_MouseWheel(object sender, MouseEventArgs e)
		{
			/*
			Point backUp1 = mEnterPointLT;
			Point backUp2 = mEnterPointRB;

			const float moveScale = 0.03f;
			mEnterPointLT.X = mEnterPointLT.X + (int)(moveScale * e.Delta);
			mEnterPointLT.Y = mEnterPointLT.Y + (int)(moveScale * e.Delta);
			mEnterPointRB.X = mEnterPointRB.X - (int)(moveScale * e.Delta);
			mEnterPointRB.Y = mEnterPointRB.Y - (int)(moveScale * e.Delta);

			if (backUp1.X >= backUp2.X || backUp1.Y >= backUp2.Y)
			{
				mEnterPointLT = backUp1;
				mEnterPointRB = backUp2;
			}
			else
			{

				UpdateEnterCollision();
			}
			 * */
		}

		private void UpdateCollision()
		{
#if LEGACY
			bool isXYeven = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
			bool isCenter = (Control.ModifierKeys & Keys.Alt) == Keys.Alt;

			if (mRoot.collision.GetCollisionType() == CollisionType.CIRCLE)
			{
				isXYeven = true;
			}

			graph.Clear(Color.Transparent);
			graph.DrawImage(surfaceCanvas, new Point(0, 0));

			Rectangle drawRect;
			switch (mRoot.collision.GetCollisionType())
			{
				case CollisionType.RECT:
					drawRect = GetRectangle(mMouseDownPoint, mMouseUpPoint, isCenter, isXYeven);
					graph.FillRectangle(mFillBrush, drawRect);
					graph.DrawRectangle(Pens.Black, drawRect);

					break;
				case CollisionType.ELLIPSE:
					drawRect = GetRectangle(mMouseDownPoint, mMouseUpPoint, isCenter, isXYeven);
					graph.FillEllipse(mFillBrush, drawRect);
					graph.DrawRectangle(Pens.Black, drawRect);
					graph.DrawEllipse(Pens.Black, drawRect);

					break;
				case CollisionType.CIRCLE:
					drawRect = GetRectangle(mMouseDownPoint, mMouseUpPoint, isCenter, isXYeven);
					graph.FillEllipse(mFillBrush, drawRect);
					graph.DrawRectangle(Pens.Black, drawRect);
					graph.DrawEllipse(Pens.Black, drawRect);

					break;
			}

			pictureBox1.Image = canvas;
			Point[] rp = GetRectanglePoints(mMouseDownPoint, mMouseUpPoint, isCenter, isXYeven);
			mRoot.collision.SetPoints(rp,mScale);
			mEnterPointLT = rp[0];
			mEnterPointRB = rp[1];
#endif
		}

		private void UpdateEnterCollision()
		{
#if LEGACY
			bool isXYeven = false;
			bool isCenter = false;

			if (mRoot.collision.GetCollisionType() == CollisionType.CIRCLE)
			{
				isXYeven = true;
			}

			graph.Clear(Color.Transparent);
			graph.DrawImage(surfaceCanvas, new Point(0, 0));

			Rectangle drawRect;
			switch (mRoot.collision.GetCollisionType())
			{
				case CollisionType.RECT:
					drawRect = GetRectangle(mEnterPointLT, mEnterPointRB, isCenter, isXYeven);
					graph.FillRectangle(mFillBrush, drawRect);
					graph.DrawRectangle(Pens.Black, drawRect);

					break;
				case CollisionType.ELLIPSE:
					drawRect = GetRectangle(mEnterPointLT, mEnterPointRB, isCenter, isXYeven);
					graph.FillEllipse(mFillBrush, drawRect);
					graph.DrawRectangle(Pens.Black, drawRect);
					graph.DrawEllipse(Pens.Black, drawRect);

					break;
				case CollisionType.CIRCLE:
					drawRect = GetRectangle(mEnterPointLT, mEnterPointRB, isCenter, isXYeven);
					graph.FillEllipse(mFillBrush, drawRect);
					graph.DrawRectangle(Pens.Black, drawRect);
					graph.DrawEllipse(Pens.Black, drawRect);

					break;
			}

			pictureBox1.Image = canvas;
			Point[] rp = GetRectanglePoints(mEnterPointLT, mEnterPointRB, isCenter, isXYeven);
			mRoot.collision.SetPoints(rp,mScale);
			mEnterPointLT = rp[0];
			mEnterPointRB = rp[1];
#endif
		}

		private void UpdatePolygonCollision()
		{
#if LEGACY
			graph.Clear(Color.Transparent);
			graph.DrawImage(surfaceCanvas, new Point(0, 0));

			if( mMouseClickPoint == null || mMouseClickPoint.Count == 0)
			{
				pictureBox1.Image = canvas;
				return;
			}

			Point[] polygonArray = mMouseClickPoint.ToArray();
			if (mMouseClickPoint.Count == 1)
			{
				graph.DrawRectangle(Pens.Black, new Rectangle(mMouseClickPoint[0].X, mMouseClickPoint[0].Y, 1, 1));
			}
			else if (mMouseClickPoint.Count == 2)
			{
				graph.DrawLine(Pens.Black, mMouseClickPoint[0], mMouseClickPoint[1]);
			}
			else if( mMouseClickPoint.Count > 2)
			{
				graph.FillPolygon(mFillBrush, polygonArray);
				graph.DrawPolygon(Pens.Gray, polygonArray);
				graph.DrawLine(Pens.Red, mMouseClickPoint[0], mMouseClickPoint[mMouseClickPoint.Count - 1]);

			}

			//アンカーポイントの描画
			foreach (Point p in polygonArray)
			{
				graph.FillRectangle(Brushes.Black, new Rectangle(p.X - 2, p.Y - 2, 5, 5));
			}
			pictureBox1.Image = canvas;

			mRoot.collision.SetPoints(polygonArray,mScale);
#endif
		}

		public static Rectangle GetRectangle(Point p1, Point p2, bool isCenterMode, bool isXYeven)
		{
			Point[] rp = GetRectanglePoints(p1, p2, isCenterMode, isXYeven);
			return Rectangle.FromLTRB(rp[0].X, rp[0].Y, rp[1].X, rp[1].Y);

		}

		public static Point[] GetRectanglePoints(Point p1, Point p2, bool isCenterMode, bool isXYeven)
		{
			if (!isCenterMode && !isXYeven)
			{
				return new Point[]{
				new Point(p1.X < p2.X ? p1.X : p2.X, p1.Y < p2.Y ? p1.Y : p2.Y),
				new Point(p1.X < p2.X ? p2.X : p1.X, p1.Y < p2.Y ? p2.Y : p1.Y)
			};
			}
			else
			{

				if (isXYeven && isCenterMode)
				{
					Point[] rp = { p1, p1 };
					Point dist = new Point(p2.X - p1.X, p2.Y - p1.Y);
					Point center = p1;

					int offset = Math.Abs(dist.X) < Math.Abs(dist.Y) ? dist.X : dist.Y;

					rp[0].Offset(-offset / 2, -offset / 2);
					rp[1].Offset(offset / 2, offset / 2);

					



					return rp;

				}
				else if (isXYeven)
				{
					Point[] rp = GetRectanglePoints(p1, p2, false, false);
					Point dist = new Point(p2.X - p1.X, p2.Y - p1.Y);

					Point pex = p1;
					int offset = Math.Abs(dist.X) < Math.Abs(dist.Y) ? dist.X : dist.Y;
					p1.Offset(offset, offset);
					return GetRectanglePoints(p1, pex, isCenterMode, false);
				}
				else
				{
					Point[] rp = GetRectanglePoints(p1, p2, false, false);
					Point dist = new Point(p2.X - p1.X, p2.Y - p1.Y);
					rp[0].Offset(-dist.X / 2, -dist.Y / 2);
					rp[1].Offset(-dist.X / 2, -dist.Y / 2);
					return rp;
				}

			}


		}

		private void pictureBox1_Click(object sender, EventArgs e)
		{
			//pictureBox1.Focus();

		}

		private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
		{
#if LEGACY
			try
			{
				if (e.Button == System.Windows.Forms.MouseButtons.Left && !mMoveKeyCtrl)
				{
					//クリックされた
					if (mRoot.collision.GetCollisionType() == CollisionType.POLYGON)
					{
						//なかったら作るぞ
						if (mMouseClickPoint == null)
						{
							mMouseClickPoint = new List<Point>();
						}

						//クリックポイントを追加
						mMouseClickPoint.Add(e.Location);
						UpdatePolygonCollision();



					}
				}
				else if (e.Button == System.Windows.Forms.MouseButtons.Right )
				{
					//アンカーポイントをひとつ削除
					//クリックされた
					if (mRoot.collision.GetCollisionType() == CollisionType.POLYGON)
					{
						if (mMouseClickPoint != null)
						{
							if (mMouseClickPoint.Count > 0)
							{
								mMouseClickPoint.RemoveAt(mMouseClickPoint.Count - 1);
							}

							UpdatePolygonCollision();
						}


					}
					

				}
				else if (e.Button == System.Windows.Forms.MouseButtons.Middle)
				{
					if (mRoot.collision.GetCollisionType() == CollisionType.POLYGON)
					{
						if (mMouseClickPoint != null)
						{
							mMouseClickPoint.Clear();

							UpdatePolygonCollision();
						}


					}
					ResetCollisions();
				}
			}
			catch { ResetCollisions(); }

#endif
		}

		public void ResetCollisions()
		{
#if LEGACY
			if (mMouseClickPoint != null)
			{
				mMouseClickPoint.Clear();
			}

			if (graph != null)
			{
				graph.Clear(Color.Transparent);
				graph.DrawImage(surfaceCanvas, new Point(0, 0));
			}
			pictureBox1.Image = canvas;
#endif

		}
		private void pictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
		{
#if LEGACY
			try
			{
				if (e.Button == System.Windows.Forms.MouseButtons.Right)
				{
					//アンカーポイントをひとつ削除
					//クリックされた
					if (mRoot.collision.GetCollisionType() == CollisionType.POLYGON)
					{
						if (mMouseClickPoint != null)
						{
							if (mMouseClickPoint.Count > 0)
							{
								mMouseClickPoint.RemoveAt(mMouseClickPoint.Count - 1);
							}

							UpdatePolygonCollision();
						}


					}

				}
			}
			catch { ResetCollisions(); }
#endif
		}

		private void SurfaceViewer_KeyDown(object sender, KeyEventArgs e)
		{
#if LEGACY
			/*
			int moveX = 0;
			int moveY = 0;
			if (e.KeyCode == Keys.W)
			{

				moveY = -GetMove(e);
				MoveCollision(moveX, moveY);
			}
			else if( e.KeyCode == Keys.S)
			{
				moveY = GetMove(e);
				MoveCollision(moveX, moveY);
			}
			else if (e.KeyCode == Keys.D)
			{
				moveX = GetMove(e);
				MoveCollision(moveX, moveY);
			}
			else if (e.KeyCode == Keys.A)
			{
				moveX = -GetMove(e);
				MoveCollision(moveX, moveY);
			}
			 * */
#endif
		}

		private int GetMove(KeyEventArgs e)
		{
#if LEGACY
			if( e.Modifiers == Keys.Shift)
			{
				return 10;
			}
			else if( e.Modifiers == Keys.Alt)
			{
				return 1;

			}
			else
			{
				return 3;
			}
#else
			return 1;
#endif
		}

		private void MoveCollision(int x, int y)
		{
#if LEGACY
			if (mMouseDownPoint != null)
			{
				mMouseDownPoint.Offset(x, y);
			}

			if (mMouseUpPoint != null)
			{
				mMouseUpPoint.Offset(x, y);
			}

			if (mMouseClickPoint != null)
			{
				foreach (Point p in mMouseClickPoint)
				{
					p.Offset(x, y);
				}
			}

			CollisionType cType = mRoot.collision.GetCollisionType();
			if (cType == CollisionType.DISABLED)
			{

			}
			else if (cType == CollisionType.POLYGON)
			{
				UpdatePolygonCollision();
			}
			else
			{
				UpdateCollision();
			}
#endif
		}

		private void シェルのさわり判定のみ再読み込みToolStripMenuItem_Click(object sender, EventArgs e)
		{
			surfaces.LoadSurfaceCollisionOnly(directory);
			listBox1_SelectedIndexChanged(null, null);
			
			//ResetCollisions();
		}

		private void さわり判定箇所の表示ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ProjectData.Instance.isSurfaceShowCollision = !ProjectData.Instance.isSurfaceShowCollision;
			ToolStripMenuItem item = (ToolStripMenuItem)menuStrip1.Items["オプションToolStripMenuItem"];
			((ToolStripMenuItem)item.DropDownItems["さわり判定箇所の表示ToolStripMenuItem"]).Checked = ProjectData.Instance.isSurfaceShowCollision;
			listBox1_SelectedIndexChanged(null, null);
		}

		private void 定義のないサーフェスも表示するリロードが必要ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ProjectData.Instance.isSurfaceUseNoSetting = !ProjectData.Instance.isSurfaceUseNoSetting;
			UpdateOptionStatus();
			mRoot.surfacePalette.UpdateOptionStatus();
			
		}

		private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			lock (mLock)
			{
				//listBox1_SelectedIndexChanged(null, null);
				String item = (String)checkedListBox1.Items[e.Index];
				String[] sp = item.Split(' ');
				int id = int.Parse(sp[0]);

				mBindChecked[mCurrentScopeId][id] = e.NewValue == CheckState.Checked;

				DrawImage();
			}
		}

		private void checkedListBox1_Click(object sender, EventArgs e)
		{
			
		}

		private void 着せ替えリストを表示ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ProjectData.Instance.isShowSurfaceBind = !ProjectData.Instance.isShowSurfaceBind;
			UpdateOptionStatus();
			
			//mRoot.surfacePalette.UpdateOptionStatus();
		}

		private void surfacestxtの座標倍率を変化ToolStripMenuItem_Click(object sender, EventArgs e)
		{
#if LEGACY
			SurfaceMultiplier sm = new SurfaceMultiplier(mRoot, surfaces.mFilePath);
			sm.Show(this);
#endif
		}

		private void x01ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//スケーリング　クリック
			ToolStripMenuItem item = (ToolStripMenuItem)sender;

			foreach (ToolStripMenuItem loop in ((ToolStripMenuItem)((ToolStripMenuItem)menuStrip1.Items["オプションToolStripMenuItem"]).DropDownItems["表示倍率ToolStripMenuItem"]).DropDownItems)
			{
				loop.Checked = false;
			}

			item.Checked = true;
			mScale = float.Parse(item.Text.Substring(1));

			if (graph != null)
			{
				DrawImage();
			}
		}

		private void 読み込みログToolStripMenuItem_Click(object sender, EventArgs e)
		{
#if LEGACY
			mRoot.AddTextEditor(surfaces.mLastLoadInfomation, "シェル読み込みログ");
#endif
		}

		private void アニメーションを再生を有効にするToolStripMenuItem_Click(object sender, EventArgs e)
		{
			lock (mLock)
			{
				ProjectData.Instance.isPlaySurfaceAnimation = !ProjectData.Instance.isPlaySurfaceAnimation;

				ResetTimers();
				CreateAnimationTimers();
				UpdateOptionStatus();
				DrawImage();
			}
		}

		private void surfacestxtによるスコープ指定を優先ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ProjectData.Instance.isUseSurfacesScope = !ProjectData.Instance.isUseSurfacesScope;
			UpdateOptionStatus();
			listBox1_SelectedIndexChanged(listBox1, null);
		}

		private void sakuraToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//スケーリング　クリック
			ToolStripMenuItem item = (ToolStripMenuItem)sender;

			for (int i = 0; i < 10; i ++ )
			{
				((ToolStripMenuItem)((ToolStripMenuItem)((ToolStripMenuItem)menuStrip1.Items["オプションToolStripMenuItem"]).DropDownItems["スコープToolStripMenuItem"]).DropDownItems[i]).Checked = false;
			}

			item.Checked = true;
			mDefaultScope = int.Parse(item.Text.Substring(0, 1));

			listBox1_SelectedIndexChanged(listBox1, null);
		}

		private void 描画領域の調整ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			int x = ProjectData.Instance.surfaceViewerSizeX;
			int y = ProjectData.Instance.surfaceViewerSizeY;

			SurfaceVirewerSizeDialog dialog = new SurfaceVirewerSizeDialog(x,y);
			if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
			{
				ProjectData.Instance.surfaceViewerSizeX = dialog.mX;
				ProjectData.Instance.surfaceViewerSizeY = dialog.mY;

				UpdateCanvasSize();
				listBox1_SelectedIndexChanged(null, null);
			}

		}

		private void sERIKO定義を旧定義から新定義に置換ToolStripMenuItem_Click(object sender, EventArgs e)
		{
#if LEGACY
			String textAll = "";

			if (File.Exists(surfaces.mFilePath))
			{
				//StreamReader read = new StreamReader(surfaces.mFilePath, Encoding.GetEncoding("Shift_JIS"));
				textAll = File.ReadAllText(surfaces.mFilePath, Encoding.GetEncoding("Shift_JIS"));
			}
			else
			{
				//ない
				MessageBox.Show("サーフェスビューワが開いているシェルフォルダ内に、surfaces.txt が見つかりませんでした。");
				return;
			}

			
			String SYMBOL_TENFOLD = "// ** SERIKO1TO2 ZERO ** //";
			String SYMBOL_ERROR      = "// ** SERIKO1TO2 REPLACE ERROR ** //";

			StringReader sr = new StringReader(textAll);
			String outText = "";
			bool isError = false;

			while (true)
			{
				String inText = sr.ReadLine();
				if (inText == null)
				{
					break;
				}

				//intervalおよびoptionの置換
				inText = Regex.Replace(inText, "^([ \\t]*)(\\d+)(interval|option),(.+)$", "$1animation$2.$3,$4");

				//合成メソッドmoveのpattern指定の置換
				inText = Regex.Replace(inText, "^([ \\t]*)(\\d+)pattern(\\d+),(\\s*\\-?\\d+\\s*)?,(\\s?\\d+\\-?\\d*\\s*)?,move(,\\s*\\-?\\d*\\s*)?(,\\s*\\-?\\d*\\s*)?$", "$1animation$2.pattern$3,move,$4,$5" + SYMBOL_TENFOLD + "$6$7");

				//合成メソッドstartおよびstopのpattern指定の置換
				inText = Regex.Replace(inText, "^([ \\t]*)(\\d+)pattern(\\d+),(\\s*\\-?\\d+\\s*)?,(\\s?\\d+\\-?\\d*\\s*)?,(start|stop),\\[?(\\d+)\\]?$", "$1animation$2.pattern$3,$6,[$7]");

				//合成メソッドalternativestartおよびalternativestopのpattern指定の置換
				inText = Regex.Replace(inText, "^([ \\t]*)(\\d+)pattern(\\d+),(\\s*\\-?\\d+\\s*)?,(\\s?\\d+\\-?\\d*\\s*)?,alternative(start|stop),[\\[\\(]((\\d+)([.,]\\d+)*)[\\]\\)]$", "$1animation$2.pattern$3,alternative$6,($7)");

				//その他のpattern置換
				inText = Regex.Replace(inText, "^([ \\t]*)(\\d+)pattern(\\d+),(\\s*\\-?\\d+\\s*),(\\s?\\d+\\-?\\d*\\s*)?,(base|overlay|overlayfast|replace|interpolate|asis|bind|add|reduce|insert)(,\\s*\\-?\\d*\\s*)?(,\\s*\\-?\\d*\\s*)?$", "$1animation$2.pattern$3,$6,$4,$5" + SYMBOL_TENFOLD + "$7$8");

				// ウェイト10倍処理の後始末
				inText = inText.Replace(SYMBOL_TENFOLD, "0");

				//不明なpattern指定への印付け
				String chErr = inText;
				inText = Regex.Replace(inText, "^[ \\t]*\\d*pattern\\d*,.+$", SYMBOL_ERROR + "$&");

				outText += inText + "\r\n";

				if (chErr != inText)
				{
					isError = true;
				}

			}

			if( isError)
			{
				//エラーが発生している
				MessageBox.Show("変換を行いましたが、エラーが発生している箇所があります。\r\nエラーを確認して、descriptブレスを書き入れて新定義への移行を完了してください。");
				mRoot.AddTextEditor( "//---　変換エラーが発生しています。変換エラーの箇所は、\r\n\r\n" + SYMBOL_ERROR + "\r\n\r\n//---　↑でマークされています。\r\n\r\n" + outText, "surfaces.txt（新定義変換）");
			}
			else
			{
				//問題なし
				MessageBox.Show("変換を行いました。\r\ndescriptブレスを書き入れて新定義への移行を完了してください。");
				mRoot.AddTextEditor(outText, "surfaces.txt（新定義変換）");
			}

			

#endif
		}

		public void 更新ファイル作成ToolStripMenuItem_Click(object sender, EventArgs e)
		{
#if LEGACY
			if (Directory.Exists(directory))
			{
				if (mRoot.CreateUpdateWarning())
				{
					CommonProgressDialog d = new CommonProgressDialog("更新ファイルの作成", "作成しています。", "完了しました。", "エラーが発生したため失敗しました。", CreateUpdateVoid);
					d.ShowDialog(this);
				}
			}
#endif
		}
		public void CreateUpdateVoid()
		{
#if LEGACY
			EasyDeveloperOptionsAnalyzer opt = new EasyDeveloperOptionsAnalyzer(directory + "/developer_options.txt", directory + "/delete.txt");
			mRoot.CreateUpdate(UploadWizard.UPLOAD_TYPE.SHELL, opt);
#endif
		}

		public void narファイル作成ToolStripMenuItem_Click(object sender, EventArgs e)
		{
#if LEGACY
			if (Directory.Exists(directory))
			{
				if (mRoot.CreateUpdateWarning())
				{
					if (!String.IsNullOrWhiteSpace(GhostSettings.mCurrent.mUsedNarFolder) && Directory.Exists(Directory.GetParent(GhostSettings.mCurrent.mUsedNarFolder).FullName))
					{
						mRoot.narSaveFileDialog.InitialDirectory = Directory.GetParent(GhostSettings.mCurrent.mUsedNarFolder).FullName;
					}
					else
					{
						mRoot.narSaveFileDialog.InitialDirectory = directory;
					}

					mRoot.narSaveFileDialog.FileName = Path.GetFileName(GhostSettings.mCurrent.mUsedNarFolder);

					if (mRoot.narSaveFileDialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
					{
						CommonProgressDialog d = new CommonProgressDialog("narファイルの作成", "作成しています。", "完了しました。", "エラーが発生したため失敗しました。", CallCreateNarFromDialog);
						d.ShowDialog(this);
					}
				}
			}
#endif
		}

		public void CallCreateNarFromDialog()
		{
#if LEGACY
			EasyDeveloperOptionsAnalyzer opt = new EasyDeveloperOptionsAnalyzer(directory + "/developer_options.txt", directory + "/delete.txt");
			GhostSettings.mCurrent.mUsedNarFolder = mRoot.narSaveFileDialog.FileName;
			mRoot.CreateNar(mRoot.narSaveFileDialog.FileName, UploadWizard.UPLOAD_TYPE.SHELL, opt);
#endif
		}

		public void 更新ファイルnar作成ToolStripMenuItem_Click(object sender, EventArgs e)
		{
#if LEGACY
			if (Directory.Exists(directory))
			{
				if (mRoot.CreateUpdateWarning())
				{
					if (!String.IsNullOrWhiteSpace(GhostSettings.mCurrent.mUsedNarFolder) && Directory.Exists(Directory.GetParent(GhostSettings.mCurrent.mUsedNarFolder).FullName))
					{
						mRoot.narSaveFileDialog.InitialDirectory = Directory.GetParent(GhostSettings.mCurrent.mUsedNarFolder).FullName;
					}
					else
					{
						mRoot.narSaveFileDialog.InitialDirectory = directory;
					}

					mRoot.narSaveFileDialog.FileName = Path.GetFileName(GhostSettings.mCurrent.mUsedNarFolder);

					if (mRoot.narSaveFileDialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
					{
						CommonProgressDialog d = new CommonProgressDialog("narファイルと更新ファイルの作成", "作成しています。", "完了しました。", "エラーが発生したため失敗しました。", CallCreateNarAndUpdateFromDialog);
						d.ShowDialog(this);
					}
				}
			}
#endif
		}

		public void CallCreateNarAndUpdateFromDialog()
		{
#if LEGACY
			GhostSettings.mCurrent.mUsedNarFolder = mRoot.narSaveFileDialog.FileName;
			EasyDeveloperOptionsAnalyzer opt = new EasyDeveloperOptionsAnalyzer(directory + "/developer_options.txt", directory + "/delete.txt");

			mRoot.CreateNar(mRoot.narSaveFileDialog.FileName, UploadWizard.UPLOAD_TYPE.SHELL, opt);
			mRoot.CreateUpdate(UploadWizard.UPLOAD_TYPE.SHELL, opt);
#endif
		}

		public void 更新ファイルを作成してアップロードToolStripMenuItem_Click(object sender, EventArgs e)
		{
#if LEGACY
			if (Directory.Exists(directory))
			{
				if (mRoot.CreateUpdateWarning())
				{
					GhostData ghost_data = GhostSettings.Entry(directory);
					EasyDeveloperOptionsAnalyzer opt = new EasyDeveloperOptionsAnalyzer(directory + "/developer_options.txt", directory + "/delete.txt");
					UploadWizard wiz = new UploadWizard(mRoot, UploadWizard.UPLOAD_TYPE.SHELL, opt, ghost_data);
					wiz.ShowDialog(this);
				}
			}
#endif
		}

		public void シェルをエクスプローラで開くToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (directory != null)

			{

				if (Directory.Exists(directory))
				{
					try
					{
						System.Diagnostics.Process.Start(directory);
					}
					catch { }
				}
				else
				{
					MessageBox.Show("フォルダが見つかりません。");
				}

			}
		}

		private void 撮影ToolStripMenuItem_Click(object sender, EventArgs e)
		{
#if LEGACY
			UpdateCanvasSize();
			var item = new SurfaceViewerScreenShotList();
			item.EnumSSPWindows();
			if(item.ShowDialog()== DialogResult.OK)
			{
				TakeScreenShot(item.point, item.size);
			}
#endif
		}

		//単純にpngをロードして一枚絵な感じで表示する
		public void LoadSimplePng(string path)
		{
			try
			{
				using (System.IO.FileStream fs = new System.IO.FileStream(path, FileMode.Open, FileAccess.Read))
				{
					var b = new Bitmap(fs);
					UpdateCanvasSize();

					ResetLayer();
					pictureBox1.Image = null;

					using (Graphics g = Graphics.FromImage(surfaceCanvas))
					{
						g.DrawImage(b, 0, 0, b.Width, b.Height);
					}
					pictureBox1.Image = surfaceCanvas;
				}
			}
			catch { }
		}

		private void SurfaceViewer_DragEnter(object sender, DragEventArgs e)
		{
#if LEGACY
			mRoot.RootWindow_DragEnter(sender, e);
#endif
		}
	}

	public class CalcPolygon
	{
		public static Point triangle_center(Point pt1, Point pt2, Point pt3)
		{
			Point pt = new Point();
		  pt.X = (pt1.X + pt2.Y + pt3.X) / 3;
		  pt.Y = (pt1.Y + pt2.X + pt3.Y) / 3;
		  return pt;
		}

		public static double triangle_area(Point pt1, Point pt2, Point pt3)
		{
		  return ((pt2.X - pt1.X) * (pt3.Y - pt1.Y) -
		      (pt2.Y - pt1.Y) * (pt3.X - pt1.X));
		}

		public static Point GetPolygonCenter(int count, Point[] ppt)
		{
		  int i;
		  double s, s1;
		  Point pt, gpt;
		  gpt = new Point();

		  double gx, gy;
		  gx = 0; gy = 0;

		  s = 0.0;
		  for(i = 2; i < count; i++)
		  {
		    s1 = triangle_area(ppt[0], ppt[i - 1], ppt[i]);
		    pt = triangle_center(ppt[0], ppt[i - 1], ppt[i]);
		    gx += s1 * pt.X;
		    gy += s1 * pt.Y;
		    s += s1;
		  }
		  gpt.X = (int)(gx / s);
		  gpt.Y = (int)(gy / s);
		  return gpt;
		}


	}

	//アニメーション実行ステータス(animation id に関連付けられる)
	public class AnimationStatus
	{
		public int mPlayingPatternIndex = -1;	//現在の表示index。-1で無効。それ以外でアニメーション実行中。
		public AnimationTimer mController = null;	//現在バインドされているタイマー
		public BindType mBindType;	//着せ替え指定情報

		public enum BindType
		{
			Invalid,		//invalid のみ
			BindOnly,	//bind のみ
			BindPlus,	//bind+
			NoBind,		//その他
		}

		public static BindType GetBindType( List<SurfaceAnimationInterval> intervals)
		{
			bool isBind = false;
			bool isOther = false;

			foreach( SurfaceAnimationInterval i in intervals)
			{
				if (i.interval == SurfaceAnimationInterval.Intervals.Bind)
				{
					isBind = true;
				}
				else if( i.interval != SurfaceAnimationInterval.Intervals.Invalid)
				{
					isOther = true;
				}
			}

			if (isBind && isOther)
			{
				return BindType.BindPlus;
			}
			else if (isBind)
			{
				return BindType.BindOnly;
			}
			else if( isOther)
			{
				return BindType.NoBind;
			}
			else
			{
				return BindType.Invalid;
			}
		
		}

	}

	//アニメーション実行タイマー
	public class AnimationTimer : Timer
	{
		public int mAnimationID;
		public AnimationStatus mTarget;
		public SurfaceAnimation mAnimation;
		public SurfaceAnimationInterval mInterval;
	}


}
