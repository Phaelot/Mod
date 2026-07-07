using System;
using System.Runtime.InteropServices;

namespace SovereigntyTK.Game.Data
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct MapTileData
	{
		public DeployZone Deployment;

		public int FeatureID;

		public int ProvinceID;

		public int RoadValue;

		public int RoadID;

		public int TerrainID;

		[MarshalAs(UnmanagedType.I1)]
		public bool VictoryPoint;

		public int IndexValue;

		public int FeatureIndexValue;
	}
}
