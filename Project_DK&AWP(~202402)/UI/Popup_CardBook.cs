using CoffeeLibrary;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;

public class Popup_CardBook : Popup
{
    public enum TAB
    {
        HUMAN, ELF, ORC, UNDEAD, ANGEL,
        MAX
    };

    [SerializeField] UIContentsLock cardSummonLock;

    [SerializeField] List<UIReddot> topReddotList = new List<UIReddot>();

    [SerializeField] List<ResourceBoard> resourceBoardList = new List<ResourceBoard>();
    [SerializeField] UITabButtonGroup tabGroup;

    [SerializeField] ListView_Card listView;

    [SerializeField] CardInfoPanel cardInfoPanel;

    [SerializeField] TextMeshProUGUI tmp_totalCardLevel;

    [SerializeField] TotalCardLevelTooltip totalcardleveltooltip;

    SPECIES_TYPE species_type = SPECIES_TYPE.HUMAN;
    TAB curTab = TAB.HUMAN;

    [HideInInspector]
    public int curSelectCardIndex;

    void OnEnable()
    {
        Messenger<int>.RegisterListener(EMessengerListenerType.ADD_LISTENER, EMessengerID.E_POPUP_CARD_SELECT, RefreshCardInfo);
        Messenger.RegisterListener(EMessengerListenerType.ADD_LISTENER, EMessengerID.E_POPUP_CARD_INFO_REFESH, RefreshTotalCardLevel);
    }
    void OnDisable()
    {
        Messenger<int>.RegisterListener(EMessengerListenerType.REMOVE_LISTENER, EMessengerID.E_POPUP_CARD_SELECT, RefreshCardInfo);
        Messenger.RegisterListener(EMessengerListenerType.REMOVE_LISTENER, EMessengerID.E_POPUP_CARD_INFO_REFESH, RefreshTotalCardLevel);
    }

    public override void OnPopupPushed(PopupBaseArgs args)
    {
        base.OnPopupPushed(args);

        cardSummonLock.UpdateUnlockInfo(CONTENTS_KEY.CARD_SUMMON);

        topReddotList[(int)TAB.HUMAN].UpdateIndex(REDDOT.CARD_SPECIES, (int)TAB.HUMAN);
        topReddotList[(int)TAB.ELF].UpdateIndex(REDDOT.CARD_SPECIES, (int)TAB.ELF);
        topReddotList[(int)TAB.ORC].UpdateIndex(REDDOT.CARD_SPECIES, (int)TAB.ORC);
        topReddotList[(int)TAB.UNDEAD].UpdateIndex(REDDOT.CARD_SPECIES, (int)TAB.UNDEAD);
        topReddotList[(int)TAB.ANGEL].UpdateIndex(REDDOT.CARD_SPECIES, (int)TAB.ANGEL);

        tabGroup.UpdateLayoutRebuilder();
        OnClick_TabButton((int)TAB.HUMAN);
    }

    public void OnClick_TabButton(int index)
    {
        curTab = (TAB)index;
        
        Enum.TryParse(curTab.ToString(), out species_type);
        tabGroup.UpdateTab(index);

        RefreshTotalCardLevel();

        RefreshCardList();
    }

    public void RefreshCardList()
    {
        listView.InitListView(curTab);

        TempCoroutine.Instance.NextFrame(() =>
        {
            var tmpData = listView.GetItemData(0);
            if (tmpData != null)
            {
                Messenger<int>.Broadcast(EMessengerID.E_POPUP_CARD_SELECT, tmpData.index);
            }
        });
    }

    public void RefreshTotalCardLevel()
    {
        List<UserCardData> cardDatas = UserDataManager.Instance.userCardData;
        int totalLevel = UserDataManager.Instance.GetUserSpeciesTotalCardLevel(species_type);

        string text = string.Format(SODataManager.Instance.GetStringTableValue("TOTAL_CARD_LEVEL_BUFF_DESC"), ProjectUtil.GetSpeciesString(species_type), totalLevel);
        
        tmp_totalCardLevel.text = text;
    }

    public void RefreshCardInfo(int cardIndex)
    {
        curSelectCardIndex = cardIndex;

        cardInfoPanel.InitCardInfo(curSelectCardIndex);
    }

    public void ShowTooltip()
    {
        totalcardleveltooltip.ShowToolTip(species_type);
    }

    public void OnClicked_PushExchangePopup()
    {
        UIManager.Instance.PopupLoad(EPopup.Popup_Exchange);        
    }

    public void OnClicked_PushEntityPopup()
    {
        var arg = new Popup_Entity.Args();
        arg.type = QUEST_TYPE.NONE;
        UIManager.Instance.PopupLoad(EPopup.Popup_Entity, arg);

        PopPopup();
    }

    public void OnClicked_PushCardSummon()
    {
        UIManager.Instance.PopupLoad(EPopup.Popup_CardSummon);

        PopPopup();
    }
}
