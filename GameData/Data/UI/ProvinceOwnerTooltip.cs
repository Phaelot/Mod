using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.UI.Text;
using System.Collections.Generic;
using System.Reflection;

namespace SovereigntyTK.UI
{
    /// <summary>
    /// Current owner + rightful (natural / alt) owner for the province name/flag bar.
    /// </summary>
    public static class ProvinceOwnerTooltip
    {
        private static readonly FieldInfo AltOwnerField = typeof(WorkingProvince).GetField("NaturalOwnerAlt", BindingFlags.Instance | BindingFlags.NonPublic);

        public static List<GameText> BuildTooltip(WorkingProvince Province)
        {
            List<GameText> Tip = new List<GameText>();

            Tip.Add(MakeRealmLine("TT_PROVINCE_OWNER", Province.OwnerRealm));

            WorkingRealm Natural = ResolveRealm(Province, Province.NaturalOwner);
            if (Natural != null)
            {
                Tip.Add(MakeRealmLine("TT_PROVINCE_NATURAL", Natural));
            }

            string AltName = GetAltOwnerName(Province);
            if (!string.IsNullOrEmpty(AltName) && AltName != "None")
            {
                WorkingRealm Alt = ResolveRealm(Province, AltName);
                if (Alt != null)
                {
                    Tip.Add(MakeRealmLine("TT_PROVINCE_NATURAL_ALT", Alt));
                }
            }

            return Tip;
        }

        private static string GetAltOwnerName(WorkingProvince Province)
        {
            if (AltOwnerField == null) return null;
            return AltOwnerField.GetValue(Province) as string;
        }

        private static WorkingRealm ResolveRealm(WorkingProvince Province, string RealmName)
        {
            if (string.IsNullOrEmpty(RealmName) || RealmName == "None") return null;
            return Province.Game.GetRealm(RealmName);
        }

        private static GameText MakeRealmLine(string LabelKey, WorkingRealm Realm)
        {
            GameText Line = GameText.CreateLocalised(LabelKey);
            Line.AddChildText(GameText.CreateLocalised(Realm.DisplayName));
            return Line;
        }
    }
}
