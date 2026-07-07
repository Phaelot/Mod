using System;
using System.Collections.Generic;
using System.Drawing;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game
{
	public class MessageBoxData
	{
		public GameText SelectedListItem
		{
			get
			{
				return this.m_SelectedListItem;
			}
			set
			{
				this.m_SelectedListItem = value;
				if (this.OnListOptionSelected != null)
				{
					this.OnListOptionSelected(this.m_SelectedListItem);
				}
			}
		}

		public event Action<GameText> OnListOptionSelected;

		public GameText CaptionText;

		public GameText MessageText;

		public List<GameText> MessageTextList;

		public MessageBoxType DisplayType;

		public GameText YesText;

		public GameText NoText;

		public GameText DismissText;

		public GameText YesTT;

		public GameText NoTT;

		public GameText DismissTT;

		public WorkingRealm Realm;

		public List<ResourceData> Resources;

		public ResourceData Resource;

		public MessageType MsgType;

		public WorkingUnit Unit;

		public WorkingProvince Province;

		public WorkingStack Stack;

		public WorkingStack InterceptStack;

		public ActivePathNode Node;

		public string CustomData;

		public string TargetControlName;

		public PointF TargetMapCoords;

		public WorkingAgent Spy;

		public UnitQueueItem QueueItem;

		public WorkingHero Hero;

		// Optional illustration shown by MessageBoxForm for world/event messages.
		public string EventImageFile;

		public List<WorkingUnit> UnitList;

		public Dictionary<WorkingUnit, int> MedalChoices;

		public Dictionary<GameText, object> ListOptions;

		public Action OnYesResponse;

		public Action OnNoResponse;

		private GameText m_SelectedListItem;

		public int Gold;

		public WorkingRealm Ally;

		public BuildingEffect Building;

		public WorkingAgent Agent;
	}
}
