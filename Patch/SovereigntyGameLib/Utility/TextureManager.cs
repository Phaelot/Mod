using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Linq;

namespace SovereigntyTK.Utility
{
	public class TextureManager
	{
		public TextureManager(GameBase Game, UtilityManager Utilities)
		{
			this.Game = Game;
			this.FileManager = Game.Utilities.FileSystem;
			this.LoadedTextures = new Dictionary<string, LoadedTextureData>();
			this.NameLookupTable = new Dictionary<GLTexture, string>();
			this.RemoveList = new List<string>();
		}

		public void ReleaseTexture(GLTexture Tex)
		{
			if (Tex == null)
			{
				return;
			}
			string text = null;
			this.NameLookupTable.TryGetValue(Tex, out text);
			if (text == null)
			{
				return;
			}
			this.LoadedTextures[text].ReferenceCount--;
			if (this.LoadedTextures[text].ReferenceCount == 0 && !this.RemoveList.Contains(text))
			{
				this.RemoveList.Add(text);
			}
		}

		public GLTextureAtlas GetTexture(string FileName, string IndexName)
		{
			FileName = FileName.ToLowerInvariant();
			if (!this.LoadedTextures.ContainsKey(FileName))
			{
				this.LoadTexture(FileName, IndexName);
			}
			this.LoadedTextures[FileName].ReferenceCount++;
			return this.LoadedTextures[FileName].Tex as GLTextureAtlas;
		}

		public GLTexture GetTexture(string FileName)
		{
			FileName = FileName.ToLowerInvariant();
			if (!this.LoadedTextures.ContainsKey(FileName))
			{
				this.LoadTexture(FileName);
			}
			this.LoadedTextures[FileName].ReferenceCount++;
			return this.LoadedTextures[FileName].Tex;
		}

		public void Dispose()
		{
			foreach (GLTexture gltexture in this.NameLookupTable.Keys)
			{
				gltexture.Dispose();
			}
			this.NameLookupTable.Clear();
			this.LoadedTextures.Clear();
			this.RemoveList.Clear();
		}

		internal void Update()
		{
			this.CleanupCounter++;
			if (this.CleanupCounter > 20)
			{
				this.CleanupCounter = 0;
				this.Cleanup();
			}
		}

		internal void Cleanup()
		{
			if (this.RemoveList.Count == 0)
			{
				return;
			}
			foreach (string text in this.RemoveList)
			{
				if (this.LoadedTextures[text].ReferenceCount <= 0)
				{
					GLTexture tex = this.LoadedTextures[text].Tex;
					this.LoadedTextures.Remove(text);
					this.NameLookupTable.Remove(tex);
					tex.Dispose();
				}
			}
			this.RemoveList.Clear();
		}

		private void LoadTexture(string FileName)
		{
			try
			{
				Stream stream = this.FileManager.OpenFile(FileName, FileTypes.Application, FileModes.ReadOnly, true);
				if (stream == null)
				{
					MessageBox.Show("Warning: missing texture file - " + FileName);
					stream = this.FileManager.OpenFile("Data\\Images\\Missing.png", FileTypes.Application, FileModes.ReadOnly, true);
				}
				GLTexture gltexture = new GLTexture(stream);
				stream.Close();
				this.LoadedTextures.Add(FileName, new LoadedTextureData(gltexture));
				this.NameLookupTable.Add(gltexture, FileName);
			}
			catch (Exception ex)
			{
				throw new Exception("Error opening texture file: " + FileName + "\nDetail: " + ex.Message);
			}
		}

		private void LoadTexture(string FileName, string IndexName)
		{
			GLTextureAtlas gltextureAtlas = new GLTextureAtlas(this.FileManager.OpenFile(FileName, FileTypes.Application, FileModes.ReadOnly, true), XElement.Load(this.FileManager.OpenFile(IndexName, FileTypes.Application, FileModes.ReadOnly, true)));
			this.LoadedTextures.Add(FileName, new LoadedTextureData(gltextureAtlas));
			this.NameLookupTable.Add(gltextureAtlas, FileName);
		}

		public void CreateTextureArray(Bitmap Image, int BlockSize)
		{
		}

		private byte[] GetBitmapBytes(Bitmap Bmp)
		{
			BitmapData bitmapData = Bmp.LockBits(new Rectangle(0, 0, Bmp.Width, Bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			IntPtr scan = bitmapData.Scan0;
			int num = Math.Abs(bitmapData.Stride) * Bmp.Height;
			byte[] array = new byte[num];
			Marshal.Copy(scan, array, 0, num);
			Bmp.UnlockBits(bitmapData);
			return array;
		}

		private FileManager FileManager;

		private GameBase Game;

		private Dictionary<string, LoadedTextureData> LoadedTextures;

		private Dictionary<GLTexture, string> NameLookupTable;

		private List<string> RemoveList;

		private int CleanupCounter;
	}
}
