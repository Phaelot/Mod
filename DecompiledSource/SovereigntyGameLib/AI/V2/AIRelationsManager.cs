using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.AI.V2.Actions;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Trade;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.AI.V2
{
	public class AIRelationsManager
	{
		public AIRelationsManager(AIPlayer AI)
		{
			this.AI = AI;
			this.Funds = new AIFundData();
		}

		internal void Dispose()
		{
		}

		internal void EndTreaties()
		{
			this.AI.Log("");
			this.AI.Log("Diplomacy manager updating (end treaties phase)");
			List<WorkingRealm> list = new List<WorkingRealm>();
			foreach (WorkingRealm workingRealm in this.AI.Game.AllRealms.Values)
			{
				int num = (int)this.AI.Realm.DiplomacyManager.GetDisposition(workingRealm);
				switch (workingRealm.DiplomacyManager.GetRelation(this.AI.Realm))
				{
				case RelationStates.Alliance:
					this.AI.Log(string.Concat(new object[] { "  Allied to ", workingRealm.Name, " disposition: ", num }));
					if (num < 10)
					{
						this.AI.Log("  Disposition too low, ending alliance");
						list.Add(workingRealm);
					}
					break;
				case RelationStates.Defence:
					this.AI.Log(string.Concat(new object[] { "  Defence pact with ", workingRealm.Name, " disposition: ", num }));
					if (num < 0)
					{
						this.AI.Log("  Disposition too low, ending pact");
						list.Add(workingRealm);
					}
					break;
				case RelationStates.NAP:
					this.AI.Log(string.Concat(new object[] { "  Non-aggression treaty with ", workingRealm.Name, " disposition: ", num }));
					if (num < -15)
					{
						this.AI.Log("  Disposition too low, ending treaty");
						list.Add(workingRealm);
					}
					break;
				}
			}
			if (list.Count > 0)
			{
				AIActionEndTreaties aiactionEndTreaties = this.AI.ActionManager.CreateAction<AIActionEndTreaties>();
				aiactionEndTreaties.Realms = list;
				this.AI.ActionManager.AddAction(aiactionEndTreaties, true);
			}
		}

		internal void HandleFailedTrade(OngoingTrade Trade)
		{
			this.AI.Realm.DiplomacyManager.TriggerEvent(Trade.Realm, "TradeFailed");
			if (this.AI.RNG.Next(100) < 75)
			{
				switch (this.AI.Realm.DiplomacyManager.GetRelation(Trade.Realm))
				{
				case RelationStates.Alliance:
				case RelationStates.Defence:
				case RelationStates.NAP:
					this.AI.Game.AllianceController.BreakCurrentTreaty(this.AI.Realm, Trade.Realm, true, false);
					break;
				default:
					return;
				}
			}
		}

		internal void ConsiderLiberation(WorkingProvince Province)
		{
			bool flag = false;
			if (this.AI.Realm.CodeOfWar)
			{
				flag = true;
			}
			else if (this.AI.RNG.Next(10) >= this.AI.Game.Data.AITraits[this.AI.Realm.Name].Opportunist)
			{
				flag = true;
			}
			if (flag)
			{
				this.AI.Game.WithdrawStack(Province.LandNode.CurrentStack);
				this.AI.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { Province });
				Province.OwnerRealm.DiplomacyManager.TriggerEvent(this.AI.Realm, "LiberatedAlly");
				if (Province.OwnerRealm.AIPlayer == null)
				{
					GameText gameText = GameText.CreateLocalised("MSG_AILIBERATE_TITLE", new object[0]);
					GameText gameText2 = GameText.CreateLocalised("MSG_AILIBERATE_TEXT", new object[0]);
					gameText2.AddChildText(GameText.CreateLocalised(this.AI.Realm.DisplayName, new object[0]));
					gameText2.AddChildText(GameText.CreateLocalised(Province.DisplayName, new object[0]));
					this.AI.Game.GameCore.MessageHandler.ShowInfoMessage(gameText, gameText2);
					return;
				}
			}
			else
			{
				Province.OwnerRealm.DiplomacyManager.TriggerEvent(this.AI.Realm, "AnnexAlly");
				if (Province.OwnerRealm.AIPlayer == null)
				{
					GameText gameText3 = GameText.CreateLocalised("MSG_AISTEALTITLE", new object[0]);
					GameText gameText4 = GameText.CreateLocalised("MSG_AISTEAL_TEXT", new object[0]);
					gameText4.AddChildText(GameText.CreateLocalised(this.AI.Realm.DisplayName, new object[0]));
					gameText4.AddChildText(GameText.CreateLocalised(Province.DisplayName, new object[0]));
					this.AI.Game.GameCore.MessageHandler.ShowInfoMessage(gameText3, gameText4);
					return;
				}
				Province.OwnerRealm.AIPlayer.RelationsManager.RespondToFailedLiberate(this.AI.Realm);
			}
		}

		public void RespondToFailedLiberate(WorkingRealm Realm)
		{
			if (Realm.DiplomacyManager.GetRelation(this.AI.Realm) == RelationStates.Peace)
			{
				if (this.AI.RNG.Next(100) > 50)
				{
					this.AI.Game.AllianceController.EstablishWar(this.AI.Realm, Realm);
					return;
				}
			}
			else if (this.AI.RNG.Next(100) > 50)
			{
				this.AI.Game.AllianceController.BreakCurrentTreaty(this.AI.Realm, Realm, true, false);
			}
		}

		public int GetDistance(WorkingRealm RealmA, WorkingRealm RealmB)
		{
			SovereigntyTK.Game.Path path = this.AI.Game.PathManager.GetPath(RealmA.CapitolProvince.LandNode, RealmB.CapitolProvince.LandNode, null, false, this.AI.Realm, false);
			List<WorkingRealm> list = new List<WorkingRealm>();
			List<WorkingZone> list2 = new List<WorkingZone>();
			foreach (PathPoint pathPoint in path.PathPoints)
			{
				WorkingZone zone = pathPoint.Node.Zone;
				WorkingProvince province = pathPoint.Node.Province;
				if (zone != null && !list2.Contains(zone))
				{
					list2.Add(zone);
				}
				if (province != null && !list.Contains(province.OwnerRealm))
				{
					list.Add(province.OwnerRealm);
				}
			}
			return list.Count + list2.Count - 2;
		}

		internal void OfferTreaties()
		{
			this.AI.Log("");
			this.AI.Log("Diplomacy manager updating (new treaties phase)");
			foreach (WorkingRealm workingRealm in from x in this.AI.Game.AllRealms.Values
				where !x.RealmIsDead
				orderby this.AI.Realm.DiplomacyManager.GetDisposition(x) descending
				select x)
			{
				if (workingRealm != this.AI.Game.RebelRealm)
				{
					RelationStates relation = workingRealm.DiplomacyManager.GetRelation(this.AI.Realm);
					this.AI.Log(string.Concat(new object[] { "    Considering realm ", workingRealm.Name, ", current state: ", relation }));
					int num = this.AI.Game.AllRealms.Values.Count((WorkingRealm x) => !x.RealmIsDead && x.DiplomacyManager.HasTreaty(this.AI.Realm));
					if (num >= 8)
					{
						this.AI.Log("    Cannot offer any treaties, already have " + num);
					}
					else
					{
						switch (relation)
						{
						case RelationStates.Defence:
							if (!this.AI.Game.AllianceController.TreatyIsPossible(this.AI.Realm, workingRealm, TreatyTypes.Alliance))
							{
								this.AI.Log("    Cannot offer Alliance, treaty not possible");
							}
							else if (!this.AI.Game.AllianceController.CanOfferAlliance(this.AI.Realm, workingRealm))
							{
								this.AI.Log("    Cannot offer Alliance, treaty not possible");
							}
							else
							{
								int num2 = this.AI.Game.AllRealms.Values.Count((WorkingRealm x) => !x.RealmIsDead && x.DiplomacyManager.GetRelation(this.AI.Realm) == RelationStates.Alliance);
								if (num2 >= 5)
								{
									this.AI.Log("    Cannot offer Alliance, already have " + num2);
								}
								else
								{
									int distance = this.GetDistance(this.AI.Realm, workingRealm);
									if (distance > 2)
									{
										this.AI.Log("    Cannot offer Alliance, realm is too far away (" + distance + ")");
									}
									else
									{
										int num3 = 20;
										int num4 = (int)this.AI.Realm.DiplomacyManager.GetDisposition(workingRealm);
										num4 -= 5 * (workingRealm.Enemies.Count - 1);
										if (num4 < num3)
										{
											this.AI.Log("    Cannot offer Alliance, disposition is " + num4);
										}
										else
										{
											this.AI.Log("    Offering Alliance");
											TradeOfferList tradeOfferList = new TradeOfferList(this.AI.Game, null, false);
											tradeOfferList.Treaty = TreatyTypes.Alliance;
											if (this.AI.TradeManager.AttemptTrade(tradeOfferList, workingRealm, false))
											{
												return;
											}
										}
									}
								}
							}
							break;
						case RelationStates.NAP:
							if (!this.AI.Game.AllianceController.TreatyIsPossible(this.AI.Realm, workingRealm, TreatyTypes.MutualDefence))
							{
								this.AI.Log("    Cannot offer Mutual Defence, treaty not possible");
							}
							else
							{
								int num5 = this.AI.Game.AllRealms.Values.Count((WorkingRealm x) => !x.RealmIsDead && x.DiplomacyManager.GetRelation(this.AI.Realm) == RelationStates.Defence);
								if (num5 >= 3)
								{
									this.AI.Log("    Cannot offer Mutual Defence, already have " + num5);
								}
								else
								{
									int distance2 = this.GetDistance(this.AI.Realm, workingRealm);
									if (distance2 > 2)
									{
										this.AI.Log("    Cannot offer Mutual Defence, realm is too far away (" + distance2 + ")");
									}
									else
									{
										int num6 = 10;
										int num7 = (int)this.AI.Realm.DiplomacyManager.GetDisposition(workingRealm);
										num7 -= 10 * (workingRealm.Enemies.Count - 1);
										if (num7 < num6)
										{
											this.AI.Log("    Cannot offer Mutual Defence, disposition is " + num7);
										}
										else
										{
											this.AI.Log("    OFfering Mutual Defence");
											TradeOfferList tradeOfferList2 = new TradeOfferList(this.AI.Game, null, false);
											tradeOfferList2.Treaty = TreatyTypes.MutualDefence;
											if (this.AI.TradeManager.AttemptTrade(tradeOfferList2, workingRealm, false))
											{
												return;
											}
										}
									}
								}
							}
							break;
						case RelationStates.Peace:
							if (!this.AI.Game.AllianceController.TreatyIsPossible(this.AI.Realm, workingRealm, TreatyTypes.NonAggression))
							{
								this.AI.Log("    Cannot offer NAP, treaty not possible");
							}
							else
							{
								int num8 = this.AI.Game.AllRealms.Values.Count((WorkingRealm x) => !x.RealmIsDead && x.DiplomacyManager.GetRelation(this.AI.Realm) == RelationStates.NAP);
								if (num8 >= 3)
								{
									this.AI.Log("    Cannot offer NAP, already have " + num8);
								}
								else
								{
									int num9 = -5;
									int num10 = (int)this.AI.Realm.DiplomacyManager.GetDisposition(workingRealm);
									if (num10 < num9)
									{
										this.AI.Log("    Cannot offer NAP, disposition is " + num10);
									}
									else
									{
										this.AI.Log("    Offering NAP");
										TradeOfferList tradeOfferList3 = new TradeOfferList(this.AI.Game, null, false);
										tradeOfferList3.Treaty = TreatyTypes.NonAggression;
										if (this.AI.TradeManager.AttemptTrade(tradeOfferList3, workingRealm, false))
										{
											return;
										}
									}
								}
							}
							break;
						}
					}
				}
			}
		}

		internal void Save(BinaryWriter w)
		{
			this.Funds.Save(w);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.Funds.Load(r, SaveVersion);
		}

		private AIPlayer AI;

		public AIFundData Funds;
	}
}
