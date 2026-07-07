using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using OpenTK;

namespace SovereigntyTK.Utility
{
	public class GLTextureAtlas : GLTexture
	{
		public GLTextureAtlas(Stream s, XElement Data)
			: base(s)
		{
			this.Indices = new Dictionary<string, Vector4>();
			foreach (XElement xelement in Data.Elements())
			{
				string value = xelement.Attribute("filename").Value;
				float num;
				float.TryParse(xelement.Attribute("left").Value, NumberStyles.Any, CultureInfo.InvariantCulture, out num);
				float num2;
				float.TryParse(xelement.Attribute("top").Value, NumberStyles.Any, CultureInfo.InvariantCulture, out num2);
				float num3;
				float.TryParse(xelement.Attribute("right").Value, NumberStyles.Any, CultureInfo.InvariantCulture, out num3);
				float num4;
				float.TryParse(xelement.Attribute("bottom").Value, NumberStyles.Any, CultureInfo.InvariantCulture, out num4);
				this.Indices.Add(value.ToLowerInvariant(), new Vector4(num, num2, num3, num4));
			}
		}

		public Vector4 GetNamedTextureCoords(string Name)
		{
			Name = Name.ToLowerInvariant();
			if (!this.Indices.ContainsKey(Name))
			{
				return Vector4.Zero;
			}
			return this.Indices[Name];
		}

		private Dictionary<string, Vector4> Indices;
	}
}
