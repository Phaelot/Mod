// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.Utility.GLShader
using System.IO;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SovereigntyTK;
using SovereigntyTK.UI;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Utility
{
	public class GLShader
	{
		private int ProgramID;

		private int VertexShaderID;

		private int FragmentShaderID;

		public GLShader(FileManager FileSystem, string VertexShaderFile, string FragmentShaderFile)
		{
			ProgramID = GL.CreateProgram();
			VertexShaderID = GL.CreateShader(ShaderType.VertexShader);
			FragmentShaderID = GL.CreateShader(ShaderType.FragmentShader);
			StreamReader streamReader = new StreamReader(FileSystem.OpenFile(VertexShaderFile, FileTypes.Application));
			string text = streamReader.ReadToEnd();
			streamReader.Close();
			streamReader = new StreamReader(FileSystem.OpenFile(FragmentShaderFile, FileTypes.Application));
			string text2 = streamReader.ReadToEnd();
			streamReader.Close();
			GL.ShaderSource(VertexShaderID, text);
			GL.ShaderSource(FragmentShaderID, text2);
			GL.CompileShader(VertexShaderID);
			string shaderInfoLog = GL.GetShaderInfoLog(VertexShaderID);
			GL.GetShader(VertexShaderID, ShaderParameter.CompileStatus, out var @params);
			if (@params != 1)
			{
				string text3 = "Cannot compile Vertex Shader " + VertexShaderFile + "\n";
				text3 = text3 + "Error: " + shaderInfoLog + "\n";
				text3 += "\n";
				text3 += "Make sure that your graphics hardware meets the minimum requirements, and that you have up to date graphics drivers for your system.";
				MessageBox.Show(text3);
				return;
			}
			GL.CompileShader(FragmentShaderID);
			shaderInfoLog = GL.GetShaderInfoLog(FragmentShaderID);
			GL.GetShader(FragmentShaderID, ShaderParameter.CompileStatus, out @params);
			if (@params != 1)
			{
				string text4 = "Cannot compile Fragment Shader " + FragmentShaderFile + "\n";
				text4 = text4 + "Error: " + shaderInfoLog + "\n";
				text4 += "\n";
				text4 += "Make sure that your graphics hardware meets the minimum requirements, and that you have up to date graphics drivers for your system.";
				MessageBox.Show(text4);
				return;
			}
			GL.AttachShader(ProgramID, VertexShaderID);
			GL.AttachShader(ProgramID, FragmentShaderID);
			GL.LinkProgram(ProgramID);
			shaderInfoLog = GL.GetProgramInfoLog(ProgramID);
			GL.GetProgram(ProgramID, GetProgramParameterName.LinkStatus, out @params);
			if (@params != 1)
			{
				string text5 = "Cannot link shader program\n";
				text5 = text5 + "Error: " + shaderInfoLog + "\n";
				text5 += "\n";
				text5 += "Make sure that your graphics hardware meets the minimum requirements, and that you have up to date graphics drivers for your system.";
				MessageBox.Show(text5);
			}
		}

		public GLShader(GameBase Game, string VertexShaderFile, string FragmentShaderFile)
		{
			ProgramID = GL.CreateProgram();
			VertexShaderID = GL.CreateShader(ShaderType.VertexShader);
			FragmentShaderID = GL.CreateShader(ShaderType.FragmentShader);
			StreamReader streamReader = new StreamReader(Game.Utilities.FileSystem.OpenFile(VertexShaderFile, FileTypes.Application));
			string text = streamReader.ReadToEnd();
			streamReader.Close();
			streamReader = new StreamReader(Game.Utilities.FileSystem.OpenFile(FragmentShaderFile, FileTypes.Application));
			string text2 = streamReader.ReadToEnd();
			streamReader.Close();
			GL.ShaderSource(VertexShaderID, text);
			GL.ShaderSource(FragmentShaderID, text2);
			GL.CompileShader(VertexShaderID);
			string shaderInfoLog = GL.GetShaderInfoLog(VertexShaderID);
			GL.GetShader(VertexShaderID, ShaderParameter.CompileStatus, out var @params);
			if (@params != 1)
			{
				string text3 = "Cannot compile Vertex Shader " + VertexShaderFile + "\n";
				text3 = text3 + "Error: " + shaderInfoLog + "\n";
				text3 += "\n";
				text3 += "Make sure that your graphics hardware meets the minimum requirements, and that you have up to date graphics drivers for your system.";
				MessageBox.Show(text3);
				Game.ForceShutdown();
				return;
			}
			GL.CompileShader(FragmentShaderID);
			shaderInfoLog = GL.GetShaderInfoLog(FragmentShaderID);
			GL.GetShader(FragmentShaderID, ShaderParameter.CompileStatus, out @params);
			if (@params != 1)
			{
				string text4 = "Cannot compile Fragment Shader " + FragmentShaderFile + "\n";
				text4 = text4 + "Error: " + shaderInfoLog + "\n";
				text4 += "\n";
				text4 += "Make sure that your graphics hardware meets the minimum requirements, and that you have up to date graphics drivers for your system.";
				MessageBox.Show(text4);
				Game.ForceShutdown();
				return;
			}
			GL.AttachShader(ProgramID, VertexShaderID);
			GL.AttachShader(ProgramID, FragmentShaderID);
			GL.LinkProgram(ProgramID);
			shaderInfoLog = GL.GetProgramInfoLog(ProgramID);
			GL.GetProgram(ProgramID, GetProgramParameterName.LinkStatus, out @params);
			if (@params != 1)
			{
				string text5 = "Cannot link shader program\n";
				text5 = text5 + "Error: " + shaderInfoLog + "\n";
				text5 += "\n";
				text5 += "Make sure that your graphics hardware meets the minimum requirements, and that you have up to date graphics drivers for your system.";
				MessageBox.Show(text5);
				Game.ForceShutdown();
			}
		}

		public void Dispose()
		{
			GL.DeleteProgram(ProgramID);
		}

		public void SetActive()
		{
			GL.UseProgram(ProgramID);
		}

		public void SetInactive()
		{
			GL.UseProgram(0);
		}

		public void SetFloat(string Name, float Value)
		{
			GL.UseProgram(ProgramID);
			int uniformLocation = GL.GetUniformLocation(ProgramID, Name);
			GL.Uniform1(uniformLocation, Value);
			GL.UseProgram(0);
		}

		public void SetVector4(string Name, ref Vector4 Value)
		{
			GL.UseProgram(ProgramID);
			int uniformLocation = GL.GetUniformLocation(ProgramID, Name);
			GL.Uniform4(uniformLocation, ref Value);
			GL.UseProgram(0);
		}

		public void SetTexture(string Name, int TextureUnit)
		{
			GL.UseProgram(ProgramID);
			int uniformLocation = GL.GetUniformLocation(ProgramID, Name);
			GL.Uniform1(uniformLocation, TextureUnit);
			GL.UseProgram(0);
		}

		public void SetMatrix(string Name, ref Matrix4 Matrix)
		{
			GL.UseProgram(ProgramID);
			int uniformLocation = GL.GetUniformLocation(ProgramID, Name);
			GL.UniformMatrix4(uniformLocation, transpose: false, ref Matrix);
			GL.UseProgram(0);
		}

		internal void UpdateMatrices(GameCamera Camera)
		{
			Matrix4 Matrix = Matrix4.Identity;
			SetMatrix("World", ref Matrix);
			SetMatrix("View", ref Camera.ViewMatrix);
			SetMatrix("Projection", ref Camera.ProjectionMatrix);
		}

		public int GetID()
		{
			return ProgramID;
		}

		public void SetInteger(string Name, int Value)
		{
			GL.UseProgram(ProgramID);
			int uniformLocation = GL.GetUniformLocation(ProgramID, Name);
			GL.Uniform1(uniformLocation, Value);
			GL.UseProgram(0);
		}
	}
}
