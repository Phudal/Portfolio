using Dimps.Application.Common.Card;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dimps.Application.Common;
using Dimps.Application.Common.Button;
using Dimps.Application.Common.UI.CardList;
using Cysharp.Threading.Tasks;
using Dimps.Application.Common.UI;
using Dimps.Application.AssetBundle;
using UnityEngine.ResourceManagement.AsyncOperations;
using Dimps.Application.Outgame.Base.FacilityData;
using System;
namespace GVNC.Application.Trade
{
    public class TradeRegisterStatusProposedItem : MonoBehaviour
    {
        // [SerializeField] private CardDataView cardDataView = null;
        [SerializeField] private UICard uiCard = null;
        [SerializeField] private UICustomButton btn_ConfirmPropose = null;

        private CardData cardData;
        private AssetLoader assetLoader;

        private int slotIndex;

        public void InitializeData(int slotIndex, CardData cardData, AssetLoader assetLoader, Action<int> action)
        {
            this.slotIndex = slotIndex;
            this.cardData = cardData;
            this.assetLoader = assetLoader;
            btn_ConfirmPropose.onClick.RemoveAllListeners();
            btn_ConfirmPropose.onClick.AddListener(() => action.Invoke(slotIndex));
        }

        public async UniTask LoadCardAsset()
        {
            if (cardData.CardAsset == null)
            {
                AsyncOperationHandle handle = assetLoader.LoadPlayerCardSmallAsync(cardData.CardParam.CardParamEntity.PlayerPicNo,
                    cardData.CardParam.CurrentRarity);

                await handle.Task;
                cardData.SetCardAsset(handle.Result as Sprite);
            }

            uiCard.Setup(cardData);
            // cardDataView.SetUpCellView(cardData);
        }
    }
}
