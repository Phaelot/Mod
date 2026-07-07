using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenTK.Input;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI;
using SovereigntyTK.UI.Map;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game.Battle
{
	public class TacticalBattleController
	{
		public event UnitDelegate OnUnitMouseover;

		public event UnitClickDelegate OnUnitClicked;

		public event TileClickDelegate OnTileClicked;

		public event TileDelegate OnTileMouseover;

		public event MouseButtonDelegate OnMapClicked;

		public event BattleModDelegate OnPlunderModRequest;

		public ActivePathNode Node
		{
			get
			{
				return this.Game.AllNodes[this.NodeID];
			}
		}

		public ActivePathNode AttackerNode
		{
			get
			{
				return this.Game.AllNodes[this.AttackerNodeID];
			}
		}

		public WorkingRealm AttackerRealm
		{
			get
			{
				return this.Game.AllRealms[this.Attacker.OwnerID];
			}
		}

		public WorkingRealm DefenderRealm
		{
			get
			{
				return this.Game.AllRealms[this.Defender.OwnerID];
			}
		}

		public IList<WorkingUnit> DeadAttackers
		{
			get
			{
				List<WorkingUnit> list = new List<WorkingUnit>();
				foreach (int num in this.DeadAttackerUnits)
				{
					WorkingUnit workingUnit = null;
					this.Game.AllUnits.TryGetValue(num, out workingUnit);
					if (workingUnit != null)
					{
						list.Add(this.Game.AllUnits[num]);
					}
				}
				return list.AsReadOnly();
			}
		}

		public IList<WorkingUnit> DeadDefenders
		{
			get
			{
				List<WorkingUnit> list = new List<WorkingUnit>();
				foreach (int num in this.DeadDefenderUnits)
				{
					WorkingUnit workingUnit = null;
					this.Game.AllUnits.TryGetValue(num, out workingUnit);
					if (workingUnit != null)
					{
						list.Add(this.Game.AllUnits[num]);
					}
				}
				return list.AsReadOnly();
			}
		}

		public WorkingStack ActiveStack
		{
			get
			{
				if (this.TurnCounter.CurrentPlayerID == this.Attacker.OwnerID)
				{
					return this.Attacker;
				}
				return this.Defender;
			}
		}

		public WorkingStack InactiveStack
		{
			get
			{
				if (this.TurnCounter.CurrentPlayerID == this.Attacker.OwnerID)
				{
					return this.Defender;
				}
				return this.Attacker;
			}
		}

		public TacticalBattleController(SovereigntyGame Game, AutoBattleController AutoBattle)
		{
			this.Game = Game;
			this.RiverCrossing = AutoBattle.RiverCrossing;
			this.CapturedFloaties = new List<FloatingSpriteData>();
			this.InitialAttackers = AutoBattle.InitialAttackers;
			this.InitialDefenders = AutoBattle.InitialDefenders;
			this.CapturedAttackerUnits = new List<WorkingUnit>();
			this.CapturedDefenderUnits = new List<WorkingUnit>();
			this.Attacker = AutoBattle.Attacker;
			this.Defender = AutoBattle.Defender;
			this.NodeID = AutoBattle.Node.ID;
			this.AttackerNodeID = AutoBattle.AttackerNode.ID;
			if (this.Attacker.Owner.AIPlayer != null)
			{
				this.Attacker.Owner.AIPlayer.TacticalManager.InitForBattle(this);
			}
			if (this.Defender.Owner.AIPlayer != null)
			{
				this.Defender.Owner.AIPlayer.TacticalManager.InitForBattle(this);
			}
			if (this.Node.Province != null)
			{
				Game.GameCore.CurrentBattleMap = new BattleMap(Game, this, this.AttackerNode.Province, this.AttackerNode.Zone, this.Node.Province);
			}
			else
			{
				Game.GameCore.CurrentBattleMap = new BattleMap(Game, this, this.Node.Zone);
			}
			this.Map = Game.GameCore.CurrentBattleMap;
			this.Map.DarkenTiles();
			if (this.Node.NodeType != PathNodeTypes.Sea)
			{
				this.Map.CreateDeploymentDistanceMaps();
			}
			this.SetInitialOwnership(this.Map.DefenderTiles);
			this.TurnCounter = new BattleTurnController(this);
			Game.GameCore.CurrentBattleMap.OnTileMouseDown += this.CurrentBattleMap_OnTileMouseDown;
			Game.GameCore.CurrentBattleMap.OnTileMouseover += this.CurrentBattleMap_OnTileMouseover;
			Game.GameCore.CurrentBattleMap.OnTileMouseUp += this.CurrentBattleMap_OnTileMouseUp;
			Game.GameCore.CurrentBattleMap.OnTileMouseClick += this.CurrentBattleMap_OnTileMouseClick;
			this.Attacker.Owner.StartBattle(this);
			this.Defender.Owner.StartBattle(this);
			AutoBattle.RepackTransports();
			this.AutoDeployAttackers();
			this.AutoDeployDefenders();
			this.AutoDeployForts();
			Game.GameCore.CurrentBattleMap.SetMode(BattleMapModes.Deploy);
			this.RemovedAttackerUnits = new List<int>();
			this.RemovedDefenderUnits = new List<int>();
			this.DeadAttackerUnits = new List<int>();
			this.DeadDefenderUnits = new List<int>();
			if (this.AttackerRealm == Game.PlayerRealm)
			{
				using (IEnumerator<WorkingUnit> enumerator = this.Attacker.Units.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						WorkingUnit workingUnit = enumerator.Current;
						workingUnit.BattleData.Sprite.Show();
					}
					goto IL_030F;
				}
			}
			foreach (WorkingUnit workingUnit2 in this.Defender.Units)
			{
				workingUnit2.BattleData.Sprite.Show();
			}
			IL_030F:
			Game.GameCore.FireEvent("FullBattleStarted", new object[] { this });
		}

		public TacticalBattleController(SovereigntyGame Game, BinaryReader r, int SaveVersion)
		{
			this.Game = Game;
			this.Loaded = true;
			this.CapturedFloaties = new List<FloatingSpriteData>();
			this.Attacker = Game.AllStacks[r.ReadInt32()];
			this.Defender = Game.AllStacks[r.ReadInt32()];
			this.NodeID = r.ReadInt32();
			this.AttackerNodeID = r.ReadInt32();
			if (this.Attacker.Owner.AIPlayer != null)
			{
				this.Attacker.Owner.AIPlayer.TacticalManager.InitForBattle(this);
			}
			if (this.Defender.Owner.AIPlayer != null)
			{
				this.Defender.Owner.AIPlayer.TacticalManager.InitForBattle(this);
			}
			if (this.Node.Province != null)
			{
				Game.GameCore.CurrentBattleMap = new BattleMap(Game, this, this.AttackerNode.Province, this.AttackerNode.Zone, this.Node.Province);
			}
			else
			{
				Game.GameCore.CurrentBattleMap = new BattleMap(Game, this, this.Node.Zone);
			}
			this.Map = Game.GameCore.CurrentBattleMap;
			this.Map.CreateDeploymentDistanceMaps();
			this.RemovedAttackerUnits = new List<int>();
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				this.RemovedAttackerUnits.Add(r.ReadInt32());
			}
			this.RemovedDefenderUnits = new List<int>();
			num = r.ReadInt32();
			for (int j = 0; j < num; j++)
			{
				this.RemovedDefenderUnits.Add(r.ReadInt32());
			}
			this.DeadAttackerUnits = new List<int>();
			num = r.ReadInt32();
			for (int k = 0; k < num; k++)
			{
				this.DeadAttackerUnits.Add(r.ReadInt32());
			}
			this.DeadDefenderUnits = new List<int>();
			num = r.ReadInt32();
			for (int l = 0; l < num; l++)
			{
				this.DeadDefenderUnits.Add(r.ReadInt32());
			}
			this.TurnCounter = new BattleTurnController(this, r);
			Game.GameCore.CurrentBattleMap.OnTileMouseDown += this.CurrentBattleMap_OnTileMouseDown;
			Game.GameCore.CurrentBattleMap.OnTileMouseover += this.CurrentBattleMap_OnTileMouseover;
			Game.GameCore.CurrentBattleMap.OnTileMouseUp += this.CurrentBattleMap_OnTileMouseUp;
			Game.GameCore.CurrentBattleMap.OnTileMouseClick += this.CurrentBattleMap_OnTileMouseClick;
			num = r.ReadInt32();
			this.VictoryPointOwnership = new Dictionary<Point, VPData>();
			for (int m = 0; m < num; m++)
			{
				Point point = new Point(r.ReadInt32(), r.ReadInt32());
				VPData vpdata = new VPData();
				vpdata.OwnerID = r.ReadInt32();
				vpdata.AttackerBonusGranted = r.ReadBoolean();
				vpdata.DefenderBonusGranted = r.ReadBoolean();
				this.VictoryPointOwnership.Add(point, vpdata);
			}
			if (r.ReadBoolean())
			{
				this.Map.SetMode(BattleMapModes.Deploy);
				this.DeployPhase = true;
			}
			else
			{
				this.Map.SetMode(BattleMapModes.Default);
				this.DeployPhase = false;
			}
			foreach (WorkingUnit workingUnit in this.Attacker.Units.Where((WorkingUnit x) => !x.Disabled))
			{
				workingUnit.CreateBattleData(this, false);
				workingUnit.BattleData.Load(r);
				Game.HandleUnitNodeChanged(workingUnit.ID, this.Node.ID);
				this.SetUnitTile(workingUnit, workingUnit.BattleData.BattleLocation);
			}
			foreach (WorkingUnit workingUnit2 in this.Defender.Units.Where((WorkingUnit x) => !x.Disabled))
			{
				workingUnit2.CreateBattleData(this, false);
				workingUnit2.BattleData.Load(r);
				this.SetUnitTile(workingUnit2, workingUnit2.BattleData.BattleLocation);
			}
			this.AttackerRealm.BattleData = new RealmBattleData(Game, this, this.AttackerRealm.ID, r);
			this.DefenderRealm.BattleData = new RealmBattleData(Game, this, this.DefenderRealm.ID, r);
			this.RiverCrossing = r.ReadBoolean();
			this.InitialAttackers = r.ReadInt32();
			this.InitialDefenders = r.ReadInt32();
			this.CapturedAttackerUnits = new List<WorkingUnit>();
			this.CapturedDefenderUnits = new List<WorkingUnit>();
			if (SaveVersion >= 54)
			{
				num = r.ReadInt32();
				for (int n = 0; n < num; n++)
				{
					WorkingUnit workingUnit3 = null;
					Game.AllUnits.TryGetValue(r.ReadInt32(), out workingUnit3);
					if (workingUnit3 != null)
					{
						this.CapturedAttackerUnits.Add(workingUnit3);
					}
				}
				num = r.ReadInt32();
				for (int num2 = 0; num2 < num; num2++)
				{
					WorkingUnit workingUnit4 = null;
					Game.AllUnits.TryGetValue(r.ReadInt32(), out workingUnit4);
					if (workingUnit4 != null)
					{
						this.CapturedDefenderUnits.Add(workingUnit4);
					}
				}
			}
			if (SaveVersion >= 55)
			{
				bool flag = r.ReadBoolean();
				if (flag)
				{
					this.RetreatingPlayer = r.ReadString();
					return;
				}
				this.RetreatingPlayer = null;
			}
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.Attacker.ID);
			w.Write(this.Defender.ID);
			w.Write(this.NodeID);
			w.Write(this.AttackerNodeID);
			w.Write(this.RemovedAttackerUnits.Count);
			foreach (int num in this.RemovedAttackerUnits)
			{
				w.Write(num);
			}
			w.Write(this.RemovedDefenderUnits.Count);
			foreach (int num2 in this.RemovedDefenderUnits)
			{
				w.Write(num2);
			}
			w.Write(this.DeadAttackerUnits.Count);
			foreach (int num3 in this.DeadAttackerUnits)
			{
				w.Write(num3);
			}
			w.Write(this.DeadDefenderUnits.Count);
			foreach (int num4 in this.DeadDefenderUnits)
			{
				w.Write(num4);
			}
			this.TurnCounter.Save(w);
			w.Write(this.VictoryPointOwnership.Count);
			foreach (KeyValuePair<Point, VPData> keyValuePair in this.VictoryPointOwnership)
			{
				w.Write(keyValuePair.Key.X);
				w.Write(keyValuePair.Key.Y);
				w.Write(keyValuePair.Value.OwnerID);
				w.Write(keyValuePair.Value.AttackerBonusGranted);
				w.Write(keyValuePair.Value.DefenderBonusGranted);
			}
			w.Write(this.Map.CurrentMode == BattleMapModes.Deploy);
			foreach (WorkingUnit workingUnit in this.Attacker.Units.Where((WorkingUnit x) => !x.Disabled))
			{
				workingUnit.BattleData.Save(w);
			}
			foreach (WorkingUnit workingUnit2 in this.Defender.Units.Where((WorkingUnit x) => !x.Disabled))
			{
				workingUnit2.BattleData.Save(w);
			}
			this.AttackerRealm.BattleData.Save(w);
			this.DefenderRealm.BattleData.Save(w);
			w.Write(this.RiverCrossing);
			w.Write(this.InitialAttackers);
			w.Write(this.InitialDefenders);
			w.Write(this.CapturedAttackerUnits.Count);
			foreach (WorkingUnit workingUnit3 in this.CapturedAttackerUnits)
			{
				w.Write(workingUnit3.ID);
			}
			w.Write(this.CapturedDefenderUnits.Count);
			foreach (WorkingUnit workingUnit4 in this.CapturedDefenderUnits)
			{
				w.Write(workingUnit4.ID);
			}
			if (this.RetreatingPlayer == null)
			{
				w.Write(false);
				return;
			}
			w.Write(true);
			w.Write(this.RetreatingPlayer);
		}

		public WorkingStack GetPlayerstack()
		{
			if (this.AttackerRealm == this.Game.PlayerRealm)
			{
				return this.Attacker;
			}
			return this.Defender;
		}

		private void CurrentBattleMap_OnTileMouseover(Point Tile)
		{
			if (!this.Game.GameCore.CurrentBattleMap.TileInsideBounds(Tile))
			{
				if (this.OnTileMouseover != null)
				{
					this.OnTileMouseover(new Point(-1, -1));
				}
				GameBase gameCore = this.Game.GameCore;
				string text = "TooltipChanged";
				object[] array = new object[1];
				gameCore.FireEvent(text, array);
				return;
			}
			this.Game.GameCore.FireEvent("TooltipChanged", new object[] { this.GetTooltip(Tile) });
			BattleTile tile = this.Game.GameCore.CurrentBattleMap.GetTile(Tile);
			if (tile == null)
			{
				return;
			}
			WorkingUnit unit = tile.Unit;
			if (unit == null && this.OnTileMouseover != null)
			{
				this.OnTileMouseover(Tile);
			}
			if (unit != null && this.OnUnitMouseover != null)
			{
				this.OnUnitMouseover(unit);
			}
		}

		private List<GameText> GetTooltip(Point p)
		{
			BattleTile tile = this.Game.GameCore.CurrentBattleMap.GetTile(p);
			if (tile == null)
			{
				return new List<GameText>();
			}
			if (tile.Terrain == null)
			{
				return new List<GameText>();
			}
			GameText gameText = GameText.CreateLocalised("FORMAT_BATTLETILE", new object[] { tile.Terrain.BaseType.CombatMoveCost });
			GameText gameText2 = GameText.CreateLocalised("UNITNAMETEMPLATE", new object[0]);
			gameText2.AddChildText(GameText.CreateLocalised(tile.Terrain.DisplayName, new object[0]));
			if (tile.HasTown())
			{
				gameText2.AddChildText(GameText.CreateLocalised("FEATURE_TOWN", new object[0]));
			}
			gameText.AddChildText(gameText2);
			List<GameText> list = new List<GameText>();
			list.Add(gameText);
			if (this.VictoryPointOwnership.ContainsKey(p))
			{
				list.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				list.Add(GameText.CreateLocalised("TEXT_VP", new object[0]));
			}
			list.AddRange(tile.GetTerrainEffects());
			return list;
		}

		private void CurrentBattleMap_OnTileMouseClick(Point Tile, MouseButton Button)
		{
			if (!this.Game.GameCore.CurrentBattleMap.TileInsideBounds(Tile))
			{
				if (this.OnMapClicked != null)
				{
					this.OnMapClicked(Button);
				}
				return;
			}
			BattleTile tile = this.Game.GameCore.CurrentBattleMap.GetTile(Tile);
			if (tile == null)
			{
				return;
			}
			WorkingUnit unit = tile.Unit;
			if (unit == null && this.OnTileClicked != null)
			{
				this.OnTileClicked(Tile, Button);
			}
			if (unit != null && this.OnUnitClicked != null)
			{
				this.OnUnitClicked(unit, Button);
			}
		}

		private void CurrentBattleMap_OnTileMouseUp(Point Tile, MouseButton Button)
		{
			if (!this.Game.GameCore.CurrentBattleMap.TileInsideBounds(Tile))
			{
				return;
			}
			if (this.Game.GameCore.CurrentBattleMap.DraggingUnit)
			{
				bool flag;
				if (this.Attacker.Owner == this.Game.PlayerRealm)
				{
					flag = this.Game.GameCore.CurrentBattleMap.AttackerTiles.Contains(Tile);
				}
				else
				{
					flag = this.Game.GameCore.CurrentBattleMap.DefenderDeployTiles.Contains(Tile);
					if (flag)
					{
						if (this.Game.GameCore.CurrentBattleMap.DragUnit.MoveType == MoveTypes.Air && this.VictoryPointOwnership.ContainsKey(Tile))
						{
							flag = false;
						}
						WorkingUnit unit = this.Game.GameCore.CurrentBattleMap.GetTile(Tile).Unit;
						if (unit != null && unit.MoveType == MoveTypes.Air && this.VictoryPointOwnership.ContainsKey(this.Game.GameCore.CurrentBattleMap.DragUnit.BattleData.BattleLocation))
						{
							flag = false;
						}
					}
				}
				if (this.Node.NodeType == PathNodeTypes.Land)
				{
					if (this.Game.GameCore.CurrentBattleMap.DragUnit.MoveType == MoveTypes.Land && this.Game.GameCore.CurrentBattleMap.GetTile(Tile).Terrain.BaseType.IsAnyType(new string[] { "sea" }))
					{
						flag = false;
					}
					if (this.Game.GameCore.CurrentBattleMap.DragUnit.MoveType == MoveTypes.Sea && !this.Game.GameCore.CurrentBattleMap.GetTile(Tile).Terrain.BaseType.IsAnyType(new string[] { "sea" }))
					{
						flag = false;
					}
				}
				if (!flag)
				{
					this.SetUnitTile(this.Game.GameCore.CurrentBattleMap.DragUnit, this.Game.GameCore.CurrentBattleMap.DragUnit.BattleData.BattleLocation);
				}
				else
				{
					WorkingUnit unit2 = this.Game.GameCore.CurrentBattleMap.GetTile(Tile).Unit;
					if (unit2 != null)
					{
						this.SetUnitTile(unit2, this.Game.GameCore.CurrentBattleMap.DragUnit.BattleData.BattleLocation);
					}
					this.SetUnitTile(this.Game.GameCore.CurrentBattleMap.DragUnit, Tile);
					this.Game.GameCore.FireEvent("UnitDeployPositionChanged", new object[] { this.Game.GameCore.CurrentBattleMap.DragUnit });
				}
				this.Game.GameCore.CurrentBattleMap.EndUnitDrag();
			}
		}

		private void CurrentBattleMap_OnTileMouseDown(Point Tile, MouseButton Button)
		{
			if (!this.Game.GameCore.CurrentBattleMap.TileInsideBounds(Tile))
			{
				return;
			}
			if (this.Game.GameCore.CurrentBattleMap.CurrentMode == BattleMapModes.Deploy)
			{
				WorkingUnit unit = this.Game.GameCore.CurrentBattleMap.GetTile(Tile).Unit;
				if (unit == null)
				{
					return;
				}
				if (unit.OwnerRealm != this.Game.PlayerRealm)
				{
					return;
				}
				if (unit.Class == UnitClasses.Fort)
				{
					return;
				}
				this.Game.GameCore.CurrentBattleMap.BeginUnitDrag(unit);
			}
		}

		private void SetUnitTile(WorkingUnit Unit, Point Tile)
		{
			if (this.Game.GameCore.CurrentBattleMap.TileInsideBounds(Unit.BattleData.BattleLocation) && this.Game.GameCore.CurrentBattleMap.GetTile(Unit.BattleData.BattleX, Unit.BattleData.BattleY).UnitID == Unit.ID)
			{
				this.Game.GameCore.CurrentBattleMap.GetTile(Unit.BattleData.BattleX, Unit.BattleData.BattleY).UnitID = -1;
			}
			Unit.BattleData.SetTile(Tile);
			this.Game.GameCore.CurrentBattleMap.GetTile(Tile.X, Tile.Y).UnitID = Unit.ID;
		}

		public UnitActionData GetUnitActions(WorkingUnit Unit)
		{
			UnitActionData unitActionData = new UnitActionData(Unit);
			unitActionData.Targets = new Dictionary<WorkingUnit, List<CombatAction>>();
			unitActionData.MovementCosts = this.Map.GetMovementTargets(Unit);
			if (this.Game.GameCore.UIActionAllowed("TacticalUnitAttack"))
			{
				unitActionData.TilesinRange = this.Map.GetRangeTiles(Unit);
				foreach (WorkingUnit workingUnit in this.Attacker.Units)
				{
					List<CombatAction> actionsOnTarget = this.GetActionsOnTarget(Unit, workingUnit, unitActionData);
					if (actionsOnTarget.Count > 0)
					{
						unitActionData.Targets.Add(workingUnit, actionsOnTarget);
					}
				}
				using (IEnumerator<WorkingUnit> enumerator2 = this.Defender.Units.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						WorkingUnit workingUnit2 = enumerator2.Current;
						List<CombatAction> actionsOnTarget2 = this.GetActionsOnTarget(Unit, workingUnit2, unitActionData);
						if (actionsOnTarget2.Count > 0)
						{
							unitActionData.Targets.Add(workingUnit2, actionsOnTarget2);
						}
					}
					return unitActionData;
				}
			}
			unitActionData.TilesinRange = new List<Point>();
			return unitActionData;
		}

		private List<CombatAction> GetActionsOnTarget(WorkingUnit Unit, WorkingUnit Target, UnitActionData UnitActions)
		{
			List<CombatAction> list = new List<CombatAction>();
			if (!Unit.BattleData.CanFight)
			{
				return list;
			}
			if (Unit.OwnerRealmID == Target.OwnerRealmID)
			{
				if (Unit.HasStatus("Healing", new object[] { Target }) && Target.Health < 100 && UnitActions.TilesinRange.Contains(Target.BattleData.BattleLocation))
				{
					list.Add(CombatAction.Heal);
				}
			}
			else if (this.UnitsAdjacent(Unit, Target))
			{
				if (Unit.Attack.GetValue(Target) > 0)
				{
					list.Add(CombatAction.MeleeAttack);
				}
			}
			else
			{
				if (Unit.RangedAttack.GetValue(Target) > 0 && this.Map.GetAdjacentEnemies(Unit.BattleData.BattleLocation, Unit.OwnerRealmID).Count == 0 && UnitActions.TilesinRange.Contains(Target.BattleData.BattleLocation))
				{
					list.Add(CombatAction.RangedAttack);
				}
				List<Point> adjacentTiles = this.Game.GameCore.Data.CombatMap.GetAdjacentTiles(Target.BattleData.BattleLocation.X, Target.BattleData.BattleLocation.Y);
				if (adjacentTiles.Count((Point x) => UnitActions.MovementCosts.ContainsKey(x) && this.Map.GetTile(x).Unit == null) > 0)
				{
					if (Unit.Class == UnitClasses.Cavalry)
					{
						bool flag = true;
						if (Target.Class == UnitClasses.Fort)
						{
							flag = false;
						}
						if (Target.HasStatus("NegateCharge", new object[0]))
						{
							flag = false;
						}
						if (Unit.HasStatus("ClassAbilitiesBlocked", new object[0]))
						{
							flag = false;
						}
						if (adjacentTiles.Count((Point x) => UnitActions.MovementCosts.ContainsKey(x) && this.Map.GetTile(x).Unit == null && !this.Map.GetTile(x).Terrain.IsNaval) == 0)
						{
							flag = false;
						}
						if (Unit.Attack.GetValue(Target) > 0)
						{
							if (flag)
							{
								list.Add(CombatAction.ChargeAttack);
							}
							else
							{
								list.Add(CombatAction.Moveattack);
							}
						}
					}
					else if (Unit.Class != UnitClasses.Siege && Unit.Attack.GetValue(Target) > 0)
					{
						list.Add(CombatAction.Moveattack);
					}
				}
			}
			return list;
		}

		public bool UnitsAdjacent(WorkingUnit UnitA, WorkingUnit UnitB)
		{
			return this.Game.Data.CombatMap.TilesAdjacent(UnitA.BattleData.BattleLocation, UnitB.BattleData.BattleLocation);
		}

		private void DeployUnit(WorkingUnit Unit, Point DeployTile)
		{
			if (DeployTile.X == -1)
			{
				return;
			}
			Unit.CreateBattleData(this, true);
			this.SetUnitTile(Unit, DeployTile);
		}

		private void AutoDeployForts()
		{
			int num = 1;
			foreach (WorkingUnit workingUnit in this.Defender.Units.Where((WorkingUnit x) => x.Class == UnitClasses.Fort))
			{
				Point fortTile = this.Game.GameCore.CurrentBattleMap.GetFortTile(num++);
				this.DeployUnit(workingUnit, fortTile);
				workingUnit.BattleData.Sprite.Show();
			}
		}

		private void AutoDeployDefenders()
		{
			if (this.Node.Zone != null)
			{
				foreach (WorkingUnit workingUnit in this.Defender.Units.Where((WorkingUnit x) => x.Class != UnitClasses.Fort))
				{
					Point navalDeployTile = this.Game.GameCore.CurrentBattleMap.GetNavalDeployTile(DeployZone.Naval2);
					this.DeployUnit(workingUnit, navalDeployTile);
				}
			}
			if (this.Node.Province != null)
			{
				foreach (WorkingUnit workingUnit2 in this.Defender.Units.Where((WorkingUnit x) => x.Class != UnitClasses.Fort))
				{
					Point defenderDeployTile = this.Game.GameCore.CurrentBattleMap.GetDefenderDeployTile(workingUnit2.Class == UnitClasses.Naval);
					this.DeployUnit(workingUnit2, defenderDeployTile);
				}
			}
		}

		private void AutoDeployAttackers()
		{
			if (this.Node.Zone != null)
			{
				foreach (WorkingUnit workingUnit in this.Attacker.Units.Where((WorkingUnit x) => x.Class != UnitClasses.Fort))
				{
					Point navalDeployTile = this.Game.GameCore.CurrentBattleMap.GetNavalDeployTile(DeployZone.Naval1);
					this.DeployUnit(workingUnit, navalDeployTile);
				}
			}
			if (this.Node.Province != null)
			{
				foreach (WorkingUnit workingUnit2 in this.Attacker.Units.Where((WorkingUnit x) => x.Class != UnitClasses.Fort))
				{
					Point attackerDeployTile = this.Game.GameCore.CurrentBattleMap.GetAttackerDeployTile(workingUnit2);
					this.DeployUnit(workingUnit2, attackerDeployTile);
				}
			}
		}

		public void EndDeployPhase()
		{
			this.Map.SetMode(BattleMapModes.Default);
			this.DeployPhase = false;
			foreach (WorkingUnit workingUnit in this.Attacker.Units)
			{
				workingUnit.ResetBattleData();
				this.CheckWaterStatus(workingUnit);
				workingUnit.BattleData.Sprite.Show();
			}
			foreach (WorkingUnit workingUnit2 in this.Defender.Units)
			{
				workingUnit2.ResetBattleData();
				this.CheckWaterStatus(workingUnit2);
				this.UpdateShieldWall(workingUnit2);
				workingUnit2.BattleData.Sprite.Show();
			}
			if (this.ActiveStack.Owner.AIPlayer != null)
			{
				this.BeginAITurn();
			}
			this.Game.GameCore.FireEvent("BattleDeployFinished", new object[0]);
		}

		public void RequestEndTurn(WorkingRealm Realm, bool IgnoreReatreat = false)
		{
			if (this.ActiveStack.Owner != Realm)
			{
				return;
			}
			if (IgnoreReatreat || this.RetreatingPlayer == null)
			{
				this.UpdateAllFlags();
				this.TurnCounter.AdvanceTurn();
				return;
			}
			WorkingStack workingStack = this.Defender;
			if (this.Attacker.Owner.Name == this.RetreatingPlayer)
			{
				workingStack = this.Attacker;
			}
			this.Game.GameCore.FireEvent("ArmyRetreated", new object[]
			{
				workingStack,
				workingStack == this.Defender
			});
			if (this.Attacker == workingStack)
			{
				this.DeclareWinner(this.Defender);
				return;
			}
			this.DeclareWinner(this.Attacker);
		}

		private void UpdateAllFlags()
		{
			foreach (WorkingUnit workingUnit in this.Attacker.Units)
			{
				workingUnit.UpdateBattleFlags();
			}
			foreach (WorkingUnit workingUnit2 in this.Defender.Units)
			{
				workingUnit2.UpdateBattleFlags();
			}
			foreach (WorkingUnit workingUnit3 in this.Attacker.Units)
			{
				workingUnit3.ResetBattleData();
			}
			foreach (WorkingUnit workingUnit4 in this.Defender.Units)
			{
				workingUnit4.ResetBattleData();
			}
		}

		internal int GetMaxTurns()
		{
			int num = 12;
			if (this.Node.Province != null)
			{
				this.Node.Province.ModifyBattleTurns(ref num);
				int num2 = this.Map.DefenderTiles.Count;
				num2 -= 400;
				if (num2 < 0)
				{
					num2 = 0;
				}
				num += num2 / 100;
			}
			return num;
		}

		public void Update(float ElapsedTime)
		{
			if (this.Winner != null)
			{
				return;
			}
			if (this.ActiveStack.Owner.AIPlayer != null)
			{
				this.ActiveStack.Owner.AIPlayer.ActionManager.CheckForBattleActions();
			}
			if (this.CurrentAction != null)
			{
				this.CurrentAction.Update(ElapsedTime);
				if (this.CurrentAction.Completed)
				{
					this.CurrentAction.UpdateUnits();
					this.CurrentAction.Dispose();
					this.CurrentAction = null;
					this.UpdateShieldWallStatus();
					this.Game.GameCore.FireEvent("BattleActionCompleted", new object[0]);
				}
			}
			foreach (WorkingUnit workingUnit in this.Attacker.Units)
			{
				if (workingUnit.BattleData != null)
				{
					workingUnit.BattleData.Update(ElapsedTime);
				}
			}
			foreach (WorkingUnit workingUnit2 in this.Defender.Units)
			{
				if (workingUnit2.BattleData != null)
				{
					workingUnit2.BattleData.Update(ElapsedTime);
				}
			}
			foreach (FloatingSpriteData floatingSpriteData in this.CapturedFloaties.ToList<FloatingSpriteData>())
			{
				floatingSpriteData.Update(ElapsedTime);
				if (floatingSpriteData.Complete)
				{
					floatingSpriteData.Dispose();
					this.CapturedFloaties.Remove(floatingSpriteData);
				}
			}
		}

		public void RequestMoveUnit(WorkingUnit Unit, Point TargetTile, UnitActionData UnitActions)
		{
			this.CurrentAction = new TacticalActionManager(this.Game, this, Unit, null, TargetTile, CombatAction.Move, UnitActions);
		}

		public void RequestAttackUnit(WorkingUnit Unit, WorkingUnit Target, UnitActionData UnitActions, CombatAction ActionType)
		{
			this.CurrentAction = new TacticalActionManager(this.Game, this, Unit, Target, Point.Empty, ActionType, UnitActions);
		}

		public bool UnitCanRetreat(WorkingUnit Unit)
		{
			if (Unit == null)
			{
				throw new ArgumentException();
			}
			return !Unit.Disabled && this.GetRetreatTile(Unit, Unit.BattleData.BattleLocation, 0).X != -1;
		}

		public void RetreatUnitHex(WorkingUnit Unit, WorkingUnit OtherUnit, bool AllowCapture)
		{
			if (Unit == null)
			{
				throw new ArgumentException();
			}
			if (OtherUnit == null)
			{
				throw new ArgumentException();
			}
			if (Unit.BattleData == null)
			{
				return;
			}
			GameText gameText = GameText.CreateLocalised("FORMAT_BATTLELOG_RETREAT", new object[0]);
			gameText.AddChildText(GameText.CreateLocalised(Unit.OwnerRealm.DisplayName, new object[0]));
			gameText.AddChildText(GameText.CreateLocalised(Unit.DisplayName, new object[0]));
			Unit.Game.GameCore.FireEvent("BattleLogEvent", new object[] { gameText });
			Point point;
			if (OtherUnit.BattleData == null)
			{
				point = OtherUnit.LocationWhenDisabled;
			}
			else
			{
				point = OtherUnit.BattleData.BattleLocation;
			}
			int direction = this.Game.GameCore.Data.CombatMap.GetDirection(point, Unit.BattleData.BattleLocation);
			if (direction == -1)
			{
				return;
			}
			Point retreatTile = this.GetRetreatTile(Unit, Unit.BattleData.BattleLocation, direction);
			if (retreatTile.X == -1)
			{
				if (AllowCapture)
				{
					this.CaptureUnit(Unit, false);
					if (OtherUnit.BattleData != null)
					{
						OtherUnit.BattleData.RecordCapture();
						return;
					}
				}
			}
			else
			{
				if (AllowCapture)
				{
					this.GiveUnitDisorder(Unit, 1);
				}
				this.TeleportUnit(Unit, retreatTile);
				OtherUnit.HandleRetreatCaused(Unit, !AllowCapture);
			}
		}

		private Point GetRetreatTile(WorkingUnit Unit, Point Tile, int RetreatDirection)
		{
			Point point = this.Game.GameCore.Data.CombatMap.GetTileInDirection(Tile, RetreatDirection);
			if (this.TileValidForRetreat(Unit, point))
			{
				return point;
			}
			int num = RetreatDirection + 1;
			if (num > 5)
			{
				num -= 6;
			}
			RetreatDirection--;
			if (RetreatDirection < 0)
			{
				RetreatDirection += 6;
			}
			point = this.Game.GameCore.Data.CombatMap.GetTileInDirection(Tile, RetreatDirection);
			Point point2 = this.Game.GameCore.Data.CombatMap.GetTileInDirection(Tile, num);
			if (this.TileValidForRetreat(Unit, point))
			{
				return point;
			}
			if (this.TileValidForRetreat(Unit, point2))
			{
				return point2;
			}
			num++;
			if (num > 5)
			{
				num -= 6;
			}
			RetreatDirection--;
			if (RetreatDirection < 0)
			{
				RetreatDirection += 6;
			}
			point = this.Game.GameCore.Data.CombatMap.GetTileInDirection(Tile, RetreatDirection);
			point2 = this.Game.GameCore.Data.CombatMap.GetTileInDirection(Tile, num);
			if (this.TileValidForRetreat(Unit, point))
			{
				return point;
			}
			if (this.TileValidForRetreat(Unit, point2))
			{
				return point2;
			}
			return new Point(-1, -1);
		}

		private bool TileValidForRetreat(WorkingUnit Unit, Point TargetTile)
		{
			BattleTile tile = this.Map.GetTile(TargetTile);
			if (tile == null)
			{
				return false;
			}
			if (!tile.InsideBattle)
			{
				return false;
			}
			if (tile.Unit != null)
			{
				return false;
			}
			if (tile.TileData.VictoryPoint)
			{
				VPData vpdata = null;
				this.VictoryPointOwnership.TryGetValue(TargetTile, out vpdata);
				if (vpdata != null && vpdata.OwnerID != Unit.OwnerRealmID)
				{
					return false;
				}
			}
			return (Unit.MoveType != MoveTypes.Land || !tile.Terrain.BaseType.IsAnyType(new string[] { "sea" })) && (Unit.MoveType != MoveTypes.Sea || tile.Terrain.IsNaval);
		}

		public void CaptureUnit(WorkingUnit Unit, bool Shackles)
		{
			GameText gameText = GameText.CreateLocalised("FORMAT_BATTLELOG_CAPTURED", new object[0]);
			gameText.AddChildText(GameText.CreateLocalised(Unit.OwnerRealm.DisplayName, new object[0]));
			gameText.AddChildText(GameText.CreateLocalised(Unit.DisplayName, new object[0]));
			Unit.Game.GameCore.FireEvent("BattleLogEvent", new object[] { gameText });
			this.Map.GetTile(Unit.BattleData.BattleLocation).UnitID = -1;
			Unit.IsPrisoner = true;
			if (Shackles)
			{
				FloatingSpriteData statusFloatie = Unit.BattleData.GetStatusFloatie(GameText.CreateLocalised("FLAGNAME55", new object[0]));
				if (statusFloatie != null)
				{
					this.CapturedFloaties.Add(statusFloatie);
				}
			}
			if (Unit.OwnerStack == this.Attacker)
			{
				this.Attacker.RemoveUnit(Unit);
				Unit.ClearBattleData();
				this.CapturedAttackerUnits.Add(Unit);
			}
			else
			{
				this.Defender.RemoveUnit(Unit);
				Unit.ClearBattleData();
				this.CapturedDefenderUnits.Add(Unit);
			}
			this.CheckForMilitaryVictory();
		}

		private void CheckForMilitaryVictory()
		{
			if (this.BattleEnded)
			{
				return;
			}
			int num = this.Attacker.Units.Count((WorkingUnit x) => !x.Disabled);
			int num2 = this.Defender.Units.Count((WorkingUnit x) => !x.Disabled && x.Class != UnitClasses.Fort);
			if (num == 0)
			{
				this.DeclareWinner(this.Defender);
				return;
			}
			if (num2 == 0)
			{
				this.DeclareWinner(this.Attacker);
			}
		}

		internal void SetInitialOwnership(List<Point> DefenderTiles)
		{
			this.VictoryPointOwnership = new Dictionary<Point, VPData>();
			foreach (Point point in DefenderTiles)
			{
				if (this.Game.GameCore.Data.CombatMap.TileData[point.X, point.Y].VictoryPoint)
				{
					VPData vpdata = new VPData();
					vpdata.OwnerID = this.Defender.OwnerID;
					this.VictoryPointOwnership.Add(point, vpdata);
					this.Map.GetTile(point).SetFlagSprite(this.Defender.Owner);
				}
			}
		}

		public int GetTotalVPCount()
		{
			return this.VictoryPointOwnership.Count<KeyValuePair<Point, VPData>>();
		}

		public int GetControlledVPCount(WorkingRealm Realm)
		{
			return this.VictoryPointOwnership.Count((KeyValuePair<Point, VPData> x) => x.Value.OwnerID == Realm.ID);
		}

		public WorkingRealm GetVPOwner(Point VPTile)
		{
			return this.Game.AllRealms[this.VictoryPointOwnership[VPTile].OwnerID];
		}

		public void KillUnit(WorkingUnit Unit)
		{
			this.Map.GetTile(Unit.BattleData.BattleLocation).UnitID = -1;
			if (Unit.OwnerStack == this.Attacker)
			{
				this.Attacker.RemoveUnit(Unit);
				this.DeadAttackerUnits.Add(Unit.ID);
			}
			else
			{
				if (Unit.Class == UnitClasses.Fort)
				{
					Unit.Health.Value = 100;
					this.Node.Province.RemoveFort();
				}
				this.Defender.RemoveUnit(Unit);
				this.DeadDefenderUnits.Add(Unit.ID);
			}
			Unit.ClearBattleData();
			this.Game.GameCore.FireEvent("UnitsUpdated", new object[0]);
			this.CheckForMilitaryVictory();
		}

		public void RemoveUnit(WorkingUnit Unit)
		{
			this.Map.GetTile(Unit.BattleData.BattleLocation).UnitID = -1;
			if (Unit.OwnerStack == this.Attacker)
			{
				this.Attacker.RemoveUnit(Unit);
				this.RemovedAttackerUnits.Add(Unit.ID);
			}
			else if (Unit.OwnerStack == this.Defender)
			{
				this.Defender.RemoveUnit(Unit);
				this.RemovedDefenderUnits.Add(Unit.ID);
			}
			Unit.ClearBattleData();
			this.Game.GameCore.FireEvent("UnitsUpdated", new object[0]);
			this.CheckForMilitaryVictory();
		}

		public void TeleportUnit(WorkingUnit Unit, Point Tile)
		{
			this.SetUnitTile(Unit, Tile);
		}

		public int GetUnprotectedVPCount(WorkingRealm Realm)
		{
			int num = 0;
			foreach (KeyValuePair<Point, VPData> keyValuePair in this.VictoryPointOwnership)
			{
				if (keyValuePair.Value.OwnerID == Realm.ID && this.Map.GetTile(keyValuePair.Key).Unit == null)
				{
					num++;
				}
			}
			return num;
		}

		public void SwitchCards()
		{
			List<CardEffect> activeCards = this.Attacker.Owner.BattleData.ActiveCards;
			List<CardEffect> activeCards2 = this.Defender.Owner.BattleData.ActiveCards;
			this.Attacker.Owner.BattleData.ActiveCards = activeCards2;
			this.Defender.Owner.BattleData.ActiveCards = activeCards;
			this.Game.GameCore.FireEvent("BattleCardsChanged", new object[0]);
		}

		public void SwitchAllegiance(WorkingUnit Unit)
		{
			if (Unit.OwnerStack == this.Defender)
			{
				this.Defender.RemoveUnit(Unit);
				if (this.RemovedAttackerUnits.Contains(Unit.ID))
				{
					this.RemovedAttackerUnits.Remove(Unit.ID);
				}
				else
				{
					this.RemovedDefenderUnits.Add(Unit.ID);
				}
				this.Attacker.AddUnit(Unit, false, false);
				Unit.OwnerRealmID = this.Attacker.OwnerID;
			}
			else if (Unit.OwnerStack == this.Attacker)
			{
				this.Attacker.RemoveUnit(Unit);
				if (this.RemovedDefenderUnits.Contains(Unit.ID))
				{
					this.RemovedDefenderUnits.Remove(Unit.ID);
				}
				else
				{
					this.RemovedAttackerUnits.Add(Unit.ID);
				}
				this.Defender.AddUnit(Unit, false, false);
				Unit.OwnerRealmID = this.Defender.OwnerID;
			}
			this.Game.GameCore.FireEvent("UnitsUpdated", new object[0]);
			this.CheckForMilitaryVictory();
		}

		public void AddNewUnit(WorkingUnit NewUnit, WorkingStack Stack, bool Deploy = true)
		{
			if (NewUnit.OwnerStackID != Stack.ID)
			{
				Stack.AddUnit(NewUnit, false, true);
			}
			NewUnit.OwnerRealmID = Stack.OwnerID;
			if (Deploy)
			{
				Point point;
				if (Stack == this.Attacker)
				{
					point = this.Map.GetAttackerDeployTile(NewUnit);
				}
				else
				{
					point = this.Map.GetDefenderDeployTile(NewUnit.Class == UnitClasses.Naval);
				}
				this.DeployUnit(NewUnit, point);
				NewUnit.BattleData.Sprite.Show();
				return;
			}
			NewUnit.CreateBattleData(this, true);
		}

		internal void BeginDefenderTurn()
		{
			foreach (WorkingUnit workingUnit in this.Defender.Units)
			{
				if (!workingUnit.Disabled)
				{
					workingUnit.BeginBattleTurn();
					workingUnit.RemoveNamedFlags("Shield Wall", 0);
					this.CheckWaterStatus(workingUnit);
					if (workingUnit.Class == UnitClasses.Wagon && this.DefenderRealm.AIPlayer == null)
					{
						WorkingUnit workingUnit2 = this.UnpackUnit(workingUnit);
						workingUnit2.ResetBattleMoves();
						workingUnit2.ResetBattleData();
						workingUnit2.BattleData.CanFight = false;
						workingUnit2.BattleData.UpdateImage();
					}
					else
					{
						workingUnit.BattleData.UpdateImage();
					}
				}
			}
			foreach (WorkingUnit workingUnit3 in this.Attacker.Units)
			{
				this.UpdateShieldWall(workingUnit3);
			}
			this.Defender.Owner.BattleData.CardPlayed = false;
			GameText gameText = GameText.CreateLocalised("FORMAT_BATTLELOG_TURN", new object[0]);
			gameText.AddChildText(GameText.CreateLocalised(this.Defender.Owner.DisplayName, new object[0]));
			this.Game.GameCore.FireEvent("BattleLogEvent", new object[] { gameText });
			this.Game.GameCore.FireEvent("BattleTurnStarted", new object[] { this.DefenderRealm });
		}

		internal void BeginAttackerTurn()
		{
			foreach (WorkingUnit workingUnit in this.Attacker.Units)
			{
				if (!workingUnit.Disabled && workingUnit.BattleData != null)
				{
					workingUnit.BeginBattleTurn();
					workingUnit.RemoveNamedFlags("Shield Wall", 0);
					this.CheckWaterStatus(workingUnit);
					if (workingUnit.Class == UnitClasses.Wagon && this.AttackerRealm.AIPlayer == null)
					{
						WorkingUnit workingUnit2 = this.UnpackUnit(workingUnit);
						workingUnit2.ResetBattleMoves();
						workingUnit2.ResetBattleData();
						workingUnit2.BattleData.CanFight = false;
						workingUnit2.BattleData.UpdateImage();
					}
					else
					{
						workingUnit.BattleData.UpdateImage();
					}
				}
			}
			foreach (WorkingUnit workingUnit3 in this.Defender.Units)
			{
				this.UpdateShieldWall(workingUnit3);
			}
			this.Attacker.Owner.BattleData.CardPlayed = false;
			GameText gameText = GameText.CreateLocalised("FORMAT_BATTLELOG_TURN", new object[0]);
			gameText.AddChildText(GameText.CreateLocalised(this.Attacker.Owner.DisplayName, new object[0]));
			this.Game.GameCore.FireEvent("BattleLogEvent", new object[] { gameText });
			this.Game.GameCore.FireEvent("BattleTurnStarted", new object[] { this.AttackerRealm });
		}

		private void CheckWaterStatus(WorkingUnit Unit)
		{
			if (this.Map.GetTile(Unit.BattleData.BattleLocation).Terrain.BaseType.IsAnyType(new string[] { "river", "sea" }) && !this.Map.GetTile(Unit.BattleData.BattleLocation).HasRoad() && (Unit.MoveType == MoveTypes.Land || Unit.HasStatus("IgnoreWaterEffects", new object[0])))
			{
				this.GiveUnitDisorder(Unit, 2);
			}
		}

		private void GiveUnitDisorder(WorkingUnit Unit, int Turns)
		{
			UnitFlag unitFlag = Unit.GetNamedFlag("Disorder");
			if (unitFlag == null)
			{
				unitFlag = UnitFlag.CreateNamedFlag(this.Game.GameCore, "Disorder");
				Unit.GrantFlag(unitFlag);
			}
			unitFlag.TurnCount = Math.Max(Turns, unitFlag.TurnCount);
		}

		public void UpdateShieldWallStatus()
		{
			foreach (WorkingUnit workingUnit in this.InactiveStack.Units)
			{
				this.UpdateShieldWall(workingUnit);
			}
		}

		private void UpdateShieldWall(WorkingUnit Unit)
		{
			if (Unit.Class != UnitClasses.Infantry)
			{
				return;
			}
			List<WorkingUnit> adjacentAllies = this.Map.GetAdjacentAllies(Unit);
			int num = adjacentAllies.Count((WorkingUnit x) => !x.Disabled && x.Class == UnitClasses.Infantry && !x.HasStatus("ClassAbilitiesBlocked", new object[0]));
			if (Unit.HasStatus("ClassAbilitiesBlocked", new object[0]))
			{
				num = 0;
			}
			if (num > 1)
			{
				num = 1;
			}
			int i = Unit.GetNamedFlagCount("Shield Wall");
			if (i > num)
			{
				Unit.RemoveNamedFlags("Shield Wall", i - num);
			}
			while (i < num)
			{
				Unit.GrantFlag(UnitFlag.CreateNamedFlag(this.Game.GameCore, "Shield Wall"));
				i++;
				this.Game.GameCore.FireEvent("ShieldWallEnabled", new object[] { Unit });
			}
		}

		internal void TileOccupied(Point TargetTile, WorkingUnit Unit)
		{
			BattleTile tile = this.Map.GetTile(TargetTile);
			if (tile == null)
			{
				return;
			}
			if (!tile.TileData.VictoryPoint)
			{
				return;
			}
			if (!this.VictoryPointOwnership.ContainsKey(TargetTile))
			{
				return;
			}
			if (this.VictoryPointOwnership[TargetTile].OwnerID != Unit.OwnerRealmID)
			{
				this.VictoryPointOwnership[TargetTile].OwnerID = Unit.OwnerRealmID;
				this.Map.GetTile(TargetTile).SetFlagSprite(Unit.OwnerRealm);
				this.TurnCounter.CheckVictoryTiles();
				this.Game.GameCore.FireEvent("VPOwnerChanged", new object[] { TargetTile });
				GameText gameText = GameText.CreateLocalised("FORMAT_BATTLELOG_VP", new object[0]);
				gameText.AddChildText(GameText.CreateLocalised(Unit.OwnerRealm.DisplayName, new object[0]));
				this.Game.GameCore.FireEvent("BattleLogEvent", new object[] { gameText });
				if (Unit.OwnerRealmID == this.AttackerRealm.ID && this.Node.Province.OwnerRealm != this.AttackerRealm)
				{
					if (!this.VictoryPointOwnership[TargetTile].AttackerBonusGranted)
					{
						this.GrantMoraleBonus(this.Attacker);
						this.GrantPlunder(Unit);
						this.VictoryPointOwnership[TargetTile].AttackerBonusGranted = true;
						return;
					}
				}
				else
				{
					if (!this.VictoryPointOwnership[TargetTile].DefenderBonusGranted)
					{
						this.GrantMoraleBonus(this.Defender);
					}
					this.VictoryPointOwnership[TargetTile].DefenderBonusGranted = true;
				}
			}
		}

		private void GrantPlunder(WorkingUnit Unit)
		{
			float num = (float)this.VictoryPointOwnership.Count;
			float num2 = (float)this.Node.Province.CurrentLoot;
			num2 /= num;
			this.AttackerRealm.Gold.Value += (int)num2;
			this.Node.Province.CurrentLoot -= (int)num2;
			this.Game.GameCore.FireEvent("ProvincePillaged", new object[]
			{
				this.AttackerRealm,
				this.Node.Province,
				(int)num2
			});
			if (Unit != null && !Unit.Disabled && Unit.BattleData != null)
			{
				GameText gameText = GameText.CreateLocalised("EFFECT_PLUNDER", new object[] { (int)num2 });
				Unit.BattleData.AddStatusFloatie(gameText);
			}
		}

		private void GrantMoraleBonus(WorkingStack Stack)
		{
			foreach (WorkingUnit workingUnit in Stack.Units.Where((WorkingUnit x) => !x.Disabled))
			{
				workingUnit.Morale.Value += 10;
			}
		}

		internal void DeclareWinner(WorkingStack Winner)
		{
			this.Winner = Winner;
			this.BattleEnded = true;
			if (Winner == this.Attacker)
			{
				this.Loser = this.Defender;
			}
			else
			{
				this.Loser = this.Attacker;
			}
			foreach (int num in this.RemovedAttackerUnits)
			{
				WorkingUnit workingUnit = this.Game.AllUnits[num];
				if (this.Defender.Units.Contains(workingUnit))
				{
					this.Defender.RemoveUnit(workingUnit);
					this.Attacker.AddUnit(workingUnit, false, false);
					workingUnit.OwnerRealmID = this.Attacker.OwnerID;
				}
				else if (!workingUnit.IsPrisoner)
				{
					this.Attacker.AddUnit(workingUnit, false, false);
				}
			}
			foreach (int num2 in this.RemovedDefenderUnits)
			{
				WorkingUnit workingUnit2 = this.Game.AllUnits[num2];
				if (this.Attacker.Units.Contains(workingUnit2))
				{
					this.Attacker.RemoveUnit(workingUnit2);
					this.Defender.AddUnit(workingUnit2, false, false);
					workingUnit2.OwnerRealmID = this.Defender.OwnerID;
				}
				else if (!workingUnit2.IsPrisoner)
				{
					this.Defender.AddUnit(workingUnit2, false, false);
				}
			}
			foreach (WorkingUnit workingUnit3 in this.Attacker.Units.Where((WorkingUnit x) => x.Class == UnitClasses.Wagon).ToList<WorkingUnit>())
			{
				if (!workingUnit3.Disabled && workingUnit3.BattleData != null && workingUnit3.CarriedUnit != null)
				{
					this.UnpackUnit(workingUnit3);
				}
			}
			foreach (WorkingUnit workingUnit4 in this.Defender.Units.Where((WorkingUnit x) => x.Class == UnitClasses.Wagon).ToList<WorkingUnit>())
			{
				if (!workingUnit4.Disabled && workingUnit4.BattleData != null && workingUnit4.CarriedUnit != null)
				{
					this.UnpackUnit(workingUnit4);
				}
			}
			foreach (WorkingUnit workingUnit5 in this.Attacker.Units)
			{
				workingUnit5.ClearBattleData();
			}
			foreach (WorkingUnit workingUnit6 in this.Defender.Units)
			{
				workingUnit6.ClearBattleData();
			}
			this.Map.EndBattle();
			this.Game.GameCore.FireEvent("TacticalBattleEnded", new object[] { this, Winner });
		}

		public void ClearDeadUnit(WorkingUnit Unit)
		{
			this.DeadAttackerUnits.Remove(Unit.ID);
			this.DeadDefenderUnits.Remove(Unit.ID);
			this.Game.DestroyUnit(Unit);
		}

		public void RetreatPlayer(WorkingRealm Realm)
		{
			this.RetreatingPlayer = Realm.Name;
			this.RequestEndTurn(Realm, true);
		}

		public void Cleanup()
		{
			this.Game.CleanupTacticalBattle(this);
		}

		internal void BeginAITurn()
		{
			this.ActiveStack.Owner.AIPlayer.TacticalManager.BeginTurn();
		}

		internal void GrantCard(WorkingRealm Realm, CardEffect Card)
		{
			Realm.BattleData.ActiveCards.Add(Card);
			this.Game.GameCore.FireEvent("BattleCardsChanged", new object[0]);
		}

		public WorkingUnit PackUnit(WorkingUnit Unit, UnitData UnitType)
		{
			WorkingUnit workingUnit = this.Game.CreateUnit(Unit.OwnerRealm.ID, UnitType);
			Point battleLocation = Unit.BattleData.BattleLocation;
			WorkingStack ownerStack = Unit.OwnerStack;
			ownerStack.PackUnit(Unit, workingUnit);
			this.AddNewUnit(workingUnit, ownerStack, false);
			this.RemoveUnit(Unit);
			workingUnit.BattleData.BattleX = battleLocation.X;
			workingUnit.BattleData.BattleY = battleLocation.Y;
			this.Game.GameCore.CurrentBattleMap.GetTile(battleLocation.X, battleLocation.Y).UnitID = workingUnit.ID;
			this.TileOccupied(battleLocation, workingUnit);
			workingUnit.BattleData.Sprite.Show();
			return workingUnit;
		}

		public WorkingUnit UnpackUnit(WorkingUnit Unit)
		{
			if (Unit.BattleData == null)
			{
				return null;
			}
			Point battleLocation = Unit.BattleData.BattleLocation;
			WorkingStack ownerStack = Unit.OwnerStack;
			WorkingUnit workingUnit = ownerStack.UnpackUnit(Unit);
			this.AddNewUnit(workingUnit, ownerStack, false);
			workingUnit.ResetBattleMoves();
			workingUnit.ResetBattleData();
			this.RemoveUnit(Unit);
			workingUnit.BattleData.BattleX = battleLocation.X;
			workingUnit.BattleData.BattleY = battleLocation.Y;
			this.Game.GameCore.CurrentBattleMap.GetTile(battleLocation.X, battleLocation.Y).UnitID = workingUnit.ID;
			this.TileOccupied(battleLocation, workingUnit);
			workingUnit.BattleData.Sprite.Show();
			return workingUnit;
		}

		public bool RealmPresent(string RealmName)
		{
			return this.Attacker.Owner.Name == RealmName || this.Defender.Owner.Name == RealmName;
		}

		internal void DisposeDeadUnits()
		{
			foreach (WorkingUnit workingUnit in this.DeadAttackers)
			{
				this.Game.DestroyUnit(workingUnit);
			}
			foreach (WorkingUnit workingUnit2 in this.DeadDefenders)
			{
				this.Game.DestroyUnit(workingUnit2);
			}
		}

		internal void UnitLeaveTile(BattleTile Tile, WorkingUnit Unit)
		{
			if (Tile.HasTown() && this.Node.Province.HasStatus("Fortified"))
			{
				Unit.Attack.OnRequestModifier -= this.Attack_OnRequestModifier;
				Unit.Defence.OnRequestModifier -= this.Defence_OnRequestModifier;
			}
		}

		private void Defence_OnRequestModifier(WorkingUnit Unit, WorkingUnit EnemyUnit, ref int Value)
		{
			Value++;
		}

		private void Attack_OnRequestModifier(WorkingUnit Unit, WorkingUnit EnemyUnit, ref int Value)
		{
			Value++;
		}

		internal void UnitEnterTile(BattleTile Tile, WorkingUnit Unit)
		{
			if (Tile.HasTown() && this.Node.Province.HasStatus("Fortified"))
			{
				Unit.Attack.OnRequestModifier += this.Attack_OnRequestModifier;
				Unit.Defence.OnRequestModifier += this.Defence_OnRequestModifier;
			}
		}

		internal void RemoveFortification()
		{
			foreach (WorkingUnit workingUnit in this.Attacker.Units)
			{
				workingUnit.Attack.OnRequestModifier -= this.Attack_OnRequestModifier;
				workingUnit.Defence.OnRequestModifier -= this.Defence_OnRequestModifier;
			}
			foreach (WorkingUnit workingUnit2 in this.Defender.Units)
			{
				workingUnit2.Attack.OnRequestModifier -= this.Attack_OnRequestModifier;
				workingUnit2.Defence.OnRequestModifier -= this.Defence_OnRequestModifier;
			}
		}

		internal void Dispose()
		{
			foreach (FloatingSpriteData floatingSpriteData in this.CapturedFloaties.ToList<FloatingSpriteData>())
			{
				floatingSpriteData.Dispose();
			}
			this.CapturedFloaties.Clear();
		}

		public WorkingStack Attacker;

		public WorkingStack Defender;

		public WorkingStack Winner;

		public WorkingStack Loser;

		public int NodeID;

		public int AttackerNodeID;

		private List<int> RemovedAttackerUnits;

		private List<int> RemovedDefenderUnits;

		private List<int> DeadAttackerUnits;

		private List<int> DeadDefenderUnits;

		public List<WorkingUnit> CapturedAttackerUnits;

		public List<WorkingUnit> CapturedDefenderUnits;

		public List<FloatingSpriteData> CapturedFloaties;

		public SovereigntyGame Game;

		public BattleMap Map;

		public BattleTurnController TurnCounter;

		public TacticalActionManager CurrentAction;

		public Dictionary<Point, VPData> VictoryPointOwnership;

		public bool BattleEnded;

		public bool RiverCrossing;

		public int InitialAttackers;

		public int InitialDefenders;

		public bool Loaded;

		public bool DeployPhase = true;

		public string RetreatingPlayer;
	}
}
