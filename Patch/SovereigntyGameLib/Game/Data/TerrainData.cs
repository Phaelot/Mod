using System;

namespace SovereigntyTK.Game.Data
{
	public class TerrainData : BaseData
	{
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Terrain Name")]
		[PrimaryKey(1)]
		[DataName("name")]
		public string TerrainName { get; set; }

		[DataName("terraintype")]
		[EditorData("Base Type")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string BaseType { get; set; }

		[EditorData("Move Cost (Trade)")]
		[DataConverter(typeof(GeneralFloatConverter))]
		[DataName("movecosttrade")]
		public float TradeMoveCost { get; set; }

		[EditorData("Move cost (Unit)")]
		[DataConverter(typeof(GeneralFloatConverter))]
		[DataName("movecostcampaign")]
		public float UnitMoveCost { get; set; }

		[EditorData("Move Cost (Battle)")]
		[DataConverter(typeof(GeneralFloatConverter))]
		[DataName("movecostcombat")]
		public float CombatMoveCost { get; set; }

		[EditorData("Block Battle Movement")]
		[DataName("combatblocking")]
		[DataConverter(typeof(YesNoConverter))]
		public bool CombatBlocking { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("iconname")]
		[EditorData("Icon Name")]
		public string TerrainIconName { get; set; }

		[DataConverter(typeof(GeneralFloatConverter))]
		[EditorData("Human Cost Multiplier")]
		[DataName("humanmult")]
		public float HumanMult { get; set; }

		[DataConverter(typeof(GeneralFloatConverter))]
		[EditorData("Elf Cost Multiplier")]
		[DataName("elfmult")]
		public float ElfMult { get; set; }

		[EditorData("Dwarf Cost Multiplier")]
		[DataConverter(typeof(GeneralFloatConverter))]
		[DataName("dwarfmult")]
		public float DwarfMult { get; set; }

		[DataName("undeadmult")]
		[EditorData("Undead Cost Multiplier")]
		[DataConverter(typeof(GeneralFloatConverter))]
		public float UndeadMult { get; set; }

		[DataName("outcastmult")]
		[EditorData("Outcast Cost Multiplier")]
		[DataConverter(typeof(GeneralFloatConverter))]
		public float OutcastMult { get; set; }

		[DataConverter(typeof(GeneralFloatConverter))]
		[DataName("orcmult")]
		[EditorData("Orc Cost Multiplier")]
		public float OrcMult { get; set; }

		[DataName("plaguestartchance")]
		[EditorData("Plague Start Chance")]
		[DataConverter(typeof(GeneralIntConverter))]
		public int PlagueStartChance { get; set; }

		[DataName("plaguespreadchance")]
		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Placgue Spread Chance")]
		public int PlagueSpreadChance { get; set; }

		[EditorData("Plague Turns")]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("plagueturns")]
		public int PlagueTurns { get; set; }

		[EditorData("Localised Name")]
		[DataConverter(typeof(TextIndexConverter))]
		[DataName("displayname")]
		public string DisplayName { get; set; }

		public static TerrainData NullTerrain
		{
			get
			{
				if (TerrainData.m_NullTerrain == null)
				{
					TerrainData.m_NullTerrain = new TerrainData();
					TerrainData.m_NullTerrain.BaseType = "plains";
				}
				return TerrainData.m_NullTerrain;
			}
		}

		public bool IsAnyType(params string[] Types)
		{
			foreach (string text in Types)
			{
				if (text.ToLowerInvariant() == this.BaseType.ToLowerInvariant())
				{
					return true;
				}
			}
			return false;
		}

		public override string ToString()
		{
			return this.TerrainName;
		}

		public float GetEconomyMultiplier(Races Race)
		{
			switch (Race)
			{
			case Races.Human:
				return this.HumanMult;
			case Races.Elf:
				return this.ElfMult;
			case Races.Dwarf:
				return this.DwarfMult;
			case Races.Undead:
				return this.UndeadMult;
			case Races.Orc:
				return this.OrcMult;
			case Races.Outcast:
				return this.OutcastMult;
			}
			throw new Exception("Unknown economy type: " + Race);
		}

		private static TerrainData m_NullTerrain;
	}
}
