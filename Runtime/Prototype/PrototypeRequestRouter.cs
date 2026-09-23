#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace JTLStudio.SDK.Prototype
{
    internal static class PrototypeRequestRouter
    {
        private static readonly Queue<Action> Pending = new Queue<Action>();
        private static AdResult? _rememberedInterstitial;
        private static AdResult? _rememberedRewarded;
        private static Action<PrototypePurchaseRequest> _rememberedPurchase;
        private static bool _showing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Attach()
        {
            Pending.Clear();
            _rememberedInterstitial = null;
            _rememberedRewarded = null;
            _rememberedPurchase = null;
            _showing = false;

            PrototypeBridge.AdRequested -= OnAdRequested;
            PrototypeBridge.PurchaseRequested -= OnPurchaseRequested;
            PrototypeBridge.AdRequested += OnAdRequested;
            PrototypeBridge.PurchaseRequested += OnPurchaseRequested;
        }

        private static void OnAdRequested(PrototypeAdRequest request)
        {
            AdResult? remembered = request.IsRewarded ? _rememberedRewarded : _rememberedInterstitial;

            if (remembered.HasValue)
            {
                request.Complete(remembered.Value);
                return;
            }

            Enqueue(() => ShowAd(request));
        }

        private static void OnPurchaseRequested(PrototypePurchaseRequest request)
        {
            if (_rememberedPurchase != null)
            {
                _rememberedPurchase(request);
                return;
            }

            Enqueue(() => ShowPurchase(request));
        }

        private static void ShowAd(PrototypeAdRequest request)
        {
            List<string> captions = new List<string>();
            List<string> notes = new List<string>();
            List<AdResult> results = new List<AdResult>();

            if (request.IsRewarded)
            {
                Add(captions, notes, results, AdResult.Rewarded, "watched to the end");
                Add(captions, notes, results, AdResult.Closed, "closed early");
                Add(captions, notes, results, AdResult.NotShown, "no ad available");
                Add(captions, notes, results, AdResult.Failed, "platform error");
            }
            else
            {
                Add(captions, notes, results, AdResult.Shown, "shown and closed");
                Add(captions, notes, results, AdResult.NotShown, "no ad available");
                Add(captions, notes, results, AdResult.Failed, "platform error");
            }

            string title = request.IsRewarded ? "Ads.ShowRewarded(\"" + request.RewardId + "\")" : "Ads.ShowInterstitial()";

            Show(title, captions, notes, (index, remember) =>
            {
                AdResult result = results[index];

                if (remember)
                {
                    if (request.IsRewarded)
                    {
                        _rememberedRewarded = result;
                    }
                    else
                    {
                        _rememberedInterstitial = result;
                    }
                }

                request.Complete(result);
            });
        }

        private static void ShowPurchase(PrototypePurchaseRequest request)
        {
            List<string> captions = new List<string>
            {
                "PurchaseResult.Purchased",
                "PurchaseResult.Failed",
                "PurchaseResult.Cancelled",
                "PurchaseResult.Failed"
            };

            List<string> notes = new List<string>
            {
                "granted, then consumed after the save",
                "paid, restored on next launch",
                "player closed the payment window",
                "payment error"
            };

            string title = "Payments.Purchase(\"" + request.ProductId + "\")" + (string.IsNullOrEmpty(request.PriceText) ? "" : "   " + request.PriceText);

            Show(title, captions, notes, (index, remember) =>
            {
                if (remember)
                {
                    _rememberedPurchase = RememberPurchase(index);
                }

                Complete(request, index);
            });
        }

        private static Action<PrototypePurchaseRequest> RememberPurchase(int index)
        {
            return pending => Complete(pending, index);
        }

        private static void Complete(PrototypePurchaseRequest request, int index)
        {
            switch (index)
            {
                case 0:
                    request.Complete(PurchaseResult.Purchased);
                    break;

                case 1:
                    request.CompleteAsCrashBeforeGrant();
                    break;

                case 2:
                    request.Complete(PurchaseResult.Cancelled);
                    break;

                default:
                    request.Complete(PurchaseResult.Failed);
                    break;
            }
        }

        private static void Add(List<string> captions, List<string> notes, List<AdResult> results, AdResult result, string note)
        {
            captions.Add("AdResult." + result);
            notes.Add(note);
            results.Add(result);
        }

        private static void Show(string title, List<string> captions, List<string> notes, Action<int, bool> onChosen)
        {
            _showing = true;

            PrototypeOverlayView.Show(title, captions, notes, (index, remember) =>
            {
                _showing = false;
                onChosen(index, remember);
                ShowNext();
            });
        }

        private static void Enqueue(Action show)
        {
            Pending.Enqueue(show);

            if (_showing == false)
            {
                ShowNext();
            }
        }

        private static void ShowNext()
        {
            if (_showing || Pending.Count == 0)
            {
                return;
            }

            Pending.Dequeue()();
        }
    }
}
#endif
