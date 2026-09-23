using System;
using System.Collections.Generic;
using JTLStudio.SDK.Providers;

namespace JTLStudio.SDK.Services
{
    public class RestoreData : IRestoreData
    {
        private readonly PaymentsService _payments;
        private readonly List<PlatformPurchase> _pending;

        internal RestoreData(PaymentsService payments, IReadOnlyList<string> allPurchases, IReadOnlyList<string> pendingProducts, List<PlatformPurchase> pending)
        {
            _payments = payments;
            _pending = pending;
            AllPurchases = allPurchases;
            PendingProducts = pendingProducts;
        }

        public IReadOnlyList<string> AllPurchases { get; }
        public IReadOnlyList<string> PendingProducts { get; }

        public void RestoreProduct(string productId, Action onProductRestore)
        {
            _payments.RestoreProduct(productId, onProductRestore, _pending);
        }
    }
}
