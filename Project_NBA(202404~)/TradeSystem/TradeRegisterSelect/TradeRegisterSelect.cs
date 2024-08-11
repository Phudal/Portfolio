using Cysharp.Threading.Tasks;
using Dimps.Application.API;
using Dimps.Application.API.Message;
using Dimps.Application.AssetBundle;
using Dimps.Application.Common;
using Dimps.Application.Common.Button;
using Dimps.Application.Common.Dialog;
using Dimps.Application.Common.UI;
using Dimps.Application.Flow;
using Dimps.Application.Global;
using Dimps.Application.Global.UserData;
using Dimps.Application.MasterData;
using Dimps.Application.MasterData.Master;
using Dimps.Application.MasterData.Types;
using Dimps.Application.Scene;
using Dimps.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;

namespace GVNC.Application.Trade
{
    public class TradeRegisterSelect : SceneBaseFlow<TradeRegisterSelect.Param>, ICardListGridScrollContext
    {
        public class Param : ISceneParameter
        {
            public int slotIndex;

            public Param(int slotIndex)
            {
                this.slotIndex = slotIndex;
            }
        }
        private const SortRule DefaultSortRule = SortRule.Rarity;

        [SerializeField] private CardListController cardListController = null;

        [SerializeField] private TextMeshProUGUI tmp_curPlayerCount = null;

        [SerializeField] private UICustomButton acceptButton = null;
        [SerializeField] private UICustomButton backButton = null;
        [SerializeField] private UICustomTab filterButton = null;

        private List<CardData> tradeableCardList = new List<CardData>();
        private List<CardData> viewCardList = new List<CardData>();

        private AssetLoader assetLoader = new();
        private UniTask lockTask;
        private UserCardList userCardList = null;

        private CardListRule myRule;

        private CardData selectedRegisterCardData = null;

        private Param curParam;

        //----------------------------------------------------------------------------------------------------
        // Static.
        //----------------------------------------------------------------------------------------------------

        /// <summary>
        /// シーンを開く.
        /// </summary>
        /// <param name="param"></param>
        public static void Open(Param param)
        {
            SceneProvider.Instance.ChangeScene(SceneContent.Type.TradeRegisterSelect, param);
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
            var param = new Param(0);
            return param;
        }

        /// <summary>
        /// シーン初期化
        /// </summary>
        /// <param name="param">シーンパラメータ</param>
        protected override void OnInitialize(Param param)
        {
            curParam = param;

            myRule = new CardListRule(DefaultSortRule, true);

            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(() =>
            {
                OnClick_AcceptButton();
            });

            filterButton.onClick.RemoveAllListeners();
            filterButton.onClick.AddListener(() =>
            {
                OpenSortFilterMenu();
            });

            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() =>
            {
                OnClicked_BackButton();
            });

            General.Instance.Footer.Show(); // フッター表示
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

            userCardList = await GlobalDataManager.Instance.GlobalUser.GetUserCardList();
            foreach (var data in userCardList.UserPlayerCard.Values)
            {
                // 사용 중인 카드는 트레이드 불가
                // 잠금               스킬트레이닝              트레이드          팀편성
                if (data.IsLocked || data.UseSkillTraining || data.IsTrading || data.UseDeckStatus)
                    continue;

                // 트레이드 금지 카드
                if (MasterDataManager.Instance.TradeBlockEntityMaster.IsTradeBlockCard(data.CardId))
                    continue;

                tradeableCardList.Add(new CardData(0, data, CardData.CardViewType.Normal));
            }

            viewCardList = CardListSortFilter.SortFilter(tradeableCardList, myRule).ToList();

            await GlobalDataManager.Instance.GlobalUser.GetUserCardLogbook();
        }

        /// <summary>
        /// シーン有効化時
        /// </summary>
        /// <param name="token">キャンセルトークン</param>
        /// <returns></returns>
        protected override async UniTask OnActivate(CancellationToken token)
        {
            await UniTask.Yield(cancellationToken: token);

            cardListController.Setup(this, viewCardList);
            tmp_curPlayerCount.text = viewCardList.Count.ToString();

            await cardListController.LoadInitCard(viewCardList, assetLoader);
            cardListController.LoadCard(12, 4, assetLoader, token).Forget();
        }

        /// <summary>
        /// シーン実行
        /// </summary>
        /// <param name="param">シーンパラメータ</param>
        protected override void OnExecute(Param param)
        {
            cardListController.SetEnableDisplaySelectView(true);

            acceptButton.Inactive = true;

            // ヘッダー非表示
            // General.Instance.Header.Hide();
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
            cardListController.Dispose();
            assetLoader.Dispose();
            assetLoader = null;
            userCardList.SetViewFlag();
        }

        protected override void OnEscape()
        {

        }

        public void Select(CardData data)
        {
            foreach (CardData card in viewCardList)
            {
                if (data.CardParam.CardInfoPlayer.CardInsId == card.CardParam.CardInfoPlayer.CardInsId)
                {
                    continue;
                }

                card.IsSelectedNum = 0;
            }

            SelectCardData(data);

            bool isActiveAcceptBtn = false;
            foreach (CardData card in viewCardList)
            {
                if (card.IsSelectedNum > 0)
                {
                    isActiveAcceptBtn = true;
                    break;
                }
            }

            acceptButton.Inactive = !isActiveAcceptBtn;
            cardListController.UpdateCellListStatus();
        }

        public void LongSelect(CardData data)
        {
            OpenCardDetail(data);
        }

        public void ZoomInCard(CardData data)
        {

        }

        private void OpenSortFilterMenu()
        {
            SortFilterMenu.Open(new SortFilterMenu.Param(myRule, (rules) =>
            {
                myRule = rules;

                viewCardList = CardListSortFilter.SortFilter(tradeableCardList, myRule).ToList();
                tmp_curPlayerCount.text = viewCardList.Count.ToString();

                cardListController.ResetCardData(viewCardList);
            }));

        }

        private void OnClick_AcceptButton()
        {
            if (selectedRegisterCardData == null)
                return;

            OpenTradeRegisterConfirmation(selectedRegisterCardData);
        }

        private void OpenTradeRegisterConfirmation(CardData registerCardData)
        {
            int itemIndex = MasterDataManager.Instance.ItemMaster.GetTradingCardIndexByRarity(registerCardData.CardParam.CurrentRarity);

            int cost = 0;
            if (MasterDataManager.Instance.TradeFeeMaster.TryGetTradeFeeEntity(registerCardData.CardParam.CurrentRarity, out TradeFeeEntity entity))
            {
                cost = entity.BaseFee + entity.UpgradeLevelFee * registerCardData.CardParam.UpgradeLevel;
            }

            TradeConfirmDialog.Open(new TradeConfirmDialog.Param(
                dialogType: TradeConfirmDialog.DialogType.CostView,
                itemType: ItemType.TradingCard,
                costItemId: itemIndex,
                cost: cost,
                title: "ID_TRD_1504",
                message: "ID_TRD_1532",
                confirmAction: () => RegisterCard(registerCardData).Forget()));
        }

        /// <summary>
        /// 선수 또는 감독의 카드 정보를 오픈.
        /// </summary>
        /// <param name="data"></param>
        private void OpenCardDetail(CardData data)
        {
            TradePlayerDetail.Open(new TradePlayerDetail.Param(data.CardParam, TradePlayerDetail.ButtonType.ConfirmAndClose,
                null, (outData) => { OpenTradeRegisterConfirmation(outData); }));
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

        private void SelectCardData(CardData data)
        {
            if (data.IsSelectedNum > 0)
            {
                data.IsSelectedNum = 0;
                selectedRegisterCardData = null;
            }
            else
            {
                data.IsSelectedNum = 1;
                selectedRegisterCardData = data;
            }
        }

        private async UniTask RegisterCard(CardData registerCardData)
        {
            var response = await APICommand.Trade.RegisterTrade(registerCardData.CardParam.CardInfoPlayer.CardInsId, curParam.slotIndex);
            if (response.commonResponse.Code != 0)
                DebugTool.LogError($"{response.commonResponse.Message}");


            await GlobalDataManager.Instance.GlobalUser.GetUserCardList(true);

            // 등록 수수료 소모
            GlobalDataManager.Instance.GlobalUser.UserInfo.AddItemInfomation(
                new ItemInformation((int)ItemType.TradingCard,
                response.registerTradeResponse.FeeCardId,
                response.registerTradeResponse.FeeCardNum * -1));
                        
            TradeTop.Open(new TradeTop.Param(TradeTop.TradeTopTab.ReceptionList));
        }
    }
}
