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

namespace SovereigntyTK.Data.Scripts.Campaigns
{
    public class Riesental1: CampaignBase
    {
        ObjectiveBoolean ObjProvince1;
        ObjectiveBoolean ObjProvince2;
        ObjectiveBoolean ObjProvince3;
        ObjectiveBoolean ObjSpell;

        bool SecondBatchActive;

        ObjectiveCounter ObjRangers;
        ObjectiveCounter ObjGold;
        ObjectiveCounter ObjAllies;
        ObjectiveCounter ObjTrade;

        public Riesental1(Sovereignty Game, string RealmName)
            : base(Game, "Core_Riesental1", 1, RealmName)
        {
            NameText = GameText.CreateLocalised("CAMPAIGN_RIESENTAL1");
            DescriptionText = GameText.CreateLocalised("CAMPAIGN_RIESENTAL1_DESC");

            DescriptionObjectives = new List<GameText>();
            DescriptionObjectives.Add(GameText.CreateLocalised("OBJ_RIESENTAL_PROVINCE1"));
            DescriptionObjectives.Add(GameText.CreateLocalised("OBJ_RIESENTAL_PROVINCE2"));
            DescriptionObjectives.Add(GameText.CreateLocalised("OBJ_RIESENTAL_PROVINCE3"));
            DescriptionObjectives.Add(GameText.CreateLocalised("OBJ_RIESENATAL_SPELL"));
        }

        protected override void LoadData(System.IO.BinaryReader r, int SaveVersion)
        {
            SecondBatchActive = r.ReadBoolean();
        }

        protected override void SaveData(System.IO.BinaryWriter w)
        {
            w.Write(SecondBatchActive);
        }

        protected override bool GetCampaignAvailability(RealmData Realm)
        {
            return Realm.Name == "Riesental";
        }

        protected override void CampaignInit()
        {
            
        }

        protected override void CreateObjectives()
        {
            ObjProvince1 = new ObjectiveBoolean(Game, true, "ProvinceOwnerChanged", Params => { if (IsPlayerOwned("Galisong")) return Results.Complete; else return Results.Incomplete; });
            ObjProvince1.TargetProvince = "Galisong";
            ObjProvince1.SetText("OBJ_RIESENTAL_PROVINCE1_TITLE", "OBJ_RIESENTAL_PROVINCE1");
            AddObjective(ObjProvince1);

            ObjProvince2 = new ObjectiveBoolean(Game, true, "ProvinceOwnerChanged", Params => { if (IsPlayerOwned("Feybrunn")) return Results.Complete; else return Results.Incomplete; });
            ObjProvince2.TargetProvince = "Feybrunn";
            ObjProvince2.SetText("OBJ_RIESENTAL_PROVINCE2_TITLE", "OBJ_RIESENTAL_PROVINCE2");
            AddObjective(ObjProvince2);

            ObjProvince3 = new ObjectiveBoolean(Game, true, "ProvinceOwnerChanged", Params => { if (IsPlayerOwned("Telonnen")) return Results.Complete; else return Results.Incomplete; });
            ObjProvince3.TargetProvince = "Telonnen";
            ObjProvince3.SetText("OBJ_RIESENTAL_PROVINCE3_TITLE", "OBJ_RIESENTAL_PROVINCE3");
            AddObjective(ObjProvince3);

            ObjSpell = new ObjectiveBoolean(Game, false, "SpellsChanged", Params => { if (Game.CurrentGame.PlayerRealm.SpellEffects.AffectedBySpell("Golden Route")) return Results.Complete; else return Results.Ignore; });
            ObjSpell.SetText("OBJ_RIESENATAL_SPELL_TITLE", "OBJ_RIESENATAL_SPELL");
            ObjSpell.SetUnlockUnit("Tipper the Bard");
            AddObjective(ObjSpell);

            ObjRangers = new ObjectiveCounter(Game, 4, "UnitDestroyed", Params => 
            {
                int Value = ObjRangers.CurrentValue;
                WorkingUnit Unit = Params[0] as WorkingUnit;

                if (Unit.BaseName == "Riesental.Rangers") Value++;

                return Value;
            });
            ObjRangers.SetText("OBJ_RIESENTAL_RANGERS_TITLE", "OBJ_RIESENTAL_RANGERS");
            AddObjective(ObjRangers, false, false);

            ObjGold = new ObjectiveCounter(Game, 50000, "PlayerGoldChanged", Params => { return Game.CurrentGame.PlayerRealm.Gold; });
            ObjGold.SetText("OBJ_RIESENTAL_GOLD_TITLE", "OBJ_RIESENTAL_GOLD");
            AddObjective(ObjGold, false, false);

            ObjAllies = new ObjectiveCounter(Game, 2, "AlliancesChanged", Params => { return Game.CurrentGame.PlayerRealm.Allies.Count; });
            ObjAllies.SetText("OBJ_RIESENTAL_ALLY_TITLE", "OBJ_RIESENTAL_ALLY");
            AddObjective(ObjAllies, false, false);

            ObjTrade = new ObjectiveCounter(Game, 400, "TradeGoodGiven", Params =>
            {
                WorkingRealm Realm1 = (WorkingRealm)Params[0];
                WorkingRealm Realm2 = (WorkingRealm)Params[1];
                int Quantity = (int)Params[3];

                int Value = ObjTrade.CurrentValue;

                if (Realm1 == Game.CurrentGame.PlayerRealm || Realm2 == Game.CurrentGame.PlayerRealm) Value += Quantity;
                return Value;
            });
            ObjTrade.SetText("OBJ_RIESENTAL_TRADE_TITLE", "OBJ_RIESENTAL_TRADE");
            AddObjective(ObjTrade, false, false);
        }

        private bool IsPlayerOwned(string ProvinceName)
        {
            WorkingProvince Province = Game.CurrentGame.GetProvince(ProvinceName);
            return Province != null && Province.OwnerRealm == Game.CurrentGame.PlayerRealm;
        }

        protected override void CampaignStarted()
        {
            AddPlugin(GetPlugin("CorePlugin"));

            Game.CurrentGame.PlayerRealm.Restrictions.DenyUnits.Add("Rangers");
            Game.CurrentGame.PlayerRealm.HeroClasses.RemoveAll(x => x.ClassName == "Ranger");
            Game.CurrentGame.PlayerRealm.HeroClasses.Add(Game.Data.HeroClasses["Trafficker"]);

            foreach (WorkingUnit Unit in Game.CurrentGame.PlayerRealm.Units)
            {
                if (Unit.BaseType.Name == "Rangers") Game.CurrentGame.DestroyUnit(Unit);
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

        }

        protected override void HandleObjectiveStatusChanged(Objective Obj, ObjectiveStatuses Status)
        {
            if (!SecondBatchActive)
            {
                if (ObjectivesCompleted(ObjProvince1, ObjProvince2, ObjProvince3))
                {
                    SecondBatchActive = true;

                    List<WorkingProvince> TargetProvinces = new List<WorkingProvince>();
                    List<string> TargetNames = new List<string>();

                    TargetProvinces.Add(Game.CurrentGame.GetProvince("Ashengrove"));
                    TargetProvinces.Add(Game.CurrentGame.GetProvince("Leapingdale"));
                    TargetProvinces.Add(Game.CurrentGame.GetProvince("Wooddorf"));

                    TargetNames.Add("Brumberry");
                    TargetNames.Add("Rivermeet");
                    TargetNames.Add("Plumbrook");
                    TargetNames.Add("Waterford");
                    TargetNames.Add("Brent");
                    TargetNames.Add("Millyard");
                    // Province names must match sov_data.xml exactly (ASCII apostrophe).
                    TargetNames.Add("Tam's Path");
                    TargetNames.Add("Merridale");
                    TargetNames.Add("Rockham's Clearing");

                    for (int i = 0; i < 4; i++)
                    {
                        int Index = RNG.Next(TargetNames.Count);
                        string Name = TargetNames[Index];

                        TargetNames.Remove(Name);
                        WorkingProvince Found = Game.CurrentGame.GetProvince(Name);
                        if (Found != null) TargetProvinces.Add(Found);
                    }

                    TargetProvinces.RemoveAll(x => x == null);

                    foreach (WorkingProvince Province in TargetProvinces)
                    {
                        Game.CurrentGame.ChangeProvinceOwner(Province, Game.CurrentGame.RebelRealm);
                        if (Province.LandNode != null && Province.LandNode.CurrentStack != null)
                        {
                            Province.LandNode.CurrentStack.SetOwner(Game.CurrentGame.RebelRealm.ID);
                        }
                    }

                    UnitData UnitType;
                    Game.Data.Units.TryGetValue("Riesental.Rangers", out UnitType);

                    for (int i = 0; i < 4 && TargetProvinces.Count > 0 && UnitType != null; i++)
                    {
                        WorkingProvince Province = TargetProvinces[RNG.Next(TargetProvinces.Count)];
                        TargetProvinces.Remove(Province);
                        if (Province.LandNode == null) continue;

                        WorkingUnit Unit = Game.CurrentGame.CreateUnit(Game.CurrentGame.RebelRealm.ID, UnitType);

                        WorkingStack Stack = Province.LandNode.CurrentStack;
                        if (Stack == null) Stack = Game.CurrentGame.CreateStack(Game.CurrentGame.RebelRealm.ID, Province.LandNode.ID);

                        Stack.AddUnit(Unit);
                    }

                    Game.MessageHandler.ShowInfoMessage("MSG_RIESENTAL1_TITLE", "MSG_RIESENTAL1_TEXT");

                    CreatePermenantWar(Game.CurrentGame.GetRealm("Sirucil"), Game.CurrentGame.PlayerRealm);
                    CreatePermenantWar(Game.CurrentGame.GetRealm("Sonneneve"), Game.CurrentGame.PlayerRealm);
                    CreateNormalWar(Game.CurrentGame.GetRealm("Bereska"), Game.CurrentGame.PlayerRealm);

                    ActivateObjective(ObjRangers);
                    ActivateObjective(ObjGold);
                    ActivateObjective(ObjAllies);
                    ActivateObjective(ObjTrade);

                    ObjGold.ForceCheck();
                    ObjAllies.ForceCheck();
                }
            }
            else
            {
                if (ObjectivesCompleted(ObjRangers, ObjGold, ObjAllies, ObjTrade, ObjProvince1, ObjProvince2, ObjProvince3, ObjSpell))
                {
                    SetCampaignVictory();
                }
            }
        }
    }
}
