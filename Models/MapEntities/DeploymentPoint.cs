﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;

namespace Imperial_Commander_Editor
{
	public class DeploymentPoint : INotifyPropertyChanged, IMapEntity
	{
		string _name;
		Guid _mapSectionOwner;
		string _deploymentColor;

		//common props
		public Guid GUID { get; set; }
		public string name
		{
			get { return _name; }
			set
			{
				_name = string.IsNullOrEmpty( value ) ? "New Deployment Point" : value;
				PC();
			}
		}
		public EntityType entityType { get; set; }
		public Vector entityPosition { get; set; }
		public double entityRotation { get; set; }
		[JsonIgnore]
		public EntityRenderer mapRenderer { get; set; }
		public EntityProperties entityProperties { get; set; }
		public bool hasProperties { get { return false; } }

		//dp props
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
					mapRenderer.entityShape.Fill = new SolidColorBrush( c );
					mapRenderer.unselectedBGColor = new SolidColorBrush( c );
					mapRenderer.selectedBGColor = new SolidColorBrush( c );
				}
			}
		}
		public Guid mapSectionOwner { get { return _mapSectionOwner; } set { _mapSectionOwner = value; PC(); } }

		public event PropertyChangedEventHandler PropertyChanged;
		public void PC( [CallerMemberName] string n = "" )
		{
			if ( !string.IsNullOrEmpty( n ) )
				PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( n ) );
		}

		public DeploymentPoint()
		{
		}

		public DeploymentPoint( Guid ownderGUID )
		{
			GUID = Guid.NewGuid();
			name = "New Deployment Point";
			entityType = EntityType.DeploymentPoint;
			entityProperties = new() { name = name };
			mapSectionOwner = ownderGUID;

			entityProperties.isActive = false;

			deploymentColor = "Gray";
		}

		public IMapEntity Duplicate()
		{
			var dupe = new DeploymentPoint();
			dupe.GUID = Guid.NewGuid();
			dupe.name = name + " (Duplicate)";
			dupe.entityType = entityType;
			dupe.entityProperties = new();
			dupe.entityProperties.CopyFrom( this );
			dupe.entityProperties.name = dupe.name;
			dupe.entityPosition = entityPosition;
			dupe.entityRotation = entityRotation;
			dupe.mapSectionOwner = mapSectionOwner;
			dupe.deploymentColor = deploymentColor;
			return dupe;
		}

		public void BuildRenderer( Canvas canvas, Vector where, double scale )
		{
			Color c = Utils.ColorFromName( _deploymentColor ).color;

			mapRenderer = new( this, where, scale, new( 1, 1 ) )
			{
				selectedZ = 300,
				selectedImageZ = 305,
				selectedBGColor = new( c ),
				unselectedBGColor = new( c ),
				unselectedStrokeColor = new( Colors.Red )
			};
			mapRenderer.BuildShape( TokenShape.Circle );
			mapRenderer.BuildImage( "pack://application:,,,/Imperial Commander Editor;component/Assets/Tiles/dp.png" );
			canvas.Children.Add( mapRenderer.entityImage );
			canvas.Children.Add( mapRenderer.entityShape );
		}

		public bool Validate()
		{
			if ( !Utils.mainWindow.mission.EntityExists( GUID ) )
			{
				if ( GUID != Guid.Empty )
					name += " (NO LONGER VALID)";
				GUID = Guid.Empty;
				return false;
			}
			return true;
		}
	}
}
