using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class RealmBattleData
	{
		public WorkingRealm Realm
		{
			get
			{
				return this.Game.AllRealms[this.RealmID];
			}
		}

		public RealmBattleData(SovereigntyGame Game, TacticalBattleController Battle, int RealmID)
		{
			this.Game = Game;
			this.Battle = Battle;
			this.RealmID = RealmID;
			this.ActiveCards = new List<CardEffect>();
			this.UsedCards = new List<CardEffect>();
			this.CardTypes = new Dictionary<string, Type>();
			List<Type> list = (from t in Game.GameCore.Utilities.ScriptManager.CardAssembly.GetTypes()
				where t.IsSubclassOf(typeof(CardEffect))
				select t).ToList<Type>();
			foreach (Type type in list)
			{
				Type type2 = type;
				object[] array = new object[2];
				array[0] = Battle;
				CardEffect cardEffect = (CardEffect)Activator.CreateInstance(type2, array);
				this.CardTypes.Add(cardEffect.Name, type);
			}
			WorkingStack workingStack = Battle.Attacker;
			if (Battle.Attacker.OwnerID != RealmID)
			{
				workingStack = Battle.Defender;
			}
			if (workingStack.Hero != null)
			{
				Type type3 = this.CardTypes[workingStack.Hero.BaseAbility];
				CardEffect cardEffect2 = (CardEffect)Activator.CreateInstance(type3, new object[] { Battle, workingStack.Hero });
				cardEffect2.ArtName = workingStack.Hero.GetArt(workingStack.Owner);
				HeroAbilityData heroAbilityData = Game.GameCore.Data.HeroAbilities[workingStack.Hero.BaseAbility];
				cardEffect2.DisplayName = heroAbilityData.DisplayName;
				cardEffect2.DisplayDesc = heroAbilityData.DisplayDesc;
				cardEffect2.Panel = workingStack.Hero.Panel;
				cardEffect2.SoundFile = workingStack.Hero.SoundName;
				this.ActiveCards.Add(cardEffect2);
				if (workingStack.Hero.Legendary)
				{
					Type type4 = this.CardTypes[workingStack.Hero.LegendaryAbilityName];
					cardEffect2 = (CardEffect)Activator.CreateInstance(type4, new object[] { Battle, workingStack.Hero });
					cardEffect2.ArtName = workingStack.Hero.GetArt(workingStack.Owner);
					heroAbilityData = Game.GameCore.Data.HeroAbilities[workingStack.Hero.LegendaryAbilityName];
					cardEffect2.DisplayName = heroAbilityData.DisplayName;
					cardEffect2.DisplayDesc = heroAbilityData.DisplayDesc;
					cardEffect2.Panel = workingStack.Hero.Panel;
					cardEffect2.SoundFile = heroAbilityData.SoundFile;
					this.ActiveCards.Add(cardEffect2);
				}
			}
		}

		public RealmBattleData(SovereigntyGame Game, TacticalBattleController Battle, int RealmID, BinaryReader r)
		{
			this.Game = Game;
			this.Battle = Battle;
			this.RealmID = RealmID;
			this.ActiveCards = new List<CardEffect>();
			this.UsedCards = new List<CardEffect>();
			this.CardTypes = new Dictionary<string, Type>();
			List<Type> list = (from t in Game.GameCore.Utilities.ScriptManager.CardAssembly.GetTypes()
				where t.IsSubclassOf(typeof(CardEffect))
				select t).ToList<Type>();
			foreach (Type type in list)
			{
				Type type2 = type;
				object[] array = new object[2];
				array[0] = Battle;
				CardEffect cardEffect = (CardEffect)Activator.CreateInstance(type2, array);
				this.CardTypes.Add(cardEffect.Name, type);
			}
			WorkingStack workingStack = Battle.Attacker;
			if (Battle.Attacker.OwnerID != RealmID)
			{
				workingStack = Battle.Defender;
			}
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				Type type3 = this.CardTypes[r.ReadString()];
				CardEffect cardEffect2 = (CardEffect)Activator.CreateInstance(type3, new object[] { Battle, workingStack.Hero });
				cardEffect2.Load(r);
				this.ActiveCards.Add(cardEffect2);
			}
			num = r.ReadInt32();
			for (int j = 0; j < num; j++)
			{
				Type type4 = this.CardTypes[r.ReadString()];
				CardEffect cardEffect3 = (CardEffect)Activator.CreateInstance(type4, new object[] { Battle, workingStack.Hero });
				cardEffect3.Load(r);
				this.UsedCards.Add(cardEffect3);
			}
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.ActiveCards.Count);
			foreach (CardEffect cardEffect in this.ActiveCards)
			{
				w.Write(cardEffect.Name);
				cardEffect.Save(w);
			}
			w.Write(this.UsedCards.Count);
			foreach (CardEffect cardEffect2 in this.UsedCards)
			{
				w.Write(cardEffect2.Name);
				cardEffect2.Save(w);
			}
		}

		internal void Dispose()
		{
			this.ActiveCards.Clear();
			this.UsedCards.Clear();
			this.CardTypes.Clear();
		}

		public List<CardEffect> ActiveCards;

		public List<CardEffect> UsedCards;

		public Dictionary<string, Type> CardTypes;

		private SovereigntyGame Game;

		private TacticalBattleController Battle;

		public int RealmID;

		public bool CardPlayed;
	}
}
