using Cysharp.Threading.Tasks;
using Dimps.Application.Common;
using Dimps.Application.Common.Button;
using Dimps.Application.Common.Card;
using Dimps.Application.Common.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GVNC.Application.Trade
{
    public class TradeProposeScrollItem : DoubleLoadGridScrollCell<ProposeListScrollDataConatainer>
    {
        [SerializeField] private TradeTop tradeTop;

        [SerializeField] private UICard giveCard = null;
        [SerializeField] private UICard receiveCard = null;

        [SerializeField] private UILocalizeText tmp_proposeStatus;
        [SerializeField] private GameObject gameObject_EndDTDesc;
        [SerializeField] private UILocalizeText tmp_proposeEndDT;

        [SerializeField] private UICustomButton btn_viewDetail;
        [SerializeField] private UICustomButton btn_cancelPropose;
        [SerializeField] private UICustomButton btn_revertPropose;
        [SerializeField] private UICustomButton btn_completePropose;

        protected override void UpdateContent()
        {
            List<Action<Sprite>> actions = new List<Action<Sprite>>();
            actions.Add((sprite) => giveCard.Setup(mCellData.CardDataList[0]));
            actions.Add((sprite) => receiveCard.Setup(mCellData.CardDataList[1]));
            mCellData.LoadSuccessAction = actions;

            btn_viewDetail.onClick.RemoveAllListeners();
            btn_cancelPropose.onClick.RemoveAllListeners();
            btn_revertPropose.onClick.RemoveAllListeners();
            btn_completePropose.onClick.RemoveAllListeners();            

            btn_viewDetail.onClick.AddListener(() => Context.method.ScrollItemCallback_1(mCellData));
            btn_cancelPropose.onClick.AddListener(() => Context.method.ScrollItemCallback_2(mCellData));
            btn_revertPropose.onClick.AddListener(() => Context.method.ScrollItemCallback_3(mCellData));
            btn_completePropose.onClick.AddListener(() => Context.method.ScrollItemCallback_4(mCellData));

            btn_viewDetail.gameObject.SetActive(true);
            btn_cancelPropose.gameObject.SetActive(mCellData.tradeState == ProposeListScrollDataConatainer.TradeProposeState.Offered);
            btn_revertPropose.gameObject.SetActive(
                mCellData.tradeState == ProposeListScrollDataConatainer.TradeProposeState.Declined ||
                mCellData.tradeState == ProposeListScrollDataConatainer.TradeProposeState.Expired);
            btn_completePropose.gameObject.SetActive(mCellData.tradeState == ProposeListScrollDataConatainer.TradeProposeState.Accepted);

            giveCard.Setup(mCellData.CardDataList[0]);
            receiveCard.Setup(mCellData.CardDataList[1]);

            string statusStringID = string.Empty;
            switch (mCellData.tradeState)
            {
                case ProposeListScrollDataConatainer.TradeProposeState.Offered:
                    statusStringID = LanguageManager.Instance.GetOSTText("ID_TRD_1542");
                    break;

                case ProposeListScrollDataConatainer.TradeProposeState.Accepted:
                    statusStringID = LanguageManager.Instance.GetOSTText("ID_TRD_1538");
                    break;

                case ProposeListScrollDataConatainer.TradeProposeState.Received:
                    statusStringID = LanguageManager.Instance.GetOSTText("ID_TRD_1538");
                    break;

                case ProposeListScrollDataConatainer.TradeProposeState.Declined:
                    statusStringID = LanguageManager.Instance.GetOSTText("ID_TRD_1547");
                    break;

                case ProposeListScrollDataConatainer.TradeProposeState.Expired:
                    statusStringID = LanguageManager.Instance.GetOSTText("ID_TRD_1547");
                    break;

                default:
                    break;
            }

            tmp_proposeStatus.SetTextDirect(statusStringID);

            UpdateEndDT();
        }

        private void UpdateEndDT()
        {
            if (mCellData.tradeState == ProposeListScrollDataConatainer.TradeProposeState.Offered)
            {
                if ((DateTime.Now - mCellData.myProposedInfo.offerDT.AddDays(1)).TotalMilliseconds > 0)
                {
                    mCellData.tradeState = ProposeListScrollDataConatainer.TradeProposeState.Expired;

                    if (tradeTop.RefreshProposeListLock == false)
                    {
                        tradeTop.RefreshProposeList().Forget();
                    }

                    tmp_proposeEndDT.SetTextDirect(string.Empty);
                    gameObject_EndDTDesc.SetActive(false);
                }
                else
                {
                    TimeSpan ts = mCellData.myProposedInfo.offerDT.AddDays(1) - DateTime.Now;
                    string remainTime = string.Format("{0}:{1}:{2}", ts.Hours.ToString("D2"), ts.Minutes.ToString("D2"), ts.Seconds.ToString("D2"));
                    tmp_proposeEndDT.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1511"), remainTime).Replace("\\n", "\n"));
                    gameObject_EndDTDesc.SetActive(true);
                }
                                               
            }
            else
            {
                tmp_proposeEndDT.SetTextDirect(string.Empty);
                gameObject_EndDTDesc.SetActive(false);
            }
        }

        private void FixedUpdate()
        {
            UpdateEndDT();
        }
    }
}
