using System;
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game
{
	public class PlayerMovementController
	{
		public PlayerMovementController(SovereigntyGame Game)
		{
			this.Game = Game;
		}

		public bool RequestMoveToNode(WorkingStack Stack, ActivePathNode Node, bool CheckNonMovers = true)
		{
			if (this.PreventAllMoves)
			{
				return false;
			}
			if (Node == null)
			{
				return false;
			}
			if (Stack.Node == Node)
			{
				return false;
			}
			if (this.Game.DestinationChecker.NodeOkForStack(Stack, Node) != UnitMoveResult.OK)
			{
				return false;
			}
			if (CheckNonMovers)
			{
				Dictionary<WorkingUnit, UnitMoveResult> nonMovableUnits = this.Game.DestinationChecker.GetNonMovableUnits(Stack, Node);
				if (nonMovableUnits.Count > 0)
				{
					MessageBoxData messageBoxData = new MessageBoxData();
					messageBoxData.CaptionText = GameText.CreateLocalised("LEAVEBEHINDTITLE", new object[0]);
					messageBoxData.MessageTextList = new List<GameText>();
					messageBoxData.MessageTextList.Add(GameText.CreateLocalised("LEAVEBEHINDTEXT", new object[0]));
					messageBoxData.MessageTextList.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
					foreach (KeyValuePair<WorkingUnit, UnitMoveResult> keyValuePair in nonMovableUnits)
					{
						messageBoxData.MessageTextList.Add(GameText.CreateLocalised(keyValuePair.Key.DisplayName, new object[0]));
						messageBoxData.MessageTextList.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
					}
					messageBoxData.DisplayType = MessageBoxType.YesNo;
					messageBoxData.MsgType = MessageType.MoveConfirm;
					messageBoxData.YesText = GameText.CreateLocalised("LEAVEBEHINDYES", new object[0]);
					messageBoxData.NoText = GameText.CreateLocalised("LEAVEBEHINDNO", new object[0]);
					messageBoxData.Stack = Stack;
					messageBoxData.Node = Node;
					this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
					return false;
				}
			}
			if (Node.Province == null)
			{
				this.MoveStackToNode(Stack, Node);
				return true;
			}
			if (Node.Province.OwnerRealm == Stack.Owner)
			{
				return this.AttemptInternalMove(Stack, Node);
			}
			switch (Node.Province.OwnerRealm.DiplomacyManager.GetRelation(Stack.Owner))
			{
			case RelationStates.Alliance:
				return this.AttemptAlliedMove(Stack, Node);
			case RelationStates.Defence:
			case RelationStates.NAP:
			case RelationStates.Peace:
			case RelationStates.ForcedPeace:
				return this.AttemptPeaceMove(Stack, Node);
			case RelationStates.War:
				return this.AttemptWarMove(Stack, Node);
			default:
				return false;
			}
		}

		private bool AttemptWarMove(WorkingStack Stack, ActivePathNode Node)
		{
			if (Node.CurrentStack == null)
			{
				bool flag = Stack.Owner.IsDefensiveInvasion(Node.Province.OccupierRealm);
				if (Stack.Owner.CodeOfWar && !flag && !Stack.Owner.HasStatus("IgnoreCode", new object[0]) && Node.Province.OccupierRealm.DiplomacyManager.GetRelationTime(Stack.Owner) == 0)
				{
					MessageBoxData messageBoxData = new MessageBoxData();
					messageBoxData.CaptionText = GameText.CreateLocalised("CODEMSGTITLE", new object[0]);
					messageBoxData.MessageText = GameText.CreateLocalised("CODEMSGTEXT", new object[0]);
					messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(Node.Province.OccupierRealm.DisplayName, new object[0]));
					messageBoxData.DisplayType = MessageBoxType.Info;
					messageBoxData.MsgType = MessageType.WarFail;
					this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
					return false;
				}
				if (this.NodeHasEnemyActiveFortifications(Stack, Node))
				{
					this.DoBattleMove(Stack, Node);
					return false;
				}
				this.MoveStackToNode(Stack, Node);
				return true;
			}
			else
			{
				if (Node.CurrentStack.Owner == Stack.Owner)
				{
					this.MoveStackToNode(Stack, Node);
					return true;
				}
				RelationStates relation = Node.CurrentStack.Owner.DiplomacyManager.GetRelation(Stack.Owner);
				if (relation == RelationStates.Alliance)
				{
					return false;
				}
				if (relation == RelationStates.Defence || relation == RelationStates.NAP)
				{
					this.ShowTreatyBreakDialog(Node.CurrentStack.Owner);
					return false;
				}
				if (relation == RelationStates.Peace)
				{
					this.ShowWarDialog(Stack, Node);
					return false;
				}
				if (relation == RelationStates.ForcedPeace)
				{
					return false;
				}
				this.DoBattleMove(Stack, Node);
				return false;
			}
		}

		private bool AttemptPeaceMove(WorkingStack Stack, ActivePathNode Node)
		{
			if (Node.CurrentStack == null)
			{
				if (Node.Province.OwnerRealm.DiplomacyManager.GetRelation(Stack.Owner) == RelationStates.ForcedPeace)
				{
					this.ShowForcedPeaceDialog(Node.Province.OwnerRealm);
					return false;
				}
				if (Node.Province.OwnerRealm.DiplomacyManager.GetRelation(Stack.Owner) == RelationStates.Peace)
				{
					this.ShowWarDialog(Stack, Node);
					return false;
				}
				this.ShowTreatyBreakDialog(Node.Province.OwnerRealm);
				return false;
			}
			else
			{
				if (Node.CurrentStack.Owner == Stack.Owner)
				{
					this.MoveStackToNode(Stack, Node);
					return true;
				}
				RelationStates relation = Node.CurrentStack.Owner.DiplomacyManager.GetRelation(Stack.Owner);
				if (relation == RelationStates.Alliance)
				{
					return false;
				}
				if (relation == RelationStates.Defence || relation == RelationStates.NAP)
				{
					this.ShowTreatyBreakDialog(Node.CurrentStack.Owner);
					return false;
				}
				if (relation == RelationStates.Peace)
				{
					this.ShowWarDialog(Stack, Node);
					return false;
				}
				if (relation == RelationStates.ForcedPeace)
				{
					this.ShowForcedPeaceDialog(Node.CurrentStack.Owner);
					return false;
				}
				this.DoBattleMove(Stack, Node);
				return false;
			}
		}

		private bool AttemptAlliedMove(WorkingStack Stack, ActivePathNode Node)
		{
			if (Node.CurrentStack == null)
			{
				if (this.NodeHasEnemyActiveFortifications(Stack, Node) && Node.Province.OccupierRealm.DiplomacyManager.GetRelation(Stack.Owner) == RelationStates.War)
				{
					this.DoBattleMove(Stack, Node);
					return false;
				}
				this.MoveStackToNode(Stack, Node);
				return true;
			}
			if (Node.CurrentStack.Owner == Stack.Owner)
			{
				this.MoveStackToNode(Stack, Node);
				return true;
			}
			RelationStates relation = Node.CurrentStack.Owner.DiplomacyManager.GetRelation(Stack.Owner);
			if (relation == RelationStates.Alliance)
			{
				this.MoveStackToNode(Stack, Node);
				return true;
			}
			if (relation == RelationStates.Defence || relation == RelationStates.NAP)
			{
				this.ShowTreatyBreakDialog(Node.CurrentStack.Owner);
				return false;
			}
			if (relation == RelationStates.Peace)
			{
				this.ShowWarDialog(Stack, Node);
				return false;
			}
			if (relation == RelationStates.ForcedPeace)
			{
				return false;
			}
			this.DoBattleMove(Stack, Node);
			return false;
		}

		private bool AttemptInternalMove(WorkingStack Stack, ActivePathNode Node)
		{
			if (Node.CurrentStack == null)
			{
				if (this.NodeHasEnemyActiveFortifications(Stack, Node))
				{
					this.DoBattleMove(Stack, Node);
					return false;
				}
				this.MoveStackToNode(Stack, Node);
				return true;
			}
			if (Node.CurrentStack.Owner == Stack.Owner)
			{
				this.MoveStackToNode(Stack, Node);
				return true;
			}
			RelationStates relation = Node.CurrentStack.Owner.DiplomacyManager.GetRelation(Stack.Owner);
			if (relation == RelationStates.Alliance || relation == RelationStates.Defence || relation == RelationStates.NAP)
			{
				this.ShowTreatyBreakDialog(Node.CurrentStack.Owner);
				return false;
			}
			if (relation == RelationStates.Peace)
			{
				this.ShowWarDialog(Stack, Node);
				return false;
			}
			if (relation == RelationStates.ForcedPeace)
			{
				return false;
			}
			this.DoBattleMove(Stack, Node);
			return false;
		}


		private bool NodeHasEnemyActiveFortifications(WorkingStack Stack, ActivePathNode Node)
		{
			if (Stack == null || Node == null || Node.Province == null)
			{
				return false;
			}
			if (Node.Province.OccupierRealm == null || Node.Province.OccupierRealm == Stack.Owner)
			{
				return false;
			}
			if (Node.Province.FortLevel <= 0 || Node.Province.Forts == null)
			{
				return false;
			}
			int fortCount = Math.Min(Node.Province.FortLevel, Node.Province.Forts.Count);
			for (int i = 0; i < fortCount; i++)
			{
				WorkingUnit fort = Node.Province.Forts[i];
				if (fort != null && !fort.Disabled && (int)fort.Health > 0)
				{
					return true;
				}
			}
			return false;
		}

		private void DoBattleMove(WorkingStack Stack, ActivePathNode Node)
		{
			List<WorkingUnit> list = Stack.Units.Where((WorkingUnit x) => x.Selected && this.Game.DestinationChecker.NodeOKForUnit(x, Node) == UnitMoveResult.OK).ToList<WorkingUnit>();
			bool flag = Stack.Owner.IsDefensiveInvasion(Node.Province.OccupierRealm);
			if (Stack.Owner.CodeOfWar && !flag && !Stack.Owner.HasStatus("IgnoreCode", new object[0]) && Node.Province.OwnerRealm != this.Game.PlayerRealm && Node.Province.OccupierRealm.DiplomacyManager.GetRelationTime(Stack.Owner) == 0)
			{
				MessageBoxData messageBoxData = new MessageBoxData();
				messageBoxData.CaptionText = GameText.CreateLocalised("CODEMSGTITLE", new object[0]);
				messageBoxData.MessageText = GameText.CreateLocalised("CODEMSGTEXT", new object[0]);
				messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(Node.Province.OccupierRealm.DisplayName, new object[0]));
				messageBoxData.DisplayType = MessageBoxType.Info;
				messageBoxData.MsgType = MessageType.WarFail;
				this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
				return;
			}
			if (this.AwaitingConfirmNode != Node)
			{
				this.AwaitingConfirmNode = Node;
				MessageBoxData messageBoxData2 = new MessageBoxData();
				messageBoxData2.CaptionText = GameText.CreateLocalised("ATTACKCONFIRMTITLE", new object[0]);
				messageBoxData2.MessageText = GameText.CreateLocalised("ATTACKCONFIRMTEXT", new object[0]);
				messageBoxData2.MessageText.AddChildText(GameText.CreateLocalised(Node.Province.DisplayName, new object[0]));
				messageBoxData2.YesText = GameText.CreateLocalised("ATTACKCONFIRMYES", new object[0]);
				messageBoxData2.NoText = GameText.CreateLocalised("ATTACKCONFIRMNO", new object[0]);
				messageBoxData2.Node = Node;
				messageBoxData2.Stack = Stack;
				messageBoxData2.DisplayType = MessageBoxType.YesNo;
				messageBoxData2.MsgType = MessageType.AttackConfirm;
				this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData2);
				return;
			}
			this.AwaitingConfirmNode = null;
			foreach (WorkingUnit workingUnit in list)
			{
				workingUnit.ClearMoves();
			}
			if (Stack.Node.Zone != null && Node.Province != null && Node.Province.OccupierRealm != this.Game.PlayerRealm)
			{
				WorkingStack interceptStack = this.Game.GetInterceptStack(Node.Province.OccupierRealm, Stack.Node.Zone, Stack);
				if (interceptStack != null)
				{
					MessageBoxData messageBoxData3 = new MessageBoxData();
					messageBoxData3.CaptionText = GameText.CreateLocalised("INTERCEPTTITLE", new object[0]);
					messageBoxData3.MessageText = GameText.CreateLocalised("INTERCEPTTEXT", new object[0]);
					messageBoxData3.Stack = Stack;
					messageBoxData3.InterceptStack = interceptStack;
					messageBoxData3.DisplayType = MessageBoxType.Info;
					messageBoxData3.MsgType = MessageType.AttackIntercept;
					this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData3);
					return;
				}
			}
			foreach (WorkingUnit workingUnit2 in Stack.Units)
			{
				if (!list.Contains(workingUnit2))
				{
					workingUnit2.Selected = false;
				}
			}
			Path path = this.Game.PathManager.GetPath(Stack.Node, Node, list, false, Stack.Owner, false);
			if (Node.CurrentStack != null && Node.CurrentStack.Units.Count > 0)
			{
				this.Game.StartBattle(Stack, Node.CurrentStack, path);
				return;
			}
			if (this.NodeHasEnemyActiveFortifications(Stack, Node))
			{
				if (Node.CurrentStack != null && Node.CurrentStack.Units.Count == 0)
				{
					this.Game.RemoveStack(Node.CurrentStack);
				}
				WorkingStack fortStack = this.Game.CreateStack(Node.Province.OccupierRealm.ID, Node.ID, false);
				this.Game.StartBattle(Stack, fortStack, path);
				return;
			}
			if (Node.CurrentStack != null && Node.CurrentStack.Units.Count == 0)
			{
				this.Game.RemoveStack(Node.CurrentStack);
			}
		}

		private void ShowForcedPeaceDialog(WorkingRealm TargetRealm)
		{
			MessageBoxData messageBoxData = new MessageBoxData();
			messageBoxData.CaptionText = GameText.CreateLocalised("FORCEDPEACE_TITLE", new object[0]);
			messageBoxData.MessageText = GameText.CreateLocalised("FORCEDPEACE_TEXT", new object[] { 5 - this.Game.PlayerRealm.DiplomacyManager.GetRelationTime(TargetRealm) });
			messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(TargetRealm.DisplayName, new object[0]));
			messageBoxData.DisplayType = MessageBoxType.Info;
			messageBoxData.MsgType = MessageType.GenericInfo;
			this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
		}

		private void ShowWarDialog(WorkingStack Stack, ActivePathNode Node)
		{
			WorkingRealm workingRealm;
			if (Node.CurrentStack != null)
			{
				workingRealm = Node.CurrentStack.Owner;
			}
			else
			{
				workingRealm = Node.Province.OwnerRealm;
			}
			this.Game.GameCore.FireEvent("ShowWarDialog", new object[] { workingRealm, Stack, Node });
		}

		private void ShowTreatyBreakDialog(WorkingRealm Realm)
		{
			MessageBoxData messageBoxData = new MessageBoxData();
			messageBoxData.CaptionText = GameText.CreateLocalised("TREATY_BREAK_TITLE", new object[0]);
			messageBoxData.MessageText = GameText.CreateLocalised("TREATY_BREAK_TEXT", new object[0]);
			messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(Realm.DisplayName, new object[0]));
			messageBoxData.YesText = GameText.CreateLocalised("TREATY_BREAK_YES", new object[0]);
			messageBoxData.NoText = GameText.CreateLocalised("TREATY_BREAK_NO", new object[0]);
			messageBoxData.DisplayType = MessageBoxType.YesNo;
			messageBoxData.MsgType = MessageType.TreatyBreak;
			messageBoxData.Realm = Realm;
			this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
		}

		private void MoveStackToNode(WorkingStack Stack, ActivePathNode Node)
		{
			List<WorkingUnit> list = Stack.Units.Where((WorkingUnit x) => x.Selected && this.Game.DestinationChecker.NodeOKForUnit(x, Node) == UnitMoveResult.OK).ToList<WorkingUnit>();
			ActivePathNode node = Stack.Node;
			if (Node.NodeType == PathNodeTypes.Land)
			{
				this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\move_stack_standard.wav");
			}
			else
			{
				this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\paddle_water02.wav");
			}
			Path path = this.Game.PathManager.GetPath(Stack.Node, Node, list, false, Stack.Owner, false);
			path.TotalMoveCost = 10f;
			WorkingStack workingStack = Node.GetRealmStack(Stack.Owner);
			if (workingStack == null)
			{
				workingStack = this.Game.CreateStack(Stack.OwnerID, Node.ID, false);
				if (Node.Province != null && !Node.Province.Occupied && Node.Province.OwnerRealm != this.Game.PlayerRealm && this.Game.PlayerRealm.DiplomacyManager.GetRelation(Node.Province.OwnerRealm) == RelationStates.Alliance)
				{
					Node.AllyStacks.Add(workingStack.ID);
				}
				else
				{
					Node.CurrentStackID = workingStack.ID;
				}
			}
			int num = 0;
			foreach (WorkingUnit workingUnit in list)
			{
				if (workingStack.Disposed)
				{
					workingStack = this.Game.CreateStack(Stack.OwnerID, Node.ID, false);
					if (Node.Province != null && !Node.Province.Occupied && Node.Province.OwnerRealm != this.Game.PlayerRealm && this.Game.PlayerRealm.DiplomacyManager.GetRelation(Node.Province.OwnerRealm) == RelationStates.Alliance)
					{
						Node.AllyStacks.Add(workingStack.ID);
					}
					else
					{
						Node.CurrentStackID = workingStack.ID;
					}
				}
				if (!this.Game.MoveUnit(Stack, workingStack, workingUnit, path, false))
				{
					num++;
				}
			}
			if (num > 0)
			{
				MessageBoxData messageBoxData = new MessageBoxData();
				messageBoxData.CaptionText = GameText.CreateLocalised("RAPIDS_TITLE", new object[0]);
				messageBoxData.MessageText = GameText.CreateLocalised("RAPIDS_TEXT", new object[] { num });
				messageBoxData.DisplayType = MessageBoxType.Info;
				messageBoxData.MsgType = MessageType.GenericInfo;
				this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
			}
			if (Node.Province != null && Node.Province.OwnerRealm != Stack.Owner && Node.Province.OwnerRealm.DiplomacyManager.GetRelation(Stack.Owner) != RelationStates.Alliance)
			{
				foreach (WorkingUnit workingUnit2 in list)
				{
					workingUnit2.ClearMoves();
				}
			}
			if (Stack.Hero != null && Stack.Hero.Selected && workingStack.Hero == null)
			{
				workingStack.TransferHeroFromStack(Stack, Stack.Hero);
			}
			if (Stack.Units.Count == 0)
			{
				if (Stack.Node.NodeType == PathNodeTypes.Land && Stack.Node.Province.IsCapitol && Stack.Node.Province.OwnerRealm != Stack.Owner)
				{
					this.Game.GameCore.FireEvent("OccupiedCapitolRetreat", new object[0]);
				}
				this.Game.RemoveStack(Stack);
			}
			if (workingStack.Units.Count == 0)
			{
				this.Game.RemoveStack(workingStack);
			}
			this.Game.GameCore.FireEvent("PlayerStacksChanged", new object[0]);
			if (node.Province != null && !node.Province.Occupied)
			{
				this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { node.Province });
			}
			if (Node.Province != null && Node.Province.Occupied)
			{
				this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { Node.Province });
				this.Game.GameCore.FireEvent("PlayerOccupiedProvince", new object[] { Node.Province });
			}
		}

		private SovereigntyGame Game;

		private ActivePathNode AwaitingConfirmNode;

		public bool PreventAllMoves;
	}
}
