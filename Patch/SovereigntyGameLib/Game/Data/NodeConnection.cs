using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Game.Data
{
	public class NodeConnection
	{
		public NodeConnection()
		{
		}

		public NodeConnection(XElement Element)
		{
			this.NodeID = int.Parse(Element.Element("ID").Value);
			this.ConnectionType = this.ParseType(Element.Element("Type").Value);
		}

		private ConnectionTypes ParseType(string TypeName)
		{
			switch (TypeName)
			{
			case "Normal":
				return ConnectionTypes.Normal;
			case "River":
				return ConnectionTypes.River;
			case "Bridge":
				return ConnectionTypes.Bridge;
			case "Road":
				return ConnectionTypes.Road;
			case "Harbour":
				return ConnectionTypes.Harbour;
			case "Amphibious":
				return ConnectionTypes.Amphibious;
			case "Blocked":
				return ConnectionTypes.Blocked;
			case "Special":
				return ConnectionTypes.Special;
			}
			throw new Exception("Unknown connection type: " + TypeName);
		}

		public XElement GetXML()
		{
			XElement xelement = new XElement("Connection");
			XElement xelement2 = new XElement("ID");
			xelement2.Add(this.Node.ID);
			XElement xelement3 = new XElement("Type");
			xelement3.Add(this.ConnectionType.ToString());
			xelement.Add(xelement2);
			xelement.Add(xelement3);
			return xelement;
		}

		public void FixConnection(List<PathingNodeData> Nodes)
		{
			this.Node = Nodes.Single((PathingNodeData x) => x.ID == this.NodeID);
		}

		public PathingNodeData Node;

		public int NodeID;

		public ConnectionTypes ConnectionType;

		public GLSprite EditorSprite;
	}
}
