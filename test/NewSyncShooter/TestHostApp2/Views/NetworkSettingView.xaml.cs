﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TestHostApp2.Views
{
	/// <summary>
	/// NetworkSelectionView.xaml の相互作用ロジック
	/// </summary>
	public partial class NetworkSettingView : UserControl
	{
		public NetworkSettingView()
		{
			InitializeComponent();
		}

		private void UserControl_Loaded( object sender, RoutedEventArgs e )
		{
			this.DataGrid_Network.Focus();
		}
	}
}
