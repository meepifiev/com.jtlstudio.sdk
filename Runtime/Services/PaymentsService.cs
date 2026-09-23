using System;
using System.Collections.Generic;
using JTLStudio.SDK.Providers;

namespace JTLStudio.SDK.Services
{
    public class PaymentsService : ModuleBase, IPayments
    {
        internal const string GrantedTokensKey = "jtlsdk.granted";

        private const float RestoreReminderSeconds = 8f;
        private const char TokenSeparator = '\n';

        private readonly IPaymentsProvider _provider;
        private readonly DataService _data;
        private readonly PauseService _pause;
        private readonly IReadOnlyList<ProductDefinition> _catalog;
        private readonly PlatformId _platform;
        private readonly HashSet<string> _grantedTokens = new HashSet<string>();
        private bool _tokensLoaded;
        private bool _purchaseInProgress;
        private bool _restoreRequested;
        private bool _reminderShown;
        private float _readyTime;

        public PaymentsService(
            IPaymentsProvider provider,
            DataService data,
            PauseService pause,
            IReadOnlyList<ProductDefinition> catalog,
            PlatformId platform,
            SdkLogger logger) : base(logger)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _platform = platform;
        }

        internal override string ModuleName => "Payments";

        public ProductData GetProductData(string productId)
        {
            ProductDefinition definition = FindDefinition(productId);

            if (definition == null || State != ModuleState.Ready)
            {
                return default;
            }

            string platformProductId = definition.PlatformIdFor(_platform);

            foreach (PlatformProduct product in _provider.Products)
            {
                if (product.Id == platformProductId)
                {
                    return new ProductData(definition.Id, product.Price.Value, product.Price.CurrencyCode, product.Price.Formatted);
                }
            }

            return default;
        }

        public bool IsAlreadyPurchased(string productId)
        {
            ProductDefinition definition = FindDefinition(productId);

            if (definition == null || State != ModuleState.Ready)
            {
                return false;
            }

            string platformProductId = definition.PlatformIdFor(_platform);

            foreach (PlatformPurchase purchase in _provider.Purchases)
            {
                if (purchase.ProductId == platformProductId)
                {
                    return true;
                }
            }

            return false;
        }

        public void Purchase(string productId, Action onSuccess, Action onError = null)
        {
            if (onSuccess == null)
            {
                throw new ArgumentNullException(nameof(onSuccess));
            }

            ProductDefinition definition = FindDefinition(productId);

            if (IsReady == false)
            {
                Logger.Warning("Payments are still starting, '" + productId + "' was not bought.");
                onError?.Invoke();
                return;
            }

            if (State != ModuleState.Ready)
            {
                Logger.Warning("Payments are not available on this platform, '" + productId + "' was not bought.");
                onError?.Invoke();
                return;
            }

            if (definition == null)
            {
                Logger.Error("Product '" + productId + "' is not declared in the catalog.");
                onError?.Invoke();
                return;
            }

            if (_purchaseInProgress)
            {
                Logger.Warning("A purchase is already in progress, '" + productId + "' was not bought.");
                onError?.Invoke();
                return;
            }

            _purchaseInProgress = true;
            Logger.Info("Purchase of '" + definition.Id + "' started.");
            IDisposable pauseHold = _pause.Hold(PauseSources.Purchase);

            try
            {
                _provider.Purchase(definition.PlatformIdFor(_platform), (result, purchase) => OnPurchased(definition, pauseHold, result, purchase, onSuccess, onError));
            }
            catch (Exception exception)
            {
                Logger.Exception(exception);
                OnPurchased(definition, pauseHold, PurchaseResult.Failed, default, onSuccess, onError);
            }
        }

        public void RestorePurchases(Action<IRestoreData> onRestoreData)
        {
            if (onRestoreData == null)
            {
                throw new ArgumentNullException(nameof(onRestoreData));
            }

            _restoreRequested = true;

            if (State != ModuleState.Ready)
            {
                Logger.Warning("Payments are not available on this platform, there is nothing to restore.");
                onRestoreData(null);
                return;
            }

            _data.WhenReady(() => _provider.RefreshPurchases(() => onRestoreData(BuildRestoreData())));
        }

        internal override void Initialize()
        {
            _provider.Configure(_catalog, _platform);
            _provider.Initialize(CompleteInitialization);
        }

        internal void Tick(float unscaledDeltaTime)
        {
            if (_reminderShown || _restoreRequested || State != ModuleState.Ready || _data.IsReady == false)
            {
                return;
            }

            _readyTime += unscaledDeltaTime;

            if (_readyTime < RestoreReminderSeconds)
            {
                return;
            }

            _reminderShown = true;
            int pending = CountPendingPurchases();

            if (pending > 0)
            {
                Logger.Warning("The player has " + pending + " paid purchases that were never granted. Call JTLSDK.Payments.RestorePurchases after JTLSDK.WhenReady.");
            }
        }

        internal void RestoreProduct(string productId, Action onProductRestore, List<PlatformPurchase> pending)
        {
            if (onProductRestore == null)
            {
                throw new ArgumentNullException(nameof(onProductRestore));
            }

            ProductDefinition definition = FindDefinition(productId);

            if (definition == null)
            {
                Logger.Error("Product '" + productId + "' is not declared in the catalog.");
                return;
            }

            string platformProductId = definition.PlatformIdFor(_platform);

            foreach (PlatformPurchase purchase in new List<PlatformPurchase>(pending))
            {
                if (purchase.ProductId != platformProductId || IsGranted(purchase))
                {
                    continue;
                }

                pending.Remove(purchase);
                Grant(definition, purchase, onProductRestore, null);
            }
        }

        private void OnPurchased(ProductDefinition definition, IDisposable pauseHold, PurchaseResult result, PlatformPurchase purchase, Action onSuccess, Action onError)
        {
            _purchaseInProgress = false;
            pauseHold.Dispose();

            if (result != PurchaseResult.Purchased)
            {
                Logger.Info("Purchase of '" + definition.Id + "' ended with " + result + ".");
                onError?.Invoke();
                return;
            }

            Grant(definition, purchase, onSuccess, null);
        }

        private void Grant(ProductDefinition definition, PlatformPurchase purchase, Action onGrant, Action onDone)
        {
            try
            {
                onGrant();
            }
            catch (Exception exception)
            {
                Logger.Error("Granting '" + definition.Id + "' threw. The purchase stays pending and is restored on the next launch.");
                Logger.Exception(exception);
                onDone?.Invoke();
                return;
            }

            Logger.Info("Product '" + definition.Id + "' was granted to the player.");
            MarkGranted(purchase);
            _data.Save(success => OnGrantSaved(definition, purchase, success, onDone));
        }

        private void OnGrantSaved(ProductDefinition definition, PlatformPurchase purchase, bool success, Action onDone)
        {
            if (success == false)
            {
                Logger.Warning("Save data was not written after granting '" + definition.Id + "'. The purchase is not consumed.");
                onDone?.Invoke();
                return;
            }

            if (definition.Type != ProductType.Consumable)
            {
                onDone?.Invoke();
                return;
            }

            _provider.Consume(purchase, consumed => OnConsumed(definition, purchase, consumed, onDone));
        }

        private void OnConsumed(ProductDefinition definition, PlatformPurchase purchase, bool consumed, Action onDone)
        {
            if (consumed == false)
            {
                Logger.Warning("The platform did not consume '" + definition.Id + "'.");
                onDone?.Invoke();
                return;
            }

            Logger.Info("Product '" + definition.Id + "' was consumed on the platform.");
            ForgetToken(purchase);
            onDone?.Invoke();
        }

        private IRestoreData BuildRestoreData()
        {
            LoadTokens();
            ForgetMissingTokens();

            List<string> allPurchases = new List<string>();
            List<string> pendingProducts = new List<string>();
            List<PlatformPurchase> pending = new List<PlatformPurchase>();

            foreach (PlatformPurchase purchase in _provider.Purchases)
            {
                ProductDefinition definition = FindDefinitionByPlatformId(purchase.ProductId);
                allPurchases.Add(definition == null ? purchase.ProductId : definition.Id);

                if (definition == null)
                {
                    Logger.Warning("The player owns '" + purchase.ProductId + "', which is not declared in the catalog.");
                    continue;
                }

                if (IsGranted(purchase))
                {
                    continue;
                }

                pending.Add(purchase);

                if (pendingProducts.Contains(definition.Id) == false)
                {
                    pendingProducts.Add(definition.Id);
                }
            }

            Logger.Info("Restore: " + allPurchases.Count + " purchases on the platform, " + pendingProducts.Count + " products still to grant.");
            return new RestoreData(this, allPurchases, pendingProducts, pending);
        }

        private int CountPendingPurchases()
        {
            LoadTokens();
            int pending = 0;

            foreach (PlatformPurchase purchase in _provider.Purchases)
            {
                if (FindDefinitionByPlatformId(purchase.ProductId) != null && IsGranted(purchase) == false)
                {
                    pending++;
                }
            }

            return pending;
        }

        private bool IsGranted(PlatformPurchase purchase)
        {
            LoadTokens();
            return _grantedTokens.Contains(TokenOf(purchase));
        }

        private void MarkGranted(PlatformPurchase purchase)
        {
            LoadTokens();

            if (_grantedTokens.Add(TokenOf(purchase)))
            {
                WriteTokens();
            }
        }

        private void ForgetToken(PlatformPurchase purchase)
        {
            LoadTokens();

            if (_grantedTokens.Remove(TokenOf(purchase)))
            {
                WriteTokens();
            }
        }

        private void ForgetMissingTokens()
        {
            HashSet<string> owned = new HashSet<string>();

            foreach (PlatformPurchase purchase in _provider.Purchases)
            {
                owned.Add(TokenOf(purchase));
            }

            if (_grantedTokens.RemoveWhere(token => owned.Contains(token) == false) > 0)
            {
                WriteTokens();
            }
        }

        private void LoadTokens()
        {
            if (_tokensLoaded || _data.IsReady == false)
            {
                return;
            }

            _tokensLoaded = true;
            string stored = _data.GetString(GrantedTokensKey, "");

            if (string.IsNullOrEmpty(stored))
            {
                return;
            }

            foreach (string token in stored.Split(TokenSeparator))
            {
                if (string.IsNullOrEmpty(token) == false)
                {
                    _grantedTokens.Add(token);
                }
            }
        }

        private void WriteTokens()
        {
            if (_grantedTokens.Count == 0)
            {
                _data.DeleteKey(GrantedTokensKey);
                return;
            }

            string[] tokens = new string[_grantedTokens.Count];
            _grantedTokens.CopyTo(tokens);
            _data.SetString(GrantedTokensKey, string.Join(TokenSeparator.ToString(), tokens));
        }

        private string TokenOf(PlatformPurchase purchase)
        {
            return string.IsNullOrEmpty(purchase.Token) ? purchase.ProductId : purchase.Token;
        }

        private ProductDefinition FindDefinition(string productId)
        {
            if (string.IsNullOrEmpty(productId))
            {
                throw new ArgumentException(nameof(productId));
            }

            foreach (ProductDefinition definition in _catalog)
            {
                if (definition.Id == productId)
                {
                    return definition;
                }
            }

            return null;
        }

        private ProductDefinition FindDefinitionByPlatformId(string platformProductId)
        {
            foreach (ProductDefinition definition in _catalog)
            {
                if (definition.PlatformIdFor(_platform) == platformProductId)
                {
                    return definition;
                }
            }

            return null;
        }
    }
}
