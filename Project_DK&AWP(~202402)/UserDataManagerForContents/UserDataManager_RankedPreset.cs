using CodeStage.AntiCheat.ObscuredTypes;
using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class UserRankedPresetData
{
    public int pk;    
    public RankedPresetType type;
    public RankedUserFormationData formationData = new RankedUserFormationData();
    public UserSpiritPresetData spiritPresetData = new UserSpiritPresetData();
    public List<List<UserEntityEquipment>> equipmentData = new List<List<UserEntityEquipment>>();

    public bool isDataEnable = false;    
}

public enum RankedPresetType
{
    STAGE,
    NEST,
    PVP,
    MAX
}

[System.Serializable]
public class RankedUserFormationData
{
    public ObscuredInt index = 0;
    
    public List<RankedUserFormationSlotData> slotList = new List<RankedUserFormationSlotData>();
    public RankedUserFormationData()
    {
        for (int i = 0; i < CDefineData.max_formation_slot; i++)
        {
            var tmp = new RankedUserFormationSlotData();
            tmp.index = i;
            tmp.entityIndex = CDefineData.formation_slot_empty;
            tmp.entityLevel = 1;
            tmp.entityStarGrade = 1;
            tmp.transcendence = 0;
            tmp.skillLevel = 1;
            this.slotList.Add(tmp);
        }
    }
    
    public List<int> GetSlotList()
    {
        var retList = new List<int>();
        for (int i = 0; i < slotList.Count; i++)
        {
            if (slotList[i].entityIndex > CDefineData.formation_slot_empty)
            {
                retList.Add(slotList[i].entityIndex);
            }
        }

        return retList;
    }

    public RankedUserFormationSlotData GetFormationSlotData(int slotIndex)
    {
        foreach (var item in slotList)
        {
            if (item.index == slotIndex)
                return item;
        }

        return null;
    }
    public bool IsDeploy(int entityIndex)
    {
        return slotList.Where(v => v.entityIndex == entityIndex && v.entityIndex > CDefineData.formation_slot_empty).ToList().Count > 0;
    }
    public RankedUserFormationSlotData GetDeploySlotData(int entityIndex)
    {
        return slotList.Where(v => v.entityIndex == entityIndex && v.entityIndex > CDefineData.formation_slot_empty).ToList().FirstOrDefault();
    }
    public int GetCurSlotCount()
    {
        int ret = 0;
        foreach (var item in slotList)
        {
            if (item.entityIndex > CDefineData.formation_slot_empty)
            {
                ret++;
            }
        }

        return ret;
    }
    public bool IsUseSlotMax()
    {
        return GetCurSlotCount() >= ProjectUtil.GetMaxFormationSlot_UseEnable();
    }
    public List<int> ClearSlot()
    {
        var retList = new List<int>();
        foreach (var item in slotList)
        {
            if (item.IsEmpty() == false)
            {
                retList.Add(item.index);
            }
            item.entityIndex = CDefineData.formation_slot_empty;
        }

        return retList;
    }

    public RankedUserFormationData GetDeepCopy()
    {
        RankedUserFormationData ret = new RankedUserFormationData();
        ret.index = this.index;

        for (int i = 0; i < slotList.Count; i++)
        {
            ret.slotList[i].index = slotList[i].index;
            ret.slotList[i].entityIndex = slotList[i].entityIndex;
            ret.slotList[i].entityLevel = slotList[i].entityLevel;
            ret.slotList[i].entityStarGrade = slotList[i].entityStarGrade;
            ret.slotList[i].transcendence = slotList[i].transcendence;
            ret.slotList[i].skillLevel = slotList[i].skillLevel;
            ret.slotList[i].upgrade = slotList[i].upgrade;
            ret.slotList[i].bonusSkillLevel = slotList[i].bonusSkillLevel;
            ret.slotList[i].rideIndex = slotList[i].rideIndex;
            ret.slotList[i].cardLevel = slotList[i].cardLevel;
        }

        return ret;
    }
}

[System.Serializable]
public class RankedUserFormationSlotData
{
    public ObscuredInt index;
    public ObscuredInt entityIndex;
    public ObscuredInt entityLevel;
    public ObscuredInt entityStarGrade;
    public ObscuredInt transcendence;
    public ObscuredInt skillLevel;
    public ObscuredInt upgrade;
    public ObscuredInt bonusSkillLevel;

    public ObscuredInt rideIndex;
    public ObscuredInt cardLevel;    

    public bool IsEmpty()
    {
        return entityIndex <= CDefineData.formation_slot_empty;
    }
}

public partial class UserDataManager
{
    public UserRankedPresetData anotherUserRankedPreset = new UserRankedPresetData();    

    public UserRankedPresetData CreateRankedPresetData(RankedPresetType rankedType)
    {
        UserRankedPresetData rankedPresetData = new UserRankedPresetData();        

        rankedPresetData.type = rankedType;

        int curFormationIndex = UserDataManager.Instance.curPresetData.cur_formation_no;        
        rankedPresetData.formationData = UserDataManager.Instance.CreateRankedUserFormationData();

        int curSpiritIndex = UserDataManager.Instance.curPresetData.cur_spirit_no;
        rankedPresetData.spiritPresetData = UserDataManager.Instance.GetUserSpiritPresetData(curSpiritIndex).GetDeepCopy();
        
        rankedPresetData.equipmentData = CreateRankedUserEquipmentData();

        return rankedPresetData;
    }

    public RankedUserFormationData CreateRankedUserFormationData()
    {
        UserFormationData curFormationData = UserDataManager.Instance.GetFormationData(UserDataManager.Instance.curPresetData.cur_formation_no);

        RankedUserFormationData ret = new RankedUserFormationData();

        ret.index = curFormationData.index;
        for (int i = 0; i < curFormationData.slotList.Count; i++)
        {
            ret.slotList[i] = (curFormationData.slotList[i].ConvertRankedData());
        }

        return ret;
    }

    public List<List<UserEntityEquipment>> CreateRankedUserEquipmentData()
    {
        List<List<UserEntityEquipment>> ret = new List<List<UserEntityEquipment>>();

        List<UserFormationSlotData> curSlotData = UserDataManager.Instance.GetFormationData(UserDataManager.Instance.curPresetData.cur_formation_no).slotList;

        for (int i = 0; i < curFormationData.slotList.Count; i++)
        {
            ret.Add(UserDataManager.Instance.GetUserEquipmentFromEntity(curSlotData[i].entityIndex));
        }

        return ret;
    }

    public List<UserEntityEquipment> GetRankedUserEntityEquipmentFromEntity(int entityIndex)
    {
        List<UserEntityEquipment> targetEquipment = new List<UserEntityEquipment>();
        List<List<UserEntityEquipment>> equipmentData = UserDataManager.Instance.anotherUserRankedPreset.equipmentData;
        for (int i = 0; i < equipmentData.Count; i++)
        {
            for (int j = 0; j < equipmentData[i].Count; j++)
            {
                if (equipmentData[i][j].equipEntity == entityIndex)
                {
                    targetEquipment.Add(equipmentData[i][j]);
                }
            }
        }

        return targetEquipment;
    }
}
