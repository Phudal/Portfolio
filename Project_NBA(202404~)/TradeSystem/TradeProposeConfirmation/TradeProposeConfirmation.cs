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
using Dimps.Application.Common;

namespace GVNC.Application.Trade
{
    public class TradeProposeConfirmation : SceneBase<TradeProposeConfirmation.Param>
    {
        public class Param : ISceneParameter
        {
            public long trade_id;

            public bool isNPCData;

            public CardData cardData_Give;
            public CardData cardData_Receive;

            public Param(long trade_id, bool isNPCData, CardData cardData_Give, CardData cardData_Receive)
            {
                this.trade_id = trade_id;
                this.isNPCData = isNPCData;
                this.cardData_Give = cardData_Give;
                this.cardData_Receive = cardData_Receive;
            }   
        }

        [SerializeField, Header("Card Data View")] private CardDataView cardDataView_Give;
        [SerializeField] private CardDataView cardDataView_Receive;

        [SerializeField, Header("Propose Limit Time")] private UILocalizeText tmp_proposeLimitTime;

        [SerializeField, Header("Butttons")] private UICustomButton btn_OK;        
        [SerializeField] private UICustomButton btn_Cancel;

        [SerializeField, Header("Cost View")] private GameObject gameObject_CostInfo;
        [SerializeField] private Image image_costIcon;
        [SerializeField] private Image image_curItemIcon;
        [SerializeField] private TextMeshProUGUI tmp_Cost;
        [SerializeField] private TextMeshProUGUI tmp_beforeMoney;
        [SerializeField] private TextMeshProUGUI tmp_afterMoney;

        [SerializeField, Header("Waring Text")] private UILocalizeText tmp_tradeFeeWarning;

        private AssetLoader assetLoader = new AssetLoader();

        private Param curParam;

        public static void Open(Param param)
        {
            SceneProvider.Instance.AddScene(SceneContent.Type.TradeProposeConfirm, param);
        }

        protected override Param OnRootStart()
        {
            var param = new Param(default, false, null, null);
            return param;
        }
        protected override void OnInitialize(Param param)
        {
            curParam = param;
            
            btn_OK.onClick.AddListener(() => OnClicked_Propose());            
            btn_Cancel.onClick.AddListener(() => CloseDialog(null));
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

            curParam.cardData_Give.ViewType = CardData.CardViewType.TradeInfo;
            cardDataView_Give.SetUpCellView(curParam.cardData_Give);
            curParam.cardData_Receive.ViewType = CardData.CardViewType.TradeInfo;
            cardDataView_Receive.SetUpCellView(curParam.cardData_Receive);            

            tmp_proposeLimitTime.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1511"), GlobalFunction.GetLocalTime(GlobalDataManager.Instance.GlobalTime.CurrentTime).AddDays(1).ToString("g")).Replace("\\n", "\n"));

            #region Cost View
            int entityIndex = MasterDataManager.Instance.ItemMaster.GetTradingCardIndexByRarity(curParam.cardData_Give.CardParam.CurrentRarity);

            Sprite sprite = await GlobalFunction.GetItemIcon(entityIndex, ItemType.TradingCard);
            image_costIcon.sprite = sprite;
            image_curItemIcon.sprite = sprite;

            if (MasterDataManager.Instance.TradeFeeMaster.TryGetTradeFeeEntity(curParam.cardData_Give.CardParam.CurrentRarity, out TradeFeeEntity entity))
            {
                int totalFee = entity.BaseFee + (entity.UpgradeLevelFee * curParam.cardData_Give.CardParam.UpgradeLevel);

                gameObject_CostInfo.SetActive(totalFee > 0);
                if (totalFee <= 0)
                {
                    return;
                }

                tmp_Cost.text = totalFee.ToString();

                int userHaveTradingCard = GlobalDataManager.Instance.GlobalUser.UserInfo.GetUserHaveTradingCard(curParam.cardData_Give.CardParam.CurrentRarity);

                tmp_beforeMoney.text = userHaveTradingCard.ToString();
                tmp_afterMoney.text = (userHaveTradingCard - totalFee).ToString();

                btn_OK.Inactive = userHaveTradingCard - totalFee < 0;

            }

            tmp_tradeFeeWarning.SetTextDirect(LanguageManager.Instance.GetOSTText("ID_TRD_1513").Replace("\\n", "\n"));
            #endregion
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

        private void OnClicked_Propose()
        {
            ProposeTrade().Forget();
        }

        private async UniTask ProposeTrade()
        {
            if (curParam.isNPCData == false)
            {
                var response = await APICommand.Trade.ProposeTrade(curParam.trade_id, curParam.cardData_Give.CardParam.CardInfoPlayer.CardInsId, 0);
                if (response.commonResponse.Code != 0)
                    DebugTool.LogError($"{response.commonResponse.Message}");

                // 변경된 유저 카드 상태 반영
                await GlobalDataManager.Instance.GlobalUser.GetUserCardList(true);

                // 등록 수수료 소모
                GlobalDataManager.Instance.GlobalUser.UserInfo.AddItemInfomation(
                    new ItemInformation((int)ItemType.TradingCard,
                    response.proposeTradeResponse.FeeCardId,
                    response.proposeTradeResponse.FeeCardNum * -1));
            }
            else
            {
                var response = await APICommand.Trade.RegisterNPCTrade(
                    cardId: curParam.cardData_Receive.CardParam.CardId,
                    OfferCardId: curParam.cardData_Give.CardParam.CardId,
                    OfferCardRarity: (int)curParam.cardData_Receive.CardParam.CurrentRarity,
                    offerCardInstanceId: curParam.cardData_Give.CardParam.CardInfoPlayer.CardInsId);

                if (response.commonResponse.Code != 0)
                    DebugTool.LogError($"{response.commonResponse.Message}");

                // 변경된 유저 카드 상태 반영
                await GlobalDataManager.Instance.GlobalUser.GetUserCardList(true);

                // 등록 수수료 소모
                GlobalDataManager.Instance.GlobalUser.UserInfo.AddItemInfomation(
                    new ItemInformation((int)ItemType.TradingCard,
                    response.registerNpcTradeResponse.FeeCardId,
                    response.registerNpcTradeResponse.FeeCardNum * -1));
            }

            TradeTop.Open(new TradeTop.Param(TradeTop.TradeTopTab.ProposeList));
        }
    }
}