using Dimps.Application.Common;
using Dimps.Application.Common.Button;
using Dimps.Application.Common.Card;
using Dimps.Application.Common.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GVNC.Application.Trade
{
    public class SearchedTradeItemCell : SingleLoadGridScrollCell<SearchedTradeItemData>
    {
        [SerializeField] private UICard card = null;

        [SerializeField] private UILocalizeText text_playerName;
        [SerializeField] private UILocalizeText text_playerRarity;
        [SerializeField] private UILocalizeText text_playerPosition;
        [SerializeField] private UICustomButton btn_viewDetail;
        [SerializeField] private TextMeshProUGUI tmp_isNPC;

        protected override void UpdateContent()
        {
            btn_viewDetail.onClick.RemoveAllListeners();
            btn_viewDetail.onClick.AddListener(() => Context.method.ScrollItemCallback_1(mCellData));

            text_playerName.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1518"), mCellData.CardDataList[0].CardParam.CardName));
            text_playerRarity.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1519"), mCellData.CardDataList[0].CardParam.CurrentRarity));
            text_playerPosition.SetTextDirect(string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1521"), mCellData.CardDataList[0].CardParam.GoodPosition));

#if UNITY_EDITOR
            if (mCellData.isNPCData)
            {
                tmp_isNPC.gameObject.SetActive(true);
                tmp_isNPC.text = "NPC";
            }
            else
            {
                tmp_isNPC.gameObject.SetActive(false);
            }
#else
            tmp_isNPC.gameObject.SetActive(false);
            tmp_isNPC.text = string.Empty;
#endif

            mCellData.LoadSuccessAction.Clear();
            mCellData.LoadSuccessAction.Add((sprite) => card.Setup(mCellData.CardDataList[0]));

            card.Setup(mCellData.CardDataList[0]);
            card.InitButton(
                click: () =>
                {
                    Context.method.ScrollItemCallback_1(mCellData);
                },
                longClick: () =>
                {
                    Context.method.ScrollItemCallback_2(mCellData);
                },
                zoomInCardClick: () =>
                {
                    
                }
            );
        }
    }
}