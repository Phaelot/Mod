using System;
using System.Collections.Generic;

namespace SovereigntyTK.Game.Data
{
	public class AITraitData : BaseData
	{
		[DataName("realm")]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataBinding("RlmStats", "Name", false)]
		[EditorData("Realm", EditorTypes.DropDown)]
		[PrimaryKey(1)]
		public string RealmName { get; set; }

		[DataName("militarist")]
		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Militarist", EditorTypes.Text)]
		public int Militarist { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Warmonger", EditorTypes.Text)]
		[DataName("warmonger")]
		public int Warmonger { get; set; }

		[EditorData("Economist", EditorTypes.Text)]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("economist")]
		public int Economist { get; set; }

		[DataName("trader")]
		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Trader", EditorTypes.Text)]
		public int Trader { get; set; }

		[EditorData("Diplomat", EditorTypes.Text)]
		[DataName("diplomat")]
		[DataConverter(typeof(GeneralIntConverter))]
		public int Diplomat { get; set; }

		[EditorData("Agent", EditorTypes.Text)]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("agent")]
		public int Agent { get; set; }

		[EditorData("Mage", EditorTypes.Text)]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("mage")]
		public int Mage { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Opportunist", EditorTypes.Text)]
		[DataName("opportunist")]
		public int Opportunist { get; set; }

		public Dictionary<AITraits, int> CreateDictionary()
		{
			return new Dictionary<AITraits, int>
			{
				{
					AITraits.Militarist,
					this.Militarist
				},
				{
					AITraits.Warmonger,
					this.Warmonger
				},
				{
					AITraits.Economist,
					this.Economist
				},
				{
					AITraits.Trader,
					this.Trader
				},
				{
					AITraits.Diplomat,
					this.Diplomat
				},
				{
					AITraits.Agent,
					this.Agent
				},
				{
					AITraits.Mage,
					this.Mage
				},
				{
					AITraits.Opportunist,
					this.Opportunist
				}
			};
		}

		public override string ToString()
		{
			return this.RealmName;
		}
	}
}
