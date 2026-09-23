using JTLStudio.SDK.Editor.Configuration;
using NUnit.Framework;
using UnityEngine;

namespace JTLStudio.SDK.Tests.Build
{
    public class TemplateSettingsTests
    {
        [Test]
        public void ColorBackgroundIsHex()
        {
            TemplateBackground background = new TemplateBackground { Kind = BackgroundKind.Color, Color = Color.red };

            Assert.AreEqual("#FF0000", background.ToCss("background.png"));
        }

        [Test]
        public void LinearGradientUsesAngle()
        {
            TemplateBackground background = new TemplateBackground { Kind = BackgroundKind.Gradient, GradientFrom = Color.black, GradientTo = Color.white, Angle = 90 };

            Assert.AreEqual("linear-gradient(90deg, #000000, #FFFFFF)", background.ToCss("background.png"));
        }

        [Test]
        public void RadialGradientIgnoresAngle()
        {
            TemplateBackground background = new TemplateBackground { Kind = BackgroundKind.Gradient, GradientFrom = Color.black, GradientTo = Color.white, Radial = true };

            Assert.AreEqual("radial-gradient(circle, #000000, #FFFFFF)", background.ToCss("background.png"));
        }

        [Test]
        public void ImageWithoutTextureFallsBackToColor()
        {
            TemplateBackground background = new TemplateBackground { Kind = BackgroundKind.Image, Color = Color.blue };

            Assert.AreEqual("#0000FF", background.ToCss("background.png"));
        }

        [Test]
        public void ProgressAndBuildNumberAreClamped()
        {
            JTLSDKEditorSettings settings = JTLSDKEditorSettings.instance;
            int width = settings.ProgressWidthPercent;
            int number = settings.BuildNumber;

            settings.ProgressWidthPercent = 500;
            settings.BuildNumber = -3;

            Assert.AreEqual(100, settings.ProgressWidthPercent);
            Assert.AreEqual(0, settings.BuildNumber);

            settings.ProgressWidthPercent = width;
            settings.BuildNumber = number;
        }

        [Test]
        public void FixedAspectGoesIntoTheTemplate()
        {
            JTLSDKEditorSettings settings = JTLSDKEditorSettings.instance;
            bool fixedAspect = settings.FixedAspect;
            string ratio = settings.AspectRatio;
            bool freeOnMobile = settings.FreeAspectOnMobile;

            try
            {
                settings.FixedAspect = true;
                settings.AspectRatio = "16/9";
                settings.FreeAspectOnMobile = true;

                System.Collections.Generic.Dictionary<string, string> values = new TemplateService().Values(settings, null, 1, false);

                Assert.AreEqual("16/9", values["JTLSDK_ASPECT"]);
                Assert.AreEqual("free", values["JTLSDK_ASPECT_MOBILE"]);

                settings.FixedAspect = false;
                values = new TemplateService().Values(settings, null, 1, false);

                Assert.AreEqual("free", values["JTLSDK_ASPECT"]);
            }
            finally
            {
                settings.FixedAspect = fixedAspect;
                settings.AspectRatio = ratio;
                settings.FreeAspectOnMobile = freeOnMobile;
            }
        }

        [Test]
        public void PageTakesTheLoadingScreenBackgroundOnlyWithFixedAspect()
        {
            JTLSDKEditorSettings settings = JTLSDKEditorSettings.instance;
            bool fixedAspect = settings.FixedAspect;
            bool pageAsLoader = settings.PageUsesLoaderBackground;
            BackgroundKind loaderKind = settings.LoaderBackground.Kind;
            BackgroundKind pageKind = settings.PageBackground.Kind;
            Color loaderFrom = settings.LoaderBackground.GradientFrom;
            Color loaderTo = settings.LoaderBackground.GradientTo;
            Color pageColor = settings.PageBackground.Color;

            try
            {
                settings.LoaderBackground.Kind = BackgroundKind.Gradient;
                settings.LoaderBackground.Radial = true;
                settings.LoaderBackground.GradientFrom = Color.black;
                settings.LoaderBackground.GradientTo = Color.white;
                settings.PageBackground.Kind = BackgroundKind.Color;
                settings.PageBackground.Color = Color.red;
                settings.PageUsesLoaderBackground = true;

                settings.FixedAspect = false;
                System.Collections.Generic.Dictionary<string, string> values = new TemplateService().Values(settings, null, 1, false);

                Assert.AreEqual("#FF0000", values["JTLSDK_PAGE_BACKGROUND"]);

                settings.FixedAspect = true;
                values = new TemplateService().Values(settings, null, 1, false);

                Assert.AreEqual(values["JTLSDK_LOADER_BACKGROUND"], values["JTLSDK_PAGE_BACKGROUND"]);

                settings.PageUsesLoaderBackground = false;
                values = new TemplateService().Values(settings, null, 1, false);

                Assert.AreEqual("#FF0000", values["JTLSDK_PAGE_BACKGROUND"]);
            }
            finally
            {
                settings.FixedAspect = fixedAspect;
                settings.PageUsesLoaderBackground = pageAsLoader;
                settings.LoaderBackground.Kind = loaderKind;
                settings.LoaderBackground.GradientFrom = loaderFrom;
                settings.LoaderBackground.GradientTo = loaderTo;
                settings.PageBackground.Kind = pageKind;
                settings.PageBackground.Color = pageColor;
            }
        }
    }
}
