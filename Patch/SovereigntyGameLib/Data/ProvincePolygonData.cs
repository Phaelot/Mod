using System;
using System.Collections.Generic;
using System.IO;

namespace SovereigntyTK.Data
{
	public class ProvincePolygonData
	{
		public ProvincePolygonData()
		{
			this.Polygons = new List<PolygonData>();
			this.ID = ProvincePolygonData.NextID++;
		}

		public void Save(BinaryWriter w)
		{
			w.Write(this.RegionName);
			w.Write(this.Polygons.Count);
			w.Write(this.ID);
			foreach (PolygonData polygonData in this.Polygons)
			{
				polygonData.Save(w);
			}
		}

		public void Load(BinaryReader r)
		{
			this.RegionName = r.ReadString();
			int num = r.ReadInt32();
			this.ID = r.ReadInt32();
			if (ProvincePolygonData.NextID < this.ID)
			{
				ProvincePolygonData.NextID = this.ID + 1;
			}
			for (int i = 0; i < num; i++)
			{
				PolygonData polygonData = new PolygonData();
				polygonData.Load(r);
				this.Polygons.Add(polygonData);
			}
		}

		public void MovePolyUp(int ID)
		{
			if (ID == 0)
			{
				return;
			}
			if (this.Polygons.Count < 2)
			{
				return;
			}
			int num = ID - 1;
			PolygonData polygonData = this.Polygons[ID];
			this.Polygons.Remove(polygonData);
			this.Polygons.Insert(num, polygonData);
		}

		public void MovePolyDown(int ID)
		{
			if (ID == 0)
			{
				return;
			}
			if (this.Polygons.Count < 2)
			{
				return;
			}
			int num = ID + 1;
			PolygonData polygonData = this.Polygons[ID];
			this.Polygons.Remove(polygonData);
			this.Polygons.Insert(num, polygonData);
		}

		public string RegionName;

		public List<PolygonData> Polygons;

		public int ID;

		public static int NextID = 1;
	}
}
