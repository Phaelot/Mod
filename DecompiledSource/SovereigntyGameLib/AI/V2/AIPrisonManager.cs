using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.AI.V2.Actions;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.AI.V2
{
	public class AIPrisonManager
	{
		public AIPrisonManager(AIPlayer AI)
		{
			this.AI = AI;
			this.Funds = new AIFundData();
		}

		internal void Dispose()
		{
		}

		internal void UpdatePrisoners()
		{
			List<WorkingUnit> list = new List<WorkingUnit>();
			List<WorkingUnit> list2 = new List<WorkingUnit>();
			List<WorkingUnit> list3 = new List<WorkingUnit>();
			List<WorkingUnit> list4 = new List<WorkingUnit>();
			List<WorkingUnit> list5 = new List<WorkingUnit>();
			List<WorkingUnit> list6 = new List<WorkingUnit>();
			this.AI.Log("");
			this.AI.Log("Prison manager updating");
			this.AI.Log("  Available funds: " + this.Funds.CurrentGold);
			int num = this.AI.Realm.Provinces.Sum((WorkingProvince x) => x.GetSlaverySlots());
			int num2 = this.AI.Realm.MagicData.SpellCoolDown;
			int num3 = this.AI.Game.EconomyController.GetRealmTotalIncome(this.AI.Realm) - this.AI.Game.EconomyController.GetTotalExpenses(this.AI.Realm);
			if (this.AI.Realm.Prison.AllPrisoners.Count == 0)
			{
				this.AI.Log("  No units in prison, prison manager skipping");
			}
			foreach (WorkingUnit workingUnit in this.AI.Realm.Prison.AllPrisoners)
			{
				RelationStates relation = this.AI.Realm.DiplomacyManager.GetRelation(workingUnit.OwnerRealm);
				float num4;
				if (workingUnit.OwnerRealm.AIPlayer != null)
				{
					num4 = workingUnit.OwnerRealm.AIPlayer.TradeManager.GetPrisonerValue(workingUnit);
				}
				else
				{
					num4 = this.AI.TradeManager.GetPrisonerValue(workingUnit);
				}
				if (workingUnit.OwnerRealm == this.AI.Game.RebelRealm || workingUnit.OwnerRealm.RealmIsDead)
				{
					num4 = 0f;
				}
				if (relation == RelationStates.War)
				{
					if (this.AI.Realm.Alignment == RealmAlignments.Evil && num > 0)
					{
						if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.Enslave, workingUnit))
						{
							continue;
						}
						if (num4 < 50f || (num4 < 200f && this.AI.RNG.Next(100) > 75))
						{
							this.AI.Log(string.Concat(new object[]
							{
								"  Enslaving unit ",
								workingUnit.ID,
								" (",
								workingUnit.OwnerRealm.Name,
								")"
							}));
							list.Add(workingUnit);
							num--;
							continue;
						}
					}
					if (this.AI.Realm.MagicValue > 0 && num2 > 0 && !this.AI.Realm.CodeOfWar)
					{
						if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.Experiment, workingUnit))
						{
							continue;
						}
						if (num4 < 50f || (num4 < 200f && this.AI.RNG.Next(100) > 75))
						{
							this.AI.Log(string.Concat(new object[]
							{
								"  Sacrificing unit ",
								workingUnit.ID,
								" (",
								workingUnit.OwnerRealm.Name,
								")"
							}));
							list6.Add(workingUnit);
							num2--;
							continue;
						}
					}
					if (num3 < 0 && this.AI.BudgetManager.GetTotalGold() < 500)
					{
						if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.Execute, workingUnit))
						{
							continue;
						}
						if (num4 < 50f || this.AI.RNG.Next(100) > 75)
						{
							this.AI.Log(string.Concat(new object[]
							{
								"  Executing unit ",
								workingUnit.ID,
								" (",
								workingUnit.OwnerRealm.Name,
								")"
							}));
							list5.Add(workingUnit);
							num3 += workingUnit.Upkeep / 3;
							continue;
						}
					}
					if (num3 > workingUnit.Upkeep && this.Funds.CurrentGold > this.AI.Game.PrisonerController.GetRecruitCost(workingUnit, this.AI.Realm))
					{
						if (this.AI.Realm.Race == Races.Undead)
						{
							if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.MakeUndead, workingUnit))
							{
								continue;
							}
							this.AI.Log(string.Concat(new object[]
							{
								"  Raising unit ",
								workingUnit.ID,
								" as undead (",
								workingUnit.OwnerRealm.Name,
								")"
							}));
							list4.Add(workingUnit);
						}
						else
						{
							if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.Recruit, workingUnit))
							{
								continue;
							}
							this.AI.Log(string.Concat(new object[]
							{
								"  Bribing unit ",
								workingUnit.ID,
								" (",
								workingUnit.OwnerRealm.Name,
								")"
							}));
							list3.Add(workingUnit);
						}
						this.Funds.CurrentGold -= this.AI.Game.PrisonerController.GetRecruitCost(workingUnit, this.AI.Realm);
						num3 -= workingUnit.Upkeep;
					}
				}
				else
				{
					if (this.AI.Realm.CodeOfWar)
					{
						if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.Release, workingUnit))
						{
							continue;
						}
						if (workingUnit.OwnerRealm != this.AI.Game.RebelRealm && !workingUnit.OwnerRealm.RealmIsDead)
						{
							this.AI.Log(string.Concat(new object[]
							{
								"  Releasing unit ",
								workingUnit.ID,
								" (",
								workingUnit.OwnerRealm.Name,
								")"
							}));
							if (relation == RelationStates.Peace)
							{
								list2.Add(workingUnit);
								continue;
							}
							continue;
						}
					}
					if (this.AI.Realm.Alignment == RealmAlignments.Evil && num > 0)
					{
						if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.Enslave, workingUnit))
						{
							continue;
						}
						if (num4 < 50f || (num4 < 200f && this.AI.RNG.Next(100) > 75))
						{
							this.AI.Log(string.Concat(new object[]
							{
								"  Enslaving unit ",
								workingUnit.ID,
								" (",
								workingUnit.OwnerRealm.Name,
								")"
							}));
							list.Add(workingUnit);
							num--;
							continue;
						}
					}
					if (num3 > workingUnit.Upkeep && this.Funds.CurrentGold > this.AI.Game.PrisonerController.GetRecruitCost(workingUnit, this.AI.Realm))
					{
						if (this.AI.Realm.Race == Races.Undead)
						{
							if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.MakeUndead, workingUnit))
							{
								continue;
							}
							this.AI.Log(string.Concat(new object[]
							{
								"  Raising unit ",
								workingUnit.ID,
								" as undead (",
								workingUnit.OwnerRealm.Name,
								")"
							}));
							list4.Add(workingUnit);
						}
						else
						{
							if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.Recruit, workingUnit))
							{
								continue;
							}
							this.AI.Log(string.Concat(new object[]
							{
								"  Bribing unit ",
								workingUnit.ID,
								" (",
								workingUnit.OwnerRealm.Name,
								")"
							}));
							list3.Add(workingUnit);
						}
						this.Funds.CurrentGold -= this.AI.Game.PrisonerController.GetRecruitCost(workingUnit, this.AI.Realm);
						num3 -= workingUnit.Upkeep;
					}
					else
					{
						if (this.AI.Realm.DiplomacyManager.GetDisposition(workingUnit.OwnerRealm) > 5f)
						{
							if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.Release, workingUnit))
							{
								continue;
							}
							if (workingUnit.OwnerRealm != this.AI.Game.RebelRealm && !workingUnit.OwnerRealm.RealmIsDead)
							{
								this.AI.Log(string.Concat(new object[]
								{
									"  Releasing unit ",
									workingUnit.ID,
									" (",
									workingUnit.OwnerRealm.Name,
									")"
								}));
								list2.Add(workingUnit);
								continue;
							}
						}
						if (this.AI.Realm.MagicValue > 0 && num2 > 0 && !this.AI.Realm.CodeOfWar && this.AI.Realm.Prison.ActionPossible(PrisonActions.Experiment, workingUnit) && (num4 < 50f || (num4 < 200f && this.AI.RNG.Next(100) > 75)))
						{
							this.AI.Log(string.Concat(new object[]
							{
								"  Sacrificing unit ",
								workingUnit.ID,
								" (",
								workingUnit.OwnerRealm.Name,
								")"
							}));
							list6.Add(workingUnit);
							num2--;
						}
					}
				}
			}
			if (list2.Count > 0)
			{
				AIActionReleasePrisoners aiactionReleasePrisoners = this.AI.ActionManager.CreateAction<AIActionReleasePrisoners>();
				aiactionReleasePrisoners.Units = list2;
				this.AI.ActionManager.AddAction(aiactionReleasePrisoners, true);
			}
			if (list5.Count > 0)
			{
				AIActionExecutePrisoners aiactionExecutePrisoners = this.AI.ActionManager.CreateAction<AIActionExecutePrisoners>();
				aiactionExecutePrisoners.Units = list2;
				this.AI.ActionManager.AddAction(aiactionExecutePrisoners, true);
			}
			if (list.Count > 0)
			{
				AIActionEnslavePrisoners aiactionEnslavePrisoners = this.AI.ActionManager.CreateAction<AIActionEnslavePrisoners>();
				aiactionEnslavePrisoners.Units = list2;
				this.AI.ActionManager.AddAction(aiactionEnslavePrisoners, true);
			}
			if (list6.Count > 0)
			{
				AIActionSacrificePrisoners aiactionSacrificePrisoners = this.AI.ActionManager.CreateAction<AIActionSacrificePrisoners>();
				aiactionSacrificePrisoners.Units = list2;
				this.AI.ActionManager.AddAction(aiactionSacrificePrisoners, true);
			}
			if (list3.Count > 0)
			{
				AIActionRecruitPrisoners aiactionRecruitPrisoners = this.AI.ActionManager.CreateAction<AIActionRecruitPrisoners>();
				aiactionRecruitPrisoners.Units = list2;
				this.AI.ActionManager.AddAction(aiactionRecruitPrisoners, true);
			}
			if (list4.Count > 0)
			{
				AIActionRaisePrisoners aiactionRaisePrisoners = this.AI.ActionManager.CreateAction<AIActionRaisePrisoners>();
				aiactionRaisePrisoners.Units = list2;
				this.AI.ActionManager.AddAction(aiactionRaisePrisoners, true);
			}
		}

		public void ShowPrisonerMessage(List<WorkingUnit> Units, string Title, string Text)
		{
			List<WorkingUnit> list = Units.Where((WorkingUnit x) => x.OwnerRealm == this.AI.Game.PlayerRealm).ToList<WorkingUnit>();
			if (list.Count == 0)
			{
				return;
			}
			GameText gameText = GameText.CreateLocalised(Title, new object[0]);
			List<GameText> list2 = new List<GameText>();
			GameText gameText2 = GameText.CreateLocalised(Text, new object[0]);
			gameText2.AddChildText(GameText.CreateLocalised(this.AI.Realm.DisplayName, new object[0]));
			list2.Add(gameText2);
			foreach (WorkingUnit workingUnit in list)
			{
				list2.Add(GameText.CreateLocalised(workingUnit.DisplayName, new object[0]));
				list2.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
			}
			MessageBoxData messageBoxData = new MessageBoxData();
			messageBoxData.CaptionText = gameText;
			messageBoxData.MessageTextList = list2;
			messageBoxData.DisplayType = MessageBoxType.Info;
			messageBoxData.MsgType = MessageType.GenericInfo;
			this.AI.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
		}

		internal void Save(BinaryWriter w)
		{
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
		}

		public AIPlayer AI;

		public AIFundData Funds;

		private enum AIMessageTypes
		{
			Prison,
			Budget,
			Construction,
			Espionage,
			War,
			Diplomacy,
			Revolt,
			Tactical,
			Trade,
			Units
		}
	}
}
