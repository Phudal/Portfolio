using Dimps.Application.Common.Button;
using Dimps.Application.Common.Card;
using Dimps.Application.Common.Dialog;
using Dimps.Application.Common;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Dimps.Application.AssetBundle;
using System;
using Cysharp.Threading.Tasks;
using Dimps.Application.Card;
using Dimps.Application.Deck;
using Dimps.Application.Global;
using Dimps.Application.Global.UserData;
using FancyCarouselView.Runtime.Scripts;
using System.Threading;
using UnityEngine.ResourceManagement.AsyncOperations;
using Dimps.Application.API;
using Dimps.Utility;

namespace GVNC.Application.Trade
{
    public class TradePlayerDetailCarouselCell : CarouselCell<PlayerDetailPageData, TradePlayerDetailCarouselCell>
    {
        [SerializeField, Header("Player Detail")] private TradePlayerDetail playerDetail = null;

        [SerializeField, Header("タブ")] private UITabBar tabBar = null;
        [SerializeField, Header("名前")] private TMP_Text nameLabel = null;
        [SerializeField, Header("シーズン")] private TMP_Text dateLabel = null;
        [SerializeField, Header("プレイヤーアイコン")] private UICardIcon playerIcon = null;

        [SerializeField, Header("データキャンバス")] private CanvasGroup dataCanvas = null;
        [SerializeField, Header("データ タブ")] private PlayerDetailData dataTab = null;

        [SerializeField, Header("スキルキャンバス")] private CanvasGroup skillCanvas = null;
        [SerializeField, Header("スキル タブ")] private PlayerDetailSkill skillTab = null;

        [SerializeField, Header("情報キャンバス")] private CanvasGroup infoCanvas = null;
        [SerializeField, Header("情報 タブ")] private PlayerDetailInfo infoTab = null;
        
        [SerializeField, Header("最大Param注釈")] private TextMeshProUGUI attentionText = null;


        private AssetLoader assetLoader = null;
        private AssetLoader sceneAssetLoader = null;
        private AssetLoader viewCardAssetLoader = null; // カード閲覧シーン用のAssetLoader
        private UICustomButton cardIconButton = null;
        private Func<UniTask<bool>> beforeSceneTransition = null;
        private CardParamPlayer cardPlayer = null;
        private DeckPlayer deckPlayer = null;
        private bool initLockState = false;
        private bool enabledTraining = false;
        private bool isUpdated = false;
        private bool isPossessionCard = false;
        private UserSkillTrainingSlot slotInfo = null;
        private Dictionary<CommonDialog.SelectType, CommonDialog.ButtonData> skillTrainingDialogNameMap;
        private UserCardList userCardList = null;

        private Action<bool> close = null;

        private TradePlayerDetail.TabType curTabType = TradePlayerDetail.TabType.Data;


#if DEV_BUILD
        private bool isIngameDebug = false; //< インゲームデバッグ中フラグ
#endif

        protected override void Refresh(PlayerDetailPageData itemData)
        {
            playerDetail.RefreshCardData(itemData);
            OpenPage(itemData);

            int hash = GetHashCode();
            int data = itemData.cardPlayer.CardId;
        }

        public void OpenPage(PlayerDetailPageData param)
        {
            cardPlayer = param.cardPlayer;
            deckPlayer = param.player;

            // beforeSceneTransition = param.beforeSceneTransition;
            // 成長済みカードまたはシーンで許可されていないならトレーニングボタンを非アクティブ
            enabledTraining = param.enabledTraining && !cardPlayer.IsGrownParam;
            // 所持カードフラグが有効かつ成長済み仮パラメータ表示じゃないなら既読フラグを有効化する
            isPossessionCard = param.isPossessionCard && !cardPlayer.IsGrownParam;

            // 基本情報.
            dataTab.Init(cardPlayer, deckPlayer);
            // 成長済みパラメータはスキルレベルが足りていなくてもスキル詳細を表示する
            skillTab.Init(cardPlayer, cardPlayer.IsGrownParam);
            infoTab.Init(cardPlayer);
            nameLabel.text = cardPlayer.CardName;
            dateLabel.text = cardPlayer.ReleaseSeason;
            dataCanvas.alpha = 1;
            dataCanvas.blocksRaycasts = true;
            skillCanvas.alpha = 0;
            skillCanvas.blocksRaycasts = false;
            infoCanvas.alpha = 0;
            infoCanvas.blocksRaycasts = false;
            tabBar.Init(0);
            tabBar.SelectTabEvent =
                index =>
                {
                    dataCanvas.alpha = (index == 0 ? 1 : 0);
                    dataCanvas.blocksRaycasts = index == 0;
                    skillCanvas.alpha = (index == 1 ? 1 : 0);
                    skillCanvas.blocksRaycasts = index == 1;
                    infoCanvas.alpha = (index == 2 ? 1 : 0);
                    infoCanvas.blocksRaycasts = index == 2;

                    SetTabType(index);
                };

            cardIconButton = playerIcon.GetComponent<UICustomButton>();
            cardIconButton.onClick.RemoveAllListeners();
            cardIconButton.onClick.AddListener(() =>
            {
                PlayerCardView.Open(new PlayerCardView.Param(cardPlayer, viewCardAssetLoader, playerDetail.GetDisplayCardDataList(),
                    playerDetail.OnScrollTo));
            });

            initLockState = cardPlayer.CardInfoPlayer.IsLocked;
            attentionText.enabled = cardPlayer.IsGrownParam;

            skillTrainingDialogNameMap = new()
        {
            {
                // OK
                CommonDialog.SelectType.Center,
                new CommonDialog.ButtonData(
                    LanguageManager.Instance.GetOSTText("ID_SYS_1002"),
                    () => {
                        // 遷移なし
                        // SkillTrainingSlot.Open(new SkillTrainingSlot.Param());
                    }
                )
            },
        };

            playerDetail.RemoveTabButtonCallback(RefreshCurTab);
            playerDetail.AddTabButtonCallback(RefreshCurTab);

            RefreshCurTab((int)playerDetail.GetCurTabType());

            LoadPlayerCardSmallAysnc().Forget();
        }

        AsyncOperationHandle handle = new AsyncOperationHandle();
        private async UniTask LoadPlayerCardSmallAysnc()
        {
            assetLoader = playerDetail.GetPlayerDetailAssetLoader();

            while (!handle.IsDone)
            {
                await UniTask.Yield();
            }

            handle = assetLoader.LoadPlayerCardSmallAsync(
                cardPlayer.CardParamEntity.PlayerPicNo, cardPlayer.CurrentRarity
            );
            await handle.Task;
            var logBook = await GlobalDataManager.Instance.GlobalUser.GetUserCardLogbook();
            bool isSample = true;
            if (logBook.UserLogbook.TryGetValue(cardPlayer.CardId, out var rarities))
            {
                isSample = !rarities.Contains(cardPlayer.CurrentRarity);
            }
            playerIcon.Init(handle.Result as Sprite, null, false, upgradeLevel: cardPlayer.UpgradeLevel);
            userCardList = await GlobalDataManager.Instance.GlobalUser.GetUserCardList();

            slotInfo = await GlobalDataManager.Instance.GlobalUser.GetUserSkillTrainingSlot();
        }


        private void SetTabType(int index)
        {
            curTabType = (TradePlayerDetail.TabType)index;
            playerDetail.BroadcastCurTabType(curTabType);
        }

        private void RefreshCurTab(int index)
        {
            tabBar.Init(index);

            dataCanvas.alpha = (index == 0 ? 1 : 0);
            dataCanvas.blocksRaycasts = index == 0;
            skillCanvas.alpha = (index == 1 ? 1 : 0);
            skillCanvas.blocksRaycasts = index == 1;
            infoCanvas.alpha = (index == 2 ? 1 : 0);
            infoCanvas.blocksRaycasts = index == 2;
        }
    }
}