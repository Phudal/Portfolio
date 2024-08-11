using Cysharp.Threading.Tasks;
using Dimps.Application.Flow;
using Dimps.Application.Scene;
using System.Collections.Generic;
using System.Threading;
using Dimps.Application.Common;
using UnityEngine;
using Dimps.Application.Common.UI;
using Dimps.Utility;
using Dimps.Application.Card;
using Dimps.Application.Common.Button;
using Dimps.Application.Outgame.Storage.Dialog;
using System.Linq;
using TMPro;
using Dimps.Application.Common.Dialog;
using Dimps.Application.Outgame.Dialog;
using Dimps.Application.MasterData.Types;
using Dimps.Application.Outgame.Team;
using System.Data;
using System;
using Dimps.Application.AssetBundle;
using Dimps.Application.API;
using Dimps.Application.MasterData;
using Dimps.Application.Global;
using UnityEngine.UI;
using Dimps.Application.Global.UserData;
using Dimps.Application.Outgame.Storage;

namespace GVNC.Application.Trade
{
    public class TradeProposeSelect : SceneBaseFlow<TradeProposeSelect.Param>, ICardListGridScrollContext
    {
        public class Param : ISceneParameter
        {
            public long trade_id;

            public bool isNPCData;

            public CardData selectCardData;

            public Param(long trade_id, bool isNPCData, CardData selectCardData)
            {
                this.trade_id = trade_id;
                this.isNPCData = isNPCData;
                this.selectCardData = selectCardData;
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

        private CardData selectedGiveCardData = null;

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
            SceneProvider.Instance.ChangeScene(SceneContent.Type.TradeProposeSelect, param);
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
            var param = new Param(default, false, null);
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

                // 트레이드하고자 하는 카드보다 더 높거나 같은 등급으로만 신청 가능
                if (GlobalFunction.CompareRarity((Rarity)data.CurrentRarity, curParam.selectCardData.CardParam.CurrentRarity) < 0)
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

        /// <summary>
        /// ソートフィルタメニューの起動
        /// </summary>
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
            TradeProposeConfirmation.Open(new TradeProposeConfirmation.Param(curParam.trade_id, curParam.isNPCData, selectedGiveCardData, curParam.selectCardData));
        }

        /// <summary>
        /// 선수 또는 감독의 카드 정보를 오픈.
        /// </summary>
        /// <param name="data"></param>
        private void OpenCardDetail(CardData data)
        {
            PlayerDetail.Open(new PlayerDetail.Param(data.CardParam, null, null, false));

            // TradePlayerDetail.Open(new TradePlayerDetail.Param(data.CardParam, null, null, false));            
        }

        public void Select(CardData data)
        {
            foreach(CardData card in viewCardList)
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

        private void OnClicked_BackButton()
        {
            if (SceneProvider.Instance.RegressionCurrentScene == SceneContent.Type.Application)
            {
                TradeTop.Open(new TradeTop.Param());
                return;
            }

            SceneProvider.Instance.RegressionScene();
        }

        private void SelectCardData(CardData data)
        {
            if (data.IsSelectedNum > 0)
            {
                data.IsSelectedNum = 0;
                selectedGiveCardData = null;
            }
            else
            {
                data.IsSelectedNum = 1;
                selectedGiveCardData = data;
            }
        }
    }
}