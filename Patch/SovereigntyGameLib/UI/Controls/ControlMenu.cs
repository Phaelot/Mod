using System;
using System.Linq;
using System.Xml.Linq;

namespace SovereigntyTK.UI.Controls
{
	public class ControlMenu : UIControl
	{
		public event MenuItemDelegate OnItemClicked;

		public int ItemCount
		{
			get
			{
				return this.MenuItems.VisibleItems;
			}
			set
			{
				this.MenuItems.VisibleItems = value;
			}
		}

		public ControlMenu(GameBase Game)
			: base(Game)
		{
			this.MenuBG = new ControlImage(Game);
			this.MenuBG.SetImageFile("Data\\Images\\HUD\\bg.png");
			this.MenuItems = new ControlList<MenuItem>(Game);
			this.MenuItems.ManualUpdate = true;
			this.MenuItems.OnListItemUpdated += this.MenuItems_OnListItemUpdated;
			this.MenuItems.OnItemClicked += this.MenuItems_OnItemClicked;
			this.MenuItems.OnHoverItemChanged += this.MenuItems_OnHoverItemChanged;
			base.AddChild(this.MenuBG);
			base.AddChild(this.MenuItems);
		}

		protected override void ParseElement(XElement Element)
		{
			string localName;
			if ((localName = Element.Name.LocalName) != null && localName == "itemcount")
			{
				this.ItemCount = int.Parse(Element.Value);
				return;
			}
			base.ParseElement(Element);
		}

		private void MenuItems_OnHoverItemChanged(UIControl Control)
		{
			if (this.MenuItems.HoverItem == null)
			{
				return;
			}
			MenuItem data = this.MenuItems.HoverItem.Data;
			foreach (ListItem<MenuItem> listItem in this.MenuItems.Items)
			{
				if (listItem.Data.SubMenu != null)
				{
					base.RemoveChild(listItem.Data.SubMenu);
					listItem.Data.SubMenu.Visible = false;
				}
			}
			if (data != null && data.SubMenu != null)
			{
				base.AddChild(data.SubMenu);
				data.SubMenu.Visible = true;
				data.SubMenu.SetPosition(this.MenuItems.HoverItem.Container.GetOriginalX() + this.MenuItems.HoverItem.Container.getOriginalWidth(), this.MenuItems.HoverItem.Container.GetOriginalY(), UIUnits.Pixel);
			}
		}

		private void MenuItems_OnItemClicked(UIControl Control)
		{
			this.MenuItems.ClickedItem.Data.HandleClicked();
		}

		private void MenuItems_OnListItemUpdated(ListItem<MenuItem> Item)
		{
			Item.Data.Update(Item.Container);
		}

		protected override void ReclaculateBounds()
		{
			base.ReclaculateBounds();
			if (this.MenuBG != null)
			{
				this.MenuBG.SetBounds(0f, 0f, base.getOriginalWidth(), base.GetOriginalHeight(), UIUnits.Pixel);
				this.MenuItems.SetBounds(0f, 0f, base.getOriginalWidth(), base.GetOriginalHeight(), UIUnits.Pixel);
			}
		}

		public void AddItem(MenuItem Item)
		{
			this.MenuItems.AddItem(Item);
		}

		public void RemoveItem(MenuItem Item)
		{
			Item.Dispose();
			this.MenuItems.RemoveItem(Item);
		}

		public void ClearItems()
		{
			foreach (ListItem<MenuItem> listItem in this.MenuItems.Items)
			{
				listItem.Data.Dispose();
			}
			this.MenuItems.ClearItems();
			foreach (UIControl uicontrol in (from x in base.GetChildren()
				where x is ControlMenu
				select x).ToList<UIControl>())
			{
				base.RemoveChild(uicontrol);
			}
		}

		public override void Dispose()
		{
			base.RemoveChild(this.MenuItems);
			base.RemoveChild(this.MenuBG);
			this.ClearItems();
			this.MenuItems.Dispose();
			this.MenuBG.Dispose();
			base.Dispose();
		}

		private ControlList<MenuItem> MenuItems;

		private ControlImage MenuBG;
	}
}
