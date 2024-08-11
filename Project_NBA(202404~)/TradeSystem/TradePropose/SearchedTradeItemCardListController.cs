using Dimps.Application.Common.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GVNC.Application.Trade
{
    public class SearchedTradeItemCardListController : SingleLoadCardListController<SearchedTradeItemData>
    {
       
    }

    public class SearchedTradeItemData : MultiLoadScrollDataConatainer
    {
        public long tradeId;

        public bool isNPCData;

        public SearchedTradeItemData(CardData cardData, long tradeId, bool isNPCData) :
            base(new List<CardData>() { cardData })
        {
            this.tradeId = tradeId;
            this.isNPCData = isNPCData;
        }
    }
}