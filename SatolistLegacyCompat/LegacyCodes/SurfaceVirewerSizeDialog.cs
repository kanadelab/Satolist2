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
	public partial class SurfaceVirewerSizeDialog : Form
	{
		public int mX = 0;
		public int mY = 0;
		public SurfaceVirewerSizeDialog( int x, int y)
		{
			InitializeComponent();
			DialogResult = System.Windows.Forms.DialogResult.Cancel;

			currentSetting.Text = currentSetting.Text + x.ToString() + "x" + y.ToString();
			sizeX.Value = x;
			sizeY.Value = y;
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			mX = (int)sizeX.Value;
			mY = (int)sizeY.Value;

			DialogResult = System.Windows.Forms.DialogResult.OK;
			Close();
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
