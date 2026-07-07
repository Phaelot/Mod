// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.Utility.GLVertexFormat
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Utility
{
	public class GLVertexFormat
	{
		private List<GLVertexAttribute> Attributes;

		public GLVertexFormat()
		{
			Attributes = new List<GLVertexAttribute>();
		}

		public void ApplyToBuffer(int BufferID)
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, BufferID);
			foreach (GLVertexAttribute attribute in Attributes)
			{
				if (attribute.AttributeIndex != -1)
				{
					GL.VertexAttribPointer(attribute.AttributeIndex, attribute.AttributeSize, attribute.AttributeType, attribute.AttributeNormalize, attribute.AttributeStride, attribute.AttributeStart);
				}
			}
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}

		public void AddAttribute(GLVertexAttribute Attribute)
		{
			Attributes.Add(Attribute);
		}

		public void MakeActive()
		{
			for (int i = 0; i < Attributes.Count; i++)
			{
				if (Attributes[i].AttributeIndex != -1)
				{
					GL.EnableVertexAttribArray(i);
				}
			}
		}

		public void MakeInactive()
		{
			for (int i = 0; i < Attributes.Count; i++)
			{
				if (Attributes[i].AttributeIndex != -1)
				{
					GL.DisableVertexAttribArray(i);
				}
			}
		}
	}
}