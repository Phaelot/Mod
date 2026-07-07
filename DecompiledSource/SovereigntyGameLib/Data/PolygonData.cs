using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Data
{
	public class PolygonData
	{
		public PolygonData()
		{
			this.Points = new List<PointF>();
			this.NodeSprites = new List<GLBaseSprite>();
			this.LineSprites = new List<GLBaseSprite>();
		}

		public void Save(BinaryWriter w)
		{
			w.Write(this.Points.Count);
			foreach (PointF pointF in this.Points)
			{
				w.Write(pointF.X);
				w.Write(pointF.Y);
			}
		}

		public void Load(BinaryReader r)
		{
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				PointF pointF = new PointF(r.ReadSingle(), r.ReadSingle());
				this.Points.Add(pointF);
			}
		}

		public List<PointF> Points;

		public List<GLBaseSprite> NodeSprites;

		public List<GLBaseSprite> LineSprites;
	}
}
