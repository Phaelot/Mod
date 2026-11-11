using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.IO;
using System.Threading;
using SovereigntyTK.UI.Controls;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Game.Data;
using System.Linq;
using SovereigntyTK.Game;
using SovereigntyTK.Utility;
using SovereigntyTK.Game.Campaign;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.UI
{
    public class TickerPanel: ScriptBase
    {
        const int MaxEntries = 50;
        // Wide enough for typical "Non-Aggression Treaty signed between X and Y" on one line.
        const int WrapChars = 72;
        const int PanelWidth = 560;
        const int LogWidth = 520;
        const string WrapIndent = "   ";

        static readonly Color NormalColour = Color.FromArgb(255, 238, 161);
        // White for the filterable categories: foreign treaties + battle reports (always white when shown).
        static readonly Color HighlightColour = Color.FromArgb(255, 255, 255);
        static readonly string[] HighlightTextNames = { "NEW_TREATY", "END_TREATY", "BATTLE_ATTACKED", "BATTLE_DEFEATED" };
        List<string> HighlightPhrases;

        class TickerEntry
        {
            public GameText Text;
            public bool UseHighlight;
        }

        class TickerRow
        {
            public string Text;
            public Color Colour;
        }

        // Ticker messages also arrive from AI turn threads, so the handler only queues entries;
        // controls are touched from Update/UI callbacks on the render thread.
        readonly object EntryLock = new object();
        List<TickerEntry> TickerTexts;
        List<TickerEntry> PendingTexts;
        ControlText Ticker;
        ControlList<TickerRow> LogList;
        bool LogDirty;
        float Timer;

        public bool IsOpen;

        public void FormLoaded(UIControl Control)
        {
            GameSystem.RegisterEvent(HandleCloseRequest, "CloseTicker");
            GameSystem.RegisterEvent(GameSystem_OnScriptEvent, "TickerMessage");
            GameSystem.OnViewportChanged += GameSystem_OnViewportChanged;
            GameSystem.CurrentGame.OnPlayerTurnStarted += HandlePlayerTurnStarted;

            Ticker = GameForm.GetControlByName<ControlText>("TickerText");

            // One row per line so each message can have its own colour (the text renderer has no colour tags).
            LogList = new ControlList<TickerRow>(GameSystem);
            GameForm.AddChild(LogList);
            LogList.ManualUpdate = true;
            LogList.HighlightEnabled = false;
            LogList.SelectionEnabled = false;
            LogList.OnListItemUpdated += LogList_OnListItemUpdated;
            LogList.SetVisibleItems(18);
            LogList.SetBounds(8, 4, LogWidth, 292, UIUnits.Pixel);
            LogList.Visible = false;

            TickerTexts = new List<TickerEntry>();
            PendingTexts = new List<TickerEntry>();

            foreach (GameText Text in GameSystem.CurrentGame.TickerTexts)
            {
                TickerTexts.Add(new TickerEntry { Text = Text, UseHighlight = IsHighlightText(Text) });
            }

            ShowCollapsed();
        }

        private void HandleCloseRequest(string EventName, params object[] Args)
        {
            if (IsOpen) CloseTicker();
        }

        void GameSystem_OnViewportChanged()
        {
            GameForm.SetBounds(0, 30, PanelWidth, 30, UIUnits.Pixel);
            if (IsOpen) OpenTicker();
            else CloseTicker();
        }

        void GameSystem_OnScriptEvent(string EventName, params object[] Args)
        {
            if (EventName != "TickerMessage") return;

            TickerMessage Msg = (TickerMessage)Args[0];

            // Foreign treaty/battle filtering happens where the messages are raised (SovereigntyGameLib).
            GameText Text = GameText.CreateLocalised("FORMAT_TICKER");
            Text.AddChildText(Msg.MessageText);
            TickerEntry Entry = new TickerEntry { Text = Text, UseHighlight = IsHighlightText(Msg.MessageText) || IsHighlightText(Text) };

            lock (EntryLock)
            {
                if (TickerTexts.Count == MaxEntries) TickerTexts.RemoveAt(0);
                TickerTexts.Add(Entry);

                if (IsOpen) LogDirty = true;
                else PendingTexts.Add(Entry);
            }
        }

        // White highlight: battle reports + treaty signed/ended (the two filterable message families).
        private bool IsHighlightText(GameText Text)
        {
            if (Text == null) return false;

            if (HighlightTextNames.Contains(Text.TextName)) return true;

            // Saved logs and FORMAT_TICKER wrappers only have the resolved string, so match it
            // against the fixed part of each template in the current language.
            string Resolved = Text.GetActualText(GameSystem);
            if (string.IsNullOrEmpty(Resolved)) return false;
            if (HighlightPhrases == null) HighlightPhrases = BuildHighlightPhrases();
            return HighlightPhrases.Any(Phrase => Resolved.IndexOf(Phrase, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        // Longest literal fragment between [textN] placeholders, e.g. " Treaty signed between ".
        private List<string> BuildHighlightPhrases()
        {
            List<string> Phrases = new List<string>();
            foreach (string Name in HighlightTextNames)
            {
                string Template = GameText.CreateLocalised(Name).GetActualText(GameSystem);
                string Longest = Regex.Split(Template, @"\[[Tt]ext\d+\]")
                    .OrderByDescending(Part => Part.Trim().Length)
                    .FirstOrDefault();
                if (Longest != null && Longest.Trim().Length >= 4) Phrases.Add(Longest);
            }
            return Phrases;
        }

        public override void Dispose()
        {
            GameSystem.UnregisterEvent(HandleCloseRequest, "CloseTicker");
            GameSystem.UnregisterEvent(GameSystem_OnScriptEvent, "TickerMessage");
            GameSystem.OnViewportChanged -= GameSystem_OnViewportChanged;
            if (GameSystem.CurrentGame != null)
            {
                GameSystem.CurrentGame.OnPlayerTurnStarted -= HandlePlayerTurnStarted;
            }
        }

        void HandlePlayerTurnStarted(WorkingRealm Realm)
        {
            if (Realm == null || Realm != GameSystem.CurrentGame.PlayerRealm) return;

            // Match TopPanel / save detail: displayed turn is TurnNumber + 1.
            int Turn = GameSystem.CurrentGame.TurnController.TurnNumber + 1;
            GameSystem.FireEvent("TickerMessage", new TickerMessage(
                GameText.CreateFromLiteral("------------------------"), TickerMessageType.Default, 1));
            GameSystem.FireEvent("TickerMessage", new TickerMessage(
                GameText.CreateLocalised("TICKER_TURN_START", Turn), TickerMessageType.Default, 1));
        }

        public void TickerToggleClick(UIControl Sender)
        {
            if (IsOpen) CloseTicker();
            else OpenTicker();

            string Img1 = "";
            string Img2 = "";
            string Img3 = "";

            if (IsOpen)
            {
                Img1 = "Data\\Images\\HUD\\Economy\\arrow_up_normal.png";
                Img2 = "Data\\Images\\HUD\\Economy\\arrow_up_mouseover.png";
                Img3 = "Data\\Images\\HUD\\Economy\\arrow_up_pressed.png";
            }
            else
            {
                Img1 = "Data\\Images\\HUD\\Economy\\arrow_down_normal.png";
                Img2 = "Data\\Images\\HUD\\Economy\\arrow_down_mouseover.png";
                Img3 = "Data\\Images\\HUD\\Economy\\arrow_down_pressed.png";
            }

            GameForm.GetControlByName<ControlButton>("ToggleButton").SetImageFiles(Img1, Img2, Img3);
        }

        public override void Update(float ElapsedTime)
        {
            if (Ticker == null) return;

            if (IsOpen)
            {
                if (LogDirty) RebuildLog();
                return;
            }

            TickerEntry Next = null;
            lock (EntryLock)
            {
                if (PendingTexts.Count > 0)
                {
                    Timer += ElapsedTime;
                    if (Timer > 0.25f)
                    {
                        Timer -= 0.25f;
                        Next = PendingTexts[0];
                        PendingTexts.RemoveAt(0);
                    }
                }
            }

            if (Next != null) ShowEntry(Next);
        }

        private void ShowEntry(TickerEntry Entry)
        {
            Ticker.Visible = true;
            Ticker.SetTextData(Entry.Text);
            // SetTextData can reset colour from the control default — apply after.
            Ticker.Colour = Entry.UseHighlight ? HighlightColour : NormalColour;
        }

        public void OpenTicker()
        {
            lock (EntryLock)
            {
                PendingTexts.Clear();
                if (TickerTexts.Count == 0)
                {
                    Ticker.Visible = false;
                    return;
                }
                IsOpen = true;
            }

            GameForm.SetSize(PanelWidth, 300, UIUnits.Pixel);
            Ticker.Visible = false;
            LogList.Visible = true;
            RebuildLog();
        }

        public void CloseTicker()
        {
            if (IsOpen) ShowCollapsed();
        }

        // Also used on load, where IsOpen starts false: the old CloseTicker() then never set any text,
        // leaving the bar blank with its scrollbar parts drawn at their default (stretched) size.
        private void ShowCollapsed()
        {
            TickerEntry Last = null;
            lock (EntryLock)
            {
                IsOpen = false;
                if (TickerTexts.Count > 0) Last = TickerTexts[TickerTexts.Count - 1];
            }

            GameForm.SetSize(PanelWidth, 30, UIUnits.Pixel);
            LogList.Visible = false;

            if (Last == null) Ticker.Visible = false;
            else ShowEntry(Last);
        }

        private void RebuildLog()
        {
            List<TickerEntry> Snapshot;
            lock (EntryLock)
            {
                LogDirty = false;
                Snapshot = TickerTexts.ToList();
            }

            LogList.ClearItems();
            foreach (TickerEntry Entry in Snapshot)
            {
                Color Colour = Entry.UseHighlight ? HighlightColour : NormalColour;
                foreach (string Line in WrapText(Entry.Text.GetActualText(GameSystem)))
                {
                    LogList.AddItem(new TickerRow { Text = Line, Colour = Colour });
                }
            }
            LogList.ScrollToBottom();
        }

        void LogList_OnListItemUpdated(ListItem<TickerRow> Item)
        {
            ControlText Line = new ControlText(GameSystem);
            Line.SetBounds(0, 0, Item.Container.Sprite.Bounds.Width, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
            Line.SetFontSize((int)(Item.Container.Sprite.Bounds.Height * 0.75f), UIUnits.PixelScaled);
            Line.FontName = "Trebuchet MS";
            Line.Bold = true;
            Line.TextAnchor = AnchorPoints.Left;
            Line.SetTextData(GameText.CreateFromLiteral(Item.Data.Text));
            // SetTextData can reset colour from the control default — apply after.
            Line.Colour = Item.Data.Colour;
            Item.Container.AddChild(Line);
        }

        // Rows are one line tall, so long messages are pre-wrapped (approximate width of the 12px bold font).
        private static List<string> WrapText(string Text)
        {
            List<string> Lines = new List<string>();
            foreach (string Part in Text.Split('\n'))
            {
                string Remaining = Part.Trim();
                if (Remaining.Length == 0) continue;

                while (Remaining.Length > WrapChars)
                {
                    int Cut = Remaining.LastIndexOf(' ', WrapChars);
                    if (Cut <= WrapIndent.Length) Cut = WrapChars;

                    Lines.Add(Remaining.Substring(0, Cut).TrimEnd());
                    Remaining = WrapIndent + Remaining.Substring(Cut).TrimStart();
                }
                Lines.Add(Remaining);
            }
            return Lines;
        }
    }
}
