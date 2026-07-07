using System;

namespace SovereigntyTK.Game.Data
{
	public class AnimationData : BaseData
	{
		[DataName("name")]
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Name", EditorTypes.Text)]
		[PrimaryKey(1)]
		public string Name { get; set; }

		[DataName("texturename")]
		[EditorData("Image File", EditorTypes.Text)]
		[DataConverter(typeof(GeneralStringConverter))]
		public string TextureName { get; set; }

		[DataName("framesx")]
		[EditorData("Horizontal Frames", EditorTypes.Text)]
		[DataConverter(typeof(GeneralIntConverter))]
		public int FramesX { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Bertical Frames", EditorTypes.Text)]
		[DataName("framesy")]
		public int FramesY { get; set; }

		[EditorData("Frame count", EditorTypes.Text)]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("framecount")]
		public int FrameCount { get; set; }

		[DataConverter(typeof(GeneralFloatConverter))]
		[DataName("length")]
		[EditorData("Duration(s)", EditorTypes.Text)]
		public float Time { get; set; }

		public override string ToString()
		{
			return this.Name;
		}
	}
}
