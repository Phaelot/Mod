using System;
using System.IO;

namespace SovereigntyTK.Game.Data
{
	public class DiplomaticEventData : BaseData
	{
		[DataName("name")]
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Name", EditorTypes.Text)]
		[PrimaryKey(1)]
		public string EventName { get; set; }

		[DataName("displayname")]
		[EditorData("Localised Name", EditorTypes.Text)]
		[DataConverter(typeof(GeneralStringConverter))]
		public string DisplayName { get; set; }

		[DataName("effect")]
		[DataConverter(typeof(GeneralFloatConverter))]
		[EditorData("Disposition Effect", EditorTypes.Text)]
		public float DispositionEffect { get; set; }

		[EditorData("Decay Per Turn", EditorTypes.Text)]
		[DataConverter(typeof(GeneralFloatConverter))]
		[DataName("decay")]
		public float DecayRate { get; set; }

		[DataName("stack")]
		[EditorData("Stacking Mode", EditorTypes.DropDownEnum)]
		[DataConverter(typeof(StackModeConverter))]
		public DiplomaticStackModes StackMode { get; set; }

		[DataConverter(typeof(YesNoConverter))]
		[DataName("expires")]
		[EditorData("Expires", EditorTypes.Boolean)]
		public bool Expires { get; set; }

		public override string ToString()
		{
			return this.EventName;
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.EventName);
			w.Write(this.DisplayName);
			w.Write(this.DispositionEffect);
			w.Write(this.DecayRate);
			w.Write((short)this.StackMode);
			w.Write(this.Expires);
		}

		internal void Load(BinaryReader r)
		{
			this.EventName = r.ReadString();
			this.DisplayName = r.ReadString();
			this.DispositionEffect = r.ReadSingle();
			this.DecayRate = r.ReadSingle();
			this.StackMode = (DiplomaticStackModes)r.ReadInt16();
			this.Expires = r.ReadBoolean();
		}
	}
}
