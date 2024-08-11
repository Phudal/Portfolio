using Cysharp.Threading.Tasks;
using Dimps.Application.API;
using Dimps.Application.Common.UI;
using Dimps.Application.Flow;
using Dimps.Application.Global;
using Dimps.Application.Global.UserData;
using Dimps.Application.MasterData;
using Dimps.Application.Scene;
using Dimps.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Dimps.Application.MasterData.Master;
using Dimps.Application.AssetBundle;
using Dimps.Application.Common;
using Dimps.Application.Common.Card;
using UnityEngine.ResourceManagement.AsyncOperations;
using Dimps.Application.Common.Button;
using TMPro;

namespace GVNC.Application.Trade
{
    public class TradeRegisterStatus : SceneBaseFlow<TradeRegisterStatus.Param>
    {
        public class Param : ISceneParameter
        {
            public long trade_id;

            public CardData registeredCardData;

            public List<OtherPlayerProposedTradeInfo> proposedTradeInfo;

            public DateTime registerDT;

            public Param(long trade_id, CardData registeredCardData, List<OtherPlayerProposedTradeInfo> proposedCardDatas, DateTime registerDT)
            {
                this.trade_id = trade_id;
                this.registeredCardData = registeredCardData;
                this.proposedTradeInfo = proposedCardDatas;
                this.registerDT = registerDT;
            }
        }

        [SerializeField, Header("Registered Card Info")] private UICard registeredCardView = null;
        [SerializeField] private UILocalizeText text_playerName;
        [SerializeField] private UILocalizeText text_playerRarity;
        [SerializeField] private UILocalizeText text_playerLevel;
        [SerializeField] private UILocalizeText text_registeredDT;
        [SerializeField] private UILocalizeText text_proposedCount;

        [SerializeField, Header("Proposed Card Info")] private List<TradeRegisterStatusProposedItem> proposedItems = new List<TradeRegisterStatusProposedItem>();

        [SerializeField, Header("Buttons")] private UICustomButton customButton_registerCancel = null;
        [SerializeField] private UICustomButton customButton_back = null;

        [SerializeField] private GameObject gameObject_ProposedItem;
        [SerializeField] private GameObject gameObject_NotProposedItem;

        private AssetLoader assetLoader = new AssetLoader();

        private Param curParam;

        public static void Open(Param param)
        {
            SceneProvider.Instance.ChangeScene(SceneContent.Type.TradeRegisterStatus, param);
        }

        //----------------------------------------------------------------------------------------------------
        // SceneBaseFlow.
        //----------------------------------------------------------------------------------------------------

        /// <summary>
        /// 初期シーン開始時
        /// <para>アプリケーション起動時の初期シーンとして実行された場合に呼ばれます</para>
        /// <para>MonoBehaviour.Start時に呼ばれます</para>
        /// </summary>
        /// <returns>シーン開始時のパラメータ</returns>
        protected override Param OnRootStart()
        {
            var param = new Param(default, null, null, default);
            return param;
        }

        /// <summary>
        /// シーン初期化
        /// </summary>
        /// <param name="param">シーンパラメータ</param>
        protected override void OnInitialize(Param param)
        {
            curParam = param;

            customButton_registerCancel.onClick.RemoveAllListeners();
            customButton_registerCancel.onClick.AddListener(OnClicked_CancelRegister);

            customButton_back.onClick.RemoveAllListeners();
            customButton_back.onClick.AddListener(OnClicked_BackButton);
        }

        /// <summary>
        /// シーン読み込み時
        /// </summary>
        /// <param name="token">キャンセルトークン</param>
        /// <returns></returns>
        protected override async UniTask OnLoad(CancellationToken token)
        {
            // 仮ログイン エディタ上のみ有効
            await APIUtility.EditorSimpleLogin();

            SetupRegisteredData();

            await SetupProposedTradeData();
        }

        /// <summary>
        /// シーン有効化時
        /// </summary>
        /// <param name="token">キャンセルトークン</param>
        /// <returns></returns>
        protected override async UniTask OnActivate(CancellationToken token)
        {
            await UniTask.Yield(cancellationToken: token);
        }

        /// <summary>
        /// シーン実行
        /// </summary>
        /// <param name="param">シーンパラメータ</param>
        protected override void OnExecute(Param param)
        {

        }


        /// <summary>
        /// フロー終了
        /// </summary>
        /// <param name="param">シーン終了パラメータ</param>
        protected override void OnFlowEnd(IFlowEndParameter param)
        {

        }

        /// <summary>
        /// シーン破棄
        /// <para>MonoBehaviour.OnDestroy時に呼ばれます</para>
        /// </summary>
        protected override void OnDispose()
        {
            assetLoader?.Dispose();
            assetLoader = null;
        }

        protected override void OnEscape()
        {

        }

        private void SetupRegisteredData()
        {
            registeredCardView.Setup(curParam.registeredCardData);

            text_playerName.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1518"), curParam.registeredCardData.CardParam.CardName));
            text_playerRarity.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1519"), curParam.registeredCardData.CardParam.CurrentRarity));
            text_playerLevel.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1520"), curParam.registeredCardData.CardParam.CardLevel));
            text_registeredDT.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1516"), curParam.registerDT.ToString("g")));
            text_proposedCount.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1517"), curParam.proposedTradeInfo.Count));            
        }

        private async UniTask SetupProposedTradeData()
        {
            for (int i = 0; i < proposedItems.Count; i++)
            {
                if (i >= curParam.proposedTradeInfo.Count)
                {
                    proposedItems[i].gameObject.SetActive(false);
                    continue;
                }
                proposedItems[i].gameObject.SetActive(true);

                if (curParam.proposedTradeInfo[i].cardData.CardAsset == null)
                {
                    AsyncOperationHandle handle = assetLoader.LoadPlayerCardSmallAsync(
                        curParam.proposedTradeInfo[i].cardData.CardParam.CardParamEntity.PlayerPicNo,
                        curParam.proposedTradeInfo[i].cardData.CardParam.CurrentRarity);
                    await handle.Task;
                    curParam.proposedTradeInfo[i].cardData.SetCardAsset(handle.Result as Sprite);
                }

                proposedItems[i].InitializeData(i, curParam.proposedTradeInfo[i].cardData, assetLoader, OnClicked_ProposedDetail);
                await proposedItems[i].LoadCardAsset();
            }

            gameObject_ProposedItem.SetActive(curParam.proposedTradeInfo.Count > 0);
            gameObject_NotProposedItem.SetActive(curParam.proposedTradeInfo.Count <= 0);
        }

        private void OnClicked_BackButton()
        {
            if (SceneProvider.Instance.RegressionCurrentScene == SceneContent.Type.Application)
            {
                TradeRegister.Open(new TradeRegister.Param());
                return;
            }

            SceneProvider.Instance.RegressionScene();
        }

        private void OnClicked_ProposedDetail(int slotIndex)
        {
            TradeViewProposeDetail.Open(new TradeViewProposeDetail.Param(
                curParam.proposedTradeInfo[slotIndex], curParam.registeredCardData, curParam.proposedTradeInfo[slotIndex].cardData,
                (offerId) => RefreshByReject(offerId)));
        }

        private void OnClicked_CancelRegister()
        {
            int itemIndex = MasterDataManager.Instance.ItemMaster.GetTradingCardIndexByRarity(curParam.registeredCardData.CardParam.CurrentRarity);

            int cost = 0;
            if (MasterDataManager.Instance.TradeFeeMaster.TryGetTradeFeeEntity(curParam.registeredCardData.CardParam.CurrentRarity, out TradeFeeEntity entity))
            {
                cost = entity.BaseFee + entity.UpgradeLevelFee * curParam.registeredCardData.CardParam.UpgradeLevel;
            }

            TradeConfirmDialog.Open(new TradeConfirmDialog.Param(
                dialogType: TradeConfirmDialog.DialogType.ReverseCostView,
                itemType: ItemType.TradingCard,
                costItemId: itemIndex,
                cost: cost,
                title: "ID_TRD_1527",
                message: "ID_TRD_1528",
                confirmAction: () => {
                    var _ = CancelRegister();
                }));
        }

        private async UniTask CancelRegister()
        {
            var response = await APICommand.Trade.CancelRegisterTrade(curParam.trade_id);
            if (response.commonResponse.Code != 0)
                DebugTool.LogError($"{response.commonResponse.Message}");

            await GlobalDataManager.Instance.GlobalUser.GetUserCardList(true);

            // 등록 수수료 반환
            GlobalDataManager.Instance.GlobalUser.UserInfo.AddItemInfomation(
                new ItemInformation((int)ItemType.TradingCard,
                response.cancelRegisterTradeResponse.FeeCardId,
                response.cancelRegisterTradeResponse.FeeCardNum));

            OnClicked_BackButton();
        }

        private void RefreshByReject(long offerId)
        {
            OtherPlayerProposedTradeInfo rejected = null;
            foreach (OtherPlayerProposedTradeInfo info in curParam.proposedTradeInfo)
            {
                if (info.offer_id == offerId)
                {
                    rejected = info;
                    break;
                }
            }
            curParam.proposedTradeInfo.Remove(rejected);

            SetupRegisteredData();

            SetupProposedTradeData().Forget();
        }
    }
}