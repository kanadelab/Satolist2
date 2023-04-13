using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Satolist
{
	public partial class OpeningGhostDialog : Form
	{
		public OpeningGhostDialog()
		{
			InitializeComponent();
		}



		private delegate void ProgressDelegate(int data, int max);
		public void AddProgress( int data, int max)
		{
			
			Invoke(new ProgressDelegate(AddProgressInternal), data, max);
			
		}

		private void AddProgressInternal(int data, int max)
		{

			progressBar1.Maximum = max;
			progressBar1.Value = data;

			if (data != max - 1)
			{
				progressInfo.Text = "読み込みサーフェス: " + data + " / " + max ;
			}
			else
			{
				progressInfo.Text = "リストの作成中...";
			}
		}

		public void CloseInvoke()
		{
			
			Invoke(new Action(CallClose));
		}

		private void CallClose()
		{
			//progressInfo.Text = "完了しました。";
			base.Close();
		}

	}
}
