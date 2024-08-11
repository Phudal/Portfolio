using Cysharp.Threading.Tasks;
using Dimps.Application.API;
using Dimps.Application.AssetBundle;
using Dimps.Application.Common;
using Dimps.Application.Common.Button;
using Dimps.Application.Common.Card;
using Dimps.Application.Common.Dialog;
using Dimps.Application.Common.UI.CardList;
using Dimps.Application.Flow;
using Dimps.Application.Global;
using Dimps.Application.MasterData.Master;
using Dimps.Application.MasterData;
using Dimps.Application.Outgame.RarityUp;
using Dimps.Application.Scene;
using Dimps.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Dimps.Application.Global.UserData;
using Dimps.Application.Common.Constants;


namespace GVNC.Application.Trade
{
    public class TradeProposeStatusDetail : SceneBaseFlow<TradeProposeStatusDetail.Param>
    {
        public class Param : ISceneParameter
        {
            public ProposeListScrollDataConatainer data;

            public Param(ProposeListScrollDataConatainer data)
            {
                this.data = data;
            }
        }

        [SerializeField, Header("Registered Card Info")] private CardDataView registeredCardDataView = null;
        [SerializeField] private UILocalizeText text_registered_playerName;
        [SerializeField] private UILocalizeText text_registered_playerRarity;
        [SerializeField] private UILocalizeText text_registered_playerLevel;
        [SerializeField] private UILocalizeText text_registered_playerPosition;
        // [SerializeField] private Image image_registered_rank;

        [SerializeField, Header("Proposed Card Info")] private CardDataView proposedCardDataView = null;
        [SerializeField] private UILocalizeText text_proposed_playerName;
        [SerializeField] private UILocalizeText text_proposed_playerRarity;
        [SerializeField] private UILocalizeText text_proposed_playerLevel;
        [SerializeField] private UILocalizeText text_proposed_playerPosition;
        // [SerializeField] private Image image_proposed_rank;

        [SerializeField, Header("Status")] private UILocalizeText tmp_tradeStatus;
        [SerializeField] private UILocalizeText tmp_statusDT;

        [SerializeField, Header("Buttons")]
        private RectTransform rect_btnLayout = null;
        [SerializeField] private GameObject gameObject_Confirm = null;
        [SerializeField] private GameObject gameObject_ReceiveAcceptedCard = null;
        [SerializeField] private GameObject gameObject_ReceiveDeclined = null;
        [SerializeField] private GameObject gameObject_CancelOffer = null;
        [SerializeField] private UICustomButton customButton_Confirm = null;
        [SerializeField] private UICustomButton customButton_ReceiveAcceptedCard = null;
        [SerializeField] private UICustomButton customButton_ReceiveDeclined = null;
        [SerializeField] private UICustomButton customButton_CancelOffer = null;
        [SerializeField] private UICustomButton customButton_back = null;

        private Param curParam;
        private AssetLoader assetLoader = new AssetLoader();

        public static void Open(Param param)
        {
            SceneProvider.Instance.ChangeScene(SceneContent.Type.TradeProposeStatusDetail, param);
        }


        protected override Param OnRootStart()
        {
            var param = new Param(null);
            return param;
        }

        /// <summary>
        /// シーン初期化
        /// </summary>
        /// <param name="param">シーンパラメータ</param>
        protected override void OnInitialize(Param param)
        {
            curParam = param;

            customButton_ReceiveAcceptedCard.onClick.RemoveAllListeners();
            customButton_ReceiveAcceptedCard.onClick.AddListener(OnClicked_ReceiveAccepted);

            customButton_CancelOffer.onClick.RemoveAllListeners();
            customButton_CancelOffer.onClick.AddListener(OnClicked_OfferCancel);

            customButton_ReceiveDeclined.onClick.RemoveAllListeners();
            customButton_ReceiveDeclined.onClick.AddListener(OnClick_ReceiveDeclined);

            customButton_Confirm.onClick.RemoveAllListeners();
            customButton_Confirm.onClick.AddListener(OnClicked_BackButton);

            customButton_back.onClick.RemoveAllListeners();
            customButton_back.onClick.AddListener(OnClicked_BackButton);
        }


        protected override async UniTask OnLoad(CancellationToken token)
        {
            // 仮ログイン エディタ上のみ有効
            await APIUtility.EditorSimpleLogin();
           
            #region Registered Card Initialize

            if (curParam.data.CardDataList[1].CardAsset == null)
            {
                AsyncOperationHandle handle = assetLoader.LoadPlayerCardSmallAsync(
                        curParam.data.CardDataList[1].CardParam.CardParamEntity.PlayerPicNo,
                        curParam.data.CardDataList[1].CardParam.CurrentRarity);
                await handle.Task;

                curParam.data.CardDataList[1].SetCardAsset(handle.Result as Sprite);
            }

            // registeredCardView.Setup(curParam.data.CardDataList[0]);
            registeredCardDataView.SetUpCellView(curParam.data.CardDataList[1]);

            text_registered_playerName.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1518"), curParam.data.CardDataList[1].CardParam.CardName));
            text_registered_playerRarity.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1519"), curParam.data.CardDataList[1].CardParam.CurrentRarity.ToString()));
            text_registered_playerLevel.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1520"), OutGameConstatns.AfterTradeLevel));
            text_registered_playerPosition.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1521"), curParam.data.CardDataList[1].CardParam.GoodPosition));

            // int registeredRankIndex = GlobalRarityUp.GetRarityIndex(curParam.data.CardDataList[0].CardParam.CardId, curParam.data.CardDataList[0].CardParam.CurrentRarity);
            // image_registered_rank.sprite = General.Instance.CommonSprite.rankArray[registeredRankIndex].icon;

            registeredCardDataView.Card.InitButton(() =>
            {
                TradePlayerDetail.Open(new TradePlayerDetail.Param(curParam.data.CardDataList[1].CardParam, TradePlayerDetail.ButtonType.Close));
            },
            () =>
            {
                TradePlayerDetail.Open(new TradePlayerDetail.Param(curParam.data.CardDataList[1].CardParam, TradePlayerDetail.ButtonType.Close));
            },
            null);

            #endregion


            #region Proposed Card Initialize

            if (curParam.data.CardDataList[0].CardAsset == null)
            {
                AsyncOperationHandle handle = assetLoader.LoadPlayerCardSmallAsync(
                        curParam.data.CardDataList[0].CardParam.CardParamEntity.PlayerPicNo,
                        curParam.data.CardDataList[0].CardParam.CurrentRarity);
                await handle.Task;

                curParam.data.CardDataList[0].SetCardAsset(handle.Result as Sprite);
            }


            // proposedCardView.Setup(curParam.data.CardDataList[1]);
            proposedCardDataView.SetUpCellView(curParam.data.CardDataList[0]);

            text_proposed_playerName.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1518"), curParam.data.CardDataList[0].CardParam.CardName));
            text_proposed_playerRarity.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1519"), curParam.data.CardDataList[0].CardParam.CurrentRarity.ToString()));
            text_proposed_playerLevel.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1520"), curParam.data.CardDataList[0].CardParam.CardLevel));
            text_proposed_playerPosition.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1521"), curParam.data.CardDataList[0].CardParam.GoodPosition));

            // int proposedRankIndex = GlobalRarityUp.GetRarityIndex(curParam.data.CardDataList[1].CardParam.CardId, curParam.data.CardDataList[1].CardParam.CurrentRarity);
            // image_proposed_rank.sprite = General.Instance.CommonSprite.rankArray[proposedRankIndex].icon;

            proposedCardDataView.Card.InitButton(() =>
            {
                TradePlayerDetail.Open(new TradePlayerDetail.Param(curParam.data.CardDataList[0].CardParam, TradePlayerDetail.ButtonType.Close));
            },
            () =>
            {
                TradePlayerDetail.Open(new TradePlayerDetail.Param(curParam.data.CardDataList[0].CardParam, TradePlayerDetail.ButtonType.Close));
            },
            null);

            #endregion            

            ProposeListScrollDataConatainer.TradeProposeState state = curParam.data.tradeState;

            string stateString = string.Empty;

            switch (state)
            {
                case ProposeListScrollDataConatainer.TradeProposeState.Offered:
                    stateString = LanguageManager.Instance.GetOSTText("ID_TRD_1542");                    
                    break;

                case ProposeListScrollDataConatainer.TradeProposeState.Canceled:
                    stateString = LanguageManager.Instance.GetOSTText("ID_TRD_1514");
                    break;

                case ProposeListScrollDataConatainer.TradeProposeState.Accepted:
                    stateString = LanguageManager.Instance.GetOSTText("ID_TRD_1538");                    
                    break;

                case ProposeListScrollDataConatainer.TradeProposeState.Declined:
                    stateString = LanguageManager.Instance.GetOSTText("ID_TRD_1547");                    
                    break;

                case ProposeListScrollDataConatainer.TradeProposeState.Reverted:
                    stateString = LanguageManager.Instance.GetOSTText("ID_TRD_1547");                    
                    break;

                case ProposeListScrollDataConatainer.TradeProposeState.Received:
                    stateString = LanguageManager.Instance.GetOSTText("ID_TRD_1538");                    
                    break;

                case ProposeListScrollDataConatainer.TradeProposeState.Expired:
                    stateString = LanguageManager.Instance.GetOSTText("ID_TRD_1547");                    
                    break;
            }            

            tmp_tradeStatus.SetTextDirect(stateString);

            tmp_statusDT.gameObject.SetActive(state == ProposeListScrollDataConatainer.TradeProposeState.Offered ||
                state == ProposeListScrollDataConatainer.TradeProposeState.Accepted ||
                state == ProposeListScrollDataConatainer.TradeProposeState.Received);
            
            tmp_statusDT.SetTextDirect(curParam.data.myProposedInfo.offerDT.ToString("g"));
        }


        protected override async UniTask OnActivate(CancellationToken token)
        {
            await UniTask.Yield(cancellationToken: token);
        }

        protected override void OnExecute(Param param)
        {
            ProposeListScrollDataConatainer.TradeProposeState tradeStatus = curParam.data.tradeState;

            // Offered = 0
            // 확인, 취소            
            // Canceled = 1
            // 취소되었으므로 UI 진입 불가
            // Accepted = 2
            // 확인, 수령(상대 카드)
            // Declined = 3
            // 확인, 수령(본인 카드)                        
            // Reverted = 4
            // 이미 수령하였으므로 UI 진입 불가
            // Received = 5
            // 이미 수령하였으므로 UI 진입 불가
            // Expired = 6
            // 요청 기간이 만료됨
            

            gameObject_Confirm.gameObject.SetActive(true);
            gameObject_ReceiveAcceptedCard.gameObject.SetActive(tradeStatus == ProposeListScrollDataConatainer.TradeProposeState.Accepted);
            gameObject_CancelOffer.gameObject.SetActive(tradeStatus == ProposeListScrollDataConatainer.TradeProposeState.Offered);            
            gameObject_ReceiveDeclined.gameObject.SetActive(
                tradeStatus == ProposeListScrollDataConatainer.TradeProposeState.Declined ||
                tradeStatus == ProposeListScrollDataConatainer.TradeProposeState.Expired);

            LayoutRebuilder.ForceRebuildLayoutImmediate(rect_btnLayout);
        }

        protected override void OnFlowEnd(IFlowEndParameter param)
        {

        }

        protected override void OnDispose()
        {
            assetLoader?.Dispose();
            assetLoader = null;
        }

        protected override void OnEscape()
        {

        }

        public void OnClicked_BackButton()
        {
            TradeTop.Open(new TradeTop.Param(TradeTop.TradeTopTab.ProposeList));
        }

        public void OnClicked_ReceiveAccepted()
        {
            CompleteTrade().Forget();
        }

        public void OnClicked_OfferCancel()
        {
            int itemIndex = MasterDataManager.Instance.ItemMaster.GetTradingCardIndexByRarity(curParam.data.myProposedInfo.myProposedCard.CardParam.CurrentRarity);

            int cost = 0;
            if (MasterDataManager.Instance.TradeFeeMaster.TryGetTradeFeeEntity(curParam.data.myProposedInfo.myProposedCard.CardParam.CurrentRarity, out TradeFeeEntity entity))
            {
                cost = entity.BaseFee + entity.UpgradeLevelFee * curParam.data.myProposedInfo.myProposedCard.CardParam.UpgradeLevel;
            }

            TradeConfirmDialog.Open(new TradeConfirmDialog.Param(
                dialogType: TradeConfirmDialog.DialogType.ReverseCostView,
                itemType: ItemType.TradingCard,
                costItemId: itemIndex,
                cost: cost,
                title: "ID_TRD_1531",
                message: "ID_TRD_1544",
                confirmAction: () => CancelPropose().Forget()));
        }

        private async UniTask CompleteTrade()
        {
            var response = await APICommand.Trade.ReceiveAcceptedRegisteredCard(curParam.data.myProposedInfo.offerId);
            if (response.commonResponse.Code != 0)
                DebugTool.LogError($"{response.commonResponse.Message}");

            // 변경된 유저 카드 상태 반영
            await GlobalDataManager.Instance.GlobalUser.GetUserCardList(true);            

            TradeResult.Open(new TradeResult.Param(curParam.data.CardDataList[0].CardParam));
        }

        private async UniTask CancelPropose()
        {
            var response = await APICommand.Trade.CancelProposeTrade(curParam.data.myProposedInfo.offerId);
            if (response.commonResponse.Code != 0)
                DebugTool.LogError($"{response.commonResponse.Message}");

            // 변경된 유저 카드 상태 반영
            await GlobalDataManager.Instance.GlobalUser.GetUserCardList(true);

            // 등록 수수료 반환
            GlobalDataManager.Instance.GlobalUser.UserInfo.AddItemInfomation(
                new ItemInformation((int)ItemType.TradingCard,
                response.cancelProposeTradeResponse.FeeCardId,
                response.cancelProposeTradeResponse.FeeCardNum));


            TradeTop.Open(new TradeTop.Param(TradeTop.TradeTopTab.ProposeList));
        }

        private void OnClick_ReceiveDeclined()
        {
            RevertCard().Forget();
        }

        private async UniTask RevertCard()
        {
            var response = await APICommand.Trade.ReceiveRejectedCard(curParam.data.myProposedInfo.offerId);
            if (response.commonResponse.Code != 0)
                DebugTool.LogError($"{response.commonResponse.Message}");

            // 변경된 유저 카드 상태 반영
            await GlobalDataManager.Instance.GlobalUser.GetUserCardList(true);

            // 등록 수수료 반환
            GlobalDataManager.Instance.GlobalUser.UserInfo.AddItemInfomation(
                new ItemInformation((int)ItemType.TradingCard,
                response.receiveRejectedCardResponse.FeeCardId,
                response.receiveRejectedCardResponse.FeeCardNum));

            TradeTop.Open(new TradeTop.Param(TradeTop.TradeTopTab.ProposeList));

            //Dictionary<CommonDialog.SelectType, CommonDialog.ButtonData> tmpButtonMap = new Dictionary<CommonDialog.SelectType, CommonDialog.ButtonData>();            
            //tmpButtonMap.Add(CommonDialog.SelectType.Center, new CommonDialog.ButtonData(LanguageManager.Instance.GetOSTText("ID_SYS_1800"), () =>
            //{
            //    TradeTop.Open(new TradeTop.Param(TradeTop.TradeTopTab.ProposeList));
            //}));
            //CommonDialog.Open(new CommonDialog.Param(
            //    CommonDialog.ButtonType.Single, LanguageManager.Instance.GetOSTText("ID_TRD_1500"), "__카드와 수수료가 반환되었습니다.__", tmpButtonMap));
        }
    }

}
