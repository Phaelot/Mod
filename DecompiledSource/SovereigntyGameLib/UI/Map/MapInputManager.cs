// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.UI.Map.MapInputManager
using System.Collections.Generic;
using OpenTK;
using OpenTK.Input;
using SovereigntyTK;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.UI.Map
{
	public class MapInputManager
	{
		public Sovereignty Game;

		private List<MouseButton> ActiveButtons;

		private bool Dragging;

		private Vector3 DragMapStart;

		private Vector2 DragMouseStart;

		private Vector3 DragCameraStart;

		private float DragMinDistance = 10f;

		private float MapDistPerPixelX;

		private float MapDistPerPixelY;

		public MapInputManager(Sovereignty Game)
		{
			this.Game = Game;
			ActiveButtons = new List<MouseButton>();
		}

		internal void HandleMouseMove(MouseMoveEventArgs e)
		{
			Vector3 terrainIntersect = Game.Camera.GetTerrainIntersect(e.X, e.Y);
			if (!Dragging && ActiveButtons.Contains(MouseButton.Left))
			{
				Vector2 vector = new Vector2(e.X, e.Y);
				if ((vector - DragMouseStart).Length > DragMinDistance)
				{
					Dragging = true;
				}
			}
			if (Dragging && ActiveButtons.Contains(MouseButton.Left))
			{
				Vector2 vector2 = new Vector2(e.X, e.Y);
				Vector2 vector3 = vector2 - DragMouseStart;
				vector3.X *= MapDistPerPixelX;
				vector3.Y *= MapDistPerPixelY;
				Game.Camera.SetPosition(DragCameraStart.X - vector3.X, Game.Camera.CamPos.Y, DragCameraStart.Z - vector3.Y);
			}
			string provinceAtPoint = Game.Data.GetProvinceAtPoint(terrainIntersect);
			Game.Map.SetMouseoverRegion(provinceAtPoint);
			ActivePathNode nearestNode = GetNearestNode(provinceAtPoint, terrainIntersect);
			Game.Map.SetNearestNode(nearestNode);
		}

		private ActivePathNode GetNearestNode(string ProvinceName, Vector3 MapPoint)
		{
			if (Game.CurrentGame == null)
			{
				return null;
			}
			WorkingProvince province = Game.CurrentGame.GetProvince(ProvinceName);
			if (province != null)
			{
				if (province.HarbourNode == null)
				{
					return province.LandNode;
				}
				Vector3 vector = new Vector3(province.LandNode.MapCoords.X, 0f, province.LandNode.MapCoords.Y);
				Vector3 vector2 = new Vector3(province.HarbourNode.MapCoords.X, 0f, province.HarbourNode.MapCoords.Y);
				float length = (MapPoint - vector).Length;
				float length2 = (MapPoint - vector2).Length;
				if (length > length2)
				{
					return province.HarbourNode;
				}
				return province.LandNode;
			}
			WorkingZone zone = Game.CurrentGame.GetZone(ProvinceName);
			if (zone != null)
			{
				float num = float.MaxValue;
				ActivePathNode result = null;
				{
					foreach (ActivePathNode node in zone.Nodes)
					{
						Vector3 vector3 = new Vector3(node.MapCoords.X, 0f, node.MapCoords.Y);
						float length3 = (vector3 - MapPoint).Length;
						if (length3 < num)
						{
							num = length3;
							result = node;
						}
					}
					return result;
				}
			}
			return null;
		}

		internal void HandleMouseDown(MouseButtonEventArgs e)
		{
			if (!ActiveButtons.Contains(e.Button))
			{
				ActiveButtons.Add(e.Button);
				Vector3 terrainIntersect = Game.Camera.GetTerrainIntersect(e.X, e.Y);
				DragMapStart = terrainIntersect;
				DragMouseStart = new Vector2(e.X, e.Y);
				DragCameraStart = Game.Camera.CamPos;
				Vector3 terrainIntersect2 = Game.Camera.GetTerrainIntersect(0, 0);
				Vector3 terrainIntersect3 = Game.Camera.GetTerrainIntersect(50, 50);
				MapDistPerPixelX = (terrainIntersect3.X - terrainIntersect2.X) / 50f;
				MapDistPerPixelY = (terrainIntersect3.Z - terrainIntersect2.Z) / 50f;
			}
		}

		internal void ForceMouseUp(MouseButton Button)
		{
			if (Button == MouseButton.Left && ActiveButtons.Contains(Button))
			{
				ActiveButtons.Remove(Button);
				Dragging = false;
			}
		}

		internal void HandleMouseUp(MouseButtonEventArgs e)
		{
			if (ActiveButtons.Contains(e.Button))
			{
				ActiveButtons.Remove(e.Button);
				if (!Dragging)
				{
					Vector3 terrainIntersect = Game.Camera.GetTerrainIntersect(e.X, e.Y);
					string provinceAtPoint = Game.Data.GetProvinceAtPoint(terrainIntersect);
					Game.Map.HandleProvinceClicked(provinceAtPoint, e.Button);
				}
				if (e.Button == MouseButton.Left)
				{
					Dragging = false;
				}
			}
		}
	}
}