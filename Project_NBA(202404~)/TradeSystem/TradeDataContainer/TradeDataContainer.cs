using Dimps.Application.Common.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GVNC.Application.Trade
{
    public class MyProposedTradeInfo
    {
        public long tradeId;

        public long offerId;

        public CardData myProposedCard;

        public CardData registeredCard;

        public DateTime offerDT;

        public MyProposedTradeInfo(long tradeId, long offerId, CardData myProposedCard, CardData registeredCard, DateTime offerDT)
        {
            this.tradeId = tradeId;
            this.offerId = offerId;
            this.myProposedCard = myProposedCard;
            this.registeredCard = registeredCard;
            this.offerDT = offerDT;
        }
    }

    public class OtherPlayerProposedTradeInfo
    {
        public long trade_id;

        public long offer_id;

        public CardData cardData;

        public DateTime proposedDT;

        public OtherPlayerProposedTradeInfo(long trade_id, long offer_id, CardData cardData, DateTime proposedDT)
        {
            this.trade_id = trade_id;
            this.offer_id = offer_id;
            this.cardData = cardData;
            this.proposedDT = proposedDT;
        }
    }
}
