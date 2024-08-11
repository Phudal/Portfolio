using Dimps.Application.Common.UI;
using Dimps.Application.MasterData;
using Dimps.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GVNC.Application.Trade
{
    public class TradeHistoryScrollDataContainer : MultiLoadScrollDataConatainer
    {
        public long tradeId;

        public DateTime dateTime;

        public TradeHistoryScrollDataContainer(CardData giveCard, CardData receiveCard, long tradeId, DateTime dateTime) :
            base(new List<CardData>() { giveCard, receiveCard })
        {
            this.tradeId = tradeId;
            this.dateTime = dateTime;
        }
    }

    public class TradeHistoryListController : DoubleLoadCardListController<TradeHistoryScrollDataContainer>
    {
        
    }
}

