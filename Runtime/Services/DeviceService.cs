using System;
using System.Collections.Generic;
using JTLStudio.SDK.Bridge;
using JTLStudio.SDK.Providers;
using UnityEngine;
using UnityEngine.Rendering;

namespace JTLStudio.SDK.Services
{
    public class DeviceService : ModuleBase, IDevice
    {
        private const string EventSystemTypeName = "UnityEngine.EventSystems.EventSystem, UnityEngine.UI";
        private const int PausedRenderFrameInterval = 100000;

        private readonly IPlatformProvider _platformProvider;
        private readonly PauseService _pause;
        private readonly bool _showCursorOnPause;
        private readonly bool _disableEventSystemOnPause;
        private readonly bool _pauseRendering;
        private readonly bool _blockInputOnPause;
        private int _renderFrameInterval = 1;
        private bool _inputOverride;
        private readonly System.Type _eventSystemType = System.Type.GetType(EventSystemTypeName);
        private readonly List<Behaviour> _disabledEventSystems = new List<Behaviour>();
        private bool _cursorVisible = true;
        private CursorLockMode _cursorLock = CursorLockMode.None;

        public DeviceService(IPlatformProvider platformProvider, PauseService pause, bool showCursorOnPause, bool disableEventSystemOnPause, bool pauseRendering, bool blockInputOnPause, SdkLogger logger) : base(logger)
        {
            _platformProvider = platformProvider ?? throw new ArgumentNullException(nameof(platformProvider));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            _showCursorOnPause = showCursorOnPause;
            _disableEventSystemOnPause = disableEventSystemOnPause;
            _pauseRendering = pauseRendering;
            _blockInputOnPause = blockInputOnPause;
            _pause.Changed += OnPauseChanged;
        }

        public bool IsMobile => Type == DeviceType.Mobile || Type == DeviceType.Tablet;
        public DeviceType Type => _platformProvider.DeviceType;

        public bool CursorVisible
        {
            get => _cursorVisible;
            set
            {
                _cursorVisible = value;
                Apply();
            }
        }

        public CursorLockMode CursorLock
        {
            get => _cursorLock;
            set
            {
                _cursorLock = value;
                Apply();
            }
        }

        internal override string ModuleName => "Device";

        internal override void Initialize()
        {
            SetState(ModuleState.Ready);
        }

        internal override void Dispose()
        {
            _pause.Changed -= OnPauseChanged;
            RestoreRendering();
            RestoreEventSystems();
            BlockInput(false);
        }

        private void Apply()
        {
            if (_showCursorOnPause && _pause.IsPaused)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                return;
            }

            Cursor.visible = _cursorVisible;
            Cursor.lockState = _cursorLock;
        }

        internal void KeepInputAlive(bool keep)
        {
            _inputOverride = keep;

            if (keep)
            {
                RestoreEventSystems();
                return;
            }

            if (_pause.IsPaused)
            {
                DisableEventSystems();
            }
        }

        private void OnPauseChanged(bool paused)
        {
            Apply();

            if (paused)
            {
                BlockInput(true);
                DisableEventSystems();
                StopRendering();
            }
            else
            {
                RestoreRendering();
                RestoreEventSystems();
                BlockInput(false);
                Input.ResetInputAxes();
            }
        }

        private void BlockInput(bool blocked)
        {
            if (_blockInputOnPause)
            {
                WebInput.Block(blocked);
            }
        }

        private void StopRendering()
        {
            if (_pauseRendering == false || OnDemandRendering.renderFrameInterval == PausedRenderFrameInterval)
            {
                return;
            }

            _renderFrameInterval = OnDemandRendering.renderFrameInterval;
            OnDemandRendering.renderFrameInterval = PausedRenderFrameInterval;
        }

        private void RestoreRendering()
        {
            if (_pauseRendering == false)
            {
                return;
            }

            OnDemandRendering.renderFrameInterval = _renderFrameInterval;
        }

        private void DisableEventSystems()
        {
            if (_inputOverride || _disableEventSystemOnPause == false || _eventSystemType == null)
            {
                return;
            }

            foreach (UnityEngine.Object found in UnityEngine.Object.FindObjectsOfType(_eventSystemType))
            {
                if (found is Behaviour eventSystem && eventSystem.enabled)
                {
                    eventSystem.enabled = false;
                    _disabledEventSystems.Add(eventSystem);
                }
            }
        }

        private void RestoreEventSystems()
        {
            foreach (Behaviour eventSystem in _disabledEventSystems)
            {
                if (eventSystem != null)
                {
                    eventSystem.enabled = true;
                }
            }

            _disabledEventSystems.Clear();
        }
    }
}
