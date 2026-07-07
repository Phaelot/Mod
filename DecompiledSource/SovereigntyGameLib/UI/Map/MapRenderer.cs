// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.UI.Map.MapRenderer
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SovereigntyTK;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Map;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI.Map
{
	public class MapRenderer
	{
		private MapVertex[] Vertices;

		private GLVertexBuffer VB;

		private short[] Indices;

		private GLIndexBuffer IB;

		private GLShader ShaderEffect;

		private GLTexture[,] RealmMapTextures;

		private GLVertexBuffer MapBGBuffer;

		private GLTexture MapBGTexture;

		public bool ReadyToRender;

		private float AlphaTime;

		private List<RegionOutlineRenderer> BorderOutlines;

		private RegionOutlineRenderer RealmSelectOutline;

		private Dictionary<string, RegionOutlineRenderer> RealmBorders;

		private List<RegionOutlineRenderer> HighlightOutlines;

		public Dictionary<string, ProvinceMeshRenderer> MeshRenderers;

		private MapArrow Arrow;

		public int MapWidth;

		public int MapHeight;

		private Sovereignty Game;

		public bool BordersOnTop;

		private bool Disposed;

		public MapRenderer(Sovereignty Game)
		{
			this.Game = Game;
			ShaderEffect = Game.Utilities.ShaderManager.GetShader("Data\\Shaders\\Map.vert", "Data\\Shaders\\Map.frag", UsesCamera: true);
			ShaderEffect.SetTexture("Texture", 0);
			MapBGTexture = Game.Utilities.TextureManager.GetTexture("Data\\Images\\Map\\wood.jpg");
			MapBGTexture.SetWrapMode(TextureWrapMode.Repeat);
			HighlightOutlines = new List<RegionOutlineRenderer>();
		}

		public void CreateMeshRenderers()
		{
			if (MeshRenderers != null)
			{
				foreach (ProvinceMeshRenderer value in MeshRenderers.Values)
				{
					value.Dispose();
				}
				MeshRenderers.Clear();
				MeshRenderers = null;
			}
			MeshRenderers = new Dictionary<string, ProvinceMeshRenderer>();
			foreach (ProvinceOutlineData value2 in Game.Data.ProvinceOutlines.Values)
			{
				ProvinceMeshRenderer provinceMeshRenderer = new ProvinceMeshRenderer(Game, value2);
				MeshRenderers.Add(provinceMeshRenderer.RegionName, provinceMeshRenderer);
			}
		}

		public void AddRealmHighlight(string RealmName)
		{
			WorkingRealm Realm = Game.CurrentGame.GetRealm(RealmName);
			if (Realm == null)
			{
				return;
			}
			RegionOutlineRenderer regionOutlineRenderer = new RegionOutlineRenderer(Game);
			HighlightOutlines.Add(regionOutlineRenderer);
			List<WorkingProvince> list = Game.CurrentGame.AllProvinces.Values.Where((WorkingProvince x) => x.OwnerID == Realm.ID).ToList();
			List<string> list2 = new List<string>();
			foreach (WorkingProvince item in list)
			{
				list2.Add(item.Name);
			}
			regionOutlineRenderer.Colour = new Vector4(0f, 0f, 0f, 1f);
			regionOutlineRenderer.LineWidth = -12f;
			regionOutlineRenderer.Update(list2);
		}

		public void RemoveProvinceHighlight(string ProvinceName)
		{
			RegionOutlineRenderer regionOutlineRenderer = HighlightOutlines.FirstOrDefault((RegionOutlineRenderer x) => x.ProvinceName == ProvinceName);
			if (regionOutlineRenderer != null)
			{
				regionOutlineRenderer.Dispose();
				HighlightOutlines.Remove(regionOutlineRenderer);
			}
		}

		public void AddProvinceHighlight(string ProvinceName, Vector4 Colour)
		{
			RegionOutlineRenderer regionOutlineRenderer = HighlightOutlines.FirstOrDefault((RegionOutlineRenderer x) => x.ProvinceName == ProvinceName);
			if (regionOutlineRenderer == null)
			{
				RegionOutlineRenderer regionOutlineRenderer2 = new RegionOutlineRenderer(Game);
				regionOutlineRenderer2.ProvinceName = ProvinceName;
				HighlightOutlines.Add(regionOutlineRenderer2);
				List<string> list = new List<string>();
				list.Add(ProvinceName);
				regionOutlineRenderer2.Colour = Colour;
				regionOutlineRenderer2.LineWidth = -12f;
				regionOutlineRenderer2.Update(list);
			}
		}

		public void ClearHighlights()
		{
			foreach (RegionOutlineRenderer highlightOutline in HighlightOutlines)
			{
				highlightOutline.Dispose();
			}
			HighlightOutlines.Clear();
		}

		public void ClearRealmBorders()
		{
			if (RealmBorders == null)
			{
				return;
			}
			foreach (RegionOutlineRenderer value in RealmBorders.Values)
			{
				value?.Dispose();
			}
			RealmBorders.Clear();
			RealmBorders = null;
		}

		public void RemoveRealmBorder(string RealmName)
		{
			if (RealmBorders != null)
			{
				RegionOutlineRenderer value = null;
				RealmBorders.TryGetValue(RealmName, out value);
				if (value != null)
				{
					RealmBorders.Remove(RealmName);
					value.Dispose();
				}
			}
		}

		public void UpdateRealmBorder(string RealmName)
		{
			if (Game.CurrentGame == null)
			{
				return;
			}
			if (RealmBorders == null)
			{
				RealmBorders = new Dictionary<string, RegionOutlineRenderer>();
			}
			WorkingRealm Realm = Game.CurrentGame.GetRealm(RealmName);
			if (Realm == null)
			{
				return;
			}
			RegionOutlineRenderer value = null;
			RealmBorders.TryGetValue(RealmName, out value);
			if (value == null)
			{
				value = new RegionOutlineRenderer(Game);
				RealmBorders.Add(RealmName, value);
			}
			List<WorkingProvince> list = Game.CurrentGame.AllProvinces.Values.Where((WorkingProvince x) => x.OwnerID == Realm.ID).ToList();
			List<string> list2 = new List<string>();
			foreach (WorkingProvince item in list)
			{
				list2.Add(item.Name);
			}
			value.LineWidth = -7f;
			value.Update(list2);
		}

		public void ClearSelectionOutlines()
		{
			if (BorderOutlines == null)
			{
				return;
			}
			foreach (RegionOutlineRenderer borderOutline in BorderOutlines)
			{
				borderOutline.Dispose();
			}
			BorderOutlines.Clear();
			BorderOutlines = null;
		}

		public void CreateSelectionOutlines()
		{
			if (BorderOutlines != null)
			{
				foreach (RegionOutlineRenderer borderOutline in BorderOutlines)
				{
					borderOutline.Dispose();
				}
				BorderOutlines.Clear();
				BorderOutlines = null;
			}
			BorderOutlines = new List<RegionOutlineRenderer>();
			foreach (RealmData Realm in Game.Data.ActiveRealms.Values)
			{
				RegionOutlineRenderer regionOutlineRenderer = new RegionOutlineRenderer(Game);
				Dictionary<string, ProvinceData>.ValueCollection values = Game.Data.ActiveProvinces.Values;
				Func<ProvinceData, bool> predicate = (ProvinceData x) => x.Owner == Realm.Name;
				List<ProvinceData> list = values.Where(predicate).ToList();
				List<string> list2 = new List<string>();
				foreach (ProvinceData item in list)
				{
					list2.Add(item.Name);
				}
				regionOutlineRenderer.LineWidth = -5f;
				regionOutlineRenderer.Update(list2);
				BorderOutlines.Add(regionOutlineRenderer);
			}
		}

		public void CreateBuffers()
		{
			int num = (int)Math.Ceiling((float)MapWidth / 508f);
			int num2 = (int)Math.Ceiling((float)MapHeight / 508f);
			Vertices = new MapVertex[num * num2 * 4];
			int num3 = 0;
			float[] array = new float[100];
			float[] array2 = new float[100];
			for (int i = 0; i < 100; i++)
			{
				array[i] = i * 512;
				array2[i] = i * 512;
			}
			for (int j = 0; j < num; j++)
			{
				for (int k = 0; k < num2; k++)
				{
					float x = array[j];
					float x2 = array[j + 1];
					float z = array2[k];
					float z2 = array2[k + 1];
					ref MapVertex reference = ref Vertices[num3++];
					reference = new MapVertex(new Vector3(x, 0f, z), new Vector2(0f, 0f), 1f);
					ref MapVertex reference2 = ref Vertices[num3++];
					reference2 = new MapVertex(new Vector3(x2, 0f, z), new Vector2(1f, 0f), 1f);
					ref MapVertex reference3 = ref Vertices[num3++];
					reference3 = new MapVertex(new Vector3(x, 0f, z2), new Vector2(0f, 1f), 1f);
					ref MapVertex reference4 = ref Vertices[num3++];
					reference4 = new MapVertex(new Vector3(x2, 0f, z2), new Vector2(1f, 1f), 1f);
				}
			}
			Indices = new short[num * num2 * 6];
			int num4 = 0;
			num3 = 0;
			for (int l = 0; l < num; l++)
			{
				for (int m = 0; m < num2; m++)
				{
					Indices[num4++] = (short)num3;
					Indices[num4++] = (short)(num3 + 1);
					Indices[num4++] = (short)(num3 + 2);
					Indices[num4++] = (short)(num3 + 1);
					Indices[num4++] = (short)(num3 + 3);
					Indices[num4++] = (short)(num3 + 2);
					num3 += 4;
				}
			}
			VB = new GLVertexBuffer(MapVertex.GetFormat(ShaderEffect.GetID()));
			VB.SetBufferData(Vertices, BufferUsageHint.StaticDraw);
			IB = new GLIndexBuffer();
			IB.SetBufferData(Indices, BufferUsageHint.StaticDraw);
			MapVertex[] data = new MapVertex[6]
			{
			new MapVertex(new Vector3(-1000f, 0f, -1000f), new Vector2(0f, 0f), 1f),
			new MapVertex(new Vector3(6000f, 0f, -1000f), new Vector2(10f, 0f), 1f),
			new MapVertex(new Vector3(-1000f, 0f, 6000f), new Vector2(0f, 10f), 1f),
			new MapVertex(new Vector3(6000f, 0f, -1000f), new Vector2(10f, 0f), 1f),
			new MapVertex(new Vector3(6000f, 0f, 6000f), new Vector2(10f, 10f), 1f),
			new MapVertex(new Vector3(-1000f, 0f, 6000f), new Vector2(0f, 10f), 1f)
			};
			MapBGBuffer = new GLVertexBuffer(MapVertex.GetFormat(ShaderEffect.GetID()));
			MapBGBuffer.SetBufferData(data, BufferUsageHint.StaticDraw);
		}

		public void LoadRealmTextures(string Foldername)
		{
			UnloadRealmTextures();
			LoadTextureSplit(ref RealmMapTextures, Foldername);
		}

		private void UnloadRealmTextures()
		{
			if (RealmMapTextures == null)
			{
				return;
			}
			for (int i = 0; i < RealmMapTextures.GetLength(0); i++)
			{
				for (int j = 0; j < RealmMapTextures.GetLength(1); j++)
				{
					RealmMapTextures[i, j].Dispose();
				}
			}
			RealmMapTextures = null;
		}

		private void LoadTextureSplit(ref GLTexture[,] Textures, string Foldername)
		{
			int num = (int)Math.Ceiling((float)MapWidth / 508f);
			int num2 = (int)Math.Ceiling((float)MapHeight / 508f);
			Textures = new GLTexture[num, num2];
			for (int i = 0; i < num; i++)
			{
				for (int j = 0; j < num2; j++)
				{
					Textures[i, j] = new GLTexture(File.OpenRead(Foldername + "\\Map_" + i + "_" + j + ".png"));
				}
			}
		}

		public void Dispose()
		{
			Disposed = true;
			if (VB != null)
			{
				VB.Dispose();
				VB = null;
			}
			if (IB != null)
			{
				IB.Dispose();
				IB = null;
			}
			UnloadRealmTextures();
			if (MeshRenderers == null)
			{
				return;
			}
			foreach (ProvinceMeshRenderer value in MeshRenderers.Values)
			{
				value.Dispose();
			}
			MeshRenderers.Clear();
			MeshRenderers = null;
		}

		public void Render(float ElapsedTime)
		{
			if (!ReadyToRender || VB == null)
			{
				return;
			}
			MapBGBuffer.SetActive();
			ShaderEffect.SetActive();
			MapBGTexture.SetActive(TextureUnit.Texture0);
			GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
			MapBGTexture.SetInactive(TextureUnit.Texture0);
			ShaderEffect.SetInactive();
			MapBGBuffer.SetInactive();
			VB.SetActive();
			IB.SetActive();
			ShaderEffect.SetActive();
			int num = 0;
			for (int i = 0; i < 7; i++)
			{
				for (int j = 0; j < 6; j++)
				{
					RealmMapTextures[i, j].SetActive(TextureUnit.Texture0);
					GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedShort, num * 2);
					num += 6;
					RealmMapTextures[i, j].SetInactive(TextureUnit.Texture0);
				}
			}
			ShaderEffect.SetInactive();
			IB.SetInactive();
			VB.SetInactive();
			if (HighlightOutlines.Count > 0)
			{
				AlphaTime += ElapsedTime * 5f;
				if ((double)AlphaTime > Math.PI * 2.0)
				{
					AlphaTime -= (float)Math.PI * 2f;
				}
				float num2 = (float)Math.Sin(AlphaTime);
				if (num2 < 0f)
				{
					num2 = 0f;
				}
				foreach (RegionOutlineRenderer highlightOutline in HighlightOutlines)
				{
					highlightOutline.Colour[3] = num2;
				}
			}
			if (BordersOnTop)
			{
				Game.Utilities.SpriteManager.Render();
				foreach (ProvinceMeshRenderer value in MeshRenderers.Values)
				{
					value.Render(ElapsedTime);
				}
				RenderBorders();
			}
			else
			{
				RenderBorders();
				Game.Utilities.SpriteManager.Render();
				foreach (ProvinceMeshRenderer value2 in MeshRenderers.Values)
				{
					value2.Render(ElapsedTime);
				}
			}
			if (Arrow != null)
			{
				Arrow.Render();
			}
		}

		private void RenderBorders()
		{
			if (BorderOutlines != null)
			{
				foreach (RegionOutlineRenderer borderOutline in BorderOutlines)
				{
					borderOutline.Render();
				}
			}
			if (RealmBorders != null)
			{
				foreach (RegionOutlineRenderer value in RealmBorders.Values)
				{
					value.Render();
				}
			}
			if (RealmSelectOutline != null)
			{
				RealmSelectOutline.Render();
			}
			foreach (RegionOutlineRenderer highlightOutline in HighlightOutlines)
			{
				highlightOutline.Render();
			}
		}

		internal void SetArrowColour(Color Col)
		{
			Arrow.SetColour(Col);
		}

		internal void ShowArrow(List<Vector3> PathPoints)
		{
			ClearArrow();
			Arrow = new MapArrow(PathPoints, Game);
		}

		internal void ClearArrow()
		{
			if (Arrow != null)
			{
				Arrow.Dispose();
				Arrow = null;
			}
		}

		internal void SetHighlight(string RegionName)
		{
			if (Disposed)
			{
				return;
			}
			foreach (ProvinceMeshRenderer value in MeshRenderers.Values)
			{
				value.HighlightActive = value.RegionName == RegionName;
			}
		}

		internal void ClearHighlight()
		{
			if (MeshRenderers != null)
			{
				foreach (ProvinceMeshRenderer value in MeshRenderers.Values)
				{
					value.HighlightActive = false;
				}
			}
			if (RealmSelectOutline != null)
			{
				RealmSelectOutline.Dispose();
				RealmSelectOutline = null;
			}
		}

		internal void SetProvinceHighlight(string ProvinceName)
		{
			if (RealmSelectOutline != null)
			{
				RealmSelectOutline.Dispose();
			}
			List<string> list = new List<string>();
			list.Add(ProvinceName);
			RealmSelectOutline = new RegionOutlineRenderer(Game);
			RealmSelectOutline.LineWidth = -5f;
			RealmSelectOutline.Colour = new Vector4(0.8f, 0.8f, 0f, 1f);
			RealmSelectOutline.Update(list);
		}

		internal void SetRealmHighlight(string RealmName)
		{
			List<ProvinceData> list = null;
			if (Game.CurrentGame == null)
			{
				list = Game.Data.ActiveProvinces.Values.Where((ProvinceData x) => x.Owner == RealmName).ToList();
			}
			else
			{
				list = new List<ProvinceData>();
				foreach (WorkingProvince province in Game.CurrentGame.GetRealm(RealmName).Provinces)
				{
					list.Add(Game.Data.ActiveProvinces[province.Name]);
				}
			}
			if (MeshRenderers == null)
			{
				return;
			}
			foreach (ProvinceMeshRenderer Mesh in MeshRenderers.Values)
			{
				ProvinceMeshRenderer provinceMeshRenderer = Mesh;
				List<ProvinceData> source = list;
				Func<ProvinceData, bool> predicate = (ProvinceData x) => x.Name == Mesh.RegionName;
				provinceMeshRenderer.HighlightActive = source.Count(predicate) > 0;
			}
			if (RealmSelectOutline != null)
			{
				RealmSelectOutline.Dispose();
			}
			List<string> list2 = new List<string>();
			foreach (ProvinceData item in list)
			{
				list2.Add(item.Name);
			}
			RealmSelectOutline = new RegionOutlineRenderer(Game);
			RealmSelectOutline.LineWidth = -7f;
			RealmSelectOutline.Colour = new Vector4(0.8f, 0.8f, 0f, 1f);
			RealmSelectOutline.Update(list2);
		}

		internal void SetBorderColour(string RealmName, Color NewColour)
		{
			SetBorderColour(RealmName, new Vector4
			{
				X = (float)(int)NewColour.R / 255f,
				Y = (float)(int)NewColour.G / 255f,
				Z = (float)(int)NewColour.B / 255f,
				W = (float)(int)NewColour.A / 255f
			});
		}

		internal void SetBorderColour(string RealmName, Vector4 NewColour)
		{
			if (Game.CurrentGame == null)
			{
				return;
			}
			if (RealmBorders == null)
			{
				RealmBorders = new Dictionary<string, RegionOutlineRenderer>();
			}
			WorkingRealm realm = Game.CurrentGame.GetRealm(RealmName);
			if (realm != null)
			{
				RegionOutlineRenderer value = null;
				RealmBorders.TryGetValue(RealmName, out value);
				if (value != null)
				{
					value.Colour = NewColour;
				}
			}
		}

		internal void SetLiveRealmColour(string RealmName, Color Colour)
		{
			if (MeshRenderers == null)
			{
				return;
			}
			List<WorkingProvince> source = Game.CurrentGame.AllProvinces.Values.Where((WorkingProvince x) => x.OwnerRealm.Name == RealmName).ToList();
			Vector4 colour = new Vector4
			{
				X = (float)(int)Colour.R / 255f,
				Y = (float)(int)Colour.G / 255f,
				Z = (float)(int)Colour.B / 255f,
				W = (float)(int)Colour.A / 255f
			};
			foreach (ProvinceMeshRenderer Mesh in MeshRenderers.Values)
			{
				Func<WorkingProvince, bool> predicate = (WorkingProvince x) => x.Name == Mesh.RegionName;
				if (source.Count(predicate) > 0)
				{
					Mesh.SetColour(colour);
				}
			}
		}

		internal void SetRealmColour(string RealmName, Color Colour)
		{
			List<ProvinceData> source = Game.Data.ActiveProvinces.Values.Where((ProvinceData x) => x.Owner == RealmName).ToList();
			Vector4 colour = new Vector4
			{
				X = (float)(int)Colour.R / 255f,
				Y = (float)(int)Colour.G / 255f,
				Z = (float)(int)Colour.B / 255f,
				W = (float)(int)Colour.A / 255f
			};
			foreach (ProvinceMeshRenderer Mesh in MeshRenderers.Values)
			{
				Func<ProvinceData, bool> predicate = (ProvinceData x) => x.Name == Mesh.RegionName;
				if (source.Count(predicate) > 0)
				{
					Mesh.SetColour(colour);
				}
			}
		}

		internal void SetProvinceColour(string ProvinceName, Color Colour)
		{
			ProvinceData provinceData = Game.Data.ActiveProvinces.Values.SingleOrDefault((ProvinceData x) => x.Name == ProvinceName);
			Vector4 colour = new Vector4
			{
				X = (float)(int)Colour.R / 255f,
				Y = (float)(int)Colour.G / 255f,
				Z = (float)(int)Colour.B / 255f,
				W = (float)(int)Colour.A / 255f
			};
			if (provinceData == null)
			{
				return;
			}
			foreach (ProvinceMeshRenderer value in MeshRenderers.Values)
			{
				if (provinceData.Name == value.RegionName)
				{
					value.SetColour(colour);
				}
			}
		}

		internal void SetZoneColour(string ZoneName, Color Colour)
		{
			SeaZoneData seaZoneData = Game.Data.ActiveSeaZones.Values.SingleOrDefault((SeaZoneData x) => x.Name == ZoneName);
			Vector4 colour = new Vector4
			{
				X = (float)(int)Colour.R / 255f,
				Y = (float)(int)Colour.G / 255f,
				Z = (float)(int)Colour.B / 255f,
				W = (float)(int)Colour.A / 255f
			};
			if (seaZoneData == null)
			{
				return;
			}
			foreach (ProvinceMeshRenderer value in MeshRenderers.Values)
			{
				if (seaZoneData.Name == value.RegionName)
				{
					value.SetColour(colour);
				}
			}
		}

		internal void SetBorder(string RealmName, Color AllyColour, float BorderWidth)
		{
			RegionOutlineRenderer value = null;
			RealmBorders.TryGetValue(RealmName, out value);
			SetBorderColour(RealmName, AllyColour);
			if (value == null)
			{
				return;
			}
			value.LineWidth = BorderWidth;
			List<ProvinceData> list = Game.Data.ActiveProvinces.Values.Where((ProvinceData x) => x.Owner == RealmName).ToList();
			List<string> list2 = new List<string>();
			foreach (ProvinceData item in list)
			{
				list2.Add(item.Name);
			}
			value.Update(list2);
		}

		internal void SetLiveBorder(string RealmName, Color AllyColour, float BorderWidth)
		{
			RegionOutlineRenderer value = null;
			RealmBorders.TryGetValue(RealmName, out value);
			SetBorderColour(RealmName, AllyColour);
			if (value == null)
			{
				return;
			}
			value.LineWidth = BorderWidth;
			List<WorkingProvince> list = Game.CurrentGame.AllProvinces.Values.Where((WorkingProvince x) => x.OwnerRealm.Name == RealmName).ToList();
			List<string> list2 = new List<string>();
			foreach (WorkingProvince item in list)
			{
				list2.Add(item.Name);
			}
			value.Update(list2);
		}
	}
}