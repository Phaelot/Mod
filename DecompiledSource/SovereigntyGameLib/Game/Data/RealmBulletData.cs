using System;

namespace SovereigntyTK.Game.Data
{
	public class RealmBulletData : BaseData
	{
		[PrimaryKey(1)]
		[DataName("realm")]
		[DataBinding("RlmStats", "Name", false)]
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Realm", EditorTypes.DropDown)]
		public string Realm { get; set; }

		[DataName("bullet_text")]
		[EditorData("Localised Bullet Name", EditorTypes.Text)]
		[DataConverter(typeof(TextIndexConverter))]
		public string BulletName { get; set; }

		[DataName("bullet_exp")]
		[EditorData("Localise Bullet Value", EditorTypes.Text)]
		[DataConverter(typeof(TextIndexConverter))]
		public string BulletText { get; set; }

		public override string ToString()
		{
			return this.Realm + "." + this.BulletName;
		}
	}
}
