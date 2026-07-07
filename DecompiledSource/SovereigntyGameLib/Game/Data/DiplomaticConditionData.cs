using System;
using System.IO;

namespace SovereigntyTK.Game.Data
{
	public class DiplomaticConditionData : BaseData
	{
		[EditorData("Name", EditorTypes.Text)]
		[PrimaryKey(1)]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("name")]
		public string ConditionName { get; set; }

		[EditorData("Localised Name", EditorTypes.Text)]
		[DataName("displayname")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string DisplayName { get; set; }

		[DataConverter(typeof(GeneralFloatConverter))]
		[DataName("effect")]
		[EditorData("Effect Per Turn", EditorTypes.Text)]
		public float DispositionEffect { get; set; }

		[DataConverter(typeof(GeneralFloatConverter))]
		[DataName("maximum")]
		[EditorData("Maximum Effect", EditorTypes.Text)]
		public float MaximumEffect { get; set; }

		[DataName("minimum")]
		[EditorData("Minimum Effect", EditorTypes.Text)]
		[DataConverter(typeof(GeneralFloatConverter))]
		public float MinimumEffect { get; set; }

		[DataConverter(typeof(GeneralFloatConverter))]
		[DataName("decay")]
		[EditorData("Decay Per Turn", EditorTypes.Text)]
		public float DecayRate { get; set; }

		public override string ToString()
		{
			return this.ConditionName;
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.ConditionName);
			w.Write(this.DisplayName);
			w.Write(this.DispositionEffect);
			w.Write(this.MaximumEffect);
			w.Write(this.MinimumEffect);
			w.Write(this.DecayRate);
		}

		internal void Load(BinaryReader r)
		{
			this.ConditionName = r.ReadString();
			this.DisplayName = r.ReadString();
			this.DispositionEffect = r.ReadSingle();
			this.MaximumEffect = r.ReadSingle();
			this.MinimumEffect = r.ReadSingle();
			this.DecayRate = r.ReadSingle();
		}

		public DiplomaticConditionData Clone()
		{
			return new DiplomaticConditionData
			{
				ConditionName = this.ConditionName,
				DisplayName = this.DisplayName,
				DispositionEffect = this.DispositionEffect,
				MaximumEffect = this.MaximumEffect,
				MinimumEffect = this.MinimumEffect,
				DecayRate = this.DecayRate
			};
		}

		public bool DoNotSave;
	}
}
