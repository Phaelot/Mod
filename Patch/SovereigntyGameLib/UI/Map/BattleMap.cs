// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.UI.Map.BattleMap
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenTK;
using OpenTK.Input;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Map;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI.Map
{
	public class BattleMap
	{
		private int[] HEX_Y_EVEN = new int[6] { -1, -1, 0, 1, 0, -1 };

		private int[] HEX_X_EVEN = new int[6] { 0, 1, 1, 0, -1, -1 };

		private int[] HEX_Y_ODD = new int[6] { -1, 0, 1, 1, 1, 0 };

		private int[] HEX_X_ODD = new int[6] { 0, 1, 1, 0, -1, -1 };

		private SovereigntyGame Game;

		private bool FadingIn;

		private bool FadingOut;

		public List<Point> AttackerTiles;

		public List<Point> DefenderTiles;

		public List<Point> AllTiles;

		public List<Point> DefenderDeployTiles;

		private Rectangle TileBounds;

		private RectangleF CameraBounds;

		private Dictionary<Point, BattleTile> Tiles;

		private DistanceMap AttackerDeployDistanceMap;

		private DistanceMap DefenderDeployDistanceMap;

		private float AlphaValue = -1f;

		public BattleMapModes CurrentMode;

		private Random RNG;

		private TacticalBattleController Battle;

		private List<MouseButton> ActiveButtons;

		internal bool DraggingMap;

		internal bool DraggingUnit;

		internal WorkingUnit DragUnit;

		private Vector3 DragMapStart;

		private Vector2 DragMouseStart;

		private Vector3 DragCameraStart;

		private float DragMinDistance = 10f;

		private float MapDistPerPixelX;

		private float MapDistPerPixelY;

		private GLSprite HighlightTileSprite;

		private Point LastMouseTile = new Point(-1, -1);

		private MapArrow MovementArrow;

		public WorkingUnit MovingUnit;

		public List<Point> DisengageTiles = new List<Point>();

		private List<Point> ValidCastTiles;

		public event TileDelegate OnTileMouseover;

		public event TileClickDelegate OnTileMouseDown;

		public event TileClickDelegate OnTileMouseUp;

		public event TileClickDelegate OnTileMouseClick;

		public BattleMap(SovereigntyGame Game, TacticalBattleController Battle, WorkingProvince AttackerProvince, WorkingZone AttackerZone, WorkingProvince DefenderProvince)
		{
			this.Game = Game;
			this.Battle = Battle;
			RNG = new Random();
			DefenderTiles = Game.Data.CombatMap.GetRegionTiles(DefenderProvince.RegionID);
			if (AttackerProvince != null)
			{
				AttackerTiles = Game.Data.CombatMap.GetattackerTiles(DefenderProvince.RegionID, AttackerProvince.RegionID, AttackerZone != null);
			}
			else
			{
				AttackerTiles = Game.Data.CombatMap.GetattackerTiles(DefenderProvince.RegionID, AttackerZone.RegionID, AttackerZone != null);
			}
			DefenderDeployTiles = new List<Point>();
			foreach (Point defenderTile in DefenderTiles)
			{
				if (Game.GameCore.Data.CombatMap.TileData[defenderTile.X, defenderTile.Y].Deployment == DeployZone.Defender)
				{
					DefenderDeployTiles.Add(defenderTile);
				}
			}
			AllTiles = new List<Point>();
			AllTiles.AddRange(DefenderTiles);
			AllTiles.AddRange(AttackerTiles);
			CreateTiles(AllTiles);
			ActiveButtons = new List<MouseButton>();
			Game.GameCore.RegisterEvent(UpdateGrid, "MapGridToggled");
			UpdateGrid("");
		}

		public BattleMap(SovereigntyGame Game, TacticalBattleController Battle, WorkingZone BattleZone)
		{
			this.Game = Game;
			this.Battle = Battle;
			DefenderTiles = Game.Data.CombatMap.GetRegionTiles(BattleZone.RegionID);
			AttackerTiles = Game.Data.CombatMap.GetRegionDeployTiles(BattleZone.RegionID, DeployZone.Naval1);
			AllTiles = new List<Point>();
			AllTiles.AddRange(DefenderTiles);
			CreateTiles(AllTiles);
			ActiveButtons = new List<MouseButton>();
			Game.GameCore.RegisterEvent(UpdateGrid, "MapGridToggled");
			UpdateGrid("");
		}

		private void UpdateGrid(string EventName, params object[] Args)
		{
			if (Game.GameCore.Settings.GetBooleanSetting("MapGrid"))
			{
				foreach (Point allTile in AllTiles)
				{
					Tiles[allTile].ShowGrid();
				}
				return;
			}
			foreach (Point allTile2 in AllTiles)
			{
				Tiles[allTile2].HideGrid();
			}
		}

		public BattleTile GetTile(Point p)
		{
			BattleTile value = null;
			Tiles.TryGetValue(p, out value);
			return value;
		}

		public BattleTile GetTile(int x, int y)
		{
			BattleTile value = null;
			Tiles.TryGetValue(new Point(x, y), out value);
			return value;
		}

		public Point GetFrontmostVP()
		{
			Point result = Point.Empty;
			float num = 8000f;
			foreach (Point defenderTile in DefenderTiles)
			{
				if (Tiles[defenderTile].TileData.VictoryPoint && !(DefenderDeployDistanceMap[defenderTile.X, defenderTile.Y] >= num))
				{
					result = defenderTile;
					num = DefenderDeployDistanceMap[defenderTile.X, defenderTile.Y];
				}
			}
			return result;
		}

		public List<Point> GetVPTiles()
		{
			List<Point> list = new List<Point>();
			foreach (Point defenderTile in DefenderTiles)
			{
				if (Tiles[defenderTile].TileData.VictoryPoint)
				{
					list.Add(defenderTile);
				}
			}
			return list;
		}

		public Point GetBackmostVP()
		{
			Point result = Point.Empty;
			float num = 0f;
			foreach (Point defenderTile in DefenderTiles)
			{
				if (Tiles[defenderTile].TileData.VictoryPoint && !(DefenderDeployDistanceMap[defenderTile.X, defenderTile.Y] <= num))
				{
					result = defenderTile;
					num = DefenderDeployDistanceMap[defenderTile.X, defenderTile.Y];
				}
			}
			return result;
		}

		public DistanceMap CreateDistanceMap(List<Point> OriginPoints)
		{
			DistanceMap distanceMap = new DistanceMap(TileBounds);
			distanceMap.MaxDist = 100f;
			distanceMap.GenerateMap(OriginPoints);
			return distanceMap;
		}

		public Dictionary<Point, float> GetValidMoves(WorkingUnit Unit)
		{
			if (!Unit.BattleData.CanMove)
			{
				return new Dictionary<Point, float>();
			}
			MovingUnit = Unit;
			DisengageTiles.Clear();
			List<Point> list = new List<Point>();
			list.Add(Unit.BattleData.BattleLocation);
			float num = Unit.CombatMoves;
			if (Unit.Class == UnitClasses.Siege && Unit.CanPack && Unit.OwnerRealm.AIPlayer == null)
			{
				UnitData unitByClass = Unit.OwnerRealm.UnitPurchaseManager.GetUnitByClass(UnitClasses.Wagon);
				num = unitByClass.Move;
			}
			DistanceMap distanceMap = new DistanceMap(TileBounds);
			distanceMap.MaxDist = num;
			distanceMap.GenerateMap(list, UnitMoveTileValid, UnitMoveTargetTileValid, UnitMoveCost);
			Dictionary<Point, float> dictionary = new Dictionary<Point, float>();
			for (int i = TileBounds.X; i < TileBounds.Right; i++)
			{
				for (int j = TileBounds.Y; j < TileBounds.Bottom; j++)
				{
					float num2 = distanceMap[i, j];
					if (num2 != -1f && num2 <= num)
					{
						dictionary.Add(new Point(i, j), num2);
					}
				}
			}
			return dictionary;
		}

		private float UnitMoveCost(Point Tile)
		{
			if (Tiles[Tile].Terrain == null)
			{
				return 1f;
			}
			if (Tiles[Tile].Terrain.BaseType.IsAnyType("sea"))
			{
				if (MovingUnit.Class != UnitClasses.Naval)
				{
					return 100f;
				}
			}
			else
			{
				if (MovingUnit.Class == UnitClasses.Naval && MovingUnit.CarriedUnit == null)
				{
					return 100f;
				}
				if (MovingUnit.Class == UnitClasses.Naval && MovingUnit.CarriedUnit != null)
				{
					return (int)MovingUnit.MaxCombatMoves;
				}
			}
			float Result = Tiles[Tile].Terrain.BaseType.CombatMoveCost;
			if ((MovingUnit.Class == UnitClasses.Cavalry || MovingUnit.Class == UnitClasses.Wagon) && !Tiles[Tile].Terrain.BaseType.IsAnyType("plains"))
			{
				Result *= 2f;
			}
			if (MovingUnit.Race == Races.Undead && Tiles[Tile].Terrain.BaseType.IsAnyType("swamp"))
			{
				Result = 1f;
			}
			if (Tiles[Tile].HasRoad())
			{
				Result = 0.6f;
			}
			if (MovingUnit.HasStatus("IgnoreTerrainMoveCost", Tiles[Tile].Terrain.BaseType))
			{
				Result = Math.Min(Result, 1f);
			}
			MovingUnit.ModifyMovementCost(Tiles[Tile], ref Result);
			return Result;
		}

		private bool UnitMoveTargetTileValid(Point Tile)
		{
			if (Tiles[Tile].Terrain.BaseType.IsAnyType("sea"))
			{
				if (MovingUnit.MoveType == MoveTypes.Land)
				{
					return false;
				}
			}
			else if (MovingUnit.MoveType == MoveTypes.Sea)
			{
				return false;
			}
			if (MovingUnit.BattleData.BattleLocation != Tile)
			{
				if (MovingUnit.TerrainIsBlocking(Tiles[Tile]))
				{
					return false;
				}
				if (Tiles[Tile].Terrain.BaseType.IsAnyType("river") && !Tiles[Tile].HasRoad() && MovingUnit.HasStatus("Bridging"))
				{
					return false;
				}
				if (MovingUnit.MoveType == MoveTypes.Air || MovingUnit.MoveType == MoveTypes.Phantom || MovingUnit.HasStatus("IgnoreNormalZOC"))
				{
					if (GetAdjacentEnemies(Tile, MovingUnit.OwnerRealmID).Count((WorkingUnit x) => x.HasStatus("HardZOC")) > 0)
					{
						return false;
					}
				}
				else
				{
					List<WorkingUnit> engagingEnemies = GetAdjacentEnemies(MovingUnit.BattleData.BattleLocation, MovingUnit.OwnerRealmID)
						.Where((WorkingUnit x) => x.MoveType != MoveTypes.Phantom && !x.Disabled).ToList();
					if (engagingEnemies.Count > 0)
					{
						List<WorkingUnit> destEnemies = GetAdjacentEnemies(Tile, MovingUnit.OwnerRealmID)
							.Where((WorkingUnit x) => x.MoveType != MoveTypes.Phantom && !x.Disabled).ToList();
						bool staysEngaged = false;
						foreach (WorkingUnit enemy in engagingEnemies)
						{
							if (destEnemies.Contains(enemy))
							{
								staysEngaged = true;
								break;
							}
						}
						if (!staysEngaged)
						{
							DisengageTiles.Add(Tile);
						}
					}
					else
					{
						if (GetAdjacentEnemies(Tile, MovingUnit.OwnerRealmID).Count((WorkingUnit x) => x.MoveType != MoveTypes.Phantom) > 0)
						{
							return false;
						}
					}
				}
			}
			return true;
		}

		public List<WorkingUnit> GetAdjacentEnemies(Point Tile, int OwnerID)
		{
			List<Point> adjacentTiles = Game.GameCore.Data.CombatMap.GetAdjacentTiles(Tile.X, Tile.Y);
			List<WorkingUnit> list = new List<WorkingUnit>();
			foreach (Point item in adjacentTiles)
			{
				if (TileInsideBounds(item) && Tiles[item].UnitID != -1 && Tiles[item].Unit.OwnerRealmID != OwnerID)
				{
					if (Tiles[item].Unit.BattleData == null)
					{
						Tiles[item].UnitID = -1;
					}
					else
					{
						list.Add(Tiles[item].Unit);
					}
				}
			}
			return list;
		}

		public List<WorkingUnit> GetEngagingEnemies(WorkingUnit Unit)
		{
			return GetAdjacentEnemies(Unit.BattleData.BattleLocation, Unit.OwnerRealmID)
				.Where((WorkingUnit x) => x.MoveType != MoveTypes.Phantom && !x.Disabled).ToList();
		}

		public bool IsDisengageMove(WorkingUnit Unit, Point TargetTile)
		{
			if (Unit.MoveType == MoveTypes.Air || Unit.MoveType == MoveTypes.Phantom || Unit.HasStatus("IgnoreNormalZOC"))
			{
				return false;
			}
			List<WorkingUnit> engaging = GetEngagingEnemies(Unit);
			if (engaging.Count == 0)
			{
				return false;
			}
			List<WorkingUnit> destEnemies = GetAdjacentEnemies(TargetTile, Unit.OwnerRealmID)
				.Where((WorkingUnit x) => x.MoveType != MoveTypes.Phantom && !x.Disabled).ToList();
			foreach (WorkingUnit enemy in engaging)
			{
				if (destEnemies.Contains(enemy))
				{
					return false;
				}
			}
			return true;
		}

		private bool UnitMoveTileValid(Point Tile)
		{
			if (!Tiles[Tile].InsideBattle)
			{
				return false;
			}
			if (Tiles[Tile].UnitID != -1 && (MovingUnit.MoveType == MoveTypes.Land || MovingUnit.MoveType == MoveTypes.Sea) && Tiles[Tile].Unit.OwnerRealmID != MovingUnit.OwnerRealmID)
			{
				return false;
			}
			return true;
		}

		public List<Point> GetRangeTiles(WorkingUnit Unit)
		{
			List<Point> list = new List<Point>();
			if (!Unit.BattleData.CanFight)
			{
				return list;
			}
			List<Point> list2 = new List<Point>();
			list2.Add(Unit.BattleData.BattleLocation);
			DistanceMap distanceMap = new DistanceMap(TileBounds);
			distanceMap.MaxDist = Unit.Range.GetValue();
			distanceMap.GenerateMap(list2);
			for (int i = TileBounds.X; i < TileBounds.Right; i++)
			{
				for (int j = TileBounds.Y; j < TileBounds.Bottom; j++)
				{
					float num = distanceMap[i, j];
					if (num != -1f)
					{
						list.Add(new Point(i, j));
					}
				}
			}
			list.Remove(Unit.BattleData.BattleLocation);
			return list;
		}

		public Dictionary<Point, float> GetMovementTargets(WorkingUnit Unit)
		{
			Dictionary<Point, float> validMoves = GetValidMoves(Unit);
			validMoves.Remove(Unit.BattleData.BattleLocation);
			return validMoves;
		}

		public void SetMoveOverlay(Dictionary<Point, float> ValidMoves, WorkingUnit Unit)
		{
			CurrentMode = BattleMapModes.Moving;
			foreach (Point allTile in AllTiles)
			{
				if (ValidMoves.ContainsKey(allTile))
				{
					if (DisengageTiles.Contains(allTile))
					{
						Tiles[allTile].SetOverlayColour(Color.FromArgb(50, 220, 80, 60));
					}
					else
					{
						Tiles[allTile].SetOverlayColour(Color.FromArgb(40, 85, 170, 255));
					}
					int num = 0;
					for (int i = 0; i < 6; i++)
					{
						int x = allTile.X;
						int y = allTile.Y;
						if (allTile.X % 2 == 1)
						{
							x += HEX_X_ODD[i];
							y += HEX_Y_ODD[i];
						}
						else
						{
							x += HEX_X_EVEN[i];
							y += HEX_Y_EVEN[i];
						}
						if (x >= 0 && y >= 0 && x < Game.GameCore.Data.CombatMap.TilesX && y < Game.GameCore.Data.CombatMap.TilesY && ValidMoves.ContainsKey(new Point(x, y)))
						{
							num += (int)Math.Pow(2.0, i);
						}
					}
					if (num > 0 && num < 63)
					{
						Tiles[allTile].SetOverlayBorder(num, Color.FromArgb(85, 170, 255));
					}
					else
					{
						Tiles[allTile].ClearOverlayBorder();
					}
					if (Unit.Class == UnitClasses.Siege && Unit.CanPack && Unit.OwnerRealm.AIPlayer == null && ValidMoves[allTile] > (float)Unit.CombatMoves)
					{
						Tiles[allTile].ShowWagonSprite();
					}
					if (Game.GameCore.ShowMoveOverlay)
					{
						Tiles[allTile].SetOverlayText($"{ValidMoves[allTile]:0.00}");
					}
				}
				else
				{
					Tiles[allTile].ClearOverlay();
					Tiles[allTile].ClearOverlayBorder();
					Tiles[allTile].ClearWagonSprite();
				}
			}
		}

		public void SetRangeOverlay(List<Point> ValidTiles, Dictionary<Point, float> ValidMoves)
		{
			foreach (Point allTile in AllTiles)
			{
				if (ValidTiles.Contains(allTile))
				{
					Tiles[allTile].SetSecondOverlayColour(Color.FromArgb(60, 200, 100, 50));
					int num = 0;
					for (int i = 0; i < 6; i++)
					{
						int x = allTile.X;
						int y = allTile.Y;
						if (allTile.X % 2 == 1)
						{
							x += HEX_X_ODD[i];
							y += HEX_Y_ODD[i];
						}
						else
						{
							x += HEX_X_EVEN[i];
							y += HEX_Y_EVEN[i];
						}
						if (x >= 0 && y >= 0 && x < Game.GameCore.Data.CombatMap.TilesX && y < Game.GameCore.Data.CombatMap.TilesY && ValidTiles.Contains(new Point(x, y)))
						{
							num += (int)Math.Pow(2.0, i);
						}
					}
					if (num > 0 && num < 63)
					{
						Tiles[allTile].SetSecondOverlayBorder(num, Color.FromArgb(200, 100, 50));
					}
				}
				else
				{
					Tiles[allTile].ClearSecondOverlayBorder();
					Tiles[allTile].ClearSecondOverlay();
				}
			}
		}

		public void SetMode(BattleMapModes Mode)
		{
			CurrentMode = Mode;
			switch (Mode)
			{
				case BattleMapModes.Deploy:
					if (Battle.Attacker.Owner == Game.PlayerRealm)
					{
						foreach (Point attackerTile in AttackerTiles)
						{
							Tiles[attackerTile].SetOverlayColour(Color.FromArgb(150, 50, 50, 200));
						}
						break;
					}
					{
						foreach (Point defenderDeployTile in DefenderDeployTiles)
						{
							Tiles[defenderDeployTile].SetOverlayColour(Color.FromArgb(150, 50, 50, 200));
						}
						break;
					}
				case BattleMapModes.Casting:
					{
						foreach (Point allTile in AllTiles)
						{
							if (ValidCastTiles.Contains(allTile))
							{
								Tiles[allTile].ClearOverlay();
								Tiles[allTile].ClearSecondOverlay();
								Tiles[allTile].ClearOverlayBorder();
								Tiles[allTile].ClearSecondOverlayBorder();
							}
							else
							{
								Tiles[allTile].SetOverlayColour(Color.FromArgb(80, 0, 0, 0));
							}
						}
						break;
					}
				case BattleMapModes.Default:
					{
						foreach (Point allTile2 in AllTiles)
						{
							Tiles[allTile2].ClearOverlay();
							Tiles[allTile2].ClearSecondOverlay();
							Tiles[allTile2].ClearWagonSprite();
							Tiles[allTile2].ClearOverlayBorder();
							Tiles[allTile2].ClearSecondOverlayBorder();
						}
						break;
					}
			}
		}

		public void CreateDeploymentDistanceMaps()
		{
			AttackerDeployDistanceMap = new DistanceMap(TileBounds);
			AttackerDeployDistanceMap.GenerateMap(Game.GameCore.Data.CombatMap.GetBorderTiles(AttackerTiles, Battle.Node.RegionID), (Point Tile) => AttackerTiles.Contains(Tile));
			List<Point> list = new List<Point>();
			foreach (Point defenderTile in DefenderTiles)
			{
				if (GetTile(defenderTile).TileData.VictoryPoint)
				{
					list.Add(defenderTile);
				}
			}
			DefenderDeployDistanceMap = new DistanceMap(TileBounds);
			DefenderDeployDistanceMap.GenerateMap(list, (Point Tile) => DefenderTiles.Contains(Tile));
		}

		public void DarkenTiles()
		{
			foreach (BattleTile value in Tiles.Values)
			{
				if (!value.InsideBattle)
				{
					List<Point> adjacentTiles = Game.GameCore.Data.CombatMap.GetAdjacentTiles(value.X, value.Y);
					if (adjacentTiles.Count((Point x) => GetTile(x)?.InsideBattle ?? false) > 0)
					{
						value.AddFog();
					}
					value.SetOverlayColour(Color.FromArgb(128, 0, 0, 0));
				}
			}
		}

		private void CreateTiles(List<Point> AllTiles)
		{
			TileBounds = Game.Data.CombatMap.GetBattleRegion(AllTiles);
			CameraBounds = Game.Data.CombatMap.GetCameraBounds(TileBounds);
			BattleTile.TileSizeX = 128f / Game.GameCore.Data.CombatMap.ScaleX;
			BattleTile.TileSizeY = 128f / Game.GameCore.Data.CombatMap.ScaleY;
			Tiles = new Dictionary<Point, BattleTile>();
			for (int i = TileBounds.X; i <= TileBounds.Right; i++)
			{
				for (int j = TileBounds.Y; j <= TileBounds.Bottom; j++)
				{
					BattleTile value = new BattleTile(Game, Game.GameCore.Data.CombatMap.TileData[i, j], i, j);
					Tiles.Add(new Point(i, j), value);
				}
			}
			foreach (BattleTile value2 in Tiles.Values)
			{
				value2.InsideBattle = AllTiles.Contains(new Point(value2.X, value2.Y));
			}
			float x = CameraBounds.Left + (CameraBounds.Right - CameraBounds.Left) / 2f;
			float z = CameraBounds.Top + (CameraBounds.Bottom - CameraBounds.Top) / 2f;
			Vector3 target = new Vector3(x, 150f, z);
			Game.GameCore.Camera.ViewportChanged();
			Game.GameCore.Camera.BeginAutoMove(target, 1f);
			Game.GameCore.Camera.AutoMoveCompleted += Camera_AutoMoveCompleted;
			Game.GameCore.Utilities.SpriteManager.InteractionDisabled = true;
			FadingIn = true;
		}

		private void Camera_AutoMoveCompleted()
		{
			Game.GameCore.Camera.AutoMoveCompleted -= Camera_AutoMoveCompleted;
			Game.GameCore.Camera.SetBounds(CameraBounds);
			Game.GameCore.Camera.MinZoomLevel = 45f;
			Game.GameCore.Camera.ZoomStep = 15f;
		}

		public void Dispose()
		{
			Game.GameCore.UnregisterEvent(UpdateGrid, "MapGridToggled");
			Game.GameCore.Camera.AutoMoveCompleted -= Camera_AutoMoveCompleted;
			if (MovementArrow != null)
			{
				MovementArrow.Dispose();
				MovementArrow = null;
			}
			foreach (BattleTile value in Tiles.Values)
			{
				value.Dispose();
			}
			Tiles.Clear();
			HighlightTileSprite.Dispose();
			HighlightTileSprite = null;
		}

		public void EndBattle()
		{
			FadingIn = false;
			FadingOut = true;
			Game.GameCore.Camera.SetBounds(new RectangleF(-500f, -500f, 5000f, 3400f));
			Game.GameCore.Camera.ZoomStep = 100f;
			Game.GameCore.Camera.AutoMoveCompleted += Camera_AutoMoveCompleted2;
			Vector3 camPos = Game.GameCore.Camera.CamPos;
			camPos.Y = 1000f;
			Game.GameCore.Camera.ViewportChanged();
			Game.GameCore.Camera.BeginAutoMove(camPos, 1f);
			Game.GameCore.Utilities.SpriteManager.InteractionDisabled = false;
		}

		private void Camera_AutoMoveCompleted2()
		{
			Game.GameCore.Camera.AutoMoveCompleted -= Camera_AutoMoveCompleted2;
			Game.GameCore.Camera.MinZoomLevel = 800f;
		}

		public void Render(float ElapsedTime)
		{
			if (FadingIn && AlphaValue < 1f)
			{
				AlphaValue += ElapsedTime * 2f;
				Game.GameCore.Utilities.BattleSpriteManager.SetGlobalAlpha(AlphaValue);
			}
			if (FadingOut && AlphaValue > 0f)
			{
				AlphaValue -= ElapsedTime * 2f;
				Game.GameCore.Utilities.BattleSpriteManager.SetGlobalAlpha(AlphaValue);
			}
			Game.GameCore.Utilities.BattleSpriteManager.Render();
			if (MovementArrow != null)
			{
				MovementArrow.Render();
			}
			if (Battle != null && Battle.Loaded)
			{
				Battle.Loaded = false;
				DarkenTiles();
			}
		}

		internal Point GetFortTile(int ID)
		{
			foreach (Point defenderTile in DefenderTiles)
			{
				int featureID = Game.GameCore.Data.CombatMap.TileData[defenderTile.X, defenderTile.Y].FeatureID;
				if (featureID != -1 && Game.GameCore.Data.CombatFeatureList[featureID].GetFortID() == ID)
				{
					return defenderTile;
				}
			}
			return new Point(-1, -1);
		}

		internal Point GetNavalDeployTile(DeployZone ZoneType)
		{
			foreach (BattleTile value in Tiles.Values)
			{
				if (value.TileData.Deployment == ZoneType && value.UnitID == -1)
				{
					return value.Location;
				}
			}
			return new Point(-1, -1);
		}

		internal Point GetDefenderDeployTile(bool Naval)
		{
			if (DefenderDeployDistanceMap == null)
			{
				CreateDeploymentDistanceMaps();
			}
			int num = int.MaxValue;
			List<Point> list = new List<Point>();
			foreach (Point defenderTile in DefenderTiles)
			{
				if (Tiles[defenderTile].UnitID == -1 && Tiles[defenderTile].TileData.Deployment == DeployZone.Defender && (!Tiles[defenderTile].Terrain.BaseType.IsAnyType("sea") || Naval) && (Tiles[defenderTile].Terrain.BaseType.IsAnyType("sea") || !Naval))
				{
					if (DefenderDeployDistanceMap[defenderTile.X, defenderTile.Y] == (float)num)
					{
						list.Add(defenderTile);
					}
					else if (DefenderDeployDistanceMap[defenderTile.X, defenderTile.Y] < (float)num)
					{
						num = (int)DefenderDeployDistanceMap[defenderTile.X, defenderTile.Y];
						list = new List<Point>();
						list.Add(defenderTile);
					}
				}
			}
			if (list.Count == 0)
			{
				throw new Exception("Could not place defender unit for battle");
			}
			return list[RNG.Next(list.Count)];
		}

		internal Point GetAttackerDeployTile(WorkingUnit Unit)
		{
			if (AttackerDeployDistanceMap == null)
			{
				CreateDeploymentDistanceMaps();
			}
			int num = int.MaxValue;
			List<Point> list = new List<Point>();
			foreach (Point attackerTile in AttackerTiles)
			{
				if (Tiles[attackerTile].UnitID == -1 && (!Tiles[attackerTile].Terrain.BaseType.IsAnyType("sea") || Unit.Class == UnitClasses.Naval) && (Tiles[attackerTile].Terrain.BaseType.IsAnyType("sea") || Unit.Class != UnitClasses.Naval))
				{
					if (AttackerDeployDistanceMap[attackerTile.X, attackerTile.Y] == (float)num)
					{
						list.Add(attackerTile);
					}
					else if (AttackerDeployDistanceMap[attackerTile.X, attackerTile.Y] < (float)num)
					{
						num = (int)AttackerDeployDistanceMap[attackerTile.X, attackerTile.Y];
						list = new List<Point>();
						list.Add(attackerTile);
					}
				}
			}
			if (list.Count == 0)
			{
				string text = "Unable to find valid attacker deploy tile.\r\n";
				text = text + "Battle location: " + Battle.Node.GetRegion().Name + "\r\n";
				text = text + "Attacker Province: " + Battle.AttackerNode.GetRegion().Name + "\r\n";
				text = text + "Attacker: " + Battle.AttackerRealm.Name + "\r\n";
				text = text + "Defender: " + Battle.DefenderRealm.Name + "\r\n";
				text += "Attacker Units: ";
				foreach (WorkingUnit unit in Battle.Attacker.Units)
				{
					text = text + unit.BaseName + ", ";
				}
				object obj = text;
				text = string.Concat(obj, "(Total: ", Battle.Attacker.Units.Count, ")\r\n");
				text += "Defender Units: ";
				foreach (WorkingUnit unit2 in Battle.Defender.Units)
				{
					text = text + unit2.BaseName + ", ";
				}
				object obj2 = text;
				text = string.Concat(obj2, "(Total: ", Battle.Defender.Units.Count, ")\r\n");
				string text2 = text;
				text = text2 + "Affected unit: " + Unit.BaseName + " (" + Unit.Class.ToString() + ")\r\n";
				throw new Exception(text);
			}
			return list[RNG.Next(list.Count)];
		}

		internal void HandleKeyDown(KeyboardKeyEventArgs e)
		{
			if (e.Key == Key.Up)
			{
				Game.GameCore.Camera.Forward = true;
			}
			if (e.Key == Key.Down)
			{
				Game.GameCore.Camera.Back = true;
			}
			if (e.Key == Key.Left)
			{
				Game.GameCore.Camera.Left = true;
			}
			if (e.Key == Key.Right)
			{
				Game.GameCore.Camera.Right = true;
			}
		}

		internal void HandleKeyUp(KeyboardKeyEventArgs e)
		{
			if (e.Key == Key.Up)
			{
				Game.GameCore.Camera.Forward = false;
			}
			if (e.Key == Key.Down)
			{
				Game.GameCore.Camera.Back = false;
			}
			if (e.Key == Key.Left)
			{
				Game.GameCore.Camera.Left = false;
			}
			if (e.Key == Key.Right)
			{
				Game.GameCore.Camera.Right = false;
			}
		}

		internal void BeginUnitDrag(WorkingUnit Unit)
		{
			DraggingUnit = true;
			DragUnit = Unit;
		}

		internal void EndUnitDrag()
		{
			DragUnit = null;
			DraggingUnit = false;
		}

		internal void HandleMouseMove(MouseMoveEventArgs e)
		{
			Vector3 terrainIntersect = Game.GameCore.Camera.GetTerrainIntersect(e.X, e.Y);
			Point tileAtPoint = Game.GameCore.Data.CombatMap.GetTileAtPoint(terrainIntersect);
			if (tileAtPoint != LastMouseTile)
			{
				LastMouseTile = tileAtPoint;
				SetHighlightPosition(tileAtPoint);
				if (this.OnTileMouseover != null)
				{
					this.OnTileMouseover(tileAtPoint);
				}
			}
			if (DraggingUnit)
			{
				DragUnit.BattleData.SetPosition(terrainIntersect.X, terrainIntersect.Z);
			}
			if (!DraggingMap && !DraggingUnit && ActiveButtons.Contains(MouseButton.Left))
			{
				Vector2 vector = new Vector2(e.X, e.Y);
				if ((vector - DragMouseStart).Length > DragMinDistance)
				{
					DraggingMap = true;
				}
			}
			if (DraggingMap && ActiveButtons.Contains(MouseButton.Left))
			{
				Vector2 vector2 = new Vector2(e.X, e.Y);
				Vector2 vector3 = vector2 - DragMouseStart;
				vector3.X *= MapDistPerPixelX;
				vector3.Y *= MapDistPerPixelY;
				Game.GameCore.Camera.SetPosition(DragCameraStart.X - vector3.X, Game.GameCore.Camera.CamPos.Y, DragCameraStart.Z - vector3.Y);
			}
		}

		private void SetHighlightPosition(Point Tile)
		{
			PointF scaledTileCoords = Game.GameCore.Data.CombatMap.GetScaledTileCoords(Tile.X, Tile.Y);
			if (HighlightTileSprite == null)
			{
				HighlightTileSprite = Game.GameCore.Utilities.BattleSpriteManager.CreateSprite("Data\\Images\\Combat\\Tiles\\Whitehex.png");
				HighlightTileSprite.SetSize(Game.GameCore.Data.CombatMap.TileSizeX, Game.GameCore.Data.CombatMap.TileSizeY);
				HighlightTileSprite.SetBlendColour(Color.FromArgb(128, 255, 255, 255));
			}
			HighlightTileSprite.SetPosition(scaledTileCoords.X, scaledTileCoords.Y);
		}

		internal void HandleMouseDown(MouseButtonEventArgs e)
		{
			if (!ActiveButtons.Contains(e.Button))
			{
				ActiveButtons.Add(e.Button);
				Vector3 terrainIntersect = Game.GameCore.Camera.GetTerrainIntersect(e.X, e.Y);
				Point tileAtPoint = Game.GameCore.Data.CombatMap.GetTileAtPoint(terrainIntersect);
				DragMapStart = terrainIntersect;
				DragMouseStart = new Vector2(e.X, e.Y);
				DragCameraStart = Game.GameCore.Camera.CamPos;
				Vector3 terrainIntersect2 = Game.GameCore.Camera.GetTerrainIntersect(0, 0);
				Vector3 terrainIntersect3 = Game.GameCore.Camera.GetTerrainIntersect(50, 50);
				MapDistPerPixelX = (terrainIntersect3.X - terrainIntersect2.X) / 50f;
				MapDistPerPixelY = (terrainIntersect3.Z - terrainIntersect2.Z) / 50f;
				if (this.OnTileMouseDown != null)
				{
					this.OnTileMouseDown(tileAtPoint, e.Button);
				}
			}
		}

		public void ForceReleaseMouse()
		{
			ActiveButtons.Remove(MouseButton.Left);
			DraggingMap = false;
		}

		internal void HandleMouseUp(MouseButtonEventArgs e)
		{
			if (ActiveButtons.Contains(e.Button))
			{
				ActiveButtons.Remove(e.Button);
				Vector3 terrainIntersect = Game.GameCore.Camera.GetTerrainIntersect(e.X, e.Y);
				Point tileAtPoint = Game.GameCore.Data.CombatMap.GetTileAtPoint(terrainIntersect);
				if (this.OnTileMouseUp != null)
				{
					this.OnTileMouseUp(tileAtPoint, e.Button);
				}
				if (!DraggingMap && this.OnTileMouseClick != null)
				{
					this.OnTileMouseClick(tileAtPoint, e.Button);
				}
				if (DraggingMap && e.Button == MouseButton.Left)
				{
					DraggingMap = false;
				}
			}
		}

		internal bool TileInsideBounds(Point Tile)
		{
			if (Tile.X < TileBounds.X)
			{
				return false;
			}
			if (Tile.Y < TileBounds.Y)
			{
				return false;
			}
			if (Tile.X > TileBounds.Right)
			{
				return false;
			}
			if (Tile.Y > TileBounds.Bottom)
			{
				return false;
			}
			return true;
		}

		public void ClearTargetSprites()
		{
			foreach (BattleTile value in Tiles.Values)
			{
				value.ClearTargetSprite();
			}
		}

		public bool PathIsBlocked(List<Point> Path)
		{
			foreach (Point item in Path)
			{
				if (GetTile(item).Terrain.BaseType.CombatBlocking)
				{
					return true;
				}
			}
			return false;
		}

		public List<Point> GetMovePath(Point StartTile, Point EndTile, UnitActionData UnitActions)
		{
			List<Point> list = new List<Point>();
			Point point = EndTile;
			while (point != StartTile)
			{
				list.Add(point);
				List<Point> adjacentTiles = Game.GameCore.Data.CombatMap.GetAdjacentTiles(point.X, point.Y);
				float num = 10000f;
				Point point2 = new Point(-1, -1);
				foreach (Point item in adjacentTiles)
				{
					if (item == StartTile)
					{
						point2 = item;
						break;
					}
					if (TileInsideBounds(item) && !list.Contains(item) && UnitActions.MovementCosts.ContainsKey(item) && !UnitActions.Unit.TerrainIsBlocking(Tiles[item]))
					{
						float num2 = UnitActions.MovementCosts[item];
						if (num2 < num)
						{
							num = num2;
							point2 = item;
						}
					}
				}
				if (point2.X == -1)
				{
					break;
				}
				point = point2;
			}
			list.Add(StartTile);
			list.Reverse();
			return list;
		}

		public void ClearArrow()
		{
			if (MovementArrow != null)
			{
				MovementArrow.Dispose();
				MovementArrow = null;
			}
		}

		public void ShowArrow(Point StartTile, Point EndTile, UnitActionData SelectedUnitActions)
		{
			ClearArrow();
			List<Point> movePath = GetMovePath(StartTile, EndTile, SelectedUnitActions);
			if (movePath.Count < 2)
			{
				return;
			}
			List<Vector3> list = new List<Vector3>();
			foreach (Point item in movePath)
			{
				PointF scaledTileCoords = Game.GameCore.Data.CombatMap.GetScaledTileCoords(item.X, item.Y);
				list.Add(new Vector3(scaledTileCoords.X, 0f, scaledTileCoords.Y));
			}
			MovementArrow = new MapArrow(list, Game.GameCore, Game.GameCore.Data.CombatMap.TileSizeX * 0.1f, Game.GameCore.Data.CombatMap.TileSizeX * 0.3f);
			MovementArrow.SetGreen();
		}

		public void ShowActionArrow(Point StartTile, Point EndTile, bool FriendlyAction, bool UsePathing, UnitActionData SelectedUnitActions)
		{
			ClearArrow();
			List<Point> list;
			if (UsePathing)
			{
				list = (list = GetMovePath(StartTile, EndTile, SelectedUnitActions));
			}
			else
			{
				list = new List<Point>();
				list.Add(StartTile);
				list.Add(EndTile);
			}
			List<Vector3> list2 = new List<Vector3>();
			foreach (Point item in list)
			{
				PointF scaledTileCoords = Game.GameCore.Data.CombatMap.GetScaledTileCoords(item.X, item.Y);
				list2.Add(new Vector3(scaledTileCoords.X, 0f, scaledTileCoords.Y));
			}
			MovementArrow = new MapArrow(list2, Game.GameCore, Game.GameCore.Data.CombatMap.TileSizeX * 0.1f, Game.GameCore.Data.CombatMap.TileSizeX * 0.3f);
			if (FriendlyAction)
			{
				MovementArrow.SetColour(Color.DeepSkyBlue);
			}
			else
			{
				MovementArrow.SetRed();
			}
			if (!UsePathing)
			{
				MovementArrow.CreateHeightOffsets();
			}
		}

		public void SetCastingMode(List<Point> ValidTiles)
		{
			ValidCastTiles = ValidTiles;
			SetMode(BattleMapModes.Casting);
		}

		public List<WorkingUnit> GetAdjacentAllies(WorkingUnit Unit)
		{
			List<Point> adjacentTiles = Game.GameCore.Data.CombatMap.GetAdjacentTiles(Unit.BattleData.BattleX, Unit.BattleData.BattleY);
			List<WorkingUnit> list = new List<WorkingUnit>();
			foreach (Point item in adjacentTiles)
			{
				if (TileInsideBounds(item) && Tiles[item].UnitID != -1 && Tiles[item].Unit.OwnerRealmID == Unit.OwnerRealmID)
				{
					if (Tiles[item].Unit.BattleData == null)
					{
						Tiles[item].UnitID = -1;
					}
					else
					{
						list.Add(Tiles[item].Unit);
					}
				}
			}
			return list;
		}
	}
}