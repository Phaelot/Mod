using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml.Linq;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Game.Data
{
	public class PathingNodeData
	{
		public PathingNodeData()
		{
			this.ConnectedNodes = new List<NodeConnection>();
		}

		public PathingNodeData(XElement Element)
		{
			this.ConnectedNodes = new List<NodeConnection>();
			this.ID = int.Parse(Element.Element("ID").Value);
			this.ProvinceID = int.Parse(Element.Element("Province").Value);
			this.ParseCoords(Element.Element("Coords").Value, ref this.MapCoords);
			this.NodeType = this.ParseType(Element.Element("Type").Value);
			foreach (XElement xelement in Element.Elements("Connection"))
			{
				NodeConnection nodeConnection = new NodeConnection(xelement);
				this.ConnectedNodes.Add(nodeConnection);
			}
		}

		public void FixConnections(List<PathingNodeData> Nodes)
		{
			foreach (NodeConnection nodeConnection in this.ConnectedNodes)
			{
				nodeConnection.FixConnection(Nodes);
			}
		}

		private PathNodeTypes ParseType(string TypeName)
		{
			if (TypeName != null)
			{
				if (TypeName == "Land")
				{
					return PathNodeTypes.Land;
				}
				if (TypeName == "Sea")
				{
					return PathNodeTypes.Sea;
				}
				if (TypeName == "Harbour")
				{
					return PathNodeTypes.Harbour;
				}
				if (TypeName == "RiverHarbour")
				{
					return PathNodeTypes.RiverHarbour;
				}
			}
			throw new Exception("Unknown node type: " + TypeName);
		}

		private void ParseCoords(string CoordString, ref Point CoordPoint)
		{
			string[] array = CoordString.Split(new char[] { ',' });
			CoordPoint = new Point(0, 0);
			if (array.Length == 2)
			{
				int num = 0;
				int.TryParse(array[0], out num);
				CoordPoint.X = num;
				num = 0;
				int.TryParse(array[1], out num);
				CoordPoint.Y = num;
			}
		}

		public XElement GetXML()
		{
			XElement xelement = new XElement("Node");
			XElement xelement2 = new XElement("ID");
			xelement2.Add(this.ID);
			XElement xelement3 = new XElement("Province");
			xelement3.Add(this.ProvinceID);
			XElement xelement4 = new XElement("Coords");
			xelement4.Add(this.MapCoords.X.ToString() + "," + this.MapCoords.Y.ToString());
			XElement xelement5 = new XElement("Type");
			xelement5.Add(this.NodeType.ToString());
			xelement.Add(xelement2);
			xelement.Add(xelement3);
			xelement.Add(xelement4);
			xelement.Add(xelement5);
			foreach (NodeConnection nodeConnection in this.ConnectedNodes)
			{
				xelement.Add(nodeConnection.GetXML());
			}
			return xelement;
		}

		public int ID;

		public Point MapCoords;

		public int ProvinceID;

		public PathNodeTypes NodeType;

		public GLSprite EditorSprite;

		public List<NodeConnection> ConnectedNodes;
	}
}
