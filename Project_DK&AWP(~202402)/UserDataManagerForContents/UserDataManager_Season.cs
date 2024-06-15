using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SeasonState
{
    Idle,   // 상태 변경이 없음
    Start,  // 새로 시작됨
    End,    // 진행 중인 시즌이 끝남
    Change, // 시즌이 변경됨

    MAX
}

// 시즌의 상태가 바뀌었을때(시작, 종료, 변경) 데이터 처리를 담당하도록 한다.
public partial class UserDataManager
{
    public void SeasonCheckProcess(SeasonState state, int seasonIndex)
    {
        switch (state)
        {            
            case SeasonState.Idle:
                break;

            case SeasonState.Start:
                SeasonStartProcess(seasonIndex);
                break;

            case SeasonState.End:
                SeasonEndProcess();
                break;

            case SeasonState.Change:
                SeasonChangeProcess();
                break;

            default:
                break;
        }
    }

    public void SeasonStartProcess(int startIndex)
    {
        // 시즌 정보를 먼저 가져오고, 그리고 GenerateSeasonRoullet 및 GenerateSeasonQuest를 실행 후
        // 시즌 룰렛과 퀘스트에 대한 정보를 sync한다.

        // NOTE
        // Generate를 반드시 해야하는가?
        // -> SyncString을 만들 때, dic_reward_info 등 아직 비어있는 데이터로
        // SyncString을 만드는 것을 방지하기 위해, Sync하기 전에 반드시 Generate를 해야한다.
        GenerateSeasonRouletteData();
        GenerateSeasonQuestData();

        AddNetworkSync(USER_DATA_TYPE.USER_SEASON_ROULETTE);
        AddNetworkSync(USER_DATA_TYPE.USER_SEASON_QUEST);

        // 관련 UI가 열려있다면 Refresh 하도록 한다.
        // 상점 - Refresh
        // 시즌 이벤트 ui - 닫히도록(이지만 시즌이 시작되는 경우므로, 해당사항 없음)
        Messenger.Broadcast(EMessengerID.E_SEASON_REFRESH);
        
        string msg = string.Format(ProjectUtil.GetGameString(GAME_STR.MSG_SEASON_NEW),
            SODataManager.Instance.GetStringTableValue(SODataManager.Instance.GetSeasonEventData(startIndex).eventTitle));

        UIManager.Instance.ShowToastMessage(msg);

        //SendNetworkSyncWithCallback(true, false, ()=> 
        //{
        //    // 관련 UI가 열려있다면 Refresh 하도록 한다.
        //    // 상점 - Refresh
        //    // 시즌 이벤트 ui - 닫히도록(이지만 시즌이 시작되는 경우므로, 해당사항 없음)
        //    Messenger.Broadcast(EMessengerID.E_SEASON_REFRESH);
        //});
    }

    public void SeasonEndProcess()
    {
        // 관련 UI가 열려있다면 Refresh 하도록 한다.
        // 상점 - Refresh
        // 시즌 이벤트 ui - 닫히도록
        Messenger.Broadcast(EMessengerID.E_SEASON_REFRESH);

        UIManager.Instance.ShowToastMessage(ProjectUtil.GetGameString(GAME_STR.MSG_SEASON_END));
    }

    public void SeasonChangeProcess()
    {
        // 시즌 정보를 먼저 가져오고, 그리고 GenerateSeasonRoullet 및 GenerateSeasonQuest를 실행 후
        // 시즌 룰렛과 퀘스트에 대한 정보를 sync한다.

        // NOTE
        // Gererate를 시켜야하는가?
        // -> 시즌이 바뀌면서 기존에 있던 시즌 룰렛과 퀘스트 데이터를 초기화하고
        // 다음으로 진행될 시즌 룰렛과 퀘스트 데이터로 채우기 위해서 반드시 해야한다.
        GenerateSeasonRouletteData();
        GenerateSeasonQuestData();

        AddNetworkSync(USER_DATA_TYPE.USER_SEASON_ROULETTE);
        AddNetworkSync(USER_DATA_TYPE.USER_SEASON_QUEST);

        // 관련 UI가 열려있다면 Refresh 하도록 한다.
        // 상점 - Refresh
        // 시즌 이벤트 ui - 닫히도록
        Messenger.Broadcast(EMessengerID.E_SEASON_REFRESH);

        UIManager.Instance.ShowToastMessage(ProjectUtil.GetGameString(GAME_STR.MSG_SEASON_CHANGE));

        //SendNetworkSyncWithCallback(true, false, () =>
        //{
        //    // 관련 UI가 열려있다면 Refresh 하도록 한다.
        //    // 상점 - Refresh
        //    // 시즌 이벤트 ui - 닫히도록
        //    Messenger.Broadcast(EMessengerID.E_SEASON_REFRESH);
        //});
    }    
}
