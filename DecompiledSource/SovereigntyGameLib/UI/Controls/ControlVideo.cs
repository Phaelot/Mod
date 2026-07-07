using System;
using System.Drawing;
using System.Xml.Linq;
using OpenTK;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI.Controls
{
	public class ControlVideo : UIControl
	{
		public ControlVideo(GameBase Game)
			: base(Game)
		{
			if (ControlVideo.Shader == null)
			{
				ThreadedGLSLVideoPlayer.Init();
				Rectangle viewport = Game.GetViewport();
				Matrix4 matrix = Matrix4.CreateOrthographicOffCenter(0f, (float)viewport.Width, (float)viewport.Height, 0f, -1f, 1f);
				ControlVideo.Shader = Game.Utilities.ShaderManager.GetShader("Data\\Shaders\\yuvtorgb_vertex.glsl", "Data\\Shaders\\yuvtorgb_fragment.glsl", false);
				ControlVideo.Shader.SetMatrix("Projection", ref matrix);
			}
			this.videoPlayer = new ThreadedGLSLVideoPlayer(Game, ControlVideo.Shader);
		}

		protected override void ParseElement(XElement Element)
		{
			string localName;
			if ((localName = Element.Name.LocalName) != null && localName == "videofile")
			{
				this.SetVideoFile(Element.Value);
				return;
			}
			base.ParseElement(Element);
		}

		public override void Dispose()
		{
			this.videoPlayer.Stop();
			base.Dispose();
		}

		protected override void ReclaculateBounds()
		{
			base.ReclaculateBounds();
			if (this.videoPlayer != null)
			{
				this.videoPlayer.UpdateVertices(this.Sprite.Bounds.X, this.Sprite.Bounds.Y, this.Sprite.Bounds.Width, this.Sprite.Bounds.Height);
				Rectangle viewport = this.Game.GetViewport();
				Matrix4 matrix = Matrix4.CreateOrthographicOffCenter(0f, (float)viewport.Width, (float)viewport.Height, 0f, -1f, 1f);
				ControlVideo.Shader.SetMatrix("Projection", ref matrix);
			}
		}

		public void SetVideoFile(string Filename)
		{
			Filename = this.Game.Utilities.FileSystem.ConvertFilename(Filename, FileTypes.Application, true);
			Filename = Filename.Replace("\\", "/");
			this.videoPlayer.LoadVideo("file:///" + Filename);
		}

		public override void Render(GLShader Shader, float ElapsedTime)
		{
			base.Render(Shader, ElapsedTime);
			if (base.Visible)
			{
				this.videoPlayer.Update();
				this.videoPlayer.Draw();
			}
		}

		private ThreadedGLSLVideoPlayer videoPlayer;

		private static GLShader Shader;
	}
}
