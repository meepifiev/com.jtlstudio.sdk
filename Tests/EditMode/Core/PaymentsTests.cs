using System;
using System.Collections.Generic;
using JTLStudio.SDK.Tests.Fakes;
using NUnit.Framework;

namespace JTLStudio.SDK.Tests.Core
{
    public class PaymentsTests
    {
        private const string RemoveAds = "remove_ads";
        private const string Coins = "coins_1000";

        private TestSettingsBuilder _builder;
        private FakePaymentsProvider _payments;
        private FakeDataProvider _data;

        [SetUp]
        public void SetUp()
        {
            JTLSDK.Destroy();
            _payments = new FakePaymentsProvider();
            _payments.AddProduct(RemoveAds, 49m);
            _payments.AddProduct(Coins, 15m);
            _data = new FakeDataProvider();
            _builder = new TestSettingsBuilder { Payments = _payments, Data = _data };
            _builder.Products.Add(new ProductDefinition(RemoveAds, ProductType.NonConsumable));
            _builder.Products.Add(new ProductDefinition(Coins, ProductType.Consumable));
        }

        [TearDown]
        public void TearDown()
        {
            _builder.Cleanup();
        }

        [Test]
        public void ProviderReceivesCatalogBeforeInitialization()
        {
            JTLSDK.Create(_builder.Build());

            Assert.AreEqual(2, _payments.ConfiguredProducts.Count);
        }

        [Test]
        public void ProductDataComesFromPlatformCatalog()
        {
            JTLSDK.Create(_builder.Build());

            ProductData product = JTLSDK.Payments.GetProductData(RemoveAds);

            Assert.IsTrue(product.IsValid);
            Assert.AreEqual(RemoveAds, product.Id);
            Assert.AreEqual(49m, product.Price);
            Assert.AreEqual("YAN", product.Currency);
            Assert.IsFalse(JTLSDK.Payments.GetProductData("unknown").IsValid);
        }

        [Test]
        public void ConsumableIsGrantedThenSavedThenConsumed()
        {
            JTLSDK.Create(_builder.Build());
            List<string> order = new List<string>();

            JTLSDK.Payments.Purchase(Coins, () => order.Add("granted:saves=" + _data.SaveCount));
            Assert.IsTrue(JTLSDK.Pause.IsPaused);

            _payments.CompletePurchase(PurchaseResult.Purchased);

            Assert.IsFalse(JTLSDK.Pause.IsPaused);
            CollectionAssert.AreEqual(new[] { "granted:saves=0" }, order);
            Assert.AreEqual(1, _payments.ConsumedTokens.Count);
        }

        [Test]
        public void NonConsumableIsNotConsumedAndBecomesPurchased()
        {
            JTLSDK.Create(_builder.Build());
            Assert.IsFalse(JTLSDK.Payments.IsAlreadyPurchased(RemoveAds));

            JTLSDK.Payments.Purchase(RemoveAds, () => { });
            _payments.CompletePurchase(PurchaseResult.Purchased);

            Assert.IsTrue(JTLSDK.Payments.IsAlreadyPurchased(RemoveAds));
            Assert.AreEqual(0, _payments.ConsumedTokens.Count);
        }

        [Test]
        public void GrantExceptionLeavesPurchasePending()
        {
            JTLSDK.Create(_builder.Build());

            JTLSDK.Payments.Purchase(Coins, () => throw new InvalidOperationException("game bug"));
            _payments.CompletePurchase(PurchaseResult.Purchased);

            Assert.AreEqual(0, _payments.ConsumedTokens.Count);
            Assert.AreEqual(1, _payments.Purchases.Count);
            Assert.IsFalse(JTLSDK.Pause.IsPaused);
            Assert.AreEqual(1, PendingProducts().Count);
        }

        [Test]
        public void FailedSaveLeavesPurchasePending()
        {
            _data.SaveSucceeds = false;
            JTLSDK.Create(_builder.Build());

            JTLSDK.Payments.Purchase(Coins, () => { });
            _payments.CompletePurchase(PurchaseResult.Purchased);

            Assert.AreEqual(0, _payments.ConsumedTokens.Count);
        }

        [Test]
        public void PendingPurchasesAreRestoredOnce()
        {
            _payments.AddOwnedPurchase(Coins);
            _payments.AddOwnedPurchase(RemoveAds);
            JTLSDK.Create(_builder.Build());
            List<string> granted = new List<string>();

            IRestoreData restoreData = Restore();

            CollectionAssert.AreEquivalent(new[] { Coins, RemoveAds }, restoreData.AllPurchases);
            CollectionAssert.AreEquivalent(new[] { Coins, RemoveAds }, restoreData.PendingProducts);

            foreach (string productId in restoreData.PendingProducts)
            {
                restoreData.RestoreProduct(productId, () => granted.Add(productId));
            }

            CollectionAssert.AreEquivalent(new[] { Coins, RemoveAds }, granted);
            Assert.AreEqual(1, _payments.ConsumedTokens.Count);

            restoreData.RestoreProduct(Coins, () => granted.Add(Coins));
            Assert.AreEqual(2, granted.Count);
        }

        [Test]
        public void RestoreIsSkippedForAlreadyGrantedPurchase()
        {
            JTLSDK.Create(_builder.Build());

            JTLSDK.Payments.Purchase(RemoveAds, () => { });
            _payments.CompletePurchase(PurchaseResult.Purchased);

            CollectionAssert.IsEmpty(PendingProducts());
        }

        [Test]
        public void ConsumeFailureDoesNotGrantTwice()
        {
            _payments.ConsumeSucceeds = false;
            JTLSDK.Create(_builder.Build());
            int granted = 0;

            JTLSDK.Payments.Purchase(Coins, () => granted++);
            _payments.CompletePurchase(PurchaseResult.Purchased);

            Assert.AreEqual(1, granted);
            Assert.AreEqual(1, _payments.Purchases.Count);

            IRestoreData restoreData = Restore();
            CollectionAssert.IsEmpty(restoreData.PendingProducts);

            restoreData.RestoreProduct(Coins, () => granted++);
            Assert.AreEqual(1, granted);
        }

        [Test]
        public void GrantedTokenIsStoredInSaveData()
        {
            _payments.ConsumeSucceeds = false;
            JTLSDK.Create(_builder.Build());

            JTLSDK.Payments.Purchase(Coins, () => { });
            _payments.CompletePurchase(PurchaseResult.Purchased);

            Assert.IsTrue(JTLSDK.Data.HasKey("jtlsdk.granted"));
            StringAssert.Contains(JTLSDK.Data.GetString("jtlsdk.granted"), _payments.Purchases[0].Token);
        }

        [Test]
        public void ConsumedPurchaseIsForgotten()
        {
            JTLSDK.Create(_builder.Build());

            JTLSDK.Payments.Purchase(Coins, () => { });
            _payments.CompletePurchase(PurchaseResult.Purchased);

            Assert.IsFalse(JTLSDK.Data.HasKey("jtlsdk.granted"));
        }

        [Test]
        public void CancelledPurchaseCallsOnError()
        {
            JTLSDK.Create(_builder.Build());
            int granted = 0;
            int errors = 0;

            JTLSDK.Payments.Purchase(Coins, () => granted++, () => errors++);
            _payments.CompletePurchase(PurchaseResult.Cancelled);

            Assert.AreEqual(0, granted);
            Assert.AreEqual(1, errors);
            Assert.IsFalse(JTLSDK.Pause.IsPaused);
        }

        [Test]
        public void UnknownProductCallsOnError()
        {
            JTLSDK.Create(_builder.Build());
            int errors = 0;

            JTLSDK.Payments.Purchase("missing", () => { }, () => errors++);

            Assert.AreEqual(1, errors);
            Assert.IsFalse(_payments.HasPendingPurchase);
        }

        [Test]
        public void SecondPurchaseWhileFirstPendingCallsOnError()
        {
            JTLSDK.Create(_builder.Build());
            int errors = 0;

            JTLSDK.Payments.Purchase(Coins, () => { });
            JTLSDK.Payments.Purchase(RemoveAds, () => { }, () => errors++);

            Assert.AreEqual(1, errors);
        }

        [Test]
        public void UnsupportedProviderCallsOnErrorAndReturnsNoRestoreData()
        {
            _builder.Payments = null;
            JTLSDK.Create(_builder.Build());
            int errors = 0;
            bool restoreCalled = false;
            IRestoreData restoreData = null;

            JTLSDK.Payments.Purchase(Coins, () => { }, () => errors++);
            JTLSDK.Payments.RestorePurchases(data =>
            {
                restoreCalled = true;
                restoreData = data;
            });

            Assert.AreEqual(1, errors);
            Assert.IsTrue(restoreCalled);
            Assert.IsNull(restoreData);
            Assert.IsFalse(JTLSDK.Platform.Supports(Capability.Purchases));
        }

        [Test]
        public void PurchaseWithoutSuccessCallbackThrows()
        {
            JTLSDK.Create(_builder.Build());

            Assert.Throws<ArgumentNullException>(() => JTLSDK.Payments.Purchase(Coins, null));
        }

        private IRestoreData Restore()
        {
            IRestoreData restoreData = null;
            JTLSDK.Payments.RestorePurchases(data => restoreData = data);
            Assert.IsNotNull(restoreData);
            return restoreData;
        }

        private IReadOnlyList<string> PendingProducts()
        {
            return Restore().PendingProducts;
        }
    }
}
