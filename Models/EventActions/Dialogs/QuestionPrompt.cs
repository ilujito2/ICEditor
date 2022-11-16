﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Imperial_Commander_Editor
{
	public class QuestionPrompt : EventAction, IHasEventReference, IHasTriggerReference
	{
		string _theText;
		bool _includeCancel;

		public string theText { get { return _theText; } set { _theText = value; PC(); } }
		public bool includeCancel { get { return _includeCancel; } set { _includeCancel = value; PC(); } }

		public ObservableCollection<ButtonAction> buttonList { get; set; } = new();

		public QuestionPrompt()
		{

		}

		public QuestionPrompt( string dname
			, EventActionType et ) : base( et, dname )
		{
			_includeCancel = false;
		}

		public BrokenRefInfo NotifyEventRemoved( Guid guid, NotifyMode mode )
		{
			if ( buttonList.Any( x => x.eventGUID == guid ) )
			{
				var ranges = new List<string>();

				foreach ( var item in buttonList )
				{
					if ( item.eventGUID == guid )
					{
						if ( mode == NotifyMode.Update )
							item.eventGUID = Guid.Empty;
					}
				}

				ranges = buttonList.Where( x => x.eventGUID == guid ).Select( x =>
				{
					return $"'{x.buttonText}'";
				} ).ToList();

				return new()
				{
					itemName = displayName,
					isBroken = true,
					ownerGuid = GUID,
					brokenGuid = guid,
					details = $"Event from [Button(s)]: {string.Join( ", ", ranges )}"
				};
			}
			return new() { isBroken = false };
		}

		public BrokenRefInfo NotifyTriggerRemoved( Guid guid, NotifyMode mode )
		{
			if ( buttonList.Any( x => x.triggerGUID == guid ) )
			{
				var ranges = new List<string>();

				foreach ( var item in buttonList )
				{
					if ( item.triggerGUID == guid )
					{
						if ( mode == NotifyMode.Update )
							item.triggerGUID = Guid.Empty;
					}
				}

				ranges = buttonList.Where( x => x.triggerGUID == guid ).Select( x =>
				{
					return $"'{x.buttonText}'";
				} ).ToList();

				return new()
				{
					itemName = displayName,
					isBroken = true,
					ownerGuid = GUID,
					brokenGuid = guid,
					details = $"Trigger from [Button(s)]: {string.Join( ", ", ranges )}"
				};
			}
			return new() { isBroken = false };
		}
	}
}
