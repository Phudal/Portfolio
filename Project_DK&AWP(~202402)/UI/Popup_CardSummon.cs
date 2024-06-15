using CodeStage.AntiCheat.ObscuredTypes;
using CoffeeLibrary;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class SummonCardData
{
    public int drawIndex;
    public bool isSummoned;    
}

[System.Serializable]
public class SummonCardViewData
{
    public Image cardBG;

    public Image cardImage;

    public TextMeshProUGUI tmp_count;

    public TextMeshProUGUI tmp_cardName;

    public GameObject isSummoned;

    public UIParticleSystem isFocus;

    public GameObject angelAnimObject;  
}

public class Popup_CardSummon : Popup
{
    [SerializeField] UIReddot cardReddot;

    [SerializeField] List<SummonCardData> summonCards = new List<SummonCardData>();
    [SerializeField] List<SummonCardViewData> summonCardViewDatas = new List<SummonCardViewData>();

    [SerializeField] List<GameObject> gameObject_RatioSelect = new List<GameObject>();

    //[SerializeField] UISliderAni expSlider;
    //[SerializeField] TextMeshProUGUI tmp_curExp_desc;
    //[SerializeField] TextMeshProUGUI tmp_curExp;

    [SerializeField] GameObject gameObject_RatioLayout;
    [SerializeField] GameObject gameObject_SummonButton;
    [SerializeField] GameObject gameObject_GiveupButton;

    [SerializeField] ResourceBoard resourceBoard_Price;

    [SerializeField] GameObject gameObject_RerollButton;
    [SerializeField] ResourceBoard resourceBoard_RerollPrice;

    [SerializeField] TextMeshProUGUI tmp_SummonButtonDesc;
    [SerializeField] TextMeshProUGUI tmp_GiveupButtonDesc;

    [SerializeField] TextMeshProUGUI tmp_DailyResetDT;

    [SerializeField] List<Lofle.Tween.Tween> tweenList = new List<Lofle.Tween.Tween>();

    [NaughtyAttributes.HorizontalLine(5, NaughtyAttributes.EColor.Blue)]
    [SerializeField] ResourceBoard resourceBoard_SummonCoin;
    [SerializeField] ResourceBoard resourceBoard_Mileage;
    [SerializeField] ResourceBoard resourceBoard_Dia;

    RewardInfo curPrice = new RewardInfo();

    int curRatio;

    bool isSummoning;

    public override void OnPopupPushed(PopupBaseArgs args)
    {
        base.OnPopupPushed(args);

        isSummoning = false;

        curRatio = UserDataManager.Instance.GetSummonRatio();

        foreach (GameObject go in gameObject_RatioSelect)
        {
            go.SetActive(false);
        }
        gameObject_RatioSelect[curRatio - 1].SetActive(true);

        InitCardInfo();

        RefreshCurPrice();

        RefreshRerollPrice();

        System.DateTime time = TimeUtil.CurDateTime().AddDays(1);
        System.TimeSpan timeSpan = time.Date - TimeUtil.CurDateTime();
        // Card_Draw_DailyChange  일일 카드 교체까지 {0}시 {1}분 남음
        tmp_DailyResetDT.text = ProjectUtil.StringFormat("Card_Draw_DailyChange", timeSpan.Hours.ToString("00"), timeSpan.Minutes.ToString("00"));

        resourceBoard_SummonCoin.SetInfo(REWARD_TYPE.RESOURCE, DefineItem.card_summon_coin);
        resourceBoard_Mileage.SetInfo(REWARD_TYPE.RESOURCE, DefineItem.mileage);
        resourceBoard_Dia.SetInfo(REWARD_TYPE.RESOURCE, DefineItem.dia);


        foreach (var item in tweenList)
        {
            item.Play(true);
        }

        cardReddot.UpdateIndex(REDDOT.MAIN_CARD, 0);
    }


    public void InitCardInfo(bool forceGenerate = false)
    {
        // 저장된 카드가 없다
        if (UserDataManager.Instance.summonCardTargetInfo.Count < 1 || forceGenerate == true)
        {
            GenerateCardInfo();            
        }

        // 현재 카드 정보 동기화
        List<int> targetList = UserDataManager.Instance.summonCardTargetInfo.Keys.ToList();
        List<bool> summonedList = UserDataManager.Instance.summonCardTargetInfo.Values.ToList();

        summonCards.Clear();

        for (int i = 0; i < targetList.Count; i++)
        {
            SummonCardData tmp = new SummonCardData();
            tmp.drawIndex = targetList[i];
            tmp.isSummoned = summonedList[i];

            summonCards.Add(tmp);
        }


        RefreshCardInfo();

        // RefreshSummonEXP();

        SetButtonState();
    }

    public void GenerateCardInfo()
    {
        UserDataManager.Instance.GenerateSummonCardInfo();

        #region 유저 카드 데이터 저장
        UserDataManager.Instance.UpdateBuffSync(BUFF_DETAIL_TYPE.CARD_BUFF, true);
        UserDataManager.Instance.UpdateBuffSync(BUFF_DETAIL_TYPE.CARD_SPIECIES_TOTALLEVEL, true);
        UserDataManager.Instance.UpdateBuff();

        UserDataManager.Instance.SaveData(USER_DATA_TYPE.USER_CARD);
        UserDataManager.Instance.SendNetworkSync(true);
        #endregion
    }

    public void SetButtonState()
    {
        int remainCardCount = UserDataManager.Instance.GetSummonableCardCount();

        bool _isSummoning = remainCardCount > 2;

        gameObject_RatioLayout.SetActive(_isSummoning);
        gameObject_GiveupButton.SetActive(!_isSummoning);

        if (remainCardCount == SODataManager.Instance.define.CARD_SHOW_BASE)
        {
            // Card_Draw_Try : 카드 뽑기
            tmp_SummonButtonDesc.text = SODataManager.Instance.GetStringTableValue("Card_Draw_Try");
        }
        else
        {
            // Card_Draw_Try2 : 한번 더 뽑기
            tmp_SummonButtonDesc.text = SODataManager.Instance.GetStringTableValue("Card_Draw_Try2");
        }

        // 뽑을 수 있는 카드가 아직 남아있다.
        if (remainCardCount > 0)
        {
            // Card_Draw_quit : 포기하기
            tmp_GiveupButtonDesc.text = SODataManager.Instance.GetStringTableValue("Card_Draw_quit");
        }
        else
        {
            // Card_Draw_Alldraw : 카드 새로 뽑기
            tmp_GiveupButtonDesc.text = SODataManager.Instance.GetStringTableValue("Card_Draw_Alldraw");
        }
    }

    public void RefreshCardInfo()
    {        
        for (int i = 0; i < summonCards.Count; i++)
        {            
            CardDrawData drawData = SODataManager.Instance.GetCardDrawData(summonCards[i].drawIndex);
            CardData cardData = SODataManager.Instance.GetCardData(drawData.rewardInfo.index);
            EntityData entityData = SODataManager.Instance.GetEntityData(cardData.bonusEntity);            

            Sprite sprite = null;
            sprite = AtlasManager.Instance.GetSprite(entityData.GetResourcePath(1), AtlasType.EntityIcon);

            if (sprite == null)
            {
                sprite = AtlasManager.Instance.GetSprite("ui_icon_question2", AtlasType.GameIcon);
            }            
            summonCardViewDatas[i].cardImage.sprite = sprite;

            summonCardViewDatas[i].cardBG.sprite = AtlasManager.Instance.GetSprite(ProjectUtil.GetSpeciesCardString(entityData.species), AtlasType.UI);

            bool isSummoned = UserDataManager.Instance.summonCardTargetInfo[summonCards[i].drawIndex];            

            summonCardViewDatas[i].isSummoned.gameObject.SetActive(isSummoned);

            summonCardViewDatas[i].isFocus.gameObject.SetActive(false);

            summonCardViewDatas[i].tmp_cardName.text = cardData.GetCardString();

            summonCardViewDatas[i].tmp_count.text = string.Format($"x{drawData.rewardInfo.count * curRatio}");
        }        
    }

    public void OnClicked_SetRatio(int value)
    {
        if (isSummoning == true)
        {
            return;
        }

        if (UserDataManager.Instance.GetSummonableCardCount() != SODataManager.Instance.define.CARD_SHOW_BASE)
        {
            Logger.LogError("카드를 이미 뽑았을 때에는 Ratio를 변경할 수 없습니다.");            
            return;
        }

        curRatio = value;
        UserDataManager.Instance.summon_card_ratio = curRatio;

        foreach (GameObject go in gameObject_RatioSelect)
        {            
            go.SetActive(false);
        }
        gameObject_RatioSelect[curRatio - 1].SetActive(true);

        RefreshCurPrice();

        RefreshCardInfo();
    }

    public void RefreshRerollPrice()
    {
        bool isSummoned = SODataManager.Instance.define.CARD_SHOW_BASE > UserDataManager.Instance.GetSummonableCardCount();

        gameObject_RerollButton.SetActive(isSummoned == false);

        resourceBoard_RerollPrice.SetInfo(
            new RewardInfo(REWARD_TYPE.RESOURCE, DefineItem.dia, UserDataManager.Instance.GetRerollCost()), false, true);
    }

    public void RefreshCurPrice()
    {
        gameObject_SummonButton.SetActive(true);

        int maxSummonCount = SODataManager.Instance.define.CARD_SHOW_BASE;

        int summonableCount = UserDataManager.Instance.GetSummonableCardCount();        
        switch (maxSummonCount - summonableCount)
        {
            // 1회차 뽑기 비용
            case 0:
                {
                    curPrice = new RewardInfo(
                        REWARD_TYPE.RESOURCE, 
                        DefineItem.card_summon_coin, 
                        (ObscuredLong)SODataManager.Instance.define.CARD_GET_BASE);
                }
                break;

            // 2회치 뽑기 비용
            case 1:
                {
                    curPrice = new RewardInfo(
                        REWARD_TYPE.RESOURCE,
                        DefineItem.mileage,
                        (ObscuredLong)SODataManager.Instance.define.CARD_GET_SECOND);
                }
                break;
            // 3회차 뽑기 비용
            case 2:
                {
                    curPrice = new RewardInfo(
                        REWARD_TYPE.RESOURCE,
                        DefineItem.mileage,
                        (ObscuredLong)SODataManager.Instance.define.CARD_GET_THIRD);
                }
                break;
            // 전부 뽑았을 때
            case 3:
                {
                    gameObject_SummonButton.SetActive(false);
                }
                break;
            default:
                {
                    Logger.LogError($"SummonableCount is Error {summonableCount}");
                    return;
                }
        }

        curPrice.count = curPrice.count * curRatio;

        resourceBoard_Price.SetInfo(curPrice.type, curPrice.index, curPrice.count);
    }

    public void OnClicked_RerollSummonTargets()
    {
        if (isSummoning == true)
        {
            return;
        }

        RewardInfo rerollCost = new RewardInfo(REWARD_TYPE.RESOURCE, DefineItem.dia, UserDataManager.Instance.GetRerollCost());

        if (ProjectUtil.IsLackResource(rerollCost, true))
        {
            return;
        }

        // 리롤 비용 차감
        UserDataManager.Instance.AddUserItemData(rerollCost.type, rerollCost.index, rerollCost.count * -1);

        // 아직 카드를 뽑은 적이 없는데, 리롤을 하려고 한다.
        // -> 리롤 횟수를 증가시킨다.
        if (SODataManager.Instance.define.CARD_SHOW_BASE == UserDataManager.Instance.GetSummonableCardCount())
        {
            UserDataManager.Instance.summon_card_reroll_count++;
        }

        // Card_Draw_Alldraw_toast : 진열된 카드를 교체하였습니다.
        UIManager.Instance.ShowToastMessage(SODataManager.Instance.GetStringTableValue("Card_Draw_Alldraw_toast"), Color.white, true); ;

        InitCardInfo(true);        

        RefreshRerollPrice();
    }

    public void OnClicked_GiveupSummon()
    {
        if (isSummoning)
        {
            return;
        }
       
        // 아직 뽑을 수 있는 카드가 남아있다
        if (UserDataManager.Instance.GetSummonableCardCount() > 0)
        {            
            UIManager.Instance.ShowMessageBox(SODataManager.Instance.GetStringTableValue("Card_Draw_quit_Desc").Replace("\\n", "\n"), () =>
            // OK Action
            {
                // Card_Draw_Alldraw_toast : 진열된 카드를 교체하였습니다.
                UIManager.Instance.ShowToastMessage(SODataManager.Instance.GetStringTableValue("Card_Draw_Alldraw_toast"), Color.white, true); ;

                InitCardInfo(true);

                RefreshRerollPrice();

                RefreshCurPrice();

                return;
            },
            () =>
            // Cancel Action
            {
                return;
            }, UIMessageBox.EMessageBoxType.OK_Cancel);
        }
        // 남은 카드가 없을 때는 바로 리롤을 시켜준다.
        else
        {
            // Card_Draw_Alldraw_toast : 진열된 카드를 교체하였습니다.
            UIManager.Instance.ShowToastMessage(SODataManager.Instance.GetStringTableValue("Card_Draw_Alldraw_toast"), Color.white, true); ;

            InitCardInfo(true);

            RefreshRerollPrice();

            RefreshCurPrice();

            return;
        }
    }

    public void OnClicked_Summon()
    {
        if (isSummoning == true)
        {
            return;
        }

        List<int> targetList = UserDataManager.Instance.summonCardTargetInfo.Where(v => v.Value == false).Select(v => v.Key).ToList();

        if (targetList.Count < 1)
        {
            // Card_Draw_NONECARD  뽑을 수 있는 카드가 없습니다.
            UIManager.Instance.ShowToastMessage(SODataManager.Instance.GetStringTableValue("Card_Draw_NONECARD"));
            return;
        }

        if (ProjectUtil.IsLackResource(curPrice, true))
        {
            return;
        }

        isSummoning = true;

        int target = CoffeeLibrary.RandomUtil.GetRandom(targetList);                

        int targetIndex = -1;
        for (int i = 0; i < summonCards.Count; i++)
        {
            if (summonCards[i].drawIndex == target)
            {
                targetIndex = i;
                break;
            }
        }

        #region 재화 차감 및 보상 지급, 유저 데이터 변경 프로세스
        UserDataManager.Instance.AddUserItemData(curPrice.type, curPrice.index, -curPrice.count);

        CardDrawData drawData = SODataManager.Instance.GetCardDrawData(summonCards[targetIndex].drawIndex);
        RewardInfo rewardInfo = new RewardInfo();
        rewardInfo.type = drawData.rewardInfo.type;
        rewardInfo.index = drawData.rewardInfo.index;
        rewardInfo.count = drawData.rewardInfo.count * curRatio;

        UserDataManager.Instance.AddUserItemData(rewardInfo);

        //// 경험치 증가
        //// 뽑은 카드의 개수 만큼 경험치가 증가한다.
        //UserDataManager.Instance.summon_card_exp += (int)rewardInfo.count;

        // 카드 뽑기 상태 변경
        UserDataManager.Instance.SetSummonCard(summonCards[targetIndex].drawIndex);

        // 카드를 뽑으면 교체 횟수는 다시 0으로 돌아간다.
        UserDataManager.Instance.summon_card_reroll_count = 0;

        summonCards[targetIndex].isSummoned = true;
        #endregion

        #region 유저 카드 데이터 저장
        UserDataManager.Instance.UpdateBuffSync(BUFF_DETAIL_TYPE.CARD_BUFF, true);
        UserDataManager.Instance.UpdateBuffSync(BUFF_DETAIL_TYPE.CARD_SPIECIES_TOTALLEVEL, true);
        UserDataManager.Instance.UpdateBuff();

        UserDataManager.Instance.SaveData(USER_DATA_TYPE.USER_CARD);
        UserDataManager.Instance.SendNetworkSync(true);
        #endregion

        StartCoroutine(SummonAnimCO(targetIndex));        
    }

    WaitForSeconds animDelay_15 = new WaitForSeconds(0.15f);
    WaitForSeconds animDelay_30 = new WaitForSeconds(0.3f);
    WaitForSeconds animDelay_50 = new WaitForSeconds(0.5f);
    WaitForSeconds animDelay_70 = new WaitForSeconds(0.7f);

    IEnumerator SummonAnimCO(int targetIndex)
    {
        yield return null;

        int summonableCount = UserDataManager.Instance.GetSummonableCardCount();

        if (summonableCount + 1 > 1)
        {

            List<SummonCardViewData> summonableViewData = new List<SummonCardViewData>();
            for (int i = 0; i < summonCards.Count; i++)
            {
                summonableViewData.Add(summonCardViewDatas[i]);
            }

            for (int i = 0; i < summonCards.Count; i++)
            {
                if (summonCards[i].isSummoned == true && targetIndex != i)
                    continue;

                summonableViewData[i].isFocus.gameObject.SetActive(true);
                yield return animDelay_15;
                summonableViewData[i].isFocus.gameObject.SetActive(false);
            }

            for (int i = 0; i < summonCards.Count; i++)
            {
                if (summonCards[i].isSummoned == true && targetIndex != i)
                    continue;

                summonableViewData[i].isFocus.gameObject.SetActive(true);
                yield return animDelay_30;
                summonableViewData[i].isFocus.gameObject.SetActive(false);
            }

            for (int i = 0; i < summonCards.Count; i++)
            {
                if (summonCards[i].isSummoned == true && targetIndex != i)
                    continue;

                summonableViewData[i].isFocus.gameObject.SetActive(true);
                yield return animDelay_50;
                summonableViewData[i].isFocus.gameObject.SetActive(false);
            }

            for (int i = 0; i < targetIndex; i++)
            {
                if (summonCards[i].isSummoned == true && targetIndex != i)
                    continue;

                summonableViewData[i].isFocus.gameObject.SetActive(true);
                yield return animDelay_70;
                summonableViewData[i].isFocus.gameObject.SetActive(false);
            }
        }

        StartCoroutine(SummonResultCO(targetIndex));
    }

    WaitForSeconds animDelay_100 = new WaitForSeconds(1.5f);

    IEnumerator SummonResultCO(int targetIndex)
    {
        yield return null;

        isSummoning = false;

        CardDrawData drawData = SODataManager.Instance.GetCardDrawData(summonCards[targetIndex].drawIndex);

        RewardInfo rewardInfo = new RewardInfo();
        rewardInfo.type = drawData.rewardInfo.type;
        rewardInfo.index = drawData.rewardInfo.index;
        rewardInfo.count = drawData.rewardInfo.count * curRatio;

        UIManager.Instance.ShowToastRewardMessage(rewardInfo);

        RefreshCurPrice();
        RefreshRerollPrice();

        // RefreshSummonEXP();
        SetButtonState();

        RefreshCardInfo();

        bool isAngel = SODataManager.Instance.GetCardData(rewardInfo.index).species == "angel";

        summonCardViewDatas[targetIndex].isFocus.gameObject.SetActive(true);        
        summonCardViewDatas[targetIndex].angelAnimObject.gameObject.SetActive(isAngel);
        yield return animDelay_100;
        summonCardViewDatas[targetIndex].isFocus.gameObject.SetActive(false);
        summonCardViewDatas[targetIndex].angelAnimObject.gameObject.SetActive(false);
    }

    public void OnClicked_Push_Popup_SummonCardRate()
    {
        UIManager.Instance.PopupLoad(EPopup.Popup_SummonCardRate);
    }

    public void OnClick_Cardbook()
    {
        UIManager.Instance.PopupLoad(EPopup.Popup_CardBook);

        PopPopup();
    }
}
