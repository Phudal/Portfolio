using CoffeeLibrary;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Popup_Comeback : Popup
{
    public enum TAB
    {
        ComebackCharge,
        Quest,
        Exchange,
        Package,

        MAX
    };

    public class Args : PopupBaseArgs
    {
        public int tabIndex;
    }

    Args popupArgs;
    TAB curTab = TAB.ComebackCharge;

    [SerializeField] UITabButtonGroup tabGroup;
    [SerializeField] List<UIReddot> reddotList = new List<UIReddot>();

    [SerializeField] ComebackChargeEvent comebackChargeEvent;
    [SerializeField] ComebackQuestEvent comebackQuestEvent;
    [SerializeField] ComebackExchange comebackExchange;
    [SerializeField] ComebackPackageTab comebackPackage;

    [SerializeField] TextMeshProUGUI TMP_TimeInfo;

    void Awake()
    {
        tabGroup.UpdateLayoutRebuilder();
    }

    private void OnEnable()
    {
        Messenger.RegisterListener(EMessengerListenerType.ADD_LISTENER, EMessengerID.E_COMEBACK_REFRESH, InitInfo);
        Messenger.RegisterListener(EMessengerListenerType.ADD_LISTENER, EMessengerID.E_POPUP_COMEBACK_REFRESH, InitInfo);
    }

    private void OnDisable()
    {
        Messenger.RegisterListener(EMessengerListenerType.REMOVE_LISTENER, EMessengerID.E_COMEBACK_REFRESH, InitInfo);
        Messenger.RegisterListener(EMessengerListenerType.REMOVE_LISTENER, EMessengerID.E_POPUP_COMEBACK_REFRESH, InitInfo);
    }

    public override void OnPopupPushed(PopupBaseArgs args)
    {
        base.OnPopupPushed(args);

        popupArgs = args as Args;
        if (popupArgs != null)
        {
            curTab = (TAB)popupArgs.tabIndex;
        }

        InitTabButton();
        InitReddot();

        tabGroup.UpdateLayoutRebuilder();
        OnClick_TabButton((int)curTab);
    }

    public override void OnPopupPopped()
    {
        base.OnPopupPopped();
    }

    private void InitInfo()
    {
        if (UserDataManager.Instance.userComebackData.IsComebackExpire())
        {            
            PopPopup();
            return;
        }

        // 퀘스트가 만료되어있는데 탭이 퀘스트로 되어있다
        // -> 슈퍼차지로 변경 후 다시 init한다.
        if (UserDataManager.Instance.userComebackData.IsComebackQuestExpire())
        {
            if (curTab == TAB.Quest)
            {
                curTab = TAB.ComebackCharge;
                OnClick_TabButton((int)curTab);
                return;
            }
        }

        comebackChargeEvent.gameObject.SetActive(curTab == TAB.ComebackCharge);
        comebackQuestEvent.gameObject.SetActive(curTab == TAB.Quest);
        comebackExchange.gameObject.SetActive(curTab == TAB.Exchange);
        comebackPackage.gameObject.SetActive(curTab == TAB.Package);

        var timeSpan = UserDataManager.Instance.userComebackData.user_comebackEnd_dt - TimeUtil.CurDateTime();
        TMP_TimeInfo.text = ProjectUtil.StringFormat("COMMON_REMAIN_DD_HH", timeSpan.Days.ToString("00"), timeSpan.Hours.ToString("00"));

        switch (curTab)
        {
            case TAB.ComebackCharge:
                comebackChargeEvent.InitInfo();
                break;

            case TAB.Quest:
                comebackQuestEvent.InitInfo();
                break;

            case TAB.Exchange:
                comebackExchange.InitInfo();
                break;

            case TAB.Package:
                comebackPackage.InitInfo();
                break;

            default:
                Logger.Log($"{System.Reflection.MethodBase.GetCurrentMethod().Name} is not define ({curTab})");
                break;
        }
    }

    private void InitTabButton()
    {
        List<UITabButton> btnList = tabGroup.GetTabButtonList();
        bool isQuestExpire = UserDataManager.Instance.userComebackData.IsComebackQuestExpire();
        btnList[(int)TAB.Quest].gameObject.SetActive(!isQuestExpire);
    }

    private void InitReddot()
    {
        reddotList[(int)TAB.ComebackCharge].UpdateIndex(REDDOT.COMEBACK_CHARGE, 0);
        reddotList[(int)TAB.Quest].UpdateIndex(REDDOT.COMEBACK_QUEST, 0);
    }

    public void OnClick_TabButton(int index)
    {
        curTab = (TAB)index;
        tabGroup.UpdateTab(index);

        if (UserDataManager.Instance.userComebackData.IsComebackExpire())
        {
            /// 메세지 박스를 문구와 함께 띄워야한다
            UIManager.Instance.ShowMessageBox(ProjectUtil.GetGameString(GAME_STR.MSG_COMEBACK_END), () =>
            // Check Action
            {                
                UserDataManager.Instance.ReqSyncAll(false, () =>
                {
                    PopPopup();
                });
            },
            // Cancle Action
            () => { },
            UIMessageBox.EMessageBoxType.OK);
            return;            
        }

        InitInfo();
    }
}
