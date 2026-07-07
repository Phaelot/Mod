using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;

namespace SovereigntyTK.UI.Controls
{
	public class ControlList<T> : UIControl
	{
		public IList<ListItem<T>> Items
		{
			get
			{
				return this.m_Items.AsReadOnly();
			}
		}

		public event ListItemDelegate<T> OnListItemUpdated;

		public event ControlDelegate OnItemClicked;

		public event ControlDelegate OnHoverItemChanged;

		public event ControlDelegate OnItemRightClicked;

		public event ControlDelegate OnItemDoubleClicked;

		public int VisibleItems
		{
			get
			{
				return this.m_VisibleItems;
			}
			set
			{
				if (value < 0)
				{
					value = 0;
				}
				this.m_VisibleItems = value;
				this.RecalculateItemSizes();
			}
		}

		public ControlList(GameBase Game)
			: base(Game)
		{
			if (this.m_Items == null)
			{
				this.m_Items = new List<ListItem<T>>();
			}
			this.AcceptMouseWheel = true;
			this.MouseInputType = MouseInputTypes.Forced;
			this.ScrollUpButton = new ControlButton(Game);
			base.AddChild(this.ScrollUpButton);
			this.ScrollUpButton.SetBounds(0f, 0f, 20f, 20f, UIUnits.Pixel);
			this.ScrollUpButton.SetAnchor(AnchorPoints.TopRight);
			this.ScrollUpButton.SetImageFiles("Data\\Images\\HUD\\Economy\\arrow_up_normal.png", "Data\\Images\\HUD\\Economy\\arrow_up_mouseover.png", "Data\\Images\\HUD\\Economy\\arrow_up_pressed.png");
			this.ScrollUpButton.Visible = false;
			this.ScrollUpButton.AutoClick = true;
			this.ScrollUpButton.OnClick += this.ScrollUpButton_OnClick;
			this.ScrollDownButton = new ControlButton(Game);
			base.AddChild(this.ScrollDownButton);
			this.ScrollDownButton.SetBounds(0f, 0f, 20f, 20f, UIUnits.Pixel);
			this.ScrollDownButton.SetAnchor(AnchorPoints.BottomRight);
			this.ScrollDownButton.SetImageFiles("Data\\Images\\HUD\\Economy\\arrow_down_normal.png", "Data\\Images\\HUD\\Economy\\arrow_down_mouseover.png", "Data\\Images\\HUD\\Economy\\arrow_down_pressed.png");
			this.ScrollDownButton.Visible = false;
			this.ScrollDownButton.AutoClick = true;
			this.ScrollDownButton.OnClick += this.ScrollDownButton_OnClick;
			this.ScrollBarImage = new ControlImage(Game);
			base.AddChild(this.ScrollBarImage);
			this.ScrollBarImage.SetImageFile("Data\\Images\\HUD\\Economy\\arrow_line_v.png");
			this.ScrollBarImage.Visible = false;
			this.ScrollCurrentImage = new ControlImage(Game);
			base.AddChild(this.ScrollCurrentImage);
			this.ScrollCurrentImage.SetImageFile("Data\\Images\\HUD\\Realmselect\\check_normal.png");
			this.ScrollCurrentImage.SetSize(10f, 10f, UIUnits.Pixel);
			this.HighlightImage = new ControlImage(Game);
			base.AddChild(this.HighlightImage);
			this.HighlightImage.SetImageFile("Data\\Images\\HUD\\ListHighlight.png");
			this.HighlightImage.Visible = false;
			this.HighlightImage.MouseInputType = MouseInputTypes.None;
			this.SelectionImage = new ControlImage(Game);
			base.AddChild(this.SelectionImage);
			this.SelectionImage.SetImageFile("Data\\Images\\HUD\\ListSelect.png");
			this.SelectionImage.Visible = false;
			this.SelectionImage.MouseInputType = MouseInputTypes.None;
		}

		private void ScrollDownButton_OnClick(UIControl Control)
		{
			this.ScrollDown();
		}

		private void ScrollUpButton_OnClick(UIControl Control)
		{
			this.ScrollUp();
		}

		public override void HandleMousewheelUp(float LocalX, float LocalY)
		{
			base.HandleMousewheelUp(LocalX, LocalY);
			this.ScrollUp();
		}

		public override void HandleMousewheelDown(float LocalX, float LocalY)
		{
			base.HandleMousewheelDown(LocalX, LocalY);
			this.ScrollDown();
		}

		public override void Dispose()
		{
			this.ClearItems();
			this.OnListItemUpdated = null;
			this.OnItemClicked = null;
			this.OnHoverItemChanged = null;
			this.OnItemRightClicked = null;
			this.OnItemDoubleClicked = null;
			base.Dispose();
		}

		public void AddItem(T Data)
		{
			ListItem<T> listItem = new ListItem<T>(this);
			listItem.Data = Data;
			listItem.RecreateContainer(this, this.m_ItemWidth, this.m_ItemHeight);
			listItem.SetFontDetails(this.FontName, this.FontSize, this.FontColour);
			if (!this.ManualUpdate)
			{
				listItem.UpdateData(listItem.Data, true);
			}
			else if (this.OnListItemUpdated != null)
			{
				this.OnListItemUpdated(listItem);
			}
			listItem.Container.OnMouseEnter += this.Container_OnMouseEnter;
			listItem.Container.OnMouseLeave += this.Container_OnMouseLeave;
			listItem.Container.OnClick += this.Container_OnClick;
			listItem.Container.OnRightClick += this.Container_OnRightClick;
			this.m_Items.Add(listItem);
			if (this.m_Items.Count == this.m_VisibleItems + 1)
			{
				this.RecalculateItemSizes();
				return;
			}
			this.UpdateVisibleItems();
		}

		private void Container_OnRightClick(UIControl Control)
		{
			this.RightClickedItem = this.m_Items.SingleOrDefault((ListItem<T> x) => x.Container == Control);
			this.ClickedItem = null;
			if (this.OnItemRightClicked != null)
			{
				this.OnItemRightClicked(this);
			}
		}

		private void Container_OnClick(UIControl Control)
		{
			this.SelectItem(Control);
			if (this.OnItemClicked != null)
			{
				this.OnItemClicked(this);
			}
		}

		private void SelectItem(UIControl Control)
		{
			this.ClickedItem = this.m_Items.SingleOrDefault((ListItem<T> x) => x.Container == Control);
			this.RightClickedItem = null;
			this.SelectedIndex = this.m_Items.IndexOf(this.ClickedItem);
			if (this.SelectionEnabled)
			{
				this.SelectionImage.Visible = true;
				this.SelectionImage.SetBounds(Control.GetOriginalX(), Control.GetOriginalY(), Control.Sprite.Bounds.Width, Control.Sprite.Bounds.Height, UIUnits.PixelScaled);
				this.SelectionImage.BringToFront();
			}
		}

		private void Container_OnMouseLeave(UIControl Control)
		{
			this.ShowHoverHighlight(null);
		}

		private void ShowHoverHighlight(ListItem<T> Item)
		{
			this.HoverItem = Item;
			if (!this.HighlightEnabled)
			{
				Item = null;
			}
			if (Item != null)
			{
				this.HighlightImage.SetBounds(Item.Container.GetOriginalX(), Item.Container.GetOriginalY(), Item.Container.Sprite.Bounds.Width, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
				this.HighlightImage.BringToFront();
				this.HighlightImage.Visible = true;
			}
			else
			{
				this.HighlightImage.Visible = false;
			}
			if (this.OnHoverItemChanged != null)
			{
				this.OnHoverItemChanged(this);
			}
		}

		private void Container_OnMouseEnter(UIControl Control)
		{
			this.ShowHoverHighlight(this.m_Items.SingleOrDefault((ListItem<T> x) => x.Container == Control));
		}

		public void RemoveItem(T Data)
		{
			int num = -1;
			if (this.ClickedItem != null)
			{
				num = this.IndexOf(this.ClickedItem.Data);
			}
			int num2 = this.IndexOf(Data);
			if (num2 == -1)
			{
				return;
			}
			this.m_Items[num2].Dispose();
			this.m_Items.RemoveAt(num2);
			if (num == num2 && this.m_Items.Count > 0)
			{
				if (num > 0)
				{
					num--;
				}
				this.SelectItem(this.m_Items[num].Container);
			}
			if (this.m_Items.Count == 0)
			{
				this.ClickedItem = null;
				this.SelectionImage.Visible = false;
			}
			this.ShowHoverHighlight(null);
			this.UpdateVisibleItems();
		}

		public int IndexOf(T Data)
		{
			int num = 0;
			foreach (ListItem<T> listItem in this.m_Items)
			{
				if (listItem.Data.Equals(Data))
				{
					return num;
				}
				num++;
			}
			return -1;
		}

		private int GetMaxTopItem()
		{
			int num = this.m_Items.Count - this.m_VisibleItems;
			return Math.Max(num, 0);
		}

		public void ScrollUp()
		{
			if (this.TopItem > 0)
			{
				this.TopItem--;
				this.UpdateVisibleItems();
				if (this.HoverItem != null)
				{
					int num = this.IndexOf(this.HoverItem.Data);
					num--;
					if (num < 0)
					{
						this.ShowHoverHighlight(null);
						return;
					}
					this.ShowHoverHighlight(this.m_Items[num]);
				}
			}
		}

		public void ScrollDown()
		{
			int maxTopItem = this.GetMaxTopItem();
			if (this.TopItem < maxTopItem)
			{
				this.TopItem++;
				this.UpdateVisibleItems();
				if (this.HoverItem != null)
				{
					int num = this.IndexOf(this.HoverItem.Data);
					num++;
					if (num >= this.m_Items.Count)
					{
						this.ShowHoverHighlight(null);
						return;
					}
					this.ShowHoverHighlight(this.Items[num]);
				}
			}
		}

		public void ScrollToBottom()
		{
			int maxTopItem = this.GetMaxTopItem();
			if (this.TopItem < maxTopItem)
			{
				this.TopItem = maxTopItem;
				this.UpdateVisibleItems();
				if (this.HoverItem != null)
				{
					int num = this.IndexOf(this.HoverItem.Data);
					num++;
					if (num >= this.m_Items.Count)
					{
						this.ShowHoverHighlight(null);
						return;
					}
					this.ShowHoverHighlight(this.Items[num]);
				}
			}
		}

		private void UpdateVisibleItems()
		{
			int maxTopItem = this.GetMaxTopItem();
			if (this.TopItem > maxTopItem)
			{
				this.TopItem = maxTopItem;
			}
			if (this.ScrollBarImage != null)
			{
				this.ScrollBarImage.Visible = maxTopItem > 0;
				this.ScrollUpButton.Visible = maxTopItem > 0;
				this.ScrollDownButton.Visible = maxTopItem > 0;
				this.ScrollCurrentImage.Visible = maxTopItem > 0;
				if (maxTopItem > 0)
				{
					float num = (float)this.TopItem / (float)maxTopItem;
					float height = this.ScrollCurrentImage.Sprite.Bounds.Height;
					float num2 = this.ScrollBarImage.Sprite.Bounds.Height - height;
					float num3 = (num2 - height) * num + height;
					num3 -= this.ScrollBarImage.Sprite.Bounds.Height / 2f;
					this.ScrollCurrentImage.SetPosition(5f, num3, UIUnits.PixelScaled);
					this.ScrollCurrentImage.SetAnchor(AnchorPoints.Right);
				}
			}
			foreach (ListItem<T> listItem in this.m_Items)
			{
				listItem.Container.Visible = false;
			}
			int num4 = 0;
			int num5 = this.TopItem;
			while (num5 < this.TopItem + this.m_VisibleItems && num5 < this.m_Items.Count)
			{
				this.m_Items[num5].Container.SetPositionY((float)num4, UIUnits.PixelScaled);
				this.m_Items[num5].Container.Visible = true;
				num4 += this.m_ItemHeight;
				num5++;
			}
			if (this.ClickedItem != null && this.SelectionEnabled)
			{
				UIControl container = this.ClickedItem.Container;
				this.SelectionImage.SetBounds(container.GetOriginalX(), container.GetOriginalY(), container.Sprite.Bounds.Width, container.Sprite.Bounds.Height, UIUnits.PixelScaled);
				this.SelectionImage.Visible = container.Visible;
				return;
			}
			if (this.SelectionImage != null)
			{
				this.SelectionImage.Visible = false;
			}
		}

		protected override void ParseElement(XElement Element)
		{
			string localName;
			switch (localName = Element.Name.LocalName)
			{
			case "itemcount":
				this.VisibleItems = int.Parse(Element.Value);
				return;
			case "fontname":
				this.FontName = Element.Value;
				return;
			case "fontsize":
				this.SetFontSize(Element.Value);
				return;
			case "fontcolour":
				this.SetFontColour(base.ParseColour(Element.Value));
				return;
			case "showhighlight":
				bool.TryParse(Element.Value, out this.HighlightEnabled);
				return;
			case "onitemclicked":
				this.OnItemClicked += this.GetEventHandler(Element.Value);
				return;
			case "onhoveritemchanged":
				this.OnHoverItemChanged += this.GetEventHandler(Element.Value);
				return;
			case "onitemdoubleclicked":
				this.OnItemDoubleClicked += this.GetEventHandler(Element.Value);
				return;
			case "onitemrightclicked":
				this.OnItemRightClicked += this.GetEventHandler(Element.Value);
				return;
			}
			base.ParseElement(Element);
		}

		public void SetVisibleItems(int Count)
		{
			this.VisibleItems = Count;
		}

		public void SetFontName(string FontName)
		{
			this.FontName = FontName;
			this.UpdateFontDetails();
		}

		public void SetFontSize(string Value)
		{
			this.FontSize = new PositionData(Value, false);
			this.UpdateFontDetails();
		}

		public void SetFontSize(float Value, UIUnits UnitType)
		{
			this.FontSize = new PositionData(Value, UnitType, false);
			this.UpdateFontDetails();
		}

		public void SetFontColour(Color Colour)
		{
			this.FontColour = Colour;
			this.UpdateFontDetails();
		}

		private void UpdateFontDetails()
		{
			foreach (ListItem<T> listItem in this.m_Items)
			{
				listItem.SetFontDetails(this.FontName, this.FontSize, this.FontColour);
			}
		}

		protected override void ReclaculateBounds()
		{
			RectangleF bounds = this.Sprite.Bounds;
			base.ReclaculateBounds();
			if (bounds.Size == this.Sprite.Bounds.Size)
			{
				return;
			}
			this.RecalculateItemSizes();
		}

		private void RecalculateItemSizes()
		{
			this.m_ItemWidth = (int)this.Sprite.Bounds.Width;
			this.m_ItemHeight = (int)(this.Sprite.Bounds.Height / (float)this.m_VisibleItems);
			if (this.ScrollBarImage != null && this.m_Items.Count > this.m_VisibleItems)
			{
				this.m_ItemWidth -= (int)this.ScrollUpButton.Sprite.Bounds.Width;
			}
			if (this.m_Items == null)
			{
				this.m_Items = new List<ListItem<T>>();
			}
			this.SetFontSize((float)this.m_ItemHeight * 0.8f, UIUnits.PixelScaled);
			foreach (ListItem<T> listItem in this.m_Items)
			{
				listItem.RecreateContainer(this, this.m_ItemWidth, this.m_ItemHeight);
				listItem.Container.OnMouseEnter += this.Container_OnMouseEnter;
				listItem.Container.OnMouseLeave += this.Container_OnMouseLeave;
				listItem.Container.OnClick += this.Container_OnClick;
				listItem.Container.OnRightClick += this.Container_OnRightClick;
				if (!this.ManualUpdate)
				{
					listItem.UpdateData(listItem.Data, true);
				}
				else if (this.OnListItemUpdated != null)
				{
					this.OnListItemUpdated(listItem);
				}
			}
			if (this.ScrollBarImage != null)
			{
				this.ScrollBarImage.SetPositionX(7.5f, UIUnits.Pixel);
				this.ScrollBarImage.SetWidth(5f, UIUnits.Pixel);
				this.ScrollBarImage.SetHeight(this.Sprite.Bounds.Height - this.ScrollUpButton.Sprite.Bounds.Height, UIUnits.PixelScaled);
				this.ScrollBarImage.SetAnchor(AnchorPoints.Right);
			}
			this.UpdateVisibleItems();
		}

		public void ClearItems()
		{
			foreach (ListItem<T> listItem in this.m_Items)
			{
				listItem.Dispose();
			}
			this.m_Items.Clear();
			this.HoverItem = null;
			this.ClickedItem = null;
			this.HighlightImage.Visible = false;
			this.SelectionImage.Visible = false;
			this.UpdateVisibleItems();
		}

		public void ClearSelection()
		{
			this.SelectedIndex = -1;
			this.ClickedItem = null;
			this.UpdateVisibleItems();
		}

		public void SetSelectedIndex(int Index)
		{
			if (Index < 0)
			{
				Index = 0;
			}
			if (Index >= this.m_Items.Count)
			{
				Index = this.m_Items.Count - 1;
			}
			if (this.m_Items.Count == 0)
			{
				return;
			}
			this.SelectedIndex = Index;
			this.ClickedItem = this.m_Items[Index];
			this.UpdateVisibleItems();
		}

		public void StorePosition()
		{
			this.StoredTopItem = this.TopItem;
		}

		public void RestorePosition()
		{
			this.TopItem = this.StoredTopItem;
			this.UpdateVisibleItems();
		}

		private List<ListItem<T>> m_Items;

		public bool ManualUpdate;

		public bool HighlightEnabled = true;

		public bool SelectionEnabled = true;

		private int m_VisibleItems = 5;

		private int m_ItemHeight;

		private int m_ItemWidth;

		private int TopItem;

		private string FontName;

		private PositionData FontSize;

		private Color FontColour;

		private ControlButton ScrollUpButton;

		private ControlButton ScrollDownButton;

		private ControlImage ScrollBarImage;

		private ControlImage ScrollCurrentImage;

		private ControlImage HighlightImage;

		private ControlImage SelectionImage;

		private int SelectedIndex = -1;

		private int HighlightIndex;

		public ListItem<T> ClickedItem;

		public ListItem<T> HoverItem;

		public ListItem<T> RightClickedItem;

		public bool ClickOnDeletion;

		private int StoredTopItem;
	}
}
