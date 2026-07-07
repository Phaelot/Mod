using System;
using System.Reflection;
using System.Xml.Linq;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI.Controls
{
	public class ControlForm : UIControl
	{
		private event ControlDelegate OnLoad;

		public ControlForm(GameBase Game)
			: base(Game)
		{
		}

		protected override void ParseElement(XElement Element)
		{
			string localName;
			if ((localName = Element.Name.LocalName) != null)
			{
				if (localName == "scriptfile")
				{
					this.InitScript(Element.Value);
					return;
				}
				if (localName == "onload")
				{
					this.OnLoad += this.GetEventHandler(Element.Value);
					return;
				}
			}
			base.ParseElement(Element);
		}

		private void InitScript(string Typename)
		{
			this.ScriptObject = (ScriptBase)this.Game.Utilities.ScriptManager.UIAssembly.CreateInstance(Typename);
			if (this.ScriptObject != null)
			{
				this.ScriptObject.Init((Sovereignty)this.Game, this);
			}
		}

		protected override ControlDelegate GetEventHandler(string FunctionName)
		{
			if (this.ScriptObject == null)
			{
				return null;
			}
			Type type = this.ScriptObject.GetType();
			MethodInfo method = type.GetMethod(FunctionName);
			if (method == null)
			{
				return null;
			}
			return (ControlDelegate)Delegate.CreateDelegate(typeof(ControlDelegate), this.ScriptObject, method);
		}

		public override void Render(GLShader Shader, float ElapsedTime)
		{
			if (this.ScriptObject != null)
			{
				this.ScriptObject.Update(ElapsedTime);
			}
			base.Render(Shader, ElapsedTime);
		}

		public override void SetLoaded()
		{
			if (this.OnLoad != null)
			{
				this.OnLoad(this);
			}
		}

		public override void Dispose()
		{
			this.OnLoad = null;
			if (this.ScriptObject != null)
			{
				this.ScriptObject.Dispose();
			}
			base.Dispose();
		}

		public ScriptBase ScriptObject;
	}
}
