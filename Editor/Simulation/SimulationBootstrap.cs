using UnityEditor;

namespace JTLStudio.SDK.Editor.Simulation
{
    [InitializeOnLoad]
    public static class SimulationBootstrap
    {
        private static SimulationSession _session;

        static SimulationBootstrap()
        {
            EditorApplication.update += EnsureSession;
            AssemblyReloadEvents.beforeAssemblyReload += StopSession;
        }

        private static void EnsureSession()
        {
            if (_session != null || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            _session = new SimulationSession();
        }

        private static void StopSession()
        {
            EditorApplication.update -= EnsureSession;
            _session?.Dispose();
            _session = null;
        }
    }
}
