using System;
using System.Collections.Generic;
using JTLStudio.SDK.Editor.Toolkit.Components;
using UnityEditor;
using UnityEngine.UIElements;

namespace JTLStudio.SDK.Editor.Toolkit.Sections
{
    public class PauseSection : ToolkitSection
    {
        private const int LeadingWidth = 28;
        private const int NameWidth = 152;
        private const int NameGap = 12;
        private const int SwitchColumnWidth = 240;
        private const string PlatformProperty = "_platformProvider";
        private const string FocusProperty = "_pauseOnFocusLoss";

        public PauseSection(ToolkitContext context) : base(context)
        {
        }

        public override ToolkitSectionId Id => ToolkitSectionId.Pause;

        public override ToolkitStatus Status => new ToolkitStatus(StatusKind.Info, "", "");

        protected override string TemplateName => "ModuleSection";

        protected override void OnRendered()
        {
            Require<SectionHeader>("module-header").TitleKey = "nav.pause";
            VisualElement body = Require<VisualElement>("module-body");
            body.Add(CreateEffectsCard());
            body.Add(CreateSettingsCard());
            body.Add(ProvidersCard(PlatformProperty));
        }

        private VisualElement CreateEffectsCard()
        {
            JTLSDKSettings settings = Context.Project.Settings;
            Card card = new Card { TitleKey = "pause.whilePaused" };
            card.Add(Effect("pause.timeScale", settings.PauseTimeScale, value => settings.PauseTimeScale = value));
            card.Add(Effect("pause.audio", settings.PauseAudio, value => settings.PauseAudio = value));
            card.Add(Effect("pause.eventSystem", settings.DisableEventSystemOnPause, value => settings.DisableEventSystemOnPause = value));
            card.Add(Effect("pause.cursor", settings.ShowCursorOnPause, value => settings.ShowCursorOnPause = value));
            return card;
        }

        private VisualElement Effect(string labelKey, bool value, Action<bool> assign)
        {
            FieldRow row = new FieldRow(labelKey, FieldRow.DefaultLabelWidth);
            SwitchToggle toggle = new SwitchToggle(value);
            toggle.ValueChanged += changed => Context.Project.Modify(Context.Project.Settings, "Change pause settings", () => assign(changed));
            row.Add(toggle);
            return row;
        }

        private VisualElement CreateSettingsCard()
        {
            Card card = new Card { TitleKey = "simulation.behaviour", Spacing = 0 };
            IReadOnlyList<SdkConfiguration> configurations = Context.Project.Configurations;

            if (configurations.Count == 0)
            {
                card.Add(Localized("languages.noConfigurations", "jtl-text--secondary"));
                return card;
            }

            VisualElement header = Row(0);
            header.Add(Spacer(LeadingWidth + NameWidth + NameGap));
            header.Add(Column(Localized("details.pauseOnFocusLoss", "jtl-text--caption")));
            card.Add(header);

            foreach (SdkConfiguration configuration in configurations)
            {
                SerializedObject serialized = new SerializedObject(configuration);
                VisualElement row = Row(0);
                row.AddToClassList("jtl-module-row");
                row.Add(Spacer(LeadingWidth));
                VisualElement name = Row(8);
                name.style.width = NameWidth;
                name.style.flexShrink = 0;
                name.style.marginRight = NameGap;
                name.Add(new PortalMark(Context.Platforms.PortalMark(configuration.Platform), 16));
                name.Add(TextLabel(configuration.DisplayName, "jtl-text"));
                row.Add(name);
                row.Add(Column(Switch(configuration, serialized, FocusProperty)));
                card.Add(row);
            }

            return card;
        }

        private VisualElement Spacer(int width)
        {
            VisualElement spacer = new VisualElement();
            spacer.style.width = width;
            spacer.style.flexShrink = 0;
            return spacer;
        }

        private VisualElement Column(VisualElement content)
        {
            VisualElement column = new VisualElement();
            column.style.width = SwitchColumnWidth;
            column.style.flexShrink = 1;
            column.style.minWidth = 0;
            column.style.flexDirection = FlexDirection.Row;
            column.style.alignItems = Align.Center;
            column.Add(content);
            return column;
        }

        private SwitchToggle Switch(SdkConfiguration configuration, SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            SwitchToggle toggle = new SwitchToggle(property.boolValue);
            toggle.ValueChanged += value =>
            {
                serialized.Update();
                property.boolValue = value;
                serialized.ApplyModifiedProperties();
                Context.Project.Save(configuration);
            };
            return toggle;
        }
    }
}
