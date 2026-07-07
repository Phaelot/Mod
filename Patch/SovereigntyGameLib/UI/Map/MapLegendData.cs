using System;
using System.Collections.Generic;
using System.Drawing;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.UI.Map
{
	public class MapLegendData
	{
		private MapLegendData()
		{
			this.MapColours = new List<MapColour>();
		}

		public static MapLegendData GetLegend(MapModes Mode, Sovereignty Game)
		{
			MapLegendData mapLegendData = null;
			switch (Mode)
			{
			case MapModes.Political:
				mapLegendData = new MapLegendData();
				mapLegendData.ModeName = GameText.CreateLocalised("MODETITLE_POLITICAL", new object[0]);
				mapLegendData.ModeDescription = GameText.CreateLocalised("MODEDESC_POLITICAL", new object[0]);
				break;
			case MapModes.Economy:
				mapLegendData = new MapLegendData();
				mapLegendData.ModeName = GameText.CreateLocalised("MODETITLE_ECONOMY", new object[0]);
				mapLegendData.ModeDescription = GameText.CreateLocalised("MODEDESC_ECONOMY", new object[0]);
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(200, 29, 47, 0), GameText.CreateLocalised("MODECOL_ECONOMY1", new object[0])));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(200, 200, 0, 0), GameText.CreateLocalised("MODECOL_ECONOMY2", new object[0])));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(200, 100, 100, 255), GameText.CreateLocalised("MODECOL_ECONOMY3", new object[0])));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(200, 100, 100, 100), GameText.CreateLocalised("MODECOL_ECONOMY4", new object[0])));
				break;
			case MapModes.Plague:
				mapLegendData = new MapLegendData();
				mapLegendData.ModeName = GameText.CreateFromLiteral("Plague Map");
				mapLegendData.ModeDescription = GameText.CreateFromLiteral("Shows active plague, temporary plague immunity, and provinces where plague can still appear.");
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(230, 0, 0, 0), GameText.CreateFromLiteral("Active plague")));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(190, 120, 180, 230), GameText.CreateFromLiteral("Plague immunity")));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(120, 90, 80, 0), GameText.CreateFromLiteral("Susceptible")));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(60, 0, 0, 0), GameText.CreateFromLiteral("Healthy / inactive")));
				break;
			case MapModes.StackMove:
				mapLegendData = new MapLegendData();
				mapLegendData.ModeName = GameText.CreateLocalised("MODETITLE_MOVESTACK", new object[0]);
				mapLegendData.ModeDescription = GameText.CreateLocalised("MODEDESC_MOVESTACK", new object[0]);
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(200, 0, 200, 0), GameText.CreateLocalised("MODECOL_MOVE1", new object[0])));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(200, 100, 100, 200), GameText.CreateLocalised("MODECOL_MOVE2", new object[0])));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(200, 200, 200, 0), GameText.CreateLocalised("MODECOL_MOVE3", new object[0])));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(200, 200, 0, 0), GameText.CreateLocalised("MODECOL_MOVE4", new object[0])));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(200, 0, 0, 0), GameText.CreateLocalised("MODECOL_MOVE5", new object[0])));
				break;
			case MapModes.DeployUnit:
				mapLegendData = new MapLegendData();
				mapLegendData.ModeName = GameText.CreateLocalised("MODETITLE_DEPLOYUNIT", new object[0]);
				mapLegendData.ModeDescription = GameText.CreateLocalised("MODEDESC_DEPLOYUNIT", new object[0]);
				break;
			case MapModes.Relations:
				mapLegendData = new MapLegendData();
				mapLegendData.ModeName = GameText.CreateLocalised("MODETITLE_RELATIONS", new object[0]);
				mapLegendData.ModeDescription = GameText.CreateLocalised("MODEDESC_RELATIONS", new object[0]);
				mapLegendData.ModeDescription.AddChildText(GameText.CreateLocalised(Game.Map.ActiveRelationsRealm.DisplayName, new object[0]));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(200, 255, 0, 0), GameText.CreateLocalised("MODECOL_RELATION2", new object[0])));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(200, 196, 16, 16), GameText.CreateLocalised("DISP_NEUTRAL1", new object[0])));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(200, 214, 106, 0), GameText.CreateLocalised("DISP_NEUTRAL2", new object[0])));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(200, 218, 163, 0), GameText.CreateLocalised("DISP_NEUTRAL3", new object[0])));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(200, 153, 153, 102), GameText.CreateLocalised("DISP_NEUTRAL4", new object[0])));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(200, 194, 194, 102), GameText.CreateLocalised("DISP_NEUTRAL5", new object[0])));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(200, 153, 194, 102), GameText.CreateLocalised("DISP_NEUTRAL6", new object[0])));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(200, 102, 194, 153), GameText.CreateLocalised("DISP_NEUTRAL7", new object[0])));
				mapLegendData.MapColours.Add(new MapColour(Color.FromArgb(200, 51, 255, 255), GameText.CreateLocalised("MODECOL_RELATION1", new object[0])));
				break;
			case MapModes.DeployHero:
				mapLegendData = new MapLegendData();
				mapLegendData.ModeName = GameText.CreateLocalised("MODETITLE_DEPLOYHERO", new object[0]);
				mapLegendData.ModeDescription = GameText.CreateLocalised("MODEDESC_DEPLOYHERO", new object[0]);
				break;
			case MapModes.CastRealm:
				mapLegendData = new MapLegendData();
				mapLegendData.ModeName = GameText.CreateLocalised("MODETITLE_CAST", new object[0]);
				mapLegendData.ModeDescription = GameText.CreateLocalised("MODEDESC_CASTREALM", new object[0]);
				break;
			case MapModes.CastProvince:
				mapLegendData = new MapLegendData();
				mapLegendData.ModeName = GameText.CreateLocalised("MODETITLE_CAST", new object[0]);
				mapLegendData.ModeDescription = GameText.CreateLocalised("MODEDESC_CASTPROVINCE", new object[0]);
				break;
			case MapModes.CastZone:
				mapLegendData = new MapLegendData();
				mapLegendData.ModeName = GameText.CreateLocalised("MODETITLE_CAST", new object[0]);
				mapLegendData.ModeDescription = GameText.CreateLocalised("MODEDESC_CASTZONE", new object[0]);
				break;
			case MapModes.CastStack:
				mapLegendData = new MapLegendData();
				mapLegendData.ModeName = GameText.CreateLocalised("MODETITLE_CAST", new object[0]);
				mapLegendData.ModeDescription = GameText.CreateLocalised("MODEDESC_CASTSTACK", new object[0]);
				break;
			case MapModes.CastUnit:
				mapLegendData = new MapLegendData();
				mapLegendData.ModeName = GameText.CreateLocalised("MODETITLE_CAST", new object[0]);
				mapLegendData.ModeDescription = GameText.CreateLocalised("MODEDESC_CASTUNIT", new object[0]);
				break;
			}
			return mapLegendData;
		}

		public GameText ModeName;

		public GameText ModeDescription;

		public List<MapColour> MapColours;
	}
}
