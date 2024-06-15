using CoffeeLibrary;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SeasonBingo : MonoBehaviour
{
    List<SeasonBoardData> seasonBoardDatas = new List<SeasonBoardData>();
    List<int> userFixRandomDatas = new List<int>();
    List<int> userBingoNumberDatas = new List<int>();
    List<int> randomShakeList = new List<int>();

    [SerializeField] Image image_banner;

    [SerializeField] List<SeasonBingoSlotItem> slotItems = new List<SeasonBingoSlotItem>();

    [SerializeField] List<SeasonBingoLineBonus> lineBonus = new List<SeasonBingoLineBonus>();

    [SerializeField] SeasonBingoLineBonus allBingoBonusIcon;

    SeasonBingoLineBonusInfo allBingoBonusInfo;    

    [SerializeField] Toggle toggle;
    
    [SerializeField] List<UISpineGraphic> lineBonusAnim = new List<UISpineGraphic>();    

    [SerializeField] UISpineGraphic allBingoAnim;

    [SerializeField] SlotMachine slotMachine;

    [SerializeField] ListView_SeasonBingo listView_Bingo;

    [SerializeField] SummonButton summonButton;
    [SerializeField] GameObject gameObject_NextLevelBingoLock;

    [SerializeField] TextMeshProUGUI tmp_seasonLevel;
    [SerializeField] TextMeshProUGUI tmp_TimeInfo;

    RewardInfo priceData;    

    private int season_index = 0;

    /// <summary>
    /// 시즌 빙고에서 SeasonSubLevel은 빙고 횟수를 나타내고,
    /// SeasonLevel은 빙고판 횟수를 나타냅니다.
    /// </summary>
    private int seasonSubLevel = 0;

    private bool isFlippingCard = false;

    void Awake()
    {
        randomShakeList.Clear();
        for (int i = 0; i < CDefineData.max_bingo_slot * 2; i++)
        {
            randomShakeList.Add(i);
        }
        nextBingoDelay = new WaitForSeconds(SODataManager.Instance.define.NEXT_BINGO_DELAY);        
    }

    public void InitInfo(int seasonIndex)
    {
        season_index = seasonIndex;

        InitializeData();
        RefreshCardInfo();

        RefreshListView();
    }

    public bool IsCloseEnable()
    {
        // TODO, 빙고판 돌리자마자, esc 키 눌러서 나가는 경우, 방어해야함
        if (isFlippingCard)
            return false;

        return true;
    }

    private void InitializeData()
    {
        // 시즌 인덱스로 시즌 데이터를 가져옴
        var seasonData = SODataManager.Instance.GetSeasonEventData(season_index);
        if (seasonData == null)
        {
            // 시즌 데이터가 없는 상황에서 해당 UI가 열림
            return;
        }

        // 이벤트 참여용 재화 세팅
        priceData = new RewardInfo();
        priceData.Copy(seasonData.boardRewardPrice);

        // 현재 보상 그룹 인덱스를 가져옴
        int group_index = UserDataManager.Instance.GetSeasonRouletteGroupIndex();
        // 현재 빙고 횟수를 가져옴
        seasonSubLevel = UserDataManager.Instance.userSeasonRoulette.GetSeasonSubLevel();
        // 현재 보상 그룹의 리스트를 가져옴
        seasonBoardDatas = SODataManager.Instance.GetRouletteDataList(season_index, group_index);

        summonButton.priceBoard.SetInfo(priceData, true);

        var timeSpan = UserDataManager.Instance.userSeasonData.seasonEnd_dt - TimeUtil.CurDateTime();
        tmp_TimeInfo.text = ProjectUtil.StringFormat("EVENT_REMAIN_DD_HH", timeSpan.Days.ToString("00"), timeSpan.Hours.ToString("00"));

        image_banner.sprite = seasonData.GetBoardBannerSprite();

        isFlippingCard = false;
        toggle.isOn = false;
    }

    private void RefreshCardInfo()
    {
        // 유저 고정 Random Seed값을 설정합니다.
        // 그리고 해당 값을 기반으로 빙고판의 랜덤 배열을 만듭니다.
        UserFixRandom.Initialize(UserDataManager.Instance.userData.user_pk + UserDataManager.Instance.userSeasonRoulette.GetSeasonLevel());        
        userFixRandomDatas.Clear();
        for (int i = 1; i <= CDefineData.max_bingo_slot * 2; i++)
        {
            userFixRandomDatas.Add(i);
        }
        UserFixRandom.FixShuffle(userFixRandomDatas);

        // 빙고판에 표시할 숫자들을 뽑습니다.
        // 여기에 없는 숫자들은 꽝입니다.
        userBingoNumberDatas.Clear();
        userBingoNumberDatas.AddRange(userFixRandomDatas.GetRange(0, CDefineData.max_bingo_slot));

        // 0번 인덱스는 완료 보상으로 사용하므로 1부터 시작
        for (int i = 1; i < slotItems.Count + 1; i++)
        {
            SeasonBingoSlotInfo info = new SeasonBingoSlotInfo();

            info.index = i;                 
            info.frontIndex = userBingoNumberDatas[i - 1];            
            info.imagePath = seasonBoardDatas[i].resourcePath;            
            info.isFlipped = UserDataManager.Instance.userSeasonRoulette.GetRouletteRewaredState(userBingoNumberDatas[i - 1]);
            info.rewardInfo = seasonBoardDatas[i].reward;

            slotItems[i - 1].InitCardInfo(info);
        }

        // 줄보상 세팅
        for (int i = CDefineData.max_bingo_slot * 2 + 1; i < CDefineData.max_bingo_slot * 2 + (CDefineData.max_bingo_line + 1) * 2 + 1; i++)
        {
            SeasonBingoLineBonusInfo bonusInfo = new SeasonBingoLineBonusInfo();
            bonusInfo.index = i;
            // 줄 보상에는 패키지 보상이 없다.
            // 혹시라도 발생할 null 에러를 방지하기 위해 초기화를 한다.
            bonusInfo.packageInfo = new List<RewardInfo>();
            bonusInfo.rewardInfo = seasonBoardDatas[i].reward;

            // 일단 빙고를 검사하기 전에 LACK_POINT로 세팅을 해둔다.
            if (!UserDataManager.Instance.userSeasonRoulette.GetRouletteRewaredState(i))
                bonusInfo.rewardState = SEASON_GAME_REWARD_STATE.LACK_POINT;
            else
                bonusInfo.rewardState = SEASON_GAME_REWARD_STATE.COMPLETE_REWARD;

            lineBonus[i - CDefineData.max_bingo_slot * 2 - 1].InitBonusCard(bonusInfo);
        }

        // 완성 보상 세팅
        SeasonBingoLineBonusInfo allBonusInfo = new SeasonBingoLineBonusInfo();
        allBonusInfo.index = 0;

        // 패키지 보상이 있는 보상 그룹이라면
        if (UserDataManager.Instance.userSeasonRoulette.IsMaxBoardGroup())
        {
            allBonusInfo.packageInfo = seasonBoardDatas[0].packageReward;            
        }
        allBonusInfo.rewardInfo = seasonBoardDatas[0].reward;


        // 일단 빙고를 검사하기 전에 LACK_POINT로 세팅을 해둔다.
        if (!UserDataManager.Instance.userSeasonRoulette.GetRouletteRewaredState(0))
        {
            int allCnt = 0;

            for (int i = 0; i < CDefineData.max_bingo_slot; i++)
            {
                if (slotItems[i].cardData.isFlipped)
                    allCnt++;
            }

            // 빙고판을 완성한 상태
            if (allCnt == CDefineData.max_bingo_slot)
            {
                allBonusInfo.rewardState = SEASON_GAME_REWARD_STATE.ENABLE_REWARD;
            }
            else
            {
                allBonusInfo.rewardState = SEASON_GAME_REWARD_STATE.LACK_POINT;
            }
        }
        else
            allBonusInfo.rewardState = SEASON_GAME_REWARD_STATE.COMPLETE_REWARD;

        allBingoBonusInfo = allBonusInfo;

        // 패키지 보상의 경우 아이콘을 따로 쓰도록 한다.
        if (UserDataManager.Instance.userSeasonRoulette.IsMaxBoardGroup())
        {            
            allBingoBonusIcon.InitAllBonusInfo(allBingoBonusInfo);
        }
        else
        {
            allBingoBonusIcon.InitBonusCard(allBingoBonusInfo);
        }

        CheckBingo();

        RefreshNextStep();

        // 뽑는 도중에 UI가 종료되어도 다시 UI를 열었을 때 원래 뽑고자하는 숫자 나올 수 있도록 한다.
        slotMachine.InitShake();

        tmp_seasonLevel.text = ProjectUtil.StringFormat("BINGO_COUNT", seasonSubLevel.ToString());
    }

    private void RefreshListView()
    {
        listView_Bingo.InitListView(season_index);
    }

        #region User Action
    public void OnClick_FlipCard()
    {
        if (ProjectUtil.IsLackResource(priceData, true))
        {
            // 비용이 부족
            toggle.isOn = false;
            return;
        }
        if (isFlippingCard)
            return;

        // 1~50까지의 숫자 중에서 아직 뽑지 않은 숫자의 리스트를 가져옴
        List<int> targetList = UserDataManager.Instance.GetSeasonRouletteRewardEnableList(1,
            CDefineData.max_bingo_slot * 2);

        if (targetList.Count < 1 || allBingoBonusInfo.rewardState == SEASON_GAME_REWARD_STATE.ENABLE_REWARD)
        {
            toggle.isOn = false;

            // MSG_ROULETTE_ALL_REWARD 모든 보상을 수령하였습니다.\n보상 정보를 갱신합니다
            UIManager.Instance.ShowMessageBox(ProjectUtil.GetGameString(GAME_STR.MSG_ROULETTE_ALL_REWARD),
            () =>
            {
                UpgradeSeasonBoard();
            },
            () => { }, UIMessageBox.EMessageBoxType.OK);

            return;
        }

        isFlippingCard = true;

        // 아직 뽑지 않은 숫자 중, 랜덤하게 숫자 하나를 뽑음
        int targetFlippableCard = RandomUtil.GetRandom(targetList);

        // 뽑는 도중에 UI가 종료되어도 다시 UI를 열었을 때 원래 뽑고자하는 숫자 나올 수 있도록 한다.
        slotMachine.InitShake();
        // 뽑는 도중에 탭을 변경하거나 게임이 종료될 때를 상정해서
        // 아래에는 애니메이션 부분만을 처리하도록 한다.
        slotMachine.NumberShake(randomShakeList, targetFlippableCard, toggle.isOn, () =>
        {
            // 비용 차감
            UserDataManager.Instance.AddUserItemData(priceData.type, priceData.index, -1 * priceData.count);
            summonButton.priceBoard.SetInfo(priceData, true);

            // 싱크는 바로 진행하도록 한다.
            UserDataManager.Instance.SetSeasonRouletteReward(targetFlippableCard);
            SyncReward(seasonBoardDatas[targetFlippableCard].reward);

            // 뽑은 숫자의 인덱스를 찾음
            int targetIndex = userBingoNumberDatas.IndexOf(targetFlippableCard);

            // 꽝보상 획득시, 즉, userBingoNumberDatas에서 못찾았을 때
            if (CDefineData.max_bingo_slot <= targetIndex || targetIndex == -1)
            {
                // MSG_BINGO_FAIL : "꽝 보상으로 {0} {1}개를 획득하였습니다."
                string msg = string.Format(ProjectUtil.GetGameString(GAME_STR.MSG_BINGO_FAIL),                    
                    ProjectUtil.GetRewardName(seasonBoardDatas[targetFlippableCard].reward.type, seasonBoardDatas[targetFlippableCard].reward.index),
                    seasonBoardDatas[targetFlippableCard].reward.count);

                UIManager.Instance.ShowToastMessage(msg);                

                isFlippingCard = false;

                // 연속 뽑기 토글이 켜져있다면, 다시 뽑기를 준비합니다.
                if (toggle.isOn)
                    StartCoroutine(RepeatFlipCo());

                return;
            }


            // 당첨을 뽑았을 때, 즉 userBingoNumberDatas에서 인덱스를 찾았을 때            
            // 뒤집기 애니메이션이 끝난 후에 빙고 체크와 토스트 메세지가 출력될 수 있도록
            // 콜백을 달아둔다.
            slotItems[targetIndex].StartFlipAnim(() =>
            {
                CheckBingo();                

                isFlippingCard = false;
            });
            UIManager.Instance.ShowToastRewardMessage(slotItems[targetIndex].cardData.rewardInfo);

            slotItems[targetIndex].RefreshCardState(true);

            if (toggle.isOn)
                StartCoroutine(RepeatFlipCo());

        });
    }

    public void OnClick_NextStep()
    {
        if (isFlippingCard)
        {
            // 뽑는 도중에는 넘어갈 수 없도록 한다.
            return;
        }

        bool isLock = true;
        foreach (var v in lineBonus)
        {
            if (v.info.rewardState == SEASON_GAME_REWARD_STATE.COMPLETE_REWARD)
            {
                isLock = false;
                break;
            }
        }
        if (isLock)
        {
            // MSG_NEXT_BINGO_CONDITION : 빙고판 초기화를 위해서는 한 줄 이상 빙고를 완성해야합니다.
            UIManager.Instance.ShowToastMessage(ProjectUtil.GetGameString(GAME_STR.MSG_NEXT_BINGO_CONDITION));
            return;
        }

        toggle.isOn = false;


        // MSG_NEXT_BINGO 빙고판이 초기화됩니다.\n진행하시겠습니까?
        string msg = ProjectUtil.GetGameString(GAME_STR.MSG_NEXT_BINGO);
        UIManager.Instance.ShowMessageBox(msg, () =>
        {
            UpgradeSeasonBoard();
        },
        () => { }, UIMessageBox.EMessageBoxType.OK_Cancel);
    }

    public void OnClick_NextStepLock()
    {
        // MSG_NEXT_BINGO_CONDITION : 빙고판 초기화를 위해서는 한 줄 이상 빙고를 완성해야합니다.
        UIManager.Instance.ShowToastMessage(ProjectUtil.GetGameString(GAME_STR.MSG_NEXT_BINGO_CONDITION));
        return;
    }

    public void OnClick_PackageRewardPopup()
    {
        var args = new Popup_EventReward.Args();
        args.rewardList = allBingoBonusInfo.packageInfo;

        UIManager.Instance.PopupLoad(EPopup.Popup_EventReward, args);
    }
    #endregion

    #region Editor
#if UNITY_EDITOR
    [SerializeField] private int targetNum = 0;

    [NaughtyAttributes.Button]
    public void OnClick_ForceFlipCard()
    {
        if (ProjectUtil.IsLackResource(priceData, true))
        {
            // 비용이 부족
            toggle.isOn = false;
            return;
        }
        if (isFlippingCard)
            return;

        // 1~50까지의 숫자 중에서 아직 뽑지 않은 숫자의 리스트를 가져옴
        List<int> targetList = UserDataManager.Instance.GetSeasonRouletteRewardEnableList(1,
            CDefineData.max_bingo_slot * 2);

        if (targetList.Count < 1 || allBingoBonusInfo.rewardState == SEASON_GAME_REWARD_STATE.ENABLE_REWARD)
        {
            toggle.isOn = false;

            // MSG_ROULETTE_ALL_REWARD 모든 보상을 수령하였습니다.\n보상 정보를 갱신합니다
            UIManager.Instance.ShowMessageBox(ProjectUtil.GetGameString(GAME_STR.MSG_ROULETTE_ALL_REWARD),
            () =>
            {
                UpgradeSeasonBoard();
            },
            () => { }, UIMessageBox.EMessageBoxType.OK);

            return;
        }

        if (!targetList.Contains(targetNum))
            return;

        isFlippingCard = true;

        // 비용 차감
        UserDataManager.Instance.AddUserItemData(priceData.type, priceData.index, -1 * priceData.count);

        summonButton.priceBoard.SetInfo(priceData, true);

        // 아직 뽑지 않은 숫자 중, 랜덤하게 숫자 하나를 뽑음
        int targetFlippableCard = targetNum;

        List<int> randomShakeList = new List<int>();
        for (int i = 0; i < CDefineData.max_bingo_slot * 2; i++)
        {
            randomShakeList.Add(i);
        }

        // 싱크는 바로 진행하도록 한다.
        UserDataManager.Instance.SetSeasonRouletteReward(targetFlippableCard);
        SyncReward(seasonBoardDatas[targetFlippableCard].reward);

        // 뽑는 도중에 탭을 변경하거나 게임이 종료될 때를 상정해서
        // 아래에는 애니메이션 부분만을 처리하도록 한다.
        slotMachine.NumberShake(randomShakeList, targetFlippableCard, toggle.isOn, () =>
        {
            // 뽑은 숫자의 인덱스를 찾음
            int targetIndex = userBingoNumberDatas.IndexOf(targetFlippableCard);

            // 꽝보상 획득시, 즉, userBingoNumberDatas에서 못찾았을 때
            if (CDefineData.max_bingo_slot <= targetIndex || targetIndex == -1)
            {
                // MSG_BINGO_FAIL : "꽝 보상으로 {0} {1}개를 획득하였습니다."
                string msg = ProjectUtil.StringFormat("MSG_BINGO_FAIL", 
                    ProjectUtil.GetRewardName(seasonBoardDatas[targetFlippableCard].reward.type, seasonBoardDatas[targetFlippableCard].reward.index),
                    seasonBoardDatas[targetFlippableCard].reward.count.ToString() );

                UIManager.Instance.ShowToastMessage(msg);

                isFlippingCard = false;

                // 연속 뽑기 토글이 켜져있다면, 다시 뽑기를 준비합니다.
                if (toggle.isOn)
                    StartCoroutine(RepeatFlipCo());

                return;
            }


            // 당첨을 뽑았을 때, 즉 userBingoNumberDatas에서 인덱스를 찾았을 때
            slotItems[targetIndex].RefreshCardState(true);
            // 뒤집기 애니메이션이 끝난 후에 빙고 체크와 토스트 메세지가 출력될 수 있도록
            // 콜백을 달아둔다.
            slotItems[targetIndex].StartFlipAnim(() =>
            {
                CheckBingo();

                UIManager.Instance.ShowToastRewardMessage(slotItems[targetIndex].cardData.rewardInfo);

                isFlippingCard = false;
            });

            if (toggle.isOn)
                StartCoroutine(RepeatFlipCo());

        });
    }

    [NaughtyAttributes.Button]
    public void ForceNextBingo()
    {
        if (isFlippingCard)
            return;

        // MSG_NEXT_BINGO 빙고판이 초기화됩니다.\n진행하시겠습니까?
        string msg = ProjectUtil.GetGameString(GAME_STR.MSG_NEXT_BINGO);
        UIManager.Instance.ShowMessageBox(msg, () =>
        {
            UpgradeSeasonBoard();
        },
        () => { }, UIMessageBox.EMessageBoxType.OK_Cancel);
    }

    public int ForceLineTarget = 0;
    [NaughtyAttributes.Button]
    public void ForceLineEffect()
    {
        List<int> targetAnim = new List<int>();
        int maxLine = CDefineData.max_bingo_line;
        // 가로 빙고
        if (ForceLineTarget < maxLine)
        {
            for (int i = 0; i < maxLine; i++)
            {
                if (!targetAnim.Contains(ForceLineTarget * maxLine + i))
                    targetAnim.Add(ForceLineTarget * maxLine + i);
            }
        }
        // 세로 빙고
        else if (ForceLineTarget < maxLine * 2)
        {
            for (int i = 0; i < maxLine; i++)
            {
                if (!targetAnim.Contains((ForceLineTarget - maxLine) + i * maxLine))
                    targetAnim.Add((ForceLineTarget - maxLine) + i * maxLine);
            }
        }
        // 우상향 대각선 빙고
        else if (ForceLineTarget == maxLine * 2)
        {
            for (int i = maxLine - 1; i >= 0; i--)
            {
                if (!targetAnim.Contains((maxLine - 1) * (i + 1)))
                    targetAnim.Add((maxLine - 1) * (i + 1));
            }
        }
        // 우하향 대각선 빙고
        else if (ForceLineTarget == maxLine * 2 + 1)
        {
            for (int i = 0; i < maxLine; i++)
            {
                if (!targetAnim.Contains(i * maxLine + i))
                    targetAnim.Add(i * maxLine + i);
            }
        }

        Sound.Play(ePlaySoundType.ePST_EFFECT, SoundData.UI_Accept);
        for (int i = targetAnim.Count - 1; i >= 0; i--)
        {
            slotItems[targetAnim[i]].StartBingoAnim(targetAnim.Count - 1 - i, null);
        }


        var bingoSeq = DOTween.Sequence();
        bingoSeq.AppendInterval(0.1f * 5);

        bingoSeq.AppendCallback(() => {
            lineBonusAnim[ForceLineTarget].gameObject.SetActive(true);
            lineBonusAnim[ForceLineTarget].PlayAnimation("start", true, 1.0f, string.Empty );
        });
        bingoSeq.AppendInterval(1.5f);
        bingoSeq.AppendCallback(() => {
            lineBonusAnim[ForceLineTarget].gameObject.SetActive(false);
        });
    }
#endif
    #endregion

    WaitForSeconds nextBingoDelay;

    IEnumerator RepeatFlipCo()
    {
        yield return new WaitUntil(() => !isFlippingCard);

        // 꽝을 뽑았을때에는 빙고칸을 뒤집지 않으므로,
        // 너무 빠르게 다음 뽑기를 진행하는 것을 방지하기 위해
        // 조금의 유예시간을 두도록 한다.
        yield return nextBingoDelay;

        // 토글을 껐을때 한번 더 뽑는거를 방지하기 위해 추가
        if (toggle.isOn)
            OnClick_FlipCard();

        yield return null;
    }

    /// <summary>
    /// 빙고 검사 로직
    /// </summary>
    private void CheckBingo()
    {
        int maxLine = CDefineData.max_bingo_line;

        int allCnt = 0;

        // 세로 빙고 검사
        for (int i = 0; i < maxLine; i++)
        {
            int cnt = 0;
            for (int j = 0; j < maxLine; j++)
            {
                if (!slotItems[maxLine * j + i].cardData.isFlipped)
                    break;

                cnt++;
                allCnt++;
            }

            if (cnt >= maxLine)
                Bingo(maxLine + i);
        }

        // 가로 빙고 검사
        for (int i = 0; i < maxLine; i++)
        {
            int cnt = 0;
            for (int j = 0; j < maxLine; j++)
            {
                if (!slotItems[maxLine * i + j].cardData.isFlipped)
                    break;

                cnt++;
            }
            if (cnt >= maxLine)
                Bingo(i);
        }
        // 우하향 대각선
        int dCnt = 0;
        for (int i = 0; i < maxLine; i++)
        {
            if (!slotItems[i * maxLine + i].cardData.isFlipped)
                break;

            dCnt++;
            if (dCnt >= maxLine)
                Bingo(maxLine * 2 + 1);
        }

        dCnt = 0;
        // 우상향 대각선
        for (int i = 0; i < maxLine; i++)
        {
            if (!slotItems[(i + 1) * (maxLine - 1)].cardData.isFlipped)
                break;

            dCnt++;
            if (dCnt >= maxLine)
                Bingo(maxLine * 2);
        }

        if (allCnt >= CDefineData.max_bingo_slot)
        {
            AllBingo();
        }

        // BINGO_COUNT : {0}회 빙고 완성
        tmp_seasonLevel.text = ProjectUtil.StringFormat("BINGO_COUNT", seasonSubLevel.ToString());
    }

    /// 줄빙고의 Index
    /// 11 5  6  7  8  9
    /// 0
    /// 1
    /// 2
    /// 3
    /// 4
    /// 10
    /// <summary>
    /// 빙고가 되었을 때 수행하는 로직
    /// </summary>
    /// <param name="line"></param>
    private void Bingo(int line)
    {        
        if (lineBonus.Count > line)
        {
            // 아직 보상을 받지 않은 줄의 빙고가 되었다면
            // ENABLE_REWARD로 바꾼다.            
            if (!UserDataManager.Instance.userSeasonRoulette.GetRouletteRewaredState(line + (CDefineData.max_bingo_slot * 2) + 1))
            {
                lineBonus[line].RefreshInfo(SEASON_GAME_REWARD_STATE.ENABLE_REWARD);
                // 줄 보상 지급
                ReceiveLineBonus(line);

                // 유저 데이터에 빙고 횟수 추가
                UserDataManager.Instance.userSeasonRoulette.LevelupSeasonSubLevel();
                seasonSubLevel = UserDataManager.Instance.userSeasonRoulette.GetSeasonSubLevel();
                // 빙고 횟수 보상 지급
                ReceiveSubLevelReward(seasonSubLevel);
            }

            // 보상을 이미 받은 줄의 빙고라면
            // COMPLETE_REWARD로 바꾼다.
            else
                lineBonus[line].RefreshInfo(SEASON_GAME_REWARD_STATE.COMPLETE_REWARD);
        }
    }

    /// <summary>
    /// 빙고판 완료 보상 검사
    /// </summary>
    private void AllBingo()
    {
        // 아직 보상을 받지 않고 전체 칸을 채웠다면
        // ENABLE_REWARD로 바꾸고, Spine anim을 재생한다.      
        if (!UserDataManager.Instance.userSeasonRoulette.GetRouletteRewaredState(0))
        {
            // 완료 보상 애니메이션은 딱 1번만 재생하도록 한다.
            if (allBingoBonusInfo.rewardState != SEASON_GAME_REWARD_STATE.ENABLE_REWARD)
            {
                allBingoBonusInfo.rewardState = SEASON_GAME_REWARD_STATE.ENABLE_REWARD;

                var bingoSeq = DOTween.Sequence();

                bingoSeq.AppendCallback(() =>
                {
                    allBingoAnim.gameObject.SetActive(true);
                    allBingoAnim.PlayAnimation("looping", true);
                });
                bingoSeq.AppendInterval(2.0f);
                bingoSeq.AppendCallback(() =>
                {
                    allBingoAnim.gameObject.SetActive(false);
                });
            }
        }

        // 보상을 이미 받았다면
        // COMPLETE_REWARD로 바꾼다.
        else
            allBingoBonusInfo.rewardState = SEASON_GAME_REWARD_STATE.COMPLETE_REWARD;
    }

    public void ReceiveLineBonus(int index)
    {
        // 보상을 받을 수 있는 상태가 아니라면 return
        if (lineBonus[index].info.rewardState != SEASON_GAME_REWARD_STATE.ENABLE_REWARD)
            return;
        
        // 획득한 룰렛 데이터 저장 및 연출
        UserDataManager.Instance.userSeasonRoulette.SetRouletteReward(index + CDefineData.max_bingo_slot * 2 + 1);

        // 데이터 싱크
        var rewardData = lineBonus[index].info.rewardInfo;

        SyncReward(rewardData);

        UIManager.Instance.ShowToastRewardMessage(rewardData, true);

        #region Bingo Animation

        List<int> targetAnim = new List<int>();
        int maxLine = CDefineData.max_bingo_line;
        // 가로 빙고
        if (index < maxLine)
        {
            for (int i = 0; i < maxLine; i++)
            {
                if (!targetAnim.Contains(index * maxLine + i))
                    targetAnim.Add(index * maxLine + i);
            }
        }
        // 세로 빙고
        else if (index < maxLine * 2)
        {
            for (int i = 0; i < maxLine; i++)
            {
                if (!targetAnim.Contains((index - maxLine) + i * maxLine))
                    targetAnim.Add((index - maxLine) + i * maxLine);
            }
        }
        // 우상향 대각선 빙고
        else if (index == maxLine * 2)
        {
            for (int i = maxLine - 1; i >= 0; i--)
            {
                if (!targetAnim.Contains((maxLine - 1) * (i + 1)))
                    targetAnim.Add((maxLine - 1) * (i + 1));
            }
        }
        // 우하향 대각선 빙고
        else if (index == maxLine * 2 + 1)
        {
            for (int i = 0; i < maxLine; i++)
            {
                if (!targetAnim.Contains(i * maxLine + i))
                    targetAnim.Add(i * maxLine + i);
            }
        }

        Sound.Play(ePlaySoundType.ePST_EFFECT, SoundData.UI_Accept);
        for (int i = targetAnim.Count - 1; i >= 0; i--)
        {
            slotItems[targetAnim[i]].StartBingoAnim(targetAnim.Count - 1 - i, null);
        }

        var bingoSeq = DOTween.Sequence();
        bingoSeq.AppendInterval(0.1f * 5);

        bingoSeq.AppendCallback(() => {
            lineBonusAnim[index].gameObject.SetActive(true);
            lineBonusAnim[index].PlayAnimation("looping", false, 1.0f, string.Empty );
        });
        bingoSeq.AppendInterval(2.5f);
        bingoSeq.AppendCallback(() => {
            lineBonusAnim[index].gameObject.SetActive(false);
        });

        #endregion

        lineBonus[index].RefreshInfo(SEASON_GAME_REWARD_STATE.COMPLETE_REWARD);

        RefreshNextStep();
    }

    /// <summary>
    /// 빙고 횟수 보상 수령 프로세스
    /// </summary>
    /// <param name="level"></param>
    private void ReceiveSubLevelReward(int level)
    {        
        // 현재 빙고 횟수의 보상을 가져온다.
        var rewardData = SODataManager.Instance.GetSeasonLevelReward(season_index, level);
        
        // 받을 수 있는 빙고 횟수 보상이 있을 경우
        // 바로 지급한다.
        if (rewardData != null)
        {
            SyncReward(rewardData.levelReward);

            var args = new Popup_Get.Args();
            args.title = ProjectUtil.StringFormat("GET_BINGO_LINE_REWARD", seasonSubLevel.ToString());

            args.rewardList.Clear();
            args.rewardList.Add(rewardData.levelReward);
            UIManager.Instance.PopupLoad(EPopup.Popup_Get, args);

            toggle.isOn = false;

            RefreshListView();
        }
    }

    /// <summary>
    /// 빙고판 완료 보상 프로세스
    /// 보상 수령 및 싱크는 return 값으로 실행한다.
    /// </summary>
    private List<RewardInfo> GetAllBingoReward()
    {
        // 보상을 받을 수 있는 상태가 아니라면 return
        if (allBingoBonusInfo.rewardState != SEASON_GAME_REWARD_STATE.ENABLE_REWARD)
            return null;

        List<RewardInfo> ret = new List<RewardInfo>();

        // 패키지 보상 지급의 경우
        if (UserDataManager.Instance.userSeasonRoulette.IsMaxBoardGroup())
        {
            var rewardData = allBingoBonusInfo.packageInfo;
            if (rewardData != null)
            {
                foreach (var v in rewardData)
                {                
                    ret.Add(v);
                }

                toggle.isOn = false;
            }
        }
        else
        {
            var rewardData = allBingoBonusInfo.rewardInfo;
            if (rewardData != null)
            {
                ret.Add(rewardData);
                toggle.isOn = false;
            }            
        }
        return ret;
    }

    // 다음 빙고로 갈 수 있는지 체크
    private void RefreshNextStep()
    {
        bool isLock = true;
        foreach (var v in lineBonus)
        {
            if (v.info.rewardState == SEASON_GAME_REWARD_STATE.COMPLETE_REWARD)
            {
                isLock = false;
                break;
            }
        }
        
        gameObject_NextLevelBingoLock.SetActive(isLock);
    }

    /// <summary>
    /// 다음 빙고판으로 넘어갈 때 해야할 로직을 처리합니다.
    /// 받아야할 빙고판 완료 보상이 있다면 받고, SeasonLevel 세팅, 보상 정보 세팅 등을 완료 후에
    /// Sync를 요청합니다.
    /// </summary>
    private void UpgradeSeasonBoard()
    {
        List<RewardInfo> allBingoRewardList = GetAllBingoReward();        

        UserDataManager.Instance.UpgradeSeasonRouletteBoard();

        UserDataManager.Instance.userSeasonRoulette.LevelupSeasonLevel();

        InitializeData();
        RefreshCardInfo();

        // 빙고판을 완성했을 때만 완성 보상을 받도록한다.
        if (allBingoRewardList != null && allBingoRewardList.Count > 0)
        {
            // 데이터 싱크
            var syncList = new List<USER_DATA_TYPE>();
            syncList.Add(USER_DATA_TYPE.USER_SEASON_ROULETTE);
            foreach (var syncTarget in allBingoRewardList)
            {
                syncList.Add(DefineReward.GetSyncType(syncTarget.type));
                UserDataManager.Instance.AddUserItemData(syncTarget);
            }
            // 저장(Save)
            UserDataManager.Instance.SaveData(syncList.Distinct().ToList());
            UserDataManager.Instance.SendNetworkSync();

            var args = new Popup_Get.Args();
            args.title = ProjectUtil.GetGameString(GAME_STR.BINGO_COMPLETE_REWARD);
            args.rewardList.Clear();
            args.rewardList.AddRange(allBingoRewardList);

            UIManager.Instance.PopupLoad(EPopup.Popup_Get, args);
        }
        // 완성 보상을 받지 못할 때에는 UserSeasonRoullet 데이터만 싱크한다.
        else
        {
            // 저장(Save)
            UserDataManager.Instance.SaveData(USER_DATA_TYPE.USER_SEASON_ROULETTE);
            UserDataManager.Instance.SendNetworkSync();
        }

        RefreshListView();
    }

    /// <summary>
    /// 싱크와 아이템 지급을 담당합니다.
    /// </summary>
    /// <param name="rewardList"></param>
    private void SyncReward(RewardInfo reward)
    {
        // 데이터 싱크
        var syncList = new List<USER_DATA_TYPE>();
        syncList.Add(USER_DATA_TYPE.USER_SEASON_ROULETTE);

        syncList.Add(DefineReward.GetSyncType(reward.type));
        UserDataManager.Instance.AddUserItemData(reward);
        
        // 저장(Save)
        UserDataManager.Instance.SaveData(syncList.Distinct().ToList());
        UserDataManager.Instance.SendNetworkSync();
    }
}
