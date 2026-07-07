// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.UI.Map.MapText
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SovereigntyTK;
using SovereigntyTK.UI.Map;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI.Map
{
	public class MapText
	{
		private Sovereignty Game;

		private GLTexture Tex;

		private GLVertexBuffer VB;

		public static GLShader Shader;

		public MapText(Sovereignty Game, TextLine LineData, int FontSize, GameText Text)
		{
			this.Game = Game;
			string font = Game.Utilities.FontConvertor.GetFont("Marigold", Bold: true);
			GameFont font2 = GameFont.GetFont(Game, "Marigold", "Data\\Fonts\\" + font, FontSize);
			int num = (int)Math.Min(LineData.StartPoint.Y, LineData.EndPoint.Y);
			int num2 = (int)Math.Max(LineData.StartPoint.Y, LineData.EndPoint.Y);
			int num3 = (int)Math.Min(LineData.StartPoint.X, LineData.EndPoint.X);
			int num4 = (int)Math.Max(LineData.StartPoint.X, LineData.EndPoint.X);
			LineData.StartPoint.Y -= num;
			LineData.StartPoint.X -= num3;
			LineData.EndPoint.Y -= num;
			LineData.EndPoint.X -= num3;
			LineData.ControlPoint.Y -= num;
			LineData.ControlPoint.X -= num3;
			int num5 = num4 - num3;
			int num6 = num2 - num;
			if (num5 < num6)
			{
				int num7 = (num6 - num5) / 2;
				num3 -= num7;
				num4 += num7;
				LineData.StartPoint.X += num7;
				LineData.EndPoint.X += num7;
				LineData.ControlPoint.X += num7;
				num5 = num6;
			}
			if (num6 < num5)
			{
				int num8 = (num5 - num6) / 2;
				num -= num8;
				num2 += num8;
				LineData.StartPoint.Y += num8;
				LineData.EndPoint.Y += num8;
				LineData.ControlPoint.Y += num8;
				num6 = num5;
			}
			if (num5 == 0 || num6 == 0)
			{
				VB = null;
				return;
			}
			Bitmap bitmap = null;
			try
			{
				bitmap = new Bitmap(num5, num6);
			}
			catch (Exception)
			{
				VB = null;
				return;
			}
			Graphics graphics = Graphics.FromImage(bitmap);
			string text = Text.GetActualText(Game).ToUpper();
			List<FontGlyphData> list = new List<FontGlyphData>();
			string text2 = text;
			foreach (char c in text2)
			{
				list.Add(font2.GetGlyph(c));
			}
			float num9;
			for (num9 = list.Sum((FontGlyphData x) => x.AdvanceX); num9 > LineData.LineLength * 0.9f; num9 = list.Sum((FontGlyphData x) => x.AdvanceX))
			{
				FontSize = ((FontSize > 20) ? (FontSize - 5) : ((FontSize <= 10) ? (FontSize - 1) : (FontSize - 2)));
				font2 = GameFont.GetFont(Game, "Marigold", "Data\\Fonts\\" + font, FontSize);
				list.Clear();
				string text3 = text;
				foreach (char c2 in text3)
				{
					list.Add(font2.GetGlyph(c2));
				}
			}
			float num11 = LineData.LineLength / 2f - num9 / 2f;
			float num12 = DegreesToRadians(LineData.Angle - 90f);
			Vector2 zero = Vector2.Zero;
			zero.X += (float)Math.Cos(num12);
			zero.Y += (float)Math.Sin(num12);
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.InterpolationMode = InterpolationMode.High;
			foreach (FontGlyphData item in list)
			{
				float num13 = num11 / LineData.LineLength;
				Vector2 bezierPoint = GetBezierPoint(num13, LineData.StartPoint, LineData.EndPoint, LineData.ControlPoint);
				Vector2 bezierPoint2 = GetBezierPoint(num13 + 0.01f, LineData.StartPoint, LineData.EndPoint, LineData.ControlPoint);
				float value = (float)Math.Atan2(bezierPoint2.Y - bezierPoint.Y, bezierPoint2.X - bezierPoint.X);
				value = RadiansToDegrees(value);
				if (item.GlyphImage != null)
				{
					Vector2 vector = bezierPoint;
					vector += zero * (item.OffsetY - FontSize / 4);
					graphics.ResetTransform();
					graphics.TranslateTransform(vector.X, vector.Y);
					graphics.RotateTransform(value);
					graphics.DrawImage(item.GlyphImage, 0, 0);
				}
				num11 += (float)item.AdvanceX;
			}
			graphics.ResetTransform();
			graphics.Dispose();
			Tex = new GLTexture(bitmap);
			bitmap.Dispose();
			if (Shader == null)
			{
				Shader = Game.Utilities.ShaderManager.GetShader("Data\\Shaders\\MapText.vert", "Data\\Shaders\\MapText.frag", UsesCamera: true);
				Shader.SetTexture("Texture", 0);
			}
			MapTextVertex[] data = new MapTextVertex[6]
			{
			new MapTextVertex(new Vector3(num3, 0f, num), new Vector2(0f, 0f)),
			new MapTextVertex(new Vector3(num4, 0f, num), new Vector2(1f, 0f)),
			new MapTextVertex(new Vector3(num3, 0f, num2), new Vector2(0f, 1f)),
			new MapTextVertex(new Vector3(num4, 0f, num), new Vector2(1f, 0f)),
			new MapTextVertex(new Vector3(num4, 0f, num2), new Vector2(1f, 1f)),
			new MapTextVertex(new Vector3(num3, 0f, num2), new Vector2(0f, 1f))
			};
			VB = new GLVertexBuffer(MapTextVertex.GetFormat(Shader.GetID()));
			VB.SetBufferData(data, BufferUsageHint.StaticDraw);
		}

		public Vector2 GetBezierPoint(float Time, Vector2 Start, Vector2 End, Vector2 Control)
		{
			float num = 1f - Time;
			Vector2 vector = ScalePoint(Start, num * num);
			Vector2 vector2 = ScalePoint(Control, 2f * Time * num);
			Vector2 vector3 = ScalePoint(End, Time * Time);
			return new Vector2(vector.X + vector2.X + vector3.X, vector.Y + vector2.Y + vector3.Y);
		}

		public Vector2 ScalePoint(Vector2 p, float Scale)
		{
			return p * Scale;
		}

		private float DegreesToRadians(float Value)
		{
			return Value * (float)Math.PI / 180f;
		}

		private float RadiansToDegrees(float Value)
		{
			return Value * 180f / (float)Math.PI;
		}

		public void Render()
		{
			if (VB != null)
			{
				Shader.SetActive();
				VB.SetActive();
				Tex.SetActive(TextureUnit.Texture0);
				GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
				Tex.SetInactive(TextureUnit.Texture0);
				VB.SetInactive();
				Shader.SetInactive();
			}
		}

		internal void Dispose()
		{
			if (VB != null)
			{
				Tex.Dispose();
				VB.Dispose();
			}
		}
	}
}