using System;
using OpenTK;

namespace SovereigntyTK.UI
{
	public class CameraBase
	{
		public event Action ViewMatrixChanged;

		protected void HandleViewMatrixChanged()
		{
			if (this.ViewMatrixChanged != null)
			{
				this.ViewMatrixChanged();
			}
		}

		public Matrix4 WorldMatrix;

		public Matrix4 ViewMatrix;

		public Matrix4 ProjectionMatrix;
	}
}
