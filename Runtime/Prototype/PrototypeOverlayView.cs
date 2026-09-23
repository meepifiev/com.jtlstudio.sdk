#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JTLStudio.SDK.Prototype
{
    public class PrototypeOverlayView : MonoBehaviour
    {
        private const int SortingOrder = 32760;
        private const float MaximumWidth = 560f;
        private const float ScreenMargin = 24f;

        private static PrototypeOverlayView instance;

        private Canvas _canvas;
        private RectTransform _card;
        private RectTransform _options;
        private RectTransform _viewport;
        private Text _title;
        private Toggle _remember;
        private readonly List<GameObject> _optionRows = new List<GameObject>();
        private Action<int, bool> _onChosen;

        public static bool IsShowing => instance != null && instance._canvas != null && instance._canvas.enabled;

        public static void Show(string title, IReadOnlyList<string> captions, IReadOnlyList<string> notes, Action<int, bool> onChosen)
        {
            if (instance == null)
            {
                GameObject host = new GameObject("JTLSDK Simulation");
                host.hideFlags = HideFlags.HideInHierarchy;
                DontDestroyOnLoad(host);
                instance = host.AddComponent<PrototypeOverlayView>();
                instance.Build();
            }

            instance.Present(title, captions, notes, onChosen);
        }

        public static void Hide()
        {
            if (instance != null)
            {
                instance.Close();
            }
        }

        [UnityEditor.InitializeOnLoadMethod]
        private static void WatchPlayMode()
        {
            RemoveLeftovers();
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(UnityEditor.PlayModeStateChange change)
        {
            if (change == UnityEditor.PlayModeStateChange.ExitingPlayMode || change == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                RemoveLeftovers();
            }
        }

        private static void RemoveLeftovers()
        {
            instance = null;

            foreach (PrototypeOverlayView view in Resources.FindObjectsOfTypeAll<PrototypeOverlayView>())
            {
                if (view != null && view.gameObject != null)
                {
                    DestroyImmediate(view.gameObject);
                }
            }
        }

        private void Build()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = SortingOrder;

            _canvas.pixelPerfect = true;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.referencePixelsPerUnit = 100f;

            gameObject.AddComponent<GraphicRaycaster>();

            Image backdrop = Create("Backdrop", transform).gameObject.AddComponent<Image>();
            backdrop.color = new Color(0.02f, 0.03f, 0.06f, 0.88f);
            Fill(backdrop.rectTransform);

            _card = Create("Card", transform);
            _card.anchorMin = new Vector2(0.5f, 0.5f);
            _card.anchorMax = new Vector2(0.5f, 0.5f);
            _card.pivot = new Vector2(0.5f, 0.5f);
            _card.sizeDelta = new Vector2(MaximumWidth, 200f);
            Image cardImage = _card.gameObject.AddComponent<Image>();
            cardImage.color = new Color(0.07f, 0.10f, 0.16f, 1f);

            VerticalLayoutGroup layout = _card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.spacing = 8f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            _title = Label(_card, "", 18, new Color(0.91f, 0.93f, 0.97f, 1f));
            _title.fontStyle = FontStyle.Bold;

            _viewport = Create("Viewport", _card);
            _viewport.gameObject.AddComponent<RectMask2D>();
            LayoutElement viewportLayout = _viewport.gameObject.AddComponent<LayoutElement>();
            viewportLayout.flexibleHeight = 1f;

            _options = Create("Options", _viewport);
            _options.anchorMin = new Vector2(0f, 1f);
            _options.anchorMax = new Vector2(1f, 1f);
            _options.pivot = new Vector2(0.5f, 1f);
            _options.offsetMin = Vector2.zero;
            _options.offsetMax = Vector2.zero;

            VerticalLayoutGroup optionsLayout = _options.gameObject.AddComponent<VerticalLayoutGroup>();
            optionsLayout.spacing = 8f;
            optionsLayout.childForceExpandHeight = false;
            optionsLayout.childControlHeight = true;
            optionsLayout.childControlWidth = true;

            ContentSizeFitter optionsFitter = _options.gameObject.AddComponent<ContentSizeFitter>();
            optionsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = _viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = _viewport;
            scroll.content = _options;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;
        }

        private void Present(string title, IReadOnlyList<string> captions, IReadOnlyList<string> notes, Action<int, bool> onChosen)
        {
            _onChosen = onChosen;
            _title.text = title;
            _canvas.enabled = true;

            foreach (GameObject option in _optionRows)
            {
                Destroy(option);
            }

            _optionRows.Clear();

            for (int index = 0; index < captions.Count; index++)
            {
                int choice = index;
                string note = notes != null && index < notes.Count ? notes[index] : "";
                _optionRows.Add(CreateOption(captions[index], note, () => Choose(choice)).gameObject);
            }

            if (_remember == null)
            {
                _remember = CreateRemember();
            }

            _remember.transform.SetAsLastSibling();
            _remember.isOn = false;
            PrototypeInput.Hold();
        }

        private void LateUpdate()
        {
            if (_canvas == null || _canvas.enabled == false)
            {
                return;
            }

            RectTransform canvasRect = (RectTransform)_canvas.transform;
            float availableWidth = Mathf.Max(220f, canvasRect.rect.width - ScreenMargin * 2f);
            float availableHeight = Mathf.Max(180f, canvasRect.rect.height - ScreenMargin * 2f);
            float optionsHeight = LayoutUtility.GetPreferredHeight(_options);
            float extra = LayoutUtility.GetPreferredHeight(_title.rectTransform) + (_remember != null ? 28f : 0f) + 60f;
            float height = Mathf.Min(availableHeight, optionsHeight + extra);

            _card.sizeDelta = new Vector2(Mathf.Min(MaximumWidth, availableWidth), height);
        }

        private void Choose(int index)
        {
            Action<int, bool> callback = _onChosen;
            bool remember = _remember != null && _remember.isOn;
            Close();

            callback?.Invoke(index, remember);
        }

        private void Close()
        {
            _onChosen = null;

            if (_canvas != null)
            {
                _canvas.enabled = false;
            }

            PrototypeInput.Release();
        }

        private RectTransform CreateOption(string caption, string note, Action onClick)
        {
            RectTransform row = Create("Option", _options);
            Image image = row.gameObject.AddComponent<Image>();
            image.color = new Color(0.12f, 0.17f, 0.26f, 1f);

            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());

            LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 44f;
            element.preferredHeight = 44f;

            Text label = Label(row, caption, 15, new Color(0.91f, 0.93f, 0.97f, 1f));
            label.alignment = TextAnchor.MiddleLeft;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(14f, 0f);
            labelRect.offsetMax = new Vector2(-14f, 0f);

            if (string.IsNullOrEmpty(note) == false)
            {
                label.text = caption + "   <color=#7E8AA2>" + note + "</color>";
                label.supportRichText = true;
            }

            return row;
        }

        private Toggle CreateRemember()
        {
            RectTransform row = Create("Remember", _card);
            LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 28f;
            element.preferredHeight = 28f;

            Toggle toggle = row.gameObject.AddComponent<Toggle>();

            RectTransform box = Create("Box", row);
            box.anchorMin = new Vector2(0f, 0.5f);
            box.anchorMax = new Vector2(0f, 0.5f);
            box.pivot = new Vector2(0f, 0.5f);
            box.sizeDelta = new Vector2(20f, 20f);
            Image background = box.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.17f, 0.26f, 1f);

            RectTransform checkmarkRect = Create("Checkmark", box);
            Fill(checkmarkRect, 4f);
            Image checkmark = checkmarkRect.gameObject.AddComponent<Image>();
            checkmark.color = new Color(0.17f, 0.42f, 0.96f, 1f);

            toggle.targetGraphic = background;
            toggle.graphic = checkmark;

            Text label = Label(row, "Remember for this session", 13, new Color(0.58f, 0.64f, 0.75f, 1f));
            label.alignment = TextAnchor.MiddleLeft;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(28f, 0f);
            labelRect.offsetMax = new Vector2(0f, 0f);

            return toggle;
        }

        private RectTransform Create(string name, Transform parent)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return (RectTransform)child.transform;
        }

        private void Fill(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private Text Label(Transform parent, string content, int size, Color color)
        {
            RectTransform rect = Create("Label", parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.resizeTextForBestFit = false;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = content;

            LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = size + 8f;
            return text;
        }
    }
}
#endif
