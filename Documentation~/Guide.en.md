# JTL SDK · Guide

One API for WebGL portals. Game code stays the same on Yandex Games, YouTube Playables and in the editor.

## Install

Unity 2021.3.18f1 or newer. Tested on 2021.3, 2022.3 and Unity 6.

`Window › Package Manager › + › Add package from git URL`:

```text
https://github.com/meepifiev/com.jtlstudio.sdk.git
```

Then open `JTL SDK › Toolkit`.

## Toolkit

| Section | What it does |
|---|---|
| Configurations | One configuration per portal. The active one applies its define symbol and Player Settings preset. One build is one configuration. |
| Build | Active configuration picker, its build and player settings, checks, output path, ZIP or folder, build number, development badge, recent builds. |
| Simulation | Portal answers in the editor: device, initialization delay and failure, ads, purchases, player, saves. |
| Template | Loading screen, logo, background, progress bar, aspect ratio, pixel ratio. |
| Package Manager | SDK updates from GitHub Releases, the template, modules. |
| Code Analyzer | Finds `Time.timeScale`, `AudioListener`, `PlayerPrefs`, `Cursor` and `Application.OpenURL` in game code and replaces them with SDK calls. |
| Modules | Configurable and ready to use: the provider of every module in every configuration. Products, leaderboards, flags and languages are declared here. |

After editing products, leaderboards or flags, press "Generate constants". It writes the `ProductIds`, `LeaderboardIds` and `FlagKeys` classes.

## Start

```csharp
private void Awake()
{
    JTLSDK.Create();
    JTLSDK.WhenReady(OnReady);
}

private void OnReady()
{
    JTLSDK.GameEvents.GameReady();
}
```

`Create` reads `Resources/JTLSDK/JTLSDKSettings.asset`. `WhenReady` fires once, when every module is ready or the timeout expires. Every module also has `IsReady`, `IsSupported` and `WhenReady`.

## Modules

**Ads.** The SDK pauses the game and mutes audio while an ad is shown.

```csharp
JTLSDK.Ads.ShowInterstitial();
JTLSDK.Ads.ShowRewarded("double_money", result =>
{
    if (result == AdResult.Rewarded)
    {
        AddMoney(100);
    }
});
```

**Saves.** Key-value storage with delayed cloud writes. `Save` writes immediately.

```csharp
int money = JTLSDK.Data.GetInt("money");
JTLSDK.Data.SetInt("money", money + 100);
JTLSDK.Data.Save();
```

**Purchases.** The game grants the product in `onSuccess`; the SDK then saves and consumes a consumable purchase. Paid but ungranted products come back through `RestorePurchases`, called once after `WhenReady`. Granting must add to a value, not set it.

```csharp
JTLSDK.Payments.Purchase(
    productId: ProductIds.Coins1000,
    onSuccess: () => GiveProduct(ProductIds.Coins1000),
    onError: () => { });

JTLSDK.Payments.RestorePurchases(restoreData =>
{
    if (restoreData == null)
    {
        return;
    }

    foreach (string productId in restoreData.PendingProducts)
    {
        restoreData.RestoreProduct(productId, () => GiveProduct(productId));
    }
});

ProductData product = JTLSDK.Payments.GetProductData(ProductIds.Coins1000);
bool removed = JTLSDK.Payments.IsAlreadyPurchased(ProductIds.RemoveAds);

private void GiveProduct(string productId)
{
    if (productId == ProductIds.Coins1000)
    {
        JTLSDK.Data.SetInt("money", JTLSDK.Data.GetInt("money") + 1000);
    }
}
```

**Language.** The portal picks the language. `Set` lasts until the end of the session.

```csharp
Language language = JTLSDK.Language.Current;
JTLSDK.Language.Changed += OnLanguageChanged;
```

**Pause, time and audio.** The SDK owns `Time.timeScale` and the volume. Change them through the SDK.

The pause is automatic: during ads and purchases, on focus loss and on the portal's signal (`game_api_pause` on Yandex, `onPause` on YouTube). What it does is set in the toolkit, Pause section: stop `Time.timeScale`, pause `AudioListener`, disable the EventSystem, show the cursor. After the pause everything returns to what the game set.

Pause your own menu the same way as in Prime:

```csharp
JTLSDK.Time.Scale = 0f;
JTLSDK.Audio.Paused = true;
JTLSDK.GameEvents.GameplayStopped();
```

**Game events.** Call `GameReady` once after loading. Call `GameplayStarted`, `GameplayStopped` and `GameplayRestarted` around active play.

```csharp
JTLSDK.GameEvents.GameplayStarted();
JTLSDK.GameEvents.GameplayRestarted();
JTLSDK.GameEvents.GameplayStopped();
```

**Leaderboards, player, flags, review, game label.**

```csharp
JTLSDK.Leaderboards.SetScore(LeaderboardIds.Levels, 27);
JTLSDK.Player.Authorize(success => { });
bool hardMode = JTLSDK.Flags.GetBool(FlagKeys.HardMode);
JTLSDK.Review.Request(sent => { });
JTLSDK.GameLabel.ShowDialog(created => { });
```

**Platform, device and links.**

```csharp
PlatformId platform = JTLSDK.Platform.Current;
bool mobile = JTLSDK.Device.IsMobile;
JTLSDK.Links.OpenDeveloperPage();
JTLSDK.Links.OpenGamePage("123456");
```

## Editor

In Play Mode the configuration's providers are replaced with prototypes. Ads and purchases show a dialog with the possible answers, and the language switch sits in the corner of the Game view. Saves live in `PlayerPrefs` and are edited in the Saves section.

## Build

`Build › Build` makes a WebGL build of the active configuration. A red check blocks the build. YouTube Playables builds are also checked for file size, file count, compression and external scripts.

## Package modules

The toolkit's Package Manager lists modules from `modules.json` and installs them from git. No modules are published yet.

## Support

[t.me/jtlstudio](https://t.me/jtlstudio)
