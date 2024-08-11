using Dimps.Application.Common.UI;
using FancyScrollView;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GVNC.Application.Trade
{
    public class TradeHistoryGridScroll : MultiLoadGridScroll<TradeHistoryScrollDataContainer>
    {
        protected class TradeHistoryCellGroup : CellGroup { }

        [SerializeField] private FancyGridViewCell<TradeHistoryScrollDataContainer, MultiLoadGridScrollContext> _cellPrefab;

        protected override void SetupCellTemplate()
        {
            Setup<TradeHistoryCellGroup>(_cellPrefab);
        }
    }
}
