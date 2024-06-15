using CodeStage.AntiCheat.ObscuredTypes;
using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// 오프라인 보상 '저장'에만 사용하는 객체
/// </summary>
[Serializable]
public class UserSavedOfflineHistoryData
{
    // 저장된 스테이지
    public int save_stage_id;
    public List<RewardInfo> offlineHistory = new List<RewardInfo>();

    // 저장 관련된 정보
    public long kill_exp;
    public float item_rate_value;
    public int play_time;

    public int save_event_drop_rate;
    public int save_season_drop_rate;

    // 저장에 사용된 정보
    public int save_preset_no;

    public int save_dice_no;
    public int save_deck_no;
    public int save_property_no;
    public int save_spirit_no;

    // 숙련도
    public int proficiency_human;
    public int proficiency_elf;
    public int proficiency_orc;
    public int proficiency_undead;
    public int proficiency_angel;

    // 프리셋 상세 데이터
    public UserAccountDiceData save_dice_data = new UserAccountDiceData();
    public UserFormationData save_formation_data = new UserFormationData();

    // 부대 장판 레벨 총 합
    public int save_deck_total_level;
    // 부대 장판 타입
    public UserDeckBuffPresetData save_deck_buff = new UserDeckBuffPresetData();

    public UserSpiritPresetData save_spirit = new UserSpiritPresetData();

    public bool force_init = false;

    public void Clear()
    {
        save_stage_id = -1;

        offlineHistory.Clear();

        kill_exp = 0;
        item_rate_value = 0.0f;
        play_time = 0;

        save_event_drop_rate = 0;
        save_season_drop_rate = 0;

        save_preset_no = 0;

        save_dice_no = -1;
        save_deck_no = -1;
        save_property_no = -1;
        save_spirit_no = -1;

        proficiency_human = 0;
        proficiency_elf = 0;
        proficiency_orc = 0;
        proficiency_undead = 0;
        proficiency_angel = 0;

        save_dice_data = new UserAccountDiceData();
        save_formation_data = new UserFormationData();

        save_deck_total_level = 0;
        save_deck_buff = new UserDeckBuffPresetData();

        save_spirit = new UserSpiritPresetData();

        force_init = false;
    }    

    public bool IsMemorizeHistory()
    {
        return offlineHistory.Count > 0;
    }
}

public partial class UserDataManager
{   
    public UserSavedOfflineHistoryData savedOfflineHistoryData = new UserSavedOfflineHistoryData();    

    public UserSavedOfflineHistoryData LoadLocalSavedOfflineHistoryData()
    {
        if (savedOfflineHistoryData == null || savedOfflineHistoryData.save_stage_id <= 0)
            return null;

        UserSavedOfflineHistoryData ret = new UserSavedOfflineHistoryData();
        // 저장된 스테이지
        ret.save_stage_id = savedOfflineHistoryData.save_stage_id;
        // ret.offlineHistory = savedOfflineHistoryData.offlineHistory;
        // 해당 데이터도 깊은 복사가 필요하다.
        ret.offlineHistory  = savedOfflineHistoryData.offlineHistory.ConvertAll(r => new RewardInfo(r.type, r.index, r.count));

        // 저장 관련된 정보
        ret.kill_exp = savedOfflineHistoryData.kill_exp;
        ret.item_rate_value = savedOfflineHistoryData.item_rate_value;
        
        ret.play_time = savedOfflineHistoryData.play_time;
        ret.save_event_drop_rate = savedOfflineHistoryData.save_event_drop_rate;
        ret.save_season_drop_rate = savedOfflineHistoryData.save_season_drop_rate;

        // 저장에 사용된 정보
        // 통합 프리셋
        ret.save_preset_no = savedOfflineHistoryData.save_preset_no;

        // 하위 프리셋
        ret.save_dice_no = savedOfflineHistoryData.save_dice_no;
        ret.save_deck_no = savedOfflineHistoryData.save_deck_no;
        ret.save_property_no = savedOfflineHistoryData.save_property_no;
        ret.save_spirit_no = savedOfflineHistoryData.save_spirit_no;

        // 숙련도
        ret.proficiency_human = savedOfflineHistoryData.proficiency_human;
        ret.proficiency_elf = savedOfflineHistoryData.proficiency_elf;
        ret.proficiency_orc = savedOfflineHistoryData.proficiency_orc;
        ret.proficiency_undead = savedOfflineHistoryData.proficiency_undead;
        ret.proficiency_angel = savedOfflineHistoryData.proficiency_angel;

        // 프리셋 상세 정보
        ret.save_dice_data.CopyData(savedOfflineHistoryData.save_dice_data);
        ret.save_formation_data.CopyData(savedOfflineHistoryData.save_formation_data);

        ret.save_deck_total_level = savedOfflineHistoryData.save_deck_total_level;
        ret.save_deck_buff.CopyData(savedOfflineHistoryData.save_deck_buff);

        ret.save_spirit.CopyData(savedOfflineHistoryData.save_spirit);        

        return ret;
    }

    public void SaveCurOfflineHistoryData()
    {
        UserOfflineHistoryData data = UserDataManager.Instance.userOfflineHistoryData;

        // 현재 기록되어있는 오프라인 정보를 userOfflineHistoryData에 먼저 다시 저장한다.
        // 해당 함수를 실행하지 않을 경우, 재부팅 후에 데이터가 휘발되어있는 상태에서 Save가 안될 수 있으므로
        // userOfflineHistoryData에 다시 저장하도록 한다.
        CombatManager.Instance.SaveForceCurrentOfflineHistoryData();

        UserSavedOfflineHistoryData tmp = new UserSavedOfflineHistoryData();
        // 저장된 스테이지
        tmp.save_stage_id = data.save_stage_id;

        // 해당 데이터는 깊은 복사가 필요함.
        tmp.offlineHistory = data.offlineHistory.ConvertAll(r => new RewardInfo(r.type, r.index, r.count)); ;

        // 저장 관련된 정보
        tmp.kill_exp = data.kill_exp;
        tmp.item_rate_value = data.item_rate_value;

        tmp.play_time = data.play_time;
        tmp.save_event_drop_rate = data.save_event_drop_rate;
        tmp.save_season_drop_rate = data.save_season_drop_rate;

        // 저장에 사용된 정보
        // 통합 프리셋
        tmp.save_preset_no = data.save_preset_no;

        // 하위 프리셋
        tmp.save_dice_no = data.save_dice_no;
        tmp.save_deck_no = data.save_deck_no;
        tmp.save_property_no = data.save_property_no;
        tmp.save_spirit_no = data.save_spirit_no;

        // 숙련도
        tmp.proficiency_human = data.proficiency_human;
        tmp.proficiency_elf = data.proficiency_elf;
        tmp.proficiency_orc = data.proficiency_orc;
        tmp.proficiency_undead = data.proficiency_undead;
        tmp.proficiency_angel = data.proficiency_angel;

        // 아래 정보는 UserOfflineHistoryData에는 없는 데이터로, 여기서 만들어서 넣도록 한다.
        tmp.save_dice_data.CopyData(UserDataManager.Instance.GetDiceData(data.save_dice_no));
        tmp.save_formation_data.CopyData(UserDataManager.Instance.GetFormationData(data.save_deck_no));

        tmp.save_deck_total_level = UserDataManager.Instance.GetDeckLevelTotalLevel(data.save_deck_no);
        tmp.save_deck_buff.CopyData(UserDataManager.Instance.GetDeckBuffPresetData(data.save_deck_no));

        tmp.save_spirit.CopyData(UserDataManager.Instance.GetUserSpiritPresetData(data.save_spirit_no));

        savedOfflineHistoryData = tmp;
    }

    public void SetPresetSavedOfflineHistory()
    {
        UserSavedOfflineHistoryData historyData = LoadLocalSavedOfflineHistoryData();

        if (historyData == null)
        {
            return;
        }

        bool needInit = CheckNeedSavedOfflineDataReset();
        
        if (needInit)
        {
            Logger.Log("분처 저장용 프리셋에 수정사항이 있어서, 초기화를 진행합니다.");

            UserDataManager.Instance.userData.SetStageId(historyData.save_stage_id);
            ProjectUtil.SetPreset(historyData.save_preset_no, historyData.save_stage_id);

            return;
        }

        UserOfflineHistoryData _historyData = new UserOfflineHistoryData();
        _historyData.save_stage_id = historyData.save_stage_id;
        // 해당 데이터는 깊은 복사가 필요하다.
        _historyData.offlineHistory = historyData.offlineHistory.ConvertAll(r => new RewardInfo(r.type, r.index, r.count));
        _historyData.kill_exp = historyData.kill_exp;
        _historyData.item_rate_value = historyData.item_rate_value;
        _historyData.play_time = historyData.play_time;
        _historyData.save_event_drop_rate = historyData.save_event_drop_rate;
        _historyData.save_season_drop_rate = historyData.save_season_drop_rate;
        _historyData.save_preset_no = historyData.save_preset_no;
        _historyData.save_dice_no = historyData.save_dice_no;
        _historyData.save_deck_no = historyData.save_deck_no;
        _historyData.save_property_no = historyData.save_property_no;
        _historyData.save_spirit_no = historyData.save_spirit_no;
        _historyData.proficiency_human = historyData.proficiency_human;
        _historyData.proficiency_elf = historyData.proficiency_elf;
        _historyData.proficiency_orc = historyData.proficiency_orc;
        _historyData.proficiency_undead = historyData.proficiency_undead;
        _historyData.proficiency_angel = historyData.proficiency_angel;

        userOfflineHistoryData = _historyData;

        for (int i = 0; i < userOfflineHistoryData.offlineHistory.Count; i++)
        {
            CombatManager.Instance.SaveRewardHistory(userOfflineHistoryData.offlineHistory[i]);
        }

        CombatManager.Instance.SetMemorizedHistory(false);

        UserDataManager.Instance.userData.SetStageId(historyData.save_stage_id);
        ProjectUtil.SetPresetBySavingOfflineHistory(historyData.save_preset_no, historyData.save_stage_id);

        UserDataManager.Instance.SavePresetIndex_SaveStage(STAGE_TYPE.NORMAL, UserDataManager.Instance.curPresetData.preset_no);

        Messenger.Broadcast(EMessengerID.E_REFRESH_MAIN_OFFLINE_HISTORY);

        UserDataManager.Instance.SaveData(USER_DATA_TYPE.USER_DATA);
    }

    /// <summary>
    /// 현재 저장되어있는 프리셋 데이터가 변경된 이력이 있는지 확인합니다.
    /// 변경된 이력이 있다면 true를 반환합니다.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool CheckNeedSavedOfflineDataReset()
    {
        UserSavedOfflineHistoryData historyData = LoadLocalSavedOfflineHistoryData();

        if (historyData == null)
        {
            // 저장된 데이터가 없으므로, 초기화할 필요도 없다.
            return false;
        }

        if (savedOfflineHistoryData.force_init == true)
        {
            return true;
        }

        UserPresetData presetData = GetUserPresetData(historyData.save_preset_no);

        // 프리셋 번호 변경 검사        
        {
            // 저장되어있는 통합 프리셋 안의 각 프리셋이 다른 프리셋으로 변경되었는지 검사
            if (historyData.save_dice_no != presetData.cur_dice_no ||
                historyData.save_deck_no != presetData.cur_formation_no ||
                historyData.save_property_no != presetData.cur_property_no ||
                historyData.save_spirit_no != presetData.cur_spirit_no)
            {
                return true;
            }
        }

        // 진급 능력 검사        
        {
            // 현재 유저 프리셋에 저장된 진급 능력과 분처 저장 당시 진급 능력이 같은지 비교
            UserAccountDiceData curDiceData = GetDiceData(presetData.cur_dice_no);
            for (int i = 0; i < CDefineData.max_dice_slot; i++)
            {
                if (curDiceData.slotList[i].diceOptionIndex != historyData.save_dice_data.slotList[i].diceOptionIndex)
                {
                    Logger.Log("분처 저장용 프리셋에 저장되어있던 진급 능력이 수정되었습니다.");
                    return true;                     
                }
            }
        }

        // 부대 편성 검사        
        {
            // 현재 유저 프리셋에 저장된 부대 편성과 분처 저장 당시 부대 편성이 같은지 비교
            UserFormationData curFormationData = GetFormationData(presetData.cur_formation_no);
            for (int i = 0; i < CDefineData.max_formation_slot; i++)
            {
                if (curFormationData.slotList[i].entityIndex != historyData.save_formation_data.slotList[i].entityIndex)
                {                    
                    Logger.Log("분처 저장용 프리셋에 저장되어있던 부대 배치가 수정되었습니다.");
                    return true;                    
                }
            }

            /// NOTE
            /// 부대 성장 및 부대 장판 변경은
            /// 오프라인 보상을 초기화시키지 않는다.
            /// 따라서, 해당 경우에는 분처 저장을 초기화시키 않는다.
            /// 
            //// 부대 성장을 한 이력이 있다.
            //if (historyData.save_deck_total_level != GetDeckLevelTotalLevel(curFormationData.index))
            //{
            //    Logger.Log("분처 저장용 프리셋에 저장되어있던 부대 장판 레벨이 수정되었습니다.");                
            //    return true;
            //}

            //UserDeckBuffPresetData curDeckBuffData = GetDeckBuffPresetData(presetData.cur_formation_no);
            //// 부대 장판 변경을 한 이력이 있다.
            //for (int i = 0; i < DefineDeckBuff.max_deck_buff_slot_count; i++)
            //{
            //    if (curDeckBuffData.dataList[i].type != historyData.save_deck_buff.dataList[i].type)
            //    {                    
            //        Logger.Log("분처 저장용 프리셋에 저장되어있던 부대 장판 종류가 수정되었습니다.");
            //        return true;                    
            //    }
            //}
        }

        // 정령석 배치 검사        
        {
            UserSpiritPresetData curSpiritData = GetUserSpiritPresetData(presetData.cur_spirit_no);
            for (int i = 0; i < CDefineData.max_spirit_slot; i++)
            {
                if (curSpiritData.slotList[i].spiritIndex != historyData.save_spirit.slotList[i].spiritIndex)
                {                    
                    Logger.Log("분처 저장용 프리셋에 저장되어있던 정령석 배치가 수정되었습니다.");
                    return true;                    
                }
                
                if (curSpiritData.slotList[i].spiritEnchant.Count != historyData.save_spirit.slotList[i].spiritEnchant.Count)
                    continue;

                // 정령석 초월 검사
                for (int enChantIndex = 0; enChantIndex < curSpiritData.slotList[i].spiritEnchant.Count; enChantIndex++)
                {
                    if (curSpiritData.slotList[i].spiritEnchant[enChantIndex] != historyData.save_spirit.slotList[i].spiritEnchant[enChantIndex])
                    {                        
                        Logger.Log("분처 저장용 프리셋에 저장되어있던 정령석 초월이 수정되었습니다.");
                        return true;                        
                    }
                }
            }
        }
        return false;
    }

    // 저장된 데이터가 존재하는지 검사합니다.
    // 데이터가 비어있다면 true를 반환합니다.
    public bool IsOfflineHistoryDataEmpty()
    {
        if (savedOfflineHistoryData == null)
            return true;

        if (savedOfflineHistoryData.save_stage_id <= 0)
            return true;

        return false;
    }

    public List<RewardInfo> ConvertOldSeasonCoin(List<RewardInfo> rewardList)
    {
        SeasonEventData _seasonEventData = UserDataManager.Instance.userSeasonData.GetSeasonEventData();
        List<RewardInfo> oldSeasonCoins = new List<RewardInfo>();
        for (int i = 0; i < rewardList.Count; i++)
        {
            if (rewardList[i].type == REWARD_TYPE.RESOURCE)
            {
                // 지난 시즌의 코인이라면, 현재 시즌의 코인으로 바꿔준다.
                if (DefineItem.IsSeasonCoin(rewardList[i].index))
                {
                    // 시즌 이벤트가 진행 중이 아닐때는 변환하지 않는다.
                    if (_seasonEventData != null)
                    {
                        if (rewardList[i].index != _seasonEventData.dropItem)
                        {
                            Logger.Log("지난 시즌의 코인이 확인되었습니다.");
                            oldSeasonCoins.Add(rewardList[i]);
                        }
                    }
                }
            }
        }

        List<RewardInfo> newSeasonCoins = new List<RewardInfo>();
        for (int i = 0; i < oldSeasonCoins.Count; i++)
        {
            for (int j = 0; j < rewardList.Count; j++)
            {
                if (rewardList[j].index == oldSeasonCoins[i].index)
                {
                    Logger.Log("저장된 분처 데이터에 지난 시즌의 코인이 들어있어 해당 데이터를 삭제합니다.");
                    REWARD_TYPE newType = rewardList[j].type;
                    ObscuredInt newIndex = _seasonEventData.dropItem;
                    ObscuredLong newCount = rewardList[j].count;
                    rewardList.RemoveAt(j);

                    newSeasonCoins.Add(new RewardInfo(newType, newIndex, newCount));
                    break;
                }
            }
        }

        for (int i = 0; i < newSeasonCoins.Count; i++)
        {
            rewardList.Add(newSeasonCoins[i]);
        }

        return rewardList;
    }

    // 분처 저장 데이터에 시즌 코인이 비어있을 때 보여주는 용도로 쓸 함수
    public List<RewardInfo> PutDummySeasonCoin(List<RewardInfo> rewardList)
    {
        bool hasSeasonCoin = false;
        for (int i = 0; i < rewardList.Count; i++)
        {
            if (DefineItem.IsSeasonCoin(rewardList[i].index))
            {
                hasSeasonCoin = true;
                break;
            }
        }

        // 이미 시즌 코인이 있기 때문에 넣어주지 않는다.
        if (hasSeasonCoin)
        {
            return rewardList;
        }

        SeasonEventData seasonData = UserDataManager.Instance.userSeasonData.GetSeasonEventData();
        // 시즌 이벤트가 진행중이 아닐 때에는 변환하지 않는다.
        if (seasonData != null)
        {
            int dropIndex = UserDataManager.Instance.userSeasonData.GetDropItem();

            // 핫타임이 아니라면 보여주지 않는다.
            if (UserDataManager.Instance.GetUserHotTimeBuffData_ResourceIndex(dropIndex) != null)
            {
                RewardInfo seasonCoinReward = new RewardInfo();
                seasonCoinReward.type = REWARD_TYPE.RESOURCE;
                seasonCoinReward.index = dropIndex;
                seasonCoinReward.count = 0;

                rewardList.Add(seasonCoinReward);
            }
        }

        return rewardList;
    }

    public void SyncEmptyOfflineHistoryData()
    {
        userOfflineHistoryData.Clear();
        NetworkOnMessage.OnRequest_SaveOfflineHistory();
    }
}
