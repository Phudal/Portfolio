using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dimps.Application.Common.UI;
using System;
using Dimps.Application.API.Message;

namespace GVNC.Application.Trade
{
    public class ProposeListScrollDataConatainer : MultiLoadScrollDataConatainer
    {
        public enum TradeProposeState
        {
            Offered = 0,    // 확인, 취소  
            Canceled = 1,   // 취소되었으므로 UI 진입 불가
            Accepted = 2,   // 확인, 수령(상대 카드)
            Declined = 3,   // 확인, 수령(본인 카드)                        
            Reverted = 4,   // 이미 수령하였으므로 UI 진입 불가
            Received = 5,   // 이미 수령하였으므로 UI 진입 불가
            Expired = 6,    // 기간이 만료됨
        }

        public MyProposedTradeInfo myProposedInfo;

        public TradeProposeState tradeState;

        public ProposeListScrollDataConatainer(MyProposedTradeInfo myProposedInfo, TradeProposeState tradeState) :
            base(new List<CardData>() { myProposedInfo.myProposedCard, myProposedInfo.registeredCard })
        {
            this.myProposedInfo = myProposedInfo;
            this.tradeState = tradeState;
        }
    }
}
