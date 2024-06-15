using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CoffeeLibrary;

public class Popup_RankedPreset : Popup
{
    [SerializeField] TextMeshProUGUI tmp_RankedType;
    [SerializeField] TextMeshProUGUI tmp_curRanking;

    [NaughtyAttributes.HorizontalLine(2, NaughtyAttributes.EColor.Blue)]
    [SerializeField] UITabButtonGroup tabGroup;

    [SerializeField] private RankedPresetInfo rankedEntityFormation;
    [SerializeField] private TextMeshProUGUI tmp_AnotherUserName;

    [SerializeField] private RankedEntityInfo rankedEntityInfo;
    [SerializeField] private RankedSpiritInfo rankedSpiritInfo;

    [SerializeField] private GameObject gameObject_RightInfo;

    [SerializeField] private GameObject gameObject_LeftObj;
    [SerializeField] private GameObject gameObject_RankedEmpty;

    public enum TAB
    {
        Entity, 
        Spirit,
        MAX,
    }

    public class Args : PopupBaseArgs
    {
        public string anotheruserName;
        public int curRanking;
        public RankedPresetType rankedType;
    }

    TAB curTab = TAB.Entity;

    Args popupArgs;

    public void OnEnable()
    {
        Messenger<int>.RegisterListener(EMessengerListenerType.ADD_LISTENER, EMessengerID.E_RANKED_ENTITY_SELECT, SetEntityInfo);
    }

    public void OnDisable()
    {
        Messenger<int>.RegisterListener(EMessengerListenerType.REMOVE_LISTENER, EMessengerID.E_RANKED_ENTITY_SELECT, SetEntityInfo);
    }

    private void Awake()
    {
        tabGroup.UpdateLayoutRebuilder();
    }

    public override void OnPopupPushed(PopupBaseArgs args)
    {
        base.OnPopupPushed(args);

        this.popupArgs = args as Args;

        tabGroup.UpdateLayoutRebuilder();

        tmp_AnotherUserName.text = popupArgs.anotheruserName;

        // rankedEntityInfo.gameObject.SetActive(false);

        InitInfo();

        RankedIconObject rankedIconObject = rankedEntityFormation.GetFirstEntityIcon();
        if (rankedIconObject != null)
        {
            rankedIconObject.OnClicked_RankedIcon();
        }

        switch (popupArgs.rankedType)
        {
            case RankedPresetType.STAGE:
                tmp_RankedType.text = SODataManager.Instance.GetStringTableValue("RANKING_STAGE");
                break;

            case RankedPresetType.NEST:
                tmp_RankedType.text = SODataManager.Instance.GetStringTableValue("RANKING_NEST");
                break;

            case RankedPresetType.PVP:
                tmp_RankedType.text = SODataManager.Instance.GetStringTableValue("CONTENTS_KEY_PVP");
                break;
        }

        tmp_curRanking.text = string.Format(SODataManager.Instance.GetStringTableValue("RANK_NUM"), popupArgs.curRanking);
    }

    public void InitInfo()
    {
        // 랭킹에 저장된 데이터가 없다면 모든 컴포넌트를 끈다.
        if (UserDataManager.Instance.anotherUserRankedPreset.isDataEnable == false)
        {
            rankedEntityFormation.gameObject.SetActive(false);
            rankedSpiritInfo.gameObject.SetActive(false);
            rankedEntityInfo.gameObject.SetActive(false);

            gameObject_RightInfo.SetActive(false);

            gameObject_LeftObj.SetActive(false);
            gameObject_RankedEmpty.SetActive(true);
            return;
        }

        gameObject_LeftObj.SetActive(true);
        gameObject_RankedEmpty.SetActive(false);

        gameObject_RightInfo.SetActive(true);

        rankedEntityFormation.gameObject.SetActive(true);
        rankedEntityFormation.Initinfo();

        rankedEntityInfo.gameObject.SetActive(curTab == TAB.Entity);
        rankedSpiritInfo.gameObject.SetActive(curTab == TAB.Spirit);
        
        if (curTab == TAB.Spirit)
        {
            rankedSpiritInfo.InitInfo();
        }
        else if (curTab == TAB.Entity)
        {
            
        }
    }

    public void SetEntityInfo(int entityIndex)
    {
        rankedEntityInfo.gameObject.SetActive(true);
        rankedEntityInfo.InitInfo(entityIndex);

        rankedEntityFormation.SetFocusIcon(entityIndex);

        OnClick_TabButton((int)TAB.Entity);
    }

    public void OnClick_TabButton(int index)
    {
        curTab = (TAB)index;

        tabGroup.UpdateTab(index);

        InitInfo();
    }
}
