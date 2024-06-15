using CoffeeLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SeasonMarble : MonoBehaviour
{
    [SerializeField] Image image_banner;

    [SerializeField] CommonPoolController poolCtrl;

    [SerializeField] Transform T_Layout;
    [SerializeField] Transform B_Layout;
    [SerializeField] Transform L_Layout;
    [SerializeField] Transform R_Layout;

    [SerializeField] GameObject pawn;

    [SerializeField] GameObject gameobject_TimeInfo;
    [SerializeField] TextMeshProUGUI TMP_TimeInfo;

    [SerializeField] SummonButton summonButton;
    [SerializeField] Toggle toggle;
    [SerializeField] GameObject gameobject_NextStep;
    [SerializeField] GameObject gameobject_NextStepLock;

    [SerializeField] TextMeshProUGUI tmp_seasonLevel;    

    [SerializeField] List<MarbleRewardItem> rewardIconList = new List<MarbleRewardItem>();
    
    [SerializeField] List<GameObject> list_Dice;

    [SerializeField] GameObject gameObject_Levelup;

    // 룰렛에 필요한 아이콘 보드 세팅
    private bool IsInit = false;
    // 소환중 체크 여부
    public bool IsSummonIng { get; private set; } = false;

    private int season_index = 0;

    private int currentPos;

    RewardInfo priceData = new RewardInfo()
    {
        type = REWARD_TYPE.RESOURCE,
        index = DefineItem.dia,
        count = 1000
    };

    Coroutine coRewardProcess;

    public void InitInfo(int season_index)
    {
        this.season_index = season_index;
        var seasonData = SODataManager.Instance.GetSeasonEventData(season_index);

        var timeSpan = UserDataManager.Instance.userSeasonData.seasonEnd_dt - TimeUtil.CurDateTime();
        TMP_TimeInfo.text = ProjectUtil.StringFormat("EVENT_REMAIN_DD_HH", timeSpan.Days.ToString("00"), timeSpan.Hours.ToString("00"));

        priceData.Copy(seasonData.boardRewardPrice);

        image_banner.sprite = seasonData.GetBoardBannerSprite();

        tmp_seasonLevel.text = string.Format(SODataManager.Instance.GetStringTableValue("PASS_SEASON_MARBLE_STARTCOUNTING"), UserDataManager.Instance.userSeasonRoulette.GetSeasonLevel());     

        foreach (GameObject go in list_Dice)
        {
            go.SetActive(false);
        }

        gameObject_Levelup.SetActive(false);

        InitSumonState();

        RefreshPrice();

        InitReward();

        InitPos();
    }

    void InitSumonState()
    {
        toggle.isOn = false;

        IsSummonIng = false;

        if (coRewardProcess != null)
            StopCoroutine(coRewardProcess);
    }

    void InitReward()
    {
        List<int> posLevelList = UserDataManager.Instance.userSeasonRoulette.GetRewardPosLevelList();

        // 정상적인 데이터가 세팅되었는지 체크하는 로직
        var seasonData = UserDataManager.Instance.userSeasonData.GetSeasonEventData();
        var seasonEventData = SODataManager.Instance.GetSeasonEventData(seasonData.index);
        
        List<int> boardGroup = seasonEventData.boardRewardGroup;
        bool needInit = false;
        for (int i = 0; i < posLevelList.Count; i++)
        {
            // 테이블에 세팅되어있던 데이터가 없다 -> 아직 제대로 초기화가 안되었다.
            if (boardGroup.Contains(posLevelList[i]) == false)
            {
                needInit = true;
                break;
            }            
        }

        if (needInit)
        {
            for (int i = 0; i < posLevelList.Count; i++)
            {
                posLevelList[i] = seasonEventData.boardRewardGroup.FirstOrDefault();
            }

            UserDataManager.Instance.userSeasonRoulette.SetRewardPosIndexList(posLevelList);
            UserDataManager.Instance.userSeasonRoulette.season_level = 0;
            currentPos = UserDataManager.Instance.userSeasonRoulette.currentPos = 0;

            // 저장(Save)
            UserDataManager.Instance.SaveData(USER_DATA_TYPE.USER_SEASON_ROULETTE);
            UserDataManager.Instance.SendNetworkSync();
        }

        List<SeasonBoardData> rewardList = new List<SeasonBoardData>();

        for (int i = 0; i < posLevelList.Count; i++)
        {
            rewardList.Add(SODataManager.Instance.GetRouletteData(posLevelList[i], i));
        }

        for (int i = 0; i < rewardIconList.Count; i++)
        {
            int level = boardGroup.IndexOf(posLevelList[i]);

            if (i != 0)
                rewardIconList[i].InitInfo(rewardList[i], false, level);

            // 첫번째 칸은 레벨이 없다.
            else
                rewardIconList[i].InitFixedInfo(rewardList[i], false);
        }
    }

    public void InitPos()
    {
        currentPos = UserDataManager.Instance.userSeasonRoulette.currentPos;
        
        pawn.transform.SetParent(rewardIconList[currentPos].gameObject.transform);
        pawn.transform.localPosition = Vector3.zero;
        pawn.transform.localScale = Vector3.one;
    }

    public bool IsCloseEnable()
    {
        if (IsSummonIng == true)
            return false;

        return true;
    }

    public void OnClicked_Summon()
    {
        // 시즌이 만료되었는가?
        if (UserDataManager.Instance.userSeasonData.IsShopExpire())
        {
            /// 메세지 박스를 문구와 함께 띄워야한다
            UIManager.Instance.ShowMessageBox(ProjectUtil.GetGameString(GAME_STR.MSG_SEASON_NO_BUY), () =>
            // Check Action
            {
                // 팝업을 닫게할 것인지, 아니면 팝업을 Refresh 할 것인지는 브로드 캐스팅을 받는 쪽에서 정한다.
                UserDataManager.Instance.ReqSyncAll(false, () =>
                {
                    Messenger.Broadcast(EMessengerID.E_SEASON_REFRESH);
                });
            },
            // Cancle Action
            () => { },
            UIMessageBox.EMessageBoxType.OK);
            return;
        }

        // 비용 이 부족합니까?
        if (ProjectUtil.IsLackResource(priceData, true))
        {
            toggle.isOn = false;
            Logger.Log("소환을 위한 비용이 부족합니다");
            return;
        }

        if (IsSummonIng)
        {
            Logger.Log("이미 소환이 진행중입니다");
            return;
        }

        IsSummonIng = true;

        StartCoroutine(AnimProcessCO());
    }

    WaitForSeconds animDelay = new WaitForSeconds(0.6f);

    IEnumerator AnimProcessCO()
    {
        int rollNumber = UnityEngine.Random.Range(1, 7);
        
        list_Dice[rollNumber - 1].SetActive(true);
        yield return animDelay;
        list_Dice[rollNumber - 1].SetActive(false);

        if (coRewardProcess != null)
            StopCoroutine(coRewardProcess);

        coRewardProcess = StartCoroutine(RewardProcessCo(rollNumber));

        yield return null;
    }


    WaitForSeconds delay = new WaitForSeconds(0.1f);

    IEnumerator RewardProcessCo(int rollNumber)    
    {        
        List<int> focusIndex = new List<int>();
        for (int i = 1; i < rollNumber; i++)
        {
            if (currentPos + i >= CDefineData.max_marble_slot)
            {
                focusIndex.Add(currentPos + i - CDefineData.max_marble_slot);
            }
            else
            {
                focusIndex.Add(currentPos + i);
            }
        }        

        while (this.gameObject.activeSelf)
        {            
            for (int i = 0; i < focusIndex.Count; i++)
            {
                rewardIconList[focusIndex[i]].PlayFocusEffect();
                
                pawn.transform.SetParent(rewardIconList[focusIndex[i]].gameObject.transform);
                pawn.transform.localPosition = Vector3.zero;
                pawn.transform.localScale = Vector3.one;

                yield return delay;
            }

            ShowRewardProcess(rollNumber);
            yield break;
        }

        yield return null;
    }

    private void ShowRewardProcess(int rollNumber)
    {
        // 비용 차감
        UserDataManager.Instance.AddUserItemData(priceData.type, priceData.index, -1 * priceData.count);

        List<int> posLevelList = UserDataManager.Instance.userSeasonRoulette.GetRewardPosLevelList();

        List<RewardInfo> rewardList = new List<RewardInfo>();

        currentPos += rollNumber;

        bool forceRewardMessage = false;

        // 완주 보상 지급
        if (currentPos >= CDefineData.max_marble_slot)
        {            
            int nextPos = currentPos - CDefineData.max_marble_slot;
            if (nextPos != 0)
            {                
                SeasonBoardData boardData = SODataManager.Instance.GetRouletteData(
                    posLevelList[0], 0);

                if (rewardIconList[0].IsPackageReward())
                {
                    rewardList.AddRange(boardData.packageReward);
                }
                else
                {
                    rewardList.Add(boardData.reward);
                }                
            }

            currentPos = currentPos - CDefineData.max_marble_slot;

            UserDataManager.Instance.userSeasonRoulette.LevelupSeasonLevel();
            tmp_seasonLevel.text = string.Format(SODataManager.Instance.GetStringTableValue("PASS_SEASON_MARBLE_STARTCOUNTING"), UserDataManager.Instance.userSeasonRoulette.GetSeasonLevel());

            forceRewardMessage = true;
        }
        UserDataManager.Instance.userSeasonRoulette.currentPos = currentPos;

        // 현재 위치 칸 레벨업
        bool needLevelupEffect = UserDataManager.Instance.userSeasonRoulette.LevelupRewardPos(currentPos);
        
        pawn.transform.SetParent(rewardIconList[currentPos].gameObject.transform);
        pawn.transform.localPosition = Vector3.zero;
        pawn.transform.localScale = Vector3.one;

        if (rewardIconList[currentPos].IsPackageReward() || rewardList.Count > 0)
        {            
            rewardList.AddRange(rewardIconList[currentPos].GetPackageRewardData());

            SyncReward(rewardList);
        }
        else
        {
            rewardList.Add(rewardIconList[currentPos].GetRewardData());

            SyncReward(rewardList);
        }

        if (rewardList.Count > 1 || forceRewardMessage)
        {            
            Popup_Get.Args args = new Popup_Get.Args();
            args.title = ProjectUtil.GetGameString(GAME_STR.GET_MSG);
            args.rewardList.AddRange(rewardList);
            args.useCustomHideTime = true;
            args.customHideTime = 3.0f;
            UIManager.Instance.PopupLoad(EPopup.Popup_Get, args);
        }
        else
        {
            UIManager.Instance.ShowToastRewardMessage(rewardList.FirstOrDefault());
        }

        rewardIconList[currentPos].PlayFocusEffect();
        if (needLevelupEffect && currentPos != 0)
        {
            StartCoroutine(LevelupEffectCO());
        }  
                
        RefreshPrice();

        if (toggle.isOn)
        {
            EndSummmon();
            Invoke(nameof(OnClicked_Summon), 0.1f);
        }
        else
        {
            Invoke(nameof(EndSummmon), 0.1f);
        }
    }

    WaitForSeconds levelupDelay = new WaitForSeconds(0.7f);
    static WaitForEndOfFrame levelupEndFrame = new WaitForEndOfFrame();

    IEnumerator LevelupEffectCO()
    {
        gameObject_Levelup.SetActive(false);
        yield return levelupEndFrame;
        gameObject_Levelup.transform.SetParent(rewardIconList[currentPos].gameObject.transform);
        gameObject_Levelup.transform.localPosition = Vector3.zero;
        gameObject_Levelup.transform.localScale = Vector3.one * 25.0f;

        RefreshCurrentPos();

        gameObject_Levelup.SetActive(true);
        yield return levelupDelay;
        gameObject_Levelup.SetActive(false);        
    }

    public void RefreshCurrentPos()
    {
        List<int> posLevelList = UserDataManager.Instance.userSeasonRoulette.GetRewardPosLevelList();

        var seasonData = UserDataManager.Instance.userSeasonData.GetSeasonEventData();
        var seasonEventData = SODataManager.Instance.GetSeasonEventData(seasonData.index);

        int level = seasonEventData.boardRewardGroup.IndexOf(posLevelList[currentPos]);

        // 레벨업한 칸 동기화        
        if (currentPos != 0)
            rewardIconList[currentPos].InitInfo(SODataManager.Instance.GetRouletteData(posLevelList[currentPos], currentPos), false, level);

        // 첫번째 칸의 레벨은 없다.
        else
            rewardIconList[currentPos].InitFixedInfo(SODataManager.Instance.GetRouletteData(posLevelList[currentPos], currentPos), false);
    }

    void EndSummmon()
    {
        IsSummonIng = false;
    }

    void RefreshPrice()
    {
        summonButton.priceBoard.SetInfo(priceData, true);
    }

    /// <summary>
    /// 싱크와 아이템 지급을 담당합니다.
    /// </summary>
    /// <param name="rewardList"></param>
    private void SyncReward(RewardInfo reward, List<USER_DATA_TYPE> syncDatas = null)
    {
        // 데이터 싱크
        var syncList = new List<USER_DATA_TYPE>();
        syncList.Add(USER_DATA_TYPE.USER_SEASON_ROULETTE);

        syncList.Add(DefineReward.GetSyncType(reward.type));

        if (syncDatas != null)
        {
            foreach (USER_DATA_TYPE dataType in syncDatas)
            {
                syncList.Add(dataType);
            }
        }

        UserDataManager.Instance.AddUserItemData(reward);

        // 저장(Save)
        UserDataManager.Instance.SaveData(syncList.Distinct().ToList());
        UserDataManager.Instance.SendNetworkSync();
    }

    /// <summary>
    /// 싱크와 아이템 지급을 담당합니다.
    /// </summary>
    /// <param name="rewardList"></param>
    private void SyncReward(List<RewardInfo> reward, List<USER_DATA_TYPE> syncDatas = null)
    {
        // 데이터 싱크
        var syncList = new List<USER_DATA_TYPE>();
        syncList.Add(USER_DATA_TYPE.USER_SEASON_ROULETTE);

        foreach (RewardInfo r in reward)
        {
            syncList.Add(DefineReward.GetSyncType(r.type));
            UserDataManager.Instance.AddUserItemData(r);
        }

        if (syncDatas != null)
        {
            foreach (USER_DATA_TYPE dataType in syncDatas)
            {
                syncList.Add(dataType);
            }
        }

        // 저장(Save)
        UserDataManager.Instance.SaveData(syncList.Distinct().ToList());
        UserDataManager.Instance.SendNetworkSync();
    }
}
