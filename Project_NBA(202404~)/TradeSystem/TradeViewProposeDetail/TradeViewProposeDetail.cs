using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dimps.Application.Scene;
using Cysharp.Threading.Tasks;
using Dimps.Application.API;
using Dimps.Application.Common.UI;
using System.Threading;
using Dimps.Application.Common.Button;
using Dimps.Application.AssetBundle;
using Dimps.Application.Common.UI.CardList;
using UnityEngine.ResourceManagement.AsyncOperations;
using Dimps.Application.Common.Card;
using UnityEngine.UI;
using Dimps.Application.Global;
using Dimps.Application.MasterData;
using TMPro;
using Dimps.Application.MasterData.Master;
using Dimps.Utility;
using Dimps.Application.MasterData.Types;
using System;
using Dimps.Application.Common;

namespace GVNC.Application.Trade
{
    public class TradeViewProposeDetail : SceneBase<TradeViewProposeDetail.Param>
    {
        public class Param : ISceneParameter
        {
            public CardData cardData_Give;
            public CardData cardData_Receive;

            public OtherPlayerProposedTradeInfo tradeInfo;

            public Action<long> rejectAction;

            public Param(OtherPlayerProposedTradeInfo tradeInfo, CardData cardData_Give, CardData cardData_Receive, Action<long> rejectAction = null)
            {
                this.tradeInfo = tradeInfo;
                this.cardData_Give = cardData_Give;
                this.cardData_Receive = cardData_Receive;
                this.rejectAction = rejectAction;
            }
        }

        [SerializeField, Header("Card Data View")] private CardDataView cardDataView_Give;
        [SerializeField] private CardDataView cardDataView_Receive;

        [SerializeField, Header("Propose Limit Time")] private UILocalizeText tmp_proposeLimitTime;


        [SerializeField, Header("Butttons")] private UICustomButton btn_OK;
        [SerializeField] private UICustomButton btn_Reject;
        [SerializeField] private UICustomButton btn_Cancel;

        private AssetLoader assetLoader = new AssetLoader();

        private Param curParam;

        public static void Open(Param param)
        {
            SceneProvider.Instance.AddScene(SceneContent.Type.TradeViewProposeDetail, param);
        }

        protected override Param OnRootStart()
        {
            var param = new Param(null, null, null);
            return param;
        }
        protected override void OnInitialize(Param param)
        {
            curParam = param;

            btn_Cancel.onClick.AddListener(() => CloseDialog(null));
            btn_OK.onClick.AddListener(() => OpenTradeConfirmDialog());
            btn_Reject.onClick.AddListener(() => OpenTradeRejectDialog());

            cardDataView_Give.Card.InitButton(() =>
            {
                TradePlayerDetail.Open(new TradePlayerDetail.Param(curParam.cardData_Give.CardParam, TradePlayerDetail.ButtonType.Close));
            },
            () =>
            {
                TradePlayerDetail.Open(new TradePlayerDetail.Param(curParam.cardData_Give.CardParam, TradePlayerDetail.ButtonType.Close));
            },
            null);

            cardDataView_Receive.Card.InitButton(() =>
            {
                TradePlayerDetail.Open(new TradePlayerDetail.Param(curParam.cardData_Receive.CardParam, TradePlayerDetail.ButtonType.Close));
            },
            () =>
            {
                TradePlayerDetail.Open(new TradePlayerDetail.Param(curParam.cardData_Receive.CardParam, TradePlayerDetail.ButtonType.Close));
            },
            null);
        }

        protected override async UniTask OnLoad(CancellationToken token)
        {
            //仮のログイン※Editor専用
            await APIUtility.EditorSimpleLogin();

            await UniTask.Yield(cancellationToken: token);

            if (curParam.cardData_Give.CardAsset == null)
            {
                AsyncOperationHandle giveHandle = assetLoader.LoadPlayerCardSmallAsync(
                    curParam.cardData_Give.CardParam.CardParamEntity.PlayerPicNo, curParam.cardData_Give.CardParam.CurrentRarity);

                await giveHandle.Task;
                curParam.cardData_Give.SetCardAsset(giveHandle.Result as Sprite);
            }

            if (curParam.cardData_Receive.CardAsset == null)
            {
                AsyncOperationHandle receiveHandle = assetLoader.LoadPlayerCardSmallAsync(
                    curParam.cardData_Receive.CardParam.CardParamEntity.PlayerPicNo, curParam.cardData_Receive.CardParam.CurrentRarity);

                await receiveHandle.Task;
                curParam.cardData_Receive.SetCardAsset(receiveHandle.Result as Sprite);
            }

            cardDataView_Give.SetUpCellView(curParam.cardData_Give);
            cardDataView_Receive.SetUpCellView(curParam.cardData_Receive);

            cardDataView_Give.Card.InitButton(
                () => {},
                () => {
                    TradePlayerDetail.Open(new TradePlayerDetail.Param(curParam.cardData_Give.CardParam, TradePlayerDetail.ButtonType.Close));
                }, null);

            cardDataView_Receive.Card.InitButton(
                () => {},
                () => {
                    TradePlayerDetail.Open(new TradePlayerDetail.Param(curParam.cardData_Receive.CardParam, TradePlayerDetail.ButtonType.Close));
                }, null);

            tmp_proposeLimitTime.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1511"), curParam.tradeInfo.proposedDT.AddDays(1).ToString("g")).Replace("\\n", "\n"));
        }

        /// <summary>
        /// シーン有効化時
        /// </summary>
        /// <param name="token">キャンセルトークン</param>
        /// <returns></returns>
        protected override async UniTask OnActivate(CancellationToken token)
        {
            await UniTask.Yield();
        }

        protected override void OnExecute(Param param)
        {

        }

        protected override void OnDispose()
        {
            if (assetLoader != null)
            {
                assetLoader.Dispose();
                assetLoader = null;
            }
        }

        private void OpenTradeConfirmDialog()
        {
            TradeConfirmDialog.Open(new TradeConfirmDialog.Param(
                dialogType: TradeConfirmDialog.DialogType.Normal,
                itemType: ItemType.None,
                costItemId: 0,
                cost: 0,
                title: "ID_TRD_1515",
                message: "ID_TRD_1537",
                confirmAction: () => { var _ = TradeComplete(); }));
        }

        private async UniTask TradeComplete()
        {
            var acceptResponse = await APICommand.Trade.AcceptTrade(curParam.tradeInfo.trade_id, curParam.tradeInfo.offer_id);
            if (acceptResponse.commonResponse.Code != 0)
                DebugTool.LogError($"{acceptResponse.commonResponse.Message}");

            var receiveResponse = await APICommand.Trade.ReceiveProposedCard(curParam.tradeInfo.trade_id);
            if (receiveResponse.commonResponse.Code != 0)
                DebugTool.LogError($"{receiveResponse.commonResponse.Message}");
            
            await GlobalDataManager.Instance.GlobalUser.GetUserCardList(true);

            CardData card = new CardData(0, receiveResponse.rejectTradeResponse.ReceiveOfferCardInfo.CardId, (Rarity)receiveResponse.rejectTradeResponse.ReceiveOfferCardInfo.Rarity);

            TradeResult.Open(new TradeResult.Param(card.CardParam));
        }

        private void OpenTradeRejectDialog()
        {
            TradeConfirmDialog.Open(new TradeConfirmDialog.Param(
                dialogType: TradeConfirmDialog.DialogType.Normal,
                itemType: ItemType.None,
                costItemId: 0,
                cost: 0,
                title : "ID_TRD_1535",
                message: "ID_TRD_1540",
                confirmAction: () => RejectComplete().Forget()));
        }
        
        private async UniTask RejectComplete()
        {
            var response = await APICommand.Trade.RejectTrade(curParam.tradeInfo.trade_id, curParam.tradeInfo.offer_id);
            if (response.commonResponse.Code != 0)
                DebugTool.LogError($"{response.commonResponse.Message}");

            await GlobalDataManager.Instance.GlobalUser.GetUserCardList(true);

            // 열려있는 UI의 목록에서 삭제할 수 있도록 콜백 실행
            curParam.rejectAction?.Invoke(curParam.tradeInfo.offer_id);

            CloseDialog();
        }
    }
}