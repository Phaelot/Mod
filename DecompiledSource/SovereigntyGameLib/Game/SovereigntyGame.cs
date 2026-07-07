// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.Game.SovereigntyGame
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using OpenTK;
using SovereigntyTK;
using SovereigntyTK.AI.V2;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.Game.Campaign;
using SovereigntyTK.Game.Data;
using SovereigntyTK.Game.Trade;
using SovereigntyTK.UI;
using SovereigntyTK.UI.Map;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Game
{
	public class SovereigntyGame
	{
		private int NextUnitID = 1;

		private int NextStackID = 1;

		private int NextAgentID = 1;

		private int NextHeroID = 1;

		private int NextSpellID = 1;

		private int NextBuildingID = 1;

		public int NextTradeID = 1;

		public SovereigntyData Data;

		public Sovereignty GameCore;

		public CampaignBase CurrentCampaign;

		public Dictionary<int, WorkingRealm> AllRealms;

		public Dictionary<int, WorkingProvince> AllProvinces;

		public Dictionary<int, WorkingStack> AllStacks;

		public Dictionary<int, WorkingUnit> AllUnits;

		public Dictionary<int, WorkingAgent> AllAgents;

		public Dictionary<int, AIPlayer> AllAIPlayers;

		public Dictionary<int, WorkingZone> AllZones;

		public Dictionary<int, ActivePathNode> AllNodes;

		public Dictionary<int, WorkingHero> AllHeroes;

		public Dictionary<int, SpellEffect> AllSpells;

		public Dictionary<int, BuildingEffect> AllBuildings;

		public WorkingRealm RebelRealm;

		public WorkingRealm PlayerRealm;

		public AllianceController AllianceController;

		public TurnController TurnController;

		public ScoreManager ScoreKeeper;

		public EconomyController EconomyController;

		public PathManager PathManager;

		public DestinationChecker DestinationChecker;

		public PlayerMovementController PlayerMoveManager;

		public MarketPlaceData Marketplace;

		public PrisonerController PrisonerController;

		public SpellTargetData GlobalSpellEffects;

		internal BattleStarter PendingBattle;

		public AutoBattleController CurrentAutoBattle;

		private ActionMessageDataTracker ActionTracker;

		public TacticalBattleController CurrentTacticalBattle;

		private Dictionary<int, TradeOffer> PlayerPendingTrades;

		internal string GameID;

		private int CreatorThread;

		public bool Ironman;

		public bool WatchAllBattles;

		public bool AIAttacksDisabled;

		public string IronmanName;

		private Random RNG;

		private List<string> TradeAllowedRealms;

		internal List<AnimationSpriteData> SpellAnimations;

		public bool PendingDispose;

		public bool IgnoreHumanPlayer;

		public List<GameText> TickerTexts;

		private StreamWriter GoldLogFile;

		public SovereigntyStats Stats;

		public event ProvinceRealmDelegate OnProvinceOwnerChanged;

		public event DispositionChangeDelegate OnDispositionChanged;

		public event UnitNodeDelegate OnUnitNodeChanged;

		public event UnitPathDelegate OnUnitPathing;

		public event RealmDelegate OnPlayerTurnStarted;

		public event UnitDelegate OnUnitCreated;

		public SovereigntyGame(Sovereignty Game, CampaignBase Campaign, bool Ironman)
		{
			RNG = new Random();
			Data = Game.Data;
			GameCore = Game;
			CurrentCampaign = Campaign;
			this.Ironman = Ironman;
			AllRealms = new Dictionary<int, WorkingRealm>();
			AllProvinces = new Dictionary<int, WorkingProvince>();
			AllStacks = new Dictionary<int, WorkingStack>();
			AllUnits = new Dictionary<int, WorkingUnit>();
			AllAgents = new Dictionary<int, WorkingAgent>();
			AllAIPlayers = new Dictionary<int, AIPlayer>();
			AllZones = new Dictionary<int, WorkingZone>();
			AllNodes = new Dictionary<int, ActivePathNode>();
			AllHeroes = new Dictionary<int, WorkingHero>();
			AllSpells = new Dictionary<int, SpellEffect>();
			AllBuildings = new Dictionary<int, BuildingEffect>();
			PlayerPendingTrades = new Dictionary<int, TradeOffer>();
			AllianceController = new AllianceController(this);
			PathManager = new PathManager(this);
			DestinationChecker = new DestinationChecker(this);
			PlayerMoveManager = new PlayerMovementController(this);
			Marketplace = new MarketPlaceData(this);
			GlobalSpellEffects = new SpellTargetData(this);
			EconomyController = new EconomyController(this);
			PrisonerController = new PrisonerController(this);
			ActionTracker = new ActionMessageDataTracker(this);
			SpellAnimations = new List<AnimationSpriteData>();
			CreatorThread = Thread.CurrentThread.ManagedThreadId;
			TickerTexts = new List<GameText>();
			GameCore.RegisterEvent(HandleProvinceOccupierChanged, "ProvinceOccupierChanged");
			GameCore.RegisterEvent(HandleTickerMessage, "TickerMessage");
			Stats = new SovereigntyStats(this);
		}

		private void HandleTickerMessage(string EventName, params object[] Args)
		{
			TickerMessage tickerMessage = (TickerMessage)Args[0];
			if (TickerTexts.Count == 50)
			{
				TickerTexts.RemoveAt(0);
			}
			GameText gameText = GameText.CreateLocalised("FORMAT_TICKER");
			gameText.AddChildText(tickerMessage.MessageText);
			TickerTexts.Add(gameText);
		}

		private void HandleProvinceOccupierChanged(string EventName, params object[] Args)
		{
			WorkingProvince workingProvince = Args[0] as WorkingProvince;
			if (!workingProvince.Occupied)
			{
				workingProvince.EndResistance();
				return;
			}
			foreach (int item in workingProvince.LandNode.AllyStacks.ToList())
			{
				WorkingStack value = null;
				AllStacks.TryGetValue(item, out value);
				if (value != null)
				{
					WithdrawStack(value);
				}
				else
				{
					workingProvince.LandNode.AllyStacks.Remove(item);
				}
			}
			if (workingProvince.HarbourNode != null)
			{
				foreach (int item2 in workingProvince.HarbourNode.AllyStacks.ToList())
				{
					WorkingStack value2 = null;
					AllStacks.TryGetValue(item2, out value2);
					if (value2 != null)
					{
						WithdrawStack(value2);
					}
					else
					{
						workingProvince.HarbourNode.AllyStacks.Remove(item2);
					}
				}
			}
			workingProvince.StartResistance();
		}

		public void Init(string PlayerRealmName)
		{
			GameID = Guid.NewGuid().ToString();
			CreateRealms(PlayerRealmName);
			CreateAIPlayers();
			CreateProvinces();
			CreatePathingMap();
			CreateReachableMap();
			foreach (WorkingProvince value in AllProvinces.Values)
			{
				value.UpdateEconSprite();
			}
			ScoreKeeper = new ScoreManager(this);
			foreach (WorkingRealm value2 in AllRealms.Values)
			{
				if (value2.AIPlayer != null)
				{
					value2.AIPlayer.UnitsManager.Funds.CurrentGold = value2.Gold;
					value2.Gold.Value = 0;
				}
				value2.ProvincesChanged();
			}
			InitialUnitPurchaser initialUnitPurchaser = new InitialUnitPurchaser(this);
			initialUnitPurchaser.PurchaseInitialUnits();
			foreach (WorkingRealm value3 in AllRealms.Values)
			{
				value3.UnitsChanged();
			}
			TurnController = new TurnController(this);
			TurnController.StartFirstTurn();
			GameCore.FireEvent("GameStarted");
			GameCore.FireEvent("NewGameStarted", this);
		}

		public void Load(BinaryReader r, int SaveVersion)
		{
			GameID = r.ReadString();
			NextAgentID = r.ReadInt32();
			NextHeroID = r.ReadInt32();
			NextSpellID = r.ReadInt32();
			NextStackID = r.ReadInt32();
			NextUnitID = r.ReadInt32();
			if (SaveVersion >= GlobalData.SAVEVERSION_EA3)
			{
				NextBuildingID = r.ReadInt32();
			}
			if (SaveVersion >= 47)
			{
				NextTradeID = r.ReadInt32();
			}
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				WorkingRealm workingRealm = new WorkingRealm(this, r, SaveVersion);
				AllRealms.Add(workingRealm.ID, workingRealm);
			}
			num = r.ReadInt32();
			for (int j = 0; j < num; j++)
			{
				WorkingProvince workingProvince = new WorkingProvince(this, r, SaveVersion);
				AddProvince(workingProvince);
				if (workingProvince.BattleField != null && SaveVersion >= 27)
				{
					workingProvince.BattleField.GenerateTooltip();
				}
			}
			num = r.ReadInt32();
			for (int k = 0; k < num; k++)
			{
				WorkingStack workingStack = new WorkingStack(this, r, SaveVersion);
				AllStacks.Add(workingStack.ID, workingStack);
			}
			num = r.ReadInt32();
			for (int l = 0; l < num; l++)
			{
				WorkingUnit workingUnit = new WorkingUnit(this, r, SaveVersion);
				if (workingUnit.BaseType == null)
				{
					workingUnit.Dispose();
					continue;
				}
				AllUnits.Add(workingUnit.ID, workingUnit);
				workingUnit.OnStackChanged += Unit_OnStackChanged;
			}
			num = r.ReadInt32();
			for (int m = 0; m < num; m++)
			{
				WorkingAgent workingAgent = new WorkingAgent(this, r, SaveVersion);
				AllAgents.Add(workingAgent.ID, workingAgent);
			}
			num = r.ReadInt32();
			for (int n = 0; n < num; n++)
			{
				int num2 = 0;
				num2 = r.ReadInt32();
				AIPlayer aIPlayer = new AIPlayer(this, AllRealms[num2]);
				aIPlayer.Load(r, SaveVersion);
				AllAIPlayers.Add(aIPlayer.Realm.ID, aIPlayer);
			}
			num = r.ReadInt32();
			for (int num3 = 0; num3 < num; num3++)
			{
				WorkingZone workingZone = new WorkingZone(this, r, SaveVersion);
				AllZones.Add(workingZone.ID, workingZone);
			}
			num = r.ReadInt32();
			for (int num4 = 0; num4 < num; num4++)
			{
				ActivePathNode activePathNode = new ActivePathNode(this, r, SaveVersion);
				AllNodes.Add(activePathNode.ID, activePathNode);
			}
			num = r.ReadInt32();
			for (int num5 = 0; num5 < num; num5++)
			{
				WorkingHero workingHero = new WorkingHero(this, r, SaveVersion);
				if (SaveVersion > 51 || workingHero.OwnerStack != null)
				{
					AllHeroes.Add(workingHero.ID, workingHero);
				}
			}
			CreateReachableMap();
			num = r.ReadInt32();
			for (int num6 = 0; num6 < num; num6++)
			{
				string text = r.ReadString();
				RealmMagicData value = null;
				Data.Spells.TryGetValue(text, out value);
				if (value == null)
				{
					throw new Exception("Failed to load file, spell " + text + " does not exist");
				}
				SpellEffect spellEffect = SpellEffect.LoadEffect(this, value, r, SaveVersion);
				if (spellEffect.LoadOK)
				{
					AllSpells.Add(spellEffect.ID, spellEffect);
				}
			}
			if (SaveVersion >= GlobalData.SAVEVERSION_EA3)
			{
				num = r.ReadInt32();
				for (int num7 = 0; num7 < num; num7++)
				{
					string text2 = r.ReadString();
					BuildingData value2 = null;
					Data.Buildings.TryGetValue(text2, out value2);
					if (value2 == null)
					{
						throw new Exception("Failed to load file, building " + text2 + " does not exist");
					}
					BuildingEffect.LoadEffect(this, value2, r, SaveVersion);
				}
			}
			foreach (WorkingUnit value3 in AllUnits.Values)
			{
				if ((int)value3.Upkeep == 0 && value3.CarriedUnit != null)
				{
					value3.Upkeep = value3.CarriedUnit.Upkeep;
				}
			}
			PlayerRealm = AllRealms[r.ReadInt32()];
			RebelRealm = AllRealms[r.ReadInt32()];
			Marketplace.Load(r, SaveVersion);
			ScoreKeeper = new ScoreManager(this);
			ScoreKeeper.Load(r, SaveVersion);
			TurnController = new TurnController(this);
			TurnController.Load(r, SaveVersion);
			if (SaveVersion >= 46)
			{
				num = r.ReadInt32();
				TickerTexts = new List<GameText>();
				for (int num8 = 0; num8 < num; num8++)
				{
					GameText item = GameText.CreateFromLiteral(r.ReadString());
					TickerTexts.Add(item);
				}
			}
			PlayerPendingTrades = new Dictionary<int, TradeOffer>();
			num = r.ReadInt32();
			for (int num9 = 0; num9 < num; num9++)
			{
				TradeOffer tradeOffer = new TradeOffer(this, 0, 0);
				tradeOffer.Load(r, SaveVersion);
				PlayerPendingTrades.Add(tradeOffer.TargetRealmID, tradeOffer);
			}
			if (r.ReadBoolean())
			{
				CurrentTacticalBattle = new TacticalBattleController(this, r, SaveVersion);
			}
			foreach (WorkingProvince value4 in AllProvinces.Values)
			{
				value4.UpdateEconSprite();
			}
			foreach (WorkingUnit value5 in AllUnits.Values)
			{
				if (value5.BattleData == null)
				{
					value5.RemoveNamedFlags("Shatter");
				}
			}
			foreach (WorkingRealm value6 in AllRealms.Values)
			{
				GameCore.Map.UpdateRealmNameText(value6);
			}
			GameCore.FireEvent("NewGameStarted", this);
		}

		public void CheckWarStatus()
		{
			string text = "";
			foreach (WorkingRealm value in AllRealms.Values)
			{
				text = text + value.ID + "\t";
				if (value.AIPlayer == null)
				{
					text += " Human\r\n";
					continue;
				}
				foreach (WarData value2 in value.AIPlayer.WarManager.Wars.Values)
				{
					text = text + value2.EnemyID + " ";
				}
				text += "\r\n";
			}
			Stream stream = File.Create("d:\\sovwars.txt");
			StreamWriter streamWriter = new StreamWriter(stream);
			streamWriter.Write(text);
			streamWriter.Close();
		}

		public void PostInit()
		{
			GameCore.Map.SetAcivePlayer(TurnController.CurrentRealm);
			foreach (WorkingStack value in AllStacks.Values)
			{
				value.UpdateSprite();
			}
			GameCore.FireEvent("SpellsUpdated");
			List<WorkingUnit> list = new List<WorkingUnit>();
			foreach (WorkingUnit value2 in AllUnits.Values)
			{
				if (value2.OwnerStack != null && value2.BattleData == null && value2.OwnerStack.Node != null && value2.OwnerStack.Node.NodeType == PathNodeTypes.Land && value2.Class == UnitClasses.Naval)
				{
					list.Add(value2);
				}
			}
			foreach (WorkingUnit item in list)
			{
				DestroyUnit(item);
			}
			if (CurrentTacticalBattle != null)
			{
				GameCore.FireEvent("FullBattleStarted", CurrentTacticalBattle);
				if (CurrentTacticalBattle.Map.CurrentMode != BattleMapModes.Deploy)
				{
					GameCore.FireEvent("BattleDeployFinished");
					GameCore.FireEvent("BattleTurnChanged");
				}
			}
			if (TurnController.CurrentRealm.AIPlayer != null)
			{
				TurnController.CurrentRealm.AIPlayer.ResumeTurn();
			}
			else
			{
				ActionTracker.CheckPromotions();
			}
		}

		public void AddSpellAnimation(AnimationData Animation, Vector2 Position, Vector2 Size)
		{
			AnimationSpriteData item = new AnimationSpriteData(this, Animation, Position, Size, "", BattleSprite: false);
			SpellAnimations.Add(item);
		}

		public List<OngoingTrade> GetOngoingTrades(WorkingRealm Realm)
		{
			List<OngoingTrade> list = new List<OngoingTrade>();
			foreach (OngoingTrade trade in Realm.TradeManager.GetTrades())
			{
				list.Add(trade);
			}
			foreach (WorkingRealm value in AllRealms.Values)
			{
				if (value == Realm)
				{
					continue;
				}
				foreach (OngoingTrade trade2 in value.TradeManager.GetTrades())
				{
					if (trade2.TargetRealm == Realm)
					{
						list.Add(trade2);
					}
				}
			}
			return list;
		}

		public List<Tuple<WorkingRealm, TradeOfferList, int>> GetPendingShipments(WorkingRealm Realm)
		{
			List<Tuple<WorkingRealm, TradeOfferList, int>> list = new List<Tuple<WorkingRealm, TradeOfferList, int>>();
			foreach (WorkingAgent agent in Realm.Agents)
			{
				if (agent.TradeOffer != null && !agent.TradeOffer.TargetOffers.IsEmpty() && !agent.TradeOffer.TargetOffers.PerTurnOnly() && !agent.TradeOffer.TreatyOnly())
				{
					list.Add(new Tuple<WorkingRealm, TradeOfferList, int>(AllRealms[agent.TradeOffer.TargetRealmID], agent.TradeOffer.TargetOffers, agent.GetTurnsToHome()));
				}
			}
			foreach (WorkingRealm value in AllRealms.Values)
			{
				if (value == Realm)
				{
					continue;
				}
				foreach (WorkingAgent agent2 in value.Agents)
				{
					if (agent2.CurrentMode == AgentModes.CarryTradeOffer && !agent2.TradeOffer.SenderOffers.IsEmpty() && !agent2.TradeOffer.SenderOffers.PerTurnOnly() && agent2.TradeOffer.TargetRealmID == PlayerRealm.ID && !agent2.TradeOffer.TreatyOnly())
					{
						list.Add(new Tuple<WorkingRealm, TradeOfferList, int>(AllRealms[agent2.TradeOffer.SenderRealmID], agent2.TradeOffer.SenderOffers, agent2.TurnsLeft));
					}
				}
			}
			return list;
		}

		public void Autosave()
		{
		}

		public void Save(BinaryWriter w)
		{
			w.Write(GameID);
			w.Write(NextAgentID);
			w.Write(NextHeroID);
			w.Write(NextSpellID);
			w.Write(NextStackID);
			w.Write(NextUnitID);
			w.Write(NextBuildingID);
			w.Write(NextTradeID);
			w.Write(AllRealms.Count);
			foreach (WorkingRealm value in AllRealms.Values)
			{
				value.Save(w);
			}
			w.Write(AllProvinces.Count);
			foreach (WorkingProvince value2 in AllProvinces.Values)
			{
				value2.Save(w);
			}
			w.Write(AllStacks.Count);
			foreach (WorkingStack value3 in AllStacks.Values)
			{
				value3.Save(w);
			}
			w.Write(AllUnits.Count);
			foreach (WorkingUnit value4 in AllUnits.Values)
			{
				value4.Save(w);
			}
			w.Write(AllAgents.Count);
			foreach (WorkingAgent value5 in AllAgents.Values)
			{
				value5.Save(w);
			}
			w.Write(AllAIPlayers.Count);
			foreach (AIPlayer value6 in AllAIPlayers.Values)
			{
				w.Write(value6.Realm.ID);
				value6.Save(w);
			}
			w.Write(AllZones.Count);
			foreach (WorkingZone value7 in AllZones.Values)
			{
				value7.Save(w);
			}
			w.Write(AllNodes.Count);
			foreach (ActivePathNode value8 in AllNodes.Values)
			{
				value8.Save(w);
			}
			w.Write(AllHeroes.Count);
			foreach (WorkingHero value9 in AllHeroes.Values)
			{
				value9.Save(w);
			}
			w.Write(AllSpells.Count);
			foreach (SpellEffect value10 in AllSpells.Values)
			{
				w.Write(value10.SpellName);
				value10.Save(w);
			}
			w.Write(AllBuildings.Count);
			foreach (BuildingEffect value11 in AllBuildings.Values)
			{
				w.Write(value11.BuildingName);
				value11.Save(w);
			}
			w.Write(PlayerRealm.ID);
			w.Write(RebelRealm.ID);
			Marketplace.Save(w);
			ScoreKeeper.Save(w);
			TurnController.Save(w);
			w.Write(TickerTexts.Count);
			foreach (GameText tickerText in TickerTexts)
			{
				w.Write(tickerText.GetActualText(GameCore));
			}
			w.Write(PlayerPendingTrades.Count);
			foreach (TradeOffer value12 in PlayerPendingTrades.Values)
			{
				value12.Save(w);
			}
			if (CurrentTacticalBattle == null || CurrentTacticalBattle.BattleEnded)
			{
				w.Write(value: false);
				return;
			}
			w.Write(value: true);
			CurrentTacticalBattle.Save(w);
		}

		public void DestroyAgent(int AgentID)
		{
			WorkingAgent workingAgent = AllAgents[AgentID];
			workingAgent.Dispose();
			AllAgents.Remove(AgentID);
		}

		public WorkingAgent CreateAgent(int RealmID)
		{
			WorkingAgent workingAgent = new WorkingAgent(NextAgentID++, this, RealmID);
			AllAgents.Add(workingAgent.ID, workingAgent);
			return workingAgent;
		}

		private void CreatePathingMap()
		{
			int num = 1;
			Dictionary<ActivePathNode, PathingNodeData> dictionary = new Dictionary<ActivePathNode, PathingNodeData>();
			foreach (PathingNodeData node in Data.Nodes)
			{
				ActivePathNode activePathNode = new ActivePathNode(this, num, node);
				AllNodes.Add(activePathNode.ID, activePathNode);
				dictionary.Add(activePathNode, node);
				num++;
			}
			foreach (KeyValuePair<ActivePathNode, PathingNodeData> item in dictionary)
			{
				item.Key.CreateConnections(item.Value);
			}
			dictionary.Clear();
		}

		private void CreateReachableMap()
		{
			foreach (ActivePathNode value in AllNodes.Values)
			{
				List<int> list = new List<int>();
				list.Add(value.ID);
				switch (value.NodeType)
				{
					case PathNodeTypes.Harbour:
					case PathNodeTypes.RiverHarbour:
						{
							WorkingProvince province = value.Province;
							list.Add(province.LandNode.ID);
							foreach (string adjacentZone in province.AdjacentZones)
							{
								WorkingZone zone2 = GetZone(adjacentZone);
								foreach (ActivePathNode node in zone2.Nodes)
								{
									list.Add(node.ID);
								}
							}
							break;
						}
					case PathNodeTypes.Land:
						{
							WorkingProvince province2 = value.Province;
							if (province2.HarbourNode != null)
							{
								list.Add(province2.HarbourNode.ID);
							}
							foreach (string adjacentZone2 in province2.AdjacentZones)
							{
								WorkingZone zone3 = GetZone(adjacentZone2);
								foreach (ActivePathNode node2 in zone3.Nodes)
								{
									list.Add(node2.ID);
								}
							}
							List<int> list2 = new List<int>();
							list2.Add(value.ID);
							for (int i = 0; i < 3; i++)
							{
								List<int> list3 = new List<int>(list2);
								foreach (int item in list3)
								{
									ActivePathNode activePathNode = AllNodes[item];
									foreach (ActiveNodeConnection connectedNode in activePathNode.ConnectedNodes)
									{
										if (connectedNode.TargetNode.NodeType == PathNodeTypes.Land && !list2.Contains(connectedNode.TargetNodeID))
										{
											list2.Add(connectedNode.TargetNodeID);
										}
									}
								}
							}
							list.AddRange(list2);
							break;
						}
					case PathNodeTypes.Sea:
						{
							WorkingZone zone = value.Zone;
							foreach (ActivePathNode node3 in zone.Nodes)
							{
								list.Add(node3.ID);
							}
							foreach (GameRegion allConnectedRegion in zone.GetAllConnectedRegions())
							{
								if (allConnectedRegion is WorkingProvince)
								{
									WorkingProvince workingProvince = allConnectedRegion as WorkingProvince;
									list.Add(workingProvince.LandNodeID);
									if (workingProvince.HarbourNode != null)
									{
										list.Add(workingProvince.HarbourNodeID);
									}
								}
								if (!(allConnectedRegion is WorkingZone))
								{
									continue;
								}
								foreach (ActivePathNode node4 in (allConnectedRegion as WorkingZone).Nodes)
								{
									list.Add(node4.ID);
								}
							}
							break;
						}
				}
				value.ReachableNodes = list;
			}
		}

		private void CreateProvinces()
		{
			int num = 1;
			foreach (ProvinceData value in Data.ActiveProvinces.Values)
			{
				int iD = GetRealm(value.Owner).ID;
				WorkingProvince province = new WorkingProvince(num, iD, this, value);
				AddProvince(province);
				num++;
			}
			int num2 = 1;
			foreach (SeaZoneData value2 in Data.ActiveSeaZones.Values)
			{
				WorkingZone workingZone = new WorkingZone(num2, this, value2);
				AllZones.Add(workingZone.ID, workingZone);
				num2++;
			}
			foreach (WorkingProvince value3 in AllProvinces.Values)
			{
				value3.CreateForts();
			}
		}

		private void CreateAIPlayers()
		{
			foreach (WorkingRealm value2 in AllRealms.Values)
			{
				if (value2 != PlayerRealm)
				{
					AIPlayer value = new AIPlayer(this, value2);
					AllAIPlayers.Add(value2.ID, value);
				}
			}
		}

		private void CreateRealms(string PlayerRealmName)
		{
			int num = 1;
			foreach (RealmData value in Data.ActiveRealms.Values)
			{
				WorkingRealm realm = new WorkingRealm(this, num, value);
				AddRealm(realm);
				num++;
			}
			PlayerRealm = GetRealm(PlayerRealmName);
			RebelRealm = AllRealms.Values.FirstOrDefault((WorkingRealm x) => x.Name == "Rebels");
			if (RebelRealm == null)
			{
				throw new Exception("No rebel realm defined");
			}
			foreach (WorkingRealm value2 in AllRealms.Values)
			{
				value2.DiplomacyManager.PopulateRealms();
			}
		}

		public void Dispose()
		{
			if (GoldLogFile != null)
			{
				GoldLogFile.Dispose();
			}
			Stats.Dispose();
			GameCore.FireEvent("GameEnded", this);
			GameCore.FireEvent("ClearHighlights");
			GameCore.UnregisterEvent(HandleProvinceOccupierChanged, "ProvinceOccupierChanged");
			if (AllAIPlayers.Values.Count((AIPlayer x) => x.TurnActive) > 0)
			{
				PendingDispose = true;
				return;
			}
			PendingDispose = false;
			if (GameCore.CurrentBattleMap != null)
			{
				GameCore.CurrentBattleMap.EndBattle();
				GameCore.CurrentBattleMap.Dispose();
				GameCore.CurrentBattleMap = null;
			}
			CurrentCampaign.Dispose();
			ActionTracker.Dispose();
			foreach (WorkingRealm item in AllRealms.Values.ToList())
			{
				item.MagicData.Dispose();
			}
			foreach (BuildingEffect item2 in AllBuildings.Values.ToList())
			{
				item2.Demolish();
			}
			AllBuildings.Clear();
			foreach (WorkingUnit item3 in AllUnits.Values.ToList())
			{
				DestroyUnit(item3);
				item3.Dispose();
			}
			AllUnits.Clear();
			foreach (WorkingHero item4 in AllHeroes.Values.ToList())
			{
				item4.Dispose();
			}
			AllHeroes.Clear();
			foreach (WorkingProvince item5 in AllProvinces.Values.ToList())
			{
				item5.Dispose();
				RemoveProvince(item5);
			}
			AllProvinces.Clear();
			foreach (WorkingRealm item6 in AllRealms.Values.ToList())
			{
				item6.Dispose();
			}
			AllRealms.Clear();
			foreach (WorkingStack item7 in AllStacks.Values.ToList())
			{
				DestroyStack(item7);
				item7.Dispose();
			}
			AllStacks.Clear();
		}

		private void RemoveProvince(WorkingProvince Province)
		{
			Province.OnOwnerChanged -= Province_OnOwnerChanged;
			AllProvinces.Remove(Province.ID);
		}

		private void RemoveRealm(WorkingRealm Realm)
		{
			AllRealms.Remove(Realm.ID);
		}

		public void AddRealm(WorkingRealm Realm)
		{
			AllRealms.Add(Realm.ID, Realm);
		}

		public void AddProvince(WorkingProvince Province)
		{
			AllProvinces.Add(Province.ID, Province);
			Province.OnOwnerChanged += Province_OnOwnerChanged;
		}

		private void Province_OnOwnerChanged(WorkingProvince Province, WorkingRealm OldOwner, WorkingRealm Realm)
		{
			foreach (int item in Province.LandNode.AllyStacks.ToList())
			{
				WorkingStack workingStack = AllStacks[item];
				if (workingStack.Owner.DiplomacyManager.GetRelation(Realm) != RelationStates.Alliance)
				{
					WithdrawStack(workingStack);
				}
			}
			if (Province.HarbourNode != null)
			{
				foreach (int item2 in Province.HarbourNode.AllyStacks.ToList())
				{
					WorkingStack workingStack2 = AllStacks[item2];
					if (workingStack2.Owner.DiplomacyManager.GetRelation(Realm) != RelationStates.Alliance)
					{
						WithdrawStack(workingStack2);
					}
				}
			}
			GameCore.FireEvent("BuildingsChanged", Province);
			GameCore.FireEvent("ProvinceOwnerChanged", Province, Realm, OldOwner);
			if (this.OnProvinceOwnerChanged != null)
			{
				this.OnProvinceOwnerChanged(Province, OldOwner, Realm);
			}
			GameCore.Map.UpdateRealmNameText(OldOwner);
			GameCore.Map.UpdateRealmNameText(Realm);
			if (Province.Occupied)
			{
				Province.StartResistance();
			}
			else
			{
				Province.EndResistance();
			}
		}

		public int AddSpell(SpellEffect Spell)
		{
			int num = NextSpellID++;
			AllSpells.Add(num, Spell);
			return num;
		}

		public void RemoveSpell(SpellEffect Spell)
		{
			AllSpells.Remove(Spell.ID);
		}

		internal void HandleGameVictory()
		{
			MessageBoxData messageBoxData = new MessageBoxData();
			messageBoxData.CaptionText = GameText.CreateLocalised("MSG_VICTORY_TITLE");
			messageBoxData.MessageText = GameText.CreateLocalised("MSG_VICTORY_TEXT");
			messageBoxData.MsgType = MessageType.GameEnd;
			messageBoxData.DisplayType = MessageBoxType.Info;
			GameCore.MessageHandler.ShowMessage(messageBoxData);
		}

		internal void HandleGameDefeat()
		{
			MessageBoxData messageBoxData = new MessageBoxData();
			messageBoxData.CaptionText = GameText.CreateLocalised("MSG_DEFEAT_TITLE");
			messageBoxData.MessageText = GameText.CreateLocalised("MSG_DEFEAT_TEXT");
			messageBoxData.MsgType = MessageType.GameEnd;
			messageBoxData.DisplayType = MessageBoxType.Info;
			GameCore.MessageHandler.ShowMessage(messageBoxData);
		}

		public WorkingRealm GetRealm(string RealmName)
		{
			return AllRealms.Values.FirstOrDefault((WorkingRealm x) => x.Name == RealmName);
		}

		public WorkingProvince GetProvince(string ProvinceName)
		{
			return AllProvinces.Values.FirstOrDefault((WorkingProvince x) => x.Name == ProvinceName);
		}

		internal void HandleDispositionChanged(string Realm, string TargetRealm, float OldValue, float NewValue)
		{
			if (Realm == "Valegorn Palatinate")
			{
				_ = TargetRealm == "Galeni";
			}
			GameCore.FireEvent("DispositionChanged", Realm, TargetRealm, OldValue, NewValue);
		}

		public bool ContiguousBorderExists(string MainRealmName, params string[] BorderRealms)
		{
			WorkingRealm realm = GetRealm(MainRealmName);
			if (realm.CapitolProvince == null)
			{
				return false;
			}
			List<WorkingProvince> capitolRegion = GetCapitolRegion(realm);
			List<List<WorkingProvince>> list = new List<List<WorkingProvince>>();
			foreach (string realmName in BorderRealms)
			{
				WorkingRealm realm2 = GetRealm(realmName);
				list.Add(GetCapitolRegion(realm2));
			}
			foreach (List<WorkingProvince> item in list)
			{
				bool flag = false;
				if (item == null)
				{
					return false;
				}
				foreach (WorkingProvince item2 in capitolRegion)
				{
					List<GameRegion> Regions = item2.GetAllConnectedRegions();
					if (item.Count((WorkingProvince x) => Regions.Contains(x)) > 0)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return false;
				}
			}
			return true;
		}

		public List<WorkingProvince> GetCapitolRegion(WorkingRealm Realm)
		{
			List<WorkingProvince> list = new List<WorkingProvince>();
			List<WorkingProvince> list2 = Realm.Provinces.ToList();
			WorkingProvince capitolProvince = Realm.CapitolProvince;
			if (capitolProvince == null)
			{
				return null;
			}
			list.Add(capitolProvince);
			list2.Remove(capitolProvince);
			bool flag = true;
			while (flag)
			{
				flag = false;
				foreach (WorkingProvince item2 in list2.ToList())
				{
					foreach (GameRegion allConnectedRegion in item2.GetAllConnectedRegions())
					{
						if (allConnectedRegion is WorkingProvince item && !list.Contains(item) && list2.Contains(item))
						{
							list2.Remove(item);
							list.Add(item);
							flag = true;
						}
					}
				}
			}
			return list;
		}

		internal void CleanupWar(WorkingRealm Realm1, WorkingRealm Realm2)
		{
			WitchdrawFromRealm(Realm1, Realm2);
			WitchdrawFromRealm(Realm2, Realm1);
		}

		private void ReleasePrisoners(WorkingRealm Realm1, WorkingRealm Realm2)
		{
			foreach (WorkingUnit realmPrisoner in Realm1.Prison.GetRealmPrisoners(Realm2))
			{
				Realm2.QueueUnit(realmPrisoner, Instant: true);
				Realm1.Prison.ReleasePrisoner(realmPrisoner);
			}
		}

		private void WitchdrawFromRealm(WorkingRealm Realm1, WorkingRealm Realm2)
		{
			List<WorkingStack> list = new List<WorkingStack>();
			foreach (WorkingStack stack in Realm1.Stacks)
			{
				if (stack.Node.Province != null && stack.Node.Province.OwnerRealm == Realm2)
				{
					list.Add(stack);
				}
			}
			foreach (WorkingStack item in list)
			{
				WorkingProvince province = item.Node.Province;
				WithdrawStack(item);
				GameCore.FireEvent("ProvinceOccupierChanged", province);
			}
		}

		public void WithdrawStack(WorkingStack Stack)
		{
			RetreatManager retreatManager = new RetreatManager(this, Stack);
			retreatManager.Retreat(null);
			foreach (WorkingUnit unit in Stack.Units)
			{
				unit.OwnerRealm.QueueUnit(unit, Instant: true);
				Stack.RemoveUnit(unit);
			}
			if (Stack.Hero != null)
			{
				Stack.Owner.StoreHero(Stack.Hero);
				Stack.RemoveHero();
			}
			DestroyStack(Stack);
		}

		internal void CleanupAlliance(WorkingRealm Realm1, WorkingRealm Realm2)
		{
			WitchdrawFromRealm(Realm1, Realm2);
			WitchdrawFromRealm(Realm2, Realm1);
		}

		public void CreateTacticalBattle()
		{
			if (CurrentAutoBattle != null)
			{
				CurrentTacticalBattle = new TacticalBattleController(this, CurrentAutoBattle);
				CurrentAutoBattle.Dispose();
				CurrentAutoBattle = null;
			}
		}

		internal void Update(float ElapsedTime)
		{
			if (PendingDispose && AllAIPlayers.Values.Count((AIPlayer x) => x.TurnActive) == 0)
			{
				Dispose();
				return;
			}
			foreach (AIPlayer value in AllAIPlayers.Values)
			{
				value.ActionManager.CheckForActions();
			}
			foreach (WorkingStack value2 in AllStacks.Values)
			{
				value2.Update(ElapsedTime);
			}
			if (PendingBattle != null && PendingBattle.ReadyToStart())
			{
				GameCore.FireEvent("AutoBattleReady", PendingBattle);
				CurrentAutoBattle = new AutoBattleController(this, PendingBattle);
				PendingBattle = null;
				CurrentAutoBattle.Init();
			}
			if (CurrentAutoBattle != null)
			{
				CurrentAutoBattle.Update(ElapsedTime);
			}
			if (CurrentTacticalBattle != null)
			{
				CurrentTacticalBattle.Update(ElapsedTime);
			}
			foreach (AnimationSpriteData item in SpellAnimations.ToList())
			{
				item.Update(ElapsedTime);
				if (item.Complete)
				{
					item.Dispose();
					SpellAnimations.Remove(item);
				}
			}
		}

		internal void StartPlayerTurn(WorkingRealm Realm)
		{
			if (Realm.RealmIsDead)
			{
				return;
			}
			if (Realm == PlayerRealm)
			{
				GameCore.SaveManager.Autosave(TurnStart: true);
			}
			EconomyController.DoTurnStart(Realm);
			Realm.DiplomacyManager.AgeRelations();
			Realm.DiplomacyManager.UpdateDispositions();
			Realm.ResetUnitMoves();
			Realm.UpdateUnitQueue();
			Realm.UpdateAgents();
			CheckForSpies(Realm);
			Realm.MagicData.UpdateActiveSpells();
			Realm.UpdatePlague();
			Realm.TradeManager.UpdateTrades();
			Realm.MagicData.UpdateInvestment();
			EconomyController.CheckHeroRecruitment(Realm);
			foreach (WorkingStack stack in Realm.Stacks)
			{
				if (stack.Units.Count == 0)
				{
					DestroyStack(stack);
				}
			}
			if (this.OnPlayerTurnStarted != null)
			{
				this.OnPlayerTurnStarted(Realm);
			}
			if (Realm == PlayerRealm)
			{
				ActionTracker.ShowMessages();
				ActionTracker.Reset();
				string text = "";
				switch (PlayerRealm.Panel)
				{
					case SpellSchools.Death:
						text = "outofluck";
						break;
					case SpellSchools.Illusion:
						text = "stinkyalert";
						break;
					case SpellSchools.Nature:
						text = "chime04";
						break;
					case SpellSchools.War:
						text = "success04";
						break;
				}
				if (TurnController.TurnNumber > 0)
				{
					GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\" + text + ".wav");
				}
			}
			GameCore.FireEvent("TurnStarted", Realm);
		}

		private void CheckForSpies(WorkingRealm Realm)
		{
			int num = Realm.Agents.Count((WorkingAgent x) => x.CurrentMode == AgentModes.Idle && x.TurnsLeft == 0);
			foreach (WorkingAgent item in AllAgents.Values.Where((WorkingAgent x) => x.CurrentMode != AgentModes.CarryTradeOffer && x.CurrentMode != AgentModes.ImproveRelations && x.HostRealm == Realm && x.OwnerRealm != Realm))
			{
				if (RNG.Next(100) < num)
				{
					if (Realm.AIPlayer == null)
					{
						MessageBoxData messageBoxData = new MessageBoxData();
						messageBoxData.DisplayType = MessageBoxType.YesNo;
						messageBoxData.MsgType = MessageType.SpyCaught;
						messageBoxData.CaptionText = GameText.CreateLocalised("MSG_SPYCAUGHT_TITLE");
						messageBoxData.MessageText = GameText.CreateLocalised("MSG_SPYCAUGHT_TEXT");
						messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(item.OwnerRealm.DisplayName));
						messageBoxData.YesText = GameText.CreateLocalised("MSG_SPYCAUGHT_IGNORE");
						messageBoxData.YesTT = GameText.CreateLocalised("MSG_SPYCAUGHT_IGNORETT");
						messageBoxData.NoText = GameText.CreateLocalised("MSG_SPYCAUGHT_WARN");
						messageBoxData.NoTT = GameText.CreateLocalised("MSG_SPYCAUGHT_WARNTT");
						messageBoxData.Realm = item.OwnerRealm;
						messageBoxData.Agent = item;
						GameCore.MessageHandler.ShowMessage(messageBoxData);
					}
					else
					{
						Realm.AIPlayer.EspionageManager.RespondToEspionage(item);
					}
				}
			}
		}

		public WorkingZone GetZone(string Name)
		{
			return AllZones.Values.FirstOrDefault((WorkingZone x) => x.Name == Name);
		}

		public WorkingUnit CreateUnit(int RealmID, UnitData UnitType)
		{
			if (Thread.CurrentThread.ManagedThreadId != CreatorThread)
			{
				throw new Exception("Attempted to add a unit from incorrect thread");
			}
			WorkingUnit workingUnit = new WorkingUnit(this, NextUnitID++, UnitType);
			AllUnits.Add(workingUnit.ID, workingUnit);
			workingUnit.OwnerRealmID = RealmID;
			if (AllRealms[RealmID].Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Metallurgy) > 0 && (workingUnit.Race == Races.Human || workingUnit.Race == Races.Elf || workingUnit.Race == Races.Dwarf || workingUnit.Race == Races.Orc))
			{
				workingUnit.DefaultDamageType = DamageTypes.War;
			}
			if (AllRealms[RealmID].Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Siegecraft) > 0 && workingUnit.Class == UnitClasses.Siege)
			{
				workingUnit.Range.BaseValue++;
			}
			if (AllRealms[RealmID].Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Engineering) > 0 && workingUnit.Class == UnitClasses.Infantry && workingUnit.HasAnyNamedFlags("Besiege", "Saboteur", "Rebels"))
			{
				workingUnit.GrantFlag(UnitFlag.CreateNamedFlag(GameCore, "Bridging"));
			}
			if (AllRealms[RealmID].Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Medicine) > 0 && workingUnit.HasAnyNamedFlags("Healer", "MassHeal"))
			{
				workingUnit.HealRate.BaseValue++;
			}
			workingUnit.OnStackChanged += Unit_OnStackChanged;
			if (this.OnUnitCreated != null)
			{
				this.OnUnitCreated(workingUnit);
			}
			return workingUnit;
		}

		private void Unit_OnStackChanged(WorkingUnit Unit, int OldStackID, WorkingStack Stack)
		{
			if (this.OnUnitNodeChanged != null)
			{
				if (Stack == null)
				{
					this.OnUnitNodeChanged(Unit.ID, -1);
				}
				else
				{
					this.OnUnitNodeChanged(Unit.ID, Stack.NodeID);
				}
			}
			if (Stack != null && Stack.Node.NodeType == PathNodeTypes.Land && Unit.Class == UnitClasses.Naval && !Unit.Transport)
			{
				throw new Exception("Naval unit on land node");
			}
		}

		public WorkingStack CreateStack(int RealmID, int NodeID, bool AddToNode = true)
		{
			WorkingStack workingStack = new WorkingStack(NextStackID++, RealmID, this);
			workingStack.NodeID = NodeID;
			AllStacks.Add(workingStack.ID, workingStack);
			if (AddToNode)
			{
				AllNodes[NodeID].CurrentStackID = workingStack.ID;
			}
			workingStack.OnNodeChanged += Stack_OnNodeChanged;
			return workingStack;
		}

		public void HandleUnitNodeChanged(int UnitID, int NodeID)
		{
			if (this.OnUnitNodeChanged != null)
			{
				this.OnUnitNodeChanged(UnitID, NodeID);
			}
		}

		private void Stack_OnNodeChanged(int StackID, int NodeID)
		{
			if (this.OnUnitNodeChanged == null)
			{
				return;
			}
			foreach (WorkingUnit unit in AllStacks[StackID].Units)
			{
				this.OnUnitNodeChanged(unit.ID, NodeID);
			}
		}

		public void RemoveStack(WorkingStack Stack)
		{
			AllStacks.Remove(Stack.ID);
			Stack.Node.RemoveStack(Stack.ID);
			Stack.Dispose();
		}

		internal WorkingStack GetInterceptStack(WorkingRealm Realm, WorkingZone Zone, WorkingStack Stack)
		{
			WorkingStack result = null;
			Random random = new Random();
			int num = Stack.Units.Sum((WorkingUnit x) => x.ContactValue);
			foreach (ActivePathNode node in Zone.Nodes)
			{
				if (node.CurrentStack == null || node.CurrentStack == Stack || node.CurrentStack.Owner != Realm)
				{
					continue;
				}
				int num2 = node.CurrentStack.Units.Sum((WorkingUnit x) => x.ContactValue);
				if (num2 != 0)
				{
					num2 += num;
					num2 *= 2;
					if (random.Next(100) <= num2)
					{
						result = node.CurrentStack;
						break;
					}
				}
			}
			return result;
		}

		internal void StartBattle(WorkingStack Attacker, WorkingStack Defender, SovereigntyTK.Game.Path AttackPath)
		{
			int num = Attacker.Units.Count + Defender.Units.Count;
			bool flag = Attacker.Owner == PlayerRealm;
			if (num >= 28)
			{
				GameCore.Utilities.SoundManager.PlaySound("data\\sound\\effects\\explosion_underwater_blast_06.wav");
			}
			else if (num >= 15)
			{
				GameCore.Utilities.SoundManager.PlaySound("data\\sound\\effects\\distant_explosion02.wav");
			}
			else
			{
				GameCore.Utilities.SoundManager.PlaySound("data\\sound\\effects\\distant_explosion08.wav");
			}
			PendingBattle = new BattleStarter(Attacker, Defender, AttackPath, this);
			if (PendingBattle.Attacker.Units.Count == 0)
			{
				if (flag)
				{
					GameText caption = GameText.CreateLocalised("MSG_BATTLEFAILED_TITLE");
					GameText gameText = GameText.CreateLocalised("MSG_BATTLEFAILED_TEXT");
					gameText.AddChildText(GameText.CreateLocalised(Defender.Node.Province.DisplayName));
					GameCore.MessageHandler.ShowInfoMessage(caption, gameText);
				}
				DestroyStack(PendingBattle.Attacker);
				PendingBattle.Dispose();
				PendingBattle = null;
			}
		}

		internal bool MoveUnit(WorkingStack OldStack, WorkingStack NewStack, WorkingUnit Unit, SovereigntyTK.Game.Path MovePath, bool IgnorePackChecks = false)
		{
			if (NewStack.Node.Zone != null || OldStack.Node.Zone != null)
			{
				if (OldStack.Node.Zone == null || OldStack.Node.Zone != NewStack.Node.Zone)
				{
					Unit.Move(4f);
				}
			}
			else
			{
				Unit.Move(4f);
			}
			if (this.OnUnitPathing != null)
			{
				this.OnUnitPathing(Unit, MovePath);
			}
			if ((int)Unit.Health <= 0)
			{
				return false;
			}
			NewStack.TransferFromStack(Unit, MovePath, IgnorePackChecks);
			return true;
		}

		internal void DeployHero(WorkingHero Hero, ActivePathNode Node)
		{
			WorkingStack realmStack = Node.GetRealmStack(Hero.OwnerRealm);
			if (realmStack != null)
			{
				realmStack.AddHero(Hero);
				GameCore.FireEvent("HeroDeployed", Hero);
			}
		}

		internal void DeployUnit(WorkingUnit Unit, ActivePathNode Node)
		{
			if (Node.CurrentStack == null)
			{
				CreateStack(Unit.OwnerRealmID, Node.ID);
			}
			Node.CurrentStack.AddUnit(Unit);
			Unit.ClearMoves();
			GameCore.FireEvent("UnitDeployed", Unit);
		}

		internal void CleanupTacticalBattle(TacticalBattleController Battle)
		{
			CurrentTacticalBattle = null;
			CurrentAutoBattle = null;
			GameCore.CurrentBattleMap.Dispose();
			GameCore.CurrentBattleMap = null;
			BattleCleaner battleCleaner = new BattleCleaner(this, Battle);
			battleCleaner.Cleanup();
		}

		internal void CleanupAutoBattle(AutoBattleController Battle)
		{
			CurrentAutoBattle = null;
			BattleCleaner battleCleaner = new BattleCleaner(this, Battle);
			battleCleaner.Cleanup();
		}

		public void DestroyUnit(WorkingUnit Unit)
		{
			if (Thread.CurrentThread.ManagedThreadId != CreatorThread)
			{
				throw new Exception("Attempted to remove a unit from incorrect thread");
			}
			WorkingUnit carriedUnit = Unit.CarriedUnit;
			if (carriedUnit != null)
			{
				DestroyUnit(carriedUnit);
			}
			if (Unit.OwnerStackID > 0)
			{
				WorkingStack ownerStack = Unit.OwnerStack;
				ownerStack.RemoveUnit(Unit);
				if (ownerStack.Units.Count == 0)
				{
					DestroyStack(ownerStack);
				}
			}
			Unit.OwnerStackID = -1;
			if (Unit.Class != UnitClasses.Fort)
			{
				Unit.OwnerRealmID = -1;
				GameCore.FireEvent("UnitDestroyed", Unit);
				Unit.Dispose();
				AllUnits.Remove(Unit.ID);
			}
		}

		public void DestroyStack(WorkingStack Stack)
		{
			bool flag = Stack.Node != null && Stack.Node.Province != null && Stack.Node.Province.Occupied;
			WorkingProvince workingProvince = null;
			if (flag)
			{
				workingProvince = Stack.Node.Province;
			}
			if (Stack.Hero != null)
			{
				WorkingHero hero = Stack.Hero;
				AllHeroes.Remove(hero.ID);
				GameCore.FireEvent("HeroDestroyed", hero);
				hero.Dispose();
				Stack.RemoveHero();
			}
			Stack.OwnerID = -1;
			if (Stack.Node != null)
			{
				Stack.Node.RemoveStack(Stack.ID);
			}
			Stack.Dispose();
			AllStacks.Remove(Stack.ID);
			if (flag)
			{
				GameCore.FireEvent("ProvinceOccupierChanged", workingProvince);
			}
		}

		public TradeOffer GetPendingTrade(WorkingRealm Target)
		{
			if (!PlayerPendingTrades.ContainsKey(Target.ID))
			{
				PlayerPendingTrades.Add(Target.ID, new TradeOffer(this, PlayerRealm.ID, Target.ID));
			}
			return PlayerPendingTrades[Target.ID];
		}

		public void ClearPendingTrade(WorkingRealm Target)
		{
			if (PlayerPendingTrades.ContainsKey(Target.ID))
			{
				PlayerPendingTrades.Remove(Target.ID);
			}
		}

		public void SetPendingTrade(WorkingRealm Target, TradeOffer Offer)
		{
			if (!PlayerPendingTrades.ContainsKey(Target.ID))
			{
				PlayerPendingTrades.Add(Target.ID, Offer);
			}
			else
			{
				PlayerPendingTrades[Target.ID] = Offer;
			}
		}

		internal WorkingHero CreateHero(WorkingRealm Realm, HeroClassData HeroType)
		{
			WorkingHero workingHero = new WorkingHero(this, HeroType, NextHeroID++);
			workingHero.OwnerRealmID = Realm.ID;
			AllHeroes.Add(workingHero.ID, workingHero);
			return workingHero;
		}

		public void FinishSpellCasting(SpellEffect Spell, object Target)
		{
			if (Spell.CastOnTarget(Target) && Spell.Caster == PlayerRealm)
			{
				GameCore.Map.CurrentSpell = null;
				GameCore.Map.ChangeMode(MapModes.Default);
				GameCore.FireEvent("SpellCastingDone");
			}
		}

		public bool SelectSpellTarget(object Target)
		{
			switch (GameCore.Map.CurrentSpell.SpellData.TargetType)
			{
				case SpellTargets.Province:
				case SpellTargets.Realm:
				case SpellTargets.SeaZone:
				case SpellTargets.Stack:
					if (GameCore.Map.CurrentSpell.TargetIsValid(Target))
					{
						FinishSpellCasting(GameCore.Map.CurrentSpell, Target);
						return true;
					}
					return false;
				case SpellTargets.Unit:
					if (Target is WorkingStack)
					{
						if ((Target as WorkingStack).Units.Count((WorkingUnit x) => GameCore.Map.CurrentSpell.TargetIsValid(x)) == 0)
						{
							return false;
						}
						GameCore.FireEvent("UnitTargetRequested", GameCore.Map.CurrentSpell, Target as WorkingStack);
						return false;
					}
					if (GameCore.Map.CurrentSpell.TargetIsValid(Target))
					{
						FinishSpellCasting(GameCore.Map.CurrentSpell, Target);
						return true;
					}
					return false;
				default:
					throw new Exception("Spells with no target type should not use this function");
			}
		}

		public void BeginSpellCasting(SpellEffect Spell)
		{
			GameCore.Map.CurrentSpell = Spell;
			switch (Spell.SpellData.TargetType)
			{
				case SpellTargets.None:
					FinishSpellCasting(Spell, null);
					break;
				case SpellTargets.Province:
					GameCore.Map.ChangeMode(MapModes.CastProvince);
					break;
				case SpellTargets.Realm:
					if (Spell.SpellData.Range == 0)
					{
						FinishSpellCasting(Spell, PlayerRealm);
					}
					else
					{
						GameCore.Map.ChangeMode(MapModes.CastRealm);
					}
					break;
				case SpellTargets.Stack:
					GameCore.Map.ChangeMode(MapModes.CastStack);
					break;
				case SpellTargets.SeaZone:
					GameCore.Map.ChangeMode(MapModes.CastZone);
					break;
				case SpellTargets.Unit:
					GameCore.Map.ChangeMode(MapModes.CastUnit);
					break;
			}
			GameCore.FireEvent("SpellCastingStarted", Spell);
		}

		public void CancelCasting()
		{
			GameCore.Map.CurrentSpell = null;
			GameCore.Map.ChangeMode(MapModes.Default);
			GameCore.FireEvent("SpellCastingDone");
		}

		public void AddBuilding(BuildingEffect Building)
		{
			AllBuildings.Add(Building.ID, Building);
		}

		public void RemoveBuilding(BuildingEffect Building)
		{
			AllBuildings.Remove(Building.ID);
		}

		public void ChangeProvinceOwner(WorkingProvince Province, WorkingRealm Realm)
		{
			WorkingRealm ownerRealm = Province.OwnerRealm;
			Province.OwnerID = Realm.ID;
			if (Realm != RebelRealm)
			{
				foreach (WorkingRealm item in AllRealms.Values.Where((WorkingRealm x) => x.CodeOfWar && x != Realm))
				{
					if (item != RebelRealm && ownerRealm.CodeOfWar)
					{
						item.DiplomacyManager.TriggerEvent(Realm, "Annex");
					}
				}
				if (Realm.AIPlayer != null)
				{
					Realm.AIPlayer.WarManager.ResetProvinceGainTimer();
				}
			}
			if (Province.IsCapitol)
			{
				Province.RemoveCapitol();
				foreach (WorkingProvince item2 in ownerRealm.Provinces.ToList())
				{
					if (item2.Occupied)
					{
						ChangeProvinceOwner(item2, item2.OccupierRealm);
					}
					else
					{
						ChangeProvinceOwner(item2, RebelRealm);
					}
				}
				foreach (WorkingStack item3 in ownerRealm.Stacks.ToList())
				{
					if (item3.Node.NodeType == PathNodeTypes.Sea)
					{
						DestroyStack(item3);
					}
					else if (item3.Node.Province.OwnerRealm == RebelRealm)
					{
						if (item3.Hero != null)
						{
							WorkingHero hero = item3.Hero;
							item3.RemoveHero();
							AllHeroes.Remove(hero.ID);
							hero.Dispose();
						}
						item3.SetOwner(RebelRealm.ID);
					}
					else
					{
						DisbandStack(item3);
					}
				}
				foreach (WorkingRealm value in AllRealms.Values)
				{
					if (!value.RealmIsDead && value != ownerRealm && value != RebelRealm)
					{
						switch (ownerRealm.DiplomacyManager.GetRelation(value))
						{
							case RelationStates.Alliance:
							case RelationStates.Defence:
							case RelationStates.NAP:
								AllianceController.BreakCurrentTreaty(ownerRealm, value, ShowMessage: false);
								break;
							case RelationStates.War:
								AllianceController.EndWar(ownerRealm, value);
								break;
						}
					}
				}
				foreach (WorkingAgent item4 in ownerRealm.Agents.ToList())
				{
					item4.RecallImmediate();
				}
				foreach (WorkingRealm value2 in AllRealms.Values)
				{
					if (value2.RealmIsDead || value2 == ownerRealm || value2 == RebelRealm)
					{
						continue;
					}
					foreach (WorkingAgent agent in value2.Agents)
					{
						if (agent.HostRealm == ownerRealm)
						{
							agent.RecallImmediate();
						}
						if (agent.TargetRealm == ownerRealm)
						{
							agent.RecallImmediate();
						}
					}
				}
				foreach (SpellEffect item5 in ownerRealm.MagicData.CastSpells.ToList())
				{
					item5.Dispel(Force: false);
				}
				if (Realm != RebelRealm && !Realm.DiplomacyManager.IgnoreDestroyPenalties.Contains(ownerRealm.Name))
				{
					foreach (WorkingRealm value3 in AllRealms.Values)
					{
						if (value3 != RebelRealm && value3 != Realm)
						{
							if (value3.CodeOfWar)
							{
								value3.DiplomacyManager.TriggerEvent(Realm, "Usurper");
							}
							else
							{
								value3.DiplomacyManager.TriggerEvent(Realm, "Overlord");
							}
							if (value3.Alignment == ownerRealm.Alignment)
							{
								value3.DiplomacyManager.TriggerEvent(Realm, "Crusade");
							}
							if (value3.Race == ownerRealm.Race)
							{
								value3.DiplomacyManager.TriggerEvent(Realm, "Bloodbond");
							}
							value3.DiplomacyManager.AdjustBaseValue(Realm, -2f);
						}
					}
				}
				ownerRealm.KillRealm();
				GameCore.FireEvent("RealmDestroyed", ownerRealm, Realm);
			}
			GameCore.Map.UpdateRealmBorders(ownerRealm, Realm);
			GameCore.Map.ChangeMode(MapModes.Default);
		}

		private void DisbandStack(WorkingStack Stack)
		{
			foreach (WorkingUnit item in Stack.Units.ToList())
			{
				Stack.RemoveUnit(item);
				DestroyUnit(item);
			}
			DestroyStack(Stack);
		}

		internal void StartGameTurn()
		{
			ScoreKeeper.UpdateScores();
			Marketplace.Update();
			Autosave();
			GameCore.FireEvent("GameTurnStarted");
			SetAllowedTraders();
		}

		private void SetAllowedTraders()
		{
			Random random = new Random();
			int num = 0;
			int num2 = random.Next(10) + 1;
			num = ((num2 >= 6) ? ((num2 < 9) ? 1 : 2) : 0);
			TradeAllowedRealms = new List<string>();
			List<WorkingRealm> list = AllRealms.Values.Where((WorkingRealm x) => !x.RealmIsDead && x != PlayerRealm && x != RebelRealm).ToList();
			if (num > list.Count)
			{
				num = list.Count;
			}
			while (num > 0)
			{
				WorkingRealm workingRealm = list[random.Next(list.Count)];
				if (!TradeAllowedRealms.Contains(workingRealm.Name))
				{
					TradeAllowedRealms.Add(workingRealm.Name);
					num--;
				}
			}
		}

		public bool TradeAllowed(WorkingRealm Realm)
		{
			if (TradeAllowedRealms == null)
			{
				SetAllowedTraders();
			}
			return TradeAllowedRealms.Contains(Realm.Name);
		}

		public void TradeOffered(WorkingRealm Realm)
		{
			TradeAllowedRealms.Remove(Realm.Name);
		}

		public WorkingUnit GetUnit(int UnitID)
		{
			WorkingUnit value = null;
			AllUnits.TryGetValue(UnitID, out value);
			return value;
		}

		public WorkingHero GetHero(int HeroID)
		{
			WorkingHero value = null;
			AllHeroes.TryGetValue(HeroID, out value);
			return value;
		}

		public int DebugPoint()
		{
			return 10;
		}

		public void UnlockPlayerUnit(string UnitName)
		{
			PlayerRealm.Restrictions.AllowUnits.Add(UnitName);
			UnitData unit = PlayerRealm.UnitPurchaseManager.GetUnit(UnitName);
			if (unit != null)
			{
				GameCore.FireEvent("UnitUnlocked", unit);
			}
		}

		public BuildingEffect CreateBuilding(Type t, BuildingData Data, WorkingProvince Province)
		{
			BuildingEffect buildingEffect = (BuildingEffect)Activator.CreateInstance(t);
			buildingEffect.Data = Data;
			buildingEffect.Game = this;
			buildingEffect.ProvinceID = Province.ID;
			buildingEffect.ID = NextBuildingID++;
			return buildingEffect;
		}

		internal float GetStackSpeed(int SpeedIndex)
		{
			switch (SpeedIndex)
			{
				case 1:
					return 200f;
				case 2:
					return 400f;
				case 3:
					return 600f;
				case 4:
					return 800f;
				case 5:
					return 1000f;
				case 6:
					return 1200f;
				case 7:
					return 1400f;
				case 8:
					return 1600f;
				case 9:
					return 1800f;
				case 10:
					return 2000f;
				default:
					return 1000f;
			}
		}

		internal void DestroyHero(WorkingHero Hero)
		{
			if (Hero.OwnerStack != null)
			{
				Hero.OwnerStack.RemoveHero();
			}
			AllHeroes.Remove(Hero.ID);
			GameCore.FireEvent("HeroDestroyed", Hero);
			Hero.Dispose();
		}
	}
}