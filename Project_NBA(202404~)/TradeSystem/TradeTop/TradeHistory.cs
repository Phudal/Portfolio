using Cysharp.Threading.Tasks;
using Dimps.Application.AssetBundle;
using Dimps.Application.Common;
using Dimps.Application.Common.UI;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Linq;

namespace GVNC.Application.Trade
{
    public class TradeHistory : MonoBehaviour, IMultiLoadGridScrollContext
    {
        [SerializeField] private UICustomTab filterButton;
        [SerializeField] private TradeHistoryListController controller;

        private List<TradeHistoryScrollDataContainer> historyPossesion = new List<TradeHistoryScrollDataContainer>();
        private List<TradeHistoryScrollDataContainer> viewList = new List<TradeHistoryScrollDataContainer>();

        private CardListRule historyFilterRule = new CardListRule();

        private AssetLoader assetLoader;
        private CancellationToken token;

        public void Init(List<TradeHistoryScrollDataContainer> history, AssetLoader assetLoader, CancellationToken token)
        {
            this.historyPossesion = history;

            this.assetLoader = assetLoader;
            this.token = token;

            viewList = historyPossesion;

            filterButton.onClick.RemoveAllListeners();
            filterButton.onClick.AddListener(() =>
            {
                OpenTradeFilterMenu();
            });

            filterButton.Toggle = historyFilterRule.CheckFilterState();

            Load().Forget();
        }

        private async UniTask Load()
        {
            controller.Setup(this, 2, viewList);

            await controller.LoadInitCard(viewList, assetLoader);
            controller.LoadCard(12, 4, assetLoader, token).Forget();
        }

        public void Dispose()
        {
            controller.Dispose();
        }

        public void ScrollItemCallback_1(MultiLoadScrollDataConatainer data)
        {

        }

        public void ScrollItemCallback_2(MultiLoadScrollDataConatainer data)
        {
            
        }

        public void ScrollItemCallback_3(MultiLoadScrollDataConatainer data)
        {
            
        }

        public void ScrollItemCallback_4(MultiLoadScrollDataConatainer data)
        {

        }

        private void OpenTradeFilterMenu()
        {
            TradeFilterMenu.Open(new TradeFilterMenu.Param(historyFilterRule, "ID_TRD_204", (rule) => 
            {
                historyFilterRule = rule;

                filterButton.Toggle = historyFilterRule.CheckFilterState();

                viewList = TradeCardListSortFilter.Filter_TradeHistory(historyPossesion, historyFilterRule.FilterRules).ToList();

                Load().Forget();
            }));
        }
    }
}
