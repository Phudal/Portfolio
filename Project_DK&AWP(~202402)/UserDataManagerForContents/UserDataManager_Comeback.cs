using CodeStage.AntiCheat.ObscuredTypes;
using CoffeeLibrary;
using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[System.Serializable]
public class UserComebackData
{
    public ObscuredInt index; 

    public DateTime user_comebackEnd_dt;

    public bool isFirst;

    public void Clear()
    {
        index = 0;

        isFirst = false;

        user_comebackEnd_dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    }    

    public string GetComebackType()
    {
        ComebackEventData comebackData = GetComebackEventData();
        if (comebackData == null)
            return string.Empty;

        return comebackData.comebackEventType;
    }
    
    public ComebackEventData GetComebackEventData()
    {
        return SODataManager.Instance.GetComebackEventData(index);
    }

    public bool IsComebackExpire()
    {                
        DateTime curDt = TimeUtil.CurDateTime();
        var timeSpan = curDt - user_comebackEnd_dt;

        // 현재 시간이 만료기간보다 크다 => 이미 만료 기한이 지났다
        return timeSpan.TotalSeconds > 0;
    }

    public bool IsComebackQuestExpire()
    {
        return UserDataManager.Instance.userComebackCharge.loginCount > DefineEvent.MAX_COMBACK_CHARGE_SLOT();
    }
}

[System.Serializable]
public class UserCombackCharge
{
    public int loginCount;
    public Dictionary<int, bool> dic_reward_info = new Dictionary<int, bool>();

    public void Clear()
    {
        dic_reward_info.Clear();
        loginCount = 0;
    }

    

    /// <summary>
    /// 보상 획득 가능 여부를 리턴해준다
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool GetRewardState(int position)
    {
        if (dic_reward_info.ContainsKey(position))
        {
            return dic_reward_info[position];
        }
        else
        {
            Logger.Log($"알 수 없는 position_index : {position}", Logger.LOG.eLC_Error);
        }

        return false;
    }

    /// <summary>
    /// 보상 획득상태로 만들어준다
    /// </summary>
    /// <param name="position"></param>
    public void SetReward(int position)
    {
        if (dic_reward_info.ContainsKey(position))
        {
            dic_reward_info[position] = true;
        }
        else
        {
            Logger.Log($"알 수 없는 position_index : {position}", Logger.LOG.eLC_Error);
        }
    }

    /// <summary>
    /// 획득가능한 보상 리스트를 넘겨준다
    /// </summary>
    /// <returns></returns>
    public List<int> GetRewardEnableList()
    {
        return dic_reward_info.Where(v => v.Value == false).Select(v => v.Key).ToList();
    }

    /// <summary>
    /// 이미 획득한 보상 리스트를 넘겨준다
    /// </summary>
    /// <returns></returns>
    public List<int> GetRewardReceiveList()
    {
        return dic_reward_info.Where(v => v.Value == true).Select(v => v.Key).ToList();
    }

    // 다음 보상으로 받을 인덱스를 넘겨줍니다.
    // 모든 보상을 받았다면 -1을 넘겨줍니다.
    public int GetRewardIndex()
    {
        for (int i = 0; i < DefineEvent.MAX_COMBACK_CHARGE_SLOT(); i++)
        {
            if (GetRewardState(i) == false)
            {
                return i;
            }
        }

        return -1;
    }
}

[System.Serializable]
public class UserComebackQuestData
{
    public ObscuredInt index = 0;
    public ObscuredDouble value = 0;
    public ObscuredInt rewardState = 0;
    
    public QUEST_TYPE type = QUEST_TYPE.NONE;

    public string comebackGroup;
    

    public void AddValue(double value)
    {
        if (value > 0)
        {
            if (this.value + value < 0)
                return;
        }

        this.value += value;
    }

    public void SetValue(double value)
    {
        this.value = value;
    }
    public void ResetValue()
    {
        this.value = 0;
        this.rewardState = DefineQuest.LACK_POINT;
    }

    public void CopyData(UserComebackQuestData data)
    {
        this.index = data.index;
        this.value = data.value;

        this.comebackGroup = data.comebackGroup;

        this.rewardState = data.rewardState;
    }

    public void Clear()
    {
        this.index = 0;
        this.value = 0;
        this.rewardState = 0;

        this.comebackGroup = string.Empty;

        this.type = QUEST_TYPE.NONE;
    }
    public bool IsDailyType()
    {
        ComebackQuestData comebackQuestData = SODataManager.Instance.GetComebackQuestData(index);
        if (comebackQuestData == null)
            return false;

        return comebackQuestData.resetType == RESET_TYPE.DAILY;
    }

    public void ComebackQuestRewardProcess(System.Action cb)
    {
        int rewardState = UserDataManager.Instance.GetComebackQuestRewardState(index);
        if (rewardState != DefineQuest.ENABLE_REWARD)
        {
            Logger.Log($"보상을 받을수 없습니다 {rewardState}");
            return;
        }

        ComebackQuestData comebackQuestData = SODataManager.Instance.GetComebackQuestData(index);
        if (comebackQuestData == null)
        {
            UnityEngine.Debug.Log($"{System.Reflection.MethodBase.GetCurrentMethod().Name} comebackQuestData is null ({index})");
            return;
        }

        UserDataManager.Instance.AddQuestValue(QUEST_TYPE.COMEBACK_QUEST_CLEAR);

        foreach (var item in comebackQuestData.reward)
        {
            UserDataManager.Instance.AddUserItemData(item);
        }

        UserDataManager.Instance.SetComebackQuestRewardState(index, DefineQuest.COMPLETE_REWARD);

        // 네트워크 싱크
        UserDataManager.Instance.SaveData(new List<USER_DATA_TYPE>()
        {
            USER_DATA_TYPE.USER_RESOURCES,
            USER_DATA_TYPE.USER_COMEBACK_QUEST,
        });
        UserDataManager.Instance.SendNetworkSync(true);

        UIManager.Instance.ShowRewardMessage(comebackQuestData.reward);

        // 화면 갱신
        Messenger.Broadcast(EMessengerID.E_POPUP_EVENT_UPDATE);

        Sound.Play(ePlaySoundType.ePST_EFFECT, SoundData.UI_Accept);

        if (cb != null)
        {
            cb();
        }
    }
}

public partial class UserDataManager
{
    public UserComebackData userComebackData = new UserComebackData();

    public UserCombackCharge userComebackCharge = new UserCombackCharge();

    public SerializeDictionary<int, UserComebackQuestData> userComebackQuestData = new SerializeDictionary<int, UserComebackQuestData>();


    #region Comeback Charge
    public int GetComebackChargeRewardIndex()
    {
        return userComebackCharge.GetRewardIndex();
    }

    public bool GetComebackChargeRewardState(int index)
    {
        return userComebackCharge.GetRewardState(index);
    }

    public void SetComebackChargeReward(int position)
    {
        userComebackCharge.SetReward(position);
    }

    public bool IsComebackChargeComplete()
    {
        if (userComebackCharge.GetRewardIndex() <= -1)
            return true;

        return false;
    }
    #endregion

    #region Comeback Quest
    public UserComebackQuestData GetUserComebackQuestData(int index)
    {
        if (userComebackQuestData.ContainsKey(index))
        {
            return userComebackQuestData[index];
        }
        return null;
    }
    public double GetComebackQuestDataValue(int index)
    {
        UserComebackQuestData questData = GetUserComebackQuestData(index);
        if (questData == null)
            return 0;

        return questData.value;
    }
    public int GetComebackQuestRewardState(int index)
    {
        ComebackQuestData comebackQuestData = SODataManager.Instance.GetComebackQuestData(index);
        if (comebackQuestData == null)
            return DefineQuest.LACK_POINT;

        UserComebackQuestData userComebackQuestData = GetUserComebackQuestData(index);
        if (userComebackQuestData == null)
            return DefineQuest.LACK_POINT;

        if (userComebackQuestData.value >= comebackQuestData.questPoint)
        {
            if (userComebackQuestData.rewardState == DefineQuest.COMPLETE_REWARD)
                return DefineQuest.COMPLETE_REWARD;
            else
                return DefineQuest.ENABLE_REWARD;
        }
        else
        {
            return DefineQuest.LACK_POINT;
        }
    }
    public void SetComebackQuestRewardState(int index, int state)
    {
        UserComebackQuestData userComebackQuestData = GetUserComebackQuestData(index);
        userComebackQuestData.rewardState = state;
    }
    #endregion
}
