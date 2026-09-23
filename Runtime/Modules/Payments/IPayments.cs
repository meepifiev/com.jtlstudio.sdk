using System;

namespace JTLStudio.SDK
{
    public interface IPayments : IModule
    {
        ProductData GetProductData(string productId);
        bool IsAlreadyPurchased(string productId);
        void Purchase(string productId, Action onSuccess, Action onError = null);
        void RestorePurchases(Action<IRestoreData> onRestoreData);
    }
}
