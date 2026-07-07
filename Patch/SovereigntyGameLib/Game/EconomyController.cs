using System;
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game
{
	public class EconomyController
	{
		public EconomyController(SovereigntyGame Game)
		{
			this.Game = Game;
			this.RNG = new Random();
			this.RecentlyConqueredProvinces = new HashSet<int>();
		}

		internal HashSet<int> RecentlyConqueredProvinces;

		public void DoTurnStart(WorkingRealm Realm)
		{
			if (Realm == this.Game.RebelRealm)
			{
				this.UpdateOccupiedProvinces(Realm);
				this.CheckForPlague();
				this.CheckForHarvestFaire();
				return;
			}
			this.DoRealmIncome(Realm);
			this.DoRealmResourceIncome(Realm);
			this.CheckForCradle(Realm);
			this.CheckForPatron(Realm);
			this.DoPrisonerUpkeep(Realm);
			this.DoUnitUpkeep(Realm);
			this.UpdateOccupiedProvinces(Realm);
		}

		private void DoPrisonerUpkeep(WorkingRealm Realm)
		{
			int num = this.GetPrisonerUpkeep(Realm);
			num = Math.Min(num, Realm.GetPrisonGold());
			Realm.SpendPrisonGold(num);
		}

		private void CheckForPatron(WorkingRealm Realm)
		{
			if (this.Game.AllProvinces.Values.Count((WorkingProvince x) => x.HasArts()) >= 9)
			{
				return;
			}
			if (Realm.ArtsValue == 0)
			{
				return;
			}
			if (Realm.Enemies.Count > 1)
			{
				return;
			}
			foreach (WorkingProvince workingProvince in Realm.Provinces)
			{
				if (workingProvince.Cradle == ArtScienceTypes.None && workingProvince.CurrentEconomy >= 6)
				{
					if (workingProvince.Occupied)
					{
						break;
					}
					if (this.RNG.Next(Realm.ArtsValue) == 0)
					{
						this.AwardArts(workingProvince);
					}
				}
			}
		}

		private void CheckForCradle(WorkingRealm Realm)
		{
			if (this.Game.AllProvinces.Values.Count((WorkingProvince x) => x.HasScience()) >= 8)
			{
				return;
			}
			if (Realm.ScienceValue == 0)
			{
				return;
			}
			if (Realm.Enemies.Count < 2)
			{
				return;
			}
			foreach (WorkingProvince workingProvince in Realm.Provinces)
			{
				if (workingProvince.Cradle == ArtScienceTypes.None && workingProvince.CurrentEconomy >= 6)
				{
					if (workingProvince.Occupied)
					{
						break;
					}
					if (this.RNG.Next(Realm.ScienceValue) == 0)
					{
						this.AwardScience(workingProvince);
					}
				}
			}
		}

		public void AwardArts(WorkingProvince Prov)
		{
			WorkingRealm ownerRealm = Prov.OwnerRealm;
			int num = 0;
			if (this.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.PublicArt) > 3)
			{
				if (ownerRealm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.PublicArt) > 0)
				{
					num++;
				}
			}
			if (this.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Statecraft) > 3)
			{
				if (ownerRealm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Statecraft) > 0)
				{
					num++;
				}
			}
			if (this.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Medicine) > 3)
			{
				if (ownerRealm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Medicine) > 0)
				{
					num++;
				}
			}
			if (num == 3)
			{
				return;
			}
			if (ownerRealm.AIPlayer != null)
			{
				ownerRealm.AIPlayer.ConstructionManager.ChooseArts(Prov);
				return;
			}
			MessageBoxData messageBoxData = new MessageBoxData();
			messageBoxData.MsgType = MessageType.GenericInfo;
			messageBoxData.DisplayType = MessageBoxType.Patron;
			messageBoxData.CaptionText = GameText.CreateLocalised("PATRONMSGTITLE", new object[0]);
			messageBoxData.MessageText = GameText.CreateLocalised("PATRONMSGTEXT", new object[0]);
			messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(Prov.DisplayName, new object[0]));
			messageBoxData.Province = Prov;
			this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
		}

		public void AwardScience(WorkingProvince Prov)
		{
			WorkingRealm ownerRealm = Prov.OwnerRealm;
			int num = 0;
			if (this.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Alchemy) > 2)
			{
				if (ownerRealm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Alchemy) > 0)
				{
					num++;
				}
			}
			if (this.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Engineering) > 2)
			{
				if (ownerRealm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Engineering) > 0)
				{
					num++;
				}
			}
			if (this.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Metallurgy) > 2)
			{
				if (ownerRealm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Metallurgy) > 0)
				{
					num++;
				}
			}
			if (this.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Siegecraft) > 2)
			{
				if (ownerRealm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Siegecraft) > 0)
				{
					num++;
				}
			}
			if (num == 4)
			{
				return;
			}
			if (ownerRealm.AIPlayer != null)
			{
				ownerRealm.AIPlayer.ConstructionManager.ChooseScience(Prov);
				return;
			}
			MessageBoxData messageBoxData = new MessageBoxData();
			messageBoxData.MsgType = MessageType.GenericInfo;
			messageBoxData.DisplayType = MessageBoxType.Cradle;
			messageBoxData.CaptionText = GameText.CreateLocalised("CRADLEMSGTITLE", new object[0]);
			messageBoxData.MessageText = GameText.CreateLocalised("CRADLEMSGTEXT", new object[0]);
			messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(Prov.DisplayName, new object[0]));
			messageBoxData.Province = Prov;
			this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
		}

		private void UpdateOccupiedProvinces(WorkingRealm Realm)
		{
			foreach (WorkingProvince workingProvince in Realm.OccupiedProvinces.ToList<WorkingProvince>())
			{
				workingProvince.Resist(1, this.RNG.Next(100) < 30);
				if (workingProvince.ActiveResistance == 0)
				{
					this.RecentlyConqueredProvinces.Add(workingProvince.ID);
					this.Game.ChangeProvinceOwner(workingProvince, Realm);
				}
			}
		}

		private bool StackIsPureNavalInWaterOrHarbour(WorkingStack Stack)
		{
			if (Stack == null || Stack.Node == null || Stack.Units == null || Stack.Units.Count == 0)
			{
				return false;
			}
			if (Stack.Node.NodeType != PathNodeTypes.Harbour && Stack.Node.NodeType != PathNodeTypes.RiverHarbour && Stack.Node.NodeType != PathNodeTypes.Sea)
			{
				return false;
			}
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (unit == null || unit.Class != UnitClasses.Naval)
				{
					return false;
				}
			}
			return true;
		}

		internal void ApplyAttrition(WorkingRealm Realm)
		{
			string logPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "SovereigntyAILogs", "attrition_debug.txt");
			try
			{
				if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(logPath)))
				{
					System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath));
				}
			}
			catch {}
			Realm.StacksChanged();
			try { System.IO.File.AppendAllText(logPath, DateTime.Now.ToString("HH:mm:ss") + " ApplyAttrition called for " + Realm.Name + " (realmID=" + Realm.ID + ") stacks=" + Realm.Stacks.Count + "\r\n"); } catch {}
			foreach (WorkingStack stack in Realm.Stacks.ToList())
			{
				if (stack.Units.Count == 0 || stack.Node.Province == null)
				{
					continue;
				}
				WorkingProvince province = stack.Node.Province;
				try { System.IO.File.AppendAllText(logPath, "  Stack node=" + stack.Node.ID + " units=" + stack.Units.Count + " province=" + province.Name + " ownerID=" + province.OwnerID + " realmID=" + Realm.ID + " occupied=" + province.Occupied + " nodeType=" + stack.Node.NodeType + "\r\n"); } catch {}
				if (this.StackIsPureNavalInWaterOrHarbour(stack))
				{
					try { System.IO.File.AppendAllText(logPath, "    -> SKIP naval stack in water/harbour\r\n"); } catch {}
					continue;
				}
				if (province.OwnerID == Realm.ID)
				{
					if (!this.RecentlyConqueredProvinces.Contains(province.ID))
					{
						try { System.IO.File.AppendAllText(logPath, "    -> SKIP own territory\r\n"); } catch {}
						continue;
					}
					try { System.IO.File.AppendAllText(logPath, "    -> Recently conquered, attrition still applies\r\n"); } catch {}
				}
				if (Realm.Allies.Contains(province.OwnerRealm))
				{
					try { System.IO.File.AppendAllText(logPath, "    -> SKIP allied territory (ally=" + province.OwnerRealm.Name + ")\r\n"); } catch {}
					continue;
				}
				try { System.IO.File.AppendAllText(logPath, "    -> APPLYING attrition\r\n"); } catch {}
				bool hasHero = stack.Hero != null;
				float stackSizeMultiplier = Math.Max(0.5f, (float)stack.Units.Count / 10f);
				string terrain = "";
				try
				{
					if (province.LandNode != null && province.LandNode.GetRegion() != null && province.LandNode.GetRegion().Terrain != null)
					{
						terrain = province.LandNode.GetRegion().Terrain.BaseType.ToLowerInvariant();
					}
				}
				catch
				{
				}
				float terrainMultiplier = 1f;
				switch (terrain)
				{
				case "old forest":
					terrainMultiplier = 1.5f;
					break;
				case "mountain":
					terrainMultiplier = 1.5f;
					break;
				case "swamp":
					terrainMultiplier = 2f;
					break;
				case "wasteland":
					terrainMultiplier = 2f;
					break;
				}
				foreach (WorkingUnit unit in stack.Units.ToList())
				{
					if (unit.Disabled)
					{
						continue;
					}
					if (unit.HasAnyNamedFlag("Raider"))
					{
						continue;
					}
					if (unit.HasAnyNamedFlag("Forester") && (terrain == "lt forest" || terrain == "old forest"))
					{
						continue;
					}
					if (unit.HasAnyNamedFlag("Mountaineer") && (terrain == "hills" || terrain == "mountain"))
					{
						continue;
					}
					if (unit.HasAnyNamedFlag("Darkdweller") && (terrain == "swamp" || terrain == "wasteland"))
					{
						continue;
					}
					if (unit.Race == Races.Undead && terrain == "swamp")
					{
						continue;
					}
					if (unit.Race == Races.Orc && terrain == "wasteland")
					{
						continue;
					}
					float attrition = 2f * stackSizeMultiplier * terrainMultiplier;
					if (unit.HasAnyNamedFlag("Scout"))
					{
						attrition *= 0.5f;
					}
					if (hasHero)
					{
						attrition *= 0.7f;
					}
					float healNegation = 0f;
					int damage = Math.Max(1, (int)Math.Ceiling(attrition));
					int hpBefore = (int)unit.Health;
					unit.Health.Value = Math.Max(1, (int)unit.Health - damage);
					int hpAfter = (int)unit.Health;
					try { System.IO.File.AppendAllText(logPath, "    Unit " + unit.ID + " healRate=" + (int)unit.HealRate + " healNeg=" + healNegation + " attrition=" + attrition + " damage=" + damage + " hp=" + hpBefore + "->" + hpAfter + "\r\n"); } catch {}
				}
			}
			this.RecentlyConqueredProvinces.Clear();
		}

		public int GetTotalExpenses(WorkingRealm Realm)
		{
			return this.GetUnitUpkeep(Realm) + this.GetPrisonerUpkeep(Realm) + this.GetMagicExpenses(Realm) + this.GetTradeExpenses(Realm);
		}

		private int GetTradeExpenses(WorkingRealm Realm)
		{
			int num = 0;
			foreach (OngoingTrade ongoingTrade in this.Game.GetOngoingTrades(Realm))
			{
				if (ongoingTrade.Realm == Realm && ongoingTrade.Gold > 0)
				{
					num += ongoingTrade.Gold;
				}
			}
			return num;
		}

		public int GetMagicExpenses(WorkingRealm Realm)
		{
			return Realm.MagicData.GetCurrentInvestCost();
		}

		public void DoUnitUpkeep(WorkingRealm Realm)
		{
			int unitUpkeep = this.GetUnitUpkeep(Realm);
			if (Realm.GetUnitGold() >= unitUpkeep)
			{
				Realm.SpendUnitsGold(unitUpkeep);
				return;
			}
			int num = unitUpkeep - Realm.GetUnitGold();
			Realm.SpendUnitsGold(Realm.GetUnitGold());
			this.DisbandUnits(Realm, num);
		}

		private void DisbandUnits(WorkingRealm Realm, int DisbandValue)
		{
			List<WorkingUnit> desertableUnits = this.GetDesertableUnits(Realm);
			desertableUnits.Sort(new Comparison<WorkingUnit>(this.CompareUnitLoyalty));
			Dictionary<string, Dictionary<string, int>> dictionary = new Dictionary<string, Dictionary<string, int>>();
			while (DisbandValue > 0 && desertableUnits.Count > 0)
			{
				WorkingUnit workingUnit = desertableUnits[desertableUnits.Count - 1];
				desertableUnits.Remove(workingUnit);
				DisbandValue -= workingUnit.Upkeep;
				WorkingStack ownerStack = workingUnit.OwnerStack;
				string displayName = ownerStack.Node.GetRegion().DisplayName;
				if (!dictionary.ContainsKey(displayName))
				{
					dictionary.Add(displayName, new Dictionary<string, int>());
				}
				if (!dictionary[displayName].ContainsKey(workingUnit.DisplayName))
				{
					dictionary[displayName].Add(workingUnit.DisplayName, 0);
				}
				Dictionary<string, int> dictionary2;
				string displayName2;
				(dictionary2 = dictionary[displayName])[displayName2 = workingUnit.DisplayName] = dictionary2[displayName2] + 1;
				this.Game.DestroyUnit(workingUnit);
				if (ownerStack.Units.Count == 0)
				{
					this.Game.DestroyStack(ownerStack);
				}
			}
			if (dictionary.Count > 0)
			{
				this.Game.GameCore.FireEvent("UnitsDisbanded", new object[] { Realm, dictionary });
			}
		}

		private int CompareUnitLoyalty(WorkingUnit UnitA, WorkingUnit UnitB)
		{
			if (UnitA.GetLoyalty() > UnitB.GetLoyalty())
			{
				return 1;
			}
			if (UnitB.GetLoyalty() > UnitA.GetLoyalty())
			{
				return -1;
			}
			if (UnitA.Health > UnitB.Health)
			{
				return 1;
			}
			if (UnitB.Health > UnitA.Health)
			{
				return -1;
			}
			if (UnitA.Upkeep > UnitB.Upkeep)
			{
				return -1;
			}
			if (UnitB.Upkeep > UnitA.Upkeep)
			{
				return 1;
			}
			if (UnitA.HealRate > UnitB.HealRate)
			{
				return 1;
			}
			if (UnitB.HealRate > UnitA.HealRate)
			{
				return -1;
			}
			return 0;
		}

		private List<WorkingUnit> GetDesertableUnits(WorkingRealm Realm)
		{
			List<WorkingUnit> list = new List<WorkingUnit>();
			foreach (WorkingUnit workingUnit in Realm.Units)
			{
				if (workingUnit.OwnerStack != null && workingUnit.Upkeep != 0 && workingUnit.Rank != UnitRanks.Unique && (workingUnit.Rank == UnitRanks.Mercenary || workingUnit.OwnerStack.Node.Province == null || !workingUnit.OwnerStack.Node.Province.IsCapitol || workingUnit.OwnerStack.Node.Province.OwnerID != Realm.ID))
				{
					list.Add(workingUnit);
				}
			}
			return list;
		}

		public void CheckHeroRecruitment(WorkingRealm Realm)
		{
			if (this.Game.TurnController.TurnNumber < 5)
			{
				return;
			}
			if (Realm.Restrictions.PreventHeroes)
			{
				return;
			}
			if (!Realm.HeroSlotAvailable())
			{
				return;
			}
			if (Realm.Heroes.Count - Realm.StoredHeroIDs.Count >= Realm.HeroLimit)
			{
				return;
			}
			if (this.Game.GameCore.UIHasRestictions())
			{
				return;
			}
			if (Realm.StoredHeroIDs.Count > 0)
			{
				WorkingHero firstStoredHero = Realm.GetFirstStoredHero();
				this.GrantHero(Realm, firstStoredHero, 0);
				return;
			}
			int num = 4;
			num += Realm.Heroes.Count<WorkingHero>() * 20;
			if (Realm.Enemies.Count > 0)
			{
				num /= 4;
			}
			if (this.RNG.Next(num) == 0)
			{
				int num2 = 900;
				num2 += 5 * this.Game.TurnController.TurnNumber;
				if (num2 > 3000)
				{
					num2 = 3000;
				}
				if (Realm.HasStatus("FreeHeroes", new object[0]))
				{
					num2 = 0;
				}
				if (Realm.GetUnitGold() < num2)
				{
					return;
				}
				int num3 = this.RNG.Next(Realm.HeroClasses.Count);
				WorkingHero workingHero = this.Game.CreateHero(Realm, Realm.HeroClasses[num3]);
				this.GrantHero(Realm, workingHero, num2);
			}
		}

		public void OfferFreeHero(WorkingRealm Realm)
		{
			int num = 0;
			WorkingHero workingHero = this.Game.CreateHero(Realm, Realm.HeroClasses[num]);
			this.GrantHero(Realm, workingHero, 0);
		}

		private void GrantHero(WorkingRealm Realm, WorkingHero Hero, int Cost)
		{
			if (Realm.AIPlayer != null)
			{
				Realm.AIPlayer.UnitsManager.HandleHeroOffer(Hero, Cost);
				return;
			}
			this.Game.GameCore.FireEvent("HeroOffered", new object[] { Hero, Cost });
		}

		public int GetPrisonerUpkeep(WorkingRealm Realm)
		{
			return Realm.Prison.AllPrisoners.Sum((WorkingUnit x) => (int)((float)x.Upkeep * Realm.Prison.PrisonerUpkeepMultiplier));
		}

		public int GetUnitUpkeep(WorkingRealm Realm)
		{
			int num = Realm.Units.Sum(delegate(WorkingUnit x)
			{
				if (x.OwnerStackID <= 0)
				{
					return 0;
				}
				return x.Upkeep;
			});
			foreach (UnitQueueItem unitQueueItem in Realm.GetCurrentUnitQueue())
			{
				if (unitQueueItem.Unit != null && unitQueueItem.TurnsLeft == 0)
				{
					num += unitQueueItem.Unit.Upkeep;
				}
			}
			return num;
		}

		private void DoRealmResourceIncome(WorkingRealm Realm)
		{
			Realm.DoResourceIncome();
		}

		public int GetRealmTotalIncome(WorkingRealm Realm)
		{
			return this.GetRealmLandmarkIncome(Realm) + this.GetRealmProvinceIncome(Realm) + this.GetRealmTradeIncome(Realm) + this.GetRealmBonusIncome(Realm) + Realm.SpecialIncomes.Sum();
		}

		public int GetRealmTradeIncome(WorkingRealm Realm)
		{
			int num = 0;
			foreach (OngoingTrade ongoingTrade in this.Game.GetOngoingTrades(Realm))
			{
				if (ongoingTrade.TargetRealm == Realm && ongoingTrade.Gold > 0)
				{
					num += ongoingTrade.Gold;
				}
			}
			return num;
		}

		public int GetRealmSpellIncome(WorkingRealm Realm)
		{
			return 0;
		}

		public int GetRealmProvinceIncome(WorkingRealm Realm)
		{
			int num = 0;
			WorkingProvince capitolProvince = Realm.CapitolProvince;
			IList<WorkingProvince> provinces = Realm.Provinces;
			foreach (WorkingProvince workingProvince in provinces)
			{
				num += this.GetProvinceIncome(workingProvince, capitolProvince, 1f);
			}
			num = (int)((float)num * (float)Realm.ProvinceIncomeMultiplier.GetValue() * 0.01f);
			return num;
		}

		public int GetProvinceEconValue(WorkingProvince Province, WorkingProvince CapitolProvince)
		{
			float num = 150f;
			if (CapitolProvince == null)
			{
				return (int)num;
			}
			Path path = this.Game.PathManager.GetPath(Province.LandNode, CapitolProvince.LandNode, null, false, Province.OwnerRealm, false);
			int i = path.PathPoints.Count;
			if (i == 0 && !Province.IsCapitol)
			{
				return 0;
			}
			while (i > 2)
			{
				num *= 0.78f;
				i--;
			}
			return (int)num;
		}

		public float GetProvinceDistancePenalty(WorkingProvince Province, WorkingProvince CapitolProvince)
		{
			if (CapitolProvince == null)
			{
				return 0f;
			}
			if (Province.HasStatus("IgnoreDistancePenalty"))
			{
				return 1f;
			}
			Path path = this.Game.PathManager.GetPath(Province.LandNode, CapitolProvince.LandNode, null, false, Province.OwnerRealm, false);
			int i = path.GetUniqueRegionCount();
			if (i == 0 && !Province.IsCapitol)
			{
				return 0f;
			}
			float num = 1f;
			while (i > 2)
			{
				num *= 0.78f;
				i--;
			}
			return num;
		}

		public int GetProvinceDistanceEffect(WorkingProvince Province, WorkingProvince CapitolProvince)
		{
			int currentEconomy = Province.CurrentEconomy;
			float num = (float)Province.BaseIncome;
			float num2 = (float)Province.IncomeMultiplier * 0.01f;
			num *= num2;
			if (Province.HasStatus("IgnoreDistancePenalty"))
			{
				return (int)num;
			}
			if (CapitolProvince == null)
			{
				return (int)num;
			}
			Path path = this.Game.PathManager.GetPath(Province.LandNode, CapitolProvince.LandNode, null, false, Province.OwnerRealm, false);
			int i = path.PathPoints.Count;
			if (i == 0 && !Province.IsCapitol)
			{
				return 0;
			}
			float num3 = num;
			for (i += Province.CapitolDistanceModifier; i > 2; i--)
			{
				num *= 0.78f;
			}
			return (int)(num - num3);
		}

		public int GetProvinceIncome(WorkingProvince Province, WorkingProvince CapitolProvince, float Pct)
		{
			int currentEconomy = Province.CurrentEconomy;
			float num = (float)Province.BaseIncome;
			float num2 = (float)Province.IncomeMultiplier * 0.01f;
			num *= num2;
			if (Province.HasStatus("IgnoreDistancePenalty"))
			{
				return (int)num;
			}
			if (CapitolProvince == null)
			{
				return (int)num;
			}
			Path path = this.Game.PathManager.GetPath(Province.LandNode, CapitolProvince.LandNode, null, false, Province.OwnerRealm, true);
			int i = 0;
			List<int> regionIDs = path.GetRegionIDs();
			foreach (int num3 in regionIDs)
			{
				if (this.Game.AllZones.ContainsKey(num3))
				{
					i++;
				}
				else
				{
					i++;
				}
			}
			if (i == 0 && !Province.IsCapitol)
			{
				return 0;
			}
			for (i += Province.CapitolDistanceModifier; i > 2; i--)
			{
				num *= 0.78f;
			}
			return (int)num;
		}

		public int GetRealmLandmarkIncome(WorkingRealm Realm)
		{
			return 0;
		}

		private void DoRealmIncome(WorkingRealm Realm)
		{
			Realm.Gold.Value += this.GetRealmLandmarkIncome(Realm) + this.GetRealmProvinceIncome(Realm) + this.GetRealmBonusIncome(Realm) + Realm.SpecialIncomes.Sum();
			float num = Realm.InterestRate.GetValue() * 0.01f * Realm.GetTotalGold();
			Realm.Gold.Value += (int)num;
		}

		private int GetRealmBonusIncome(WorkingRealm Realm)
		{
			int num = 0;
			if (!Realm.MagicData.SpellCast)
			{
				if (Realm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Alchemy) > 0)
				{
					num += 75;
				}
			}
			return num;
		}

		private void CheckForHarvestFaire()
		{
			WorkingProvince workingProvince = this.Game.AllProvinces.Values.ElementAt(this.RNG.Next(this.Game.AllProvinces.Values.Count));
			WorkingRealm ownerRealm = workingProvince.OwnerRealm;
			if (ownerRealm.Enemies.Count > 1)
			{
				return;
			}
			if (ownerRealm.Alignment == RealmAlignments.Evil)
			{
				return;
			}
			if (workingProvince.Occupied)
			{
				return;
			}
			if (workingProvince.Terrain.IsAnyType(new string[] { "hills", "mountain", "swamp", "wasteland" }))
			{
				return;
			}
			if (workingProvince.IsCapitol)
			{
				return;
			}
			if (workingProvince.HasHarbour)
			{
				return;
			}
			if (workingProvince.FortLevel > 0)
			{
				return;
			}
			if (workingProvince.Landmark != null && workingProvince.Landmark != "")
			{
				return;
			}
			if (workingProvince.CurrentEconomy >= 5)
			{
				return;
			}
			if (workingProvince.PlagueTurns > 0)
			{
				return;
			}
			if (workingProvince.SpellEffects.ActiveSpells.Count((SpellEffect x) => x.SpellData.Type == SpellTypes.Negative) > 0)
			{
				return;
			}
			this.EnactHarvestFaire(workingProvince);
		}

		public void EnactHarvestFaire(WorkingProvince Province)
		{
			WorkingRealm ownerRealm = Province.OwnerRealm;
			ownerRealm.Gold.Value += 320;
			Province.PlagueImmuneTurns = 20;
			WorkingUnit workingUnit = null;
			if (Province.LandNode.CurrentStack != null)
			{
				List<WorkingUnit> list = Province.LandNode.CurrentStack.Units.Where((WorkingUnit x) => this.UnitValidAsChampion(x)).ToList<WorkingUnit>();
				if (list.Count > 0)
				{
					workingUnit = list[this.RNG.Next(list.Count)];
				}
			}
			if (workingUnit != null)
			{
				workingUnit.GrantFlag(UnitFlag.CreateNamedFlag(this.Game.GameCore, "Champion"));
				workingUnit.GrantFlag(UnitFlag.CreateNamedFlag(this.Game.GameCore, "Dragonslayer"));
			}
			else
			{
				ownerRealm.Gold.Value += 75;
			}
			this.Game.GameCore.FireEvent("HarvestFaire", new object[] { Province.ID, workingUnit });
		}

		private bool UnitValidAsChampion(WorkingUnit Unit)
		{
			return Unit.Rank == UnitRanks.Standard && (Unit.Race == Races.Human || Unit.Race == Races.Elf) && (Unit.Class == UnitClasses.Archer || Unit.Class == UnitClasses.Infantry) && Unit.BaseCost <= 400 && Unit.Upkeep <= 45 && Unit.GetDamageType() == DamageTypes.Physical && !Unit.HasAnyNamedFlag("Champion");
		}

		private void CheckForPlague()
		{
			foreach (WorkingProvince workingProvince in this.Game.AllProvinces.Values)
			{
				if (workingProvince.PlaguePossible())
				{
					int num = this.RNG.Next(200);
					if (num < workingProvince.Terrain.PlagueStartChance && workingProvince.Terrain.PlagueTurns > 0)
					{
						workingProvince.PlagueTurns = workingProvince.Terrain.PlagueTurns;
						this.Game.GameCore.FireEvent("PlagueSpread", new object[] { workingProvince });
					}
				}
			}
			foreach (WorkingProvince workingProvince2 in this.Game.AllProvinces.Values.Where((WorkingProvince x) => x.PlagueTurns > 0))
			{
				foreach (ActiveNodeConnection activeNodeConnection in workingProvince2.LandNode.ConnectedNodes)
				{
					WorkingProvince province = activeNodeConnection.TargetNode.Province;
					if (province != null && province.Cradle != ArtScienceTypes.Medicine && province.PlagueImmuneTurns <= 0 && province.PlagueTurns <= 0)
					{
						int num2 = province.Terrain.PlagueSpreadChance;
						if (province.Occupied)
						{
							num2 += 15;
						}
						if (activeNodeConnection.ConnectionType == ConnectionTypes.Road || activeNodeConnection.ConnectionType == ConnectionTypes.Bridge)
						{
							num2 += 7;
						}
						if (province.OwnerID == workingProvince2.OwnerID)
						{
							num2 += 6;
						}
						else
						{
							if (province.OwnerRealm.DiplomacyManager.GetRelation(workingProvince2.OwnerRealm) == RelationStates.Alliance)
							{
								num2 += 3;
							}
							if (province.OwnerRealm.DiplomacyManager.GetDisposition(workingProvince2.OwnerRealm) < 0f)
							{
								num2 -= 10;
							}
						}
						if (province.EconomyDamaged)
						{
							num2 += 2;
						}
						if (province.OwnerRealm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Medicine) > 0 && province.LandNode.CurrentStack != null)
						{
							num2 -= province.LandNode.CurrentStack.Units.Sum((WorkingUnit x) => x.HealRate);
						}
						int num3 = this.RNG.Next(100);
						if (num3 < num2 && province.Terrain.PlagueTurns > 0)
						{
							province.PlagueTurns = province.Terrain.PlagueTurns;
							this.Game.GameCore.FireEvent("PlagueSpread", new object[] { province });
						}
					}
				}
			}
		}

		public SovereigntyGame Game;

		private Random RNG;
	}
}
