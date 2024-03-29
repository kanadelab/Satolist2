﻿using MahApps.Metro.Controls;
using Satolist2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Satolist2
{
	/// <summary>
	/// DarkRootWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class DarkRootWindow : MetroWindow
	{
		public DarkRootWindow()
		{
			InitializeComponent();
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			Main.SetRoot(this);
		}
	}
}
