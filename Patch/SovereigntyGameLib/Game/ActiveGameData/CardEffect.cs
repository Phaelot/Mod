using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.ActiveGameData
{
	public abstract class CardEffect
	{
		public CardEffect(string Name, WorkingHero Hero, TacticalBattleController Battle)
		{
			this.Name = Name;
			this.Battle = Battle;
			this.Hero = Hero;
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.DisplayName);
			w.Write(this.DisplayDesc);
			w.Write(this.ArtName);
			w.Write((short)this.Panel);
		}

		internal void Load(BinaryReader r)
		{
			this.DisplayName = r.ReadString();
			this.DisplayDesc = r.ReadString();
			this.ArtName = r.ReadString();
			this.Panel = (SpellSchools)r.ReadInt16();
		}

		public abstract List<CardTargetData> GetTargetData();

		public virtual bool TileTargetValid(Point Tile, int TargetIndex)
		{
			return false;
		}

		public virtual void CastEffect(List<CardTargetData> Targets)
		{
			this.Battle.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\" + this.SoundFile + ".wav");
		}

		public virtual int GetTargetWeight(Point TargetTile)
		{
			return 0;
		}

		public virtual CastTimes GetAICastTime()
		{
			return CastTimes.None;
		}

		public virtual int GetAICastChance()
		{
			return 0;
		}

		public string Name;

		public string DisplayName;

		public string DisplayDesc;

		public string ArtName;

		public string SoundFile;

		public TacticalBattleController Battle;

		public SpellSchools Panel;

		public WorkingHero Hero;
	}
}
