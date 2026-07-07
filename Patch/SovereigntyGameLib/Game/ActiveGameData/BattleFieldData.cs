using System;
using System.Collections.Generic;
using System.IO;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class BattleFieldData
	{
		public WorkingRealm WinnerRealm
		{
			get
			{
				return this.Game.AllRealms[this.WinnerRealmID];
			}
		}

		public WorkingRealm LoserRealm
		{
			get
			{
				return this.Game.AllRealms[this.LoserRealmID];
			}
		}

		public WorkingProvince Province
		{
			get
			{
				WorkingProvince result = null;
				if (this.Game == null || this.Game.AllProvinces == null)
				{
					return null;
				}
				this.Game.AllProvinces.TryGetValue(this.ProvinceID, out result);
				return result;
			}
		}

		public BattleFieldData(WorkingProvince Province, SovereigntyGame Game)
		{
			this.Game = Game;
			this.ProvinceID = Province.ID;
			this.BattleSprite = Game.GameCore.Utilities.SpriteManager.CreateIndexedSprite("Data\\Images\\Map\\mapicons.png", Province.Terrain.TerrainIconName + "combat.png", true);
			this.BattleSprite.SetSize(32f, 32f);
			this.BattleSprite.SetPosition((float)Province.CapitolCoords.X, (float)Province.CapitolCoords.Y);
			this.BattleSprite.IgnoreMouseClicks = true;
			this.UpdateVisibility();
		}

		public void SetData(GameRegion Location, WorkingRealm Winner, WorkingRealm Loser, int WinnerCasualties, int LoserCasualties, bool DefenderWasWinner)
		{
			this.WinnerRealmID = Winner.ID;
			this.LoserRealmID = Loser.ID;
			this.WinnerCasualties = WinnerCasualties;
			this.LoserCasualties = LoserCasualties;
			this.DefenderWasWinner = DefenderWasWinner;
			this.GenerateTooltip();
		}

		public void GenerateTooltip()
		{
			WorkingProvince province = this.Province;
			if (province == null)
			{
				return;
			}
			List<GameText> list = new List<GameText>();
			GameText gameText = GameText.CreateLocalised("BATTLEFIELD_TITLE_TEMPLATE", new object[0]);
			gameText.AddChildText(GameText.CreateLocalised(province.DisplayName, new object[0]));
			GameText gameText2;
			if (this.DefenderWasWinner)
			{
				gameText2 = GameText.CreateLocalised("BATTLEFIELD_MOUSE_OVER_DEF_SUMMARY_TEMPLATE", new object[0]);
				gameText2.AddChildText(GameText.CreateLocalised(this.WinnerRealm.DisplayName, new object[0]));
				gameText2.AddChildText(GameText.CreateLocalised(this.LoserRealm.DisplayName, new object[0]));
				if (this.WinnerRealm.Alignment == RealmAlignments.Good)
				{
					gameText2.AddChildText(GameText.CreateLocalised("BATTLEFIELD_MOUSE_OVER_DEF_ADJ_GOOD", new object[0]));
				}
				if (this.WinnerRealm.Alignment == RealmAlignments.Neutral)
				{
					gameText2.AddChildText(GameText.CreateLocalised("BATTLEFIELD_MOUSE_OVER_DEF_ADJ_NEUT", new object[0]));
				}
				if (this.WinnerRealm.Alignment == RealmAlignments.Evil)
				{
					gameText2.AddChildText(GameText.CreateLocalised("BATTLEFIELD_MOUSE_OVER_DEF_ADJ_EVIL", new object[0]));
				}
			}
			else
			{
				gameText2 = GameText.CreateLocalised("BATTLEFIELD_MOUSE_OVER_ATK_SUMMARY_TEMPLATE", new object[0]);
				gameText2.AddChildText(GameText.CreateLocalised(this.WinnerRealm.DisplayName, new object[0]));
				gameText2.AddChildText(GameText.CreateLocalised(this.LoserRealm.DisplayName, new object[0]));
				if (this.WinnerRealm.Alignment == RealmAlignments.Good)
				{
					gameText2.AddChildText(GameText.CreateLocalised("BATTLEFIELD_MOUSE_OVER_ATK_VERB_GOOD", new object[0]));
				}
				if (this.WinnerRealm.Alignment == RealmAlignments.Neutral)
				{
					gameText2.AddChildText(GameText.CreateLocalised("BATTLEFIELD_MOUSE_OVER_ATK_VERB_NEUT", new object[0]));
				}
				if (this.WinnerRealm.Alignment == RealmAlignments.Evil)
				{
					gameText2.AddChildText(GameText.CreateLocalised("BATTLEFIELD_MOUSE_OVER_ATK_VERB_EVIL", new object[0]));
				}
			}
			GameText gameText3 = GameText.CreateLocalised("BATTLEFIELD_MOUSE_OVER_DETAIL_TEMPLATE", new object[] { this.WinnerCasualties, this.LoserCasualties });
			gameText3.AddChildText(GameText.CreateLocalised(this.WinnerRealm.DisplayName, new object[0]));
			gameText3.AddChildText(GameText.CreateLocalised(this.LoserRealm.DisplayName, new object[0]));
			list.Add(gameText);
			list.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
			list.Add(gameText2);
			list.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
			list.Add(gameText3);
			this.BattleSprite.TooltipList = list;
		}

		public void SetVisible(bool visible)
		{
			if (this.BattleSprite == null)
			{
				return;
			}
			this.BattleSprite.SetAlpha(visible ? 1f : 0f);
			this.BattleSprite.IgnoreMouse = !visible;
			this.BattleSprite.IgnoreMouseClicks = !visible;
		}

		public void UpdateVisibility()
		{
			WorkingProvince province = this.Province;
			this.SetVisible(province != null);
		}

		public void Dispose()
		{
			this.BattleSprite.Dispose(false);
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.ProvinceID);
			w.Write(this.WinnerRealmID);
			w.Write(this.LoserRealmID);
			w.Write(this.WinnerCasualties);
			w.Write(this.LoserCasualties);
			w.Write(this.DefenderWasWinner);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.ProvinceID = r.ReadInt32();
			this.WinnerRealmID = r.ReadInt32();
			this.LoserRealmID = r.ReadInt32();
			if (SaveVersion >= 27)
			{
				this.WinnerCasualties = r.ReadInt32();
				this.LoserCasualties = r.ReadInt32();
				this.DefenderWasWinner = r.ReadBoolean();
			}
		}

		private GLSprite BattleSprite;

		private int ProvinceID;

		private SovereigntyGame Game;

		public int WinnerRealmID;

		public int LoserRealmID;

		public int WinnerCasualties;

		public int LoserCasualties;

		public bool DefenderWasWinner;
	}
}
