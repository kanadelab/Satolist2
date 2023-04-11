using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Satolist
{
    public partial class ShellSelectDialog : Form
    {
        public bool mIsEnter = false;
        public Dictionary<String,string> mDir;   //表示名,フルパス
        //引数:検出済みの里々ディレクトリ
        public ShellSelectDialog( String satoriDir )
        {
            InitializeComponent();
            mDir = new Dictionary<string,string>();
            Init(satoriDir);
        }

        public void Init(String satoriDir )
        {
            //ghost
            try
            {
                DirectoryInfo d = Directory.GetParent(satoriDir);

                //ゴーストルート
                d = d.Parent;

                if (Directory.Exists(d.FullName + "/" + "shell"))
                {
                    //シェルフォルダが存在する
                    String[] s = Directory.GetDirectories(d.FullName + "/" + "shell");
                    foreach (string p in s)
                    {
                        comboBox1.Items.Add(Path.GetFileName(p));
                        mDir.Add(Path.GetFileName(p),p);
                    }
                }
            }
            catch
            {

            }
        }

        private void ok_Click(object sender, EventArgs e)
        {
            mIsEnter = true;
            Close();
        }

        private void cancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        public String GetDir()
        {
			if (comboBox1.SelectedItem == null)
			{
				if (String.IsNullOrEmpty(comboBox1.Text))
				{
					return null;	//空白はnull
				}
				else
				{
					return comboBox1.Text;
				}
				
			}

            if (mDir.Count <= 0)
                return null;
            else
            {
                if (mDir.ContainsKey((String)comboBox1.SelectedItem))
                    return mDir[(String)comboBox1.SelectedItem];
                else
                    return (String)comboBox1.SelectedItem;
            }
        }


    }
}
