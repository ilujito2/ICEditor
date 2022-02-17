﻿using System.Windows;

namespace Imperial_Commander_Editor
{
	/// <summary>
	/// Interaction logic for DeploymentGroupPropsDialog.xaml
	/// </summary>
	public partial class DeploymentPointPropsDialog : Window
	{
		public DeploymentPointPropsDialog( DeploymentPointProps props )
		{
			InitializeComponent();

			DataContext = props;
		}

		private void okButton_Click( object sender, RoutedEventArgs e )
		{
			Close();
		}

		private void Window_MouseDown( object sender, System.Windows.Input.MouseButtonEventArgs e )
		{
			if ( e.LeftButton == System.Windows.Input.MouseButtonState.Pressed )
				DragMove();
		}
	}
}
