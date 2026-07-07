using System;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class ActiveStat<T> where T : struct, IComparable<T>
	{
		public event StatChanged<T> OnStatChanged;

		public event StatChanged<T> OnStatIncreased;

		public T Value
		{
			get
			{
				return this.InternalValue;
			}
			set
			{
				bool flag = !this.InternalValue.Equals(value);
				bool flag2 = value.CompareTo(this.InternalValue) > 0;
				this.InternalValue = value;
				if (flag && this.OnStatChanged != null)
				{
					this.OnStatChanged();
				}
				if (flag2 && this.OnStatIncreased != null)
				{
					this.OnStatIncreased();
				}
			}
		}

		public ActiveStat(T InitialValue)
		{
			this.InternalValue = InitialValue;
		}

		public static implicit operator T(ActiveStat<T> Value)
		{
			return Value.InternalValue;
		}

		public override string ToString()
		{
			return this.InternalValue.ToString();
		}

		public void Dispose()
		{
			this.OnStatChanged = null;
		}

		private T InternalValue;
	}
}
