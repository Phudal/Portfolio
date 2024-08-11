using Cysharp.Threading.Tasks;
using Dimps.Application.AssetBundle;
using Dimps.Application.Common;
using Dimps.Application.Common.Button;
using Dimps.Application.Common.Constants;
using Dimps.Application.Common.Dialog;
using Dimps.Application.Common.UI;
using Dimps.Application.Global;
using Dimps.Application.MasterData;
using Dimps.Utility;
using GVNC.Application.Trade;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace GVNC.Application.Trade
{

    public class TradeProposeAndRegister : MonoBehaviour
    {
        [SerializeField] private UICustomButton btn_RegisterTrade;
        [SerializeField] private UICustomButton btn_ProposeTrade;


        // private List<int> viewCardIndexList = new List<int>();

        private List<TradeRegisterSlotDataContainer> registerContainerList = new List<TradeRegisterSlotDataContainer>();

        private int userBaseLevel;

        public void Init(List<TradeRegisterSlotDataContainer> registerContainerList, int userBaseLevel)
        {
            this.registerContainerList = registerContainerList;
            this.userBaseLevel = userBaseLevel;

            btn_RegisterTrade.onClick.RemoveAllListeners();
            btn_ProposeTrade.onClick.RemoveAllListeners();

            btn_RegisterTrade.onClick.AddListener(OnClicked_RegisterTrade);
            btn_ProposeTrade.onClick.AddListener(OnClicked_ProposeTrade);
        }

        public void OnClicked_ProposeTrade()
        {
            if (GlobalDataManager.Instance.GlobalTime.CurrentTime - GlobalDataManager.Instance.GlobalTime.LastTradeRequestTime < OutGameConstatns.TradeSearchCoolTime)
            {
                int nextReqSec = 10 - (int)(GlobalDataManager.Instance.GlobalTime.CurrentTime - GlobalDataManager.Instance.GlobalTime.LastTradeRequestTime) / 1000;

                // 요청한지 얼마 안되서 잠시 후 시도하라고 해야함
                Dictionary<CommonDialog.SelectType, CommonDialog.ButtonData> tmpButtonMap = new Dictionary<CommonDialog.SelectType, CommonDialog.ButtonData>();
                tmpButtonMap.Add(CommonDialog.SelectType.Center, new CommonDialog.ButtonData(LanguageManager.Instance.GetOSTText("ID_SYS_1800"), null));
                CommonDialog.Open(new CommonDialog.Param(
                    CommonDialog.ButtonType.Single, LanguageManager.Instance.GetOSTText("ID_TRD_1500"),
                    string.Format(LanguageManager.Instance.GetOSTText("ID_TRD_1551"), nextReqSec), tmpButtonMap));

                return;
            }

            TradeFilterMenu.Open(new TradeFilterMenu.Param(new CardListRule(SortRule.Name, true),
                "ID_TRD_1503",
                (cardListRule) =>
                {
                    // Close Callback                
                    FilteringCard(cardListRule);
                }));
        }

        public void OnClicked_RegisterTrade()
        {
            TradeRegister.Open(new TradeRegister.Param(registerContainerList, userBaseLevel));
        }

        private void FilteringCard(CardListRule rule)
        {
            List<CardData> list = new List<CardData>();

            var dictionary = MasterDataManager.Instance.PlayerCardMaster.GetPlayerCardDispListExceptTradeBlock();            
            foreach (int id in dictionary.Keys)
            {                
                CardData card = new CardData(0, dictionary[id].CardId, dictionary[id].Rarity);
                card.ViewType = CardData.CardViewType.TradeInfo;
                list.Add(card);
            }

            List<CardData> ret = CardListSortFilter.SortFilter(list, rule).ToList();

            if (ret.Count > 0)
            {                
                // TradePropose UI를 열어서 해당 UI에서 서버에 검색을 요청 후 스크롤에 띄움
                TradePropose.Open(new TradePropose.Param(rule));
            }
            else
            {
                // 조건에 만족하는 선수가 존재하지 않는다고 팝업을 띄워야함
                Dictionary<CommonDialog.SelectType, CommonDialog.ButtonData> tmpButtonMap = new Dictionary<CommonDialog.SelectType, CommonDialog.ButtonData>();
                tmpButtonMap.Add(CommonDialog.SelectType.Center, new CommonDialog.ButtonData(LanguageManager.Instance.GetOSTText("ID_SYS_1800"), null));
                CommonDialog.Open(new CommonDialog.Param(
                    CommonDialog.ButtonType.Single, LanguageManager.Instance.GetOSTText("ID_TRD_1500"), 
                    LanguageManager.Instance.GetOSTText("ID_TRD_1552"), tmpButtonMap));
            }
        }
    }    
}