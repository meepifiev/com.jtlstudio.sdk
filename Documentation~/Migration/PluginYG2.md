# Переезд с PluginYG2 на JTL SDK

Карта вызовов и шагов, собранная на реальной миграции игры (SuikaGame, Unity 6, PluginYourGames 2.x). Файл ведётся как основа для будущей тулзы миграции: всё, что можно заменить регулярным выражением, вынесено в `pluginyg2-rules.json`.

## Замены в коде

| PluginYG2 | JTL SDK | Заметки |
|---|---|---|
| `using YG;` | `using JTLStudio.SDK;` | если файл трогает сейвы, добавляется `using <Game>.Save;` |
| `YG2.isSDKEnabled` | `JTLSDK.IsCreated && JTLSDK.<модуль>.IsReady` | в игре обычно означает «сейвы доступны», удобно завести обёртку `Saves.IsReady` |
| `YG2.onGetSDKData += H` | `JTLSDK.Data.Loaded += H` | событие приходит при каждой загрузке сейва: старт, вход игрока, удачный повтор |
| `YG2.onGetSDKData -= H` | `JTLSDK.Data.Loaded -= H` | |
| проверка `if (YG2.isSDKEnabled) H();` в `Start` | `if (Saves.IsReady) H();` или `JTLSDK.WhenReady(H)` | `WhenReady` одноразовый и не требует отписки |
| `YG2.saves.<поле>` | `Saves.Data.<поле>` | см. «Модель сохранений» |
| `YG2.SaveProgress()` | `Saves.Save()` | внутри `JTLSDK.Data.SetObject` + `JTLSDK.Data.Save()`. PluginYG2 придерживает частые вызовы своим таймером, у нас `Save()` пишет сразу: то, что вызывается в геймплее (состояние забега, счётчики), переводите на `Saves.Save(important: false)` |
| `YG2.onSwitchLang += H`, `YG2.onCorrectLang += H` | `JTLSDK.Language.Changed += H` | два события PluginYG2 сводятся в одно; сигнатура обработчика меняется с `Action<string>` на `Action<Language>` |
| `YG2.lang` | `new LanguageCodes().ToCode(JTLSDK.Language.Current)` | `Language` это enum, код языка отдаёт `LanguageCodes` |
| `YG2.RewardedAdvShow(id, onReward)` + `onCloseRewardedAdv` + `onErrorRewardedAdv` | `JTLSDK.Ads.ShowRewarded(id, result => ...)` | один колбэк вместо трёх событий: `AdResult.Rewarded` это награда, остальное отказ |
| `YG2.InterstitialAdvShow()` | `JTLSDK.Ads.ShowInterstitial()` | |
| `YG2.isTimerAdvCompleted` | убрать | интервал между interstitial держит сам SDK, внутри интервала приходит `AdResult.NotShown` |
| `YG2.nowAdsShow` | `JTLSDK.Ads.IsShowing` | |
| `YG2.envir.isMobile`, `YG2.envir.isTablet` | `JTLSDK.Device.IsMobile` | true и для Mobile, и для Tablet |
| `YG2.infoYG.Simulation.device` (в `#if UNITY_EDITOR`) | `JTLSDK.Device.Type` | устройство в редакторе переключается в оверлее вкладки Game, отдельная ветка под редактор не нужна |
| `typeof(YG2).GetMethod("SetLeaderboard")` через рефлексию | `JTLSDK.Leaderboards.SetScore(id, score)` | модуль всегда в пакете, рефлексия и проверка «модуль не установлен» больше не нужны |
| `YG2.MetricaSend(name, data)` | нет | в SDK нет модуля аналитики: либо оставить свой `.jslib`, либо выкинуть |
| `#if EnvirData_yg`, `#if Leaderboard_yg` и прочие define-символы модулей | убрать | модули не опциональные |

## Модель сохранений

PluginYG2 хранит типизированный `partial class SavesYG` с полями. JTL SDK хранит документ ключ-значение, поэтому поля переезжают в один объект:

1. Поля из `SavesYG` переносятся в свой `[Serializable]` класс (`SuikaSaveData` в примере). Префиксы вроде `suika` можно снять: в своём классе они не нужны.
2. Пишется статическая обёртка: `Data` читает объект через `JTLSDK.Data.GetObject(key, new ...)` один раз после готовности модуля, `Save()` пишет `SetObject` и вызывает `Save()`, `Reload()` сбрасывает кеш.
3. Обёртка подписывается на `JTLSDK.Data.Loaded`, чтобы после входа игрока или повторной загрузки кеш не устарел.
4. Часто меняющиеся значения пишутся как `Saves.Save(important: false)`: не будят автосохранение, но уходят при следующем важном сохранении и при скрытии вкладки.
5. `JsonUtility` сериализует только свои типы: `List<T>` и вложенные классы должны быть `[Serializable]`, свойства не сохраняются.

Старые сейвы игроков остаются в облаке площадки под ключом PluginYG2 и в новый документ сами не переедут. Если игра уже опубликована, нужен разовый импорт: прочитать старый ключ и переложить поля.

## Запуск SDK

PluginYG2 поднимает себя сам из префаба в `Resources`. В JTL SDK нужен один вызов:

```csharp
public static class SdkBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (JTLSDK.IsCreated)
        {
            return;
        }

        JTLSDK.Create();
        JTLSDK.Data.Loaded += Saves.Reload;
        JTLSDK.WhenReady(() => JTLSDK.GameEvents.GameReady());
    }
}
```

`GameReady` в PluginYG2 вызывается плагином автоматически, в JTL SDK его вызывает игра, когда игрок уже может играть.

## Шаги по проекту

0. Нужен JTL SDK 1.0.1 или новее: до 1.0.1 отсутствие ассета настроек роняло `Create()`, а обращение к модулю после остановки игры бросало исключение прямо из `OnDisable`.
1. `Packages/manifest.json`: убрать зависимости плагина, добавить `"com.jtlstudio.sdk": "https://github.com/meepifiev/com.jtlstudio.sdk.git#v1.0.0"`.
2. Удалить папку плагина (`Assets/PluginYourGames`) и его шаблон из `Assets/WebGLTemplates`.
3. В окне SDK: создать конфигурацию площадки, сделать активной, установить шаблон, перенести языки, доски лидербордов, товары и флаги. Без ассета настроек (`Assets/Resources/JTLSDK/JTLSDKSettings.asset`) SDK стартует без площадки и пишет об этом в консоль.
4. Player Settings: `webGLTemplate` после удаления шаблона плагина указывает на несуществующий шаблон, активация конфигурации ставит `PROJECT:JTLSDK`.
5. Сцены: удалить объекты плагина и объекты сервисов, которые больше не нужны, иначе останутся missing script.
6. Проверить раздел Code Analyzer: игры на PluginYG2 часто пишут напрямую в `Time.timeScale`, `AudioListener` и `PlayerPrefs`.

## Что стоит проверить после переезда

- Сейв читается и пишется, прогресс переживает перезагрузку страницы.
- Реклама: награда приходит только на `AdResult.Rewarded`, игра встаёт на паузу и возвращается.
- Язык: событие смены языка приходит, тексты обновляются.
- Лидерборд: счёт уходит после входа игрока.
- Сборка проходит проверки раздела Build без предупреждений.
