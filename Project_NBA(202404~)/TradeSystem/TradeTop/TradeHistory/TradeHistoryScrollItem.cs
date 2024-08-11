using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dimps.Application.Common;
using Dimps.Application.Common.Button;
using Dimps.Application.Common.Card;
using Dimps.Application.Common.UI;
using TMPro;
using System;

namespace GVNC.Application.Trade
{
    public class TradeHistoryScrollItem : DoubleLoadGridScrollCell<TradeHistoryScrollDataContainer>
    {
        // [SerializeField] private UICard giveCard = null;
        // [SerializeField] private UICard receiveCard = null;

        // [SerializeField] private TextMeshProUGUI tmp_TradeDesc;
        [SerializeField] private UILocalizeText tmp_DateTime;

        protected override void UpdateContent()
        {
            List<Action<Sprite>> actions = new List<Action<Sprite>>();
            actions.Add((sprite) => {
                firstCardView.SetUpCellView(mCellData.CardDataList[0]);
                // giveCard.Setup(mCellData.CardDataList[0]);
            });
            actions.Add((sprite) => {
                secondCardView.SetUpCellView(mCellData.CardDataList[1]);
                // receiveCard.Setup(mCellData.CardDataList[1]);
            });
            mCellData.LoadSuccessAction = actions;

            firstCardView.SetUpCellView(mCellData.CardDataList[0]);
            secondCardView.SetUpCellView(mCellData.CardDataList[1]);

            firstCardView.Card.InitButton(() =>
            {
                TradePlayerDetail.Open(new TradePlayerDetail.Param(mCellData.CardDataList[0].CardParam, TradePlayerDetail.ButtonType.Close));
            },
            () =>
            {
                TradePlayerDetail.Open(new TradePlayerDetail.Param(mCellData.CardDataList[0].CardParam, TradePlayerDetail.ButtonType.Close));
            });

            secondCardView.Card.InitButton(() =>
            {
                TradePlayerDetail.Open(new TradePlayerDetail.Param(mCellData.CardDataList[1].CardParam, TradePlayerDetail.ButtonType.Close));
            },
            () =>
            {
                TradePlayerDetail.Open(new TradePlayerDetail.Param(mCellData.CardDataList[1].CardParam, TradePlayerDetail.ButtonType.Close));
            });

            // giveCard.Setup(mCellData.CardDataList[0]);
            // receiveCard.Setup(mCellData.CardDataList[1]);

            tmp_DateTime.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1545"), mCellData.dateTime.ToString("g")).Replace("\\n", "\n"));
        }
    }
}
