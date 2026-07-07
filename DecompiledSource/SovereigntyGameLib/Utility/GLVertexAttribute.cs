using System;
using OpenTK.Graphics.OpenGL;

namespace SovereigntyTK.Utility
{
	public struct GLVertexAttribute
	{
		public GLVertexAttribute(int Index, int Size, VertexAttribPointerType Type, bool Normalize, int Stride, int Start)
		{
			this.AttributeSize = Size;
			this.AttributeType = Type;
			this.AttributeNormalize = Normalize;
			this.AttributeStride = Stride;
			this.AttributeStart = Start;
			this.AttributeIndex = Index;
		}

		public int AttributeSize;

		public VertexAttribPointerType AttributeType;

		public bool AttributeNormalize;

		public int AttributeStride;

		public int AttributeStart;

		public int AttributeIndex;
	}
}
