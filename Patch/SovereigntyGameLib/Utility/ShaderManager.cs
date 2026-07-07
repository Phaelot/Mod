using System;
using System.Collections.Generic;
using SovereigntyTK.UI;

namespace SovereigntyTK.Utility
{
	public class ShaderManager
	{
		public ShaderManager(GameBase Game)
		{
			this.Game = Game;
			this.LoadedShaders = new Dictionary<string, GLShader>();
			this.CameraShaders = new List<GLShader>();
		}

		internal void Dispose()
		{
			foreach (GLShader glshader in this.LoadedShaders.Values)
			{
				glshader.Dispose();
			}
			this.LoadedShaders.Clear();
			this.CameraShaders.Clear();
		}

		public void UpdateCameraMatrices(GameCamera Camera)
		{
			foreach (GLShader glshader in this.CameraShaders)
			{
				glshader.UpdateMatrices(Camera);
			}
		}

		public GLShader GetShader(string VertexFilename, string FragmentFilename, bool UsesCamera)
		{
			string text = VertexFilename + FragmentFilename;
			GLShader glshader = null;
			this.LoadedShaders.TryGetValue(text, out glshader);
			if (glshader == null)
			{
				glshader = new GLShader(this.Game, VertexFilename, FragmentFilename);
				this.LoadedShaders.Add(text, glshader);
				if (UsesCamera)
				{
					this.CameraShaders.Add(glshader);
				}
			}
			return glshader;
		}

		private GameBase Game;

		private Dictionary<string, GLShader> LoadedShaders;

		private List<GLShader> CameraShaders;
	}
}
