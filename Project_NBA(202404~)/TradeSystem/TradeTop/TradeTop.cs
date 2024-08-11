using Cysharp.Threading.Tasks;
using Dimps.Application.API;
using Dimps.Application.AssetBundle;
using Dimps.Application.Common;
using Dimps.Application.Common.Button;
using Dimps.Application.Common.UI;
using Dimps.Application.Flow;
using Dimps.Application.Global.UserData;
using Dimps.Application.Global;
using Dimps.Application.MasterData;
using Dimps.Application.MasterData.Master;
using Dimps.Application.Scene;
using Dimps.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;
using static Dimps.Application.Outgame.Season.SeasonData;
using static GVNC.Application.Trade.ProposeListScrollDataConatainer;
using Dimps.Application.MasterData.Types;
using Dimps.Application.Common.Constants;

namespace GVNC.Application.Trade
{
    public class TradeTop : SceneBaseFlow<TradeTop.Param>
    {
        public class Param : ISceneParameter
        {
            public TradeTopTab InitialTab;
            public Param(TradeTopTab topTab = TradeTopTab.ProposeAndRegister)
            {
                InitialTab = topTab;
            }
        }

        public enum TradeTopTab
        {
            ProposeAndRegister = 0,
            ProposeList = 1,
            ReceptionList = 2,
            History = 3,
            Max
        }

        [SerializeField] private UITabBar tradeTopTabBar = null;

        [SerializeField] private TradeProposeAndRegister proposeAndRegister;
        [SerializeField] private TradeProposeList tradeProposeList;
        [SerializeField] private TradeReceptionList tradeReceptionList;
        [SerializeField] private TradeHistory tradeHistory;

        private CancellationToken token;
        private AssetLoader assetLoader = new AssetLoader();

        private List<ProposeListScrollDataConatainer> proposedContainerList = new List<ProposeListScrollDataConatainer>();
        private List<TradeRegisterSlotDataContainer> registerContainerList = new List<TradeRegisterSlotDataContainer>();
        private List<TradeHistoryScrollDataContainer> historyContainerList = new List<TradeHistoryScrollDataContainer>();

        private int userBaseLevel = 0;

        public bool RefreshProposeListLock = false;

        public static void Open(Param param)
        {
            SceneProvider.Instance.RegressionClear();
            SceneProvider.Instance.ChangeScene(SceneContent.Type.TradeTop, param);            
        }

        protected override Param OnRootStart()
        {
            var param = new Param();
            return param;
        }

        protected override void OnInitialize(Param param)
        {
            RefreshProposeListLock = false;

            tradeTopTabBar.SelectTabEvent = (index) => InitTabButton(index);
        }

        protected override async UniTask OnLoad(CancellationToken token)
        {
            await UniTask.Yield();

            await InitializeProposeData();

            await InitializeRegisterData();

            InitializeHistoryData().Forget();
        }

        protected override async UniTask OnActivate(CancellationToken token)
        {
            //에디터용 임시 로그인
            await APIUtility.EditorSimpleLogin();

            this.token = token;

            await UniTask.Yield(cancellationToken: token);

            General.Instance.Header.Show();
            General.Instance.Footer.Show();

            SceneProvider.Instance.SetRegressionParam(new SceneProvider.RegressionParam()
            {
                scene = SceneType,
                param = SceneParameter
            });

            PreLoadProposeResources().Forget();

            PreLoadHistoryResources().Forget();
        }

        protected override void OnDispose()
        {            
            assetLoader?.Dispose();
            tradeProposeList.Dispose();
            tradeHistory.Dispose();
            assetLoader = null;
        }

        protected override void OnFlowEnd(IFlowEndParameter param)
        {

        }

        protected override void OnExecute(Param param)
        {
            InitTabButton((int)param.InitialTab);
        }

        protected override void OnEscape()
        {
            SceneProvider.Instance.RegressionClear();
        }


        private void InitTabButton(int index)
        {
            tradeTopTabBar.Init(index);
            proposeAndRegister.gameObject.SetActive(index == (int)TradeTopTab.ProposeAndRegister);
            tradeProposeList.gameObject.SetActive(index == (int)TradeTopTab.ProposeList);
            tradeReceptionList.gameObject.SetActive(index == (int)TradeTopTab.ReceptionList);
            tradeHistory.gameObject.SetActive(index == (int)TradeTopTab.History);

            switch((TradeTopTab)index)
            {
                case TradeTopTab.ProposeAndRegister:
                    proposeAndRegister.Init(registerContainerList, userBaseLevel);
                    break;
                case TradeTopTab.ProposeList:
                    tradeProposeList.Init(proposedContainerList, assetLoader, token);
                    break;
                case TradeTopTab.ReceptionList:
                    tradeReceptionList.Init(registerContainerList, assetLoader, token);
                    break;
                case TradeTopTab.History:
                    tradeHistory.Init(historyContainerList, assetLoader, token);
                    break;

                default:
                    break;
            }
        }

        public async UniTask InitializeProposeData()
        {                        
            proposedContainerList = new List<ProposeListScrollDataConatainer>();

            var response = await APICommand.Trade.GetMyProposedTradeInfoResponse();
            if (response.commonResponse.Code != 0)
                DebugTool.LogError($"{response.commonResponse.Message}");

            foreach (var myProposed in response.getTradeListResponse.TradeOfferInfo)
            {
                CardData proposedCardData = await GlobalFunction.GetCardDataFromCardInsid(myProposed.TradeOffer.CardInstanceId, CardData.CardViewType.TradeInfo);

                CardData rCard = new CardData(0, myProposed.Trade.CardId, (Rarity)myProposed.Trade.Rarity);
                rCard.ViewType = CardData.CardViewType.TradeInfo;
                rCard.CardParam.ForceSetupInsid(myProposed.Trade.CardInstanceId);

                MyProposedTradeInfo info = new MyProposedTradeInfo(myProposed.Trade.TradeId, myProposed.TradeOffer.OfferId, proposedCardData, rCard, GlobalFunction.GetLocalTime(myProposed.TradeOffer.OfferTime));

                proposedContainerList.Add(new ProposeListScrollDataConatainer(info, (TradeProposeState)myProposed.State));
            }
        }

        public async UniTask InitializeRegisterData()
        {
            #region Set User Max Register Slot
            UserInformation userInformation = await GlobalDataManager.Instance.GlobalUser.GetBaseInfo();
            var res = await APICommand.Profile.GetProfile(userInformation.UserId);
            if (res.commonResponse.Code != 0) 
                DebugTool.LogError($"{res.commonResponse.Message}");

            userBaseLevel = Math.Clamp(res.getUserProfileResponse.UserPublicDetail.BaseGrade, 1, 5);

            int unlockSlotCount = 0;
            if (MasterDataManager.Instance.BaseTradeSlotMaster.TryGetFacilityTradeSlotEntity(userBaseLevel, out TradeBaseEntity entity))
            {
                unlockSlotCount = entity.SlotCount;            
            }
            int MaxTradeSlot = MasterDataManager.Instance.BaseTradeSlotMaster.GetMaxFacilityTradeSlotCount();
            #endregion
            
            List<TradeRegisterSlotDataContainer> _tmpDataList = new List<TradeRegisterSlotDataContainer>();
            for (int i = 0; i < MaxTradeSlot; i++)
            {
                _tmpDataList.Add(new TradeRegisterSlotDataContainer());
                if (unlockSlotCount <= i)
                {
                    _tmpDataList[i].status = TradeRegister.RegisterSlotStatus.Lock;
                }
                else
                {
                    _tmpDataList[i].status = TradeRegister.RegisterSlotStatus.Registerable;
                }
            }

            await PushNPCOfferData();

            // API 요청으로 본인이 등록한 리스트를 가져옴
            var response = await APICommand.Trade.GetMyRegisteredTradeInfoResponse();
            if (response.commonResponse.Code != 0)
                DebugTool.LogError($"{response.commonResponse.Message}");

            int slotIndex = 0;
            foreach (var info in response.getTradeListResponse.TradeInfo)
            {
                CardData registeredCardData = await GlobalFunction.GetCardDataFromCardInsid(info.Trade.CardInstanceId, CardData.CardViewType.TradeInfo);

                List<OtherPlayerProposedTradeInfo> proposedTradeInfoList = new List<OtherPlayerProposedTradeInfo>();
                foreach (var proposed in info.TradeOffer)
                {
                    CardData pCard = new CardData(0, proposed.CardId, (Rarity)proposed.Rarity);
                    pCard.ViewType = CardData.CardViewType.TradeInfo;
                    pCard.CardParam.ForceSetupInsid(proposed.CardInstanceId);

                    OtherPlayerProposedTradeInfo proposedInfo = new OtherPlayerProposedTradeInfo(info.Trade.TradeId, proposed.OfferId, pCard, GlobalFunction.GetLocalTime(proposed.OfferTime));

                    proposedTradeInfoList.Add(proposedInfo);
                }
                    
                _tmpDataList[slotIndex].SetData(info.Trade.TradeId, registeredCardData, proposedTradeInfoList, GlobalFunction.GetLocalTime(info.Trade.RegisterTime));
                _tmpDataList[slotIndex].status = TradeRegister.RegisterSlotStatus.Registered;

                slotIndex++;
            }
                        
            registerContainerList = _tmpDataList;
        }

        private async UniTask InitializeHistoryData()
        {
            historyContainerList.Clear();

            var response = await APICommand.Trade.GetTradeHistory();
            if (response.commonResponse.Code != 0) 
                DebugTool.LogError($"{response.commonResponse.Message}");

            foreach (var history in response.tradeHistoryResponse.TradeHistoryList)
            {
                CardData register = new CardData(0, history.TradeCardId, (Rarity)history.TradeCardRarity);
                register.ViewType = CardData.CardViewType.TradeInfo;

                CardData offer = new CardData(0, history.OfferCardId, (Rarity)history.OfferCardRarity);
                offer.ViewType = CardData.CardViewType.TradeInfo;
                                
                if (history.ImOffer == 0)
                {
                    TradeHistoryScrollDataContainer container = new TradeHistoryScrollDataContainer(register, offer, history.TradeId, GlobalFunction.GetLocalTime(history.TradeTime));
                    historyContainerList.Add(container);
                }
                else
                {
                    TradeHistoryScrollDataContainer container = new TradeHistoryScrollDataContainer(offer, register, history.TradeId, GlobalFunction.GetLocalTime(history.TradeTime));
                    historyContainerList.Add(container);
                }                
            }
        }

        private async UniTask PushNPCOfferData()
        {
            // API 요청으로 본인이 등록한 리스트를 가져옴
            var response = await APICommand.Trade.GetMyRegisteredTradeInfoResponse();
            if (response.commonResponse.Code != 0)
                DebugTool.LogError($"{response.commonResponse.Message}");

            RandomGenerator rand = new RandomGenerator(System.DateTime.Now.Millisecond);

            foreach (var info in response.getTradeListResponse.TradeInfo)
            {
                // 오퍼가 이미 들어온 상태라면 패스
                if (info.TradeOffer.Count > 0)
                {
                    continue;
                }

                // 오퍼 요청 시간이 아직이라면 패스
                if (info.LastNpcOfferTime == 0)
                {                    
                    if (GlobalFunction.GetLocalTime(info.Trade.RegisterTime).AddHours(OutGameConstatns.TradeNPCOfferPushCoolTime) > System.DateTime.Now)
                    {
                        continue;
                    }
                }
                else
                {                    
                    if (GlobalFunction.GetLocalTime(info.LastNpcOfferTime).AddHours(OutGameConstatns.TradeNPCOfferPushCoolTime) > System.DateTime.Now)
                    {
                        continue;
                    }
                }

                Rarity targetRarity = (Rarity)info.Trade.Rarity;
                TradeFeeEntity entity = null;
                if (MasterDataManager.Instance.TradeFeeMaster.TryGetTradeFeeEntity(targetRarity, out entity) == false)
                {
                    continue;
                }


                if (rand.Next(0, 100) < entity.TradeSuccessRate)
                {
                    // 요청으로 받을 수 있는 카드를 하나 고름
                    CardData targetNPCCard = PickRandomNPCData(targetRarity);

                    // 서버에 해당 카드로 NPC가 오퍼를 넣도록 요청
                    var NPCOfferResponse = await APICommand.Trade.RegisterNPCTradeOffer(info.Trade.TradeId, targetNPCCard.CardParam.CardId);
                    if (NPCOfferResponse.commonResponse.Code != 0)
                        DebugTool.LogError($"{NPCOfferResponse.commonResponse.Message}");
                }
                else
                {                    
                    // NPC 오퍼 시간 갱신
                    var resetLastReqTime = await APICommand.Trade.ResetNpcLastOfferTime(info.Trade.TradeId);
                    if (resetLastReqTime.commonResponse.Code != 0)
                        DebugTool.LogError($"{resetLastReqTime.commonResponse.Message}");
                }
            }
        }

        private CardData PickRandomNPCData(Rarity targetRarity)
        {
            var dictionary = MasterDataManager.Instance.PlayerCardMaster.GetPlayerCardDispListExceptTradeBlock();
            List<CardData> list = new List<CardData>();
            
            foreach (var pair in dictionary)
            {
                if (pair.Value.Rarity == Rarity.Legendary && targetRarity != Rarity.Legendary)
                {
                    continue;
                }

                if (pair.Value.Rarity == Rarity.Legendary)
                {
                    CardData card = new CardData(0, dictionary[pair.Key].CardId, Rarity.Legendary);
                    card.ViewType = CardData.CardViewType.TradeInfo;
                    list.Add(card);
                }
                // 더 높은 등급으로는 신청 X
                else if (pair.Value.Rarity > targetRarity)
                {
                    continue;
                }
                else
                {
                    CardData card = new CardData(0, dictionary[pair.Key].CardId, targetRarity);
                    card.ViewType = CardData.CardViewType.TradeInfo;
                    list.Add(card);
                }                
            }

            RandomGenerator rand = new RandomGenerator(System.DateTime.Now.Millisecond);

            return list[rand.Next(0, list.Count)];            
        }

        // 미리 로딩하여 탭 전환시 프레임 드랍을 방지
        private async UniTask PreLoadProposeResources()
        {
            foreach (ProposeListScrollDataConatainer container in proposedContainerList)
            {
                foreach (CardData cardData in container.CardDataList)
                {
                    // 로딩이 끝나기 전에 Dispose 된 경우
                    if (assetLoader == null)
                        break;

                    AsyncOperationHandle handle = cardData.LoadSprite(assetLoader, true);
                    await handle.Task;

                    if (handle.IsValid() == false)
                        break;

                    cardData.LoadSuccess(handle);
                }
            }
        }

        // 미리 로딩하여 탭 전환시 프레임 드랍을 방지
        private async UniTask PreLoadHistoryResources()
        {
            foreach (TradeHistoryScrollDataContainer container in historyContainerList)
            {
                foreach (CardData cardData in container.CardDataList)
                {
                    // 로딩이 끝나기 전에 Dispose 된 경우
                    if (assetLoader == null)                    
                        break;                    

                    AsyncOperationHandle handle = cardData.LoadSprite(assetLoader, true);
                    await handle.Task;

                    if (handle.IsValid() == false)
                        break;

                    cardData.LoadSuccess(handle);
                }
            }
        }

        public async UniTask RefreshReceptionList()
        {
            await InitializeRegisterData();

            InitTabButton((int)TradeTopTab.ReceptionList);
        }

        public async UniTask RefreshProposeList()
        {
            RefreshProposeListLock = true;

            await InitializeProposeData();

            RefreshProposeListLock = false;

            InitTabButton((int)TradeTopTab.ProposeList);
        }
    }
}
