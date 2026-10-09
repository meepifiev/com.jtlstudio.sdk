using System.Runtime.InteropServices;

namespace JTLStudio.SDK.Bridge
{
    public static class WebInput
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void JTLSDK_BlockInput(int blocked);
#endif

        public static void Block(bool blocked)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JTLSDK_BlockInput(blocked ? 1 : 0);
#endif
        }
    }
}
