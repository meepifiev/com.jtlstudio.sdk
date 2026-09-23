# JTL SDK · Руководство

Один API для WebGL-площадок. Код игры не меняется между Yandex Games, YouTube Playables и редактором.

## Установка

Unity 2021.3.18f1 и новее. Проверено на 2021.3, 2022.3 и Unity 6.

`Window › Package Manager › + › Add package from git URL`:

```text
https://github.com/meepifiev/com.jtlstudio.sdk.git
```

Затем `JTL SDK › Toolkit`.

## Тулкит

| Раздел | Что делает |
|---|---|
| Configurations | Конфигурация на каждую площадку. Активная конфигурация включает свой define-символ и пресет Player Settings. Одна сборка = одна конфигурация. |
| Build | Выбор активной конфигурации, её настройки сборки и проекта, проверки, путь, ZIP или папка, номер сборки, Development-плашка, последние сборки. |
| Simulation | Ответы площадки в редакторе: устройство, задержка и ошибка инициализации, реклама, покупки, игрок, сохранения. |
| Template | Экран загрузки, логотип, фон, прогресс, пропорции, pixel ratio. |
| Package Manager | Обновления SDK из GitHub Releases, шаблон, модули. |
| Code Analyzer | Находит в коде игры `Time.timeScale`, `AudioListener`, `PlayerPrefs`, `Cursor` и `Application.OpenURL` и заменяет их на вызовы SDK. |
| Modules | Настраиваемые и готовые к работе: провайдер каждого модуля в каждой конфигурации. Покупки, лидерборды, флаги и языки описываются здесь. |

После правки товаров, лидербордов и флагов нажмите «Generate constants». Появятся классы `ProductIds`, `LeaderboardIds` и `FlagKeys`.

## Запуск

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

`Create` читает `Resources/JTLSDK/JTLSDKSettings.asset`. `WhenReady` вызывается один раз, когда готовы все модули или истёк таймаут. Каждый модуль тоже имеет `IsReady`, `IsSupported` и `WhenReady`.

## Модули

**Реклама.** Пауза и звук на время показа управляются SDK.

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

**Сохранения.** Ключ-значение, запись в облако с задержкой. `Save` пишет сразу.

```csharp
int money = JTLSDK.Data.GetInt("money");
JTLSDK.Data.SetInt("money", money + 100);
JTLSDK.Data.Save();
```

**Покупки.** Товар выдаётся в `onSuccess`, дальше SDK сохраняет сейв и списывает расходуемую покупку. Оплаченные, но не выданные товары возвращаются через `RestorePurchases` один раз после `WhenReady`. Выдача должна добавлять значение, а не устанавливать его.

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

**Язык.** Язык выбирает площадка. `Set` действует до конца сессии.

```csharp
Language language = JTLSDK.Language.Current;
JTLSDK.Language.Changed += OnLanguageChanged;
```

**Пауза, время и звук.** SDK владеет `Time.timeScale` и громкостью. Меняйте их через SDK.

Пауза ставится сама: на время рекламы и покупки, при потере фокуса и по сигналу площадки (`game_api_pause` на Яндексе, `onPause` на YouTube). Что она делает, настраивается в тулките, раздел Pause: остановить `Time.timeScale`, поставить на паузу `AudioListener`, выключить EventSystem, показать курсор. После паузы всё возвращается к тому, что задала игра.

Своё меню ставьте на паузу так же, как в Prime:

```csharp
JTLSDK.Time.Scale = 0f;
JTLSDK.Audio.Paused = true;
JTLSDK.GameEvents.GameplayStopped();
```

**Игровые события.** `GameReady` один раз после загрузки. `GameplayStarted`, `GameplayStopped` и `GameplayRestarted` вокруг активной игры.

```csharp
JTLSDK.GameEvents.GameplayStarted();
JTLSDK.GameEvents.GameplayRestarted();
JTLSDK.GameEvents.GameplayStopped();
```

**Лидерборды, игрок, флаги, отзыв, ярлык игры.**

```csharp
JTLSDK.Leaderboards.SetScore(LeaderboardIds.Levels, 27);
JTLSDK.Player.Authorize(success => { });
bool hardMode = JTLSDK.Flags.GetBool(FlagKeys.HardMode);
JTLSDK.Review.Request(sent => { });
JTLSDK.GameLabel.ShowDialog(created => { });
```

**Площадка, устройство и ссылки.**

```csharp
PlatformId platform = JTLSDK.Platform.Current;
bool mobile = JTLSDK.Device.IsMobile;
JTLSDK.Links.OpenDeveloperPage();
JTLSDK.Links.OpenGamePage("123456");
```

## Редактор

В Play Mode провайдеры конфигурации заменяются прототипами. Реклама и покупки показывают окно с вариантами ответа, язык меняется в углу вкладки Game. Сохранения лежат в `PlayerPrefs` и редактируются в разделе Saves.

## Сборка

`Build › Build` собирает WebGL для активной конфигурации. Красная проверка блокирует сборку. Для YouTube Playables дополнительно проверяются размер файлов, их количество, сжатие и внешние скрипты.

## Модули пакета

Package Manager в тулките показывает модули из `modules.json` и ставит их по git-ссылке. Опубликованных модулей пока нет.

## Поддержка

[t.me/jtlstudio](https://t.me/jtlstudio)
