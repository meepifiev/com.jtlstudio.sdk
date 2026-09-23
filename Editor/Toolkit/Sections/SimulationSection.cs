using System;
using System.Collections.Generic;
using System.Globalization;
using JTLStudio.SDK.Editor.Toolkit.Components;
using JTLStudio.SDK.Prototype;
using UnityEngine.UIElements;

namespace JTLStudio.SDK.Editor.Toolkit.Sections
{
    public class SimulationSection : ToolkitSection
    {
        private const int LabelWidth = FieldRow.CardLabelWidth;
        private const float ControlWidth = 240f;
        private const float DropdownWidth = 200f;
        private const float MinimumTwoColumnWidth = 820f;

        private readonly PrototypeSimulationSettings _settings = new PrototypeSimulationSettings();
        private readonly DeviceType[] _devices = { DeviceType.Desktop, DeviceType.Mobile, DeviceType.Tablet, DeviceType.TV };
        private readonly AdResult[] _interstitialResults = { AdResult.Shown, AdResult.NotShown, AdResult.Failed };
        private readonly AdResult[] _rewardedResults = { AdResult.Rewarded, AdResult.Closed, AdResult.NotShown, AdResult.Failed };
        private readonly PurchaseResult[] _purchaseResults = { PurchaseResult.Purchased, PurchaseResult.Cancelled, PurchaseResult.Failed };

        public SimulationSection(ToolkitContext context) : base(context)
        {
        }

        public override ToolkitSectionId Id => ToolkitSectionId.Simulation;

        public override ToolkitStatus Status => new ToolkitStatus(StatusKind.Info, "", "");

        protected override string TemplateName => "SimulationSection";

        protected override void OnRendered()
        {
            _settings.Load();

            SectionHeader header = Require<SectionHeader>("simulation-header");
            header.Add(Localized("simulation.overlay", "jtl-text--secondary"));
            header.Add(Switch(_settings.OverlayInGameView, value => _settings.OverlayInGameView = value));

            VisualElement body = Require<VisualElement>("simulation-body");
            VisualElement left = Column(12);
            left.AddToClassList("jtl-basis");
            left.Add(CreatePlayModeCard());
            left.Add(CreateAdsCard());
            left.Add(CreatePlayerCard());
            body.Add(left);

            VisualElement right = Column(12);
            right.AddToClassList("jtl-basis");
            right.Add(CreatePurchasesCard());
            right.Add(CreateLeaderboardsCard());
            right.Add(CreateFlagsCard());
            right.Add(CreateSavesCard());
            body.Add(right);
            StackWhenNarrow(body, left, right, MinimumTwoColumnWidth, 0f);
        }

        private VisualElement CreatePlayModeCard()
        {
            Card card = new Card { TitleKey = "simulation.playMode" };
            SdkConfiguration active = Context.Project.Active;
            FieldRow platform = new FieldRow("simulation.platform", LabelWidth);
            VisualElement box = Row(6);
            box.AddToClassList("jtl-field-box");
            box.AddToClassList("jtl-read-only");
            Fit(box, ControlWidth);

            if (active != null)
            {
                box.Add(new PortalMark(Context.Platforms.PortalMark(active.Platform), 16));
            }

            box.Add(TextLabel(active == null ? Context.Text("topbar.noConfiguration") : active.DisplayName, "jtl-read-only__text"));
            platform.Add(box);
            card.Add(platform);

            card.Add(Field("simulation.device", DropdownOf(_devices, "simulation.devices", Array.IndexOf(_devices, _settings.DeviceType), index => _settings.DeviceType = _devices[index])));
            card.Add(Field("simulation.initializationDelay", Seconds(_settings.InitializationDelaySeconds, value => _settings.InitializationDelaySeconds = value)));
            card.Add(Field("simulation.simulateInitFailure", Switch(_settings.SimulateInitializationFailure, value => _settings.SimulateInitializationFailure = value)));
            return card;
        }

        private VisualElement CreateAdsCard()
        {
            Card card = new Card { TitleKey = "simulation.ads" };
            card.Add(Field("simulation.behaviour", Behaviour(_settings.AskAdResult, value => _settings.AskAdResult = value)));
            card.Add(Field("simulation.interstitial", DropdownOf(_interstitialResults, "simulation.interstitialResults", Array.IndexOf(_interstitialResults, _settings.InterstitialResult), index => _settings.InterstitialResult = _interstitialResults[index])));
            card.Add(Field("simulation.rewarded", DropdownOf(_rewardedResults, "simulation.rewardedResults", Array.IndexOf(_rewardedResults, _settings.RewardedResult), index => _settings.RewardedResult = _rewardedResults[index])));
            card.Add(Field("simulation.adDuration", Seconds(_settings.AdDurationSeconds, value => _settings.AdDurationSeconds = value)));
            return card;
        }

        private VisualElement CreatePurchasesCard()
        {
            Card card = new Card { TitleKey = "simulation.purchases" };
            card.Add(Field("simulation.behaviour", Behaviour(_settings.AskPurchaseResult, value => _settings.AskPurchaseResult = value)));
            card.Add(Field("simulation.result", DropdownOf(_purchaseResults, "simulation.purchaseResults", Array.IndexOf(_purchaseResults, _settings.PurchaseResult), index => _settings.PurchaseResult = _purchaseResults[index])));
            return card;
        }

        private VisualElement CreatePlayerCard()
        {
            Card card = new Card { TitleKey = "simulation.player" };
            card.Add(Field("simulation.authorized", Switch(_settings.Authorized, value => _settings.Authorized = value)));
            card.Add(Field("simulation.name", Text(_settings.PlayerName, value => _settings.PlayerName = value)));
            card.Add(Field("simulation.id", Text(_settings.PlayerId, value => _settings.PlayerId = value)));
            return card;
        }

        private VisualElement CreateLeaderboardsCard()
        {
            Card card = new Card { TitleKey = "simulation.leaderboards" };
            card.Add(Field("simulation.entriesAbove", Number(_settings.LeaderboardEntriesAbove, value => _settings.LeaderboardEntriesAbove = value)));
            card.Add(Field("simulation.entriesBelow", Number(_settings.LeaderboardEntriesBelow, value => _settings.LeaderboardEntriesBelow = value)));
            card.Add(Field("simulation.scoreStep", Number((int)_settings.LeaderboardScoreStep, value => _settings.LeaderboardScoreStep = value)));
            return card;
        }

        private VisualElement CreateFlagsCard()
        {
            Card card = new Card { TitleKey = "simulation.flags" };
            List<FlagDefinition> flags = Context.Project.Settings.Flags;

            if (flags.Count == 0)
            {
                card.Add(Localized("simulation.noFlags", "jtl-text--secondary"));
                return card;
            }

            foreach (FlagDefinition flag in flags)
            {
                string key = flag.Key;
                _settings.TryGetFlag(key, out string current);
                TextField field = new TextField { value = current };
                field.AddToClassList("jtl-field");
                Fit(field, ControlWidth);
                field.RegisterCallback<FocusOutEvent>(focusEvent =>
                {
                    _settings.SetFlag(key, field.value);
                    Save();
                });

                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 6;

                Label label = new Label(key + "   ·   " + flag.Type + "   " + flag.DefaultValue);
                label.AddToClassList("jtl-text--secondary");
                label.style.width = LabelWidth;
                label.style.flexShrink = 0;
                row.Add(label);
                row.Add(field);
                card.Add(row);
            }

            return card;
        }

        private VisualElement CreateSavesCard()
        {
            Card card = new Card { TitleKey = "simulation.saves" };
            card.Add(Field("simulation.loadFailure", Switch(_settings.SimulateLoadFailure, value => _settings.SimulateLoadFailure = value)));
            card.Add(Field("simulation.emptySave", Switch(_settings.EmptySaveOnStart, value => _settings.EmptySaveOnStart = value)));
            return card;
        }

        private FieldRow Field(string labelKey, VisualElement control)
        {
            FieldRow row = new FieldRow(labelKey, LabelWidth);
            row.Add(control);
            return row;
        }

        private SwitchToggle Switch(bool value, Action<bool> assign)
        {
            SwitchToggle toggle = new SwitchToggle(value);
            toggle.ValueChanged += changed =>
            {
                assign(changed);
                Save();
            };
            return toggle;
        }

        private RadioGroup Behaviour(bool ask, Action<bool> assign)
        {
            RadioGroup group = new RadioGroup();
            group.SetChoices(Context.Localization.GetList("simulation.behaviours"));
            group.Index = ask ? 0 : 1;
            group.IndexChanged += index =>
            {
                assign(index == 0);
                Save();
            };
            return group;
        }

        private Dropdown DropdownOf<T>(T[] values, string choicesKey, int selected, Action<int> assign)
        {
            Dropdown dropdown = new Dropdown();
            Fit(dropdown, DropdownWidth);
            List<string> choices = new List<string>(Context.Localization.GetList(choicesKey));
            dropdown.choices = choices.Count == values.Length ? choices : Names(values);
            dropdown.index = Math.Max(0, selected);
            dropdown.RegisterValueChangedCallback(_ =>
            {
                if (dropdown.index >= 0 && dropdown.index < values.Length)
                {
                    assign(dropdown.index);
                    Save();
                }
            });
            return dropdown;
        }

        private NumberFieldWithUnit Seconds(float value, Action<float> assign)
        {
            NumberFieldWithUnit field = new NumberFieldWithUnit { UnitKey = "unit.seconds" };
            field.Value = value.ToString(CultureInfo.InvariantCulture);
            field.Input.RegisterCallback<FocusOutEvent>(focusEvent =>
            {
                bool valid = float.TryParse(field.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds) && seconds >= 0f;
                field.Error = valid == false;

                if (valid)
                {
                    assign(seconds);
                    Save();
                }
            });
            return field;
        }

        private TextField Number(int value, Action<int> assign)
        {
            TextField field = new TextField { value = value.ToString(CultureInfo.InvariantCulture) };
            field.AddToClassList("jtl-field");
            Fit(field, ControlWidth);
            field.RegisterCallback<FocusOutEvent>(focusEvent =>
            {
                if (int.TryParse(field.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) == false || parsed < 0)
                {
                    field.value = value.ToString(CultureInfo.InvariantCulture);
                    return;
                }

                assign(parsed);
                Save();
            });
            return field;
        }

        private TextField Text(string value, Action<string> assign)
        {
            TextField field = new TextField { value = value };
            field.AddToClassList("jtl-field");
            Fit(field, ControlWidth);
            field.RegisterCallback<FocusOutEvent>(focusEvent =>
            {
                assign(field.value);
                Save();
            });
            return field;
        }

        private List<string> Names<T>(T[] values)
        {
            List<string> names = new List<string>();

            foreach (T value in values)
            {
                names.Add(value.ToString());
            }

            return names;
        }

        private void Save()
        {
            _settings.Save();
            Context.Report(StatusKind.Success, "simulation.saved");
        }
    }
}
