// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.UI.Map.ProvinceMeshRenderer
using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SovereigntyTK;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Map;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI.Map
{
	public class ProvinceMeshRenderer
	{
		public ProvinceData Province;

		public SeaZoneData Zone;

		public GameBase Game;

		public MapArrowVertex[] Vertices;

		public GLVertexBuffer VB;

		private static GLShader ShaderEffect;

		private static Vector4 HighlightColour = new Vector4(1f, 1f, 1f, 0.25f);

		private Vector4 Colour;

		private bool Flashing;

		private float FlashCounter;

		private float FlashMax = (float)Math.PI;

		private bool TutorialFlashing;

		private float TutorialFlashTimer;

		public bool HighlightActive;

		public string RegionName
		{
			get
			{
				if (Province != null)
				{
					return Province.Name;
				}
				if (Zone != null)
				{
					return Zone.Name;
				}
				return "";
			}
		}

		public ProvinceMeshRenderer(GameBase Game, ProvinceOutlineData Data)
		{
			this.Game = Game;
			Province = Data.Province;
			Zone = Data.Zone;
			if (ShaderEffect == null)
			{
				ShaderEffect = Game.Utilities.ShaderManager.GetShader("Data\\Shaders\\ProvinceMesh.vert", "Data\\Shaders\\ProvinceMesh.frag", UsesCamera: true);
			}
			Vertices = new MapArrowVertex[Data.MeshVertices.Count];
			for (int i = 0; i < Data.MeshVertices.Count; i++)
			{
				Vertices[i].Position = Data.MeshVertices[i];
			}
			VB = new GLVertexBuffer(MapArrowVertex.GetFormat(ShaderEffect.GetID()));
			VB.SetBufferData(Vertices, BufferUsageHint.StaticDraw);
			Colour = new Vector4(0f, 0f, 0f, 0f);
		}

		public void Dispose()
		{
			VB.Dispose();
			VB = null;
		}

		public void Flash()
		{
			Flashing = true;
			FlashCounter = 0f;
		}

		public void BeginTutorialFlash()
		{
			TutorialFlashing = true;
			TutorialFlashTimer = 0f;
		}

		public void EndTutorialFlash()
		{
			TutorialFlashing = false;
		}

		public void SetColour(float A, float R, float G, float B)
		{
			Colour = new Vector4(A, R, G, B);
		}

		public void SetColour(Vector4 Colour)
		{
			this.Colour = Colour;
		}

		public void Render(float Elapsedtime)
		{
			if (VB == null)
			{
				return;
			}
			Vector4 Value = Colour;
			if (Flashing)
			{
				FlashCounter += Elapsedtime;
				if (FlashCounter >= 2f)
				{
					FlashCounter = 2f;
					Flashing = false;
				}
				Value.X = 1f;
				Value.Y = 1f;
				Value.Z = 1f;
				Value.W = (float)Math.Sin(FlashCounter * 0.5f * FlashMax);
			}
			if (TutorialFlashing)
			{
				TutorialFlashTimer += Elapsedtime * 3f;
				if ((double)TutorialFlashTimer > Math.PI * 2.0)
				{
					TutorialFlashTimer -= (float)Math.PI * 2f;
				}
				float num = (float)Math.Sin(TutorialFlashTimer);
				if (num < 0f)
				{
					num = 0f;
				}
				Value.X = 0.058f;
				Value.Y = 0.074f;
				Value.Z = 0.662f;
				Value.W = num * 0.5f;
			}
			ShaderEffect.SetVector4("Colour", ref Value);
			VB.SetActive();
			ShaderEffect.SetActive();
			GL.DrawArrays(PrimitiveType.Triangles, 0, Vertices.Length);
			ShaderEffect.SetInactive();
			VB.SetInactive();
			if (HighlightActive)
			{
				ShaderEffect.SetVector4("Colour", ref HighlightColour);
				VB.SetActive();
				ShaderEffect.SetActive();
				GL.DrawArrays(PrimitiveType.Triangles, 0, Vertices.Length);
				ShaderEffect.SetInactive();
				VB.SetInactive();
			}
		}
	}
}