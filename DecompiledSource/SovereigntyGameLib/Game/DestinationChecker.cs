using System;
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game
{
	public class DestinationChecker
	{
		public DestinationChecker(SovereigntyGame Game)
		{
			this.Game = Game;
		}

		public Dictionary<WorkingUnit, UnitMoveResult> GetNonMovableUnits(WorkingStack Stack, ActivePathNode Node)
		{
			Dictionary<WorkingUnit, UnitMoveResult> dictionary = new Dictionary<WorkingUnit, UnitMoveResult>();
			foreach (WorkingUnit workingUnit in Stack.Units)
			{
				if (workingUnit.Selected)
				{
					UnitMoveResult unitMoveResult = this.NodeOKForUnit(workingUnit, Node);
					if (unitMoveResult != UnitMoveResult.OK)
					{
						dictionary.Add(workingUnit, unitMoveResult);
					}
					else
					{
						List<WorkingUnit> list = new List<WorkingUnit>();
						list.Add(workingUnit);
						Path path = this.Game.PathManager.GetPath(Stack.Node, Node, list, true, Stack.Owner, false);
						if (path.PathPoints.Count == 0)
						{
							dictionary.Add(workingUnit, UnitMoveResult.NotEnoughMoves);
						}
					}
				}
			}
			return dictionary;
		}

		public UnitMoveResult NodeOkForStack(WorkingStack Stack, ActivePathNode Node)
		{
			if (Node == null)
			{
				return UnitMoveResult.InvalidTerrain;
			}
			if (Stack.Owner == null)
			{
				return UnitMoveResult.InvalidTerrain;
			}
			if (Node == Stack.Node)
			{
				return UnitMoveResult.OK;
			}
			WorkingRealm owner = Stack.Owner;
			int num = 0;
			if (Stack.Hero != null && Stack.Hero.Selected)
			{
				if (Stack.Hero.HasStatus("Rooted", new object[0]))
				{
					return UnitMoveResult.HeroBlocked;
				}
				if (Stack.Hero.MovePoints == 0f)
				{
					return UnitMoveResult.HeroBlocked;
				}
			}
			if (Node.Province != null && Node.Province.OccupierRealm != owner)
			{
				RelationStates relation = Node.Province.OccupierRealm.DiplomacyManager.GetRelation(owner);
				if (relation != RelationStates.War && relation != RelationStates.Alliance && !owner.Restrictions.CanDeclareWar(Node.Province.OccupierRealm))
				{
					return UnitMoveResult.AttackNotPossible;
				}
			}
			WorkingStack realmStack = Node.GetRealmStack(owner);
			if (Node.Zone != null && Node.CurrentStack != null && Node.CurrentStack.Owner != Stack.Owner)
			{
				return UnitMoveResult.NotOwned;
			}
			if (realmStack != null && Stack.Hero != null && realmStack.Hero != null && Stack.Hero.Selected)
			{
				if (Stack.Units.Count((WorkingUnit x) => !x.Selected) == 0)
				{
					return UnitMoveResult.TooManyHeroes;
				}
			}
			if (Stack.Hero != null)
			{
				if (Stack.Hero.Selected)
				{
					if (realmStack == null || realmStack.Hero != null)
					{
						if (Stack.Units.Count((WorkingUnit x) => x.Selected) == 0)
						{
							return UnitMoveResult.HeroNeedsUnit;
						}
					}
				}
				else if (Stack.Units.Count((WorkingUnit x) => !x.Selected) == 0)
				{
					return UnitMoveResult.HeroNeedsUnit;
				}
			}
			bool flag = false;
			foreach (WorkingUnit workingUnit in Stack.Units)
			{
				if (workingUnit.Selected)
				{
					num++;
					if (this.NodeOKForUnit(workingUnit, Node) == UnitMoveResult.OK)
					{
						flag = true;
					}
				}
			}
			if (!flag)
			{
				return UnitMoveResult.FlagBlocked;
			}
			flag = false;
			if (Stack.Units.Count((WorkingUnit x) => !x.TeleportActive && x.Selected) > 0)
			{
				foreach (WorkingUnit workingUnit2 in Stack.Units.Where((WorkingUnit x) => x.Selected))
				{
					List<WorkingUnit> list = new List<WorkingUnit>();
					list.Add(workingUnit2);
					Path path = this.Game.PathManager.GetPath(Stack.Node, Node, list, true, Stack.Owner, false);
					if (path.PathPoints.Count > 0)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return UnitMoveResult.NotEnoughMoves;
				}
			}
			if (num == 0 && Stack.Hero != null && !Stack.Hero.Selected)
			{
				return UnitMoveResult.NoUnitsSelected;
			}
			if (num == 0 && Stack.Hero == null)
			{
				return UnitMoveResult.NoUnitsSelected;
			}
			if (realmStack != null && realmStack.Units.Count + num > 20)
			{
				return UnitMoveResult.ProvinceFull;
			}
			WorkingProvince province = Node.Province;
			if (province == null)
			{
				return UnitMoveResult.OK;
			}
			BorderStates borderState = province.GetBorderState(null);
			if (borderState == BorderStates.NoEntry || borderState == BorderStates.Closed)
			{
				return UnitMoveResult.ProvinceLocked;
			}
			return UnitMoveResult.OK;
		}

		public UnitMoveResult NodeOKToDeploy(WorkingUnit Unit, WorkingRealm Realm, ActivePathNode Node)
		{
			if (Node == null)
			{
				return UnitMoveResult.NotOwned;
			}
			if (Node.Province == null)
			{
				return UnitMoveResult.NotOwned;
			}
			if (Node.Province.OwnerRealm != Realm)
			{
				return UnitMoveResult.NotOwned;
			}
			if (Node.Province.Occupied)
			{
				return UnitMoveResult.NotOwned;
			}
			if (Node.Province.GetBorderState(Unit) == BorderStates.Closed || Node.Province.GetBorderState(Unit) == BorderStates.NoEntry)
			{
				return UnitMoveResult.ProvinceLocked;
			}
			if (Unit.Class == UnitClasses.Naval && Node.NodeType != PathNodeTypes.Harbour && Node.NodeType != PathNodeTypes.RiverHarbour)
			{
				return UnitMoveResult.NoHarbour;
			}
			if (Node.CurrentStack != null && Node.CurrentStack.Units.Count >= 20)
			{
				return UnitMoveResult.ProvinceFull;
			}
			return UnitMoveResult.OK;
		}

		public UnitMoveResult NodeOKForUnit(WorkingUnit Unit, ActivePathNode Node)
		{
			if (Unit.OwnerStack != null && Unit.OwnerStack.Node == Node)
			{
				if (Node.Province == null)
				{
					return UnitMoveResult.OK;
				}
				if (Node.Province.OwnerRealm == Unit.OwnerRealm)
				{
					return UnitMoveResult.OK;
				}
			}
			if (Unit.OwnerRealm == null)
			{
				return UnitMoveResult.InvalidTerrain;
			}
			if (Node.Zone != null && Node.CurrentStack != null && Node.CurrentStack.Owner != Unit.OwnerRealm)
			{
				return UnitMoveResult.NotOwned;
			}
			if (Node.NodeType == PathNodeTypes.RiverHarbour)
			{
				if (Unit.Class == UnitClasses.Naval && !Unit.HasStatus("Riverboat", new object[0]))
				{
					return UnitMoveResult.InvalidTerrain;
				}
				if (Unit.Class != UnitClasses.Naval)
				{
					if (Unit.OwnerRealm.UnitPurchaseManager.GetUnitsInClass(UnitClasses.Naval).FirstOrDefault((UnitData x) => x.AllowTransport && x.Abilities.Contains("Riverboat")) == null)
					{
						return UnitMoveResult.InvalidTerrain;
					}
				}
			}
			if (Unit.Class == UnitClasses.Naval && !Unit.Transport && Node.NodeType == PathNodeTypes.Land)
			{
				return UnitMoveResult.InvalidTerrain;
			}
			if (Node.Province != null && Node.Province.Floating && Unit.OwnerRealm != Node.Province.OwnerRealm && !Unit.HasAnyNamedFlag("Flier"))
			{
				return UnitMoveResult.InvalidTerrain;
			}
			if ((Node.NodeType == PathNodeTypes.Harbour || Node.NodeType == PathNodeTypes.RiverHarbour) && Node.CurrentStack != null && Unit.OwnerRealm != Node.CurrentStack.Owner && Unit.Attack == 0 && Unit.Transport)
			{
				return UnitMoveResult.NoTransportAttack;
			}
			if (Node.NodeType == PathNodeTypes.Land && Unit.Transport && Unit.CarriedUnit != null)
			{
				Unit.CarriedUnit.SetTempOwnerStackID(Unit.OwnerStackID);
				UnitMoveResult unitMoveResult = this.NodeOKForUnit(Unit.CarriedUnit, Node);
				Unit.CarriedUnit.SetTempOwnerStackID(-1);
				return unitMoveResult;
			}
			if (Unit.OwnerStack.Node != null)
			{
				BorderStates borderState = Unit.OwnerStack.Node.GetRegion().GetBorderState(Unit);
				if (borderState == BorderStates.NoExit || borderState == BorderStates.Closed)
				{
					return UnitMoveResult.ProvinceLocked2;
				}
			}
			BorderStates borderState2 = Node.GetRegion().GetBorderState(Unit);
			if (borderState2 == BorderStates.NoEntry || borderState2 == BorderStates.Closed)
			{
				return UnitMoveResult.ProvinceLocked;
			}
			if (!Unit.CanEnterTerrain(Node.GetRegion()))
			{
				return UnitMoveResult.FlagBlocked;
			}
			return UnitMoveResult.OK;
		}

		internal bool CanDeployHero(WorkingRealm Realm, ActivePathNode Node)
		{
			if (Node == null)
			{
				return false;
			}
			WorkingStack realmStack = Node.GetRealmStack(Realm);
			return realmStack != null && realmStack.Owner == Realm && realmStack.Hero == null;
		}

		public SovereigntyGame Game;
	}
}
