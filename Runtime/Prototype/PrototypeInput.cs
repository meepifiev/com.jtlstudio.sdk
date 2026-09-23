#if UNITY_EDITOR
namespace JTLStudio.SDK.Prototype
{
    internal static class PrototypeInput
    {
        private static int _holds;

        public static void Hold()
        {
            _holds++;
            Apply();
        }

        public static void Release()
        {
            _holds = _holds > 0 ? _holds - 1 : 0;
            Apply();
        }

        private static void Apply()
        {
            JTLSDK.Current?.DeviceInput.KeepInputAlive(_holds > 0);
        }
    }
}
#endif
