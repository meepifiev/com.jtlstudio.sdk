using System;
using UnityEngine;

namespace JTLStudio.SDK
{
    [Serializable]
    public class PlayerSettingsPreset
    {
        public const string DefaultTemplate = "PROJECT:JTLSDK";

        [SerializeField] private bool _applyTemplate = true;
        [SerializeField] private string _template = DefaultTemplate;
        [SerializeField] private bool _applyExceptions;
        [SerializeField] private ExceptionSupport _exceptions = ExceptionSupport.ExplicitlyThrownExceptionsOnly;
        [SerializeField] private bool _applyCompression = true;
        [SerializeField] private WebCompression _compression = WebCompression.Brotli;
        [SerializeField] private bool _applyDecompressionFallback = true;
        [SerializeField] private bool _decompressionFallback;
        [SerializeField] private bool _applyNameFilesAsHashes;
        [SerializeField] private bool _nameFilesAsHashes;
        [SerializeField] private bool _applyDataCaching = true;
        [SerializeField] private bool _dataCaching = true;
        [SerializeField] private bool _applyStripping = true;
        [SerializeField] private StrippingLevel _stripping = StrippingLevel.Medium;
        [SerializeField] private bool _applyRunInBackground = true;
        [SerializeField] private bool _runInBackground = true;
        [SerializeField] private bool _applyDebugSymbols = true;
        [SerializeField] private DebugSymbolMode _debugSymbols = DebugSymbolMode.Off;
        [SerializeField] private bool _applyMemorySize;
        [SerializeField] private int _memorySizeMegabytes = 512;

        public bool ApplyTemplate { get => _applyTemplate; set => _applyTemplate = value; }
        public string Template { get => _template; set => _template = value ?? DefaultTemplate; }
        public bool ApplyExceptions { get => _applyExceptions; set => _applyExceptions = value; }
        public ExceptionSupport Exceptions { get => _exceptions; set => _exceptions = value; }
        public bool ApplyCompression { get => _applyCompression; set => _applyCompression = value; }
        public WebCompression Compression { get => _compression; set => _compression = value; }
        public bool ApplyDecompressionFallback { get => _applyDecompressionFallback; set => _applyDecompressionFallback = value; }
        public bool DecompressionFallback { get => _decompressionFallback; set => _decompressionFallback = value; }
        public bool ApplyNameFilesAsHashes { get => _applyNameFilesAsHashes; set => _applyNameFilesAsHashes = value; }
        public bool NameFilesAsHashes { get => _nameFilesAsHashes; set => _nameFilesAsHashes = value; }
        public bool ApplyDataCaching { get => _applyDataCaching; set => _applyDataCaching = value; }
        public bool DataCaching { get => _dataCaching; set => _dataCaching = value; }
        public bool ApplyStripping { get => _applyStripping; set => _applyStripping = value; }
        public StrippingLevel Stripping { get => _stripping; set => _stripping = value; }
        public bool ApplyRunInBackground { get => _applyRunInBackground; set => _applyRunInBackground = value; }
        public bool RunInBackground { get => _runInBackground; set => _runInBackground = value; }
        public bool ApplyDebugSymbols { get => _applyDebugSymbols; set => _applyDebugSymbols = value; }
        public DebugSymbolMode DebugSymbols { get => _debugSymbols; set => _debugSymbols = value; }
        public bool ApplyMemorySize { get => _applyMemorySize; set => _applyMemorySize = value; }
        public int MemorySizeMegabytes { get => _memorySizeMegabytes; set => _memorySizeMegabytes = Mathf.Max(32, value); }
    }
}
