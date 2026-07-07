using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml.Linq;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI.Controls
{
	public class ControlGraph : UIControl
	{
		public int MinimumY
		{
			get
			{
				return this.m_MinimumY;
			}
			set
			{
				this.m_MinimumY = value;
				this.RedrawGraph();
			}
		}

		public int MaximumY
		{
			get
			{
				return this.m_MaximumY;
			}
			set
			{
				this.m_MaximumY = value;
				this.RedrawGraph();
			}
		}

		public ControlGraph(GameBase Game)
			: base(Game)
		{
		}

		protected override void ParseElement(XElement Element)
		{
			string localName = Element.Name.LocalName;
			base.ParseElement(Element);
		}

		public override void Dispose()
		{
			this.RemoveTexture();
			base.Dispose();
		}

		public void SetData(List<int> Data)
		{
			this.DataPoints = Data;
			this.RedrawGraph();
		}

		public void RedrawGraph()
		{
			this.RemoveTexture();
			if (this.DataPoints == null)
			{
				return;
			}
			if (this.DataPoints.Count < 2)
			{
				return;
			}
			if (this.m_MaximumY <= this.m_MinimumY)
			{
				return;
			}
			this.GraphCanvas = new Bitmap((int)this.Sprite.Bounds.Width, (int)this.Sprite.Bounds.Height);
			Graphics graphics = Graphics.FromImage(this.GraphCanvas);
			float num = (float)this.GraphCanvas.Width / (float)(this.DataPoints.Count - 1);
			for (int i = 1; i < this.DataPoints.Count; i++)
			{
				float num2 = (float)(i - 1) * num;
				float num3 = num2 + num;
				num2 -= num;
				num3 -= num;
				float num4 = (float)(this.DataPoints[i - 1] - this.m_MinimumY) / (float)(this.m_MaximumY - this.m_MinimumY);
				num4 *= (float)this.GraphCanvas.Height;
				num4 = (float)this.GraphCanvas.Height - num4;
				float num5 = (float)(this.DataPoints[i] - this.m_MinimumY) / (float)(this.m_MaximumY - this.m_MinimumY);
				num5 *= (float)this.GraphCanvas.Height;
				num5 = (float)this.GraphCanvas.Height - num5;
				graphics.DrawLine(Pens.White, new Point((int)num2, (int)num4), new Point((int)num3, (int)num5));
			}
			graphics.Dispose();
			this.Sprite.CurrentTexture = new GLTexture(this.GraphCanvas);
		}

		protected override void ReclaculateBounds()
		{
			base.ReclaculateBounds();
			this.RedrawGraph();
		}

		public override void Render(GLShader Shader, float ElapsedTime)
		{
			base.Render(Shader, ElapsedTime);
		}

		private void RemoveTexture()
		{
			if (this.Sprite == null)
			{
				return;
			}
			if (this.Sprite.CurrentTexture != null)
			{
				this.Sprite.CurrentTexture.Dispose();
			}
			if (this.GraphCanvas != null)
			{
				this.GraphCanvas.Dispose();
			}
			this.GraphCanvas = null;
			this.Sprite.CurrentTexture = null;
		}

		private Bitmap GraphCanvas;

		private List<int> DataPoints;

		private int m_MinimumY;

		private int m_MaximumY;
	}
}
