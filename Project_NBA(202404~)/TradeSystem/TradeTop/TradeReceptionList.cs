using Dimps.Application.AssetBundle;
using Dimps.Application.Common.UI;
using Dimps.Application.MasterData.Master;
using Dimps.Application.MasterData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Dimps.Application.API;
using Dimps.Utility;
using Dimps.Application.Global;
using Dimps.Application.Global.UserData;
using Dimps.Application.MasterData.Types;

namespace GVNC.Application.Trade
{
    public class TradeReceptionList : MonoBehaviour
    {
        [SerializeField] private TradeTop tradeTop = null;

        [SerializeField] private RectTransform rect_scrollContent = null;

        [SerializeField] private GameObject cellPrefab = null;

        private List<TradeRegisterSlotDataContainer> registerList = new List<TradeRegisterSlotDataContainer>();
        private List<TradeRegisterSlotDataContainer> viewList = new List<TradeRegisterSlotDataContainer>();

        private AssetLoader assetLoader = null;

        public void Init(List<TradeRegisterSlotDataContainer> registerList, AssetLoader assetLoader, CancellationToken token)
        {
            this.registerList = registerList;

            this.assetLoader = assetLoader;

            viewList = this.registerList.Where(v => v.proposedCount > 0 && v.status == TradeRegister.RegisterSlotStatus.Registered).ToList();

            SetupScroll();
        }

        private void SetupScroll()
        {
            Queue<GameObject> destoryTargetQueue = new Queue<GameObject>();
            for (int i = 0; i < rect_scrollContent.gameObject.transform.childCount; i++)
            {
                destoryTargetQueue.Enqueue(rect_scrollContent.gameObject.transform.GetChild(i).gameObject);
            }

            while (destoryTargetQueue.Count > 0)
            {
                Destroy(destoryTargetQueue.Dequeue());
            }

            for (int i = 0; i < viewList.Count; i++)
            {
                GameObject tmpOb = Instantiate(cellPrefab, rect_scrollContent);

                tmpOb.SetActive(true);

                TradeRegisterSlotTypeRegistered registered = tmpOb.GetComponent<TradeRegisterSlotTypeRegistered>();
                registered.Initialize(i, viewList[i], assetLoader, OnClicked_ViewDetail, OnClicked_CancelRegister);

                tmpOb.transform.localPosition = Vector3.zero;
                tmpOb.transform.localScale = Vector3.one;                
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(rect_scrollContent);
        }

        private void OnClicked_ViewDetail(int slotIndex)
        {
            TradeRegisterStatus.Open(new TradeRegisterStatus.Param(
                viewList[slotIndex].trade_id, 
                viewList[slotIndex].registeredCardData, 
                viewList[slotIndex].proposedCardList,
                viewList[slotIndex].registerDT));
        }

        private void OnClicked_CancelRegister(int slotIndex)
        {
            int cost = 0;
            if (MasterDataManager.Instance.TradeFeeMaster.TryGetTradeFeeEntity(viewList[slotIndex].registeredCardData.CardParam.CurrentRarity, out TradeFeeEntity entity))
            {
                cost = entity.BaseFee + entity.UpgradeLevelFee * viewList[slotIndex].registeredCardData.CardParam.UpgradeLevel;
            }

            int tradingCardIndex = MasterDataManager.Instance.ItemMaster.GetTradingCardIndexByRarity(viewList[slotIndex].registeredCardData.CardParam.CurrentRarity);

            TradeConfirmDialog.Open(new TradeConfirmDialog.Param(TradeConfirmDialog.DialogType.ReverseCostView,
                ItemType.TradingCard, tradingCardIndex, cost, "ID_TRD_1504", "ID_TRD_1532", () =>
                {
                    var _ = CancelRegister(slotIndex);
                }
                ));
        }

        private async UniTask CancelRegister(int slotIndex)
        {
            var response = await APICommand.Trade.CancelRegisterTrade(viewList[slotIndex].trade_id);
            if (response.commonResponse.Code != 0)
                DebugTool.LogError($"{response.commonResponse.Message}");

            await GlobalDataManager.Instance.GlobalUser.GetUserCardList(true);

            // 등록 수수료 반환
            GlobalDataManager.Instance.GlobalUser.UserInfo.AddItemInfomation(
                new ItemInformation((int)ItemType.TradingCard,
                response.cancelRegisterTradeResponse.FeeCardId,
                response.cancelRegisterTradeResponse.FeeCardNum));

            // Refresh
            await tradeTop.RefreshReceptionList();
        }        
    }
}