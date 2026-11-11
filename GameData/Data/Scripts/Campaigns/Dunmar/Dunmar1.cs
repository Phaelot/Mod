using SovereigntyTK.Game.Campaign;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Data.Scripts.Campaigns.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SovereigntyTK.Game.Battle;

namespace SovereigntyTK.Data.Scripts.Campaigns
{
    public class Dunmar1: CampaignBase
    {
        ObjectiveBoolean ObjTotems;
        ObjectiveBoolean Decision;

        public Dunmar1(Sovereignty Game, string RealmName)
            : base(Game, "Core_Dunmar1", 1, RealmName)
        {
            NameText = GameText.CreateLocalised("CAMPAIGN_DUNMAR1");
            DescriptionText = GameText.CreateLocalised("CAMPAIGN_DUNMAR1_DESC");

            DescriptionObjectives = new List<GameText>();
            DescriptionObjectives.Add(GameText.CreateLocalised("OBJ_DUNMAR_TOTEM"));
        }

        protected override void LoadData(System.IO.BinaryReader r, int SaveVersion)
        {

        }

        protected override void SaveData(System.IO.BinaryWriter w)
        {

        }

        protected override bool GetCampaignAvailability(RealmData Realm)
        {
            return Realm.Name == "Dunmar";
        }

        protected override void CampaignInit()
        {
            
        }

        protected override void CreateObjectives()
        {
            ObjTotems = new ObjectiveBoolean(Game, true, "TotemCountChanged", Params => 
            {
                string Realm = (string)Params[0];
                int Count = (int)Params[1];

                if (Realm != "Vessoi") return Results.Ignore;
                if (Count <= 1) return Results.Complete;
                else return Results.Incomplete;
            });
            ObjTotems.SetText("OBJ_DUNMAR_TOTEM_TITLE", "OBJ_DUNMAR_TOTEM");
            ObjTotems.SetUnlockUnit("Fanwick's Merry Men");
            AddObjective(ObjTotems);

            // Vanilla only listened to AutoBattleStart, so fighting Vessoi in a full
            // tactical battle never opened the Cor Vilaad / Dragonhold choice.
            Decision = new ObjectiveBoolean(Game, false, "AutoBattleStart", Params => 
            {
                if (Game.CurrentGame.PlayerRealm.Gold <= 1000) return Results.Ignore;

                string attackerName = null;
                string defenderName = null;

                AutoBattleController autoBattle = Params[0] as AutoBattleController;
                if (autoBattle != null && autoBattle.Attacker != null && autoBattle.Defender != null
                    && autoBattle.Attacker.Owner != null && autoBattle.Defender.Owner != null)
                {
                    attackerName = autoBattle.Attacker.Owner.Name;
                    defenderName = autoBattle.Defender.Owner.Name;
                }
                else
                {
                    TacticalBattleController tacticalBattle = Params[0] as TacticalBattleController;
                    if (tacticalBattle != null && tacticalBattle.AttackerRealm != null && tacticalBattle.DefenderRealm != null)
                    {
                        attackerName = tacticalBattle.AttackerRealm.Name;
                        defenderName = tacticalBattle.DefenderRealm.Name;
                    }
                }

                if (attackerName == "Vessoi" && defenderName == "Dunmar") return Results.Complete;
                return Results.Ignore;
            });
            Decision.AddEvent("FullBattleStarted");
            AddObjective(Decision, false);
        }

        protected override void CampaignStarted()
        {
            AddPlugin(GetPlugin("CorePlugin"));

            WorkingRealm Vessoi = Game.CurrentGame.GetRealm("Vessoi");
            WorkingRealm Jotland = Game.CurrentGame.GetRealm("Jotland");
            WorkingRealm Dragonhold = Game.CurrentGame.GetRealm("Dragonhold");
            WorkingRealm CorVilaad = Game.CurrentGame.GetRealm("Cor Vilaad");
            WorkingRealm Dunmar = Game.CurrentGame.GetRealm("Dunmar");
            WorkingRealm BoruvianEmpire = Game.CurrentGame.GetRealm("Boruvian Empire");

            Vessoi.Gold.Value += 20000;

            List<WorkingProvince> Provinces = new List<WorkingProvince>();
            Provinces.Add(Game.CurrentGame.GetProvince("Ostervall"));
            Provinces.Add(Game.CurrentGame.GetProvince("Dewskod"));
            Provinces.Add(Game.CurrentGame.GetProvince("Mala"));
            Provinces.Add(Game.CurrentGame.GetProvince("Jenaborg"));
            Provinces.Add(Game.CurrentGame.GetProvince("Hennebronn"));
            Provinces.Add(Game.CurrentGame.GetProvince("Olmark"));

            foreach (WorkingProvince Province in Provinces)
            {
                if (Province == null) continue;

                Game.CurrentGame.ChangeProvinceOwner(Province, Vessoi);
                if (Province.LandNode != null && Province.LandNode.CurrentStack != null)
                {
                    Province.LandNode.CurrentStack.OwnerID = Vessoi.ID;
                }
            }

            WorkingProvince TargetProvince = Game.CurrentGame.GetProvince("Hundholm");
            if (TargetProvince != null && TargetProvince.LandNode != null && TargetProvince.LandNode.CurrentStack != null)
            {
                TargetProvince.LandNode.CurrentStack.OwnerID = Vessoi.ID;
            }

            TargetProvince = Game.CurrentGame.GetProvince("Freyda");
            if (TargetProvince != null && TargetProvince.LandNode != null && TargetProvince.LandNode.CurrentStack != null)
            {
                TargetProvince.LandNode.CurrentStack.OwnerID = Vessoi.ID;
            }

            CreateNormalWar(Vessoi, Jotland);

            Vessoi.DiplomacyManager.AdjustBaseValue(Jotland, 5);
            Jotland.DiplomacyManager.AdjustBaseValue(Vessoi, 5);

            Vessoi.DiplomacyManager.AdjustBaseValue(Dunmar, -7);
            Vessoi.DiplomacyManager.AdjustBaseValue(BoruvianEmpire, -7);

            Vessoi.AIPlayer.WarManager.ModifyProvinceLust("Gotmar", 20);
            Vessoi.AIPlayer.WarManager.ModifyProvinceLust("Dewskod", 20);

            Vessoi.MagicData.IgnoreSpells.Add("Thievery");

            Dunmar.AllyValue = 0;

            CorVilaad.Restrictions.AllowWar = false;
            Dragonhold.Restrictions.AllowWar = false;

            foreach (WorkingRealm Realm in Game.CurrentGame.AllRealms.Values)
            {
                Realm.Restrictions.PermaPeace.Add("Dragonhold");
                Realm.Restrictions.PermaPeace.Add("Cor Vilaad");
            }
        }

        protected override void CampaignRestored()
        {

        }

        protected override void CampaignDisposed()
        {

        }

        protected override void HandleMessageClosed(Game.MessageBoxData Msg, MessageCloseType CloseType)
        {
            if (Msg.CustomData == "DECISION")
            {
                WorkingRealm Vessoi = Game.CurrentGame.GetRealm("Vessoi");
                WorkingRealm Jotland = Game.CurrentGame.GetRealm("Jotland");
                WorkingRealm Dragonhold = Game.CurrentGame.GetRealm("Dragonhold");
                WorkingRealm CorVilaad = Game.CurrentGame.GetRealm("Cor Vilaad");
                WorkingRealm Dunmar = Game.CurrentGame.GetRealm("Dunmar");
                WorkingRealm BoruvianEmpire = Game.CurrentGame.GetRealm("Boruvian Empire");

                CorVilaad.Restrictions.AllowWar = true;
                Dragonhold.Restrictions.AllowWar = true;

                foreach (WorkingRealm Realm in Game.CurrentGame.AllRealms.Values)
                {
                    Realm.Restrictions.PermaPeace.Remove("Dragonhold");
                    Realm.Restrictions.PermaPeace.Remove("Cor Vilaad");
                }

                Dunmar.AllyValue = 2;

                if (CloseType == MessageCloseType.Yes)
                {
                    Game.MessageHandler.ShowInfoMessage("MSG_DUNMAR1A_TITLE", "MSG_DUNMAR1A_TEXT");

                    Dunmar.Gold.Value -= 1000;
                    CorVilaad.Gold.Value += 1000;

                    CorVilaad.Restrictions.PermaPeace.Add("Dunmar");
                    Dunmar.Restrictions.PermaPeace.Add("Cor Vilaad");

                    CorVilaad.DiplomacyManager.AdjustBaseValue(Dunmar, 5);
                }

                if (CloseType == MessageCloseType.No)
                {
                    Game.MessageHandler.ShowInfoMessage("MSG_DUNMAR1B_TITLE", "MSG_DUNMAR1B_TEXT");

                    Dragonhold.DiplomacyManager.AdjustBaseValue(Dunmar, 11);

                    CreateNormalAlliance(Dragonhold, Dunmar);

                    Dragonhold.DiplomacyManager.AdjustBaseValue(Vessoi, -9);

                    CreateNormalWar(Dragonhold, Vessoi);
                }
            }
        }

        protected override void HandleObjectiveStatusChanged(Objective Obj, ObjectiveStatuses Status)
        {
            if (Obj == Decision && Status == ObjectiveStatuses.Complete)
            {
                MessageBoxData Msg = new MessageBoxData();
                Msg.CaptionText = GameText.CreateLocalised("MSG_DUNMAR1_TITLE");
                Msg.MessageText = GameText.CreateLocalised("MSG_DUNMAR1_TEXT");
                Msg.YesText = GameText.CreateLocalised("MSG_DUNMAR1_YES");
                Msg.NoText = GameText.CreateLocalised("MSG_DUNMAR1_NO");
                Msg.DisplayType = MessageBoxType.YesNo;
                Msg.MsgType = MessageType.Campaign;
                Msg.CustomData = "DECISION";
                Game.MessageHandler.ShowMessage(Msg);
            }

            if (Obj == ObjTotems && Status == ObjectiveStatuses.Complete)
            {
                SetCampaignVictory();
            }
        }
    }
}
