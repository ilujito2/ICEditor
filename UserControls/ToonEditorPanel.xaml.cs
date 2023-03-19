﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Imperial_Commander_Editor
{
	/// <summary>
	/// Interaction logic for ToonEditorPanel.xaml
	/// </summary>
	public partial class ToonEditorPanel : UserControl, INotifyPropertyChanged
	{
		public void PC( [CallerMemberName] string n = "" )
		{
			if ( !string.IsNullOrEmpty( n ) )
				PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( n ) );
		}
		public event PropertyChangedEventHandler PropertyChanged;

		DeploymentCard _selectedCopyFrom;

		public CustomToon customToon { get; set; }
		public DeploymentCard selectedCopyFrom { get => _selectedCopyFrom; set { _selectedCopyFrom = value; PC(); } }
		public bool isStandalone = false;
		public ObservableCollection<DeploymentColor> deploymentColors
		{
			get { return Utils.deploymentColors; }
		}

		public ToonEditorPanel() => InitializeComponent();

		public ToonEditorPanel( CustomToon ct = null )
		{
			InitializeComponent();

			customToon = ct;
			DataContext = customToon;

			propBox.Header = $"General Properties For '{customToon.deploymentCard.name}'";
			thumbListCB.ItemsSource = Utils.thumbnailData.Filter( ThumbType.All );
			thumbListCB.SelectedItem = customToon.thumbnail;

			copyFromCB.ItemsSource = Utils.enemyData;

			tierCB.ItemsSource = new int[] { 1, 2, 3 };
			priorityCB.ItemsSource = new int[] { 1, 2 };

			instructionsBtn.Foreground = customToon.instructions.Length == 0 ? new SolidColorBrush( Colors.Red ) : new SolidColorBrush( Colors.LawnGreen );
			bonusBtn.Foreground = string.IsNullOrEmpty( customToon.bonuses ) ? new SolidColorBrush( Colors.Red ) : new SolidColorBrush( Colors.LawnGreen );
			abilityBtn.Foreground = customToon.deploymentCard.abilities.Length == 0 ? new SolidColorBrush( Colors.Red ) : new SolidColorBrush( Colors.LawnGreen );
			surgeBtn.Foreground = customToon.deploymentCard.surges.Length == 0 ? new SolidColorBrush( Colors.Red ) : new SolidColorBrush( Colors.LawnGreen );
			keywordsBtn.Foreground = customToon.deploymentCard.keywords.Length == 0 ? new SolidColorBrush( Colors.Red ) : new SolidColorBrush( Colors.LawnGreen );
		}

		public void SetStandalone()
		{
			isStandalone = true;
			sizeWarningText.Visibility = Visibility.Collapsed;
		}

		private void textbox_KeyDown( object sender, KeyEventArgs e )
		{
			if ( e.Key == Key.Enter )
				Utils.LoseFocus( sender as Control );
		}

		private void nameTB_TextChanged( object sender, TextChangedEventArgs e )
		{
			propBox.Header = $"General Properties For '{((TextBox)sender).Text}'";
		}

		private void instructionsBtn_Click( object sender, RoutedEventArgs e )
		{
			string s = "";
			if ( customToon.instructions.Length > 0 )
			{
				s = customToon.instructions.Aggregate( ( acc, cur ) => acc + "\r\n" + cur );
			}

			var dlg = new GenericTextDialog( "EDIT INSTRUCTIONS", s );
			dlg.ShowDialog();
			customToon.instructions = dlg.theText.Split( "\r\n" ).Select( x => x.Trim() ).ToArray();

			instructionsBtn.Foreground = customToon.instructions.Length == 0 ? new SolidColorBrush( Colors.Red ) : new SolidColorBrush( Colors.LawnGreen );
		}

		private void bonusBtn_Click( object sender, RoutedEventArgs e )
		{
			var dlg = new GenericTextDialog( "EDIT BONUSES", customToon.bonuses );
			dlg.ShowDialog();
			customToon.bonuses = dlg.theText;

			bonusBtn.Foreground = string.IsNullOrEmpty( customToon.bonuses ) ? new SolidColorBrush( Colors.Red ) : new SolidColorBrush( Colors.LawnGreen );
		}

		private void abilityBtn_Click( object sender, RoutedEventArgs e )
		{
			try
			{
				string s = "";
				if ( customToon.deploymentCard.abilities.Length > 0 )
				{
					s = customToon.deploymentCard.abilities.Select( x => $"{x.name}:{x.text}" ).Aggregate( ( acc, cur ) => acc + "\r\n" + cur );
				}

				var dlg = new GenericTextDialog( "EDIT ABILITIES", s );
				dlg.ShowDialog();
				//(eventAction as CustomEnemyDeployment).abilities = dlg.theText;
				var array = dlg.theText.Split( "\r\n" );//get each line of text
				var list = new List<GroupAbility>();
				foreach ( var item in array )
				{
					var a = item.Trim().Split( ":" );//split into name and text
					list.Add( new() { name = a[0].Trim(), text = a[1].Trim() } );
				}
				customToon.deploymentCard.abilities = list.ToArray();
			}
			catch ( Exception ex )
			{
				MessageBox.Show( $"There was a problem parsing the text.  Did you format the text correctly?\n\nException:\n{ex.Message}", "Parsing Error" );
			}

			abilityBtn.Foreground = customToon.deploymentCard.abilities.Length == 0 ? new SolidColorBrush( Colors.Red ) : new SolidColorBrush( Colors.LawnGreen );
		}

		private void surgeBtn_Click( object sender, RoutedEventArgs e )
		{
			try
			{
				string s = "";
				if ( customToon.deploymentCard.surges.Length > 0 )
					s = string.Join( "\n", customToon.deploymentCard.surges );

				var dlg = new GenericTextDialog( "EDIT SURGES", s );
				dlg.ShowDialog();
				customToon.deploymentCard.surges = dlg.theText.Split( "\r\n" ).Select( x => x.Trim() ).ToArray();
			}
			catch ( Exception ex )
			{
				MessageBox.Show( $"There was a problem parsing the text.  Did you format the text correctly?\n\nException:\n{ex.Message}", "Parsing Error" );
			}

			surgeBtn.Foreground = customToon.deploymentCard.surges.Length == 0 ? new SolidColorBrush( Colors.Red ) : new SolidColorBrush( Colors.LawnGreen );
		}

		private void keywordsBtn_Click( object sender, RoutedEventArgs e )
		{
			try
			{
				string s = "";
				if ( customToon.deploymentCard.keywords.Length > 0 )
					s = string.Join( "\n", customToon.deploymentCard.keywords );

				var dlg = new GenericTextDialog( "EDIT KEYWORDS", s );
				dlg.ShowDialog();
				customToon.deploymentCard.keywords = dlg.theText.Split( "\r\n" ).Select( x => x.Trim() ).ToArray();
			}
			catch ( Exception ex )
			{
				MessageBox.Show( $"There was a problem parsing the text.  Did you format the text correctly?\n\nException:\n{ex.Message}", "Parsing Error" );
			}

			keywordsBtn.Foreground = customToon.deploymentCard.keywords.Length == 0 ? new SolidColorBrush( Colors.Red ) : new SolidColorBrush( Colors.LawnGreen );
		}

		private void targetBtn_Click( object sender, RoutedEventArgs e )
		{
			var dlg = new PriorityTraitsDialog( customToon.groupPriorityTraits );
			dlg.ShowDialog();
		}

		private void filterAllButton_Click( object sender, RoutedEventArgs e )
		{
			SetThumbSource( ThumbType.All );
		}

		private void filterRebelButton_Click( object sender, RoutedEventArgs e )
		{
			SetThumbSource( ThumbType.Rebel );
		}

		private void filterImperialButton_Click( object sender, RoutedEventArgs e )
		{
			SetThumbSource( ThumbType.Imperial );
		}

		private void filterMercButton_Click( object sender, RoutedEventArgs e )
		{
			SetThumbSource( ThumbType.Mercenary );
		}

		private void filterStockImperialButton_Click( object sender, RoutedEventArgs e )
		{
			SetThumbSource( ThumbType.StockImperial );
		}

		private void filterHeroButton_Click( object sender, RoutedEventArgs e )
		{
			SetThumbSource( ThumbType.StockHero );
		}

		private void filterVillainButton_Click( object sender, RoutedEventArgs e )
		{
			SetThumbSource( ThumbType.StockVillain );
		}

		private void filterAllyButton_Click( object sender, RoutedEventArgs e )
		{
			SetThumbSource( ThumbType.StockAlly );
		}

		private void SetThumbSource( ThumbType ttype )
		{
			iconFilterBox.Text = "";
			thumbListCB.SelectionChanged -= thumbListCB_SelectionChanged;
			thumbListCB.ItemsSource = Utils.thumbnailData.Filter( ttype );
			thumbListCB.SelectionChanged += thumbListCB_SelectionChanged;
		}

		private void thumbListCB_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			customToon.thumbnail = thumbListCB.SelectedItem as Thumbnail;
			SetThumbnailImage();
			iconFilterBox.Text = "";
			SetThumbSource( ThumbType.All );
		}

		public void SetThumbnailImage()
		{
			var item = Utils.thumbnailData.Filter( ThumbType.All ).Where( x => x.ID == customToon.thumbnail.ID ).FirstOrDefault();
			thumbListCB.SelectedItem = item;
			thumbPreview.Source = new BitmapImage( new Uri( $"pack://application:,,,/Imperial Commander Editor;component/Assets/Thumbnails/{customToon.thumbnail.ID.ThumbFolder()}/{customToon.thumbnail.ID}.png" ) );
		}

		private void confirmCopyFromButton_Click( object sender, RoutedEventArgs e )
		{
			if ( selectedCopyFrom != null )
			{
				var dg = Utils.enemyData.Where( x => x.id == selectedCopyFrom.id ).FirstOr( null );
				customToon.CopyFrom( dg );
			}
		}

		private void editTraitsButton_Click( object sender, RoutedEventArgs e )
		{
			var dlg = new CharacterTraitsDialog( customToon.deploymentCard.traits );
			dlg.ShowDialog();
			customToon.deploymentCard.traits = dlg.GetTraitStringArray();
			traitsText.Text = "None";
			if ( customToon.deploymentCard.traits.Length > 0 )
			{
				var t = customToon.deploymentCard.traits.Aggregate( ( acc, cur ) => acc + ", " + cur );
				traitsText.Text = t;
			}
		}

		private void charHelpButton_Click( object sender, RoutedEventArgs e )
		{
			var dlg = new CharacterHelpWindow( isStandalone );
			dlg.ShowDialog();
		}

		private void iconFilterBox_TextChanged( object sender, TextChangedEventArgs e )
		{
			if ( string.IsNullOrEmpty( iconFilterBox.Text ) )
				return;

			var fthumbs = Utils.thumbnailData.Filter( ThumbType.All ).Where( x => x.Name.ToLower().Contains( iconFilterBox.Text.ToLower() ) );
			if ( fthumbs == null )
				return;

			//set custom filtered CB source
			thumbListCB.SelectionChanged -= thumbListCB_SelectionChanged;
			thumbListCB.ItemsSource = fthumbs;
			thumbListCB.SelectionChanged += thumbListCB_SelectionChanged;

			//select first one found
			if ( fthumbs.FirstOr( null ) != null )
			{
				thumbListCB.SelectionChanged -= thumbListCB_SelectionChanged;
				thumbListCB.SelectedItem = fthumbs.First();
				thumbListCB.SelectionChanged += thumbListCB_SelectionChanged;
				customToon.thumbnail = fthumbs.First();
				SetThumbnailImage();
			}
		}

		private void iconFilterBox_KeyDown( object sender, KeyEventArgs e )
		{
			if ( e.Key == Key.Enter )
			{
				Utils.LoseFocus( sender as Control );
				if ( string.IsNullOrEmpty( iconFilterBox.Text ) )
					return;

				var fthumb = Utils.thumbnailData.Filter( ThumbType.All ).Where( x => x.Name.ToLower().Contains( iconFilterBox.Text.ToLower() ) ).FirstOr( null );
				if ( fthumb != null )
				{
					thumbListCB.SelectedItem = fthumb;
					customToon.thumbnail = fthumb;
					SetThumbnailImage();
					iconFilterBox.Text = "";
					SetThumbSource( ThumbType.All );
				}
			}
		}

		private void eliteCheckbox_Click( object sender, RoutedEventArgs e )
		{
			if ( eliteCheckbox.IsChecked == true && customToon.deploymentCard.isElite )
				customToon.outlineColor = "Red";
		}
	}
}
