using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class UnitFlag
	{
		public bool Visible
		{
			get
			{
				return this.m_Visible;
			}
		}

		public static void CheckFlagTypes(Sovereignty Game)
		{
			if (UnitFlag.FlagTypes == null)
			{
				UnitFlag.FlagTypes = new Dictionary<string, Type>();
				List<Type> list = (from t in Game.Utilities.ScriptManager.FlagAssembly.GetTypes()
					where t.IsSubclassOf(typeof(UnitFlag))
					select t).ToList<Type>();
				foreach (Type type in list)
				{
					if (type.GetConstructor(Type.EmptyTypes) == null)
					{
						throw new Exception("Empty constructor required for " + type.Name);
					}
					UnitFlag unitFlag = (UnitFlag)Activator.CreateInstance(type);
					UnitFlag.FlagTypes.Add(unitFlag.Name, type);
				}
			}
		}

		public static UnitFlag AttemptCreateNamedFlag(Sovereignty Game, string Flagname)
		{
			UnitFlag.CheckFlagTypes(Game);
			Type type = null;
			UnitFlag.FlagTypes.TryGetValue(Flagname, out type);
			if (type == null)
			{
				return null;
			}
			return (UnitFlag)Activator.CreateInstance(type);
		}

		public static UnitFlag CreateNamedFlag(Sovereignty Game, string Flagname)
		{
			UnitFlag.CheckFlagTypes(Game);
			Type type = null;
			UnitFlag.FlagTypes.TryGetValue(Flagname, out type);
			if (type == null)
			{
				throw new Exception("Flag type does not exist: " + Flagname);
			}
			return (UnitFlag)Activator.CreateInstance(type);
		}

		public static bool NamedFlagExists(Sovereignty Game, string Flagname)
		{
			UnitFlag.CheckFlagTypes(Game);
			Type type = null;
			UnitFlag.FlagTypes.TryGetValue(Flagname, out type);
			return type != null;
		}

		public UnitFlag(string Name, string DisplayName, string DisplayDesc)
		{
			this.Name = Name;
			this.DisplayName = DisplayName;
			this.DisplayDesc = DisplayDesc;
			this.NamedVariables = new Dictionary<string, int>();
		}

		public int GetVariable(string Name)
		{
			if (this.NamedVariables.ContainsKey(Name))
			{
				return this.NamedVariables[Name];
			}
			return 0;
		}

		public void SetVariable(string Name, int Value)
		{
			if (!this.NamedVariables.ContainsKey(Name))
			{
				this.NamedVariables.Add(Name, Value);
				return;
			}
			this.NamedVariables[Name] = Value;
		}

		public virtual void SaveCurrentState(BinaryWriter w)
		{
			w.Write(this.TurnCount);
			w.Write(this.NamedVariables.Count);
			foreach (KeyValuePair<string, int> keyValuePair in this.NamedVariables)
			{
				w.Write(keyValuePair.Key);
				w.Write(keyValuePair.Value);
			}
		}

		public virtual void LoadSavedState(BinaryReader r, int SaveVersion)
		{
			this.TurnCount = r.ReadInt32();
			if (SaveVersion >= 51)
			{
				int num = r.ReadInt32();
				for (int i = 0; i < num; i++)
				{
					string text = r.ReadString();
					int num2 = r.ReadInt32();
					this.NamedVariables.Add(text, num2);
				}
			}
		}

		protected virtual void AddModifier(StatTypes Stat, List<Tuple<string, int>> RawModifiers, WorkingUnit Unit)
		{
		}

		public virtual void Detach(WorkingUnit Unit)
		{
			this.Unit = null;
			this.AttachedToUnit = false;
			Unit.HandleFlagDetached(this);
		}

		public virtual void Attach(WorkingUnit Unit)
		{
			this.Unit = Unit;
			this.AttachedToUnit = true;
			Unit.HandleFlagAttached(this);
		}

		public virtual void GetStatusOnSelf(bool Attacking, bool Retal, bool Ranged, WorkingUnit OtherUnit, List<GameText> Result)
		{
		}

		public virtual void GetStatusOnEnemy(bool Attacking, bool Retal, bool Ranged, WorkingUnit OtherUnit, List<GameText> Result)
		{
		}

		private static Dictionary<string, Type> FlagTypes;

		private Dictionary<string, int> NamedVariables;

		public string Name;

		public string DisplayName;

		public string DisplayDesc;

		public int MaxStack = 1;

		public int TurnCount = -1;

		public bool AttachedToUnit;

		protected WorkingUnit Unit;

		protected bool m_Visible = true;

		public bool DoNotSave;

		public bool NoFloaties;
	}
}
