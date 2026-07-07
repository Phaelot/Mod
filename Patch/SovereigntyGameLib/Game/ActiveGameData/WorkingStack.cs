using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using OpenTK;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Map;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class WorkingStack
	{
		public event UnitNodeDelegate OnNodeChanged;

		public bool Disposed
		{
			get
			{
				return this.m_Disposed;
			}
		}

		public int OwnerID
		{
			get
			{
				return this.m_OwnerID;
			}
			set
			{
				if (this.m_OwnerID == value)
				{
					return;
				}
				if (this.m_OwnerID > 0 && this.Game != null)
				{
					this.Game.AllRealms[this.m_OwnerID].StacksChanged();
				}
				this.m_OwnerID = value;
				if (this.m_OwnerID > 0 && this.Game != null)
				{
					this.Game.AllRealms[this.m_OwnerID].StacksChanged();
				}
			}
		}

		public int NodeID
		{
			get
			{
				return this.m_NodeID;
			}
			set
			{
				if (this.m_NodeID != value)
				{
					this.m_NodeID = value;
					if (this.OnNodeChanged != null)
					{
						this.OnNodeChanged(this.ID, this.m_NodeID);
					}
					this.RefreshFogOfWarIfProvidingScoutVision();
				}
			}
		}

		public IList<WorkingUnit> Units
		{
			get
			{
				if (this.m_Units == null)
				{
					this.m_Units = this.Game.AllUnits.Values.Where((WorkingUnit x) => x.OwnerStackID == this.ID).ToList<WorkingUnit>();
				}
				return this.m_Units.AsReadOnly();
			}
		}

		public ActivePathNode Node
		{
			get
			{
				ActivePathNode activePathNode = null;
				this.Game.AllNodes.TryGetValue(this.NodeID, out activePathNode);
				return activePathNode;
			}
		}

		public WorkingRealm Owner
		{
			get
			{
				WorkingRealm workingRealm = null;
				this.Game.AllRealms.TryGetValue(this.OwnerID, out workingRealm);
				return workingRealm;
			}
		}

		public WorkingHero Hero
		{
			get
			{
				WorkingHero workingHero = null;
				this.Game.AllHeroes.TryGetValue(this.HeroID, out workingHero);
				return workingHero;
			}
		}

		public WorkingStack(int ID, int OwnerID, SovereigntyGame Game)
		{
			this.ID = ID;
			this.OwnerID = OwnerID;
			this.Game = Game;
			this.NodeID = -1;
			this.HeroID = -1;
			this.SlidingCounters = new List<SlidingCounter>();
			this.ActiveModifiers = new List<StackStatModifierData>();
		}

		public WorkingStack(SovereigntyGame Game, BinaryReader r, int SaveVersion)
		{
			this.Game = Game;
			this.ID = r.ReadInt32();
			this.OwnerID = r.ReadInt32();
			this.m_NodeID = r.ReadInt32();
			this.HeroID = r.ReadInt32();
			if (SaveVersion < 28)
			{
				int num = r.ReadInt32();
				for (int i = 0; i < num; i++)
				{
					r.ReadInt32();
				}
			}
			this.SlidingCounters = new List<SlidingCounter>();
			this.ActiveModifiers = new List<StackStatModifierData>();
		}

		public void UnitsChanged()
		{
			this.m_Units = null;
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.ID);
			w.Write(this.OwnerID);
			w.Write(this.m_NodeID);
			w.Write(this.HeroID);
		}

		public void SetOwner(int ID)
		{
			this.OwnerID = ID;
			foreach (WorkingUnit workingUnit in this.Units)
			{
				workingUnit.OwnerRealmID = ID;
			}
			this.UpdateSprite();
		}

		public void UnpackTransports()
		{
			foreach (WorkingUnit workingUnit in this.Units.ToList<WorkingUnit>())
			{
				if (workingUnit.Class == UnitClasses.Naval && workingUnit.CarriedUnit != null)
				{
					WorkingUnit carriedUnit = workingUnit.CarriedUnit;
					carriedUnit.Move(100f);
					workingUnit.CarriedUnitID = -1;
					this.RemoveUnit(workingUnit);
					this.Game.DestroyUnit(workingUnit);
					this.AddUnit(carriedUnit, false, false);
				}
			}
		}

		public void AddUnit(WorkingUnit Unit, bool IgnorePackChecks = false, bool IgnoreSizeLimit = false)
		{
			if (!IgnoreSizeLimit && Unit.Class != UnitClasses.Fort)
			{
				if (this.Units.Count((WorkingUnit x) => x.Class != UnitClasses.Fort) >= 20)
				{
					throw new Exception("Adding more than 20 units to a stack");
				}
			}
			Unit.OwnerStackID = this.ID;
			this.SpriteUpdateNeeded = true;
			if (!IgnorePackChecks)
			{
				if ((this.Node.NodeType == PathNodeTypes.Harbour || this.Node.NodeType == PathNodeTypes.Sea || this.Node.NodeType == PathNodeTypes.RiverHarbour) && Unit.Class != UnitClasses.Naval && Unit.Class != UnitClasses.Fort)
				{
					UnitData unitData = this.Owner.UnitPurchaseManager.GetUnitsInClass(UnitClasses.Naval).FirstOrDefault((UnitData x) => x.AllowTransport);
					WorkingUnit workingUnit = this.Game.CreateUnit(this.OwnerID, unitData);
					workingUnit.CarriedUnitID = Unit.ID;
					workingUnit.Move(100f);
					workingUnit.Upkeep = Unit.Upkeep;
					this.RemoveUnit(Unit);
					this.AddUnit(workingUnit, false, false);
					return;
				}
				if (this.Node.NodeType == PathNodeTypes.Land)
				{
					if (Unit.Class == UnitClasses.Naval && Unit.CarriedUnit != null)
					{
						WorkingUnit carriedUnit = Unit.CarriedUnit;
						carriedUnit.Move(100f);
						Unit.CarriedUnitID = -1;
						this.RemoveUnit(Unit);
						this.Game.DestroyUnit(Unit);
						this.AddUnit(carriedUnit, false, false);
						return;
					}
					if (Unit.Class == UnitClasses.Naval && Unit.CarriedUnit == null)
					{
						throw new Exception("Moved non-transport naval unit to a land node.");
					}
				}
			}
			List<WorkingProvince> list = null;
			List<WorkingZone> list2 = new List<WorkingZone>();
			if (this.Node.Province != null)
			{
				list = this.Game.PathManager.GetAreaProvinces(this.Node.Province, 1);
				foreach (string text in this.Node.Province.AdjacentZones)
				{
					list2.Add(this.Game.GetZone(text));
				}
			}
			if (this.Node.Zone != null)
			{
				list = this.Game.PathManager.GetAreaProvinces(this.Node.Zone, 1);
				foreach (GameRegion gameRegion in this.Node.Zone.GetAllConnectedRegions())
				{
					if (!(gameRegion is WorkingProvince))
					{
						list2.Add(gameRegion as WorkingZone);
					}
				}
				foreach (ActivePathNode activePathNode in this.Node.Zone.Nodes)
				{
					if (activePathNode.CurrentStack != null)
					{
						activePathNode.CurrentStack.SpriteUpdateNeeded = true;
					}
				}
			}
			foreach (WorkingZone workingZone in list2)
			{
				foreach (ActivePathNode activePathNode2 in workingZone.Nodes)
				{
					if (activePathNode2.CurrentStack != null)
					{
						activePathNode2.CurrentStack.SpriteUpdateNeeded = true;
					}
				}
			}
			foreach (WorkingProvince workingProvince in list)
			{
				if (workingProvince.LandNode.CurrentStack != null)
				{
					workingProvince.LandNode.CurrentStack.SpriteUpdateNeeded = true;
				}
				foreach (int num in workingProvince.LandNode.AllyStacks)
				{
					this.Game.AllStacks[num].SpriteUpdateNeeded = true;
				}
				if (workingProvince.HarbourNode != null && workingProvince.HarbourNode.CurrentStack != null)
				{
					workingProvince.HarbourNode.CurrentStack.SpriteUpdateNeeded = true;
				}
			}
		}

		public void RemoveUnit(WorkingUnit Unit)
		{
			SlidingCounter slidingCounter = this.SlidingCounters.FirstOrDefault((SlidingCounter x) => x.Units.Contains(Unit.ID));
			if (slidingCounter != null)
			{
				slidingCounter.RemoveUnit(Unit);
				if (slidingCounter.Units.Count == 0)
				{
					slidingCounter.Dispose();
					this.SlidingCounters.Remove(slidingCounter);
				}
			}
			Unit.OwnerStackID = -1;
			this.UpdateSprite();
		}

		public void TransferFromStack(WorkingUnit Unit, Path MovePath, bool IgnorePackChecks = false)
		{
			string text = this.Owner.CounterFilename + this.GetCounterTypeString() + ".png";
			WorkingStack ownerStack = Unit.OwnerStack;
			List<Point> list = null;
			if (MovePath != null && MovePath.PathPoints != null && MovePath.PathPoints.Count > 1)
			{
				try
				{
					list = MovePath.GetPointList();
				}
				catch
				{
					list = null;
				}
			}
			if (list == null || list.Count < 2)
			{
				list = new List<Point>();
				if (ownerStack != null && ownerStack.Node != null)
				{
					list.Add(ownerStack.Node.MapCoords);
				}
				if (this.Node != null)
				{
					list.Add(this.Node.MapCoords);
				}
			}
			float num;
			if (this.Owner.AIPlayer != null)
			{
				num = this.Game.GetStackSpeed(this.Game.GameCore.Settings.GetIntSetting("CampaignSpeedHuman"));
			}
			else
			{
				num = this.Game.GetStackSpeed(this.Game.GameCore.Settings.GetIntSetting("CampaignSpeedAI"));
			}
			if (list.Count >= 2)
			{
				SlidingCounter slidingCounter = new SlidingCounter(this.Game, Unit, list, text, num);
				if (!slidingCounter.Finished)
				{
					this.SlidingCounters.Add(slidingCounter);
				}
				else
				{
					slidingCounter.Dispose();
				}
			}
			if (ownerStack != null)
			{
				ownerStack.RemoveUnit(Unit);
			}
			else
			{
				Unit.OwnerStackID = -1;
			}
			this.AddUnit(Unit, IgnorePackChecks, false);
		}

		public void Update(float ElapsedTime)
		{
			if (this.m_Disposed)
			{
				return;
			}
			if (this.SpriteUpdateNeeded)
			{
				this.SpriteUpdateNeeded = false;
				this.UpdateSprite();
			}
			if (this.HighlightSprite != null)
			{
				this.AlphaTime += ElapsedTime * 5f;
				if ((double)this.AlphaTime > 6.283185307179586)
				{
					this.AlphaTime -= 6.2831855f;
				}
				float num = (float)Math.Sin((double)this.AlphaTime);
				if (num < 0f)
				{
					num = 0f;
				}
				this.HighlightSprite.SetAlpha(num);
			}
			if (this.SlidingCounters.Count == 0)
			{
				return;
			}
			foreach (SlidingCounter slidingCounter in this.SlidingCounters.ToList<SlidingCounter>())
			{
				slidingCounter.Update(ElapsedTime);
				if (slidingCounter.Finished)
				{
					slidingCounter.Dispose();
					this.SlidingCounters.Remove(slidingCounter);
					this.UpdateSprite();
				}
			}
		}

		public void ShowHighlight()
		{
			this.HideHighlight();
			this.HighlightSprite = this.Game.GameCore.Utilities.SpriteManager.CreateSprite("Data\\Images\\Units\\stack_highlight.png", false);
			this.HighlightSprite.SetSize(72f, 72f);
			this.HighlightSprite.SetPosition((float)this.Node.MapCoords.X, (float)this.Node.MapCoords.Y);
			this.AlphaTime = 0f;
		}

		public void HideHighlight()
		{
			if (this.HighlightSprite != null)
			{
				this.HighlightSprite.Dispose(false);
				this.HighlightSprite = null;
			}
		}

		public void ShowSelected()
		{
			this.HideSelected();
			this.SelectedSprite = this.Game.GameCore.Utilities.SpriteManager.CreateSprite("Data\\Images\\Units\\stackselect.png", false);
			this.SelectedSprite.SetSize(72f, 72f);
			this.SelectedSprite.BringToFront();
			this.SelectedSprite.SetPosition((float)this.Node.MapCoords.X, (float)this.Node.MapCoords.Y);
		}

		public void HideSelected()
		{
			if (this.SelectedSprite != null)
			{
				this.SelectedSprite.Dispose(false);
				this.SelectedSprite = null;
			}
		}


		private static bool UnitProvidesScoutLikeVision(WorkingUnit unit)
		{
			return unit != null && (unit.HasAnyNamedFlags("Scout", "Reconnaissance", "Recon", "Reconaissance") || unit.HasStatus("Scout", new object[0]) || unit.HasStatus("Reconnaissance", new object[0]) || unit.HasStatus("Recon", new object[0]));
		}

		private bool HasScoutLikeUnit()
		{
			return this.Units.Any((WorkingUnit x) => UnitProvidesScoutLikeVision(x));
		}

		private void RefreshFogOfWarIfProvidingScoutVision()
		{
			if (this.Game == null || this.Game.PlayerRealm == null || this.Game.GameCore == null || this.Game.GameCore.Map == null)
			{
				return;
			}
			if (this.Owner != this.Game.PlayerRealm)
			{
				return;
			}
			if (!this.HasScoutLikeUnit())
			{
				return;
			}
			this.Game.GameCore.Map.RefreshMode();
		}

		private bool IsVisibleOnCampaignMap()
		{
			if (this.Owner == this.Game.PlayerRealm)
			{
				return true;
			}
			if (this.Node == null || this.Game == null || this.Game.GameCore == null || this.Game.GameCore.Map == null)
			{
				return true;
			}
			return this.Game.GameCore.Map.IsRegionVisible(this.Node.GetRegion());
		}

		public void UpdateSprite()
		{
			if (this.m_Disposed)
			{
				return;
			}
			this.ClearSprite();
			if (this.Node == null)
			{
				return;
			}
			if (this.Owner == null)
			{
				return;
			}
			if (!this.IsVisibleOnCampaignMap())
			{
				this.HideHighlight();
				this.HideSelected();
				return;
			}
			string text = this.Owner.CounterFilename + this.GetCounterTypeString() + ".png";
			float num = this.GetCounterSize(this.Units.Count - this.SlidingCounters.Count);
			if (this.Hero != null)
			{
				num *= 1.25f;
			}
			this.CounterSprite = this.Game.GameCore.Utilities.SpriteManager.CreateIndexedSprite("Data\\Images\\Map\\mapicons.png", text, true);
			if (!this.ScoutActive())
			{
				this.CounterSprite.SetAlpha(0.4f);
			}
			this.CounterSprite.SetSize(num, num);
			Point stackCoords = this.Node.GetStackCoords(this.ID);
			this.CounterSprite.SetPosition((float)stackCoords.X, (float)stackCoords.Y);
			if (this.HighlightSprite != null)
			{
				this.HighlightSprite.SetPosition((float)stackCoords.X, (float)stackCoords.Y);
			}
			this.CounterSprite.OnMouseEnter += this.CounterSprite_OnMouseEnter;
			this.CounterSprite.OnMouseLeave += this.CounterSprite_OnMouseLeave;
			this.CounterSprite.OnClick += this.CounterSprite_OnClick;
			this.CounterSprite.OnRightClick += this.CounterSprite_OnRightClick;
			this.UpdateAttritionSprite(stackCoords, num);
		}

		public bool ScoutActive()
		{
			if (this.ForceScout)
			{
				return true;
			}
			if (this.Game.PlayerRealm == null)
			{
				return false;
			}
			if (this.Owner == this.Game.PlayerRealm)
			{
				return true;
			}
			if (this.Node == null || this.Game.GameCore == null || this.Game.GameCore.Map == null)
			{
				return false;
			}
			if (!this.Game.GameCore.Map.SimpleFogOfWarEnabled)
			{
				return true;
			}
			if (!this.Game.GameCore.Map.IsRegionVisible(this.Node.GetRegion()))
			{
				return false;
			}
			if (this.Node.Province != null && this.Node.Province.OwnerRealm == this.Game.PlayerRealm)
			{
				return true;
			}
			if (this.Node.Province != null && this.Node.Province.OwnerRealm != null && this.Node.Province.OwnerRealm.DiplomacyManager.GetRelation(this.Game.PlayerRealm) == RelationStates.Alliance)
			{
				return true;
			}
			if (this.Node.Province != null && this.Node.Province.HasStatus("ScoutingBlocked"))
			{
				return false;
			}
			if (this.Game.PlayerRealm.HasStatus("MagicScout", new object[] { this.Owner }))
			{
				return true;
			}
			if (this.Node.Province != null && this.Game.PlayerRealm.HasStatus("SpecialScout", new object[] { this.Node.Province }))
			{
				return true;
			}
			List<WorkingProvince> list = null;
			if (this.Node.Province != null)
			{
				list = this.Game.PathManager.GetAreaProvinces(this.Node.Province, 1);
			}
			if (this.Node.Zone != null)
			{
				list = this.Game.PathManager.GetAreaProvinces(this.Node.Zone, 1);
			}
			if (list == null)
			{
				return false;
			}
			foreach (WorkingProvince workingProvince in list)
			{
				if (workingProvince != this.Node.Province && workingProvince.LandNode != null && workingProvince.LandNode.CurrentStack != null && workingProvince.LandNode.CurrentStack.Owner == this.Game.PlayerRealm)
				{
					if (workingProvince.LandNode.CurrentStack.HasScoutLikeUnit())
					{
						return true;
					}
				}
			}
			return false;
		}

		private void CounterSprite_OnRightClick(GLBaseSprite Sprite)
		{
			if (this.Game.TurnController.CurrentRealm != this.Game.PlayerRealm)
			{
				return;
			}
			MapModes currentMode = this.Game.GameCore.Map.CurrentMode;
			if (currentMode == MapModes.Political || currentMode == MapModes.Economy || currentMode == MapModes.Relations || currentMode == MapModes.StackMove)
			{
				this.Game.GameCore.Map.ChangeMode(MapModes.Default, false);
			}
			if (currentMode == MapModes.CastProvince || currentMode == MapModes.CastRealm || currentMode == MapModes.CastStack || currentMode == MapModes.CastUnit || currentMode == MapModes.CastZone)
			{
				this.Game.CancelCasting();
			}
		}

		private void CounterSprite_OnClick(GLBaseSprite Sprite)
		{
			if (this.Game.TurnController.CurrentRealm != this.Game.PlayerRealm)
			{
				return;
			}
			if (this.Game.GameCore.Map.CurrentMode == MapModes.CastUnit)
			{
				if (this.Owner == this.Game.PlayerRealm || this.ScoutActive())
				{
					this.Game.SelectSpellTarget(this);
				}
				return;
			}
			if (this.Game.GameCore.Map.CurrentMode == MapModes.CastStack)
			{
				this.Game.SelectSpellTarget(this);
				return;
			}
			if (this.Game.GameCore.Map.CurrentMode == MapModes.DeployHero)
			{
				this.Game.GameCore.Map.AttemptDeployHero(this.Node);
				return;
			}
			if (this.Game.GameCore.Map.CurrentMode == MapModes.DeployUnit)
			{
				this.Game.GameCore.Map.AttemptDeployUnit(this.Node);
				return;
			}
			if (this.Game.GameCore.Map.CurrentMode == MapModes.StackMove)
			{
				this.Game.PlayerMoveManager.RequestMoveToNode(this.Game.GameCore.Map.ActiveStack, this.Node, true);
				this.Game.GameCore.Map.ChangeMode(MapModes.Default, false);
				return;
			}
			if (!this.Game.GameCore.UIActionAllowed("SelectStack"))
			{
				return;
			}
			if (this.Owner == this.Game.PlayerRealm || this.ScoutActive())
			{
				foreach (WorkingUnit workingUnit in this.Units)
				{
					workingUnit.Selected = true;
				}
				if (this.Hero != null)
				{
					this.Hero.Selected = true;
				}
				if (this.Game.GameCore.Map.CurrentMode == MapModes.StackMove)
				{
					this.Game.GameCore.Map.ChangeMode(MapModes.Default, false);
				}
				if (this.Game.GameCore.Map.ActiveStack != null)
				{
					this.Game.GameCore.Map.ActiveStack.HideSelected();
				}
				this.Game.GameCore.Map.ActiveStack = this;
				this.ShowSelected();
				if (this.Owner == this.Game.PlayerRealm)
				{
					this.Game.GameCore.Map.ChangeMode(MapModes.StackMove, false);
				}
				this.Game.GameCore.FireEvent("StackSelectionChanged", new object[] { this });
			}
		}

		public void SelectStack()
		{
			if (this.Game.GameCore.Map.CurrentMode == MapModes.StackMove)
			{
				this.Game.GameCore.Map.ChangeMode(MapModes.Default, false);
			}
			if (this.Game.GameCore.Map.CurrentMode != MapModes.Default)
			{
				return;
			}
			this.CounterSprite_OnClick(null);
		}

		private void CounterSprite_OnMouseLeave(GLBaseSprite Sprite)
		{
			this.Game.GameCore.Window.Cursor = MouseCursor.Default;
		}

		private void CounterSprite_OnMouseEnter(GLBaseSprite Sprite)
		{
			if (!this.ScoutActive())
			{
				return;
			}
			Bitmap bitmap = new Bitmap(this.Game.GameCore.Utilities.FileSystem.OpenFile("Data\\Images\\HUD\\Pointers\\pointer_hand.png", FileTypes.Application, FileModes.ReadOnly, true));
			BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			this.Game.GameCore.Window.Cursor = new MouseCursor(8, 1, bitmap.Width, bitmap.Height, bitmapData.Scan0);
			bitmap.UnlockBits(bitmapData);
			bitmap.Dispose();
		}

		private float GetCounterSize(int UnitCount)
		{
			float num = 56f;
			float num2 = 24f;
			float num3 = num - num2;
			float num4 = num3 / 20f * (float)UnitCount + num2;
			if (UnitCount == 0)
			{
				return 0f;
			}
			return num4;
		}

		private string GetCounterTypeString()
		{
			if (this.Hero == null)
			{
				return "N";
			}
			if (this.Hero.Legendary)
			{
				return "L";
			}
			return "H";
		}

		private void RecordTrace()
		{
		}


		private void UpdateAttritionSprite(Point stackCoords, float counterSize)
		{
			if (this.AttritionSprite != null)
			{
				this.AttritionSprite.Dispose(false);
				this.AttritionSprite = null;
			}
			if (!this.HadAttritionThisTurn || string.IsNullOrEmpty(this.AttritionTooltipText))
			{
				return;
			}
			this.AttritionSprite = this.Game.GameCore.Utilities.SpriteManager.CreateSprite("Data\\Images\\HUD\\Info\\attrition_skull.png", true);
			if (!this.ScoutActive())
			{
				this.AttritionSprite.SetAlpha(0.4f);
			}
			this.AttritionSprite.SetSize(16f, 16f);
			this.AttritionSprite.SetPosition((float)stackCoords.X + counterSize * 0.3f, (float)stackCoords.Y - counterSize * 0.3f);
			this.AttritionSprite.Tooltip = GameText.CreateFromLiteral(this.AttritionTooltipText);
			this.AttritionSprite.OnClick += this.CounterSprite_OnClick;
			this.AttritionSprite.OnRightClick += this.CounterSprite_OnRightClick;
			this.AttritionSprite.OnMouseEnter += this.CounterSprite_OnMouseEnter;
			this.AttritionSprite.OnMouseLeave += this.CounterSprite_OnMouseLeave;
		}

		public void ClearAttritionMarker()
		{
			this.HadAttritionThisTurn = false;
			this.LastAttritionDamage = 0;
			this.AttritionTooltipText = null;
		}

		public void Dispose()
		{
			this.m_Disposed = true;
			this.RecordTrace();
			this.ClearSprite();
			this.HideHighlight();
			foreach (SlidingCounter slidingCounter in this.SlidingCounters.ToList<SlidingCounter>())
			{
				slidingCounter.Dispose();
				this.SlidingCounters.Remove(slidingCounter);
			}
			foreach (WorkingUnit workingUnit in this.Units)
			{
				this.Game.DestroyUnit(workingUnit);
			}
			this.OnNodeChanged = null;
		}

		private void ClearSprite()
		{
			if (this.CounterSprite != null)
			{
				this.CounterSprite.Dispose(false);
				this.CounterSprite = null;
			}
			if (this.AttritionSprite != null)
			{
				this.AttritionSprite.Dispose(false);
				this.AttritionSprite = null;
			}
		}

		internal bool CanAffordMove(float p)
		{
			return true;
		}

		public void TransferHeroFromStack(WorkingStack Stack, WorkingHero Hero)
		{
			Stack.RemoveHero();
			Stack.UpdateSprite();
			this.AddHero(Hero);
			Hero.Move(100f);
			this.UpdateSprite();
		}

		internal bool HasMovingUnits()
		{
			return this.SlidingCounters.Count > 0;
		}

		internal void ClearDeadUnits()
		{
			foreach (WorkingUnit workingUnit in this.Units)
			{
				if (workingUnit.Disabled)
				{
					this.Game.DestroyUnit(workingUnit);
				}
			}
			this.UpdateSprite();
		}

		internal void AddHero(WorkingHero Hero)
		{
			this.HeroID = Hero.ID;
			Hero.OwnerStackID = this.ID;
			Hero.ApplyModifiers(this);
			this.UpdateSprite();
		}

		internal void PackUnit(WorkingUnit UnitA, WorkingUnit Transport)
		{
			Transport.CarriedUnitID = UnitA.ID;
			this.RemoveUnit(UnitA);
			this.AddUnit(Transport, false, true);
		}

		internal WorkingUnit UnpackUnit(WorkingUnit UnitA)
		{
			WorkingUnit carriedUnit = UnitA.CarriedUnit;
			UnitA.CarriedUnitID = -1;
			this.RemoveUnit(UnitA);
			this.AddUnit(carriedUnit, false, true);
			return carriedUnit;
		}

		internal List<Point> GetBattlePoints()
		{
			List<Point> list = new List<Point>();
			foreach (WorkingUnit workingUnit in this.Units)
			{
				if (!workingUnit.Disabled && workingUnit.BattleData.BattleX != -1)
				{
					list.Add(workingUnit.BattleData.BattleLocation);
				}
			}
			return list;
		}

		public void AwardHeroXP(int XP)
		{
			if (this.Hero != null)
			{
				this.Hero.XP += XP;
			}
		}

		public void RemoveModifier(string Name)
		{
			foreach (StackStatModifierData stackStatModifierData in this.ActiveModifiers.ToList<StackStatModifierData>())
			{
				if (stackStatModifierData.Name == Name)
				{
					this.ActiveModifiers.Remove(stackStatModifierData);
				}
			}
		}

		public void ApplyModifier(string Name, bool AllowStacking, UnitStatNames StatName, int Value)
		{
			if (!AllowStacking)
			{
				if (this.ActiveModifiers.Count((StackStatModifierData x) => x.Name == Name) > 0)
				{
					return;
				}
			}
			StackStatModifierData stackStatModifierData = new StackStatModifierData();
			stackStatModifierData.Name = Name;
			stackStatModifierData.Stack = AllowStacking;
			stackStatModifierData.StatName = StatName;
			stackStatModifierData.Value = Value;
			this.ActiveModifiers.Add(stackStatModifierData);
		}

		public int GetStackModifier(UnitStatNames StatName)
		{
			return this.ActiveModifiers.Sum(delegate(StackStatModifierData x)
			{
				if (x.StatName != StatName)
				{
					return 0;
				}
				return x.Value;
			});
		}

		internal void RemoveHero()
		{
			if (this.Hero != null)
			{
				this.Hero.RemoveModifiers(this);
				this.Hero.OwnerStackID = -1;
			}
			this.HeroID = -1;
			this.UpdateSprite();
		}

		public int ID;

		public SovereigntyGame Game;

		private int m_OwnerID;

		private int m_NodeID;

		public int HeroID;

		private List<SlidingCounter> SlidingCounters;

		private GLSprite CounterSprite;

		private GLSprite HighlightSprite;

		private GLSprite SelectedSprite;

		private float AlphaTime;

		public bool ForceScout;

		private bool m_Disposed;

		public string DisposeStackTrace;

		private bool SpriteUpdateNeeded;

		public List<StackStatModifierData> ActiveModifiers;

		private List<WorkingUnit> m_Units;

		private GLSprite AttritionSprite;

		public bool HadAttritionThisTurn;

		public int LastAttritionDamage;

		public string AttritionTooltipText;
	}
}
