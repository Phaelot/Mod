// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.UI.Map.ArrowSprite
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SovereigntyTK;
using SovereigntyTK.UI.Map;
using SovereigntyTK.Utility;
using TriangleNet;
using TriangleNet.Data;
using TriangleNet.Geometry;

namespace SovereigntyTK.UI.Map
{
	public class ArrowSprite
	{
		private GLVertexBuffer VB;

		public static GLShader ShaderEffect;

		private MapArrowVertex[] Vertices;

		private bool Visible = true;

		private Vector4 Colour;

		private int TriangleCount;

		private Sovereignty Game;

		public ArrowSprite(PointF[] Points, Sovereignty Game)
		{
			this.Game = Game;
			if (ShaderEffect == null)
			{
				ShaderEffect = Game.Utilities.ShaderManager.GetShader("Data\\Shaders\\Arrow.vert", "Data\\Shaders\\Arrow.frag", UsesCamera: true);
			}
			Random random = new Random();
			Mesh mesh;
			while (true)
			{
				try
				{
					mesh = new Mesh();
					mesh.Behavior.Convex = false;
					mesh.Behavior.Algorithm = TriangulationAlgorithm.SweepLine;
					InputGeometry inputGeometry = new InputGeometry();
					for (int i = 0; i < Points.Length; i++)
					{
						PointF pointF = Points[i];
						inputGeometry.AddPoint(pointF.X, pointF.Y);
					}
					for (int j = 0; j < inputGeometry.Points.Count(); j++)
					{
						if (j == inputGeometry.Points.Count() - 1)
						{
							inputGeometry.AddSegment(j, 0);
						}
						else
						{
							inputGeometry.AddSegment(j, j + 1);
						}
					}
					mesh.Triangulate(inputGeometry);
				}
				catch
				{
					for (int k = 0; k < Points.Length; k++)
					{
						Points[k].X += (float)random.NextDouble() / 10f;
						Points[k].Y += (float)random.NextDouble() / 10f;
					}
					continue;
				}
				break;
			}
			TriangleCount = mesh.Triangles.Count;
			Vertices = new MapArrowVertex[mesh.Triangles.Count * 3];
			int num = 0;
			foreach (Triangle triangle in mesh.Triangles)
			{
				double x = triangle.GetVertex(0).X;
				double y = triangle.GetVertex(0).Y;
				double x2 = triangle.GetVertex(1).X;
				double y2 = triangle.GetVertex(1).Y;
				double x3 = triangle.GetVertex(2).X;
				double y3 = triangle.GetVertex(2).Y;
				ref MapArrowVertex reference = ref Vertices[num++];
				reference = new MapArrowVertex(new Vector3((float)x, 0f, (float)y));
				ref MapArrowVertex reference2 = ref Vertices[num++];
				reference2 = new MapArrowVertex(new Vector3((float)x2, 0f, (float)y2));
				ref MapArrowVertex reference3 = ref Vertices[num++];
				reference3 = new MapArrowVertex(new Vector3((float)x3, 0f, (float)y3));
			}
			VB = new GLVertexBuffer(MapArrowVertex.GetFormat(ShaderEffect.GetID()));
			VB.SetBufferData(Vertices, BufferUsageHint.DynamicDraw);
		}

		public Vector3 ScalePoint(Vector3 p, float Scale)
		{
			return p * Scale;
		}

		public Vector3 GetBezierPoint(float Time, Vector3 Start, Vector3 End, Vector3 Control)
		{
			float num = 1f - Time;
			Vector3 vector = ScalePoint(Start, num * num);
			Vector3 vector2 = ScalePoint(Control, 2f * Time * num);
			Vector3 vector3 = ScalePoint(End, Time * Time);
			return new Vector3(vector.X + vector2.X + vector3.X, vector.Y + vector2.Y + vector3.Y, vector.Z + vector2.Z + vector3.Z);
		}

		public void CreateHeightOffsets(List<Vector3> GuideLine)
		{
			float length = (GuideLine[0] - GuideLine[GuideLine.Count - 1]).Length;
			Vector3 start = new Vector3(0f, 0f, 0f);
			Vector3 end = new Vector3(100f, 0f, 0f);
			Vector3 control = new Vector3(50f, length * 0.75f, 0f);
			for (int i = 0; i < Vertices.Length; i++)
			{
				int num = -1;
				int num2 = 0;
				float num3 = float.MaxValue;
				foreach (Vector3 item in GuideLine)
				{
					float lengthSquared = (item - Vertices[i].Position).LengthSquared;
					if (lengthSquared < num3)
					{
						num3 = lengthSquared;
						num = num2;
					}
					num2++;
				}
				if (num > -1)
				{
					float time = (GuideLine[num] - GuideLine[0]).Length / length;
					Vector3 bezierPoint = GetBezierPoint(time, start, end, control);
					Vertices[i].Position.Y = bezierPoint.Y;
				}
			}
			VB.SetBufferData(Vertices, BufferUsageHint.DynamicDraw);
		}

		public void Dispose()
		{
			if (VB != null)
			{
				VB.Dispose();
			}
			VB = null;
		}

		public void Render()
		{
			if (VB != null && Visible)
			{
				ShaderEffect.SetVector4("ArrowColour", ref Colour);
				VB.SetActive();
				ShaderEffect.SetActive();
				GL.DrawArrays(PrimitiveType.Triangles, 0, Vertices.Length);
				ShaderEffect.SetInactive();
				VB.SetInactive();
			}
		}

		internal void SetColour(Color color)
		{
			float x = (float)(int)color.R / 255f;
			float y = (float)(int)color.G / 255f;
			float z = (float)(int)color.B / 255f;
			float w = (float)(int)color.A / 255f;
			Colour = new Vector4(x, y, z, w);
		}
	}
}