using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game
{
	internal static class StackAnomalyLogger
	{
		public static void CheckAllStacks(SovereigntyGame Game, string Phase, WorkingRealm CurrentRealm)
		{
			try
			{
				if (Game == null || Game.AllProvinces == null)
				{
					return;
				}
				List<string> lines = new List<string>();
				foreach (WorkingProvince province in Game.AllProvinces.Values.OrderBy((WorkingProvince x) => x.ID))
				{
					if (province == null)
					{
						continue;
					}
					AppendNodeAnomalyLines(Game, province.LandNode, Phase, CurrentRealm, lines);
					if (province.HarbourNode != null && province.HarbourNode != province.LandNode)
					{
						AppendNodeAnomalyLines(Game, province.HarbourNode, Phase, CurrentRealm, lines);
					}
				}
				if (lines.Count > 0)
				{
					WriteLines(Game, lines);
				}
			}
			catch
			{
				// Diagnostic code must never change campaign behaviour.
			}
		}

		public static void CheckNode(SovereigntyGame Game, ActivePathNode Node, string Phase, WorkingRealm CurrentRealm)
		{
			try
			{
				List<string> lines = new List<string>();
				AppendNodeAnomalyLines(Game, Node, Phase, CurrentRealm, lines);
				if (lines.Count > 0)
				{
					WriteLines(Game, lines);
				}
			}
			catch
			{
				// Diagnostic code must never change campaign behaviour.
			}
		}

		private static void AppendNodeAnomalyLines(SovereigntyGame Game, ActivePathNode Node, string Phase, WorkingRealm CurrentRealm, List<string> Lines)
		{
			if (Game == null || Node == null || Node.Province == null || Lines == null)
			{
				return;
			}
			WorkingProvince province = Node.Province;
			WorkingRealm provinceOwner = province.OwnerRealm;
			List<WorkingStack> nodeStacks = GetNodeStacks(Game, Node);
			if (nodeStacks.Count == 0)
			{
				return;
			}

			List<string> reasons = new List<string>();
			bool noResistance = !province.Occupied && province.ActiveResistance <= 0 && string.IsNullOrEmpty(province.ResistingRealmName);
			List<WorkingStack> allySlotStacks = GetAllySlotStacks(Game, Node);
			foreach (WorkingStack allyStack in allySlotStacks)
			{
				if (allyStack == null || allyStack.Owner == null || provinceOwner == null)
				{
					continue;
				}
				if (allyStack.Owner != provinceOwner && allyStack.Owner.DiplomacyManager.GetRelation(provinceOwner) != RelationStates.Alliance)
				{
					reasons.Add("BAD_ALLYSTACK_NON_ALLIED_OWNER stack=" + allyStack.ID + " stackOwner=" + RealmText(allyStack.Owner) + " provinceOwner=" + RealmText(provinceOwner) + " relation=" + RelationText(allyStack.Owner, provinceOwner));
				}
			}

			for (int i = 0; i < nodeStacks.Count; i++)
			{
				for (int j = i + 1; j < nodeStacks.Count; j++)
				{
					WorkingStack a = nodeStacks[i];
					WorkingStack b = nodeStacks[j];
					if (a.Owner == null || b.Owner == null || a.Owner == b.Owner)
					{
						continue;
					}
					RelationStates relation = a.Owner.DiplomacyManager.GetRelation(b.Owner);
					if (relation != RelationStates.Alliance)
					{
						if (noResistance)
						{
							reasons.Add("NON_ALLIED_STACKS_ON_SAME_NODE_NO_RESISTANCE stackA=" + a.ID + " ownerA=" + RealmText(a.Owner) + " stackB=" + b.ID + " ownerB=" + RealmText(b.Owner) + " relation=" + relation.ToString());
						}
						else if (relation == RelationStates.War)
						{
							reasons.Add("WAR_STACKS_ON_SAME_NODE stackA=" + a.ID + " ownerA=" + RealmText(a.Owner) + " stackB=" + b.ID + " ownerB=" + RealmText(b.Owner));
						}
					}
				}
			}

			foreach (WorkingStack stack in nodeStacks)
			{
				if (stack.Owner == null || provinceOwner == null || stack.Owner == provinceOwner)
				{
					continue;
				}
				if (IsPureNavalHarbourStack(stack, Node))
				{
					continue;
				}
				RelationStates relationToOwner = stack.Owner.DiplomacyManager.GetRelation(provinceOwner);
				if (noResistance && stack.Owner.AIPlayer != null && relationToOwner != RelationStates.Alliance)
				{
					reasons.Add("AI_STACK_IN_NON_ALLIED_PROVINCE_NO_RESISTANCE stack=" + stack.ID + " stackOwner=" + RealmText(stack.Owner) + " provinceOwner=" + RealmText(provinceOwner) + " relation=" + relationToOwner.ToString());
				}
			}

			if (Node.AllyStacks != null)
			{
				foreach (int duplicateID in Node.AllyStacks.GroupBy((int x) => x).Where((IGrouping<int, int> x) => x.Count() > 1).Select((IGrouping<int, int> x) => x.Key))
				{
					reasons.Add("DUPLICATE_ALLYSTACK_ID stackID=" + duplicateID);
				}
			}

			if (reasons.Count == 0)
			{
				return;
			}

			Lines.Add(BuildPrefix(Game, Phase, CurrentRealm) + " province=" + ProvinceText(province) + " node=" + NodeText(Node) + " noResistance=" + noResistance.ToString() + " reasons=[" + string.Join(" | ", reasons.ToArray()) + "] current=" + StackText(Node.CurrentStack) + " allies=" + StackListText(allySlotStacks));
		}

		private static List<WorkingStack> GetNodeStacks(SovereigntyGame Game, ActivePathNode Node)
		{
			List<WorkingStack> result = new List<WorkingStack>();
			WorkingStack current = Node.CurrentStack;
			if (IsStackOnNode(current, Node))
			{
				result.Add(current);
			}
			foreach (WorkingStack stack in GetAllySlotStacks(Game, Node))
			{
				if (IsStackOnNode(stack, Node) && !result.Contains(stack))
				{
					result.Add(stack);
				}
			}
			return result;
		}

		private static List<WorkingStack> GetAllySlotStacks(SovereigntyGame Game, ActivePathNode Node)
		{
			List<WorkingStack> result = new List<WorkingStack>();
			if (Game == null || Node == null || Node.AllyStacks == null)
			{
				return result;
			}
			foreach (int stackID in Node.AllyStacks)
			{
				WorkingStack stack;
				if (Game.AllStacks.TryGetValue(stackID, out stack) && stack != null && !stack.Disposed)
				{
					result.Add(stack);
				}
			}
			return result;
		}

		private static bool IsStackOnNode(WorkingStack Stack, ActivePathNode Node)
		{
			return Stack != null && !Stack.Disposed && Stack.Node == Node && (Stack.Units.Count > 0 || Stack.Hero != null);
		}

		private static bool IsPureNavalHarbourStack(WorkingStack Stack, ActivePathNode Node)
		{
			if (Stack == null || Node == null || Stack.Units == null || Stack.Units.Count == 0)
			{
				return false;
			}
			if (Node.NodeType != PathNodeTypes.Harbour && Node.NodeType != PathNodeTypes.RiverHarbour)
			{
				return false;
			}
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (unit == null || unit.Class != UnitClasses.Naval)
				{
					return false;
				}
			}
			return true;
		}

		private static string BuildPrefix(SovereigntyGame Game, string Phase, WorkingRealm CurrentRealm)
		{
			string turn = "T?";
			string date = "date?";
			if (Game != null && Game.TurnController != null)
			{
				turn = "T" + Game.TurnController.TurnNumber.ToString();
				if (Game.TurnController.CurrentDate != null)
				{
					date = Game.TurnController.CurrentDate.Value.ToString("yyyy-MM-dd");
				}
			}
			string realm = (CurrentRealm == null) ? "realm?" : RealmText(CurrentRealm);
			return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + turn + " " + date + " phase=" + Phase + " currentRealm=" + realm;
		}

		private static string ProvinceText(WorkingProvince Province)
		{
			if (Province == null)
			{
				return "null";
			}
			return Province.Name + "#" + Province.ID.ToString() + " owner=" + RealmText(Province.OwnerRealm) + " occupier=" + RealmText(Province.OccupierRealm) + " occupied=" + Province.Occupied.ToString() + " activeResistance=" + Province.ActiveResistance.ToString() + " resisting='" + Province.ResistingRealmName + "'";
		}

		private static string NodeText(ActivePathNode Node)
		{
			if (Node == null)
			{
				return "null";
			}
			return Node.ID.ToString() + " type=" + Node.NodeType.ToString();
		}

		private static string StackListText(List<WorkingStack> Stacks)
		{
			if (Stacks == null || Stacks.Count == 0)
			{
				return "[]";
			}
			return "[" + string.Join("; ", Stacks.Select((WorkingStack x) => StackText(x)).ToArray()) + "]";
		}

		private static string StackText(WorkingStack Stack)
		{
			if (Stack == null)
			{
				return "null";
			}
			string hp = "hp=none";
			try
			{
				if (Stack.Units.Count > 0)
				{
					int minHp = Stack.Units.Min((WorkingUnit x) => x.Health.Value);
					int maxHp = Stack.Units.Max((WorkingUnit x) => x.Health.Value);
					hp = "hp=" + minHp.ToString() + "-" + maxHp.ToString();
				}
			}
			catch
			{
				hp = "hp=error";
			}
			string units = "";
			try
			{
				units = " units=[" + string.Join(",", Stack.Units.Take(8).Select((WorkingUnit x) => x.ID.ToString() + ":" + x.DisplayName + ":" + x.Class.ToString() + ":hp" + x.Health.Value.ToString()).ToArray()) + (Stack.Units.Count > 8 ? ",..." : "") + "]";
			}
			catch
			{
				units = " units=error";
			}
			return "stack=" + Stack.ID.ToString() + " owner=" + RealmText(Stack.Owner) + " node=" + Stack.NodeID.ToString() + " count=" + Stack.Units.Count.ToString() + " " + hp + " hero=" + ((Stack.Hero == null) ? "none" : Stack.Hero.DisplayName) + units;
		}

		private static string RealmText(WorkingRealm Realm)
		{
			if (Realm == null)
			{
				return "null";
			}
			return Realm.Name + "#" + Realm.ID.ToString() + ((Realm.AIPlayer != null) ? ":AI" : ":Player");
		}

		private static string RelationText(WorkingRealm A, WorkingRealm B)
		{
			if (A == null || B == null)
			{
				return "null";
			}
			if (A == B)
			{
				return "SameRealm";
			}
			try
			{
				return A.DiplomacyManager.GetRelation(B).ToString();
			}
			catch
			{
				return "relation-error";
			}
		}

		private static void WriteLines(SovereigntyGame Game, List<string> Lines)
		{
			if (Lines == null || Lines.Count == 0)
			{
				return;
			}
			string directory = GetLogDirectory();
			Directory.CreateDirectory(directory);
			string file = System.IO.Path.Combine(directory, "stack_anomalies.txt");
			File.AppendAllLines(file, Lines.ToArray());
		}

		private static string GetLogDirectory()
		{
			string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (string.IsNullOrEmpty(documents))
			{
				documents = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			}
			if (string.IsNullOrEmpty(documents))
			{
				documents = AppDomain.CurrentDomain.BaseDirectory;
			}
			return System.IO.Path.Combine(documents, "SovereigntyAILogs");
		}
	}
}
