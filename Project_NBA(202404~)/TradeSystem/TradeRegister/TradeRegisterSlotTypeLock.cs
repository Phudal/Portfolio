using Dimps.Application.Common;
using Dimps.Application.MasterData;
using Dimps.Application.MasterData.Master;
using GVNC.Application.Trade;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TradeRegisterSlotTypeLock : MonoBehaviour
{
    [SerializeField] private UILocalizeText localizeText_LockCondition;

    public void Initialize(int slotIndex)
    {
        if (MasterDataManager.Instance.BaseTradeSlotMaster.TryGetBaseTradeSlotEntityFromSlotCount(
            slotIndex + 1, out TradeBaseEntity entity))
        {
            localizeText_LockCondition.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1523"), entity.FacilityLevel));
        }        
    }
}
