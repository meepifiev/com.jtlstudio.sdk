using System;
using System.Collections.Generic;
using System.Globalization;
using JTLStudio.SDK.Prototype;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace JTLStudio.SDK.Editor.Simulation
{
    public class GameViewOverlay : VisualElement
    {
        private const float ToolbarHeight = 21f;
        private const float Margin = 8f;
        private const float DragThreshold = 4f;
        private const float PanelWidth = 240f;
        private const string PositionKey = "JTLSDK.Simulation.OverlayPosition";
        private const string Separator = " · ";

        private readonly SimulationSession _session;
        private readonly LanguageCodes _codes = new LanguageCodes();
        private readonly Button _button;
        private readonly VisualElement _backdrop;
        private readonly VisualElement _panel;
        private readonly DropdownField _languageField;
        private readonly DropdownField _deviceField;
        private readonly VisualElement _muteRow;
        private readonly Toggle _muteToggle;
        private readonly List<Language> _languageChoices = new List<Language>();
        private Vector2 _position = new Vector2(float.NaN, float.NaN);
        private Vector2 _grab;
        private bool _dragging;
        private bool _moved;
        private bool _open;
        private bool _refreshing;

        public GameViewOverlay(SimulationSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;

            _backdrop = new VisualElement();
            _backdrop.AddToClassList("jtl-overlay-backdrop");
            _backdrop.RegisterCallback<PointerDownEvent>(pointerEvent => SetOpen(false));
            Add(_backdrop);

            _button = new Button(OnButtonClicked);
            _button.AddToClassList("jtl-overlay-button");
            _button.tooltip = "Тяните, чтобы переставить. Правая кнопка - вернуть в угол.";
            _button.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            _button.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            _button.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            _button.AddManipulator(new ContextualMenuManipulator(OnContextMenu));
            Add(_button);

            _panel = new VisualElement();
            _panel.AddToClassList("jtl-overlay-panel");
            Add(_panel);

            _languageField = new DropdownField();
            _languageField.RegisterValueChangedCallback(OnLanguageChanged);
            _panel.Add(CreateRow("Language", _languageField));

            _deviceField = new DropdownField(new List<string>(Enum.GetNames(typeof(DeviceType))), 0);
            _deviceField.RegisterValueChangedCallback(OnDeviceChanged);
            _panel.Add(CreateRow("Device", _deviceField));

            _muteToggle = new Toggle();
            _muteToggle.RegisterValueChangedCallback(OnMuteChanged);
            _muteRow = CreateRow("Platform audio muted", _muteToggle);
            _panel.Add(_muteRow);

            RegisterCallback<GeometryChangedEvent>(geometryEvent => Arrange());
            SetOpen(false);
            Refresh();
        }

        public void Refresh()
        {
            _refreshing = true;

            try
            {
                Language language = RefreshLanguages();
                RefreshDevice();
                RefreshMute();
                _button.text = "JTL" + Separator + _codes.ToCode(language).ToUpperInvariant() + Separator + _session.Settings.DeviceType + "  ▾";
            }
            finally
            {
                _refreshing = false;
            }
        }

        private void Arrange()
        {
            Vector2 view = new Vector2(resolvedStyle.width, resolvedStyle.height);

            if (float.IsNaN(view.x) || view.x <= 0f)
            {
                return;
            }

            Vector2 size = ButtonSize();

            if (float.IsNaN(_position.x))
            {
                _position = Load(view, size);
            }

            _position = Clamp(_position, view, size);
            _button.style.left = _position.x;
            _button.style.top = _position.y;
            ArrangePanel(view, size);
        }

        private void ArrangePanel(Vector2 view, Vector2 size)
        {
            float height = _panel.resolvedStyle.height;
            float panelHeight = float.IsNaN(height) || height <= 0f ? 120f : height;
            float left = _position.x + size.x > view.x * 0.5f ? _position.x + size.x - PanelWidth : _position.x;
            float top = _position.y + size.y + 4f;

            if (top + panelHeight > view.y && _position.y - panelHeight - 4f > ToolbarHeight)
            {
                top = _position.y - panelHeight - 4f;
            }

            _panel.style.left = Mathf.Clamp(left, Margin, Mathf.Max(Margin, view.x - PanelWidth - Margin));
            _panel.style.top = Mathf.Clamp(top, ToolbarHeight + Margin, Mathf.Max(ToolbarHeight + Margin, view.y - Margin));
        }

        private Vector2 ButtonSize()
        {
            float width = _button.resolvedStyle.width;
            float height = _button.resolvedStyle.height;
            return new Vector2(float.IsNaN(width) || width <= 0f ? 140f : width, float.IsNaN(height) || height <= 0f ? 18f : height);
        }

        private Vector2 Corner(Vector2 view, Vector2 size)
        {
            return new Vector2(Mathf.Max(Margin, view.x - size.x - Margin), ToolbarHeight + Margin);
        }

        private Vector2 Clamp(Vector2 position, Vector2 view, Vector2 size)
        {
            float x = Mathf.Clamp(position.x, Margin, Mathf.Max(Margin, view.x - size.x - Margin));
            float y = Mathf.Clamp(position.y, ToolbarHeight + Margin, Mathf.Max(ToolbarHeight + Margin, view.y - size.y - Margin));
            return new Vector2(x, y);
        }

        private Vector2 Load(Vector2 view, Vector2 size)
        {
            string stored = EditorPrefs.GetString(PositionKey, "");
            string[] parts = stored.Split(';');

            if (parts.Length == 2
                && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
            {
                return Clamp(new Vector2(x, y), view, size);
            }

            return Corner(view, size);
        }

        private void Save()
        {
            EditorPrefs.SetString(PositionKey, _position.x.ToString("0.#", CultureInfo.InvariantCulture) + ";" + _position.y.ToString("0.#", CultureInfo.InvariantCulture));
        }

        private void OnButtonClicked()
        {
            if (_moved)
            {
                _moved = false;
                return;
            }

            SetOpen(_open == false);
        }

        private void OnPointerDown(PointerDownEvent pointerEvent)
        {
            if (pointerEvent.button != 0)
            {
                return;
            }

            _dragging = true;
            _moved = false;
            _grab = (Vector2)pointerEvent.position - _position;
            _button.CapturePointer(pointerEvent.pointerId);
        }

        private void OnPointerMove(PointerMoveEvent pointerEvent)
        {
            if (_dragging == false)
            {
                return;
            }

            Vector2 moved = (Vector2)pointerEvent.position - _grab;

            if (_moved == false && Vector2.Distance(moved, _position) < DragThreshold)
            {
                return;
            }

            _moved = true;
            _position = Clamp(moved, new Vector2(resolvedStyle.width, resolvedStyle.height), ButtonSize());
            SetOpen(false);
            Arrange();
        }

        private void OnPointerUp(PointerUpEvent pointerEvent)
        {
            if (_dragging == false)
            {
                return;
            }

            _dragging = false;
            _button.ReleasePointer(pointerEvent.pointerId);

            if (_moved)
            {
                Save();
            }
        }

        private void OnContextMenu(ContextualMenuPopulateEvent menuEvent)
        {
            menuEvent.menu.AppendAction("Вернуть в угол", action =>
            {
                _position = Corner(new Vector2(resolvedStyle.width, resolvedStyle.height), ButtonSize());
                Save();
                Arrange();
            });
        }

        private void SetOpen(bool open)
        {
            _open = open;
            _panel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            _backdrop.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            _button.EnableInClassList("jtl-overlay-button--open", open);
        }

        private VisualElement CreateRow(string labelText, VisualElement control)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("jtl-overlay-row");
            Label label = new Label(labelText);
            label.AddToClassList("jtl-overlay-label");
            row.Add(label);
            row.Add(control);
            return row;
        }

        private Language RefreshLanguages()
        {
            _languageChoices.Clear();
            Language current = _session.Settings.StartLanguage;

            if (JTLSDK.IsCreated)
            {
                _languageChoices.AddRange(JTLSDK.Language.Supported);
                current = JTLSDK.Language.Current;
            }
            else
            {
                JTLSDKSettings settings = Resources.Load<JTLSDKSettings>(JTLSDKSettings.ResourcePath);

                if (settings != null && settings.SupportedLanguages.Count > 0)
                {
                    _languageChoices.AddRange(settings.SupportedLanguages);
                }
                else
                {
                    _languageChoices.AddRange((Language[])Enum.GetValues(typeof(Language)));
                }

                if (_languageChoices.Contains(current) == false && _languageChoices.Count > 0)
                {
                    current = _languageChoices[0];
                }
            }

            List<string> names = new List<string>();

            foreach (Language language in _languageChoices)
            {
                names.Add(language.ToString());
            }

            _languageField.choices = names;
            _languageField.SetValueWithoutNotify(current.ToString());
            return current;
        }

        private void RefreshDevice()
        {
            _deviceField.SetValueWithoutNotify(_session.Settings.DeviceType.ToString());
        }

        private void RefreshMute()
        {
            PrototypePlatformProvider platform = PrototypeBridge.ActivePlatform;
            bool visible = platform != null && platform.SupportsPlatformMute;
            _muteRow.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (visible)
            {
                _muteToggle.SetValueWithoutNotify(platform.IsPlatformMuted);
            }
        }

        private void OnLanguageChanged(ChangeEvent<string> changeEvent)
        {
            if (_refreshing)
            {
                return;
            }

            int index = _languageField.choices.IndexOf(changeEvent.newValue);

            if (index < 0 || index >= _languageChoices.Count)
            {
                return;
            }

            Language language = _languageChoices[index];
            _session.Settings.StartLanguage = language;
            _session.Settings.Save();

            if (JTLSDK.IsCreated && IsSupported(language))
            {
                JTLSDK.Language.Set(language);
            }

            Refresh();
        }

        private bool IsSupported(Language language)
        {
            foreach (Language supported in JTLSDK.Language.Supported)
            {
                if (supported == language)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDeviceChanged(ChangeEvent<string> changeEvent)
        {
            if (_refreshing)
            {
                return;
            }

            if (Enum.TryParse(changeEvent.newValue, out DeviceType deviceType) == false)
            {
                return;
            }

            _session.Settings.DeviceType = deviceType;
            _session.Settings.Save();
            Refresh();
        }

        private void OnMuteChanged(ChangeEvent<bool> changeEvent)
        {
            if (_refreshing)
            {
                return;
            }

            PrototypeBridge.ActivePlatform?.SetPlatformMuted(changeEvent.newValue);
        }
    }
}
