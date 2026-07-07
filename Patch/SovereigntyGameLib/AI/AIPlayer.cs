using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using SovereigntyTK.AI.V2;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.Game.Trade;
using SovereigntyTK.UI;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.AI
{
	public class AIPlayer
	{
		public int RealmID
		{
			get
			{
				return this.m_RealmID;
			}
		}

		public WorkingRealm Realm
		{
			get
			{
				if (this.m_Realm == null)
				{
					this.Game.AllRealms.TryGetValue(this.m_RealmID, out this.m_Realm);
				}
				return this.m_Realm;
			}
		}

		public AIPlayer(SovereigntyGame Game, int RealmID)
		{
			this.Game = Game;
			this.m_RealmID = RealmID;
			this.RNG = new Random();
			this.SW = new Stopwatch();
			this.ProvinceManager = new AIProvinceManager(this, Game);
			this.PrisonManager = new AIPrisonManager(this, Game);
			this.BattleManager = new AIBattleManager(this, Game);
			this.MagicManager = new AIMagicManager(this, Game);
			this.ResourceManager = new AIResourceManager(this, Game);
			this.TreatyManager = new AITreatyManager(this, Game);
			this.UnitManager = new AIUnitManager(this, Game);
			this.Utility = new AIUtilities(this, Game);
			this.Trade = new AITradeUtilities(this, Game);
			this.InvasionTargets = new Dictionary<int, int>();
			this.LustModifiers = new Dictionary<string, int>();
			this.Wars = new Dictionary<int, WarData>();
			this.IgnoreProvinces = new List<int>();
			Game.GameCore.RegisterEvent(new GenericDelegate(this.GameCore_OnScriptEvent), "BattleCompleted");
			Game.AllianceController.OnWarDeclared += this.AllianceController_OnWarDeclared;
			Game.AllianceController.OnWarEnded += this.AllianceController_OnWarEnded;
		}

		public AIPlayer(SovereigntyGame Game, BinaryReader r, int SaveVersion)
		{
			this.Game = Game;
			this.m_RealmID = r.ReadInt32();
			this.RNG = new Random();
			this.ProvinceManager = new AIProvinceManager(this, Game);
			this.PrisonManager = new AIPrisonManager(this, Game);
			this.BattleManager = new AIBattleManager(this, Game);
			this.MagicManager = new AIMagicManager(this, Game);
			this.ResourceManager = new AIResourceManager(this, Game);
			this.TreatyManager = new AITreatyManager(this, Game);
			this.UnitManager = new AIUnitManager(this, Game);
			this.Utility = new AIUtilities(this, Game);
			this.Trade = new AITradeUtilities(this, Game);
			this.InvasionTargets = new Dictionary<int, int>();
			this.LustModifiers = new Dictionary<string, int>();
			this.Wars = new Dictionary<int, WarData>();
			Game.GameCore.RegisterEvent(new GenericDelegate(this.GameCore_OnScriptEvent), "BattleCompleted");
			Game.AllianceController.OnWarDeclared += this.AllianceController_OnWarDeclared;
			Game.AllianceController.OnWarEnded += this.AllianceController_OnWarEnded;
			if (r.ReadBoolean())
			{
				this.BattleManager.Load(r, SaveVersion);
			}
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = r.ReadInt32();
				WarData warData = new WarData(Game, this, r, SaveVersion);
				this.Wars.Add(num2, warData);
			}
			num = r.ReadInt32();
			for (int j = 0; j < num; j++)
			{
				this.InvasionTargets.Add(r.ReadInt32(), r.ReadInt32());
			}
			this.IgnoreProvinces = new List<int>();
			num = r.ReadInt32();
			for (int k = 0; k < num; k++)
			{
				this.IgnoreProvinces.Add(r.ReadInt32());
			}
			this.IgnoreCapitolLust = r.ReadBoolean();
			num = r.ReadInt32();
			for (int l = 0; l < num; l++)
			{
				this.LustModifiers.Add(r.ReadString(), r.ReadInt32());
			}
			this.RebelProvinceID = r.ReadInt32();
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.m_RealmID);
			if (this.PendingAction != null && this.PendingAction.ActionName == AIActionTypes.Attack)
			{
				w.Write(true);
				this.BattleManager.Save(w);
			}
			else
			{
				w.Write(false);
			}
			w.Write(this.Wars.Count);
			foreach (KeyValuePair<int, WarData> keyValuePair in this.Wars)
			{
				w.Write(keyValuePair.Key);
				keyValuePair.Value.Save(w);
			}
			w.Write(this.InvasionTargets.Count);
			foreach (KeyValuePair<int, int> keyValuePair2 in this.InvasionTargets)
			{
				w.Write(keyValuePair2.Key);
				w.Write(keyValuePair2.Value);
			}
			w.Write(this.IgnoreProvinces.Count);
			foreach (int num in this.IgnoreProvinces)
			{
				w.Write(num);
			}
			w.Write(this.IgnoreCapitolLust);
			w.Write(this.LustModifiers.Count);
			foreach (KeyValuePair<string, int> keyValuePair3 in this.LustModifiers)
			{
				w.Write(keyValuePair3.Key);
				w.Write(keyValuePair3.Value);
			}
			w.Write(this.RebelProvinceID);
		}

		public void ModifyProvinceLust(string ProvinceName, int LustModifier)
		{
			if (!this.LustModifiers.ContainsKey(ProvinceName))
			{
				this.LustModifiers.Add(ProvinceName, 0);
			}
			Dictionary<string, int> lustModifiers;
			(lustModifiers = this.LustModifiers)[ProvinceName] = lustModifiers[ProvinceName] + LustModifier;
		}

		public void PromoteUnits()
		{
			foreach (WorkingUnit workingUnit in this.Realm.Units.Where((WorkingUnit x) => x.ReadytoPromote()))
			{
				if (this.RNG.Next(1) == 0)
				{
					UnitFlag unitFlag = UnitFlag.CreateNamedFlag(this.Game.GameCore, workingUnit.GetFirstMedalName());
					workingUnit.GrantFlag(unitFlag);
					workingUnit.Medals++;
					workingUnit.MedalNames.Add(unitFlag.Name);
					this.Game.GameCore.FireEvent("UnitPromoted", new object[] { workingUnit });
				}
				else
				{
					UnitFlag unitFlag2 = UnitFlag.CreateNamedFlag(this.Game.GameCore, workingUnit.GetSecondMedalName());
					workingUnit.GrantFlag(unitFlag2);
					workingUnit.Medals++;
					workingUnit.MedalNames.Add(unitFlag2.Name);
					this.Game.GameCore.FireEvent("UnitPromoted", new object[] { workingUnit });
				}
			}
		}

		private void AllianceController_OnWarEnded(WorkingRealm Realm1, WorkingRealm Realm2)
		{
			WorkingRealm workingRealm = null;
			if (Realm1 == this.Realm)
			{
				workingRealm = Realm2;
			}
			if (Realm2 == this.Realm)
			{
				workingRealm = Realm1;
			}
			if (workingRealm == null)
			{
				return;
			}
			if (!this.Wars.ContainsKey(workingRealm.ID))
			{
				return;
			}
			this.Wars[workingRealm.ID].Dispose();
			this.Wars.Remove(workingRealm.ID);
		}

		private void AllianceController_OnWarDeclared(WorkingRealm Realm1, WorkingRealm Realm2)
		{
			WorkingRealm workingRealm = null;
			if (Realm1 == this.Realm)
			{
				workingRealm = Realm2;
			}
			if (Realm2 == this.Realm)
			{
				workingRealm = Realm1;
			}
			if (workingRealm == null)
			{
				return;
			}
			if (this.Wars.ContainsKey(workingRealm.ID))
			{
				return;
			}
			this.Wars.Add(workingRealm.ID, new WarData(this.Game, workingRealm, this));
		}

		private void GameCore_OnScriptEvent(string EventName, params object[] Args)
		{
			if (EventName == "BattleCompleted")
			{
				this.StopWaiting();
			}
		}

		public void ResumeTurn()
		{
			Thread thread = new Thread(new ThreadStart(this.ContinueTurn));
			thread.Start();
		}

		public void BeginTurn()
		{
			Thread thread = new Thread(new ThreadStart(this.TakeTurn));
			thread.Start();
		}

		public List<Tuple<string, int>> GetPeaceDesireBreakdown(WorkingRealm Enemy)
		{
			List<Tuple<string, int>> list = new List<Tuple<string, int>>();
			list.Add(new Tuple<string, int>("Income:", this.GetPeaceIncomeValue()));
			list.Add(new Tuple<string, int>("Damaged provinces:", this.GetPeaceDamageValue()));
			if (this.Realm == this.Game.PlayerRealm)
			{
				list.AddRange(Enemy.AIPlayer.WarManager.Wars[this.Realm.ID].GetWarScoreBreakDown());
			}
			else
			{
				foreach (Tuple<string, int> tuple in this.Wars[Enemy.ID].GetWarScoreBreakDown())
				{
					list.Add(new Tuple<string, int>(tuple.Item1, tuple.Item2 * -1));
				}
			}
			list.Add(new Tuple<string, int>("Value of war:", -1 * ((int)this.TreatyManager.GetWarValue(Enemy) / 10)));
			list.Add(new Tuple<string, int>("Other wars:", this.GetPeaceEnemiesValue()));
			list.Add(new Tuple<string, int>("Expecting to win:", (int)this.GetPeaceStrategicValue(Enemy)));
			list.Add(new Tuple<string, int>("Disposition:", (int)(this.Realm.DiplomacyManager.GetDisposition(Enemy) / 5f)));
			return list;
		}

		private int GetPeaceEnemiesValue()
		{
			int num = this.Realm.Enemies.Count - 2;
			return num * 2;
		}

		private int GetPeaceDamageValue()
		{
			int num = this.Realm.Provinces.Count((WorkingProvince x) => x.GetUnmodifiedEconomy() == 0);
			int num2 = this.Realm.Provinces.Count / 5;
			num -= num2;
			if (num < 0)
			{
				num = 0;
			}
			return num * 5;
		}

		private int GetPeaceIncomeValue()
		{
			int num = this.Game.EconomyController.GetRealmTotalIncome(this.Realm) - this.Game.EconomyController.GetTotalExpenses(this.Realm);
			num /= 400;
			num *= -1;
			if (num < -20)
			{
				num = -20;
			}
			if (num > 20)
			{
				num = 20;
			}
			return num;
		}

		public int GetPeaceDesire(WorkingRealm Enemy)
		{
			float num = 0f;
			num += (float)this.GetPeaceIncomeValue();
			num += (float)this.GetPeaceDamageValue();
			if (this.Realm == this.Game.PlayerRealm)
			{
				num += (float)Enemy.AIPlayer.WarManager.Wars[this.Realm.ID].GetWarScore();
			}
			else
			{
				num -= (float)this.Wars[Enemy.ID].GetWarScore();
			}
			num -= this.TreatyManager.GetWarValue(Enemy) / 10f;
			num += (float)this.GetPeaceEnemiesValue();
			num += this.GetPeaceStrategicValue(Enemy);
			num += this.Realm.DiplomacyManager.GetDisposition(Enemy) / 5f;
			if (num > 100f)
			{
				num = 100f;
			}
			if (num < -100f)
			{
				num = -100f;
			}
			return (int)num;
		}

		private float GetPeaceStrategicValue(WorkingRealm Enemy)
		{
			if (this.TreatyManager.VictoryIsPossible(Enemy))
			{
				return -10f;
			}
			if (this.TreatyManager.DefeatIsLikely(Enemy))
			{
				return 10f;
			}
			return 0f;
		}

		public void CheckForAction()
		{
			if (this.PendingAction == null)
			{
				return;
			}
			switch (this.PendingAction.ActionName)
			{
			case AIActionTypes.Endturn:
				this.Game.TurnController.RequestEndTurn();
				this.PendingAction = null;
				return;
			case AIActionTypes.PurchaseUnits:
				foreach (UnitData unitData in this.PendingAction.UnitTypes)
				{
					if (this.Realm.Gold >= this.Realm.UnitPurchaseManager.GetUnitCost(unitData))
					{
						this.Realm.QueueUnit(this.Game.CreateUnit(this.Realm.ID, unitData), false, true);
					}
				}
				this.PendingAction = null;
				return;
			case AIActionTypes.DeployUnits:
				foreach (KeyValuePair<UnitQueueItem, ActivePathNode> keyValuePair in this.PendingAction.DeployTargets)
				{
					if (keyValuePair.Value.CurrentStack != null)
					{
						if (keyValuePair.Value.CurrentStack.Units.Count((WorkingUnit x) => x.Class != UnitClasses.Fort) >= 20)
						{
							continue;
						}
					}
					this.Game.DeployUnit(keyValuePair.Key.Unit, keyValuePair.Value);
					this.Realm.EndUnitTraining(keyValuePair.Key);
				}
				this.PendingAction = null;
				return;
			case AIActionTypes.MoveUnits:
				foreach (UnitMoveData unitMoveData in this.PendingAction.MoveTargets)
				{
					WorkingStack workingStack = unitMoveData.TargetNode.GetRealmStack(this.Realm);
					WorkingStack ownerStack = unitMoveData.Unit.OwnerStack;
					if (workingStack == null || workingStack.Disposed)
					{
						workingStack = this.Game.CreateStack(this.Realm.ID, unitMoveData.TargetNode.ID, false);
						if (unitMoveData.TargetNode.Province != null && !unitMoveData.TargetNode.Province.Occupied && unitMoveData.TargetNode.Province.OwnerRealm != this.Realm)
						{
							unitMoveData.TargetNode.AllyStacks.Add(workingStack.ID);
						}
						else
						{
							unitMoveData.TargetNode.CurrentStackID = workingStack.ID;
						}
					}
					this.Game.MoveUnit(ownerStack, workingStack, unitMoveData.Unit, unitMoveData.MovePath, false);
					unitMoveData.Unit.ClearMoves();
					if (ownerStack.Units.Count == 0)
					{
						WorkingProvince sourceProvince = (ownerStack.Node != null) ? ownerStack.Node.Province : null;
						this.Game.RemoveStack(ownerStack);
						if (sourceProvince != null && !sourceProvince.Occupied)
						{
							this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { sourceProvince });
						}
					}
					if (workingStack.Units.Count == 0)
					{
						WorkingProvince destProvince = (workingStack.Node != null) ? workingStack.Node.Province : null;
						this.Game.RemoveStack(workingStack);
						if (destProvince != null && !destProvince.Occupied)
						{
							this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { destProvince });
						}
					}
				}
				this.PendingAction = null;
				return;
			case AIActionTypes.DeclareWar:
				this.Game.AllianceController.EstablishWar(this.Realm, this.PendingAction.Realm);
				this.PendingAction = null;
				return;
			case AIActionTypes.Attack:
			{
				if (this.PendingAction.Completed)
				{
					return;
				}
				WorkingRealm occupierRealm = this.PendingAction.Node.Province.OccupierRealm;
				if (this.Realm.DiplomacyManager.GetRelation(occupierRealm) != RelationStates.War)
				{
					if (!this.InvasionTargets.ContainsKey(occupierRealm.ID))
					{
						throw new Exception("Attempted to invade invalid target realm");
					}
					this.Game.AllianceController.EstablishWar(this.Realm, occupierRealm);
				}
				SovereigntyTK.Game.Path path = this.Game.PathManager.GetPath(this.PendingAction.Stack.Node, this.PendingAction.Node, this.PendingAction.Units, false, this.PendingAction.Stack.Owner, false);
				if (this.PendingAction.Node.CurrentStack == null || this.PendingAction.Node.CurrentStack.Units.Count <= 0)
				{
					WorkingStack workingStack2 = this.PendingAction.Node.CurrentStack;
					if (workingStack2 == null || workingStack2.Disposed)
					{
						workingStack2 = this.Game.CreateStack(this.Realm.ID, this.PendingAction.Node.ID, true);
					}
					foreach (WorkingUnit workingUnit in this.PendingAction.Units)
					{
						if (workingStack2.Disposed)
						{
							workingStack2 = this.Game.CreateStack(this.Realm.ID, this.PendingAction.Node.ID, true);
						}
						this.Game.MoveUnit(this.PendingAction.Stack, workingStack2, workingUnit, path, false);
						workingUnit.ClearMoves();
					}
					if (this.PendingAction.Stack.Units.Count == 0)
					{
						WorkingProvince attackSourceProvince = (this.PendingAction.Stack.Node != null) ? this.PendingAction.Stack.Node.Province : null;
						this.Game.RemoveStack(this.PendingAction.Stack);
						if (attackSourceProvince != null && !attackSourceProvince.Occupied)
						{
							this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { attackSourceProvince });
						}
					}
					if (this.PendingAction.Node.Province != null)
					{
						this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { this.PendingAction.Node.Province });
					}
					this.PendingAction = null;
					return;
				}
				foreach (WorkingUnit workingUnit2 in this.PendingAction.Stack.Units)
				{
					workingUnit2.Selected = false;
				}
				foreach (WorkingUnit workingUnit3 in this.PendingAction.Units)
				{
					workingUnit3.Selected = true;
				}
				this.Game.StartBattle(this.PendingAction.Stack, this.PendingAction.Node.CurrentStack, path);
				if (this.Game.PendingBattle == null)
				{
					this.PendingAction = null;
					return;
				}
				this.PendingAction.Completed = true;
				return;
			}
			case AIActionTypes.LearnSpell:
				this.Realm.MagicData.LearnSpell(this.PendingAction.Spell.Name);
				this.PendingAction = null;
				return;
			case AIActionTypes.CastSpell:
			{
				this.Game.FinishSpellCasting(this.PendingAction.SpellEffect, this.PendingAction.SpellTarget);
				GameText gameText = GameText.CreateLocalised("SPELLCASTMSG", new object[0]);
				gameText.AddChildText(GameText.CreateLocalised(this.Realm.DisplayName, new object[0]));
				gameText.AddChildText(GameText.CreateLocalised(this.PendingAction.SpellEffect.SpellData.DisplayName, new object[0]));
				this.Game.GameCore.FireEvent("TickerMessage", new object[]
				{
					new TickerMessage(gameText, TickerMessageType.Magic, 1)
				});
				this.PendingAction = null;
				return;
			}
			case AIActionTypes.MarketTrade:
				foreach (MarketTradeData marketTradeData in this.PendingAction.TradeList)
				{
					if (marketTradeData.Purchase)
					{
						this.Game.Marketplace.BuyFromMarket(this.Realm, marketTradeData.Resource, marketTradeData.Quantity);
						if (this.Game.GameCore.DebugMessagesEnabled)
						{
							this.Game.GameCore.FireEvent("TickerMessage", new object[]
							{
								new TickerMessage(GameText.CreateFromLiteral(string.Concat(new object[]
								{
									"AI ",
									this.Realm.Name,
									" has purchased ",
									marketTradeData.Quantity,
									" ",
									marketTradeData.Resource.ResourceName
								})), TickerMessageType.Debug, 1)
							});
						}
					}
					else
					{
						this.Game.Marketplace.SellToMarket(this.Realm, marketTradeData.Resource, marketTradeData.Quantity);
						if (this.Game.GameCore.DebugMessagesEnabled)
						{
							this.Game.GameCore.FireEvent("TickerMessage", new object[]
							{
								new TickerMessage(GameText.CreateFromLiteral(string.Concat(new object[]
								{
									"AI ",
									this.Realm.Name,
									" has sold ",
									marketTradeData.Quantity,
									" ",
									marketTradeData.Resource.ResourceName
								})), TickerMessageType.Debug, 1)
							});
						}
					}
				}
				this.PendingAction = null;
				return;
			case AIActionTypes.OfferTrade:
				if (this.PendingAction.Completed)
				{
					return;
				}
				if (this.Game.GameCore.DebugMessagesEnabled)
				{
					this.Game.GameCore.FireEvent("TickerMessage", new object[]
					{
						new TickerMessage(GameText.CreateFromLiteral("AI " + this.Realm.Name + " has offered trade to " + this.PendingAction.Realm.Name), TickerMessageType.Debug, 1)
					});
				}
				if (this.PendingAction.Realm.AIPlayer == null)
				{
					this.Game.GameCore.FireEvent("PlayerTradeOffer", new object[] { this.PendingAction.TradeOffer });
					if (this.PendingAction != null)
					{
						this.PendingAction.Completed = true;
						return;
					}
				}
				else
				{
					WorkingAgent freeAgent = this.Trade.GetFreeAgent();
					freeAgent.TradeOffer = this.PendingAction.TradeOffer;
					freeAgent.Send(this.PendingAction.Realm, null, AgentModes.CarryTradeOffer);
					WorkingRealm realm = this.Realm;
					WorkingRealm workingRealm = this.Game.AllRealms[this.PendingAction.TradeOffer.TargetRealmID];
					if (this.PendingAction.TradeOffer.SenderOffers.Treaty != TreatyTypes.None)
					{
						this.Game.AllianceController.FormTreaty(realm, workingRealm, this.PendingAction.TradeOffer.SenderOffers.Treaty);
					}
					foreach (int num in this.PendingAction.TradeOffer.SenderOffers.GetProvinces())
					{
						WorkingProvince workingProvince = this.Game.AllProvinces[num];
						this.Game.ChangeProvinceOwner(workingProvince, workingRealm);
						if (workingProvince.LandNode.CurrentStack != null)
						{
							this.Game.WithdrawStack(workingProvince.LandNode.CurrentStack);
						}
						if (workingProvince.HarbourNode != null && workingProvince.HarbourNode.CurrentStack != null)
						{
							this.Game.WithdrawStack(workingProvince.HarbourNode.CurrentStack);
						}
						this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { workingProvince });
					}
					foreach (int num2 in this.PendingAction.TradeOffer.TargetOffers.GetProvinces())
					{
						WorkingProvince workingProvince2 = this.Game.AllProvinces[num2];
						this.Game.ChangeProvinceOwner(workingProvince2, realm);
						if (workingProvince2.LandNode.CurrentStack != null)
						{
							this.Game.WithdrawStack(workingProvince2.LandNode.CurrentStack);
						}
						if (workingProvince2.HarbourNode != null && workingProvince2.HarbourNode.CurrentStack != null)
						{
							this.Game.WithdrawStack(workingProvince2.HarbourNode.CurrentStack);
						}
						this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { workingProvince2 });
					}
					this.PendingAction = null;
				}
				break;
			case AIActionTypes.CreateRebelStack:
				if (this.PendingAction.Province.OccupierRealm == this.Game.PlayerRealm)
				{
					MessageBoxData messageBoxData = new MessageBoxData();
					messageBoxData.MsgType = MessageType.Revolt;
					messageBoxData.DisplayType = MessageBoxType.Info;
					messageBoxData.CaptionText = GameText.CreateLocalised("REVOLT_HEADER", new object[0]);
					messageBoxData.MessageText = GameText.CreateLocalised("REVOLT_TEXT", new object[0]);
					messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(this.PendingAction.Province.DisplayName, new object[0]));
					messageBoxData.Province = this.PendingAction.Province;
					this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
				}
				else
				{
					GameText gameText2 = GameText.CreateLocalised("REVOLT_TICKER", new object[0]);
					gameText2.AddChildText(GameText.CreateLocalised(this.PendingAction.Province.DisplayName, new object[0]));
					this.Game.GameCore.FireEvent("TickerMessage", new object[]
					{
						new TickerMessage(gameText2, TickerMessageType.Military, 1)
					});
				}
				this.RebelStack = this.CreateRebelArmy(this.PendingAction.Province);
				this.PendingAction = null;
				return;
			case AIActionTypes.RebelOccupy:
				this.RebelStack.NodeID = this.PendingAction.Province.LandNode.ID;
				this.PendingAction.Province.LandNode.CurrentStackID = this.RebelStack.ID;
				this.RebelStack.UpdateSprite();
				this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { this.PendingAction.Province });
				this.PendingAction = null;
				return;
			case AIActionTypes.DebugStop:
				break;
			case AIActionTypes.Enslave:
				this.ShowPrisonerMessage(this.PendingAction.Units, "PRISON_AI_ENSLAVE_TITLE", "PRISON_AI_ENSLAVE_TEXT");
				this.Game.PrisonerController.EnslaveUnits(this.PendingAction.Units, this.Realm);
				this.PendingAction = null;
				return;
			case AIActionTypes.ReleaseUnits:
				this.ShowPrisonerMessage(this.PendingAction.Units, "PRISON_AI_RELEASE_TITLE", "PRISON_AI_RELEASE_TEXT");
				this.Game.PrisonerController.ReleaseUnits(this.PendingAction.Units, this.Realm);
				this.PendingAction = null;
				return;
			case AIActionTypes.ExecuteUnits:
				this.ShowPrisonerMessage(this.PendingAction.Units, "PRISON_AI_EXECUTE_TITLE", "PRISON_AI_EXECUTE_TEXT");
				this.Game.PrisonerController.ExecuteUnits(this.PendingAction.Units, this.Realm);
				this.PendingAction = null;
				return;
			case AIActionTypes.SacrificeUnits:
				this.ShowPrisonerMessage(this.PendingAction.Units, "PRISON_AI_SACRIFICE_TITLE", "PRISON_AI_SACRIFICE_TEXT");
				this.Game.PrisonerController.SacrificeUnits(this.PendingAction.Units, this.Realm);
				this.PendingAction = null;
				return;
			case AIActionTypes.RecruitUnits:
				this.ShowPrisonerMessage(this.PendingAction.Units, "PRISON_AI_RECRUIT_TITLE", "PRISON_AI_RECRUIT_TEXT");
				this.Game.PrisonerController.RecruitUnits(this.PendingAction.Units, this.Realm, false);
				this.PendingAction = null;
				return;
			case AIActionTypes.RaiseUnits:
				this.ShowPrisonerMessage(this.PendingAction.Units, "PRISON_AI_RAISE_TITLE", "PRISON_AI_RAISE_TEXT");
				this.Game.PrisonerController.RecruitUnits(this.PendingAction.Units, this.Realm, true);
				this.PendingAction = null;
				return;
			case AIActionTypes.ImproveProvinces:
				foreach (WorkingProvince workingProvince3 in this.PendingAction.ProvinceList)
				{
					workingProvince3.ImproveEconomy(1);
				}
				this.PendingAction = null;
				return;
			case AIActionTypes.EndTreaties:
				foreach (WorkingRealm workingRealm2 in this.PendingAction.Realms)
				{
					this.Game.AllianceController.BreakCurrentTreaty(this.Realm, workingRealm2, true, false);
				}
				this.PendingAction = null;
				return;
			case AIActionTypes.ConstructBuilding:
			{
				BuildingEffect buildingEffect = BuildingEffect.CreateEffect(this.Game, this.PendingAction.Building.Data, this.PendingAction.Province);
				buildingEffect.Construct(this.Realm, this.PendingAction.Province, true);
				this.PendingAction.Province.ConstructionState = ConstructionStates.Building;
				this.PendingAction = null;
				return;
			}
			case AIActionTypes.Intercept:
			{
				if (this.PendingAction.Completed)
				{
					return;
				}
				WorkingRealm occupierRealm2 = this.PendingAction.Node.Province.OccupierRealm;
				if (this.Realm.DiplomacyManager.GetRelation(occupierRealm2) != RelationStates.War)
				{
					if (!this.InvasionTargets.ContainsKey(occupierRealm2.ID))
					{
						throw new Exception("Attempted to invade invalid target realm");
					}
					this.Game.AllianceController.EstablishWar(this.Realm, occupierRealm2);
				}
				foreach (WorkingUnit workingUnit4 in this.PendingAction.Stack.Units)
				{
					workingUnit4.Selected = false;
				}
				foreach (WorkingUnit workingUnit5 in this.PendingAction.Units)
				{
					workingUnit5.Selected = true;
				}
				this.Game.StartBattle(this.PendingAction.Stack, this.PendingAction.InterceptStack, null);
				if (this.Game.PendingBattle == null)
				{
					this.PendingAction = null;
					return;
				}
				this.PendingAction.Completed = true;
				return;
			}
			default:
				return;
			}
		}

		private void ShowPrisonerMessage(List<WorkingUnit> Units, string Title, string Text)
		{
			List<WorkingUnit> list = Units.Where((WorkingUnit x) => x.OwnerRealm == this.Game.PlayerRealm).ToList<WorkingUnit>();
			if (list.Count == 0)
			{
				return;
			}
			GameText gameText = GameText.CreateLocalised(Title, new object[0]);
			List<GameText> list2 = new List<GameText>();
			GameText gameText2 = GameText.CreateLocalised(Text, new object[0]);
			gameText2.AddChildText(GameText.CreateLocalised(this.Realm.DisplayName, new object[0]));
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
			this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
		}

		public void Dispose()
		{
			this.Game.GameCore.UnregisterEvent(new GenericDelegate(this.GameCore_OnScriptEvent), "BattleCompleted");
			this.Disposed = true;
		}

		public void StopWaiting()
		{
			this.PendingAction = null;
		}

		public void SetAction(AIAction Action)
		{
			if (this.Game.PendingDispose)
			{
				return;
			}
			this.PendingAction = Action;
			while (this.PendingAction != null && !this.Disposed && !this.Game.PendingDispose)
			{
				Thread.Sleep(1);
			}
		}

		private void BeginRevoltChecks()
		{
			this.RebelProvinceID = 0;
			this.CheckForRevolts();
		}

		private void CheckForRevolts()
		{
			Random random = new Random();
			while (RebelProvinceID < Game.AllProvinces.Count)
			{
				WorkingProvince value = Game.AllProvinces.ElementAt(RebelProvinceID).Value;
				RebelProvinceID++;
				if (!value.Occupied && !value.IsCapitol)
				{
					int num = value.RevoltChance;
					int maxValue = 100;
					_ = value.OwnerRealm.AIPlayer;
					if (random.Next(maxValue) < num)
					{
						CreateRebellion(value);
					}
				}
			}
		}


		private void CreateRebellion(WorkingProvince Province)
		{
			this.SetAction(new AIAction(AIActionTypes.CreateRebelStack)
			{
				Province = Province
			});
			foreach (WorkingUnit workingUnit in this.RebelStack.Units)
			{
				workingUnit.Selected = true;
			}
			if (Province.LandNode.CurrentStack != null)
			{
				this.SetAction(new AIAction(AIActionTypes.Attack)
				{
					Province = Province,
					Stack = this.RebelStack,
					Units = this.RebelStack.Units.ToList<WorkingUnit>(),
					Node = Province.LandNode,
					Realm = Province.OwnerRealm
				});
				return;
			}
			this.SetAction(new AIAction(AIActionTypes.RebelOccupy)
			{
				Province = Province
			});
		}

		public WorkingStack CreateRebelArmy(WorkingProvince Province)
		{
			int num = 8 + this.RNG.Next(7);
			int num2 = this.RNG.Next(Province.LandNode.ConnectedNodes.Count((ActiveNodeConnection x) => x.TargetNode.NodeType == PathNodeTypes.Land));
			ActivePathNode targetNode = Province.LandNode.ConnectedNodes.Where((ActiveNodeConnection x) => x.TargetNode.NodeType == PathNodeTypes.Land).ElementAt(num2).TargetNode;
			WorkingStack workingStack = this.Game.CreateStack(this.Game.RebelRealm.ID, targetNode.ID, false);
			WorkingRealm realm = this.Game.GetRealm(Province.NaturalOwner);
			for (int i = 0; i < num; i++)
			{
				List<KeyValuePair<UnitData, UnitTrainStates>> list = realm.UnitPurchaseManager.GetAvailableUnitTypes();
				list = list.Where((KeyValuePair<UnitData, UnitTrainStates> x) => x.Key.Rank != UnitRanks.Elite && x.Key.Rank != UnitRanks.Unique && x.Key.Class != UnitClasses.Naval).ToList<KeyValuePair<UnitData, UnitTrainStates>>();
				if (list.Count<KeyValuePair<UnitData, UnitTrainStates>>() == 0)
				{
					break;
				}
				int num3 = 0;
				foreach (KeyValuePair<UnitData, UnitTrainStates> keyValuePair in list)
				{
					num3 += keyValuePair.Key.GetUnitWeight(WarMode.War);
				}
				if (num3 == 0)
				{
					break;
				}
				int num4 = this.RNG.Next(num3) + 1;
				int num5 = 0;
				UnitData unitData = null;
				foreach (KeyValuePair<UnitData, UnitTrainStates> keyValuePair2 in list)
				{
					num5 += keyValuePair2.Key.GetUnitWeight(WarMode.War);
					unitData = keyValuePair2.Key;
					if (num5 >= num4)
					{
						break;
					}
				}
				if (unitData == null)
				{
					break;
				}
				WorkingUnit workingUnit = this.Game.CreateUnit(this.Game.RebelRealm.ID, unitData);
				workingStack.AddUnit(workingUnit, false, false);
			}
			return workingStack;
		}

		[HandleProcessCorruptedStateExceptions]
		private void ContinueTurn()
		{
			try
			{
				this.TurnActive = true;
				this.PendingAction = new AIAction(AIActionTypes.Attack);
				this.PendingAction.Completed = true;
				while (this.PendingAction != null && !this.Disposed && !this.Game.PendingDispose)
				{
					Thread.Sleep(1);
				}
				if (this.Realm == this.Game.RebelRealm)
				{
					if (this.RevoltsEnabled)
					{
						this.DoAIAction(new Action(this.CheckForRevolts));
					}
				}
				else
				{
					this.DoAIAction(new Action(this.BattleManager.DoAttacks));
					this.IgnoreRebels--;
				}
				this.SetAction(new AIAction(AIActionTypes.Endturn));
				this.TurnActive = false;
			}
			catch (Exception ex)
			{
				ErrorDialog errorDialog = new ErrorDialog(ex.Message, ex.StackTrace, this.Game.GameCore);
				errorDialog.ShowDialog();
				this.Game.GameCore.Stop();
			}
		}

		[HandleProcessCorruptedStateExceptions]
		private void TakeTurn()
		{
			try
			{
				this.TurnActive = true;
				this.DebugMessage = this.Realm.Name + "\n";
				if (this.Realm.RealmIsDead)
				{
					this.SetAction(new AIAction(AIActionTypes.Endturn));
					this.TurnActive = false;
				}
				else if (this.Realm == this.Game.RebelRealm)
				{
					if (this.RevoltsEnabled)
					{
						this.DoAIAction(new Action(this.BeginRevoltChecks));
						this.DoAIAction(new Action(this.CheckForRevolts));
					}
					this.SetAction(new AIAction(AIActionTypes.Endturn));
					this.TurnActive = false;
				}
				else
				{
					this.DoAIAction(new Action(this.ResourceManager.UpdateResources));
					this.DoAIAction(new Action(this.TreatyManager.EndTreaties));
					this.DoAIAction(new Action(this.TreatyManager.OfferTreaties));
					this.DoAIAction(new Action(this.TreatyManager.MakePeaceOffers));
					this.IgnoreRebels--;
					this.SetAction(new AIAction(AIActionTypes.Endturn));
					this.TurnActive = false;
				}
			}
			catch (Exception ex)
			{
				ErrorDialog errorDialog = new ErrorDialog(ex.Message, ex.StackTrace, this.Game.GameCore);
				errorDialog.ShowDialog();
				this.Game.GameCore.Stop();
			}
		}

		private void DoAIAction(Action Func)
		{
			if (this.Game.PendingDispose)
			{
				return;
			}
			Func();
		}

		private void UpdateInvasionTargets()
		{
			foreach (int num in this.InvasionTargets.Keys.ToList<int>())
			{
				Dictionary<int, int> invasionTargets;
				int num2;
				(invasionTargets = this.InvasionTargets)[num2 = num] = invasionTargets[num2] - 1;
				if (this.InvasionTargets[num] == 0)
				{
					this.InvasionTargets.Remove(num);
				}
				else if (!this.Realm.Restrictions.CanDeclareWar(this.Game.AllRealms[num]))
				{
					this.InvasionTargets.Remove(num);
				}
			}
		}

		internal bool ShouldJoinWar(WorkingRealm Realm, WorkingRealm Target)
		{
			return this.Realm.Restrictions.CanDeclareWar(Target) && this.TreatyManager.GetWarValue(Target) >= this.TreatyManager.WarJoinThreshold && this.TreatyManager.VictoryIsPossible(Target);
		}

		internal void HandleHeroOffer(WorkingHero Hero, int Cost)
		{
			if (this.Realm.Gold.Value < Cost + 500)
			{
				return;
			}
			List<WorkingStack> list = this.Realm.Stacks.Where((WorkingStack x) => x.Hero == null).ToList<WorkingStack>();
			if (list.Count == 0)
			{
				return;
			}
			List<WorkingStack> list2 = list.Where((WorkingStack x) => x.Node != null && x.Node.Province != null).ToList<WorkingStack>();
			List<WorkingStack> list3 = list.Where((WorkingStack x) => x.Node != null && x.Node.Zone != null).ToList<WorkingStack>();
			if (list2.Count > 0)
			{
				WorkingStack workingStack = list2[this.RNG.Next(list2.Count)];
				this.Game.DeployHero(Hero, workingStack.Node);
				this.Realm.Gold.Value -= Cost;
				return;
			}
			if (list3.Count > 0)
			{
				WorkingStack workingStack2 = list3[this.RNG.Next(list2.Count)];
				this.Game.DeployHero(Hero, workingStack2.Node);
				this.Realm.Gold.Value -= Cost;
			}
		}

		internal void HandleFailedTrade(OngoingTrade Trade)
		{
			this.Realm.DiplomacyManager.TriggerEvent(Trade.Realm, "TradeFailed");
			if (this.RNG.Next(100) < 75)
			{
				switch (this.Realm.DiplomacyManager.GetRelation(Trade.Realm))
				{
				case RelationStates.Alliance:
				case RelationStates.Defence:
				case RelationStates.NAP:
					this.Game.AllianceController.BreakCurrentTreaty(this.Realm, Trade.Realm, true, false);
					break;
				default:
					return;
				}
			}
		}

		public void PlayerAcceptedTrade(TradeOffer CurrentTrade)
		{
			WorkingAgent freeAgent = this.Trade.GetFreeAgent();
			freeAgent.TradeOffer = CurrentTrade;
			freeAgent.Send(this.PendingAction.Realm, null, AgentModes.CarryTradeOffer);
			WorkingRealm realm = this.Realm;
			WorkingRealm workingRealm = this.Game.AllRealms[this.PendingAction.TradeOffer.TargetRealmID];
			if (this.PendingAction.TradeOffer.SenderOffers.Treaty != TreatyTypes.None)
			{
				this.Game.AllianceController.FormTreaty(realm, workingRealm, this.PendingAction.TradeOffer.SenderOffers.Treaty);
			}
			foreach (int num in this.PendingAction.TradeOffer.SenderOffers.GetProvinces())
			{
				WorkingProvince workingProvince = this.Game.AllProvinces[num];
				this.Game.ChangeProvinceOwner(workingProvince, workingRealm);
				if (workingProvince.LandNode.CurrentStack != null)
				{
					this.Game.WithdrawStack(workingProvince.LandNode.CurrentStack);
				}
				if (workingProvince.HarbourNode != null && workingProvince.HarbourNode.CurrentStack != null)
				{
					this.Game.WithdrawStack(workingProvince.HarbourNode.CurrentStack);
				}
				this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { workingProvince });
			}
			foreach (int num2 in this.PendingAction.TradeOffer.TargetOffers.GetProvinces())
			{
				WorkingProvince workingProvince2 = this.Game.AllProvinces[num2];
				this.Game.ChangeProvinceOwner(workingProvince2, realm);
				if (workingProvince2.LandNode.CurrentStack != null)
				{
					this.Game.WithdrawStack(workingProvince2.LandNode.CurrentStack);
				}
				if (workingProvince2.HarbourNode != null && workingProvince2.HarbourNode.CurrentStack != null)
				{
					this.Game.WithdrawStack(workingProvince2.HarbourNode.CurrentStack);
				}
				this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { workingProvince2 });
			}
			foreach (int num3 in this.PendingAction.TradeOffer.SenderOffers.GetPrisoners())
			{
				WorkingUnit workingUnit = this.Game.AllUnits[num3];
				realm.Prison.ReleasePrisoner(workingUnit);
			}
			foreach (int num4 in this.PendingAction.TradeOffer.TargetOffers.GetPrisoners())
			{
				WorkingUnit workingUnit2 = this.Game.AllUnits[num4];
				workingRealm.Prison.ReleasePrisoner(workingUnit2);
			}
			this.PendingAction = null;
		}

		internal void ChooseScience(WorkingProvince Prov)
		{
			Dictionary<ArtScienceTypes, int> dictionary = new Dictionary<ArtScienceTypes, int>();
			if (this.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Alchemy) < 2)
			{
				if (this.Realm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Alchemy) == 0)
				{
					dictionary.Add(ArtScienceTypes.Alchemy, this.Realm.Science_AlchemyValue);
				}
			}
			if (this.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Engineering) < 2)
			{
				if (this.Realm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Engineering) == 0)
				{
					dictionary.Add(ArtScienceTypes.Engineering, this.Realm.Science_EngineeringValue);
				}
			}
			if (this.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Metallurgy) < 2)
			{
				if (this.Realm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Metallurgy) == 0)
				{
					dictionary.Add(ArtScienceTypes.Metallurgy, this.Realm.Science_MetallurgyValue);
				}
			}
			if (this.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Siegecraft) < 2)
			{
				if (this.Realm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Siegecraft) == 0)
				{
					dictionary.Add(ArtScienceTypes.Siegecraft, this.Realm.Science_SiegecraftValue);
				}
			}
			int num = this.RNG.Next(dictionary.Sum((KeyValuePair<ArtScienceTypes, int> x) => x.Value));
			int num2 = 0;
			ArtScienceTypes artScienceTypes = ArtScienceTypes.None;
			foreach (KeyValuePair<ArtScienceTypes, int> keyValuePair in dictionary)
			{
				num2 += keyValuePair.Value;
				if (num < num2)
				{
					artScienceTypes = keyValuePair.Key;
					break;
				}
			}
			if (artScienceTypes != ArtScienceTypes.None)
			{
				Prov.Cradle = artScienceTypes;
				Prov.UpdateCradleSprite();
				if ((artScienceTypes == ArtScienceTypes.PublicArt && Prov.IsCapitol) || artScienceTypes == ArtScienceTypes.Statecraft)
				{
					foreach (WorkingRealm workingRealm in this.Game.AllRealms.Values)
					{
						if (workingRealm != this.Realm && workingRealm != this.Game.RebelRealm)
						{
							workingRealm.DiplomacyManager.AdjustBaseValue(this.Realm, 1f);
						}
					}
				}
				if (artScienceTypes == ArtScienceTypes.PublicArt && Prov.IsCapitol)
				{
					Prov.AILust -= 10;
				}
				this.Realm.CheckCradleEffects();
				this.Game.GameCore.FireEvent("CradlePlaced", new object[] { Prov });
			}
		}

		internal void ChooseArts(WorkingProvince Prov)
		{
			Dictionary<ArtScienceTypes, int> dictionary = new Dictionary<ArtScienceTypes, int>();
			if (this.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Medicine) < 3)
			{
				if (this.Realm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Medicine) == 0)
				{
					dictionary.Add(ArtScienceTypes.Medicine, this.Realm.Arts_MedicineValue);
				}
			}
			if (this.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.PublicArt) < 3)
			{
				if (this.Realm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.PublicArt) == 0)
				{
					dictionary.Add(ArtScienceTypes.PublicArt, this.Realm.Arts_PublicValue);
				}
			}
			if (this.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Statecraft) < 3)
			{
				if (this.Realm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Statecraft) == 0)
				{
					dictionary.Add(ArtScienceTypes.Statecraft, this.Realm.Arts_StatecraftValue);
				}
			}
			int num = this.RNG.Next(dictionary.Sum((KeyValuePair<ArtScienceTypes, int> x) => x.Value));
			int num2 = 0;
			ArtScienceTypes artScienceTypes = ArtScienceTypes.None;
			foreach (KeyValuePair<ArtScienceTypes, int> keyValuePair in dictionary)
			{
				num2 += keyValuePair.Value;
				if (num < num2)
				{
					artScienceTypes = keyValuePair.Key;
					break;
				}
			}
			if (artScienceTypes != ArtScienceTypes.None)
			{
				Prov.Cradle = artScienceTypes;
				Prov.UpdateCradleSprite();
				this.Game.GameCore.FireEvent("PatronPlaced", new object[] { Prov });
			}
		}

		public void AddIgnoreProvince(WorkingProvince Province)
		{
			this.IgnoreProvinces.Add(Province.ID);
		}

		public int GetLustModifier(string ProvinceName)
		{
			int num = 0;
			this.LustModifiers.TryGetValue(ProvinceName, out num);
			return num;
		}

		internal void ConsiderLiberation(WorkingProvince Province)
		{
			bool flag = false;
			if (this.Realm.CodeOfWar)
			{
				flag = true;
			}
			else if (this.RNG.Next(10) >= this.Game.Data.AITraits[this.Realm.Name].Opportunist)
			{
				flag = true;
			}
			if (flag)
			{
				this.Game.WithdrawStack(Province.LandNode.CurrentStack);
				this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { Province });
				Province.OwnerRealm.DiplomacyManager.TriggerEvent(this.Realm, "LiberatedAlly");
				if (Province.OwnerRealm.AIPlayer == null)
				{
					GameText gameText = GameText.CreateLocalised("MSG_AILIBERATE_TITLE", new object[0]);
					GameText gameText2 = GameText.CreateLocalised("MSG_AILIBERATE_TEXT", new object[0]);
					gameText2.AddChildText(GameText.CreateLocalised(this.Realm.DisplayName, new object[0]));
					gameText2.AddChildText(GameText.CreateLocalised(Province.DisplayName, new object[0]));
					this.Game.GameCore.MessageHandler.ShowInfoMessage(gameText, gameText2);
					return;
				}
			}
			else
			{
				Province.OwnerRealm.DiplomacyManager.TriggerEvent(this.Realm, "AnnexAlly");
				if (Province.OwnerRealm.AIPlayer == null)
				{
					GameText gameText3 = GameText.CreateLocalised("MSG_AISTEALTITLE", new object[0]);
					GameText gameText4 = GameText.CreateLocalised("MSG_AISTEAL_TEXT", new object[0]);
					gameText4.AddChildText(GameText.CreateLocalised(this.Realm.DisplayName, new object[0]));
					gameText4.AddChildText(GameText.CreateLocalised(Province.DisplayName, new object[0]));
					this.Game.GameCore.MessageHandler.ShowInfoMessage(gameText3, gameText4);
					return;
				}
				Province.OwnerRealm.AIPlayer.RelationsManager.RespondToFailedLiberate(this.Realm);
			}
		}

		public void RespondToFailedLiberate(WorkingRealm Realm)
		{
			if (Realm.DiplomacyManager.GetRelation(this.Realm) == RelationStates.Peace)
			{
				if (this.RNG.Next(100) > 50)
				{
					this.Game.AllianceController.EstablishWar(this.Realm, Realm);
					return;
				}
			}
			else if (this.RNG.Next(100) > 50)
			{
				this.Game.AllianceController.BreakCurrentTreaty(this.Realm, Realm, true, false);
			}
		}

		public AIAction PendingAction;

		public SovereigntyGame Game;

		public AIProvinceManager ProvinceManager;

		public AIPrisonManager PrisonManager;

		public AIBattleManager BattleManager;

		public AIMagicManager MagicManager;

		public AIResourceManager ResourceManager;

		public AITreatyManager TreatyManager;

		public AIUnitManager UnitManager;

		public AIUtilities Utility;

		public AITradeUtilities Trade;

		private int m_RealmID;

		private WorkingRealm m_Realm;

		public Random RNG;

		internal bool Disposed;

		public int ResourceStockLimit = 8;

		internal Dictionary<int, WarData> Wars;

		public Dictionary<int, int> InvasionTargets;

		private Dictionary<string, int> LustModifiers;

		public bool IgnoreCapitolLust;

		public List<int> IgnoreProvinces;

		public int IgnoreRebels;

		public bool TurnActive;

		private int RebelProvinceID;

		private WorkingStack RebelStack;

		private Stopwatch SW;

		private string DebugMessage;

		public bool RevoltsEnabled = true;
	}
}
