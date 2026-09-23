using System;
using System.Collections.Generic;

namespace JTLStudio.SDK
{
    public interface IRestoreData
    {
        IReadOnlyList<string> AllPurchases { get; }
        IReadOnlyList<string> PendingProducts { get; }
        void RestoreProduct(string productId, Action onProductRestore);
    }
}
