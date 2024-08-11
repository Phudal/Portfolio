using Cysharp.Threading.Tasks;
using Dimps.Application.API;
using Dimps.Application.Common.UI;
using Dimps.Application.Flow;
using Dimps.Application.Global;
using Dimps.Application.Global.UserData;
using Dimps.Application.MasterData;
using Dimps.Application.Scene;
using Dimps.Utility;
using GVNC.Application.Trade;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Dimps.Application.MasterData.Master;
using Dimps.Application.AssetBundle;
using Dimps.Application.Common;
using Dimps.Application.Common.Button;
using Dimps.Application.Common.Dialog;
using Dimps.Application.MasterData.Types;
using Dimps.Application.Common.Constants;
using Dimps.Application.API.Message;

namespace GVNC.Application.Trade
{
    public class TradeRegister : SceneBaseFlow<TradeRegister.Param>
    {
        public class Param : ISceneParameter
        {
            public List<TradeRegisterSlotDataContainer> registerContainerList = new List<TradeRegisterSlotDataContainer>();

            public int userBaseLevel;

            public Param(List<TradeRegisterSlotDataContainer> registerContainerList = null, int userBaseLevel = 0)
            {
                this.registerContainerList = registerContainerList;

                this.userBaseLevel = userBaseLevel;
            }
        }

        public enum RegisterSlotStatus
        {
            Registerable = 0,
            Registered = 1,
            Lock = 2
        }

        [SerializeField] private RectTransform rect_scrollContent = null;

        [SerializeField, Header("Slot Types")] private TradeRegisterSlotTypeRegisterable slotTypeRegisterable;
        [SerializeField] private TradeRegisterSlotTypeRegistered slotTypeRegistered;
        [SerializeField] private TradeRegisterSlotTypeLock slotTypeLock;

        [SerializeField, Header("Buttons")] private UICustomButton customButton_back = null;

        List<TradeRegisterSlotDataContainer> registerContainerList = new List<TradeRegisterSlotDataContainer>();

        private Param curParam;

        private AssetLoader assetLoader = new AssetLoader();

        private int userBaseLevel = 0;

        private int MaxTradeSlot = 0;

        //----------------------------------------------------------------------------------------------------
        // Static.
        //----------------------------------------------------------------------------------------------------

        /// <summary>
        /// シーンを開く.
        /// </summary>
        /// <param name="param"></param>
        public static void Open(Param param)
        {
            SceneProvider.Instance.ChangeScene(SceneContent.Type.TradeRegister, param);
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
            var param = new Param();
            return param;
        }

        /// <summary>
        /// シーン初期化
        /// </summary>
        /// <param name="param">シーンパラメータ</param>
        protected override void OnInitialize(Param param)
        {
            curParam = param;

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

            if (curParam.userBaseLevel == 0)
            {
                UserInformation userInformation = await GlobalDataManager.Instance.GlobalUser.GetBaseInfo();
                var res = await APICommand.Profile.GetProfile(userInformation.UserId);
                if (res.commonResponse.Code != 0) DebugTool.LogError($"{res.commonResponse.Message}");
                userBaseLevel = Math.Clamp(res.getUserProfileResponse.UserPublicDetail.BaseGrade, 1, 5);
            }
            else
            {
                userBaseLevel = curParam.userBaseLevel;
            }

            if (curParam.registerContainerList == null)
            {
                await InitializeCurrentTradeSlotData();
            }            
            else
            {
                registerContainerList = curParam.registerContainerList;
            }


            MaxTradeSlot = MasterDataManager.Instance.BaseTradeSlotMaster.GetMaxFacilityTradeSlotCount();

            // InitializeCurrentTradeSlotData();

            SetupScroll();
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

        private async UniTask InitializeCurrentTradeSlotData()
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

            for (int i = 0; i < MaxTradeSlot; i++)
            {
                GameObject tmpOb = null;
                switch (registerContainerList[i].status)
                {
                    case RegisterSlotStatus.Registerable:
                        tmpOb = Instantiate(slotTypeRegisterable.gameObject, rect_scrollContent);
                        TradeRegisterSlotTypeRegisterable registerable = tmpOb.GetComponent<TradeRegisterSlotTypeRegisterable>();
                        registerable.Initialize(i, OnClicked_RegisterNew);
                        break;

                    case RegisterSlotStatus.Registered:
                        tmpOb = Instantiate(slotTypeRegistered.gameObject, rect_scrollContent);
                        TradeRegisterSlotTypeRegistered registered = tmpOb.GetComponent<TradeRegisterSlotTypeRegistered>();
                        registered.Initialize(i, registerContainerList[i], assetLoader, OnClicked_ViewDetail, OnClicked_CancelRegister);
                        break;

                    case RegisterSlotStatus.Lock:
                        tmpOb = Instantiate(slotTypeLock.gameObject, rect_scrollContent);
                        TradeRegisterSlotTypeLock slotLock = tmpOb.GetComponent<TradeRegisterSlotTypeLock>();
                        slotLock.Initialize(i);
                        break;

                    default:
                        break;
                }
                tmpOb.transform.localPosition = Vector3.zero;
                tmpOb.transform.localScale = Vector3.one;
                tmpOb.SetActive(true);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(rect_scrollContent);
        }

        private void OnClicked_ViewDetail(int slotIndex)
        {           
            TradeRegisterStatus.Open(new TradeRegisterStatus.Param(
                registerContainerList[slotIndex].trade_id, 
                registerContainerList[slotIndex].registeredCardData, 
                registerContainerList[slotIndex].proposedCardList,
                registerContainerList[slotIndex].registerDT));
        }

        private void OnClicked_CancelRegister(int slotIndex)
        {
            int cost = 0;
            if (MasterDataManager.Instance.TradeFeeMaster.TryGetTradeFeeEntity(registerContainerList[slotIndex].registeredCardData.CardParam.CurrentRarity, out TradeFeeEntity entity))
            {
                cost = entity.BaseFee + entity.UpgradeLevelFee * registerContainerList[slotIndex].registeredCardData.CardParam.UpgradeLevel;
            }

            int tradingCardIndex = MasterDataManager.Instance.ItemMaster.GetTradingCardIndexByRarity(registerContainerList[slotIndex].registeredCardData.CardParam.CurrentRarity);

            TradeConfirmDialog.Open(new TradeConfirmDialog.Param(TradeConfirmDialog.DialogType.ReverseCostView,
                ItemType.TradingCard, tradingCardIndex, cost,
                "ID_TRD_1527",
                "ID_TRD_1528", 
                () =>
                {
                    var _ = CancelRegister(slotIndex);
                }
                ));
        }

        private void OnClicked_RegisterNew(int slotIndex)
        {
            TradeRegisterSelect.Open(new TradeRegisterSelect.Param(slotIndex));
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

        private async UniTask CancelRegister(int slotIndex)
        {
            var response = await APICommand.Trade.CancelRegisterTrade(registerContainerList[slotIndex].trade_id);
            if (response.commonResponse.Code != 0)
                DebugTool.LogError($"{response.commonResponse.Message}");

            await GlobalDataManager.Instance.GlobalUser.GetUserCardList(true);

            // 등록 수수료 반환
            GlobalDataManager.Instance.GlobalUser.UserInfo.AddItemInfomation(
                new ItemInformation((int)ItemType.TradingCard,
                response.cancelRegisterTradeResponse.FeeCardId,
                response.cancelRegisterTradeResponse.FeeCardNum));

            // Refresh
            await InitializeCurrentTradeSlotData();

            SetupScroll();
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
    }

    public class TradeRegisterSlotDataContainer
    {
        public long trade_id;

        public TradeRegister.RegisterSlotStatus status;

        public CardData registeredCardData;

        public List<OtherPlayerProposedTradeInfo> proposedCardList = new List<OtherPlayerProposedTradeInfo>();

        public DateTime registerDT;

        public int proposedCount;

        public void SetData(long trade_id, CardData registeredCardData, List<OtherPlayerProposedTradeInfo> proposedCardList, DateTime registerDT)
        {
            this.trade_id = trade_id;
            this.registeredCardData = registeredCardData;
            this.proposedCardList = proposedCardList;
            this.registerDT = registerDT;
            this.proposedCount = proposedCardList.Count;
        }                
    }
}