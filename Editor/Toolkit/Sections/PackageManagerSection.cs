using System;
using System.Collections.Generic;
using System.IO;
using JTLStudio.SDK.Editor.Configuration;
using JTLStudio.SDK.Editor.Toolkit.Components;
using JTLStudio.SDK.Editor.Updates;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace JTLStudio.SDK.Editor.Toolkit.Sections
{
    public class PackageManagerSection : ToolkitSection
    {
        private const string PackageGitUrl = "https://github.com/meepifiev/com.jtlstudio.sdk.git#";

        private const string LogoPath = "Packages/com.jtlstudio.sdk/Editor/Toolkit/Icons/Brand/jtlsdk-template-logo.png";
        private const int LogoSize = 34;

        private readonly ModuleCatalog _catalog = new ModuleCatalog();
        private readonly ModuleUpdates _moduleUpdates = new ModuleUpdates();
        private readonly TemplateService _template = new TemplateService();
        private ModuleCatalogResult _modules;
        private bool _checkingModules;

        public PackageManagerSection(ToolkitContext context) : base(context)
        {
            Context.Updates.Changed += OnUpdatesChanged;
        }

        public override ToolkitSectionId Id => ToolkitSectionId.PackageManager;

        public override ToolkitStatus Status => new ToolkitStatus(StatusKind.Info, "", "");

        protected override string TemplateName => "PackageManagerSection";

        private SdkUpdates Updates => Context.Updates;

        private string InstalledVersion => Updates.InstalledVersion;

        private bool IsChecking => Updates.IsChecking || _checkingModules;

        protected override void OnRendered()
        {
            Updates.CheckOnce();

            if (_modules == null && _checkingModules == false)
            {
                CheckModules();
            }

            VisualElement body = Require<VisualElement>("package-body");
            body.Add(CreateSdkCard());
            body.Add(CreateTemplateCard());
            body.Add(CreateModulesCard());

            VisualElement footer = Row(10);
            footer.Add(TextLabel(IsChecking ? Context.Text("package.checking") : Context.Text("package.checkedAtFormat", Updates.CheckedAt ?? "-"), "jtl-text--caption"));
            footer.Add(Spacer());
            footer.Add(Button("package.checkNow", ToolkitButton.GhostVariant, "refresh", Check));
            body.Add(footer);
        }

        private VisualElement CreateSdkCard()
        {
            Card card = new Card();
            VisualElement header = Row(14);
            VisualElement logo = new VisualElement();
            logo.AddToClassList("jtl-brand__logo");
            logo.AddToClassList("jtl-brand__logo--package");
            header.Add(logo);

            ReleaseInfo latest = Updates.Latest;
            bool updateAvailable = latest != null && Updates.CompareVersions(latest.Version, InstalledVersion) > 0;
            header.Add(TextLabel(SdkStateText(latest, updateAvailable), "jtl-text--caption"));
            header.Add(Spacer());

            if (updateAvailable)
            {
                header.Add(new Badge("badge.updateAvailable", Badge.AccentVariant));
                ToolkitButton update = new ToolkitButton { Label = Context.Text("package.updateToFormat", latest.Version), Variant = ToolkitButton.PrimaryVariant };
                update.clicked += () => UpdateSdk(latest);
                header.Add(update);
            }

            card.Add(header);
            return card;
        }


        private VisualElement CreateTemplateCard()
        {
            Card card = new Card { Spacing = 8 };
            VisualElement row = Row(10);
            row.Add(Logo(AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath), "template"));
            VisualElement text = Column(2);
            text.Add(TextLabel(Context.Text("package.webglTemplate"), "jtl-text"));
            bool installed = _template.IsInstalled;
            text.Add(TextLabel(installed ? Context.Text("package.templateInstalled", TemplateService.TemplateFolder) : Context.Text("package.templateMissing"), "jtl-text--caption"));
            row.Add(text);
            row.Add(Spacer());
            ToolkitButton install = new ToolkitButton { Label = Context.Text(installed ? "template.reinstall" : "package.install"), Variant = ToolkitButton.SecondaryVariant };
            install.clicked += () =>
            {
                _template.Install();
                Context.Report(StatusKind.Success, "template.installedTo", TemplateService.TemplateFolder);
                Render();
            };
            row.Add(install);

            if (installed)
            {
                ToolkitButton remove = new ToolkitButton { Label = Context.Text("template.remove"), Variant = ToolkitButton.GhostVariant };
                remove.clicked += () =>
                {
                    if (Context.Confirm("template.removeTitle", "template.removeMessage", "template.remove") == false)
                    {
                        return;
                    }

                    _template.Uninstall();
                    Context.Report(StatusKind.Info, "template.removed");
                    Render();
                };
                row.Add(remove);
            }
            card.Add(row);
            return card;
        }

        private VisualElement CreateModulesCard()
        {
            Card card = new Card { TitleKey = "package.modules", Spacing = 8 };

            if (_modules == null)
            {
                card.Add(Localized("package.checking", "jtl-text--secondary"));
                return card;
            }

            if (_modules.IsSuccess == false)
            {
                card.Add(TextLabel(Context.Text("package.modulesFailed", _modules.Error), "jtl-text--secondary"));
                return card;
            }

            if (_modules.Modules.Count == 0)
            {
                card.Add(Localized("package.noModules", "jtl-text--secondary"));
                return card;
            }

            foreach (ModuleDefinition module in _modules.Modules)
            {
                card.Add(CreateModuleRow(module));
            }

            return card;
        }

        private VisualElement CreateModuleRow(ModuleDefinition module)
        {
            VisualElement row = Row(10);
            row.Add(Logo(module));
            VisualElement text = Column(2);
            text.Add(TextLabel(module.Name, "jtl-text"));
            PackageInfo installed = PackageInfo.FindForAssetPath("Packages/" + module.Package);
            bool compatible = string.IsNullOrEmpty(module.Requires) || Updates.CompareVersions(InstalledVersion, module.Requires) >= 0;
            string state = installed != null ? Context.Text("package.installedFormat", installed.version) : Context.Text("package.notInstalled");

            if (compatible == false)
            {
                state += " · " + Context.Text("package.requiresFormat", module.Requires);
            }

            List<string> platforms = new List<string>();

            foreach (string platform in module.Platforms)
            {
                platforms.Add(Enum.TryParse(platform, out PlatformId id) ? Context.Platforms.DisplayName(id) : platform);
            }

            if (platforms.Count > 0)
            {
                state += " · " + string.Join(", ", platforms);
            }

            text.Add(TextLabel(state, "jtl-text--caption"));
            row.Add(text);
            row.Add(Spacer());

            if (installed != null)
            {
                bool embedded = installed.source == PackageSource.Embedded || installed.source == PackageSource.Local;
                string latest = _moduleUpdates.Latest(module.Repository);

                if (embedded == false && _moduleUpdates.HasUpdate(module.Repository, installed.version))
                {
                    ToolkitButton update = new ToolkitButton { Label = Context.Text("package.updateToFormat", latest), Variant = ToolkitButton.SecondaryVariant };
                    update.clicked += () => UpdateModule(module, latest);
                    row.Add(update);
                }

                ToolkitButton remove = new ToolkitButton { Label = Context.Text("package.remove"), Variant = ToolkitButton.GhostVariant };
                remove.SetEnabled(embedded == false);
                remove.clicked += () => RemoveModule(module);
                row.Add(remove);
                return row;
            }

            ToolkitButton install = new ToolkitButton { Label = Context.Text("package.install"), Variant = ToolkitButton.SecondaryVariant };
            install.SetEnabled(compatible);
            install.clicked += () => InstallModule(module);
            row.Add(install);
            return row;
        }

        private string SdkStateText(ReleaseInfo latest, bool updateAvailable)
        {
            string installed = Context.Text("package.installedFormat", InstalledVersion);

            ReleaseCheckResult releases = Updates.Releases;

            if (Updates.IsChecking)
            {
                return installed + " · " + Context.Text("package.checking");
            }

            if (releases == null)
            {
                return installed;
            }

            if (releases.IsSuccess == false)
            {
                return installed + " · " + Context.Text(releases.IsRepositoryMissing ? "package.repositoryMissing" : "package.checkFailed");
            }

            if (latest == null)
            {
                return installed + " · " + Context.Text("package.noReleases");
            }

            return installed + " · " + (updateAvailable ? Context.Text("package.availableFormat", latest.Version) : Context.Text("package.upToDate"));
        }


        private void Check()
        {
            Updates.Check();
            CheckModules();
        }

        private void CheckModules()
        {
            _checkingModules = true;
            _moduleUpdates.Changed -= Render;
            _moduleUpdates.Changed += Render;
            _catalog.Fetch(result =>
            {
                _modules = result;
                _checkingModules = false;
                Render();
            });
        }

        private void OnUpdatesChanged()
        {
            if (Root.panel != null)
            {
                Render();
            }
        }

        private VisualElement Logo(ModuleDefinition module)
        {
            return Logo(ModuleLogo(module), "package");
        }

        private VisualElement Logo(Texture2D texture, string fallbackIcon)
        {
            VisualElement logo = new VisualElement();
            logo.style.width = LogoSize;
            logo.style.height = LogoSize;
            logo.style.flexShrink = 0;

            if (texture != null)
            {
                logo.style.backgroundImage = texture;
                SetScaleMode(logo);
                return logo;
            }

            logo.Add(new Icon(fallbackIcon, 20, "secondary"));
            return logo;
        }

        private Texture2D ModuleLogo(ModuleDefinition module)
        {
            Texture2D own = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/" + module.Package + "/Editor/Logo.png");
            return own != null ? own : AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
        }

        private void SetScaleMode(VisualElement element)
        {
#if UNITY_2022_1_OR_NEWER
            element.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            element.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
            element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
#else
            element.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
#endif
        }

        private void UpdateModule(ModuleDefinition module, string version)
        {
            Client.Add(_catalog.GitUrl(module, "v" + version));
            Context.Report(StatusKind.Info, "package.installing", module.Name);
        }

        private void InstallModule(ModuleDefinition module)
        {
            ReleaseInfo latest = module.Repository == SdkUpdates.Repository ? Updates.Latest : null;
            Client.Add(_catalog.GitUrl(module, latest == null ? "" : latest.Tag));
            Context.Report(StatusKind.Info, "package.installing", module.Name);
        }

        private void RemoveModule(ModuleDefinition module)
        {
            bool confirmed = EditorUtility.DisplayDialog(module.Name, Context.Text("package.removeMessage", module.Name), Context.Text("package.remove"), Context.Text("details.cancel"));

            if (confirmed == false)
            {
                return;
            }

            Client.Remove(module.Package);
            Context.Report(StatusKind.Info, "package.removing", module.Name);
        }

        private void UpdateSdk(ReleaseInfo release)
        {
            bool confirmed = EditorUtility.DisplayDialog(Context.Text("package.updateTitle"), Context.Text("package.updateMessage", release.Version), Context.Text("package.update"), Context.Text("details.cancel"));

            if (confirmed == false)
            {
                return;
            }

            Client.Add(PackageGitUrl + release.Tag);
            Context.Report(StatusKind.Info, "package.updating", release.Version);
        }
    }
}
