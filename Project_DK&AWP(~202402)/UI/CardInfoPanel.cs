using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CoffeeLibrary;
using System.Collections.Generic;

public class CardInfoPanel : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI TMP_CardName;
    [SerializeField] IconObject iconObject;
    [SerializeField] Image[] img_CardBaseOptionIcon;
    [SerializeField] GameObject[] gamesobjectsCardBaseOption;
    [SerializeField] TextMeshProUGUI[] TMP_CardBaseOptionTitle;
    [SerializeField] TextMeshProUGUI[] TMP_CardCurBaseOption;
    [SerializeField] TextMeshProUGUI[] TMP_CardNextBaseOption;

    [SerializeField] TextMeshProUGUI TMP_CardLevel;

    [SerializeField] ListView_CardOption listView;

    [SerializeField] UISpineGraphic spine_LevelEffect;

    [SerializeField] GameObject gameObject_BottomMatInfo;
    [SerializeField] GameObject gameObject_cardStateInfo;
    [SerializeField] TextMeshProUGUI TMP_stateDesc;

    [NaughtyAttributes.BoxGroup("material 1")]
    [SerializeField] IconObject iconObject_mat1;
    [NaughtyAttributes.BoxGroup("material 2")]
    [SerializeField] IconObject iconObject_mat2;

    [NaughtyAttributes.BoxGroup("material 1")]
    [SerializeField] Image slider_mat1;
    [NaughtyAttributes.BoxGroup("material 2")]
    [SerializeField] Image slider_mat2;

    [NaughtyAttributes.BoxGroup("material 1")]
    [SerializeField] TextMeshProUGUI tmp_mat1;
    [NaughtyAttributes.BoxGroup("material 2")]
    [SerializeField] TextMeshProUGUI tmp_mat2;

    [SerializeField] UIReddot reddot;

    private int curCardIndex;

    void OnEnable()
    {
        Messenger.RegisterListener(EMessengerListenerType.ADD_LISTENER, EMessengerID.E_POPUP_CARD_INFO_REFESH, RefreshCardInfo);
    }
    void OnDisable()
    {
        Messenger.RegisterListener(EMessengerListenerType.REMOVE_LISTENER, EMessengerID.E_POPUP_CARD_INFO_REFESH, RefreshCardInfo);
    }

    private void Awake()
    {
        spine_LevelEffect.gameObject.SetActive(false);
        reddot.UpdateCostClear();
    }

    public void InitCardInfo(int cardIndex)
    {
        curCardIndex = cardIndex;

        RefreshCardInfo();
    }

    public void RefreshCardInfo()
    {
        int bonusEntity = SODataManager.Instance.GetCardData(curCardIndex).bonusEntity;
        EntityData entityData = SODataManager.Instance.GetEntityData(bonusEntity);
        if (entityData == null)
            return;
        
        var CardDataInfo = SODataManager.Instance.GetCardData(curCardIndex);

        if (CardDataInfo == null)
            return;

        TMP_CardName.text = CardDataInfo.GetCardString();

        UserCardData userCardData = UserDataManager.Instance.GetUserCardData(curCardIndex);
        if (userCardData == null)
        {
            // Card_Book_Unacquired : 미획득
            TMP_CardLevel.text = SODataManager.Instance.GetStringTableValue("Card_Book_Unacquired");
        }
        else if (userCardData.isGet == false)
        {
            // Card_Book_Unacquired : 미획득
            TMP_CardLevel.text = SODataManager.Instance.GetStringTableValue("Card_Book_Unacquired");
        }
        else
        {
            TMP_CardLevel.text = string.Format($"Lv.{userCardData.level}");
        }

        int maxCount = gamesobjectsCardBaseOption.Length;

        for (int i = 0; i < maxCount; i++)
        {
            bool isBuffInfo = false;
            if(CardDataInfo.baseBuffIndexList.Count > i)                
            {
                var BuffData = SODataManager.Instance.GetBuffData(CardDataInfo.baseBuffIndexList[i]);
                if (BuffData != null)
                {
                    gamesobjectsCardBaseOption[i].SetActive(true);
                    var buffDescData = SODataManager.Instance.GetBuffDescData(BuffData.buffKey);
                    if (buffDescData != null)
                    {
                        isBuffInfo = true;
                        TMP_CardBaseOptionTitle[i].text = SODataManager.Instance.GetStringTableValue(buffDescData.title);
                    }

                    if (userCardData == null)
                    {
                        // Card_Book_Unacquired 미획득
                        TMP_CardCurBaseOption[i].text = SODataManager.Instance.GetStringTableValue("Card_Book_Unacquired");
                        TMP_CardNextBaseOption[i].text = BuffData.GetValueString(1);
                    }
                    else if (userCardData.isGet == false)
                    {
                        // Card_Book_Unacquired 미획득
                        TMP_CardCurBaseOption[i].text = SODataManager.Instance.GetStringTableValue("Card_Book_Unacquired");
                        TMP_CardNextBaseOption[i].text = BuffData.GetValueString(1);
                    }
                    else
                    {
                        TMP_CardCurBaseOption[i].text = BuffData.GetValueString(userCardData.level);
                        if (userCardData.level >= SODataManager.Instance.define.CARD_MAX_LEVEL)
                        {
                            TMP_CardNextBaseOption[i].text = "MAX";
                        }   
                        else
                        {
                            TMP_CardNextBaseOption[i].text = BuffData.GetValueString(userCardData.level + 1);
                        }                            
                    }
                }
            }

            if (isBuffInfo)
            {
                img_CardBaseOptionIcon[i].gameObject.SetActive(true);
                TMP_CardBaseOptionTitle[i].gameObject.SetActive(true);
                TMP_CardCurBaseOption[i].gameObject.SetActive(true);
                TMP_CardNextBaseOption[i].gameObject.SetActive(true);
            }
            else
            {
                gamesobjectsCardBaseOption[i].SetActive(false);
                img_CardBaseOptionIcon[i].gameObject.SetActive(false);
                TMP_CardBaseOptionTitle[i].gameObject.SetActive(false);
                TMP_CardCurBaseOption[i].gameObject.SetActive(false);
                TMP_CardNextBaseOption[i].gameObject.SetActive(false);
            }
        }        

        iconObject.SetIconObject(REWARD_TYPE.CARD, curCardIndex);

        listView.InitListView(curCardIndex);

        InitReinforceMat();

        // 미획득한 카드일 때
        if (userCardData == null)
        {
            gameObject_cardStateInfo.SetActive(true);
            gameObject_BottomMatInfo.SetActive(false);
            TMP_stateDesc.text = SODataManager.Instance.GetStringTableValue("Card_Book_Unacquired_Desc");
        }
        // 최대 레벨까지 올린 카드일 때
        else if (userCardData.level >= SODataManager.Instance.define.CARD_MAX_LEVEL)
        {
            gameObject_cardStateInfo.SetActive(true);
            gameObject_BottomMatInfo.SetActive(false);
            TMP_stateDesc.text = SODataManager.Instance.GetStringTableValue("MSG_MAX_LEVEL");
        }
        // 획득하고 아직 강화를 다 못한 카드일 때
        else
        {
            gameObject_cardStateInfo.SetActive(false);
            gameObject_BottomMatInfo.SetActive(true);
        }

        reddot.UpdateIndex(REDDOT.CARD_ITEM, curCardIndex);
    }

    public void InitReinforceMat()
    {
        CardData cardData = SODataManager.Instance.GetCardData(curCardIndex);        
        iconObject_mat1.SetIconObject(REWARD_TYPE.CARD, curCardIndex);
        iconObject_mat2.SetIconObject(REWARD_TYPE.RESOURCE, cardData.requireItemIndex);

        UserCardData userCardData = UserDataManager.Instance.GetUserCardData(curCardIndex);
        double hasCardCount = 0;
        int level = 1;
        if (userCardData != null)
        {
            hasCardCount = userCardData.count;
            level = userCardData.level;
        }

        double weaponCount = UserDataManager.Instance.GetUserItemData(cardData.requireItemIndex).count;

        CardLevelData cardLevelData = SODataManager.Instance.GetCardLevelData(cardData.species, level);
        if(cardLevelData != null)
        {
            slider_mat1.fillAmount = MathUtil.GetPercentValue(hasCardCount, cardLevelData.requireCardCount);
            slider_mat2.fillAmount = MathUtil.GetPercentValue(weaponCount, cardLevelData.requireItemCount);

            tmp_mat1.text = ProjectUtil.GetCurNeedNumberToString(hasCardCount, cardLevelData.requireCardCount);
            tmp_mat2.text = ProjectUtil.GetCurNeedNumberToString(weaponCount, cardLevelData.requireItemCount);
        }                
        else
        {
            slider_mat1.fillAmount = 1;
            slider_mat2.fillAmount = 1;

            tmp_mat1.text = "MAX";
            tmp_mat2.text = "MAX";
        }
    }

    public void OnClicked_Levelup()
    {
        UserCardData userCardData = UserDataManager.Instance.GetUserCardData(curCardIndex);
        if (userCardData == null)
        {
            // Card_Book_Unacquired_Desc : 아직 획득하지 못한 카드입니다
            UIManager.Instance.ShowToastMessage(SODataManager.Instance.GetStringTableValue("Card_Book_Unacquired_Desc"));

            return;
        }
        else if (userCardData.isGet == false)
        {
            // Card_Book_Unacquired_Desc : 아직 획득하지 못한 카드입니다
            UIManager.Instance.ShowToastMessage(SODataManager.Instance.GetStringTableValue("Card_Book_Unacquired_Desc"));

            return;
        }

        if (userCardData.level >= SODataManager.Instance.define.CARD_MAX_LEVEL)
        {
            // Card_Book_Unacquired_Desc : 아직 획득하지 못한 카드입니다
            UIManager.Instance.ShowToastMessage(SODataManager.Instance.GetStringTableValue("MSG_MAX_LEVEL"));
            return;
        }            

        CardData cardData = SODataManager.Instance.GetCardData(curCardIndex);
        CardLevelData cardLevelData = SODataManager.Instance.GetCardLevelData(cardData.species, userCardData.level);

        // 카드 보유 검사
        if (ProjectUtil.IsLackResource(REWARD_TYPE.CARD, curCardIndex, cardLevelData.requireCardCount, true))
        {
            return;
        }

        // 무기 보유 검사
        if (ProjectUtil.IsLackResource(REWARD_TYPE.RESOURCE, cardData.requireItemIndex, cardLevelData.requireItemCount, true))
        {
            return;
        }

        UserDataManager.Instance.AddUserItemData(REWARD_TYPE.CARD, curCardIndex, -cardLevelData.requireCardCount);
        UserDataManager.Instance.AddUserItemData(REWARD_TYPE.RESOURCE, cardData.requireItemIndex, -cardLevelData.requireItemCount);

        userCardData.level++;

        spine_LevelEffect.PlayAnimation("00_get_non");

        UserDataManager.Instance.UpdateBuffSync(BUFF_DETAIL_TYPE.CARD_BUFF, true);
        UserDataManager.Instance.UpdateBuffSync(BUFF_DETAIL_TYPE.CARD_SPIECIES_TOTALLEVEL, true);
        UserDataManager.Instance.UpdateBuff();

        UserDataManager.Instance.SaveData(USER_DATA_TYPE.USER_CARD);
        UserDataManager.Instance.SendNetworkSync();

        Messenger.Broadcast(EMessengerID.E_POPUP_CARD_INFO_REFESH);        
    }
}
