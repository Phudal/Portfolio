using Dimps.Application.Common.UI;
using FancyScrollView;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GVNC.Application.Trade
{
    public class TradeSearchGridScroll : MultiLoadGridScroll<SearchedTradeItemData>
    {
        protected class TradeSearchCellGroup : CellGroup { }

        [SerializeField] private FancyGridViewCell<SearchedTradeItemData, MultiLoadGridScrollContext> _cellPrefab;

        protected override void SetupCellTemplate()
        {
            Setup<TradeSearchCellGroup>(_cellPrefab);
        }
    }
}
