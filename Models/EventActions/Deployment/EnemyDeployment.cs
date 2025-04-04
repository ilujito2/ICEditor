﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Imperial_Commander_Editor
{
	public class EnemyDeployment : EventAction, IHasEventReference, IHasTriggerReference, IHasEntityReference
	{
		string _enemyName, _deploymentGroup, _modification, _repositionInstructions;
		int _threatCost;
		bool _canReinforce, _canRedeploy, _canBeDefeated, _useThreat, _showMod, _useCustomInstructions, _useGenericMugshot, _useResetOnRedeployment;
		Guid _specificDeploymentPoint;
		DeploymentSpot _deploymentPoint;
		Thumbnail _thumbnail;

		public string enemyName { get { return _enemyName; } set { _enemyName = value; PC(); } }
		public string deploymentGroup { get { return _deploymentGroup; } set { _deploymentGroup = value; PC(); } }
		public string repositionInstructions { get { return _repositionInstructions; } set { _repositionInstructions = value; PC(); } }
		public int threatCost { get { return _threatCost; } set { _threatCost = value; PC(); } }
		public string modification { get { return _modification; } set { _modification = value; PC(); } }
		public bool canReinforce { get { return _canReinforce; } set { _canReinforce = value; PC(); } }
		public bool canRedeploy { get { return _canRedeploy; } set { _canRedeploy = value; PC(); } }
		public bool canBeDefeated { get { return _canBeDefeated; } set { _canBeDefeated = value; PC(); } }
		public bool useThreat { get { return _useThreat; } set { _useThreat = value; PC(); } }
		public bool useCustomInstructions { get { return _useCustomInstructions; } set { _useCustomInstructions = value; PC(); } }
		public bool useGenericMugshot { get { return _useGenericMugshot; } set { _useGenericMugshot = value; PC(); } }
		public bool showMod { get { return _showMod; } set { _showMod = value; PC(); } }
		public bool useResetOnRedeployment { get { return _useResetOnRedeployment; } set { _useResetOnRedeployment = value; PC(); } }
		public Guid specificDeploymentPoint { get { return _specificDeploymentPoint; } set { _specificDeploymentPoint = value; PC(); } }
		public DeploymentSpot deploymentPoint { get { return _deploymentPoint; } set { _deploymentPoint = value; PC(); } }
		public EnemyGroupData enemyGroupData { get; set; }
		public Thumbnail thumbnail { get => _thumbnail; set { _thumbnail = value; PC(); } }

		public EnemyDeployment()
		{

		}

		public EnemyDeployment( string dname
			, EventActionType et ) : base( et, dname )
		{
			_deploymentGroup = "DG001";
			_threatCost = 0;
			_canRedeploy = _canReinforce = true;
			_deploymentPoint = DeploymentSpot.Active;
			_specificDeploymentPoint = Guid.Empty;
			_useThreat = _showMod = false;
			_useCustomInstructions = false;
			_canBeDefeated = true;
			_repositionInstructions = "";
			_useGenericMugshot = false;
			_useResetOnRedeployment = false;
			_modification = "";
			_thumbnail = Utils.thumbnailData.NoneThumb;

			DeploymentCard card = Utils.enemyData.First( x => x.id == _deploymentGroup );
			enemyGroupData = new( card, new() { name = "Active Deployment Point", GUID = Guid.Empty } );
		}

		public void UpdateCard( DeploymentCard newcard )
		{
			enemyGroupData.UpdateCard( newcard );
		}

		public BrokenRefInfo NotifyEventRemoved( Guid guid, NotifyMode mode )
		{
			if ( enemyGroupData.defeatedEvent == guid )
			{
				if ( mode == NotifyMode.Fix )
					enemyGroupData.defeatedEvent = Guid.Empty;
				return new()
				{
					itemName = displayName,
					isBroken = true,
					ownerGuid = GUID,
					brokenGuid = guid,
					details = "Fixed 'On Defeated' Event"
				};
			}
			return new() { isBroken = false };
		}

		public BrokenRefInfo NotifyTriggerRemoved( Guid guid, NotifyMode mode )
		{
			if ( enemyGroupData.defeatedTrigger == guid )
			{
				if ( mode == NotifyMode.Fix )
					enemyGroupData.defeatedTrigger = Guid.Empty;
				return new()
				{
					itemName = displayName,
					isBroken = true,
					ownerGuid = GUID,
					brokenGuid = guid,
					details = "Fixed 'On Defeated' Trigger"
				};
			}
			return new() { isBroken = false };
		}

		public BrokenRefInfo NotifyEntityRemoved( Guid guid, NotifyMode mode )
		{
			if ( specificDeploymentPoint == guid || enemyGroupData.pointList.Any( x => x.GUID == guid ) )
			{
				if ( mode == NotifyMode.Fix )
				{
					specificDeploymentPoint = Guid.Empty;
					deploymentPoint = DeploymentSpot.Active;
					foreach ( var dp in enemyGroupData.pointList )
					{
						if ( dp.GUID == guid )
							dp.GUID = Guid.Empty;
					}
				}
				return new()
				{
					itemName = displayName,
					isBroken = deploymentPoint == DeploymentSpot.Specific || enemyGroupData.pointList.Any( x => x.GUID == guid ),
					ownerGuid = GUID,
					brokenGuid = guid,
					details = "Specific Deployment Point reset to Active Deployment Point"
				};
			}
			return new() { isBroken = false };
		}

		public BrokenRefInfo SelfCheckEvents()
		{
			if ( !Utils.ValidateEvent( enemyGroupData.defeatedEvent ) )
			{
				return new()
				{
					isBroken = true,
					topLevelNotifyType = NotifyType.Event,
					itemName = displayName,
					ownerGuid = GUID,
					brokenGuid = enemyGroupData.defeatedEvent,
					details = "Missing 'On Defeated' Event"
				};
			}
			return new() { isBroken = false };
		}

		public BrokenRefInfo SelfCheckTriggers()
		{
			if ( !Utils.ValidateTrigger( enemyGroupData.defeatedTrigger ) )
			{
				return new()
				{
					isBroken = true,
					topLevelNotifyType = NotifyType.Trigger,
					itemName = displayName,
					ownerGuid = GUID,
					brokenGuid = enemyGroupData.defeatedTrigger,
					details = "Missing 'On Defeated' Trigger"
				};
			}
			return new() { isBroken = false };
		}

		public BrokenRefInfo SelfCheckEntities()
		{
			List<string> strings = new();

			if ( !Utils.ValidateMapEntity( specificDeploymentPoint ) )
				strings.Add( "Missing Specific Deployment Point" );

			foreach ( var dp in enemyGroupData.pointList )
			{
				if ( !Utils.ValidateMapEntity( dp.GUID ) )
					strings.Add( "Missing Deployment Point" );
			}

			if ( strings.Count > 0 )
			{
				return new()
				{
					isBroken = true,
					topLevelNotifyType = NotifyType.Entity,
					itemName = displayName,
					ownerGuid = GUID,
					brokenGuid = Guid.Empty,
					details = string.Join( "\n", strings )
				};
			}
			return new() { isBroken = false };
		}
	}
}