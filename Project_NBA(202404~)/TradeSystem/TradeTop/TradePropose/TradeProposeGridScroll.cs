using Dimps.Application.Common.UI;
using FancyScrollView;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GVNC.Application.Trade
{
    public class TradeProposeGridScroll : MultiLoadGridScroll<ProposeListScrollDataConatainer>
    {        
        protected class TradeProposeCellGroup : CellGroup { }

        [SerializeField] private FancyGridViewCell<ProposeListScrollDataConatainer, MultiLoadGridScrollContext> _cellPrefab;

        protected override void SetupCellTemplate()
        {
            Setup<TradeProposeCellGroup>(_cellPrefab);
        }
    }
}
