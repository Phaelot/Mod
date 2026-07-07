using System;
using System.Collections.Generic;
using System.Drawing;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Controls;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI.Map
{
	public class BattleTile
	{
		public int UnitID
		{
			get
			{
				return this.m_UnitID;
			}
			set
			{
				if (value == this.m_UnitID)
				{
					return;
				}
				if (this.Unit != null)
				{
					this.HandleUnitLeave(this.Unit);
				}
				this.m_UnitID = value;
				if (this.Unit != null)
				{
					this.HandleUnitEnter(this.Unit);
				}
			}
		}

		public CombatTerrainData Terrain
		{
			get
			{
				if (this.TileData.TerrainID == -1)
				{
					return null;
				}
				return this.Game.GameCore.Data.CombatTerrainList[this.TileData.TerrainID];
			}
		}

		public WorkingUnit Unit
		{
			get
			{
				WorkingUnit workingUnit = null;
				this.Game.AllUnits.TryGetValue(this.m_UnitID, out workingUnit);
				return workingUnit;
			}
		}

		public BattleTile(SovereigntyGame Game, MapTileData TileData, int x, int y)
		{
			this.X = x;
			this.Y = y;
			this.Location = new Point(x, y);
			this.Game = Game;
			this.TileData = TileData;
			this.ActiveUnitFlags = new List<UnitFlag>();
			float num = 0f;
			if (TileData.TerrainID != -1)
			{
				string displayFilename = Game.GameCore.Data.CombatTerrainList[TileData.TerrainID].DisplayFilename;
				this.BaseSprite = Game.GameCore.Utilities.BattleSpriteManager.CreateIndexedSprite("Data\\Images\\Combat\\Tiles\\" + displayFilename, TileData.IndexValue, false, 128, 128);
				if (Game.GameCore.Data.CombatTerrainList[TileData.TerrainID].FeatureFilename != "")
				{
					string featureFilename = Game.GameCore.Data.CombatTerrainList[TileData.TerrainID].FeatureFilename;
					this.TerrainSprite = Game.GameCore.Utilities.BattleSpriteManager.CreateIndexedSprite("Data\\Images\\Combat\\Tiles\\" + featureFilename, TileData.FeatureIndexValue, false, 128, Game.GameCore.Data.CombatTerrainList[TileData.TerrainID].FeatureHeight);
					if (BattleTile.FeatureOffsets == null)
					{
						BattleTile.FeatureOffsets = new Dictionary<string, float>();
					}
					if (!BattleTile.FeatureOffsets.ContainsKey(featureFilename))
					{
						float num2 = (float)(Game.GameCore.Data.CombatTerrainList[TileData.TerrainID].FeatureHeight - 128);
						num2 /= Game.GameCore.Data.CombatMap.ScaleY;
						BattleTile.FeatureOffsets.Add(featureFilename, num2);
					}
					num = BattleTile.FeatureOffsets[featureFilename];
				}
			}
			else
			{
				string text = "LayerBaseDry.png";
				this.BaseSprite = Game.GameCore.Utilities.BattleSpriteManager.CreateIndexedSprite("Data\\Images\\Combat\\Tiles\\" + text, 0, false, 128, 128);
			}
			float num3 = 0f;
			if (TileData.FeatureID != -1)
			{
				string filename = Game.GameCore.Data.CombatFeatureList[TileData.FeatureID].Filename;
				this.FeatureSprite = Game.GameCore.Utilities.BattleSpriteManager.CreateSprite("Data\\Images\\Combat\\Tiles\\" + filename, false);
				if (BattleTile.FeatureOffsets == null)
				{
					BattleTile.FeatureOffsets = new Dictionary<string, float>();
				}
				if (!BattleTile.FeatureOffsets.ContainsKey(filename))
				{
					Bitmap bitmap = new Bitmap(Game.GameCore.Utilities.FileSystem.OpenFile("Data\\Images\\Combat\\Tiles\\" + filename, FileTypes.Application, FileModes.ReadOnly, true));
					float num4 = (float)(bitmap.Height - 128);
					num4 /= Game.GameCore.Data.CombatMap.ScaleY;
					BattleTile.FeatureOffsets.Add(filename, num4);
				}
				num3 = BattleTile.FeatureOffsets[filename];
			}
			if (TileData.RoadID > -1)
			{
				string filename2 = Game.GameCore.Data.CombatRoadList[TileData.RoadID].Filename;
				this.RoadSprite = Game.GameCore.Utilities.BattleSpriteManager.CreateIndexedSprite("Data\\Images\\Combat\\Tiles\\" + filename2, TileData.RoadValue, false, 128, 128);
			}
			if (TileData.VictoryPoint)
			{
				this.VPSprite = Game.GameCore.Utilities.BattleSpriteManager.CreateSprite("Data\\Images\\Combat\\Tiles\\Victoryhex.png", false);
				this.VPSprite.SetAlpha(0.5f);
			}
			int num5 = (int)Game.GameCore.Data.CombatMap.BorderValues[x, y];
			if (num5 > 0 && num5 < 63)
			{
				this.BorderSprite = Game.GameCore.Utilities.BattleSpriteManager.CreateIndexedSprite("Data\\Images\\Combat\\Tiles\\Border.png", num5, false, 128, 128);
			}
			PointF scaledTileCoords = Game.GameCore.Data.CombatMap.GetScaledTileCoords(x, y);
			this.BaseSprite.SetPosition(scaledTileCoords.X, scaledTileCoords.Y);
			this.BaseSprite.SetSize(BattleTile.TileSizeX, BattleTile.TileSizeY);
			this.BaseSprite.Batch.DisableFiltering();
			if (this.TerrainSprite != null)
			{
				float num6 = (float)Game.GameCore.Data.CombatTerrainList[TileData.TerrainID].FeatureHeight / Game.GameCore.Data.CombatMap.ScaleY;
				this.TerrainSprite.SetPosition(scaledTileCoords.X, scaledTileCoords.Y - num);
				this.TerrainSprite.SetSize(BattleTile.TileSizeX, num6);
				this.TerrainSprite.Batch.DisableFiltering();
			}
			if (this.FeatureSprite != null)
			{
				this.FeatureSprite.SetPosition(scaledTileCoords.X, scaledTileCoords.Y - num3);
				this.FeatureSprite.SetSize(BattleTile.TileSizeX, BattleTile.TileSizeY);
			}
			if (this.RoadSprite != null)
			{
				this.RoadSprite.SetPosition(scaledTileCoords.X, scaledTileCoords.Y);
				this.RoadSprite.SetSize(BattleTile.TileSizeX, BattleTile.TileSizeY);
			}
			if (this.VPSprite != null)
			{
				this.VPSprite.SetPosition(scaledTileCoords.X, scaledTileCoords.Y);
				this.VPSprite.SetSize(BattleTile.TileSizeX, BattleTile.TileSizeY);
			}
			if (this.BorderSprite != null)
			{
				this.BorderSprite.SetPosition(scaledTileCoords.X, scaledTileCoords.Y);
				this.BorderSprite.SetSize(BattleTile.TileSizeX, BattleTile.TileSizeY);
			}
			if (this.BorderFogSprite != null)
			{
				this.BorderFogSprite.SetPosition(scaledTileCoords.X, scaledTileCoords.Y);
				this.BorderFogSprite.SetSize(BattleTile.TileSizeX, BattleTile.TileSizeY);
				this.BorderFogSprite.Batch.DisableFiltering();
			}
			this.PlotX = scaledTileCoords.X;
			this.PlotY = scaledTileCoords.Y;
		}

		public List<GameText> GetTerrainEffects()
		{
			List<GameText> list = new List<GameText>();
			if (this.Terrain.BaseType.IsAnyType(new string[] { "hills" }))
			{
				list.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				list.Add(GameText.CreateLocalised("TERRAINEFFECT_ATTACK", new object[] { 1 }));
				list.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				list.Add(GameText.CreateLocalised("TERRAINEFFECT_DEFENCE", new object[] { 1 }));
			}
			if (this.Terrain.BaseType.IsAnyType(new string[] { "lt forest" }))
			{
				list.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				list.Add(GameText.CreateLocalised("TERRAINEFFECT_COVER", new object[] { 15 }));
				list.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				list.Add(GameText.CreateLocalised("TERRAINEFFECT_DEFENCE", new object[] { 1 }));
			}
			if (this.Terrain.BaseType.IsAnyType(new string[] { "mountain" }))
			{
				list.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				list.Add(GameText.CreateLocalised("TERRAINEFFECT_ATTACK", new object[] { 1 }));
				list.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				list.Add(GameText.CreateLocalised("TERRAINEFFECT_DEFENCE", new object[] { 2 }));
			}
			if (this.Terrain.BaseType.IsAnyType(new string[] { "old forest" }))
			{
				list.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				list.Add(GameText.CreateLocalised("TERRAINEFFECT_COVER", new object[] { 30 }));
				list.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				list.Add(GameText.CreateLocalised("TERRAINEFFECT_DEFENCE", new object[] { 1 }));
			}
			if (this.Terrain.BaseType.IsAnyType(new string[] { "swamp" }))
			{
				list.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				list.Add(GameText.CreateLocalised("TERRAINEFFECT_DEFENCE", new object[] { 1 }));
			}
			if (this.Terrain.BaseType.IsAnyType(new string[] { "wasteland" }))
			{
				list.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				list.Add(GameText.CreateLocalised("TERRAINEFFECT_DEFENCE", new object[] { 1 }));
			}
			if (this.HasTown())
			{
				list.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				list.Add(GameText.CreateLocalised("TERRAINEFFECT_DEFENCE", new object[] { 1 }));
			}
			return list;
		}

		public void HandleUnitEnter(WorkingUnit Unit)
		{
			if (Unit == null)
			{
				return;
			}
			if (Unit.Disabled)
			{
				return;
			}
			this.ActiveUnitFlags = new List<UnitFlag>();
			if (this.Terrain.BaseType.IsAnyType(new string[] { "hills" }))
			{
				UnitFlag unitFlag = UnitFlag.CreateNamedFlag(this.Game.GameCore, "+1 Attack");
				unitFlag.NoFloaties = true;
				unitFlag.TurnCount = 100;
				Unit.GrantFlag(unitFlag);
				this.ActiveUnitFlags.Add(unitFlag);
				unitFlag = UnitFlag.CreateNamedFlag(this.Game.GameCore, "+1 Defense");
				unitFlag.NoFloaties = true;
				unitFlag.TurnCount = 100;
				Unit.GrantFlag(unitFlag);
				this.ActiveUnitFlags.Add(unitFlag);
			}
			if (this.Terrain.BaseType.IsAnyType(new string[] { "lt forest" }))
			{
				UnitFlag unitFlag2 = UnitFlag.CreateNamedFlag(this.Game.GameCore, "+1 Defense");
				unitFlag2.NoFloaties = true;
				unitFlag2.TurnCount = 100;
				Unit.GrantFlag(unitFlag2);
				this.ActiveUnitFlags.Add(unitFlag2);
				unitFlag2 = UnitFlag.CreateNamedFlag(this.Game.GameCore, "ForestCover");
				unitFlag2.NoFloaties = true;
				unitFlag2.TurnCount = 100;
				unitFlag2.SetVariable("Effect", 15);
				Unit.GrantFlag(unitFlag2);
				this.ActiveUnitFlags.Add(unitFlag2);
			}
			if (this.Terrain.BaseType.IsAnyType(new string[] { "mountain" }))
			{
				UnitFlag unitFlag3 = UnitFlag.CreateNamedFlag(this.Game.GameCore, "+1 Attack");
				unitFlag3.NoFloaties = true;
				unitFlag3.TurnCount = 100;
				Unit.GrantFlag(unitFlag3);
				this.ActiveUnitFlags.Add(unitFlag3);
				unitFlag3 = UnitFlag.CreateNamedFlag(this.Game.GameCore, "+1 Defense");
				unitFlag3.NoFloaties = true;
				unitFlag3.TurnCount = 100;
				Unit.GrantFlag(unitFlag3);
				this.ActiveUnitFlags.Add(unitFlag3);
				unitFlag3 = UnitFlag.CreateNamedFlag(this.Game.GameCore, "+1 Defense");
				unitFlag3.NoFloaties = true;
				unitFlag3.TurnCount = 100;
				Unit.GrantFlag(unitFlag3);
				this.ActiveUnitFlags.Add(unitFlag3);
			}
			if (this.Terrain.BaseType.IsAnyType(new string[] { "old forest" }))
			{
				UnitFlag unitFlag4 = UnitFlag.CreateNamedFlag(this.Game.GameCore, "+1 Defense");
				unitFlag4.NoFloaties = true;
				unitFlag4.TurnCount = 100;
				Unit.GrantFlag(unitFlag4);
				this.ActiveUnitFlags.Add(unitFlag4);
				unitFlag4 = UnitFlag.CreateNamedFlag(this.Game.GameCore, "ForestCover");
				unitFlag4.NoFloaties = true;
				unitFlag4.TurnCount = 100;
				unitFlag4.SetVariable("Effect", 30);
				Unit.GrantFlag(unitFlag4);
				this.ActiveUnitFlags.Add(unitFlag4);
			}
			if (this.Terrain.BaseType.IsAnyType(new string[] { "swamp" }))
			{
				UnitFlag unitFlag5 = UnitFlag.CreateNamedFlag(this.Game.GameCore, "+1 Defense");
				unitFlag5.NoFloaties = true;
				unitFlag5.TurnCount = 100;
				Unit.GrantFlag(unitFlag5);
				this.ActiveUnitFlags.Add(unitFlag5);
			}
			if (this.Terrain.BaseType.IsAnyType(new string[] { "wasteland" }))
			{
				UnitFlag unitFlag6 = UnitFlag.CreateNamedFlag(this.Game.GameCore, "+1 Defense");
				unitFlag6.NoFloaties = true;
				unitFlag6.TurnCount = 100;
				Unit.GrantFlag(unitFlag6);
				this.ActiveUnitFlags.Add(unitFlag6);
			}
			if (this.HasTown())
			{
				UnitFlag unitFlag7 = UnitFlag.CreateNamedFlag(this.Game.GameCore, "+1 Defense");
				unitFlag7.NoFloaties = true;
				unitFlag7.TurnCount = 100;
				Unit.GrantFlag(unitFlag7);
				this.ActiveUnitFlags.Add(unitFlag7);
			}
		}

		public void HandleUnitLeave(WorkingUnit Unit)
		{
			if (Unit != null && this.ActiveUnitFlags != null)
			{
				foreach (UnitFlag unitFlag in this.ActiveUnitFlags)
				{
					Unit.RemoveFlag(unitFlag);
				}
				this.ActiveUnitFlags.Clear();
				this.ActiveUnitFlags = null;
			}
		}

		public void CreateLight()
		{
			this.BaseSprite.SetImage("Data\\Images\\Combat\\Tiles\\Lakehex.png");
		}

		public void Dispose()
		{
			this.UnitID = -1;
			if (this.BaseSprite != null)
			{
				this.BaseSprite.Dispose(false);
			}
			if (this.FeatureSprite != null)
			{
				this.FeatureSprite.Dispose(false);
			}
			if (this.TerrainSprite != null)
			{
				this.TerrainSprite.Dispose(false);
			}
			if (this.RoadSprite != null)
			{
				this.RoadSprite.Dispose(false);
			}
			if (this.VPSprite != null)
			{
				this.VPSprite.Dispose(false);
			}
			if (this.BorderSprite != null)
			{
				this.BorderSprite.Dispose(false);
			}
			if (this.BorderFogSprite != null)
			{
				this.BorderFogSprite.Dispose(false);
			}
			if (this.OverlaySprite != null)
			{
				this.OverlaySprite.Dispose(false);
			}
			if (this.TargetSprite != null)
			{
				this.TargetSprite.Dispose(false);
			}
			if (this.FlagSprite != null)
			{
				this.FlagSprite.Dispose(false);
			}
			if (this.WagonSprite != null)
			{
				this.WagonSprite.Dispose(false);
			}
			if (this.OverlaySprite2 != null)
			{
				this.OverlaySprite2.Dispose(false);
			}
			if (this.OverlayBorderSprite != null)
			{
				this.OverlayBorderSprite.Dispose(false);
			}
			if (this.GridSprite != null)
			{
				this.GridSprite.Dispose(false);
			}
		}

		internal void SetOverlayText(string Text)
		{
			Bitmap bitmap = new Bitmap(128, 128);
			string font = this.Game.GameCore.Utilities.FontConvertor.GetFont("Trebuchet MS", true);
			GameFont font2 = GameFont.GetFont(this.Game.GameCore, "Trebuchet MS", "Data\\Fonts\\" + font, 24);
			TextRenderer textRenderer = new TextRenderer(this.Game.GameCore);
			textRenderer.MaxWidth = 128f;
			textRenderer.MaxHeight = 128f;
			textRenderer.DefaultFont = font2;
			textRenderer.TextAnchor = AnchorPoints.Middle;
			textRenderer.SetText(Text, textRenderer.DefaultFont, textRenderer.DefaultColour, textRenderer.MaxWidth, textRenderer.MaxHeight);
			textRenderer.RenderOverImage(bitmap, textRenderer.DefaultColour);
			GLTexture gltexture = new GLTexture(bitmap);
			bitmap.Dispose();
			this.OverlaySprite = this.Game.GameCore.Utilities.BattleSpriteManager.CreateSprite(Guid.NewGuid().ToString(), gltexture, false);
			this.OverlaySprite.SetPosition(this.PlotX, this.PlotY);
			this.OverlaySprite.SetSize(BattleTile.TileSizeX, BattleTile.TileSizeY);
			textRenderer.Dispose();
		}

		internal void AddFog()
		{
			int num = 0;
			for (int i = 0; i < 6; i++)
			{
				int num2 = this.X;
				int num3 = this.Y;
				if (this.X % 2 == 1)
				{
					num2 += this.HEX_X_ODD[i];
					num3 += this.HEX_Y_ODD[i];
				}
				else
				{
					num2 += this.HEX_X_EVEN[i];
					num3 += this.HEX_Y_EVEN[i];
				}
				if (num2 >= 0 && num3 >= 0 && num2 < this.Game.GameCore.Data.CombatMap.TilesX && num3 < this.Game.GameCore.Data.CombatMap.TilesY && this.InsideBattle == this.Game.GameCore.CurrentBattleMap.GetTile(num2, num3).InsideBattle)
				{
					num += (int)Math.Pow(2.0, (double)i);
				}
			}
			this.BorderFogSprite = this.Game.GameCore.Utilities.BattleSpriteManager.CreateIndexedSprite("Data\\Images\\Combat\\Tiles\\FoW.png", num, false, 128, 128);
			this.BorderFogSprite.SetPosition(this.PlotX, this.PlotY);
			this.BorderFogSprite.SetSize(BattleTile.TileSizeX, BattleTile.TileSizeY);
			this.BorderFogSprite.Batch.DisableFiltering();
		}

		internal void HideGrid()
		{
			if (this.GridSprite != null)
			{
				this.GridSprite.Dispose(false);
				this.GridSprite = null;
			}
		}

		internal void ShowGrid()
		{
			this.HideGrid();
			this.GridSprite = this.Game.GameCore.Utilities.BattleSpriteManager.CreateSprite("Data\\Images\\Combat\\Tiles\\gridline.png", false);
			this.GridSprite.SetPosition(this.PlotX, this.PlotY);
			this.GridSprite.SetSize(BattleTile.TileSizeX, BattleTile.TileSizeY);
		}

		internal void ClearOverlayBorder()
		{
			if (this.OverlayBorderSprite != null)
			{
				this.OverlayBorderSprite.Dispose(false);
				this.OverlayBorderSprite = null;
			}
		}

		internal void SetSecondOverlayBorder(int BorderIndex, Color BorderColour)
		{
			this.ClearSecondOverlayBorder();
			this.OverlayBorderSprite2 = this.Game.GameCore.Utilities.BattleSpriteManager.CreateIndexedSprite("Data\\Images\\Combat\\Tiles\\Border_white.png", BorderIndex, false, 128, 128);
			this.OverlayBorderSprite2.SetPosition(this.PlotX, this.PlotY);
			this.OverlayBorderSprite2.SetSize(BattleTile.TileSizeX, BattleTile.TileSizeY);
			this.OverlayBorderSprite2.SetBlendColour(BorderColour);
		}

		internal void ClearSecondOverlayBorder()
		{
			if (this.OverlayBorderSprite2 != null)
			{
				this.OverlayBorderSprite2.Dispose(false);
				this.OverlayBorderSprite2 = null;
			}
		}

		internal void SetOverlayBorder(int BorderIndex, Color BorderColour)
		{
			this.ClearOverlayBorder();
			this.OverlayBorderSprite = this.Game.GameCore.Utilities.BattleSpriteManager.CreateIndexedSprite("Data\\Images\\Combat\\Tiles\\Border_white.png", BorderIndex, false, 128, 128);
			this.OverlayBorderSprite.SetPosition(this.PlotX, this.PlotY);
			this.OverlayBorderSprite.SetSize(BattleTile.TileSizeX, BattleTile.TileSizeY);
			this.OverlayBorderSprite.SetBlendColour(BorderColour);
		}

		internal void SetOverlayColour(Color Colour)
		{
			if (this.BorderFogSprite != null)
			{
				return;
			}
			if (this.OverlaySprite == null)
			{
				this.OverlaySprite = this.Game.GameCore.Utilities.BattleSpriteManager.CreateSprite("Data\\Images\\Combat\\Tiles\\Overlay.png", false);
				this.OverlaySprite.SetPosition(this.PlotX, this.PlotY);
				this.OverlaySprite.SetSize(BattleTile.TileSizeX, BattleTile.TileSizeY);
				this.OverlaySprite.Batch.DisableFiltering();
			}
			this.OverlaySprite.SetBlendColour(Colour);
		}

		internal void SetSecondOverlayColour(Color Colour)
		{
			if (this.OverlaySprite2 == null)
			{
				this.OverlaySprite2 = this.Game.GameCore.Utilities.BattleSpriteManager.CreateSprite("Data\\Images\\Combat\\Tiles\\Overlay.png", false);
				this.OverlaySprite2.SetPosition(this.PlotX, this.PlotY);
				this.OverlaySprite2.SetSize(BattleTile.TileSizeX, BattleTile.TileSizeY);
				this.OverlaySprite2.Batch.DisableFiltering();
			}
			this.OverlaySprite2.SetBlendColour(Colour);
		}

		internal void ClearSecondOverlay()
		{
			if (this.OverlaySprite2 != null)
			{
				this.OverlaySprite2.Dispose(false);
				this.OverlaySprite2 = null;
			}
		}

		internal void ClearOverlay()
		{
			if (this.OverlaySprite != null)
			{
				this.OverlaySprite.Dispose(false);
				this.OverlaySprite = null;
			}
		}

		internal void ShowWagonSprite()
		{
			if (this.WagonSprite == null)
			{
				this.WagonSprite = this.Game.GameCore.Utilities.BattleSpriteManager.CreateSprite("Data\\Images\\Combat\\wagon_icon.png", false);
				this.WagonSprite.SetPosition(this.PlotX, this.PlotY);
				this.WagonSprite.SetSize(BattleTile.TileSizeX / 2f, BattleTile.TileSizeY / 2f);
			}
		}

		internal void ClearWagonSprite()
		{
			if (this.WagonSprite != null)
			{
				this.WagonSprite.Dispose(false);
				this.WagonSprite = null;
			}
		}

		public void SetTargetSprite(CombatAction ActionType)
		{
			this.ClearTargetSprite();
			string text = "Data\\Images\\Combat\\Tiles\\";
			switch (ActionType)
			{
			case CombatAction.MeleeAttack:
				text += "attack_melee.png";
				break;
			case CombatAction.RangedAttack:
				text += "attack_ranged.png";
				break;
			case CombatAction.Moveattack:
				text += "attack_melee.png";
				break;
			case CombatAction.ChargeAttack:
				text += "attack_charge.png";
				break;
			case CombatAction.Heal:
				text += "heal.png";
				break;
			}
			this.TargetSprite = this.Game.GameCore.Utilities.BattleSpriteManager.CreateSprite(text, false);
			this.TargetSprite.BringToFront();
			this.TargetSprite.SetPosition(this.PlotX, this.PlotY);
			this.TargetSprite.SetSize(BattleTile.TileSizeX * 0.6f, BattleTile.TileSizeY * 0.6f);
		}

		internal bool HasTown()
		{
			return this.TileData.FeatureID != -1 && (this.Game.GameCore.Data.CombatFeatureList[this.TileData.FeatureID].Name == "Town" || this.Game.GameCore.Data.CombatFeatureList[this.TileData.FeatureID].Name == "Capitol");
		}

		internal bool HasRoad()
		{
			return this.TileData.RoadID != -1 || (this.Terrain.BaseType.IsAnyType(new string[] { "river" }) && this.UnitID != -1 && this.Unit.HasStatus("Bridging", new object[0]));
		}

		internal void ClearTargetSprite()
		{
			if (this.TargetSprite != null)
			{
				this.TargetSprite.Dispose(false);
				this.TargetSprite = null;
			}
		}

		internal void SetFlagSprite(WorkingRealm Realm)
		{
			if (this.FlagSprite != null)
			{
				this.FlagSprite.Dispose(false);
			}
			string text = "Data\\Images\\HUD\\Flags\\Objectives\\" + Realm.FlagFilename + ".png";
			this.FlagSprite = this.Game.GameCore.Utilities.BattleSpriteManager.CreateSprite(text, false);
			this.FlagSprite.SetPosition(this.PlotX + BattleTile.TileSizeX * 0.25f, this.PlotY - BattleTile.TileSizeY * 0.25f);
			this.FlagSprite.SetSize(BattleTile.TileSizeX * 0.6f, BattleTile.TileSizeY * 0.6f);
		}

		private int[] HEX_Y_EVEN = new int[] { -1, -1, 0, 1, 0, -1 };

		private int[] HEX_X_EVEN = new int[] { 0, 1, 1, 0, -1, -1 };

		private int[] HEX_Y_ODD = new int[] { -1, 0, 1, 1, 1, 0 };

		private int[] HEX_X_ODD = new int[] { 0, 1, 1, 0, -1, -1 };

		private SovereigntyGame Game;

		private float PlotX;

		private float PlotY;

		public static float TileSizeX;

		public static float TileSizeY;

		public GLSprite BaseSprite;

		public GLSprite TerrainSprite;

		public GLSprite FeatureSprite;

		public GLSprite RoadSprite;

		public GLSprite VPSprite;

		public GLSprite BorderSprite;

		public GLSprite BorderFogSprite;

		public GLSprite TargetSprite;

		public GLSprite FlagSprite;

		public GLSprite OverlaySprite;

		public GLSprite OverlaySprite2;

		public GLSprite WagonSprite;

		public GLSprite OverlayBorderSprite;

		public GLSprite OverlayBorderSprite2;

		public GLSprite GridSprite;

		public int X;

		public int Y;

		public Point Location;

		public MapTileData TileData;

		public bool InsideBattle;

		public List<UnitFlag> ActiveUnitFlags;

		private static Dictionary<string, float> FeatureOffsets;

		private int m_UnitID = -1;
	}
}
