using System;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.UI;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionCastSpell : AIAction
	{
		public AIActionCastSpell(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
		}

		public override void Execute()
		{
			this.Game.FinishSpellCasting(this.Spell, this.Target);
			GameText gameText = GameText.CreateLocalised("SPELLCASTMSG", new object[0]);
			gameText.AddChildText(GameText.CreateLocalised(this.AI.Realm.DisplayName, new object[0]));
			gameText.AddChildText(GameText.CreateLocalised(this.Spell.SpellData.DisplayName, new object[0]));
			this.Game.GameCore.FireEvent("TickerMessage", new object[]
			{
				new TickerMessage(gameText, TickerMessageType.Magic, 1)
			});
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		public SpellEffect Spell;

		public object Target;
	}
}
