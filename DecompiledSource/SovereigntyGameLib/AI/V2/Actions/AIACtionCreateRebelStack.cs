using System;
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIACtionCreateRebelStack : AIAction
	{
		public AIACtionCreateRebelStack(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
		}

		public override void Execute()
		{
			if (this.Province.OccupierRealm == this.Game.PlayerRealm)
			{
				MessageBoxData messageBoxData = new MessageBoxData();
				messageBoxData.MsgType = MessageType.Revolt;
				messageBoxData.DisplayType = MessageBoxType.Info;
				messageBoxData.CaptionText = GameText.CreateLocalised("REVOLT_HEADER", new object[0]);
				messageBoxData.MessageText = GameText.CreateLocalised("REVOLT_TEXT", new object[0]);
				messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(this.Province.DisplayName, new object[0]));
				messageBoxData.Province = this.Province;
				this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
			}
			else
			{
				GameText gameText = GameText.CreateLocalised("REVOLT_TICKER", new object[0]);
				gameText.AddChildText(GameText.CreateLocalised(this.Province.DisplayName, new object[0]));
				this.Game.GameCore.FireEvent("TickerMessage", new object[]
				{
					new TickerMessage(gameText, TickerMessageType.Military, 1)
				});
			}
			this.Stack = this.CreateRebelArmy(this.Province);
			this.State = AiActionStates.Finished;
		}

		private WorkingStack CreateRebelArmy(WorkingProvince Province)
		{
			int num = 1 + this.RNG.Next(7);
			int num2 = this.RNG.Next(Province.LandNode.ConnectedNodes.Count((ActiveNodeConnection x) => x.TargetNode.NodeType == PathNodeTypes.Land));
			ActivePathNode targetNode = Province.LandNode.ConnectedNodes.Where((ActiveNodeConnection x) => x.TargetNode.NodeType == PathNodeTypes.Land).ElementAt(num2).TargetNode;
			WorkingStack workingStack = this.Game.CreateStack(this.Game.RebelRealm.ID, targetNode.ID, false);
			WorkingRealm realm = this.Game.GetRealm(Province.NaturalOwner);
			for (int i = 0; i < num; i++)
			{
				List<KeyValuePair<UnitData, UnitTrainStates>> list = realm.UnitPurchaseManager.GetAvailableUnitTypes();
				list = list.Where((KeyValuePair<UnitData, UnitTrainStates> x) => x.Key.Rank != UnitRanks.Elite && x.Key.Rank != UnitRanks.Unique && x.Key.Class != UnitClasses.Naval).ToList<KeyValuePair<UnitData, UnitTrainStates>>();
				if (list.Count<KeyValuePair<UnitData, UnitTrainStates>>() == 0)
				{
					break;
				}
				int num3 = 0;
				foreach (KeyValuePair<UnitData, UnitTrainStates> keyValuePair in list)
				{
					num3 += keyValuePair.Key.GetUnitWeight(WarMode.War);
				}
				if (num3 == 0)
				{
					break;
				}
				int num4 = this.RNG.Next(num3) + 1;
				int num5 = 0;
				UnitData unitData = null;
				foreach (KeyValuePair<UnitData, UnitTrainStates> keyValuePair2 in list)
				{
					num5 += keyValuePair2.Key.GetUnitWeight(WarMode.War);
					unitData = keyValuePair2.Key;
					if (num5 >= num4)
					{
						break;
					}
				}
				if (unitData == null)
				{
					break;
				}
				WorkingUnit workingUnit = this.Game.CreateUnit(this.Game.RebelRealm.ID, unitData);
				workingStack.AddUnit(workingUnit, false, false);
			}
			return workingStack;
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		public WorkingProvince Province;

		public WorkingStack Stack;
	}
}
