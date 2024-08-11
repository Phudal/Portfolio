using Cysharp.Threading.Tasks;
using Dimps.Application.Card;
using Dimps.Application.Deck;
using Dimps.Application.Scene;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Dimps.Application.Common.Button;
using Dimps.Utility;
using System;
using Dimps.Application.API;
using Dimps.Application.AssetBundle;
using UnityEngine.ResourceManagement.AsyncOperations;
using Dimps.Application.Global;
using Dimps.Application.MasterData;
using Dimps.Application.Global.UserData;
using Dimps.Application.Common.UI;
using GVNC.Application.Common.Help;
using Dimps.Application.Common;
using Dimps.Application.Common.Dialog;


namespace GVNC.Application.Trade
{
    public class TradePlayerDetail : SceneBase<TradePlayerDetail.Param>
    {
        public enum TabType
        {
            Data = 0,
            Skill = 1,
            Desc = 2,
            Max
        }

        public enum ButtonType
        {
            ConfirmAndClose = 0,
            Close = 1
        }

        public class Param : ISceneParameter
        {
            public ICommonAsset asset = null;
            public AssetLoader assetLoader = null;
            public CardParamPlayer cardPlayer = null;
            public bool enabledTraining = false;
            public Action<bool> close = null;
            public Action<CardData> confirm = null;
            public bool isPossessionCard = true;

            public ButtonType buttonType = ButtonType.ConfirmAndClose;

            public List<CardData> displayCardDataList = new List<CardData>();

            public Param(CardParamPlayer cardPlayer, ButtonType buttonType, Action<bool> close = null, Action<CardData> confirm = null)
            {
                this.cardPlayer = cardPlayer;
                this.asset = null;
                this.enabledTraining = false;
                this.close = close;
                this.isPossessionCard = false;

                this.buttonType = buttonType;

                this.confirm = confirm;
            }
        }

        //----------------------------------------------------------------------------------------------------
        // Static.
        //----------------------------------------------------------------------------------------------------

        [SerializeField, Header("좌우 스크롤")] private TradePlayerDetailCarouselView playerDetailCarouselView = default;
        [SerializeField] private GameObject gameObject_tradeProposeButton = null;        
        [SerializeField] private UICustomButton tradeProposeButton = null;        
        [SerializeField, Header("닫기 버튼")] private UICustomButton closeButton = null;
        [SerializeField, Header("왼쪽 버튼")] private UICustomButton leftButton = null;
        [SerializeField, Header("오른쪽 버튼")] private UICustomButton rightButton = null;

        //----------------------------------------------------------------------------------------------------
        // member.
        //----------------------------------------------------------------------------------------------------

        private AssetLoader assetLoader = null;
        private AssetLoader sceneAssetLoader = null;
        private AssetLoader viewCardAssetLoader = null; 
        private UICustomButton cardIconButton = null;
        private Action<bool> close = null;
        private CardParamPlayer cardPlayer = null;
        private bool isUpdated = false;
        private bool isPossessionCard = false;
        private UserCardList userCardList = null;

        private Param curParam;

#if DEV_BUILD
        private bool isIngameDebug = false;
#endif        

        private TabType curTabType = TabType.Data;

        private List<CardData> displayCardList = new List<CardData>();

        private List<Action<int>> tabButtonCallback = new List<Action<int>>();

        //----------------------------------------------------------------------------------------------------
        // Static.
        //----------------------------------------------------------------------------------------------------

        public static void Open(Param param)
        {
            // ダイアログシーンを開く.
            SceneProvider.Instance.AddScene(SceneContent.Type.TradePlayerDetail, param);
        }

        //----------------------------------------------------------------------------------------------------
        // SceneBaseFlow.
        //----------------------------------------------------------------------------------------------------
        
        protected override Param OnRootStart()
        {
            var param = new Param(null, ButtonType.ConfirmAndClose);
            return param;
        }

        /// <summary>
        /// シーン初期化
        /// </summary>
        /// <param name="param">シーンパラメータ</param>
        protected override void OnInitialize(Param param)
        {
            curParam = param;

            cardPlayer = param.cardPlayer;
            close = param.close;

            isPossessionCard = param.isPossessionCard && !cardPlayer.IsGrownParam;

            viewCardAssetLoader = new AssetLoader();
            if (param.asset == null && param.assetLoader == null)
            {
                // paramにAssetLoaderがないならシーン用のAssetLoaderを作成
                sceneAssetLoader = new AssetLoader();
                assetLoader = sceneAssetLoader;
            }
            else
            {
                if (param.asset != null)
                {
                    // CommonAssetLoaderがnullでないならそれが持つAssetLoaderを利用する
                    assetLoader = param.asset.AssetLoader;
                }
                else
                {
                    assetLoader = param.assetLoader;
                }
            }

            DebugTool.Log($"cardId:{cardPlayer.CardId}, cardInsId:{cardPlayer.CardInfoPlayer.CardInsId}");

            tabButtonCallback = new List<Action<int>>();

#if DEV_BUILD
            // デバッグ機能：インゲームデバッグ中なのかを判定
            isIngameDebug = false;
            Dimps.Development.IngameDebug.IngameDebugFunction.GetDebugEnable(ref isIngameDebug);
#endif
        }

        public void SetupPlayerDetailPageData(Param param)
        {
            List<PlayerDetailPageData> cellDataList = new List<PlayerDetailPageData>();

            if (param.displayCardDataList == null || param.displayCardDataList.Count < 1)
            {
                param.displayCardDataList = new List<CardData>();
                CardData cardData = new CardData(param.cardPlayer.Index, param.cardPlayer);
                // 현재 insid 등을 전달해주기 위해 복사
                cardData.CopyCardParam(param.cardPlayer);
                param.displayCardDataList.Add(cardData);
            }

            displayCardList = param.displayCardDataList;

            int targetPos = 0;
            int tmpPos = 0;
            foreach (CardData data in param.displayCardDataList)
            {
                cellDataList.Add(new PlayerDetailPageData(data.CardParam, null, true, true));

                if (data.CardParam.CardId == param.cardPlayer.CardId &&
                    data.CardParam.CardInfoPlayer.CardInsId == param.cardPlayer.CardInfoPlayer.CardInsId)
                {
                    targetPos = tmpPos;
                }
                tmpPos++;
            }

            playerDetailCarouselView.Setup(cellDataList);

            playerDetailCarouselView.ScrollTo(targetPos, playerDetailCarouselView.SnapAnimationDuration, playerDetailCarouselView.SnapAnimationType);

            OnScrollChangeComplete(targetPos);
        }

        public void RefreshCardData(PlayerDetailPageData param)
        {
            cardPlayer = param.cardPlayer;

            isPossessionCard = param.isPossessionCard && !cardPlayer.IsGrownParam;

            if (curParam.buttonType == ButtonType.ConfirmAndClose)
            {
                gameObject_tradeProposeButton.SetActive(true);
                tradeProposeButton.onClick.RemoveAllListeners();
                tradeProposeButton.onClick.AddListener(OnClicked_ProposeBtn);
            }
            else if (curParam.buttonType == ButtonType.Close)
            {
                gameObject_tradeProposeButton.SetActive(false);
            }            

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => OnClose());

            DebugTool.Log($"cardId:{cardPlayer.CardId}, cardInsId:{cardPlayer.CardInfoPlayer.CardInsId}");

#if DEV_BUILD
            // デバッグ機能：インゲームデバッグ中なのかを判定
            isIngameDebug = false;
            Dimps.Development.IngameDebug.IngameDebugFunction.GetDebugEnable(ref isIngameDebug);
#endif
        }

        /// <summary>
        /// シーン読み込み時
        /// </summary>
        /// <param name="token">キャンセルトークン</param>
        /// <returns></returns>
        protected override async UniTask OnLoad(CancellationToken token)
        {
            AsyncOperationHandle handle = assetLoader.LoadPlayerCardSmallAsync(
                cardPlayer.CardParamEntity.PlayerPicNo, cardPlayer.CurrentRarity
            );
            await handle.Task;
            var logBook = await GlobalDataManager.Instance.GlobalUser.GetUserCardLogbook();
            bool isSample = true;
            if (logBook.UserLogbook.TryGetValue(cardPlayer.CardId, out var rarities))
            {
                isSample = !rarities.Contains(cardPlayer.CurrentRarity);
            }

            userCardList = await GlobalDataManager.Instance.GlobalUser.GetUserCardList();

            SetupPlayerDetailPageData(curParam);
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
            // カード閲覧シーン用の画像を読み込んでおく
            var _ = viewCardAssetLoader.LoadPlayerCardAsync(cardPlayer.CardParamEntity.PlayerPicNo, cardPlayer.CurrentRarity);

            SetupScrollButton();

            playerDetailCarouselView.ActiveCellChanged += OnScrollChangeComplete;
        }

        /// <summary>
        /// シーン破棄
        /// <para>MonoBehaviour.OnDestroy時に呼ばれます</para>
        /// </summary>
        protected override void OnDispose()
        {
            if (sceneAssetLoader != null)
            {
                sceneAssetLoader.Dispose();
                sceneAssetLoader = null;
            }
            viewCardAssetLoader.Dispose();
            viewCardAssetLoader = null;
        }

         
        /// <summary>
        /// 閉じる.
        /// </summary>
        // public async UniTask OnClose()
        public void OnClose()
        {
            // LockState 저장은 PlayerDetailCarouselCell로 이관
            // using이 이관되었으므로, void로 교체 

            close?.Invoke(isUpdated);    // カード更新状態を呼び出し元に伝える
            CloseDialog();
        }

        private void OnClicked_ProposeBtn()
        {
            CardData cardData = new CardData(0, curParam.cardPlayer, CardData.CardViewType.TradeInfo);
            cardData.CardParam.ForceSetupInsid(curParam.cardPlayer.CardInfoPlayer.CardInsId);
            curParam.confirm?.Invoke(cardData);
        }

        public AssetLoader GetPlayerDetailAssetLoader()
        {
            return assetLoader;
        }

        private void SetupScrollButton()
        {
            rightButton.onClick.RemoveAllListeners();
            rightButton.onClick.AddListener(() => { OnRightScroll(); });
            leftButton.onClick.RemoveAllListeners();
            leftButton.onClick.AddListener(() => { OnLeftScroll(); });
        }

        private void OnScrollChangeComplete(int index)
        {
            leftButton.gameObject.SetActive(index > 0);
            rightButton.gameObject.SetActive(index < (displayCardList.Count - 1));
        }

        public void OnLeftScroll()
        {
            if (playerDetailCarouselView.IsScrolling)
            {
                return;
            }
            playerDetailCarouselView.ScrollToBefore(playerDetailCarouselView.SnapAnimationDuration, playerDetailCarouselView.SnapAnimationType);
        }

        public void OnRightScroll()
        {
            if (playerDetailCarouselView.IsScrolling)
            {
                return;
            }
            playerDetailCarouselView.ScrollToAfter(playerDetailCarouselView.SnapAnimationDuration, playerDetailCarouselView.SnapAnimationType);
        }

        public void OnScrollTo(int target)
        {
            if (playerDetailCarouselView.IsScrolling)
            {
                return;
            }
            playerDetailCarouselView.ScrollTo(target, playerDetailCarouselView.SnapAnimationDuration, playerDetailCarouselView.SnapAnimationType);
        }

        public List<CardData> GetDisplayCardDataList()
        {
            return displayCardList;
        }

        public void OnLockToggleClicked()
        {
            isUpdated = true;
        }

        public TabType GetCurTabType()
        {
            return curTabType;
        }

        public void BroadcastCurTabType(TabType tabType)
        {
            curTabType = tabType;

            foreach (Action<int> action in tabButtonCallback)
            {
                action.Invoke((int)tabType);
            }
        }

        public void AddTabButtonCallback(Action<int> action)
        {
            if (tabButtonCallback.Contains(action) == true)
                return;

            tabButtonCallback.Add(action);
        }

        public void RemoveTabButtonCallback(Action<int> action)
        {
            if (tabButtonCallback.Contains(action) == false)
                return;

            tabButtonCallback.Remove(action);
        }
    }
}
