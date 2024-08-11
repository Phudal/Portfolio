using Dimps.Application.Common.Button;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GVNC.Application.Trade
{
    public class TradeRegisterSlotTypeRegisterable : MonoBehaviour
    {
        [SerializeField] private UICustomButton btn_RegisterNew;

        private int slotIndex = 0;
        public void Initialize(int slotIndex, Action<int> btnAction)
        {
            this.slotIndex = slotIndex;

            btn_RegisterNew.onClick.RemoveAllListeners();
            btn_RegisterNew.onClick.AddListener(() => btnAction?.Invoke(slotIndex));
        }
    }
}