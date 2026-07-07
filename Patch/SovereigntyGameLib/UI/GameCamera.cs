// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.UI.GameCamera
using System;
using System.Diagnostics;
using System.Drawing;
using OpenTK;
using SovereigntyTK;
using SovereigntyTK.UI;

namespace SovereigntyTK.UI
{
	public class GameCamera : CameraBase
	{
		private float ScrollSpeedMin = 0.5f;

		private float ScrollSpeedMax = 20f;

		private float ScrollSpeedAccelMax = 40f;

		private float ScrollSpeedAccelMin = 5f;

		private float ScrollSpeedLeft;

		private float ScrollSpeedRight;

		private float ScrollSpeedUp;

		private float ScrollSpeedDown;

		public Vector3 CamPos;

		private Vector3 AutoMoveStart;

		private Vector3 AutoMoveTarget;

		private float AutoMoveElapsed;

		private float AutoMoveTotal;

		public bool DoingAutoMove;

		public bool Forward;

		public bool Back;

		public bool Left;

		public bool Right;

		public bool Up;

		public bool Down;

		public bool Updated;

		private Stopwatch UpdateTimer;

		private GameBase Game;

		internal RectangleF Bounds;

		private float m_MinZoom;

		public float ZoomStep = 100f;

		public bool TrackingMovement;

		public bool LeftActive
		{
			get
			{
				if (Left)
				{
					return true;
				}
				if (Game.UIManager.LastMousePos.X == -1)
				{
					return false;
				}
				if (Game.UIManager.LastMousePos.X <= 10)
				{
					return true;
				}
				return false;
			}
		}

		public bool RightActive
		{
			get
			{
				if (Right)
				{
					return true;
				}
				if (Game.UIManager.LastMousePos.X == -1)
				{
					return false;
				}
				if (Game.UIManager.LastMousePos.X >= Game.Window.ClientRectangle.Width - 10)
				{
					return true;
				}
				return false;
			}
		}

		public bool ForwardActive
		{
			get
			{
				if (Forward)
				{
					return true;
				}
				if (Game.UIManager.LastMousePos.X == -1)
				{
					return false;
				}
				if (Game.UIManager.LastMousePos.Y <= 10)
				{
					return true;
				}
				return false;
			}
		}

		public bool BackActive
		{
			get
			{
				if (Back)
				{
					return true;
				}
				if (Game.UIManager.LastMousePos.X == -1)
				{
					return false;
				}
				if (Game.UIManager.LastMousePos.Y >= Game.Window.ClientRectangle.Height - 10)
				{
					return true;
				}
				return false;
			}
		}

		public float MinZoomLevel
		{
			get
			{
				return m_MinZoom;
			}
			set
			{
				m_MinZoom = value;
				ClampZoom();
			}
		}

		public event Action AutoMoveCompleted;

		public GameCamera(GameBase Game)
		{
			this.Game = Game;
			CamPos = new Vector3(1672f, 1700f, 1272f);
			UpdateTimer = new Stopwatch();
			UpdateTimer.Start();
			WorldMatrix = Matrix4.Identity;
			Update();
		}

		public void Reset()
		{
			CamPos = new Vector3(1672f, 10000f, 1272f);
			ClampZoom();
			Update();
		}

		public void SetBounds(RectangleF Bounds)
		{
			this.Bounds = Bounds;
			Clamp();
		}

		public void BeginAutoMove(Vector3 Target, float Time)
		{
			DoingAutoMove = true;
			AutoMoveElapsed = 0f;
			AutoMoveTotal = Time;
			AutoMoveTarget = Target;
			AutoMoveStart = CamPos;
		}

		public RectangleF GetViewRect()
		{
			Rectangle viewport = Game.GetViewport();
			Vector3 terrainIntersect = GetTerrainIntersect(0, 0);
			Vector3 terrainIntersect2 = GetTerrainIntersect(viewport.Right, viewport.Bottom);
			float width = terrainIntersect2.X - terrainIntersect.X;
			float num = terrainIntersect.Z - terrainIntersect2.Z;
			return new RectangleF(terrainIntersect.X, terrainIntersect.Z, width, 0f - num);
		}

		private void Clamp()
		{
			if (float.IsNaN(CamPos.X))
			{
				throw new Exception("Invalid Camera Position");
			}
			CalculateMatrices();
			Rectangle viewport = Game.GetViewport();
			Vector3 terrainIntersect = GetTerrainIntersect(0, 0);
			Vector3 terrainIntersect2 = GetTerrainIntersect(viewport.Right, viewport.Bottom);
			float num = terrainIntersect2.X - terrainIntersect.X;
			float num2 = terrainIntersect.Z - terrainIntersect2.Z;
			float num3 = Bounds.Left + num / 2f;
			float num4 = Bounds.Right - num / 2f;
			float num5 = Bounds.Top - num2 / 2f;
			float num6 = Bounds.Bottom + num2 / 2f;
			if (CamPos.X < num3)
			{
				Left = false;
				ScrollSpeedLeft = 0f;
				CamPos.X = num3;
				CalculateMatrices();
			}
			if (CamPos.X > num4)
			{
				Right = false;
				ScrollSpeedRight = 0f;
				CamPos.X = num4;
				CalculateMatrices();
			}
			if (CamPos.Z < num5)
			{
				Up = false;
				ScrollSpeedUp = 0f;
				CamPos.Z = num5;
				CalculateMatrices();
			}
			if (CamPos.Z > num6)
			{
				Down = false;
				ScrollSpeedDown = 0f;
				CamPos.Z = num6;
				CalculateMatrices();
			}
		}

		public Vector4 UnProject(ref Matrix4 projection, ref Matrix4 view, Size viewport, Vector3 mouse)
		{
			Vector4 vec = default(Vector4);
			vec.X = 2f * mouse.X / (float)viewport.Width - 1f;
			vec.Y = 0f - (2f * mouse.Y / (float)viewport.Height - 1f);
			vec.Z = mouse.Z;
			vec.W = 1f;
			Matrix4 mat = Matrix4.Invert(view);
			Matrix4 mat2 = Matrix4.Invert(projection);
			Vector4.Transform(ref vec, ref mat2, out vec);
			Vector4.Transform(ref vec, ref mat, out vec);
			if (vec.W > float.Epsilon || vec.W < float.Epsilon)
			{
				vec.X /= vec.W;
				vec.Y /= vec.W;
				vec.Z /= vec.W;
			}
			return vec;
		}

		public Vector3 GetTerrainIntersect(int ScreenX, int ScreenY)
		{
			Rectangle viewport = Game.GetViewport();
			Vector3 mouse = new Vector3(ScreenX, ScreenY, 0f);
			Vector3 mouse2 = new Vector3(ScreenX, ScreenY, 1f);
			mouse = UnProject(ref ProjectionMatrix, ref ViewMatrix, viewport.Size, mouse).Xyz;
			mouse2 = UnProject(ref ProjectionMatrix, ref ViewMatrix, viewport.Size, mouse2).Xyz;
			Vector3 result = mouse;
			Vector3 vector = Vector3.Subtract(mouse2, mouse);
			vector.Normalize();
			vector *= 1f;
			result += vector * (CamPos.Y * 0.75f);
			while (result.Y > 0f)
			{
				result += vector;
				if (0f > result.Y)
				{
					return result;
				}
			}
			return new Vector3(-10000f, -10000f, -10000f);
		}

		public void ViewportChanged()
		{
			ClampZoom();
			Clamp();
		}

		public void Update()
		{
			Updated = false;
			bool flag = false;
			float num = CamPos.Y / 1800f;
			num = (ScrollSpeedMax - ScrollSpeedMin) * num + ScrollSpeedMin;
			float num2 = CamPos.Y / 1800f;
			num2 = (ScrollSpeedAccelMax - ScrollSpeedAccelMin) * num2 + ScrollSpeedAccelMin;
			UpdateTimer.Stop();
			float val = (float)UpdateTimer.Elapsed.TotalSeconds;
			UpdateTimer.Reset();
			UpdateTimer.Start();
			val = Math.Min(val, 0.016f);
			float num3 = 0f;
			if (DoingAutoMove)
			{
				AutoMoveElapsed += val;
				if (AutoMoveElapsed >= AutoMoveTotal)
				{
					AutoMoveElapsed = AutoMoveTotal;
					DoingAutoMove = false;
					if (this.AutoMoveCompleted != null)
					{
						this.AutoMoveCompleted();
					}
				}
				Vector3 vector = AutoMoveTarget - AutoMoveStart;
				float length = vector.Length;
				if (length < 0.1f)
				{
					AutoMoveElapsed = AutoMoveTotal;
					DoingAutoMove = false;
					if (this.AutoMoveCompleted != null)
					{
						this.AutoMoveCompleted();
					}
				}
				else
				{
					float num4 = AutoMoveElapsed / AutoMoveTotal * length;
					vector.Normalize();
					vector *= num4;
					CamPos = AutoMoveStart + vector;
					if (float.IsNaN(CamPos.X))
					{
						throw new Exception("Invalid camera location");
					}
					flag = true;
				}
			}
			else
			{
				if (LeftActive)
				{
					ScrollSpeedLeft += num2 * val;
					if (ScrollSpeedLeft > num)
					{
						ScrollSpeedLeft = num;
					}
				}
				else
				{
					ScrollSpeedLeft -= num2 * val;
					if (ScrollSpeedLeft < 0f)
					{
						ScrollSpeedLeft = 0f;
					}
				}
				if (ScrollSpeedLeft != 0f)
				{
					CamPos.X -= ScrollSpeedLeft;
					num3 += ScrollSpeedLeft;
					flag = true;
				}
				if (RightActive)
				{
					ScrollSpeedRight += num2 * val;
					if (ScrollSpeedRight > num)
					{
						ScrollSpeedRight = num;
					}
				}
				else
				{
					ScrollSpeedRight -= num2 * val;
					if (ScrollSpeedRight < 0f)
					{
						ScrollSpeedRight = 0f;
					}
				}
				if (ScrollSpeedRight != 0f)
				{
					CamPos.X += ScrollSpeedRight;
					num3 += ScrollSpeedRight;
					flag = true;
				}
				if (ForwardActive)
				{
					ScrollSpeedUp += num2 * val;
					if (ScrollSpeedUp > num)
					{
						ScrollSpeedUp = num;
					}
				}
				else
				{
					ScrollSpeedUp -= num2 * val;
					if (ScrollSpeedUp < 0f)
					{
						ScrollSpeedUp = 0f;
					}
				}
				if (ScrollSpeedUp != 0f)
				{
					CamPos.Z -= ScrollSpeedUp;
					num3 += ScrollSpeedUp;
					flag = true;
				}
				if (BackActive)
				{
					ScrollSpeedDown += num2 * val;
					if (ScrollSpeedDown > num)
					{
						ScrollSpeedDown = num;
					}
				}
				else
				{
					ScrollSpeedDown -= num2 * val;
					if (ScrollSpeedDown < 0f)
					{
						ScrollSpeedDown = 0f;
					}
				}
				if (ScrollSpeedDown != 0f)
				{
					CamPos.Z += ScrollSpeedDown;
					num3 += ScrollSpeedDown;
					flag = true;
				}
				if (Up)
				{
					CamPos.Y += ZoomStep;
					ClampZoom();
					flag = true;
				}
				if (Down)
				{
					CamPos.Y -= ZoomStep;
					ClampZoom();
					flag = true;
				}
			}
			if (flag)
			{
				if (TrackingMovement && num3 > 0f)
				{
					Game.FireEvent("CameraMoved", num3);
				}
				Updated = true;
				Clamp();
			}
			CalculateMatrices();
			Up = false;
			Down = false;
		}

		private void ClampZoom()
		{
			if (CamPos.Y < MinZoomLevel)
			{
				CamPos.Y = MinZoomLevel;
			}
			CalculateMatrices();
			Rectangle viewport = Game.GetViewport();
			Vector3 terrainIntersect = GetTerrainIntersect(0, 0);
			Vector3 terrainIntersect2 = GetTerrainIntersect(viewport.Right, viewport.Bottom);
			float num = terrainIntersect2.X - terrainIntersect.X;
			float num2 = terrainIntersect.Z - terrainIntersect2.Z;
			if ((num > Bounds.Width || num2 > Bounds.Height) && CamPos.Y > MinZoomLevel)
			{
				CamPos.Y -= 10f;
				ClampZoom();
			}
		}

		private void CalculateMatrices()
		{
			ViewMatrix = Matrix4.LookAt(target: new Vector3(CamPos.X, 0f, CamPos.Z), up: new Vector3(0f, 0f, -1f), eye: CamPos);
			ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4f, (float)Game.GetViewport().Width / (float)Game.GetViewport().Height, 1f, 10000f);
			Game.Utilities.ShaderManager.UpdateCameraMatrices(this);
			HandleViewMatrixChanged();
		}

		internal void ZoomOutMaximum()
		{
			CamPos.Y = 10000f;
			ClampZoom();
		}

		internal void ZoomOut()
		{
			Up = true;
		}

		internal void ZoomIn()
		{
			Down = true;
		}

		internal void SetPosition(float X, float Y, float Z)
		{
			float length = (new Vector3(X, Y, Z) - CamPos).Length;
			CamPos = new Vector3(X, Y, Z);
			if (float.IsNaN(CamPos.X))
			{
				throw new Exception("Invalid camera location");
			}
			Clamp();
			if (TrackingMovement)
			{
				Game.FireEvent("CameraMoved", length);
			}
		}

		public void SetPositionPct(float X, float Y)
		{
			X *= 3344f;
			Y *= 2544f;
			SetPosition(X, CamPos.Y, Y);
		}
	}
}