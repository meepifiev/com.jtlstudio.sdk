using UnityEngine;

namespace JTLStudio.SDK.Samples.Examples
{
    public class Shop : MonoBehaviour
    {
        private const string CoinsProduct = "coins_1000";
        private const string RemoveAdsProduct = "remove_ads";
        private const string CoinsKey = "Coins";

        private void Start()
        {
            JTLSDK.WhenReady(OnReady);
        }

        public void OnBuyCoinsClicked()
        {
            JTLSDK.Payments.Purchase(
                productId: CoinsProduct,
                onSuccess: () => GiveProduct(CoinsProduct),
                onError: () => Debug.Log("Coins were not bought"));
        }

        public void OnRemoveAdsClicked()
        {
            JTLSDK.Payments.Purchase(
                productId: RemoveAdsProduct,
                onSuccess: () => GiveProduct(RemoveAdsProduct),
                onError: () => Debug.Log("Remove ads was not bought"));
        }

        public bool AreAdsRemoved()
        {
            return JTLSDK.Payments.IsAlreadyPurchased(RemoveAdsProduct);
        }

        public string CoinsPriceText()
        {
            ProductData product = JTLSDK.Payments.GetProductData(CoinsProduct);
            return product.IsValid ? product.Formatted : "";
        }

        private void OnReady()
        {
            if (JTLSDK.Payments.IsSupported == false)
            {
                gameObject.SetActive(false);
                return;
            }

            JTLSDK.Payments.RestorePurchases(OnRestoreData);
        }

        private void OnRestoreData(IRestoreData restoreData)
        {
            if (restoreData == null)
            {
                return;
            }

            foreach (string productId in restoreData.PendingProducts)
            {
                restoreData.RestoreProduct(productId, () => GiveProduct(productId));
            }
        }

        private void GiveProduct(string productId)
        {
            if (productId == CoinsProduct)
            {
                JTLSDK.Data.SetInt(CoinsKey, JTLSDK.Data.GetInt(CoinsKey) + 1000);
            }
        }
    }
}
