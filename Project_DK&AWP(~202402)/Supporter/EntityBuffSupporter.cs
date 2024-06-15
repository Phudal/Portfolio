using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CoffeeLibrary;

public class EntityBuffSupporter : EditorWindow
{
    enum WINDOW_TYPE
    {
        POST_IT = 0,
        INSPECTOR = 1,
        UTILITY = 2,
    };

    static WINDOW_TYPE windowType = WINDOW_TYPE.POST_IT;

    // 화면상의 스크롤 뷰의 위치
    Rect rScrollRect;
    // 총 스크롤 되는 공간
    Rect rScrollArea;
    // 스크롤 바의 위치
    Vector2 vScrollPos = Vector2.zero;
    // 스크롤 영역 계산을 위한, 총 사용한 영역
    Vector2 vLastPos = Vector2.zero;
    #region ::GUI에 사용되는 값::
    /// UI 시작 위치
    int START_POS_X = 10;
    int START_POS_Y = 10;
    /// 내려 쓰기 폭
    int NEW_LINE_Y = 5;
    /// 블럭 단위 폭
    int BLOCK_LINE_Y = 25;
    /// UI 항목당 높이
    int GUI_HEIGHT = 25;
    #endregion

    private int curEntityIndex = CDefineData.formation_slot_empty;

    private bool toggleValue = false;

    private bool showUserBuff = false;
    private bool showRideBuff = false;
    private bool showDeckBuff = false;
    private bool showEquipBuff = false;
    private bool showEquipOptionBuff = false;
    private bool showCardBuff = false;
    private bool showExtraBuff = false;
    private bool showSkillBuff = false;

    private static Texture2D bannerBar;
    private static Texture2D BannerBar
    {
        get
        {
            if (EntityBuffSupporter.bannerBar == null)
            {
                UnityEngine.Object obj = EditorGUIUtility.Load("Assets/10_Resources/Background/Background_UI/banner_verse_miho.png");
                EntityBuffSupporter.bannerBar = (Texture2D)obj;
            }
            return EntityBuffSupporter.bannerBar;
        }
    }

    #region MenuItem
    // 어디든 부착가능한 포스트잇 처럼 만들기
    [MenuItem("CoffeeLibrary/EntityBuffSupporter")]
    public static void ShowWindow()
    {
        if (windowType == WINDOW_TYPE.POST_IT)
        {
            EditorWindow.GetWindow<EntityBuffSupporter>("EntityBuffSupporter", true);
        }
        else if (windowType == WINDOW_TYPE.INSPECTOR)
        {
            Type inspectorType = GetInspectorWindowType();
            EntityBuffSupporter window = GetWindow<EntityBuffSupporter>(new Type[] { inspectorType });
            window.titleContent = new GUIContent("EntityBuffSupporter");
        }
        else if (windowType == WINDOW_TYPE.UTILITY)
        {
            EditorWindow.GetWindow<EntityBuffSupporter>(true);
        }

        static Type GetInspectorWindowType()
        {
            return Type.GetType("UnityEditor.InspectorWindow, UnityEditor.dll");
        }
    }
    #endregion

    private void OnGUI()
    {
        Rect rImg = new Rect(0, 0, 900, 244);
        GUI.DrawTexture(rImg, EntityBuffSupporter.BannerBar);

        GUIStyle imgStyle = new GUIStyle(EditorStyles.boldLabel);
        if (GUI.Button(rImg, "", imgStyle))
        {
            // 클릭하면 나를 열어줘
            // open this script
            string externalEditor = UnityEditorInternal.ScriptEditorUtility.GetExternalScriptEditor();

            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
            string currentFile = st.GetFrame(0).GetFileName();
            int currentLine = st.GetFrame(0).GetFileLineNumber();
            System.Diagnostics.Process.Start(externalEditor, $"/Edit {currentFile}");
        }

        int pos_x = START_POS_X;
        int pos_y = (int)rImg.height + START_POS_Y;        

        rScrollRect = new Rect(0, pos_y, position.width, position.height);
        rScrollArea = new Rect(0, pos_y, vLastPos.x, vLastPos.y);
        vScrollPos = GUI.BeginScrollView(rScrollRect, vScrollPos, rScrollArea, false, false);

        toggleValue = GUI.Toggle(new Rect(0, pos_y, 100, GUI_HEIGHT), toggleValue!, "Use Supporter");
        pos_y += BLOCK_LINE_Y;

        if (toggleValue == true)
        {
            if (UserDataManager.Instance.curFormationData != null)
            {
                if (UserDataManager.Instance.curFormationData.slotList.Count > 0)
                {
                    List<UserFormationSlotData> slotList = UserDataManager.Instance.curFormationData.slotList;
                    {
                        for (int slotIndex = 0; slotIndex < slotList.Count; slotIndex++)
                        {
                            string slotName = string.Empty;
                            Color slotColor = Color.white;
                            if (slotList[slotIndex].entityIndex == CDefineData.formation_slot_empty)
                            {
                                slotName = "미배치";
                            }
                            else
                            {
                                slotName = SODataManager.Instance.GetEntityData(slotList[slotIndex].entityIndex).GetTitle();

                                switch (SODataManager.Instance.GetEntityData(slotList[slotIndex].entityIndex).species)
                                {
                                    case SPECIES_TYPE.HUMAN:
                                        slotColor = Color.cyan;
                                        break;

                                    case SPECIES_TYPE.ELF:
                                        slotColor = Color.green;
                                        break;

                                    case SPECIES_TYPE.ORC:
                                        slotColor = Color.red;
                                        break;

                                    case SPECIES_TYPE.UNDEAD:
                                        slotColor = Color.gray;
                                        break;

                                    case SPECIES_TYPE.ANGEL:
                                    case SPECIES_TYPE.DEVIL:
                                        slotColor = Color.yellow;
                                        break;
                                }
                            }

                            using (new BackgroundColorScope(slotColor))
                            {
                                if (GUI.Button(new Rect((50 + 4 * 150) - (slotIndex / 4) * 150, pos_y, 150, GUI_HEIGHT), slotName))
                                {
                                    curEntityIndex = slotList[slotIndex].entityIndex;
                                }
                            }

                            pos_y += BLOCK_LINE_Y;

                            if ((slotIndex + 1) % 4 == 0 && slotIndex != slotList.Count - 1)
                            {
                                pos_y -= BLOCK_LINE_Y * 4;
                            }
                        }

                        // 선택된 엔티티가 있으면 해당 정보를 출력
                        if (curEntityIndex != CDefineData.formation_slot_empty)
                        {
                            pos_y += NEW_LINE_Y;
                            GUI.Label(new Rect(0, pos_y, 300, GUI_HEIGHT), SODataManager.Instance.GetEntityData(curEntityIndex).GetTitle(), imgStyle);
                            pos_y += BLOCK_LINE_Y;

                            BaseEntity entity = CombatManager.Instance.GetAllyEntity(curEntityIndex);
                            if (entity != null)
                            {
                                using (new BackgroundColorScope(Color.red))
                                {
                                    GUI.Label(new Rect(0, pos_y, 300, GUI_HEIGHT), string.Format($"HP : {entity.GetHeathPercent() * 100}%"), imgStyle);
                                    pos_y += BLOCK_LINE_Y;
                                }

                                using (new BackgroundColorScope(Color.yellow))
                                {
                                    GUI.Label(new Rect(0, pos_y, 300, GUI_HEIGHT), string.Format($"SKILL : {entity.GetSkillRemainPercent() * 100}%"), imgStyle);
                                    pos_y += BLOCK_LINE_Y;
                                }

                                showUserBuff = GUI.Toggle(new Rect(0, pos_y, 150, GUI_HEIGHT), showUserBuff!, "지휘관 버프");
                                showRideBuff = GUI.Toggle(new Rect(200, pos_y, 150, GUI_HEIGHT), showRideBuff!, "탈것 버프");
                                showDeckBuff = GUI.Toggle(new Rect(400, pos_y, 150, GUI_HEIGHT), showDeckBuff!, "부대 편성 버프");
                                showEquipBuff = GUI.Toggle(new Rect(600, pos_y, 150, GUI_HEIGHT), showEquipBuff!, "전용장비 버프");
                                pos_y += BLOCK_LINE_Y;
                                showEquipOptionBuff = GUI.Toggle(new Rect(0, pos_y, 150, GUI_HEIGHT), showEquipOptionBuff!, "전용장비 옵션 버프");
                                showCardBuff = GUI.Toggle(new Rect(200, pos_y, 150, GUI_HEIGHT), showCardBuff!, "카드 버프");
                                showExtraBuff = GUI.Toggle(new Rect(400, pos_y, 150, GUI_HEIGHT), showExtraBuff!, "요일던전 버프");
                                showSkillBuff = GUI.Toggle(new Rect(600, pos_y, 150, GUI_HEIGHT), showSkillBuff!, "스킬 버프");

                                pos_y += BLOCK_LINE_Y;
                                pos_y += BLOCK_LINE_Y;

                                #region USER BUFF
                                SPECIES_TYPE species = entity.species;
                                JOB_TYPE job = entity.job;
                                SIDE side = entity.side;
                                

                                List<BUFF_TYPE> viewBuffType = new List<BUFF_TYPE>() 
                                {
                                    BUFF_TYPE.I_ATK,
                                    BUFF_TYPE.I_HP,
                                    BUFF_TYPE.ATK,
                                    BUFF_TYPE.HP,
                                    BUFF_TYPE.AVOID,
                                    BUFF_TYPE.CRIRATE,
                                    BUFF_TYPE.CRIDMG,
                                    BUFF_TYPE.SKILLVALUE,
                                    BUFF_TYPE.BOSSVALUE,
                                    BUFF_TYPE.ATKDELAY,
                                    BUFF_TYPE.MOVESPD,
                                    BUFF_TYPE.SKILLDELAY
                                };

                                if (showUserBuff)
                                {
                                    // 유저 전체 버프 정보
                                    var userBuff = UserDataManager.Instance.GetUserBuff();
                                    for (int i = 0; i < viewBuffType.Count; i++)
                                    {
                                        SimpleBuffData simpleBuffData = UserDataManager.Instance.GetUserBuffData(userBuff, viewBuffType[i], species, job, side);
                                        string buffString = simpleBuffData.PRINT_EDITOR();
                                        int lineCount = buffString.Count(v => v == '\n');

                                        GUI.Label(new Rect(0, pos_y, 600, 20 * lineCount), simpleBuffData.PRINT_EDITOR(), imgStyle);
                                        pos_y += 20 * lineCount;
                                        // pos_y += BLOCK_LINE_Y;
                                    }
                                }
                                
                                if (showRideBuff)
                                {
                                    // 탑승-인연효과 정보
                                    var rideBuff = UserDataManager.Instance.GetRideBuffData(entity.pk);
                                    for (int i = 0; i < viewBuffType.Count; i++)
                                    {
                                        SimpleBuffData simpleBuffData = UserDataManager.Instance.GetUserBuffData(rideBuff, viewBuffType[i], species, job, side);
                                        string buffString = simpleBuffData.PRINT_EDITOR();
                                        int lineCount = buffString.Count(v => v == '\n');

                                        GUI.Label(new Rect(0, pos_y, 600, 20 * lineCount), simpleBuffData.PRINT_EDITOR(), imgStyle);
                                        pos_y += 20 * lineCount;                                        
                                    }
                                }

                                if (showDeckBuff)
                                {
                                    // 덱 버프 효과
                                    var deckBuff = UserDataManager.Instance.GetDeckBuffData(UserDataManager.Instance.curFormationData.index, entity.pk);
                                    for (int i = 0; i < viewBuffType.Count; i++)
                                    {
                                        SimpleBuffData simpleBuffData = UserDataManager.Instance.GetUserBuffData(deckBuff, viewBuffType[i], species, job, side);
                                        string buffString = simpleBuffData.PRINT_EDITOR();
                                        int lineCount = buffString.Count(v => v == '\n');

                                        GUI.Label(new Rect(0, pos_y, 600, 20 * lineCount), simpleBuffData.PRINT_EDITOR(), imgStyle);
                                        pos_y += 20 * lineCount;
                                    }
                                }

                                if (showEquipBuff)
                                {
                                    // equipment Stat
                                    var equipmentBuff = UserDataManager.Instance.GetEquipmentBuffData(entity.pk);
                                    for (int i = 0; i < viewBuffType.Count; i++)
                                    {
                                        SimpleBuffData simpleBuffData = UserDataManager.Instance.GetUserBuffData(equipmentBuff, viewBuffType[i], species, job, side);
                                        string buffString = simpleBuffData.PRINT_EDITOR();
                                        int lineCount = buffString.Count(v => v == '\n');

                                        GUI.Label(new Rect(0, pos_y, 600, 20 * lineCount), simpleBuffData.PRINT_EDITOR(), imgStyle);
                                        pos_y += 20 * lineCount;
                                    }
                                }

                                if (showEquipOptionBuff)
                                {
                                    // equipment Option
                                    var equipmentOptionBuff = UserDataManager.Instance.GetEquipmentOptionBuffData(entity.pk);

                                    for (int i = 0; i < viewBuffType.Count; i++)
                                    {
                                        SimpleBuffData simpleBuffData = UserDataManager.Instance.GetUserBuffData(equipmentOptionBuff, viewBuffType[i], species, job, side);
                                        string buffString = simpleBuffData.PRINT_EDITOR();
                                        int lineCount = buffString.Count(v => v == '\n');

                                        GUI.Label(new Rect(0, pos_y, 600, 20 * lineCount), simpleBuffData.PRINT_EDITOR(), imgStyle);
                                        pos_y += 20 * lineCount;
                                    }
                                }                                                                

                                if (showExtraBuff)
                                {
                                    // dungeonType Species Buff
                                    int index = CombatManager.Instance.GetCurStageIndex();
                                    var stageData = SODataManager.Instance.GetStageData(index);

                                    Dictionary<BUFF_KEY, UserBuffData> extradungeonSpeciesBuff = new Dictionary<BUFF_KEY, UserBuffData>();
                                    if (stageData != null && ProjectUtil.IsExtraDungeon(stageData.stageType))
                                    {
                                        extradungeonSpeciesBuff = UserDataManager.Instance.GetExtraDungeonBuffData(DefineStage.GetStageTypeString(stageData.stageType), UserDataManager.Instance.curFormationData.index, UserDataManager.Instance.curFormationData);
                                    }                                    

                                    for (int i = 0; i < viewBuffType.Count; i++)
                                    {
                                        SimpleBuffData simpleBuffData = UserDataManager.Instance.GetUserBuffData(extradungeonSpeciesBuff, viewBuffType[i], species, job, side);
                                        string buffString = simpleBuffData.PRINT_EDITOR();
                                        int lineCount = buffString.Count(v => v == '\n');

                                        GUI.Label(new Rect(0, pos_y, 600, 20 * lineCount), simpleBuffData.PRINT_EDITOR(), imgStyle);
                                        pos_y += 20 * lineCount;
                                    }
                                }                                                                

                                if (showCardBuff)
                                {
                                    // Card Buff
                                    var cardBuff = UserDataManager.Instance.GetCardBuffData(entity.pk);

                                    for (int i = 0; i < viewBuffType.Count; i++)
                                    {
                                        SimpleBuffData simpleBuffData = UserDataManager.Instance.GetUserBuffData(cardBuff, viewBuffType[i], species, job, side);
                                        string buffString = simpleBuffData.PRINT_EDITOR();
                                        int lineCount = buffString.Count(v => v == '\n');

                                        GUI.Label(new Rect(0, pos_y, 600, 20 * lineCount), simpleBuffData.PRINT_EDITOR(), imgStyle);
                                        pos_y += 20 * lineCount;
                                    }
                                }

                                #endregion

                                if (showSkillBuff)
                                {
                                    #region SKILL BUFF
                                    Dictionary<int, CombatBuffData> combatBuff = entity.GetCombatBuff();
                                    foreach (KeyValuePair<int, CombatBuffData> pair in combatBuff)
                                    {
                                        if (pair.Value.IsActivate() == false)
                                        {
                                            continue;
                                        }

                                        string descString = string.Empty;
                                        descString += DefineCombat.GetBuffName(pair.Value.buffType);
                                        descString += " : ";
                                        descString += pair.Value.activateValue;

                                        string additionalString = string.Empty;
                                        EntitySkillData skillData = SODataManager.Instance.GetEntitySkillDataFromSkillAdditional(pair.Key);
                                        if (skillData != null)
                                        {
                                            additionalString += skillData.GetTitle();
                                        }
                                        else
                                        {
                                            additionalString += "알 수 없는 스킬";
                                        }

                                        additionalString += "  ";
                                        additionalString += string.Format($"skill Additional : {pair.Key}");

                                        GUI.Label(new Rect(0, pos_y, 400, GUI_HEIGHT), additionalString, imgStyle);
                                        GUI.Label(new Rect(400, pos_y, 400, GUI_HEIGHT), descString, imgStyle);

                                        pos_y += BLOCK_LINE_Y;
                                    }
                                    #endregion
                                }
                            }
                        }
                    }
                }
                else
                {
                    curEntityIndex = CDefineData.formation_slot_empty;
                }
            }
            else
            {
                curEntityIndex = CDefineData.formation_slot_empty;
            }

        }

        pos_y += NEW_LINE_Y;

        vLastPos.x = pos_x;
        vLastPos.y = pos_y;
        GUI.EndScrollView();
    }
}
