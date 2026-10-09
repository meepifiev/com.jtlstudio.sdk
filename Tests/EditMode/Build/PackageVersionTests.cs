using NUnit.Framework;
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace JTLStudio.SDK.Tests.Build
{
    public class PackageVersionTests
    {
        private const string PackagePath = "Packages/com.jtlstudio.sdk";

        [Test]
        public void FacadeVersionMatchesThePackage()
        {
            PackageInfo package = PackageInfo.FindForAssetPath(PackagePath);

            Assert.IsNotNull(package, "The package was not found at " + PackagePath);
            Assert.AreEqual(package.version, JTLSDK.Version);
        }
    }
}
