// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.UI.CheatConsole
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Input;
using SovereigntyTK;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI;
using SovereigntyTK.UI.Controls;
using SovereigntyTK.UI.Map;
using SovereigntyTK.UI.Text;
using Steamworks;

namespace SovereigntyTK.UI
{
	internal class CheatConsole
	{
		public ControlImage BGImage;

		public ControlText OutputText;

		public ControlInput InputText;

		public ControlImage InputMarker;

		public Sovereignty Game;

		private List<string> ConsoleOutput;

		private List<string> PreviousCommands;

		private int CommandIndex;

		public CheatConsole(Sovereignty Game)
		{
			this.Game = Game;
			ConsoleOutput = new List<string>();
			PreviousCommands = new List<string>();
			BGImage = new ControlImage(Game);
			BGImage.SetSize(100f, 40f, UIUnits.Percent);
			BGImage.SetImageFile("Data\\Images\\HUD\\BG_dark.png");
			BGImage.Visible = false;
			BGImage.Sprite.SetAlpha(0.85f);
			BGImage.MouseInputType = MouseInputTypes.Forced;
			BGImage.OnMouseDown += BGImage_OnMouseDown;
			Game.UIManager.AddControl(BGImage);
			OutputText = new ControlText(Game);
			BGImage.AddChild(OutputText);
			OutputText.SetFontSize(5, UIUnits.Percent);
			OutputText.SetSize(100f, 95f, UIUnits.Percent);
			OutputText.FontName = "Ubuntu";
			OutputText.ScrollingEnabled = true;
			OutputText.IgnoreFormatting = true;
			OutputText.MouseInputType = MouseInputTypes.None;
			InputMarker = new ControlImage(Game);
			BGImage.AddChild(InputMarker);
			InputMarker.SetHeight(5f, UIUnits.Percent);
			InputMarker.SetWidth(InputMarker.Sprite.Bounds.Height, UIUnits.PixelScaled);
			InputMarker.SetAnchor(AnchorPoints.BottomLeft);
			InputMarker.MouseInputType = MouseInputTypes.None;
			InputMarker.SetImageFile("Data\\Images\\HUD\\Input.png");
			InputText = new ControlInput(Game);
			BGImage.AddChild(InputText);
			InputText.SetHeight(5f, UIUnits.Percent);
			InputText.SetFontSize((int)InputMarker.Sprite.Bounds.Height, UIUnits.PixelScaled);
			InputText.SetWidth(BGImage.Sprite.Bounds.Width - InputMarker.Sprite.Bounds.Height, UIUnits.PixelScaled);
			InputText.SetPositionX(InputMarker.Sprite.Bounds.Height, UIUnits.PixelScaled);
			InputText.SetPositionY(OutputText.Sprite.Bounds.Height, UIUnits.PixelScaled);
			InputText.FontName = "Ubuntu";
			InputText.BlockedKeys.Add('.');
			InputText.TextAnchor = AnchorPoints.Left;
			InputText.Clear();
			InputText.OnReturn += InputText_OnReturn;
			InputText.OnFunctionKey += InputText_OnFunctionKey;
			InputText.OnArrowKey += InputText_OnArrowKey;
			InputText.MouseInputType = MouseInputTypes.None;
			for (int i = 0; i < 50; i++)
			{
				AddOutputLine("");
			}
			AddOutputLine("Console ready");
		}

		private void InputText_OnArrowKey(object sender, KeyboardKeyEventArgs e)
		{
			if (PreviousCommands.Count == 0)
			{
				return;
			}
			if (e.Key == Key.Up)
			{
				if (CommandIndex == 0)
				{
					return;
				}
				CommandIndex--;
				RepeatCommand();
			}
			if (e.Key == Key.Down && CommandIndex < PreviousCommands.Count - 1)
			{
				CommandIndex++;
				RepeatCommand();
			}
		}

		private void RepeatCommand()
		{
			InputText.SetText(PreviousCommands[CommandIndex]);
		}

		private void InputText_OnFunctionKey(object sender, KeyboardKeyEventArgs e)
		{
			if (e.Key == Key.F2)
			{
				Toggle();
			}
		}

		private void BGImage_OnMouseDown(UIControl Control, float X, float Y, MouseButton Button)
		{
			Game.UIManager.SetTextFocus(InputText);
		}

		private void UpdateOutput()
		{
			string text = "";
			foreach (string item in ConsoleOutput)
			{
				text = text + item + "\n";
			}
			OutputText.SetLiteralText(text);
			OutputText.ScrollToBottom();
		}

		private void StoreCommand(string Command)
		{
			if (PreviousCommands.Count >= 50)
			{
				PreviousCommands.RemoveAt(0);
			}
			PreviousCommands.Add(Command);
			CommandIndex = PreviousCommands.Count;
		}

		private void AddOutputLine(string Line)
		{
			if (ConsoleOutput.Count >= 50)
			{
				ConsoleOutput.RemoveAt(0);
			}
			ConsoleOutput.Add(Line);
			UpdateOutput();
		}

		private void InputText_OnReturn(UIControl Control)
		{
			string currentText = InputText.CurrentText;
			AddOutputLine(currentText);
			StoreCommand(currentText);
			InputText.Clear();
			if (currentText != null && !(currentText == ""))
			{
				ConsoleCommand command = new ConsoleCommand(currentText);
				ExecuteCommand(command);
			}
		}

		private void ExecuteCommand(ConsoleCommand Command)
		{
			switch (Command.CommandName)
			{
				case "/damagemorale":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count != 2)
						{
							AddOutputLine("Expected parameters: [UnitID] [Damage]");
							break;
						}
						WorkingUnit unit = Game.CurrentGame.GetUnit(int.Parse(Command.Parameters[0]));
						int num = int.Parse(Command.Parameters[1]);
						if (unit == null)
						{
							AddOutputLine("Unit ID " + Command.Parameters[0] + " does not exist");
							break;
						}
						unit.Morale.Value -= num;
						if (unit.BattleData != null)
						{
							unit.BattleData.UpdateImage();
						}
						break;
					}
				case "/damageunit":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count != 2)
						{
							AddOutputLine("Expected parameters: [UnitID] [Damage]");
							break;
						}
						WorkingUnit unit2 = Game.CurrentGame.GetUnit(int.Parse(Command.Parameters[0]));
						int damage = int.Parse(Command.Parameters[1]);
						if (unit2 == null)
						{
							AddOutputLine("Unit ID " + Command.Parameters[0] + " does not exist");
						}
						else
						{
							unit2.ApplyRealDamage(damage, DamageTypes.None, Ranged: false, null, "attack_death");
						}
						break;
					}
				case "/idkfa":
					if (Game.CurrentGame == null)
					{
						AddOutputLine("Game must be running to use this cheat");
						break;
					}
					if (Command.Parameters.Count > 0)
					{
						AddOutputLine("Expected no parameters");
						break;
					}
					Game.CurrentGame.PlayerRealm.Gold.Value += 500000000;
					{
						foreach (ResourceData value8 in Game.Data.Resources.Values)
						{
							Game.CurrentGame.PlayerRealm.GrantResource(value8, 10000);
						}
						break;
					}
				case "/showpeacedesire":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 2 || Command.Parameters.Count > 2)
						{
							AddOutputLine("Expected parameters: [RealmA] [RealmB]");
							break;
						}
						WorkingRealm realm14 = Game.CurrentGame.GetRealm(Command.Parameters[0]);
						WorkingRealm realm15 = Game.CurrentGame.GetRealm(Command.Parameters[1]);
						if (realm14 == null)
						{
							AddOutputLine("Realm not found: " + Command.Parameters[0]);
							break;
						}
						if (realm15 == null)
						{
							AddOutputLine("Realm not found: " + Command.Parameters[1]);
							break;
						}
						if (realm14.AIPlayer == null)
						{
							AddOutputLine("AI player required");
							break;
						}
						if (realm14.DiplomacyManager.GetRelation(realm15) != RelationStates.War)
						{
							AddOutputLine("Specified realms must be at war");
							break;
						}
						GameText caption = GameText.CreateFromLiteral("Peace desire");
						string text14 = realm14.Name + " peace desire with " + realm15.Name + ":\n\n";
						List<Tuple<string, int>> peaceDesireBreakdown = realm14.AIPlayer.WarManager.GetPeaceDesireBreakdown(realm15);
						foreach (Tuple<string, int> item in peaceDesireBreakdown)
						{
							object obj = text14;
							text14 = string.Concat(obj, item.Item1, " ", item.Item2, "\n");
						}
						text14 = text14 + "Total peace desire: " + realm14.AIPlayer.WarManager.GetPeaceDesire(realm15);
						Game.MessageHandler.ShowInfoMessage(caption, GameText.CreateFromLiteral(text14));
						break;
					}
				case "/showgold":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						List<GameText> list2 = new List<GameText>();
						foreach (WorkingRealm value9 in Game.CurrentGame.AllRealms.Values)
						{
							list2.Add(GameText.CreateFromLiteral($"{value9.Name}: {value9.GetTotalGold()}"));
						}
						MessageBoxData messageBoxData = new MessageBoxData();
						messageBoxData.CaptionText = GameText.CreateFromLiteral("Realm Gold");
						messageBoxData.MessageTextList = list2;
						messageBoxData.MsgType = MessageType.GenericInfo;
						messageBoxData.DisplayType = MessageBoxType.Info;
						Game.MessageHandler.ShowMessage(messageBoxData);
						break;
					}
				case "/takeprisoner":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 2 || Command.Parameters.Count > 2)
						{
							AddOutputLine("Expected parameters: [UnitID] [Realm]");
							break;
						}
						int result8 = 0;
						int.TryParse(Command.Parameters[0], out result8);
						WorkingUnit value4 = null;
						Game.CurrentGame.AllUnits.TryGetValue(result8, out value4);
						if (value4 == null)
						{
							AddOutputLine("Unit not found: " + result8);
							break;
						}
						WorkingRealm realm10 = Game.CurrentGame.GetRealm(Command.Parameters[1]);
						if (realm10 == null)
						{
							AddOutputLine("Realm not found: " + Command.Parameters[1]);
							break;
						}
						value4.OwnerStack.RemoveUnit(value4);
						realm10.Prison.CaptureUnit(value4);
						AddOutputLine(realm10.Name + " has captured unit " + result8);
						break;
					}
				case "/ignorehuman":
					if (Command.Parameters.Count < 1 || Command.Parameters.Count > 1)
					{
						AddOutputLine("Expected parameters: [On|Off]");
					}
					else if (Command.Parameters[0] == "On")
					{
						Game.CurrentGame.IgnoreHumanPlayer = true;
					}
					else if (Command.Parameters[0] == "Off")
					{
						Game.CurrentGame.IgnoreHumanPlayer = false;
					}
					else
					{
						AddOutputLine("Expected parameters: [On|Off]");
					}
					break;
				case "/logging":
					if (Command.Parameters.Count < 1 || Command.Parameters.Count > 1)
					{
						AddOutputLine("Expected parameters: [On|Off]");
					}
					else if (Command.Parameters[0] == "On")
					{
						GlobalData.LoggingActive = true;
					}
					else if (Command.Parameters[0] == "Off")
					{
						GlobalData.LoggingActive = false;
					}
					else
					{
						AddOutputLine("Expected parameters: [On|Off]");
					}
					break;
				case "/watchallbattles":
					if (Command.Parameters.Count < 1 || Command.Parameters.Count > 1)
					{
						AddOutputLine("Expected parameters: [On|Off]");
					}
					else if (Command.Parameters[0] == "On")
					{
						Game.CurrentGame.WatchAllBattles = true;
					}
					else if (Command.Parameters[0] == "Off")
					{
						Game.CurrentGame.WatchAllBattles = false;
					}
					else
					{
						AddOutputLine("Expected parameters: [On|Off]");
					}
					break;
				case "/givecard":
					{
						if (Game.CurrentGame == null || Game.CurrentGame.CurrentTacticalBattle == null)
						{
							AddOutputLine("This cheat requires an active tactical battle");
							break;
						}
						if (Command.Parameters.Count < 1 || Command.Parameters.Count > 1)
						{
							AddOutputLine("Expected parameters: [Cardname]");
							break;
						}
						string text16 = Command.Parameters[0];
						CardEffect cardEffect = null;
						WorkingHero hero = Game.CurrentGame.CurrentTacticalBattle.GetPlayerstack().Hero;
						List<Type> list3 = (from t in Game.Utilities.ScriptManager.CardAssembly.GetTypes()
											where t.IsSubclassOf(typeof(CardEffect))
											select t).ToList();
						foreach (Type item2 in list3)
						{
							CardEffect cardEffect2 = (CardEffect)Activator.CreateInstance(item2, Game.CurrentGame.CurrentTacticalBattle, hero);
							if (cardEffect2.Name == text16)
							{
								cardEffect = cardEffect2;
								break;
							}
						}
						if (cardEffect == null)
						{
							AddOutputLine("Card not found: " + text16);
							break;
						}
						HeroAbilityData value6 = null;
						Game.Data.HeroAbilities.TryGetValue(cardEffect.Name, out value6);
						if (value6 != null)
						{
							cardEffect.DisplayName = value6.DisplayName;
							cardEffect.DisplayDesc = value6.DisplayDesc;
							cardEffect.SoundFile = value6.SoundFile;
							cardEffect.Panel = value6.School;
							if (hero != null)
							{
								cardEffect.ArtName = hero.GetArt(Game.CurrentGame.PlayerRealm);
							}
						}
						Game.CurrentGame.CurrentTacticalBattle.GrantCard(Game.CurrentGame.PlayerRealm, cardEffect);
						AddOutputLine("Granted " + text16 + " to active player ");
						break;
					}
				case "/freehero":
					if (Game.CurrentGame == null)
					{
						AddOutputLine("Game must be running to use this cheat");
					}
					else if (Command.Parameters.Count > 0)
					{
						AddOutputLine("Expected no parameters");
					}
					else
					{
						Game.CurrentGame.EconomyController.OfferFreeHero(Game.CurrentGame.PlayerRealm);
					}
					break;
				case "/checkstacks":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count > 0)
						{
							AddOutputLine("Expected no parameters");
							break;
						}
						List<int> list = new List<int>();
						foreach (WorkingStack value10 in Game.CurrentGame.AllStacks.Values)
						{
							if (value10.Units.Count == 0)
							{
								AddOutputLine("Stack " + value10.ID + ", owned by " + value10.Owner.Name + " has no units");
							}
							if (list.Contains(value10.NodeID))
							{
								AddOutputLine("Node " + value10.NodeID + " has multiple stacks.");
							}
							else
							{
								list.Add(value10.NodeID);
							}
						}
						AddOutputLine("Stack check complete");
						break;
					}
				case "/giveflag":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 2 || Command.Parameters.Count > 2)
						{
							AddOutputLine("Expected parameters: [UnitID] [Flagname]");
							break;
						}
						int result = 0;
						int.TryParse(Command.Parameters[0], out result);
						WorkingUnit value2 = null;
						Game.CurrentGame.AllUnits.TryGetValue(result, out value2);
						if (value2 == null)
						{
							AddOutputLine("Unit not found: " + result);
							break;
						}
						string text3 = Command.Parameters[1];
						if (!UnitFlag.NamedFlagExists(Game, text3))
						{
							AddOutputLine("Flag not found: " + text3);
							break;
						}
						UnitFlag flag = UnitFlag.CreateNamedFlag(Game, text3);
						value2.GrantFlag(flag);
						AddOutputLine("Granted " + text3 + " to unit " + result);
						break;
					}
				case "/unitxp":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 2 || Command.Parameters.Count > 2)
						{
							AddOutputLine("Expected parameters: [UnitID] [XP]");
							break;
						}
						int result3 = 0;
						int.TryParse(Command.Parameters[0], out result3);
						WorkingUnit value3 = null;
						Game.CurrentGame.AllUnits.TryGetValue(result3, out value3);
						if (value3 == null)
						{
							AddOutputLine("Unit not found: " + result3);
							break;
						}
						int result4 = 0;
						int.TryParse(Command.Parameters[1], out result4);
						value3.XP += result4;
						AddOutputLine("Unit " + result3 + " granted " + result4 + " XP");
						break;
					}
				case "/unitids":
					if (Command.Parameters.Count < 1 || Command.Parameters.Count > 1)
					{
						AddOutputLine("Expected parameters: [On|Off]");
					}
					else if (Command.Parameters[0] == "On")
					{
						Game.UnitIDsEnabled = true;
					}
					else if (Command.Parameters[0] == "Off")
					{
						Game.UnitIDsEnabled = false;
					}
					else
					{
						AddOutputLine("Expected parameters: [On|Off]");
					}
					break;
				case "/heroxp":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 2 || Command.Parameters.Count > 2)
						{
							AddOutputLine("Expected parameters: [HeroID] [XP]");
							break;
						}
						int result10 = 0;
						int.TryParse(Command.Parameters[0], out result10);
						WorkingHero value7 = null;
						Game.CurrentGame.AllHeroes.TryGetValue(result10, out value7);
						if (value7 == null)
						{
							AddOutputLine("Hero not found: " + result10);
							break;
						}
						int result11 = 0;
						int.TryParse(Command.Parameters[1], out result11);
						value7.XP += result11;
						AddOutputLine("Unit " + result10 + " granted " + result11 + " XP");
						break;
					}
				case "/heroids":
					if (Command.Parameters.Count < 1 || Command.Parameters.Count > 1)
					{
						AddOutputLine("Expected parameters: [On|Off]");
					}
					else if (Command.Parameters[0] == "On")
					{
						Game.HeroIDsEnabled = true;
					}
					else if (Command.Parameters[0] == "Off")
					{
						Game.HeroIDsEnabled = false;
					}
					else
					{
						AddOutputLine("Expected parameters: [On|Off]");
					}
					break;
				case "/movecosts":
					if (Command.Parameters.Count < 1 || Command.Parameters.Count > 1)
					{
						AddOutputLine("Expected parameters: [On|Off]");
					}
					else if (Command.Parameters[0] == "On")
					{
						Game.ShowMoveOverlay = true;
					}
					else if (Command.Parameters[0] == "Off")
					{
						Game.ShowMoveOverlay = false;
					}
					else
					{
						AddOutputLine("Expected parameters: [On|Off]");
					}
					break;
				case "/ally":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 2 || Command.Parameters.Count > 2)
						{
							AddOutputLine("Expected parameters: [RealmA] [RealmB]");
							break;
						}
						WorkingRealm realm11 = Game.CurrentGame.GetRealm(Command.Parameters[0]);
						WorkingRealm realm12 = Game.CurrentGame.GetRealm(Command.Parameters[1]);
						if (realm11 == null)
						{
							AddOutputLine("Realm not found: " + Command.Parameters[0]);
							break;
						}
						if (realm12 == null)
						{
							AddOutputLine("Realm not found: " + Command.Parameters[1]);
							break;
						}
						Game.CurrentGame.AllianceController.FormTreaty(realm11, realm12, TreatyTypes.Alliance);
						AddOutputLine(realm11.Name + " is allied with " + realm12.Name);
						break;
					}
				case "/forcepeace":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 2 || Command.Parameters.Count > 2)
						{
							AddOutputLine("Expected parameters: [RealmA] [RealmB]");
							break;
						}
						WorkingRealm realm = Game.CurrentGame.GetRealm(Command.Parameters[0]);
						WorkingRealm realm2 = Game.CurrentGame.GetRealm(Command.Parameters[1]);
						if (realm == null)
						{
							AddOutputLine("Realm not found: " + Command.Parameters[0]);
						}
						else if (realm2 == null)
						{
							AddOutputLine("Realm not found: " + Command.Parameters[1]);
						}
						else
						{
							Game.CurrentGame.AllianceController.ForcePeace(realm, realm2);
						}
						break;
					}
				case "/startwar":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 2 || Command.Parameters.Count > 2)
						{
							AddOutputLine("Expected parameters: [RealmA] [RealmB]");
							break;
						}
						WorkingRealm realm5 = Game.CurrentGame.GetRealm(Command.Parameters[0]);
						WorkingRealm realm6 = Game.CurrentGame.GetRealm(Command.Parameters[1]);
						if (realm5 == null)
						{
							AddOutputLine("Realm not found: " + Command.Parameters[0]);
							break;
						}
						if (realm6 == null)
						{
							AddOutputLine("Realm not found: " + Command.Parameters[1]);
							break;
						}
						Game.CurrentGame.AllianceController.EstablishWar(realm5, realm6);
						AddOutputLine(realm5.Name + " declares war on " + realm6.Name);
						break;
					}
				case "/setdisp":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 3 || Command.Parameters.Count > 3)
						{
							AddOutputLine("Expected parameters: [OriginRealm] [TargetRealm] [Value]");
							break;
						}
						WorkingRealm realm3 = Game.CurrentGame.GetRealm(Command.Parameters[0]);
						WorkingRealm realm4 = Game.CurrentGame.GetRealm(Command.Parameters[1]);
						if (realm3 == null)
						{
							AddOutputLine("Realm not found: " + Command.Parameters[0]);
							break;
						}
						if (realm4 == null)
						{
							AddOutputLine("Realm not found: " + Command.Parameters[1]);
							break;
						}
						int result2 = 0;
						int.TryParse(Command.Parameters[2], out result2);
						realm3.DiplomacyManager.SetBaseValue(realm4, result2);
						AddOutputLine(realm3.Name + " Base disposition towards " + realm4.Name + " is now " + result2);
						break;
					}
				case "/debugticker":
					if (Command.Parameters.Count < 1 || Command.Parameters.Count > 1)
					{
						AddOutputLine("Expected parameters: [On|Off]");
					}
					else if (Command.Parameters[0] == "On")
					{
						Game.DebugMessagesEnabled = true;
					}
					else if (Command.Parameters[0] == "Off")
					{
						Game.DebugMessagesEnabled = false;
					}
					else
					{
						AddOutputLine("Expected parameters: [On|Off]");
					}
					break;
				case "/giveresearch":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 2 || Command.Parameters.Count > 2)
						{
							AddOutputLine("Expected parameters: [Realmname] [Quantity]");
							break;
						}
						string text11 = Command.Parameters[0];
						WorkingRealm realm9 = Game.CurrentGame.GetRealm(text11);
						if (realm9 == null)
						{
							AddOutputLine("Realm not found: " + text11);
							break;
						}
						int result7 = 0;
						int.TryParse(Command.Parameters[1], out result7);
						realm9.MagicData.GiveMagicXP(result7);
						AddOutputLine("Granted " + result7 + " research points to " + text11);
						break;
					}
				case "/givespellpoints":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 2 || Command.Parameters.Count > 2)
						{
							AddOutputLine("Expected parameters: [Realmname] [Quantity]");
							break;
						}
						string text7 = Command.Parameters[0];
						WorkingRealm realm8 = Game.CurrentGame.GetRealm(text7);
						if (realm8 == null)
						{
							AddOutputLine("Realm not found: " + text7);
							break;
						}
						int result5 = 0;
						int.TryParse(Command.Parameters[1], out result5);
						realm8.MagicData.GiveSpellPoints(result5);
						AddOutputLine("Granted " + result5 + " spell points to " + text7);
						break;
					}
				case "/aithreat":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 1 || Command.Parameters.Count > 1)
						{
							AddOutputLine("Expected parameters: [Realmname]");
							break;
						}
						string text17 = Command.Parameters[0];
						WorkingRealm realm16 = Game.CurrentGame.GetRealm(text17);
						if (realm16 == null)
						{
							AddOutputLine("Realm not found: " + text17);
							break;
						}
						if (realm16.AIPlayer == null)
						{
							AddOutputLine("Realm is not AI: " + text17);
							break;
						}
						Game.Map.DebugRealm = realm16;
						Game.Map.ChangeMode(MapModes.Debug1);
						break;
					}
				case "/win":
					if (Game.CurrentGame == null)
					{
						AddOutputLine("Game must be running to use this cheat");
					}
					else if (Command.Parameters.Count > 0)
					{
						AddOutputLine("Expected no parameters");
					}
					else
					{
						Game.CurrentGame.HandleGameVictory();
					}
					break;
				case "/lose":
					if (Game.CurrentGame == null)
					{
						AddOutputLine("Game must be running to use this cheat");
					}
					else if (Command.Parameters.Count > 0)
					{
						AddOutputLine("Expected no parameters");
					}
					else
					{
						Game.CurrentGame.HandleGameDefeat();
					}
					break;
				case "/giveprovince":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 2 || Command.Parameters.Count > 2)
						{
							AddOutputLine("Expected parameters: [Provincename] [Realmname]");
							break;
						}
						string text4 = Command.Parameters[0];
						WorkingProvince province2 = Game.CurrentGame.GetProvince(text4);
						if (province2 == null)
						{
							AddOutputLine("Province not found: " + text4);
							break;
						}
						string text5 = Command.Parameters[1];
						WorkingRealm realm7 = Game.CurrentGame.GetRealm(text5);
						if (realm7 == null)
						{
							AddOutputLine("Realm not found: " + text5);
							break;
						}
						Game.CurrentGame.ChangeProvinceOwner(province2, realm7);
						AddOutputLine(text4 + " now belongs to " + text5);
						break;
					}
				case "/giveresource":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 3 || Command.Parameters.Count > 3)
						{
							AddOutputLine("Expected parameters: [Realmname] [Resourcename] [Quantity]");
							break;
						}
						string text12 = Command.Parameters[0];
						WorkingRealm realm13 = Game.CurrentGame.GetRealm(text12);
						if (realm13 == null)
						{
							AddOutputLine("Realm not found: " + text12);
							break;
						}
						string text13 = Command.Parameters[1];
						ResourceData value5 = null;
						Game.Data.Resources.TryGetValue(text13, out value5);
						if (value5 == null)
						{
							AddOutputLine("Resource not found: " + text13);
							break;
						}
						int result9 = 0;
						int.TryParse(Command.Parameters[2], out result9);
						if (result9 > 0)
						{
							realm13.GrantResource(value5, result9);
						}
						else
						{
							result9 *= -1;
							result9 = Math.Min(result9, realm13.GetStockpiledResource(value5));
							realm13.RemoveResource(value5, result9);
							result9 *= -1;
						}
						AddOutputLine("Granted " + result9 + " of resource " + text13);
						break;
					}
				case "/modifyrebelchance":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 2 || Command.Parameters.Count > 2)
						{
							AddOutputLine("Expected parameters: [Provincename] [ChanceModifier]");
							break;
						}
						string text9 = Command.Parameters[0];
						WorkingProvince province4 = Game.CurrentGame.GetProvince(text9);
						if (province4 == null)
						{
							AddOutputLine("No province name " + text9 + " exists");
							break;
						}
						int rebelChanceModifier = int.Parse(Command.Parameters[1]);
						province4.RebelChanceModifier = rebelChanceModifier;
						break;
					}
				case "/faire":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 1 || Command.Parameters.Count > 1)
						{
							AddOutputLine("Expected parameters: [Provincename]");
							break;
						}
						string text6 = Command.Parameters[0];
						WorkingProvince province3 = Game.CurrentGame.GetProvince(text6);
						if (province3 == null)
						{
							AddOutputLine("No province name " + text6 + " exists");
						}
						else
						{
							Game.CurrentGame.EconomyController.EnactHarvestFaire(province3);
						}
						break;
					}
				case "/givecradle":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 1 || Command.Parameters.Count > 1)
						{
							AddOutputLine("Expected parameters: [Provincename]");
							break;
						}
						string text2 = Command.Parameters[0];
						WorkingProvince province = Game.CurrentGame.GetProvince(text2);
						if (province == null)
						{
							AddOutputLine("No province name " + text2 + " exists");
						}
						else
						{
							Game.CurrentGame.EconomyController.AwardScience(province);
						}
						break;
					}
				case "/givepatron":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 1 || Command.Parameters.Count > 1)
						{
							AddOutputLine("Expected parameters: [Provincename]");
							break;
						}
						string text15 = Command.Parameters[0];
						WorkingProvince province6 = Game.CurrentGame.GetProvince(text15);
						if (province6 == null)
						{
							AddOutputLine("No province name " + text15 + " exists");
						}
						else
						{
							Game.CurrentGame.EconomyController.AwardArts(province6);
						}
						break;
					}
				case "/battlefield":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 1 || Command.Parameters.Count > 1)
						{
							AddOutputLine("Expected parameters: [Provincename]");
							break;
						}
						string text10 = Command.Parameters[0];
						WorkingProvince province5 = Game.CurrentGame.GetProvince(text10);
						if (province5 == null)
						{
							AddOutputLine("No province name " + text10 + " exists");
							break;
						}
						if (province5.BattleField != null)
						{
							AddOutputLine(text10 + " already has a battlefield");
							break;
						}
						province5.BattleField = new BattleFieldData(province5, Game.CurrentGame);
						AddOutputLine("battlefield created in " + text10);
						break;
					}
				case "/lust":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Game must be running to use this cheat");
							break;
						}
						if (Command.Parameters.Count < 2 || Command.Parameters.Count > 2)
						{
							AddOutputLine("Expected parameters: [Provincename] [Lustvalue]");
							break;
						}
						string text8 = Command.Parameters[0];
						int result6 = 0;
						int.TryParse(Command.Parameters[1], out result6);
						Game.CurrentGame.GetProvince(text8).SetLustValue(result6);
						AddOutputLine(text8 + " lust set to " + result6);
						break;
					}
				case "/cast":
					{
						if (Game.CurrentGame == null)
						{
							AddOutputLine("Cannot cast spell, game is not started yet.");
							break;
						}
						if (Command.Parameters.Count < 1)
						{
							AddOutputLine("Expected parameter [SpellName] is missing");
							break;
						}
						if (Command.Parameters.Count > 1)
						{
							AddOutputLine("Only 1 parameter expected");
							break;
						}
						string text = Command.Parameters[0];
						RealmMagicData value = null;
						Game.Data.Spells.TryGetValue(text, out value);
						if (value == null)
						{
							AddOutputLine("Unknown spell: " + text);
							break;
						}
						SpellEffect spellEffect = SpellEffect.CreateEffect(Game.CurrentGame, value, Game.CurrentGame.PlayerRealm);
						if (!spellEffect.TargetNeeded())
						{
							if (spellEffect.SpellData.TargetType == SpellTargets.None)
							{
								Game.CurrentGame.FinishSpellCasting(spellEffect, null);
							}
							if (spellEffect.SpellData.TargetType == SpellTargets.Realm)
							{
								if (!spellEffect.TargetIsValid(Game.CurrentGame.PlayerRealm))
								{
									Game.CurrentGame.CancelCasting();
								}
								else
								{
									Game.CurrentGame.FinishSpellCasting(spellEffect, Game.CurrentGame.PlayerRealm);
								}
							}
						}
						else
						{
							Game.CurrentGame.BeginSpellCasting(spellEffect);
						}
						break;
					}
				case "/clearstats":
					SteamUserStats.ResetAllStats(bAchievementsToo: true);
					break;
				default:
					AddOutputLine("Unknown Command: " + Command.CommandName);
					break;
			}
		}

		public void Show()
		{
			BGImage.BringToFront();
			BGImage.Visible = true;
			Game.UIManager.SetTextFocus(InputText);
		}

		public void Hide()
		{
			BGImage.Visible = false;
		}

		internal void Toggle()
		{
			if (!BGImage.Visible)
			{
				Show();
			}
			else
			{
				Hide();
			}
		}
	}
}