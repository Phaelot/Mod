using System;
using SovereigntyTK.Game;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionLearnSpell : AIAction
	{
		public AIActionLearnSpell(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
		}

		public override void Execute()
		{
			this.AI.Realm.MagicData.LearnSpell(this.Spell.Name);
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		public RealmMagicData Spell;
	}
}
