using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SovereigntyTK.Game.Data
{
	public class ProvinceData : BaseData
	{
		[PrimaryKey(1)]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("provinceid")]
		[EditorData("Region ID", EditorTypes.Text)]
		public int ID { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Name", EditorTypes.Text)]
		[DataName("name")]
		[PrimaryKey(2)]
		public string Name { get; set; }

		[EditorData("Localised Name", EditorTypes.Text)]
		[DataConverter(typeof(TextIndexConverter))]
		[DataName("displayprov")]
		public string DisplayName { get; set; }

		[DataConverter(typeof(ProvinceTypeConverter))]
		[EditorData("Type", EditorTypes.DropDownEnum)]
		[DataName("type")]
		public ProvinceTypes ProvinceType { get; set; }

		[DataName("water")]
		[EditorData("Water Type", EditorTypes.DropDownEnum)]
		[DataConverter(typeof(WaterTypeConverter))]
		public WaterTypes WaterType { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("owner")]
		[EditorData("Initial Owner", EditorTypes.DropDown)]
		[DataBinding("RlmStats", "Name", false)]
		public string Owner { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[DataBinding("RlmStats", "Name", false)]
		[DataName("natural")]
		[EditorData("Natural Owner", EditorTypes.DropDown)]
		public string NaturalOwner { get; set; }

		[EditorData("Natural Owner (alt)", EditorTypes.DropDown)]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("natural_alt")]
		[DataBinding("RlmStats", "Name", true)]
		public string AltOwner { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Terrain", EditorTypes.DropDown)]
		[DataName("terrain")]
		[DataBinding("Terrain", "TerrainName", false)]
		public string Terrain { get; set; }

		[EditorData("Image File", EditorTypes.Text)]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("art")]
		public string ImageFile { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Initial Forts", EditorTypes.Text)]
		[DataName("fort")]
		public int FortLevel { get; set; }

		[EditorData("Landmark Name", EditorTypes.Text)]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("landmark")]
		public string Landmark { get; set; }

		[DataName("value")]
		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Value", EditorTypes.Text)]
		public int Value { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("economy")]
		[EditorData("Initial Economy", EditorTypes.Text)]
		public int EconomyValue { get; set; }

		[DataName("text")]
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Localised Description", EditorTypes.Text)]
		public string Description { get; set; }

		[DataConverter(typeof(CoordConverter))]
		[DataName("fortcoord")]
		[EditorData("Fort Coords", EditorTypes.Point)]
		public Point FortCoords { get; set; }

		[DataName("cptlcoord")]
		[EditorData("Capitol Coords", EditorTypes.Point)]
		[DataConverter(typeof(CoordConverter))]
		public Point CapitolCoords { get; set; }

		[EditorData("Economy Coords", EditorTypes.Point)]
		[DataConverter(typeof(CoordConverter))]
		[DataName("econcoord")]
		public Point EconCoords { get; set; }

		[DataConverter(typeof(CoordConverter))]
		[DataName("ldmkcoord")]
		[EditorData("Landmark Coords", EditorTypes.Point)]
		public Point LandmarkCoords { get; set; }

		[DataName("cradlecoord")]
		[EditorData("Cradle Coords", EditorTypes.Point)]
		[DataConverter(typeof(CoordConverter))]
		public Point CradleCoords { get; set; }

		[DataName("rescoord")]
		[EditorData("Resource Coords", EditorTypes.Point)]
		[DataConverter(typeof(CoordConverter))]
		public Point ResourceCoords { get; set; }

		[DataBinding("Resources", "ResourceName", true)]
		[DataName("resource")]
		[EditorData("Resource", EditorTypes.DropDown)]
		[DataConverter(typeof(GeneralStringConverter))]
		public string m_Resource { get; set; }

		[DataName("xp")]
		[EditorData("Research Points", EditorTypes.Text)]
		[DataConverter(typeof(GeneralIntConverter))]
		public int XP { get; set; }

		public string Resource
		{
			get
			{
				if (this.m_Resource.ToLowerInvariant() == "none")
				{
					return null;
				}
				return this.m_Resource;
			}
		}

		public void SetupLinks(List<PathingNodeData> Nodes)
		{
			PathingNodeData pathingNodeData = Nodes.FirstOrDefault((PathingNodeData x) => x.ProvinceID == this.ID && x.NodeType == PathNodeTypes.Land);
			PathingNodeData pathingNodeData2 = Nodes.FirstOrDefault((PathingNodeData x) => x.ProvinceID == this.ID && (x.NodeType == PathNodeTypes.Harbour || x.NodeType == PathNodeTypes.RiverHarbour));
			this.AdjacentProvinces = new List<ProvinceLink>();
			this.AdjacentZones = new List<ProvinceLink>();
			List<int> list = new List<int>();
			foreach (PathingNodeData pathingNodeData3 in Nodes)
			{
				if (pathingNodeData3.NodeType == PathNodeTypes.Land)
				{
					foreach (NodeConnection nodeConnection in pathingNodeData3.ConnectedNodes)
					{
						if (nodeConnection.Node == pathingNodeData)
						{
							ProvinceLink provinceLink = new ProvinceLink();
							provinceLink.LinkedProvinceID = pathingNodeData3.ProvinceID;
							switch (nodeConnection.ConnectionType)
							{
							case ConnectionTypes.Normal:
								provinceLink.LinkType = ProvinceLinkTypes.Normal;
								break;
							case ConnectionTypes.River:
								provinceLink.LinkType = ProvinceLinkTypes.River;
								break;
							case ConnectionTypes.Bridge:
								provinceLink.LinkType = ProvinceLinkTypes.Bridge;
								break;
							case ConnectionTypes.Road:
								provinceLink.LinkType = ProvinceLinkTypes.Road;
								break;
							case ConnectionTypes.Blocked:
								provinceLink.LinkType = ProvinceLinkTypes.Blocked;
								break;
							case ConnectionTypes.Special:
								provinceLink.LinkType = ProvinceLinkTypes.Normal;
								provinceLink.IgnoreForBorders = true;
								break;
							}
							this.AdjacentProvinces.Add(provinceLink);
						}
					}
				}
				if (pathingNodeData3.NodeType == PathNodeTypes.Sea)
				{
					foreach (NodeConnection nodeConnection2 in pathingNodeData3.ConnectedNodes)
					{
						if ((nodeConnection2.Node == pathingNodeData || nodeConnection2.Node == pathingNodeData2) && !list.Contains(pathingNodeData3.ProvinceID))
						{
							list.Add(pathingNodeData3.ProvinceID);
						}
					}
				}
			}
			foreach (int num in list)
			{
				ProvinceLink provinceLink2 = new ProvinceLink();
				provinceLink2.LinkedProvinceID = num;
				this.AdjacentZones.Add(provinceLink2);
			}
		}

		public bool HasWater
		{
			get
			{
				return this.WaterType != WaterTypes.NoWater;
			}
		}

		public bool HasHarbour
		{
			get
			{
				return this.WaterType == WaterTypes.Harbour;
			}
		}

		public bool IsCapitol
		{
			get
			{
				return this.ProvinceType == ProvinceTypes.Capitol;
			}
		}

		public override string ToString()
		{
			return this.Name;
		}

		public ProvinceOutlineData Outline;

		public List<ProvinceLink> AdjacentProvinces;

		public List<ProvinceLink> AdjacentZones;
	}
}
