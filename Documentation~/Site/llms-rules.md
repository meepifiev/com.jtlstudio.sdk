## Правила для ИИ-ассистентов

- Все вызовы площадки делаются только через статический класс `JTLSDK`: Ads, Data, Payments, Leaderboards, Flags, Language, GameEvents, Pause, Time, Audio, Player, Platform, Device, Review, GameLabel, Links.
- SDK запускается один раз в первой сцене: `if (JTLSDK.IsCreated == false) JTLSDK.Create();`, дальше `JTLSDK.WhenReady(...)`. После загрузки игры вызывается `JTLSDK.GameEvents.GameReady()`.
- Часто меняющиеся значения пишутся как `JTLSDK.Data.SetInt(key, value, important: false)`: они не запускают автосохранение и уходят со следующим важным изменением, `Save()` или при скрытии вкладки.
- Вместо `PlayerPrefs` используется `JTLSDK.Data`, вместо `Time.timeScale` используется `JTLSDK.Time.Scale`, вместо `AudioListener.volume` и `AudioListener.pause` используются `JTLSDK.Audio.Volume` и `JTLSDK.Audio.Paused`, вместо `Cursor` используется `JTLSDK.Device`.
- Обращение к модулям до `JTLSDK.Create()` и после остановки SDK бросает `InvalidOperationException`; безопасны только `IsCreated`, `IsReady`, `Version` и `LogLevel`. Отписки в `OnDisable` оборачиваются в `if (JTLSDK.IsCreated)`.
- Результаты приходят в колбэках: `Action<AdResult>`, `Action<bool>`, у покупок `onSuccess` и `onError`. У SDK нет async/await, на неподдерживаемые вызовы он не бросает исключений.
- Покупка: `JTLSDK.Payments.Purchase(productId, onSuccess, onError)`, товар выдаётся в `onSuccess` и пишется в `JTLSDK.Data`. Цена берётся из `GetProductData(productId)`, разовые товары проверяются через `IsAlreadyPurchased(productId)`.
- Невыданные покупки восстанавливаются один раз после `WhenReady`: `JTLSDK.Payments.RestorePurchases(restoreData => { foreach (string id in restoreData.PendingProducts) restoreData.RestoreProduct(id, () => GiveProduct(id)); });`. SDK сам помечает выданные покупки в сейве и списывает расходуемые, повторно товар не выдаётся.
- Пауза автоматическая. Своё меню паузы: `JTLSDK.Time.Scale = 0f`, `JTLSDK.Audio.Paused = true`, `JTLSDK.GameEvents.GameplayStopped()`.
- Возможности площадки проверяются через `IsSupported` модуля и `JTLSDK.Platform.Supports(Capability...)`, а не через define-символы.
- id товаров, лидербордов и флагов берутся из сгенерированных классов `ProductIds`, `LeaderboardIds` и `FlagKeys` в `Assets/Scripts/Generated/JTLSDKIds.cs`.
- YouTube Playables: Compression Format только Disabled, каждый файл меньше 30 MiB, не больше 8000 файлов, без внешних скриптов, пауза только по `onPause` и `onResume`.
- Логи SDK в консоли выключаются через `JTLSDK.LogLevel = LogLevel.None` (или Logging level в окне SDK); значение из кода сильнее настройки и меняется на ходу.
- Готовые примеры всех вызовов лежат в сэмпле пакета Examples: Package Manager Unity, JTL SDK, Samples, Examples.
