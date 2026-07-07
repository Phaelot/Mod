using System;
using System.Collections.Generic;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK
{
	public class CategorisedListItem
	{
		public CategorisedListItem(bool Header, GameText Text)
		{
			this.Header = Header;
			this.Text = Text;
			this.Children = new List<CategorisedListItem>();
		}

		public bool Header;

		public GameText Text;

		public string ImageFile;

		public List<CategorisedListItem> Children;

		public int IndentLevel;

		public bool Closed;

		public int CategoryID;

		public WorkingRealm Realm;

		public bool HasValue;

		public int Value;

		public bool IncludeRemoveButton;

		public bool IncludeNumericButtons;

		public object Data;
	}
}
