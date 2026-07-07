// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.UI.Map.CampaignMap
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenTK;
using OpenTK.Input;
using SovereigntyTK;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Map;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.UI.Map
{
	public class CampaignMap
	{
		public MapRenderer Renderer;

		public MapInputManager InputManager;

		public Sovereignty Game;

		public MapTextManager TextManager;

		private string MouseOverRegionName;

		private ActivePathNode NearestNode;

		private MapModes m_CurrentMode = MapModes.RealmSelect;

		public WorkingStack ActiveStack;

		public UnitQueueItem CurrentDeployItem;

		public SpellEffect CurrentSpell;

		private List<SpriteButton> ActiveButtons;

		public WorkingHero CurrentDeployHero;

		internal WorkingRealm DebugRealm;

		public WorkingRealm ActiveRelationsRealm;

		private Dictionary<string, List<MapText>> RealmNameTexts;

		private bool PreventModeChanges;

		public List<UnitQueueItem> CurrentDeployList;

		public MapModes CurrentMode => m_CurrentMode;

		public event StringDelegate OnProvinceClicked;

		public CampaignMap(Sovereignty Game)
		{
			this.Game = Game;
			Renderer = new MapRenderer(Game);
			InputManager = new MapInputManager(Game);
			TextManager = new MapTextManager(Game);
			ActiveButtons = new List<SpriteButton>();
			RealmNameTexts = new Dictionary<string, List<MapText>>();
			Game.Camera.SetPosition(1672f, 2000f, 1272f);
			Game.RegisterEvents(Game_OnScriptEvent, "HighlightRealm", "HighlightProvince", "ClearHighlights");
			Game.RegisterEvent(HandleOwnerChanged, "ProvinceOwnerChanged");
			Game.RegisterEvent(HandleOwnerChanged, "ProvinceOccupierChanged");
		}

		private void HandleOwnerChanged(string EventName, params object[] Args)
		{
			if (Args == null || Args.Length == 0)
			{
				return;
			}
			WorkingProvince workingProvince = Args[0] as WorkingProvince;
			if (Game.CurrentGame == null || workingProvince == null)
			{
				return;
			}
			if (workingProvince.OwnerRealm == Game.CurrentGame.PlayerRealm)
			{
				if (workingProvince.Occupied)
				{
					Renderer.AddProvinceHighlight(workingProvince.Name, new Vector4(1f, 0f, 0f, 0f));
				}
				else
				{
					Renderer.RemoveProvinceHighlight(workingProvince.Name);
				}
			}
			else
			{
				Renderer.RemoveProvinceHighlight(workingProvince.Name);
			}
		}

		private void SetDefaultNames()
		{
			foreach (RealmData Realm in Game.Data.ActiveRealms.Values)
			{
				if (RealmNameTexts.ContainsKey(Realm.Name))
				{
					foreach (MapText item in RealmNameTexts[Realm.Name])
					{
						TextManager.DisposeText(item);
					}
					RealmNameTexts[Realm.Name].Clear();
				}
				List<int> list = new List<int>();
				foreach (ProvinceData item2 in Game.Data.ActiveProvinces.Values.Where((ProvinceData x) => x.Owner == Realm.Name))
				{
					list.Add(item2.ID);
				}
				MapText mapText = TextManager.CreateText(list, GameText.CreateLocalised(Realm.DisplayName));
				if (mapText != null)
				{
					if (!RealmNameTexts.ContainsKey(Realm.Name))
					{
						RealmNameTexts.Add(Realm.Name, new List<MapText>());
					}
					RealmNameTexts[Realm.Name].Add(mapText);
				}
			}
		}

		internal void Render(float ElapsedTime)
		{
			Renderer.Render(ElapsedTime);
			TextManager.Render();
		}

		internal void Dispose()
		{
			Game.UnregisterEvents(Game_OnScriptEvent, "HighlightRealm", "HighlightProvince", "ClearHighlights");
			Renderer.Dispose();
		}

		internal void HandleMouseMove(MouseMoveEventArgs e)
		{
			if (CurrentMode != MapModes.StackMove)
			{
				Sovereignty game = Game;
				object[] args = new object[1];
				game.FireEvent("TooltipChanged", args);
			}
			InputManager.HandleMouseMove(e);
		}

		public void SetMouseoverRealm(string RealmName)
		{
			Renderer.SetRealmHighlight(RealmName);
		}

		public void ChangeMode(MapModes NewMode, bool Force = false)
		{
			if ((PreventModeChanges && !Force) || NewMode == CurrentMode)
			{
				return;
			}
			MapModes currentMode = CurrentMode;
			m_CurrentMode = NewMode;
			if (Game.CurrentGame != null)
			{
				ActiveRelationsRealm = Game.CurrentGame.PlayerRealm;
			}
			Game.FireEvent("MapModeChanged", CurrentMode);
			LeaveMode(currentMode);
			if (MapText.Shader != null)
			{
				MapText.Shader.SetFloat("EnableFading", 1f);
			}
			switch (CurrentMode)
			{
				case MapModes.Debug1:
					{
						Dictionary<GameRegion, float> dictionary = DebugRealm.AIPlayer.Utility.GenerateValueMap();
						foreach (KeyValuePair<GameRegion, float> item in dictionary)
						{
							int num = (int)item.Value;
							if (num < 0)
							{
								num = 0;
							}
							if (num > 255)
							{
								num = 255;
							}
							if (item.Key is WorkingProvince)
							{
								Renderer.SetProvinceColour(item.Key.Name, Color.FromArgb(255, 255, 255 - num, 255 - num));
							}
							else
							{
								Renderer.SetZoneColour(item.Key.Name, Color.FromArgb(255, 255, 255 - num, 255 - num));
							}
						}
						Renderer.BordersOnTop = true;
						break;
					}
				case MapModes.CastProvince:
					foreach (WorkingZone value in Game.CurrentGame.AllZones.Values)
					{
						Renderer.SetZoneColour(value.Name, Color.FromArgb(128, 0, 0, 0));
					}
					{
						foreach (WorkingProvince value2 in Game.CurrentGame.AllProvinces.Values)
						{
							if (CurrentSpell.TargetIsValid(value2))
							{
								Renderer.SetProvinceColour(value2.Name, Color.FromArgb(0, 0, 0, 0));
							}
							else
							{
								Renderer.SetProvinceColour(value2.Name, Color.FromArgb(128, 0, 0, 0));
							}
						}
						break;
					}
				case MapModes.CastRealm:
					foreach (WorkingZone value3 in Game.CurrentGame.AllZones.Values)
					{
						Renderer.SetZoneColour(value3.Name, Color.FromArgb(128, 0, 0, 0));
					}
					{
						foreach (WorkingRealm value4 in Game.CurrentGame.AllRealms.Values)
						{
							if (CurrentSpell.TargetIsValid(value4))
							{
								Renderer.SetLiveRealmColour(value4.Name, Color.FromArgb(0, 0, 0, 0));
							}
							else
							{
								Renderer.SetLiveRealmColour(value4.Name, Color.FromArgb(128, 0, 0, 0));
							}
						}
						break;
					}
				case MapModes.CastStack:
					{
						List<GameRegion> list = new List<GameRegion>();
						foreach (WorkingStack value5 in Game.CurrentGame.AllStacks.Values)
						{
							if (CurrentSpell.TargetIsValid(value5))
							{
								value5.ShowHighlight();
								list.Add(value5.Node.GetRegion());
							}
							else
							{
								value5.HideHighlight();
							}
						}
						foreach (WorkingZone value6 in Game.CurrentGame.AllZones.Values)
						{
							if (list.Contains(value6))
							{
								Renderer.SetZoneColour(value6.Name, Color.FromArgb(0, 0, 0, 0));
							}
							else
							{
								Renderer.SetZoneColour(value6.Name, Color.FromArgb(128, 0, 0, 0));
							}
						}
						{
							foreach (WorkingProvince value7 in Game.CurrentGame.AllProvinces.Values)
							{
								if (list.Contains(value7))
								{
									Renderer.SetProvinceColour(value7.Name, Color.FromArgb(0, 0, 0, 0));
								}
								else
								{
									Renderer.SetProvinceColour(value7.Name, Color.FromArgb(128, 0, 0, 0));
								}
							}
							break;
						}
					}
				case MapModes.CastUnit:
					{
						List<GameRegion> list2 = new List<GameRegion>();
						foreach (WorkingStack value8 in Game.CurrentGame.AllStacks.Values)
						{
							if (value8.Units.FirstOrDefault((WorkingUnit x) => CurrentSpell.TargetIsValid(x)) != null)
							{
								value8.ShowHighlight();
								list2.Add(value8.Node.GetRegion());
							}
							else
							{
								value8.HideHighlight();
							}
						}
						foreach (WorkingZone value9 in Game.CurrentGame.AllZones.Values)
						{
							if (list2.Contains(value9))
							{
								Renderer.SetZoneColour(value9.Name, Color.FromArgb(0, 0, 0, 0));
							}
							else
							{
								Renderer.SetZoneColour(value9.Name, Color.FromArgb(128, 0, 0, 0));
							}
						}
						{
							foreach (WorkingProvince value10 in Game.CurrentGame.AllProvinces.Values)
							{
								if (list2.Contains(value10))
								{
									Renderer.SetProvinceColour(value10.Name, Color.FromArgb(0, 0, 0, 0));
								}
								else
								{
									Renderer.SetProvinceColour(value10.Name, Color.FromArgb(128, 0, 0, 0));
								}
							}
							break;
						}
					}
				case MapModes.CastZone:
					foreach (WorkingProvince value11 in Game.CurrentGame.AllProvinces.Values)
					{
						Renderer.SetProvinceColour(value11.Name, Color.FromArgb(128, 0, 0, 0));
					}
					{
						foreach (WorkingZone value12 in Game.CurrentGame.AllZones.Values)
						{
							if (CurrentSpell.TargetIsValid(value12))
							{
								Renderer.SetZoneColour(value12.Name, Color.FromArgb(0, 0, 0, 0));
							}
							else
							{
								Renderer.SetZoneColour(value12.Name, Color.FromArgb(128, 0, 0, 0));
							}
						}
						break;
					}
				case MapModes.Relations:
					RefreshMode();
					break;
				case MapModes.DeployHero:
					PreventModeChanges = true;
					foreach (WorkingProvince value13 in Game.CurrentGame.AllProvinces.Values)
					{
						bool flag = Game.CurrentGame.DestinationChecker.CanDeployHero(Game.CurrentGame.PlayerRealm, value13.LandNode);
						bool flag2 = Game.CurrentGame.DestinationChecker.CanDeployHero(Game.CurrentGame.PlayerRealm, value13.HarbourNode);
						if (flag || flag2)
						{
							Renderer.SetProvinceColour(value13.Name, Color.FromArgb(0, 0, 0, 0));
						}
						else
						{
							Renderer.SetProvinceColour(value13.Name, Color.FromArgb(128, 0, 0, 0));
						}
					}
					{
						foreach (WorkingZone value14 in Game.CurrentGame.AllZones.Values)
						{
							if (value14.Nodes.Count((ActivePathNode x) => x.CurrentStack != null && x.CurrentStack.Owner == Game.CurrentGame.PlayerRealm && x.CurrentStack.Hero == null) > 0)
							{
								Renderer.SetProvinceColour(value14.Name, Color.FromArgb(0, 0, 0, 0));
							}
							else
							{
								Renderer.SetProvinceColour(value14.Name, Color.FromArgb(128, 0, 0, 0));
							}
						}
						break;
					}
				case MapModes.DeployUnit:
					foreach (WorkingProvince value15 in Game.CurrentGame.AllProvinces.Values)
					{
						UnitMoveResult unitMoveResult = UnitMoveResult.OK;
						UnitMoveResult unitMoveResult2 = UnitMoveResult.OK;
						if (CurrentDeployList == null)
						{
							unitMoveResult = Game.CurrentGame.DestinationChecker.NodeOKToDeploy(CurrentDeployItem.Unit, Game.CurrentGame.PlayerRealm, value15.LandNode);
							unitMoveResult2 = Game.CurrentGame.DestinationChecker.NodeOKToDeploy(CurrentDeployItem.Unit, Game.CurrentGame.PlayerRealm, value15.HarbourNode);
						}
						else
						{
							foreach (UnitQueueItem currentDeploy in CurrentDeployList)
							{
								UnitMoveResult unitMoveResult3 = Game.CurrentGame.DestinationChecker.NodeOKToDeploy(currentDeploy.Unit, Game.CurrentGame.PlayerRealm, value15.LandNode);
								if (unitMoveResult3 != UnitMoveResult.OK)
								{
									unitMoveResult = unitMoveResult3;
								}
							}
							foreach (UnitQueueItem currentDeploy2 in CurrentDeployList)
							{
								UnitMoveResult unitMoveResult4 = Game.CurrentGame.DestinationChecker.NodeOKToDeploy(currentDeploy2.Unit, Game.CurrentGame.PlayerRealm, value15.HarbourNode);
								if (unitMoveResult4 != UnitMoveResult.OK)
								{
									unitMoveResult2 = unitMoveResult4;
								}
							}
							if (value15.LandNode.CurrentStack != null && value15.LandNode.CurrentStack.Units.Count + CurrentDeployList.Count > 20)
							{
								unitMoveResult = UnitMoveResult.ProvinceFull;
							}
							if (value15.HarbourNode != null && value15.HarbourNode.CurrentStack != null && value15.HarbourNode.CurrentStack.Units.Count + CurrentDeployList.Count > 20)
							{
								unitMoveResult2 = UnitMoveResult.ProvinceFull;
							}
							if (CurrentDeployList.Count > 20)
							{
								unitMoveResult = UnitMoveResult.ProvinceFull;
								unitMoveResult2 = UnitMoveResult.ProvinceFull;
							}
						}
						if (unitMoveResult == UnitMoveResult.OK)
						{
							value15.DeploymentStatus = UnitMoveResult.OK;
						}
						else if (unitMoveResult2 == UnitMoveResult.OK)
						{
							value15.DeploymentStatus = UnitMoveResult.OK;
						}
						else if (unitMoveResult != UnitMoveResult.OK)
						{
							value15.DeploymentStatus = unitMoveResult;
						}
						else if (unitMoveResult2 != UnitMoveResult.OK)
						{
							value15.DeploymentStatus = unitMoveResult2;
						}
						if (value15.DeploymentStatus == UnitMoveResult.OK)
						{
							Renderer.SetProvinceColour(value15.Name, Color.FromArgb(0, 0, 0, 0));
						}
						else
						{
							Renderer.SetProvinceColour(value15.Name, Color.FromArgb(128, 0, 0, 0));
						}
					}
					{
						foreach (WorkingZone value16 in Game.CurrentGame.AllZones.Values)
						{
							Renderer.SetProvinceColour(value16.Name, Color.FromArgb(128, 0, 0, 0));
						}
						break;
					}
				case MapModes.Default:
					Renderer.ClearSelectionOutlines();
					foreach (WorkingRealm value17 in Game.CurrentGame.AllRealms.Values)
					{
						Renderer.SetLiveRealmColour(value17.Name, Color.Transparent);
					}
					{
						foreach (WorkingZone value18 in Game.CurrentGame.AllZones.Values)
						{
							Renderer.SetZoneColour(value18.Name, Color.Transparent);
						}
						break;
					}
				case MapModes.RealmSelect:
					if (MapText.Shader != null)
					{
						MapText.Shader.SetFloat("EnableFading", 0f);
					}
					Renderer.ClearHighlight();
					Renderer.ClearRealmBorders();
					Renderer.CreateSelectionOutlines();
					SetDefaultNames();
					foreach (string key in Game.Data.ActiveProvinces.Keys)
					{
						Renderer.SetProvinceColour(key, Color.Transparent);
					}
					foreach (string key2 in Game.Data.ActiveSeaZones.Keys)
					{
						Renderer.SetZoneColour(key2, Color.Transparent);
					}
					Game.Camera.MinZoomLevel = 800f;
					break;
				case MapModes.Political:
					foreach (SpriteButton activeButton in ActiveButtons)
					{
						activeButton.Dispose();
					}
					ActiveButtons.Clear();
					{
						foreach (WorkingRealm value19 in Game.CurrentGame.AllRealms.Values)
						{
							Color colour = Color.FromArgb(200, value19.MinimapColour.R, value19.MinimapColour.G, value19.MinimapColour.B);
							Renderer.SetLiveRealmColour(value19.Name, colour);
							if (value19 != Game.CurrentGame.PlayerRealm)
							{
								CreateRelationButtons(value19);
								if (value19.CapitolProvince != null)
								{
									value19.CapitolProvince.SetRelationsMode(State: true);
								}
							}
						}
						break;
					}
				case MapModes.Economy:
					RefreshMode();
					break;
				case MapModes.StackMove:
					RefreshMode();
					break;
			}
		}

		public void UpdateRealmNameText(WorkingRealm Realm)
		{
			List<List<int>> list = new List<List<int>>();
			List<string> list2 = new List<string>();
			foreach (WorkingProvince province in Realm.Provinces)
			{
				list2.Add(province.Name);
			}
			List<WorkingProvince> list3 = Realm.Provinces.ToList();
			while (list3.Count > 0)
			{
				List<int> list4 = new List<int>();
				list4.Add(Game.Data.ActiveProvinces[list3[0].Name].ID);
				list3.RemoveAt(0);
				bool flag = true;
				while (flag)
				{
					flag = false;
					foreach (int item in list4.ToList())
					{
						ProvinceData provinceData = Game.Data.ProvincesByID[item] as ProvinceData;
						foreach (ProvinceLink adjacentProvince in provinceData.AdjacentProvinces)
						{
							WorkingProvince Linked = Game.CurrentGame.AllProvinces[adjacentProvince.LinkedProvinceID];
							if (!adjacentProvince.IgnoreForBorders && !list4.Contains(Linked.ID) && list2.Contains(Linked.Name))
							{
								list3.RemoveAll((WorkingProvince x) => x.Name == Linked.Name);
								list4.Add(Linked.ID);
								flag = true;
							}
						}
					}
				}
				list.Add(list4);
			}
			if (!RealmNameTexts.ContainsKey(Realm.Name))
			{
				RealmNameTexts.Add(Realm.Name, new List<MapText>());
			}
			foreach (MapText item2 in RealmNameTexts[Realm.Name])
			{
				TextManager.DisposeText(item2);
			}
			RealmNameTexts[Realm.Name].Clear();
			foreach (List<int> item3 in list)
			{
				MapText mapText = TextManager.CreateText(item3, GameText.CreateLocalised(Realm.DisplayName));
				if (mapText != null)
				{
					RealmNameTexts[Realm.Name].Add(mapText);
				}
			}
		}

		private void TreatyButton_OnClick(SpriteButton Button)
		{
			TreatyTypes treatyTypes = TreatyTypes.None;
			switch (Game.CurrentGame.PlayerRealm.DiplomacyManager.GetRelation(Button.Target))
			{
				case RelationStates.Peace:
					treatyTypes = TreatyTypes.NonAggression;
					break;
				case RelationStates.NAP:
					treatyTypes = TreatyTypes.MutualDefence;
					break;
				case RelationStates.Defence:
					treatyTypes = TreatyTypes.Alliance;
					break;
			}
			if (treatyTypes != TreatyTypes.None)
			{
				Game.FireEvent("OfferTreaty", Button.Target, treatyTypes);
			}
		}

		private void WarButton_OnClick(SpriteButton Button)
		{
			Game.FireEvent("ShowWarDialog", Button.Target, null, null);
		}

		private void BreakButton_OnClick(SpriteButton Button)
		{
			MessageBoxData messageBoxData = new MessageBoxData();
			messageBoxData.CaptionText = GameText.CreateLocalised("TREATY_BREAK_TITLE");
			messageBoxData.MessageText = GameText.CreateLocalised("TREATY_BREAK_TEXT2");
			messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(Button.Target.DisplayName));
			messageBoxData.YesText = GameText.CreateLocalised("TREATY_BREAK_YES");
			messageBoxData.NoText = GameText.CreateLocalised("TREATY_BREAK_NO");
			messageBoxData.DisplayType = MessageBoxType.YesNo;
			messageBoxData.MsgType = MessageType.TreatyBreak;
			messageBoxData.Realm = Button.Target;
			Game.MessageHandler.ShowMessage(messageBoxData);
		}

		private void TradeButton_OnClick(SpriteButton Button)
		{
			Game.FireEvent("OfferTrade", Button.Target);
		}

		private void PeaceButton_OnClick(SpriteButton Button)
		{
			Game.FireEvent("OfferTreaty", Button.Target, TreatyTypes.Peace);
		}

		private PointF ModifyCoords(PointF point, int X, int Y)
		{
			return new PointF(point.X + (float)X, point.Y + (float)Y);
		}

		private void LeaveMode(MapModes OldMode)
		{
			switch (OldMode)
			{
				case MapModes.Debug1:
					Renderer.BordersOnTop = false;
					break;
				case MapModes.CastStack:
				case MapModes.CastUnit:
					if (Game.CurrentGame == null)
					{
						break;
					}
					{
						foreach (WorkingStack value in Game.CurrentGame.AllStacks.Values)
						{
							value.HideHighlight();
						}
						break;
					}
				case MapModes.Relations:
					{
						foreach (SpriteButton activeButton in ActiveButtons)
						{
							activeButton.Dispose();
						}
						ActiveButtons.Clear();
						Color allyColour = Color.FromArgb(102, 56, 89);
						if (Game.CurrentGame != null)
						{
							foreach (WorkingRealm value2 in Game.CurrentGame.AllRealms.Values)
							{
								Renderer.SetLiveBorder(value2.Name, allyColour, -7f);
								if (value2.CapitolProvince != null)
								{
									value2.CapitolProvince.SetRelationsMode(State: false);
								}
							}
							break;
						}
						{
							foreach (RealmData value3 in Game.Data.ActiveRealms.Values)
							{
								Renderer.SetBorder(value3.Name, allyColour, -7f);
							}
							break;
						}
					}
				case MapModes.Political:
					foreach (SpriteButton activeButton2 in ActiveButtons)
					{
						activeButton2.Dispose();
					}
					ActiveButtons.Clear();
					if (Game.CurrentGame == null)
					{
						break;
					}
					{
						foreach (WorkingRealm value4 in Game.CurrentGame.AllRealms.Values)
						{
							if (value4.CapitolProvince != null)
							{
								value4.CapitolProvince.SetRelationsMode(State: false);
							}
						}
						break;
					}
				case MapModes.StackMove:
					{
						if (ActiveStack != null)
						{
							ActiveStack.HideSelected();
						}
						if (Game.CurrentGame != null)
						{
							foreach (WorkingProvince value5 in Game.CurrentGame.AllProvinces.Values)
							{
								value5.SetMoveMode(State: false);
							}
						}
						Sovereignty game = Game;
						object[] args = new object[1];
						game.FireEvent("StackSelectionChanged", args);
						Renderer.ClearArrow();
						break;
					}
				case MapModes.Economy:
					if (Game.CurrentGame == null || Game.CurrentGame == null)
					{
						break;
					}
					{
						foreach (WorkingProvince value6 in Game.CurrentGame.AllProvinces.Values)
						{
							Renderer.SetProvinceColour(value6.Name, Color.Transparent);
						}
						break;
					}
				case MapModes.RealmSelect:
					Renderer.ClearHighlight();
					UpdateRealmBorders();
					Game.Camera.MinZoomLevel = 500f;
					break;
				case MapModes.DeployUnit:
				case MapModes.DeployHero:
				case MapModes.CastRealm:
				case MapModes.CastProvince:
				case MapModes.CastZone:
					break;
			}
		}

		public void UpdateRealmBorders(params WorkingRealm[] Realms)
		{
			foreach (WorkingRealm workingRealm in Realms)
			{
				Renderer.UpdateRealmBorder(workingRealm.Name);
			}
			TurnController_OnStartTurn(Game.CurrentGame.TurnController.CurrentRealm);
		}

		public void UpdateRealmBorders()
		{
			foreach (WorkingRealm value in Game.CurrentGame.AllRealms.Values)
			{
				Renderer.UpdateRealmBorder(value.Name);
			}
			TurnController_OnStartTurn(Game.CurrentGame.TurnController.CurrentRealm);
		}

		internal void SetNearestNode(ActivePathNode Node)
		{
			if (Node == NearestNode)
			{
				return;
			}
			NearestNode = Node;
			if (CurrentMode != MapModes.StackMove)
			{
				return;
			}
			if (NearestNode == null)
			{
				Renderer.ClearArrow();
				Sovereignty game = Game;
				object[] args = new object[1];
				game.FireEvent("TooltipChanged", args);
				return;
			}
			if (NearestNode == ActiveStack.Node)
			{
				Renderer.ClearArrow();
				Sovereignty game2 = Game;
				object[] args2 = new object[1];
				game2.FireEvent("TooltipChanged", args2);
				return;
			}
			Path path = Game.CurrentGame.PathManager.GetPath(ActiveStack.Node, NearestNode, ActiveStack.Units.Where((WorkingUnit x) => x.Selected), CheckUnitMoves: true, ActiveStack.Owner);
			if (path == null || path.PathPoints.Count < 2)
			{
				Renderer.ClearArrow();
			}
			else
			{
				Renderer.ShowArrow(path.GetVertices());
				Sovereignty game3 = Game;
				object[] args3 = new object[1];
				game3.FireEvent("TooltipChanged", args3);
			}
			UnitMoveResult unitMoveResult = Game.CurrentGame.DestinationChecker.NodeOkForStack(ActiveStack, NearestNode);
			if (unitMoveResult == UnitMoveResult.OK)
			{
				return;
			}
			List<UnitMoveResult> list = new List<UnitMoveResult>();
			foreach (WorkingUnit item in ActiveStack.Units.Where((WorkingUnit x) => x.Selected))
			{
				UnitMoveResult unitMoveResult2 = Game.CurrentGame.DestinationChecker.NodeOKForUnit(item, NearestNode);
				if (unitMoveResult2 != UnitMoveResult.OK && !list.Contains(unitMoveResult2))
				{
					list.Add(unitMoveResult2);
				}
			}
			if (!list.Contains(unitMoveResult))
			{
				list.Add(unitMoveResult);
			}
			if (list.Count <= 0)
			{
				return;
			}
			List<GameText> list2 = new List<GameText>();
			list2.Add(GameText.CreateLocalised("STACK_NOMOVE"));
			foreach (UnitMoveResult item2 in list)
			{
				list2.Add(Game.Utilities.TooltipFactory.GetUnitMoveStatusText(item2));
				list2.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
			}
			Game.FireEvent("TooltipChanged", list2);
		}

		internal void SetMouseoverRegion(string RegionName)
		{
			if (RegionName == MouseOverRegionName)
			{
				return;
			}
			MouseOverRegionName = RegionName;
			switch (CurrentMode)
			{
				case MapModes.RealmSelect:
					if (RegionName == null || RegionName == "")
					{
						Renderer.ClearHighlight();
					}
					else if (!Game.Data.ActiveProvinces.ContainsKey(RegionName))
					{
						Renderer.ClearHighlight();
					}
					else
					{
						Renderer.SetRealmHighlight(Game.Data.ActiveProvinces[RegionName].Owner);
					}
					break;
				case MapModes.Political:
				case MapModes.Relations:
				case MapModes.CastRealm:
					{
						WorkingProvince province3 = Game.CurrentGame.GetProvince(RegionName);
						if (province3 == null)
						{
							Renderer.ClearHighlight();
							Sovereignty game3 = Game;
							object[] args3 = new object[1];
							game3.FireEvent("MapRealmMouseoverChanged", args3);
						}
						else
						{
							Renderer.SetRealmHighlight(province3.OwnerRealm.Name);
							Game.FireEvent("MapRealmMouseoverChanged", province3.OwnerRealm);
						}
						break;
					}
				case MapModes.StackMove:
					{
						if (Game.CurrentGame == null)
						{
							Renderer.ClearHighlight();
							break;
						}
						WorkingProvince province2 = Game.CurrentGame.GetProvince(RegionName);
						WorkingZone zone2 = Game.CurrentGame.GetZone(RegionName);
						if (province2 != null)
						{
							Renderer.SetHighlight(RegionName);
							Renderer.SetProvinceHighlight(RegionName);
							Game.FireEvent("MapProvinceMouseoverChanged", province2);
						}
						else if (zone2 != null)
						{
							Renderer.SetHighlight(RegionName);
							Renderer.SetProvinceHighlight(RegionName);
							Game.FireEvent("MapZoneMouseoverChanged", zone2);
						}
						else
						{
							Renderer.ClearHighlight();
							Sovereignty game2 = Game;
							object[] args2 = new object[1];
							game2.FireEvent("MapProvinceMouseoverChanged", args2);
						}
						break;
					}
				default:
					{
						if (Game.CurrentGame == null)
						{
							Renderer.ClearHighlight();
							break;
						}
						WorkingProvince province = Game.CurrentGame.GetProvince(RegionName);
						WorkingZone zone = Game.CurrentGame.GetZone(RegionName);
						if (province != null)
						{
							Renderer.SetHighlight(RegionName);
							Renderer.SetProvinceHighlight(RegionName);
							Game.FireEvent("MapProvinceMouseoverChanged", province);
						}
						else if (zone != null)
						{
							Renderer.SetHighlight(RegionName);
							Renderer.SetProvinceHighlight(RegionName);
							Game.FireEvent("MapZoneMouseoverChanged", zone);
						}
						else
						{
							Renderer.ClearHighlight();
							Sovereignty game = Game;
							object[] args = new object[1];
							game.FireEvent("MapProvinceMouseoverChanged", args);
						}
						break;
					}
			}
			Game.FireEvent("MouseoverRegionChanged", RegionName);
		}

		internal void HandleMouseDown(MouseButtonEventArgs e)
		{
			InputManager.HandleMouseDown(e);
		}

		internal void ForceMouseUp(MouseButton Button)
		{
			InputManager.ForceMouseUp(Button);
		}

		internal void HandleMouseUp(MouseButtonEventArgs e)
		{
			InputManager.HandleMouseUp(e);
		}

		internal void HandleProvinceClicked(string ProvinceName, MouseButton Button)
		{
			if (this.OnProvinceClicked != null)
			{
				this.OnProvinceClicked(ProvinceName);
			}
			if (Game.UIActionAllowed("SelectProvince"))
			{
				Game.FireEvent("MapProvinceClicked", ProvinceName);
			}
			if (CurrentMode == MapModes.Default && Button == MouseButton.Right)
			{
				if (ActiveStack != null)
				{
					ActiveStack.HideSelected();
				}
				Sovereignty game = Game;
				object[] args = new object[1];
				game.FireEvent("StackSelectionChanged", args);
			}
			if (CurrentMode == MapModes.CastProvince)
			{
				if (Button == MouseButton.Right)
				{
					Game.CurrentGame.CancelCasting();
					return;
				}
				WorkingProvince province = Game.CurrentGame.GetProvince(ProvinceName);
				if (province != null)
				{
					Game.CurrentGame.SelectSpellTarget(province);
				}
			}
			if (CurrentMode == MapModes.CastRealm)
			{
				if (Button == MouseButton.Right)
				{
					Game.CurrentGame.CancelCasting();
					return;
				}
				WorkingProvince province2 = Game.CurrentGame.GetProvince(ProvinceName);
				if (province2 != null)
				{
					Game.CurrentGame.SelectSpellTarget(province2.OwnerRealm);
				}
			}
			if (CurrentMode == MapModes.CastZone)
			{
				if (Button == MouseButton.Right)
				{
					Game.CurrentGame.CancelCasting();
					return;
				}
				WorkingZone zone = Game.CurrentGame.GetZone(ProvinceName);
				if (zone != null)
				{
					Game.CurrentGame.SelectSpellTarget(zone);
				}
			}
			if (CurrentMode == MapModes.Relations && Button == MouseButton.Left)
			{
				WorkingProvince province3 = Game.CurrentGame.GetProvince(ProvinceName);
				if (province3 == null)
				{
					return;
				}
				WorkingRealm ownerRealm = province3.OwnerRealm;
				if (ownerRealm != Game.CurrentGame.RebelRealm)
				{
					ActiveRelationsRealm = ownerRealm;
					Game.FireEvent("MapLegendChanged");
					RefreshMode();
				}
			}
			if ((CurrentMode == MapModes.Political || CurrentMode == MapModes.Economy || CurrentMode == MapModes.Relations) && Button == MouseButton.Right)
			{
				ChangeMode(MapModes.Default);
			}
			if (CurrentMode == MapModes.StackMove)
			{
				bool flag = false;
				bool flag2 = false;
				if (Game.Settings.GetBooleanSetting("SwapActionButtons"))
				{
					if (Button == MouseButton.Left)
					{
						flag2 = true;
					}
					if (Button == MouseButton.Right)
					{
						flag = true;
					}
				}
				else
				{
					if (Button == MouseButton.Left)
					{
						flag = true;
					}
					if (Button == MouseButton.Right)
					{
						flag2 = true;
					}
				}
				if (flag && NearestNode != null)
				{
					Game.CurrentGame.PlayerMoveManager.RequestMoveToNode(ActiveStack, NearestNode);
					ChangeMode(MapModes.Default);
				}
				if (flag2)
				{
					ChangeMode(MapModes.Default);
				}
			}
			if (CurrentMode == MapModes.DeployUnit && Button == MouseButton.Left && NearestNode != null)
			{
				AttemptDeployUnit(NearestNode);
			}
			if (CurrentMode == MapModes.DeployHero && Button == MouseButton.Left && NearestNode != null)
			{
				AttemptDeployHero(NearestNode);
			}
		}

		public void AttemptDeployUnit(ActivePathNode Node)
		{
			bool flag = false;
			if (CurrentDeployList != null)
			{
				if (CurrentDeployList.Count > 20)
				{
					flag = false;
				}
				else
				{
					flag = true;
					foreach (UnitQueueItem currentDeploy in CurrentDeployList)
					{
						if (Node.CurrentStack != null && Node.CurrentStack.Units.Count + CurrentDeployList.Count > 20)
						{
							flag = false;
						}
						if (Game.CurrentGame.DestinationChecker.NodeOKToDeploy(currentDeploy.Unit, Game.CurrentGame.PlayerRealm, Node) != UnitMoveResult.OK)
						{
							flag = false;
						}
					}
				}
			}
			else
			{
				flag = Game.CurrentGame.DestinationChecker.NodeOKToDeploy(CurrentDeployItem.Unit, Game.CurrentGame.PlayerRealm, Node) == UnitMoveResult.OK;
			}
			if (!flag)
			{
				return;
			}
			if (CurrentDeployList == null)
			{
				Game.CurrentGame.DeployUnit(CurrentDeployItem.Unit, Node);
				Game.CurrentGame.PlayerRealm.EndUnitTraining(CurrentDeployItem);
			}
			else
			{
				foreach (UnitQueueItem currentDeploy2 in CurrentDeployList)
				{
					Game.CurrentGame.DeployUnit(currentDeploy2.Unit, Node);
					Game.CurrentGame.PlayerRealm.EndUnitTraining(currentDeploy2);
				}
			}
			ChangeMode(MapModes.Default);
		}

		public void AttemptDeployHero(ActivePathNode Node)
		{
			if (Game.CurrentGame.DestinationChecker.CanDeployHero(Game.CurrentGame.PlayerRealm, Node))
			{
				Game.CurrentGame.DeployHero(CurrentDeployHero, Node);
				PreventModeChanges = false;
				ChangeMode(MapModes.Default);
			}
		}

		internal void HandleKeyDown(KeyboardKeyEventArgs e)
		{
			if (e.Key == Key.Up)
			{
				Game.Camera.Forward = true;
			}
			if (e.Key == Key.Down)
			{
				Game.Camera.Back = true;
			}
			if (e.Key == Key.Left)
			{
				Game.Camera.Left = true;
			}
			if (e.Key == Key.Right)
			{
				Game.Camera.Right = true;
			}
		}

		internal void HandleKeyUp(KeyboardKeyEventArgs e)
		{
			if (e.Key == Key.Up)
			{
				Game.Camera.Forward = false;
			}
			if (e.Key == Key.Down)
			{
				Game.Camera.Back = false;
			}
			if (e.Key == Key.Left)
			{
				Game.Camera.Left = false;
			}
			if (e.Key == Key.Right)
			{
				Game.Camera.Right = false;
			}
		}

		internal void HandleWheelDown(MouseWheelEventArgs e)
		{
			Game.Camera.Up = true;
		}

		internal void HandleWheelUp(MouseWheelEventArgs e)
		{
			Game.Camera.Down = true;
		}

		internal void GameStarted()
		{
			Game.CurrentGame.TurnController.OnStartTurn += TurnController_OnStartTurn;
		}

		private void Game_OnScriptEvent(string EventName, params object[] Args)
		{
			if (EventName == "HighlightRealm")
			{
				Renderer.AddRealmHighlight((string)Args[0]);
			}
			if (EventName == "HighlightProvince")
			{
				Renderer.AddProvinceHighlight((string)Args[0], new Vector4(0f, 0f, 1f, 0f));
			}
			if (EventName == "ClearHighlights")
			{
				Renderer.ClearHighlights();
			}
		}

		internal void SetAcivePlayer(WorkingRealm Realm)
		{
			TurnController_OnStartTurn(Realm);
		}

		private void TurnController_OnStartTurn(WorkingRealm Realm)
		{
			if (Game.CurrentGame == null)
			{
				return;
			}
			foreach (WorkingRealm value in Game.CurrentGame.AllRealms.Values)
			{
				if (Realm == value)
				{
					Renderer.SetBorderColour(value.Name, new Vector4(1f, 1f, 1f, 1f));
				}
				else
				{
					Renderer.SetBorderColour(value.Name, new Vector4(0.4f, 0.22f, 0.35f, 1f));
				}
			}
		}

		public void RefreshMode()
		{
			switch (CurrentMode)
			{
				case MapModes.Relations:
					{
						foreach (SpriteButton activeButton in ActiveButtons)
						{
							activeButton.Dispose();
						}
						ActiveButtons.Clear();
						Color allyColour = Color.FromArgb(255, 0, 0);
						Color allyColour2 = Color.FromArgb(51, 255, 255);
						Color allyColour3 = Color.FromArgb(102, 56, 89);
						Color color = Color.FromArgb(200, 196, 16, 16);
						Color color2 = Color.FromArgb(200, 214, 106, 0);
						Color color3 = Color.FromArgb(200, 218, 163, 0);
						Color color4 = Color.FromArgb(200, 153, 153, 102);
						Color color5 = Color.FromArgb(200, 194, 194, 102);
						Color color6 = Color.FromArgb(200, 153, 194, 102);
						Color color7 = Color.FromArgb(200, 102, 194, 153);
						foreach (WorkingRealm value in Game.CurrentGame.AllRealms.Values)
						{
							if (value.RealmIsDead)
							{
								continue;
							}
							if (value != Game.CurrentGame.PlayerRealm && value.CapitolProvince != null)
							{
								CreateRelationButtons(value);
								value.CapitolProvince.SetRelationsMode(State: true);
							}
							if (value == ActiveRelationsRealm)
							{
								Renderer.SetLiveRealmColour(value.Name, Color.Transparent);
								Renderer.SetLiveBorder(value.Name, allyColour3, -7f);
								continue;
							}
							float disposition = value.DiplomacyManager.GetDisposition(ActiveRelationsRealm);
							Color transparent = Color.Transparent;
							transparent = ((disposition <= -36f) ? color : ((disposition <= -21f) ? color2 : ((disposition <= -5f) ? color3 : ((disposition <= 5f) ? color4 : ((disposition <= 20f) ? color5 : ((!(disposition <= 35f)) ? color7 : color6))))));
							Renderer.SetLiveRealmColour(value.Name, transparent);
							switch (value.DiplomacyManager.GetRelation(ActiveRelationsRealm))
							{
								case RelationStates.Alliance:
									Renderer.SetLiveBorder(value.Name, allyColour2, -20f);
									break;
								case RelationStates.War:
									Renderer.SetLiveBorder(value.Name, allyColour, -20f);
									break;
								default:
									Renderer.SetLiveBorder(value.Name, allyColour3, -7f);
									break;
							}
						}
						Renderer.BordersOnTop = true;
						break;
					}
				case MapModes.Economy:
					{
						foreach (WorkingProvince value2 in Game.CurrentGame.AllProvinces.Values)
						{
							if (value2.OwnerRealm != Game.CurrentGame.PlayerRealm)
							{
								Color colour6 = Color.FromArgb(128, 0, 0, 0);
								if (value2.PlagueTurns > 0)
								{
									colour6 = Color.FromArgb(200, 29, 47, 0);
								}
								Renderer.SetProvinceColour(value2.Name, colour6);
								continue;
							}
							Color colour7 = Color.FromArgb(0, 0, 0, 0);
							if (value2.PlagueTurns > 0)
							{
								colour7 = Color.FromArgb(200, 29, 47, 0);
							}
							else if (value2.EconomyDamaged || value2.BonusEconomy < 0)
							{
								colour7 = Color.FromArgb(200, 200, 0, 0);
							}
							else if (value2.BonusEconomy > 0)
							{
								colour7 = Color.FromArgb(200, 100, 100, 255);
							}
							Renderer.SetProvinceColour(value2.Name, colour7);
						}
						break;
					}
				case MapModes.StackMove:
					{
						Color colour = Color.FromArgb(200, 0, 0, 0);
						Color colour2 = Color.FromArgb(50, 0, 200, 0);
						Color colour3 = Color.FromArgb(50, 100, 100, 200);
						Color colour4 = Color.FromArgb(50, 200, 200, 0);
						Color colour5 = Color.FromArgb(50, 200, 0, 0);
						foreach (WorkingProvince value3 in Game.CurrentGame.AllProvinces.Values)
						{
							value3.SetMoveMode(State: true);
							bool flag = Game.CurrentGame.DestinationChecker.NodeOkForStack(ActiveStack, value3.LandNode) == UnitMoveResult.OK;
							if (!flag && value3.HarbourNode != null)
							{
								flag = Game.CurrentGame.DestinationChecker.NodeOkForStack(ActiveStack, value3.HarbourNode) == UnitMoveResult.OK;
							}
							RelationStates relation = value3.OccupierRealm.DiplomacyManager.GetRelation(ActiveStack.Owner);
							if (Game.PreventSelfMoves && value3.OccupierRealm == ActiveStack.Owner)
							{
								flag = false;
							}
							if (Game.PreventEnemyMoves && relation == RelationStates.War)
							{
								flag = false;
							}
							if (flag)
							{
								if (value3.OccupierRealm == ActiveStack.Owner)
								{
									Renderer.SetProvinceColour(value3.Name, colour2);
								}
								else if (relation == RelationStates.Alliance && !value3.Occupied)
								{
									Renderer.SetProvinceColour(value3.Name, colour3);
								}
								else if (relation == RelationStates.War)
								{
									Renderer.SetProvinceColour(value3.Name, colour5);
								}
								else
								{
									Renderer.SetProvinceColour(value3.Name, colour4);
								}
							}
							else
							{
								Renderer.SetProvinceColour(value3.Name, colour);
							}
						}
						{
							foreach (WorkingZone value4 in Game.CurrentGame.AllZones.Values)
							{
								bool flag2 = false;
								foreach (ActivePathNode node in value4.Nodes)
								{
									if (Game.CurrentGame.DestinationChecker.NodeOkForStack(ActiveStack, node) == UnitMoveResult.OK)
									{
										flag2 = true;
										break;
									}
								}
								if (flag2)
								{
									Renderer.SetZoneColour(value4.Name, colour2);
								}
								else
								{
									Renderer.SetZoneColour(value4.Name, colour);
								}
							}
							break;
						}
					}
				case MapModes.DeployUnit:
					break;
			}
		}

		private void CreateRelationButtons(WorkingRealm Realm)
		{
			if (Realm.CapitolProvince != null)
			{
				switch (Realm.DiplomacyManager.GetRelation(Game.CurrentGame.PlayerRealm))
				{
					case RelationStates.War:
						{
							SpriteButton spriteButton9 = new SpriteButton(Game, "Data\\Images\\HUD\\Info\\button_offerpeace", Realm.CapitolProvince.CapitolCoords, Realm, GameText.CreateLocalised("RELATIONBTN_PEACE"));
							spriteButton9.OnClick += PeaceButton_OnClick;
							ActiveButtons.Add(spriteButton9);
							break;
						}
					case RelationStates.Alliance:
						{
							SpriteButton spriteButton7 = new SpriteButton(Game, "Data\\Images\\HUD\\Info\\button_trade", ModifyCoords(Realm.CapitolProvince.CapitolCoords, -20, -20), Realm, GameText.CreateLocalised("RELATIONBTN_TRADE"));
							SpriteButton spriteButton8 = new SpriteButton(Game, "Data\\Images\\HUD\\Info\\button_severalliance", ModifyCoords(Realm.CapitolProvince.CapitolCoords, 20, 20), Realm, GameText.CreateLocalised("RELATIONBTN_SEVER"));
							spriteButton7.OnClick += TradeButton_OnClick;
							spriteButton8.OnClick += BreakButton_OnClick;
							ActiveButtons.Add(spriteButton7);
							ActiveButtons.Add(spriteButton8);
							break;
						}
					case RelationStates.Peace:
						{
							SpriteButton spriteButton4 = new SpriteButton(Game, "Data\\Images\\HUD\\Info\\button_declarewar", ModifyCoords(Realm.CapitolProvince.CapitolCoords, -20, 20), Realm, GameText.CreateLocalised("RELATIONBTN_WAR"));
							SpriteButton spriteButton5 = new SpriteButton(Game, "Data\\Images\\HUD\\Info\\button_trade", ModifyCoords(Realm.CapitolProvince.CapitolCoords, -20, -20), Realm, GameText.CreateLocalised("RELATIONBTN_TRADE"));
							SpriteButton spriteButton6 = new SpriteButton(Game, "Data\\Images\\HUD\\Info\\button_offeralliance", ModifyCoords(Realm.CapitolProvince.CapitolCoords, 20, -20), Realm, GameText.CreateLocalised("RELATIONBTN_TREATY"));
							spriteButton4.OnClick += WarButton_OnClick;
							spriteButton5.OnClick += TradeButton_OnClick;
							spriteButton6.OnClick += TreatyButton_OnClick;
							ActiveButtons.Add(spriteButton4);
							ActiveButtons.Add(spriteButton5);
							ActiveButtons.Add(spriteButton6);
							break;
						}
					default:
						{
							SpriteButton spriteButton = new SpriteButton(Game, "Data\\Images\\HUD\\Info\\button_trade", ModifyCoords(Realm.CapitolProvince.CapitolCoords, -20, -20), Realm, GameText.CreateLocalised("RELATIONBTN_TRADE"));
							SpriteButton spriteButton2 = new SpriteButton(Game, "Data\\Images\\HUD\\Info\\button_severalliance", ModifyCoords(Realm.CapitolProvince.CapitolCoords, 20, 20), Realm, GameText.CreateLocalised("RELATIONBTN_SEVER"));
							SpriteButton spriteButton3 = new SpriteButton(Game, "Data\\Images\\HUD\\Info\\button_offeralliance", ModifyCoords(Realm.CapitolProvince.CapitolCoords, 20, -20), Realm, GameText.CreateLocalised("RELATIONBTN_TREATY"));
							spriteButton.OnClick += TradeButton_OnClick;
							spriteButton2.OnClick += BreakButton_OnClick;
							spriteButton3.OnClick += TreatyButton_OnClick;
							ActiveButtons.Add(spriteButton);
							ActiveButtons.Add(spriteButton2);
							ActiveButtons.Add(spriteButton3);
							break;
						}
					case RelationStates.ForcedPeace:
						break;
				}
			}
		}

		internal void ShowAttackArrow(Path AttackPath)
		{
			if (AttackPath != null)
			{
				Renderer.ShowArrow(AttackPath.GetVertices());
				Renderer.SetArrowColour(Color.Red);
			}
		}

		internal void RemoveAttackArrow()
		{
			Renderer.ClearArrow();
		}

		public Point GetScreenCoord(Vector3 WorldCoord)
		{
			Vector3 vec = Vector3.Transform(WorldCoord, Game.Camera.ViewMatrix);
			vec = Vector3.Transform(vec, Game.Camera.ProjectionMatrix);
			Rectangle viewport = Game.GetViewport();
			vec.X /= vec.Z;
			vec.Y /= vec.Z;
			vec.X = (vec.X + 1f) * (float)viewport.Width / 2f;
			vec.Y = (vec.Y + 1f) * (float)viewport.Height / 2f;
			vec.Y = (float)viewport.Height - vec.Y;
			return new Point((int)vec.X, (int)vec.Y);
		}

		internal void GameEnded()
		{
			PreventModeChanges = false;
		}

		internal void ChangeMap(string MapFolder, int Width, int Height)
		{
			Renderer.MapWidth = Width;
			Renderer.MapHeight = Height;
			Renderer.CreateSelectionOutlines();
			Renderer.LoadRealmTextures(MapFolder);
			Renderer.CreateBuffers();
			Renderer.CreateMeshRenderers();
			SetDefaultNames();
			Renderer.ReadyToRender = true;
		}
	}
}