﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Imperial_Commander_Editor
{
	public class SpaceHighlight : INotifyPropertyChanged, IMapEntity
	{
		string _name;
		string _deploymentColor;
		int _width, _height, _duration;

		//common props
		public Guid GUID { get; set; }
		public string name { get { return _name; } set { _name = value; PC(); } }
		public EntityType entityType { get; set; }
		public Vector entityPosition { get; set; }
		public double entityRotation { get; set; }
		[JsonIgnore]
		public EntityRenderer mapRenderer { get; set; }
		public EntityProperties entityProperties { get; set; }
		public Guid mapSectionOwner { get; set; }

		//highlight props
		public string deploymentColor
		{
			get { return _deploymentColor; }
			set
			{
				_deploymentColor = value;
				PC();
				if ( mapRenderer != null )
				{
					Color c = Utils.ColorFromName( _deploymentColor ).color;
					mapRenderer.entityShape.Fill = new SolidColorBrush( Color.FromArgb( 100, c.R, c.G, c.B ) );
					mapRenderer.unselectedBGColor = new SolidColorBrush( Color.FromArgb( 100, c.R, c.G, c.B ) );
					mapRenderer.selectedBGColor = new SolidColorBrush( Color.FromArgb( 100, c.R, c.G, c.B ) );
				}
			}
		}
		public int Width { get { return _width; } set { _width = value; PC(); } }
		public int Height { get { return _height; } set { _height = value; PC(); } }
		public int Duration { get { return _duration; } set { _duration = value; PC(); } }


		public void PC( [CallerMemberName] string n = "" )
		{
			if ( !string.IsNullOrEmpty( n ) )
				PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( n ) );
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public SpaceHighlight()
		{
		}

		public SpaceHighlight( Guid ownderGUID )
		{
			GUID = Guid.NewGuid();
			name = "New Highlight";
			entityType = EntityType.Highlight;
			//defaults NOT ACTIVE, unlike other entities
			entityProperties = new() { isActive = false };
			mapSectionOwner = ownderGUID;
			Width = 1;
			Height = 1;
			Duration = 0;

			deploymentColor = "Green";
		}

		public IMapEntity Duplicate()
		{
			var dupe = new SpaceHighlight();
			dupe.GUID = Guid.NewGuid();
			dupe.name = name + " (Duplicate)";
			dupe.entityType = entityType;
			dupe.entityPosition = entityPosition;
			dupe.entityRotation = entityRotation;
			dupe.mapSectionOwner = mapSectionOwner;
			dupe.deploymentColor = deploymentColor;
			dupe.Width = Width;
			dupe.Height = Height;
			dupe.Duration = Duration;
			return dupe;
		}

		public void Rebuild()
		{
			Canvas canvas = mapRenderer.entityShape.Parent as Canvas;
			Vector w = mapRenderer.GetPosition();

			mapRenderer.RemoveVisual();

			Color c = Utils.ColorFromName( _deploymentColor ).color;

			mapRenderer = new( this, mapRenderer.where, Utils.mainWindow.mapEditor.showPanel, Utils.mainWindow.mapEditor.mScale, new( Width, Height ) )
			{
				selectedBGColor = new SolidColorBrush( Color.FromArgb( 100, c.R, c.G, c.B ) ),
				unselectedBGColor = new SolidColorBrush( Color.FromArgb( 100, c.R, c.G, c.B ) ),
				unselectedStrokeColor = new( Colors.Red ),
				selectedZ = 200
			};
			mapRenderer.BuildShape( TokenShape.Square );

			canvas.Children.Add( mapRenderer.entityShape );
			mapRenderer.Select();
			mapRenderer.SetPosition( w );
		}

		public void BuildRenderer( Canvas canvas, Vector where, bool showPanel, double scale )
		{
			Color c = Utils.ColorFromName( _deploymentColor ).color;

			mapRenderer = new( this, where, showPanel, scale, new( Width, Height ) )
			{
				selectedBGColor = new SolidColorBrush( Color.FromArgb( 100, c.R, c.G, c.B ) ),
				unselectedBGColor = new SolidColorBrush( Color.FromArgb( 100, c.R, c.G, c.B ) ),
				unselectedStrokeColor = new( Colors.Red ),
				selectedZ = 200
			};
			mapRenderer.BuildShape( TokenShape.Square );
			canvas.Children.Add( mapRenderer.entityShape );
		}
	}
}
