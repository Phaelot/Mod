using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class SlidingCounter
	{
		public SlidingCounter(SovereigntyGame Game, WorkingUnit Unit, List<Point> Points, string IconName, float Speed)
		{
			this.Game = Game;
			this.Units = new List<int>();
			this.Points = Points;
			this.Speed = Speed;
			this.CounterSprite = Game.GameCore.Utilities.SpriteManager.CreateIndexedSprite("Data\\Images\\map\\mapicons.png", IconName, false);
			this.AddUnit(Unit);
			this.BeginNextMove();
		}

		public void AddUnit(WorkingUnit Unit)
		{
			this.Units.Add(Unit.ID);
			this.CalculateSize();
		}

		public void RemoveUnit(WorkingUnit Unit)
		{
			this.Units.Remove(Unit.ID);
			this.CalculateSize();
		}

		private void CalculateSize()
		{
			float num = 72f;
			float num2 = 36f;
			float num3 = num - num2;
			float num4 = num3 / 20f * (float)this.Units.Count + num2;
			if (this.Units.Count == 0)
			{
				num4 = 0f;
			}
			this.CounterSprite.SetSize(num4, num4);
		}

		public void BeginNextMove()
		{
			if (this.Points == null || this.Points.Count < 2 || this.CurrentPoint < 0 || this.CurrentPoint + 1 >= this.Points.Count)
			{
				this.Finished = true;
				return;
			}
			Point point = this.Points[this.CurrentPoint];
			Point point2 = this.Points[this.CurrentPoint + 1];
			this.CurrentPosition = new Vector3((float)point.X, 0f, (float)point.Y);
			this.TargetPosition = new Vector3((float)point2.X, 0f, (float)point2.Y);
			this.DirVector = this.TargetPosition - this.CurrentPosition;
			this.TotalDist = this.DirVector.Length;
			if (this.TotalDist <= 0.001f)
			{
				this.CurrentPoint++;
				if (this.CurrentPoint >= this.Points.Count - 1)
				{
					this.Finished = true;
				}
				else
				{
					this.BeginNextMove();
				}
				return;
			}
			this.DirVector.Normalize();
			this.DistCovered = 0f;
		}

		public void Dispose()
		{
			if (this.CounterSprite == null)
			{
				return;
			}
			this.CounterSprite.Dispose(false);
			this.CounterSprite = null;
		}

		public void Update(float ElapsedTime)
		{
			if (this.Finished || this.CounterSprite == null)
			{
				return;
			}
			this.CurrentPosition += this.DirVector * ElapsedTime * this.Speed;
			this.DistCovered += ElapsedTime * this.Speed;
			if (this.DistCovered >= this.TotalDist)
			{
				this.CurrentPosition = this.TargetPosition;
				this.CurrentPoint++;
				if (this.CurrentPoint == this.Points.Count - 1)
				{
					this.Finished = true;
				}
				else
				{
					this.BeginNextMove();
				}
			}
			this.CounterSprite.SetPosition(this.CurrentPosition.X, this.CurrentPosition.Z);
		}

		public GLSprite CounterSprite;

		public List<int> Units;

		public Vector3 CurrentPosition;

		public Vector3 TargetPosition;

		public Vector3 DirVector;

		public float Speed;

		public List<Point> Points;

		private int CurrentPoint;

		private float TotalDist;

		private float DistCovered;

		public bool Finished;

		private SovereigntyGame Game;
	}
}
