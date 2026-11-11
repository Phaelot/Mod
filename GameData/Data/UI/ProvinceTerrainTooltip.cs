using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Text;
using System.Collections.Generic;

namespace SovereigntyTK.UI
{
    /// <summary>
    /// Province-card terrain tooltip: short flavour + what to expect in battle.
    /// </summary>
    public static class ProvinceTerrainTooltip
    {
        public static List<GameText> BuildTooltip(WorkingProvince Province)
        {
            TerrainData Terrain = Province.Terrain;
            List<GameText> Tip = new List<GameText>();

            Tip.Add(GameText.CreateLocalised(Terrain.DisplayName));
            Tip.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
            Tip.Add(GameText.CreateLocalised(GetDescriptionKey(Terrain)));

            return Tip;
        }

        private static string GetDescriptionKey(TerrainData Terrain)
        {
            if (Terrain.IsAnyType("plains")) return "TT_TERRAIN_DESC_PLAINS";
            if (Terrain.IsAnyType("hills")) return "TT_TERRAIN_DESC_HILLS";
            if (Terrain.IsAnyType("mountain")) return "TT_TERRAIN_DESC_MOUNTAIN";
            if (Terrain.IsAnyType("lt forest")) return "TT_TERRAIN_DESC_LTFOREST";
            if (Terrain.IsAnyType("old forest")) return "TT_TERRAIN_DESC_OLDFOREST";
            if (Terrain.IsAnyType("swamp")) return "TT_TERRAIN_DESC_SWAMP";
            if (Terrain.IsAnyType("wasteland")) return "TT_TERRAIN_DESC_WASTELAND";
            if (Terrain.IsAnyType("sea")) return "TT_TERRAIN_DESC_SEA";
            if (Terrain.IsAnyType("river")) return "TT_TERRAIN_DESC_RIVER";
            return "TT_TERRAIN_DESC_GENERIC";
        }
    }
}
