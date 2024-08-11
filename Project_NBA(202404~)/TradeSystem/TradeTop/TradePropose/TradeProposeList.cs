using GVNC.Application.Trade;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dimps.Application.Common.UI;
using Dimps.Application.MasterData;
using Cysharp.Threading.Tasks;
using Dimps.Application.AssetBundle;
using Dimps.Utility;
using System.Threading;
using System.Diagnostics;
using System;
using Dimps.Application.API;
using Dimps.Application.Global;
using Dimps.Application.MasterData.Master;
using Dimps.Application.MasterData.Types;
using Dimps.Application.Common.Dialog;
using Dimps.Application.Common;
using System.Linq;

public class TradeProposeList : MonoBehaviour, IMultiLoadGridScrollContext
{
    [SerializeField] private TradeTop tradeTop;

    [SerializeField] private UICustomTab filterButton;
    [SerializeField] private TradeProposeListController controller;

    private List<ProposeListScrollDataConatainer> proposedPossesion = new List<ProposeListScrollDataConatainer>();
    private List<ProposeListScrollDataConatainer> viewList = new List<ProposeListScrollDataConatainer>();

    private CardListRule proposedFilterRule = new CardListRule();

    private AssetLoader assetLoader;
    private CancellationToken token;

    public void Init(List<ProposeListScrollDataConatainer> proposedList, AssetLoader assetLoader, CancellationToken token)
    {
        this.proposedPossesion = proposedList;

        this.assetLoader = assetLoader;
        this.token = token;

        viewList = proposedPossesion;

        filterButton.onClick.RemoveAllListeners();
        filterButton.onClick.AddListener(() =>
        {
            OpenTradeFilterMenu();
        });

        filterButton.Toggle = proposedFilterRule.CheckFilterState();

        Load().Forget();
    }
    
    public void ScrollItemCallback_1(MultiLoadScrollDataConatainer data)
    {
        // View Propose Detail
        TradeProposeStatusDetail.Open(new TradeProposeStatusDetail.Param(data as ProposeListScrollDataConatainer));
    }

    public void ScrollItemCallback_2(MultiLoadScrollDataConatainer data)
    {
        // Cancel Propose
        OnClicked_ProposeCancel(data as ProposeListScrollDataConatainer);
    }

    public void ScrollItemCallback_3(MultiLoadScrollDataConatainer data)
    {
        // Revert Propose
        OnClicked_RevertPropose(data as ProposeListScrollDataConatainer);
    }

    public void ScrollItemCallback_4(MultiLoadScrollDataConatainer data)
    {
        // Complete Propose
        OnClicked_CompletePropose(data as ProposeListScrollDataConatainer);
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

    public void OnClicked_ProposeCancel(ProposeListScrollDataConatainer data)
    {
        int itemIndex = MasterDataManager.Instance.ItemMaster.GetTradingCardIndexByRarity(data.myProposedInfo.myProposedCard.CardParam.CurrentRarity);

        int cost = 0;
        if (MasterDataManager.Instance.TradeFeeMaster.TryGetTradeFeeEntity(data.myProposedInfo.myProposedCard.CardParam.CurrentRarity, out TradeFeeEntity entity))
        {
            cost = entity.BaseFee + entity.UpgradeLevelFee * data.myProposedInfo.myProposedCard.CardParam.UpgradeLevel;
        }

        TradeConfirmDialog.Open(new TradeConfirmDialog.Param(
            dialogType: TradeConfirmDialog.DialogType.ReverseCostView,
            itemType: ItemType.TradingCard,
            costItemId: itemIndex,
            cost: cost,
            title : "ID_TRD_1531",
            message: "ID_TRD_1544",
            confirmAction: () => CancelPropose(data).Forget()));
    }

    private async UniTask CancelPropose(ProposeListScrollDataConatainer data)
    {
        var response = await APICommand.Trade.CancelProposeTrade(data.myProposedInfo.offerId);
        if (response.commonResponse.Code != 0)
            DebugTool.LogError($"{response.commonResponse.Message}");

        // 변경된 유저 카드 상태 반영
        await GlobalDataManager.Instance.GlobalUser.GetUserCardList(true);

        // 등록 수수료 반환
        GlobalDataManager.Instance.GlobalUser.UserInfo.AddItemInfomation(
            new ItemInformation((int)ItemType.TradingCard,
            response.cancelProposeTradeResponse.FeeCardId,
            response.cancelProposeTradeResponse.FeeCardNum));

        await tradeTop.RefreshProposeList();

        //await RefreshProposedData();

        //Load().Forget();
    }

    private async UniTask RefreshProposedData()
    {
        proposedPossesion.Clear();

        var response = await APICommand.Trade.GetMyProposedTradeInfoResponse();
        if (response.commonResponse.Code != 0)
            DebugTool.LogError($"{response.commonResponse.Message}");

        foreach (var myProposed in response.getTradeListResponse.TradeOfferInfo)
        {
            CardData proposedCardData = await GlobalFunction.GetCardDataFromCardInsid(myProposed.TradeOffer.CardInstanceId, CardData.CardViewType.TradeInfo);

            CardData rCard = new CardData(0, myProposed.Trade.CardId, (Rarity)myProposed.Trade.Rarity);
            rCard.ViewType = CardData.CardViewType.TradeInfo;
            rCard.CardParam.ForceSetupInsid(myProposed.Trade.CardInstanceId);

            MyProposedTradeInfo info = new MyProposedTradeInfo(myProposed.TradeOffer.TradeId, myProposed.TradeOffer.OfferId, proposedCardData, rCard, GlobalFunction.GetLocalTime(myProposed.TradeOffer.OfferTime));

            proposedPossesion.Add(new ProposeListScrollDataConatainer(info, (ProposeListScrollDataConatainer.TradeProposeState)myProposed.State));
        }        
    }

    public void OnClicked_RevertPropose(ProposeListScrollDataConatainer data)
    {
        RevertPropose(data).Forget();
    }

    private async UniTask RevertPropose(ProposeListScrollDataConatainer data)
    {
        var response = await APICommand.Trade.ReceiveRejectedCard(data.myProposedInfo.offerId);
        if (response.commonResponse.Code != 0)
            DebugTool.LogError($"{response.commonResponse.Message}");

        // 변경된 유저 카드 상태 반영
        await GlobalDataManager.Instance.GlobalUser.GetUserCardList(true);

        // 등록 수수료 반환
        GlobalDataManager.Instance.GlobalUser.UserInfo.AddItemInfomation(
            new ItemInformation((int)ItemType.TradingCard,
            response.receiveRejectedCardResponse.FeeCardId,
            response.receiveRejectedCardResponse.FeeCardNum));

        await tradeTop.RefreshProposeList();

        //Dictionary<CommonDialog.SelectType, CommonDialog.ButtonData> tmpButtonMap = new Dictionary<CommonDialog.SelectType, CommonDialog.ButtonData>();
        //tmpButtonMap.Add(CommonDialog.SelectType.Center, new CommonDialog.ButtonData(
        //    LanguageManager.Instance.GetOSTText("ID_SYS_1800"), 
        //    async () => 
        //    {
        //        await tradeTop.RefreshProposeList();
        //    }));
        //CommonDialog.Open(new CommonDialog.Param(
        //    CommonDialog.ButtonType.Single, 
        //    LanguageManager.Instance.GetOSTText("ID_TRD_1500")
        //    , "__카드와 수수료가 반환되었습니다.__", tmpButtonMap));
    }

    public void OnClicked_CompletePropose(ProposeListScrollDataConatainer data)
    {
        CompletePropose(data).Forget();
    }

    private async UniTask CompletePropose(ProposeListScrollDataConatainer data)
    {
        var response = await APICommand.Trade.ReceiveAcceptedRegisteredCard(data.myProposedInfo.offerId);
        if (response.commonResponse.Code != 0)
            DebugTool.LogError($"{response.commonResponse.Message}");

        // 변경된 유저 카드 상태 반영
        await GlobalDataManager.Instance.GlobalUser.GetUserCardList(true);

        TradeResult.Open(new TradeResult.Param(data.myProposedInfo.registeredCard.CardParam));
    }

    private void OpenTradeFilterMenu()
    {
        TradeFilterMenu.Open(new TradeFilterMenu.Param(proposedFilterRule, "ID_TRD_1501", (rule) =>
        {
            proposedFilterRule = rule;

            filterButton.Toggle = proposedFilterRule.CheckFilterState();

            viewList = TradeCardListSortFilter.Filter_TradeProposed(proposedPossesion, proposedFilterRule.FilterRules).ToList();

            Load().Forget();
        }));
    }
}
