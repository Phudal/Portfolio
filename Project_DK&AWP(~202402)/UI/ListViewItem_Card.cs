using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CoffeeLibrary;

public class ListViewItem_Card : ListViewItem
{    
    [SerializeField] Image image_Entity;
    [SerializeField] Image image_EntityHasValue;
    [SerializeField] TextMeshProUGUI TMP_CardLevel;
    [SerializeField] TextMeshProUGUI TMP_CardName;
    [SerializeField] TextMeshProUGUI TMP_EntityHasValue;
    [SerializeField] Image image_SpeciesBG;

    [SerializeField] GameObject selectState;
    [SerializeField] GameObject hasState;

    [SerializeField] UIReddot reddot;

    ListViewData_Card info;

    private void Awake()
    {
        reddot.UpdateCostClear();
    }

    void OnEnable()
    {
        Messenger<int>.RegisterListener(EMessengerListenerType.ADD_LISTENER, EMessengerID.E_POPUP_CARD_SELECT, OnSelect);
        Messenger.RegisterListener(EMessengerListenerType.ADD_LISTENER, EMessengerID.E_POPUP_CARD_INFO_REFESH, UpdateInfo);
    }
    void OnDisable()
    {
        Messenger<int>.RegisterListener(EMessengerListenerType.REMOVE_LISTENER, EMessengerID.E_POPUP_CARD_SELECT, OnSelect);
        Messenger.RegisterListener(EMessengerListenerType.REMOVE_LISTENER, EMessengerID.E_POPUP_CARD_INFO_REFESH, UpdateInfo);
    }

    void OnSelect(int index)
    {
        if (info.index == index)
            UpdateInfo();
        else
            selectState.SetActive(info.index == index);
    }

    public void RefreshInfo(ListViewData_Card info)
    {
        this.info = info;

        UpdateInfo();
    }

    void UpdateInfo()
    {
        CardData cardData = SODataManager.Instance.GetCardData(info.index);

        EntityData entityData = SODataManager.Instance.GetEntityData(cardData.bonusEntity);        

        Sprite sprite = null;
        sprite = AtlasManager.Instance.GetSprite(entityData.GetResourcePath(1), AtlasType.EntityIcon);
            
        if (sprite == null)
        {
            sprite = AtlasManager.Instance.GetSprite("ui_icon_question2", AtlasType.GameIcon);
        }
        image_Entity.sprite = sprite;         

        Popup_CardBook popup = UIManager.Instance.GetPopup<Popup_CardBook>(EPopup.Popup_CardBook);
        if (popup != null)
        {
            selectState.SetActive(info.index == popup.curSelectCardIndex);
        }

        UserCardData userCardData = UserDataManager.Instance.GetUserCardData(info.index);
        double curValue = 0;
        double needValue = 0;
        if (userCardData != null)
        {
            var CardDataInfo = SODataManager.Instance.GetCardLevelData(cardData.species, userCardData.level);
            curValue = userCardData.count;     
            if(CardDataInfo != null)
                needValue = CardDataInfo.requireCardCount;
            else
                needValue = 0;

            TMP_CardLevel.text = $"Lv.{userCardData.level}";
        }
        else
        {
            needValue = SODataManager.Instance.GetCardLevelData(cardData.species, 1).requireCardCount;
            // Card_Book_Unacquired : 미획득
            TMP_CardLevel.text = SODataManager.Instance.GetStringTableValue("Card_Book_Unacquired");
        }

        TMP_CardName.text = SODataManager.Instance.GetStringTableValue(cardData.title).Replace("\\n", "\n");

        TMP_EntityHasValue.text = ProjectUtil.GetCurNeedString(curValue, needValue);
        image_EntityHasValue.fillAmount = MathUtil.GetPercentValue(curValue, needValue);

        if (userCardData != null)
        { 
            hasState.SetActive(!userCardData.isGet);            
        }
        else
        {
            hasState.SetActive(true);
        }

        string resourcePath = ProjectUtil.GetSpeciesCardString(entityData.species);
        if (string.IsNullOrEmpty(resourcePath))
            Logger.LogError($"Invaid Species Type - {entityData.species}");
        else
            image_SpeciesBG.sprite = AtlasManager.Instance.GetSprite(resourcePath, AtlasType.UI);     

        reddot.UpdateIndex(REDDOT.CARD_ITEM, info.index);
    }

    public void OnClickEvent()
    {
        if (GetPassingEvent())
        {
            return;
        }

        Messenger<int>.Broadcast(EMessengerID.E_POPUP_CARD_SELECT, info.index);
    }
}
