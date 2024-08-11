using Cysharp.Threading.Tasks;
using Dimps.Application.AssetBundle;
using Dimps.Application.Common;
using Dimps.Application.Common.Button;
using Dimps.Application.Common.Card;
using Dimps.Application.Common.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GVNC.Application.Trade
{
    public class TradeRegisterSlotTypeRegistered : MonoBehaviour
    {
        [SerializeField] private UICard card = null;

        [SerializeField] private UILocalizeText text_playerName;
        [SerializeField] private UILocalizeText text_playerRarity;
        [SerializeField] private UILocalizeText text_playerLevel;
        [SerializeField] private UILocalizeText text_registeredDT;
        [SerializeField] private UILocalizeText text_proposedCount;

        [SerializeField] private UICustomButton btn_viewDetail;
        [SerializeField] private UICustomButton btn_cancelRegister;

        private AssetLoader assetLoader = null;

        private TradeRegisterSlotDataContainer data;
        private int slotIndex = 0;

        public void Initialize(int slotIndex, TradeRegisterSlotDataContainer data, AssetLoader assetLoader, Action<int> viewDetailAction, Action<int> cancelAction)
        {
            this.slotIndex = slotIndex;

            btn_viewDetail.onClick.RemoveAllListeners();
            btn_viewDetail.onClick.AddListener(() => viewDetailAction?.Invoke(slotIndex));

            btn_cancelRegister.onClick.RemoveAllListeners();
            btn_cancelRegister.onClick.AddListener(() => cancelAction?.Invoke(slotIndex));

            this.data = data;

            this.assetLoader = assetLoader;

            LoadCardAsset().Forget();

            text_playerName.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1518"), data.registeredCardData.CardParam.CardName));
            text_playerRarity.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1519"), data.registeredCardData.CardParam.CurrentRarity));
            text_playerLevel.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1520"), data.registeredCardData.CardParam.CardLevel));
            text_registeredDT.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1516"), data.registerDT.ToString("g")));
            text_proposedCount.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1517"), data.proposedCardList.Count));                                                            
        }

        private async UniTask LoadCardAsset()
        {
            if (data.registeredCardData.CardAsset == null)
            {
                AsyncOperationHandle handle = assetLoader.LoadPlayerCardSmallAsync(data.registeredCardData.CardParam.CardParamEntity.PlayerPicNo, data.registeredCardData.CardParam.CurrentRarity);
                await handle.Task;
                data.registeredCardData.SetCardAsset(handle.Result as Sprite);                
            }

            card.Setup(data.registeredCardData);
        }
    }    
}