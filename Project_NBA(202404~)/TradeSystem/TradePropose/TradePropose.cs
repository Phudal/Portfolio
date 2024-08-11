using Cysharp.Threading.Tasks;
using Dimps.Application.API;
using Dimps.Application.AssetBundle;
using Dimps.Application.Common;
using Dimps.Application.Common.Button;
using Dimps.Application.Common.Constants;
using Dimps.Application.Common.Dialog;
using Dimps.Application.Common.UI;
using Dimps.Application.Flow;
using Dimps.Application.Global;
using Dimps.Application.MasterData;
using Dimps.Application.MasterData.Types;
using Dimps.Application.Scene;
using Dimps.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

namespace GVNC.Application.Trade
{
    public class TradePropose : SceneBaseFlow<TradePropose.Param>, IMultiLoadGridScrollContext
    {
        public class Param : ISceneParameter
        {
            public CardListRule cardListRule = new CardListRule();

            public Param(CardListRule cardListRule)
            {
                this.cardListRule = cardListRule;
            }
        }

        [SerializeField] private SearchedTradeItemCardListController searchedTradeItemController;
        [SerializeField] private UICustomButton btn_Refresh;
        [SerializeField] private UICustomButton btn_Back;

        private AssetLoader assetLoader = new AssetLoader();
        private List<SearchedTradeItemData> searchedList = new List<SearchedTradeItemData>();

        private CardListRule cardListRule = new CardListRule();

        private CancellationToken token;

        private Param curParam;

        private UniTask handle;

        public static void Open(Param param)
        {
            SceneProvider.Instance.ChangeScene(SceneContent.Type.TradePropose, param);
        }

        protected override Param OnRootStart()
        {
            var param = new Param(null);
            return param;
        }

        protected override void OnInitialize(Param param)
        {
            curParam = param;

            cardListRule = param.cardListRule;

            btn_Refresh.onClick.RemoveAllListeners();
            btn_Refresh.onClick.AddListener(OnClicked_RefreshSearchedList);

            btn_Back.onClick.RemoveAllListeners();
            btn_Back.onClick.AddListener(OnClicked_BackButton);
        }

        protected override async UniTask OnLoad(CancellationToken token)
        {
            this.token = token;

            await SetFilteredSearch(FilteringCard(curParam.cardListRule));

            searchedTradeItemController.Setup(this, 1, searchedList);

            await searchedTradeItemController.LoadInitCard(searchedList, assetLoader);

            searchedTradeItemController.LoadCard(4, 2, assetLoader, token).Forget();
        }

        protected override async UniTask OnActivate(CancellationToken token)
        {
            //에디터용 임시 로그인
            await APIUtility.EditorSimpleLogin();
        }

        protected override void OnDispose()
        {
            searchedTradeItemController.Dispose();
            assetLoader.Dispose();
        }

        protected override void OnFlowEnd(IFlowEndParameter param)
        {
        }

        protected override void OnExecute(Param param)
        {

        }

        protected override void OnEscape()
        {

        }

        public void ScrollItemCallback_1(MultiLoadScrollDataConatainer data)
        {
            TradePlayerDetail.Open(new TradePlayerDetail.Param(data.CardDataList[0].CardParam, TradePlayerDetail.ButtonType.ConfirmAndClose, null, 
                (cardData) =>
            {
                SearchedTradeItemData searched = data as SearchedTradeItemData;
                OnClicked_Open_TradeProposeSelect(searched.tradeId, searched.isNPCData, cardData);
            }));
        }

        public void ScrollItemCallback_2(MultiLoadScrollDataConatainer data)
        {
            // TradePlayerDetail.Open(new TradePlayerDetail.Param(data.CardDataList[0].CardParam, TradePlayerDetail.ButtonType.ConfirmAndClose, null, OnClicked_Open_TradeProposeSelect));

            TradePlayerDetail.Open(new TradePlayerDetail.Param(data.CardDataList[0].CardParam, TradePlayerDetail.ButtonType.ConfirmAndClose, null,
                (cardData) =>
                {
                    SearchedTradeItemData searched = data as SearchedTradeItemData;
                    OnClicked_Open_TradeProposeSelect(searched.tradeId, searched.isNPCData, cardData);
                }));
        }

        public void ScrollItemCallback_3(MultiLoadScrollDataConatainer data)
        {
            
        }

        public void ScrollItemCallback_4(MultiLoadScrollDataConatainer data)
        {
            
        }
        

        private void OnClicked_BackButton()
        {
            if (SceneProvider.Instance.RegressionCurrentScene == SceneContent.Type.TradeTop ||
                SceneProvider.Instance.RegressionCurrentScene == SceneContent.Type.TradePropose ||
                SceneProvider.Instance.RegressionCurrentScene == SceneContent.Type.Application)
            {
                TradeTop.Open(new TradeTop.Param());
                return;
            }

            SceneProvider.Instance.RegressionScene();
        }

        private void OnClicked_Open_TradeProposeSelect(long trade_id, bool isNPCData, CardData cardData)
        {
            SceneProvider.Instance.SetRegressionParam(
                    new SceneProvider.RegressionParam()
                    {
                        scene = SceneType,
                        param = SceneParameter
                    }
                );

            TradeProposeSelect.Open(new TradeProposeSelect.Param(trade_id, isNPCData, cardData));
        }

        private void OnClicked_RefreshSearchedList()
        {
            if (GlobalDataManager.Instance.GlobalTime.CurrentTime - GlobalDataManager.Instance.GlobalTime.LastTradeRequestTime < OutGameConstatns.TradeSearchCoolTime)
            {
                int nextReqSec = 10 - (int)(GlobalDataManager.Instance.GlobalTime.CurrentTime - GlobalDataManager.Instance.GlobalTime.LastTradeRequestTime) / 1000;

                // 요청한지 얼마 안되서 잠시 후 시도하라고 해야함
                Dictionary<CommonDialog.SelectType, CommonDialog.ButtonData> tmpButtonMap = new Dictionary<CommonDialog.SelectType, CommonDialog.ButtonData>();
                tmpButtonMap.Add(CommonDialog.SelectType.Center, new CommonDialog.ButtonData(LanguageManager.Instance.GetOSTText("ID_SYS_1800"), null));
                CommonDialog.Open(new CommonDialog.Param(
                    CommonDialog.ButtonType.Single, LanguageManager.Instance.GetOSTText("ID_TRD_1500"),
                    string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1551"), nextReqSec), tmpButtonMap));

                return;
            }

            TradeFilterMenu.Open(new TradeFilterMenu.Param(cardListRule,
                "ID_TRD_1503",
                (rule) =>
                {                
                    handle = SetFilteredSearch(FilteringCard(rule));
                
                    RefreshSearchedList().Forget();
                }));           
        }

        private List<int> FilteringCard(CardListRule rule)
        {
            cardListRule = rule;

            List<CardData> list = new List<CardData>();

            var dictionary = MasterDataManager.Instance.PlayerCardMaster.GetPlayerCardDispListExceptTradeBlock();
            foreach (int id in dictionary.Keys)
            {
                CardData card = new CardData(0, dictionary[id].CardId, dictionary[id].Rarity);
                card.ViewType = CardData.CardViewType.TradeInfo;
                list.Add(card);
            }

            return CardListSortFilter.SortFilter(list, cardListRule).Select(v => v.CardParam.CardId).ToList();            
        }

        private async UniTask SetFilteredSearch(List<int> viewIndexCard)
        {
            // 필터링 조건에 만족하는 선수가 아예 없을 때
            if (viewIndexCard.Count < 1)
            {
                // 조건에 만족하는 선수가 존재하지 않는다고 팝업을 띄워야함
                Dictionary<CommonDialog.SelectType, CommonDialog.ButtonData> tmpButtonMap = new Dictionary<CommonDialog.SelectType, CommonDialog.ButtonData>();
                tmpButtonMap.Add(CommonDialog.SelectType.Center, new CommonDialog.ButtonData(LanguageManager.Instance.GetOSTText("ID_SYS_1800"), null));
                CommonDialog.Open(new CommonDialog.Param(
                    CommonDialog.ButtonType.Single, 
                    LanguageManager.Instance.GetOSTText("ID_TRD_1500"), 
                    LanguageManager.Instance.GetOSTText("ID_TRD_1552"), tmpButtonMap));

                return;
            }

            searchedList.Clear();

            GlobalDataManager.Instance.GlobalTime.LastTradeRequestTime = GlobalDataManager.Instance.GlobalTime.CurrentTime;

            List<int> targetList = new List<int>();

            // 조건을 만족하는 인덱스가 30개 이상일 경우, 30개를 뽑는다.
            if (viewIndexCard.Count > OutGameConstatns.TradeSearchThresold)
            {
                RandomGenerator rand = new RandomGenerator(System.DateTime.Now.Millisecond);

                int overThresold = 0;
                while (targetList.Count < OutGameConstatns.TradeSearchThresold - 1 && overThresold < OutGameConstatns.TradeSearchThresold)
                {
                    overThresold++;

                    int randomIndex = rand.Next(0, viewIndexCard.Count);

                    if (targetList.Contains(viewIndexCard[randomIndex]))
                        continue;

                    targetList.Add(viewIndexCard[randomIndex]);
                }
            }
            else
            {
                targetList = viewIndexCard;
            }

            var response = await APICommand.Trade.SearchTrade(targetList);
            if (response.commonResponse.Code != 0)
                DebugTool.LogError($"{response.commonResponse.Message}");

            foreach (var searchedItem in response.searchTradeResponse.Trade)
            {
                CardData card = new CardData(0, searchedItem.CardId, (Rarity)searchedItem.Rarity);
                card.CardParam.ForceSetupInsid(searchedItem.CardInstanceId);

                SearchedTradeItemData conatainer = new SearchedTradeItemData(card, searchedItem.TradeId, false);
                searchedList.Add(conatainer);
            }


            PushNPCTradingCard();
        }

        private void PushNPCTradingCard()
        {
            if (searchedList.Count >= OutGameConstatns.TradeSearchViewCount)
            {
                // 충분히 많기에 NPC 데이터를 채워줄 필요 없음
                return;
            }

            var dictionary = MasterDataManager.Instance.PlayerCardMaster.GetPlayerCardDispListExceptTradeBlock();
            List<CardData> list = new List<CardData>();

            foreach (int id in dictionary.Keys)
            {
                CardData card = new CardData(0, dictionary[id].CardId, dictionary[id].Rarity);
                card.ViewType = CardData.CardViewType.TradeInfo;
                list.Add(card);
            }

            List<CardData> ret = CardListSortFilter.Filter(list, cardListRule.FilterRules).ToList();
            List<CardData> selectdNPCData = new List<CardData>();

            RandomGenerator rand = new RandomGenerator(System.DateTime.Now.Millisecond);

            if (ret.Count + searchedList.Count > OutGameConstatns.TradeSearchViewCount)
            {
                int overThresold = 0;
                while (selectdNPCData.Count < OutGameConstatns.TradeSearchViewCount && overThresold < OutGameConstatns.TradeSearchThresold)
                {
                    overThresold++;

                    int randomIndex = rand.Next(0, ret.Count);

                    if (selectdNPCData.Contains(ret[randomIndex]))
                        continue;

                    selectdNPCData.Add(ret[randomIndex]);
                }
            }
            else
            {
                selectdNPCData = ret;
            }


            foreach (CardData npcData in selectdNPCData)
            {
                if (npcData.CardParam.InitialRarity == Rarity.Legendary)
                {
                    CardData card = new CardData(0, npcData.CardParam.CardId, Rarity.Legendary);
                    SearchedTradeItemData conatainer = new SearchedTradeItemData(card, -1, true);
                    searchedList.Add(conatainer);
                }
                else
                {
                    // 카드가 가질 수 있는 레어리티 중 랜덤 선택
                    int targetRarity = rand.Next((int)npcData.CardParam.InitialRarity, (int)Rarity.Epic + 1);

                    CardData card = new CardData(0, npcData.CardParam.CardId, (Rarity)targetRarity);
                    SearchedTradeItemData conatainer = new SearchedTradeItemData(card, -1, true);
                    searchedList.Add(conatainer);
                }
            }                            
        }

        private async UniTask RefreshSearchedList()
        {
            await handle;

            searchedTradeItemController.Setup(this, 1, searchedList);

            await searchedTradeItemController.LoadInitCard(searchedList, assetLoader);

            searchedTradeItemController.LoadCard(4, 2, assetLoader, token).Forget();
        }        
    }
}