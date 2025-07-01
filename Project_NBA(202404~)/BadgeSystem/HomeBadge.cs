using Cysharp.Threading.Tasks;
using Dimps.Application.API;
using Dimps.Application.API.Message;
using Dimps.Application.AssetBundle;
using Dimps.Application.Common;
using Dimps.Application.Global;

using NBA.Common;
using NBA.Game;
using NBA.Lib;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HomeBadge : MonoBehaviour
{
    [SerializeField] private Image image_bg;
    [SerializeField] private Image image_icon;
    [SerializeField] private Image image_progress;

    [SerializeField] private GameObject gameObject_NoneGauge;
    [SerializeField] private GameObject gameObject_Gauge;

    [SerializeField] private GameObject gameObject_TimeInfo;
    [SerializeField] private GameObject gameObject_GetReward;

    [SerializeField] private TextMeshProUGUI tmp_remainTime;

    [SerializeField] private GameObject gameObject_reddot;

    [SerializeField] private Button button_onClick;

    public BadgeTableData badgeEntity;
    private List<BmGroupTableData> bmGroupEntities = new List<BmGroupTableData>();
    private PBillingSummary pBillingSummary;
    public PBadgeDetail responseDetail;
    private eBMType bmType;

    public eBMType BM => bmType;

    public void Init(PBadgeDetail badgeDetail)
    {
        responseDetail = badgeDetail;
        badgeEntity = TableDataManager.Instance.GetData<BadgeTableData>(badgeDetail.Id);
        if (badgeEntity == null)
        {
            DebugTool.LogError($"뱃지 데이터를 찾을 수 없음 {badgeDetail.Id}");
            gameObject.SetActive(false);
            return;
        }

        if (responseDetail.Type == (int)eBadgeDetailType.BM)
        {
            bmGroupEntities = TableDataManager.Instance.Extend.GetBmGroupList(badgeEntity.TargetId);
            if (bmGroupEntities == null)
            {
                DebugTool.LogError($"뱃지에 연결된 BM 데이터를 찾을 수 없음 {badgeEntity.TargetId}");
                gameObject.SetActive(false);
                return;
            }

            bmType = (eBMType)bmGroupEntities.FirstOrDefault().Type;

            // pBillingSummary는 Fixed Update에서 남은 시간 표기가 실시간으로 필요하여
            // 접근이 필요하기 때문에 캐싱하여 사용
            switch (bmType)
            {
                case eBMType.SeasonPass:
                case eBMType.MonthlyPass:
                case eBMType.ConsecutivePurchase:
                case eBMType.Package_Event:
                    pBillingSummary = responseDetail.BillingSummary.FirstOrDefault();
                    break;

                // 나머지 BM은 TODO로, pbillingSummary를 null로 두어 처리한다.
                default:
                    pBillingSummary = null;
                    break;
            }
        }

        if (responseDetail.Type == (int)eBadgeDetailType.Event)
        {
            ShowRedDot(responseDetail.EventSummary.IsEnableReward || responseDetail.IsNew, badgeEntity.Position);
        }
        else if (responseDetail.Type == (int)eBadgeDetailType.BM)
        {
            if (pBillingSummary == null && responseDetail.IsDisplayBlocked)
            {
                return;
            }

            if (pBillingSummary == null)
            {
                DebugTool.LogError("[미구현 or 개발중 컨텐츠 데이터 들어옴] pBillingSummary is null");
                return;
            }

            ShowRedDot(pBillingSummary.IsEnableReward || responseDetail.IsNew, badgeEntity.Position);
        }

        button_onClick.onClick.RemoveAllListeners();
        button_onClick.onClick.AddListener(() =>
        {
            OnClicked_Badge().Forget();
        });

        // Design Type
        // 0 : Normal
        // 1 : Guage
        // 3 : BM

        gameObject_TimeInfo.SetActive(true);
        gameObject_GetReward.SetActive(false);

        gameObject_NoneGauge.SetActive(badgeEntity.DesignType != (int)eBadgeDesignType.Gauge);
        gameObject_Gauge.SetActive(badgeEntity.DesignType == (int)eBadgeDesignType.Gauge);

        LoadBadgeIcon();

        RefreshBadge();
    }

    private void ShowRedDot(bool isShow, int pos)
    {
        gameObject_reddot.SetActive(isShow);
        if (isShow)
        {
            var rectTrans = gameObject_reddot.GetComponent<RectTransform>();
            rectTrans.anchoredPosition = Vector2.zero;
            // rectTrans.anchoredPosition = new Vector2((pos == 0) ? -68 : 0, 0);
        }
    }

    public void RefreshBadge()
    {
        // Event Type
        if (responseDetail.Type == (int)eBadgeDetailType.Event)
        {
            PEventSummary pEventSummary = responseDetail.EventSummary;

            long remainTime = (pEventSummary.FixDate - GlobalDataManager.Instance.GlobalTime.CurrentTime);
            if (remainTime > 0)
            {
                tmp_remainTime.text = GlobalFunction.GetRemainTimeText((int)(remainTime / 1000));
            }
            else if (CheckInPeriod())
            {
                tmp_remainTime.text = "FINISH";
            }
        }

        // BM Type
        else if (responseDetail.Type == (int)eBadgeDetailType.BM)
        {
            if (pBillingSummary == null)
            {
                DebugTool.LogError($"BM 뱃지에 들어간 pBillingSummary 데이터가 없습니다. {responseDetail.Id}");
                return;
            }
                

            long remainTime = (pBillingSummary.EndDate - GlobalDataManager.Instance.GlobalTime.CurrentTime);
            if (remainTime > 0)
                tmp_remainTime.text = GlobalFunction.GetRemainTimeText((int)(remainTime / 1000));
            else
            {
                tmp_remainTime.text = "FINISH";
            }
        }
        else
        {
            tmp_remainTime.text = "";
        }
    }

    public void InitEmpty()
    {
        responseDetail = null;
        badgeEntity = null;
        responseDetail = null;
    }

    public bool CheckInPeriod()
    {
        if (responseDetail == null)
            return false;

        if (responseDetail.Type == (int)eBadgeDetailType.Event)
        {
            return GlobalFunction.CheckContainsPeriod(responseDetail.EventSummary.StartDate, responseDetail.EventSummary.EndDate);
        }
        else
        {
            foreach (PBillingSummary billingSummary in responseDetail.BillingSummary)
            {
                if (GlobalFunction.CheckContainsPeriod(billingSummary.StartDate, billingSummary.EndDate))
                    return true;
            }
            return false;
        }
    }

    public int GetBadgeIndex()
    {
        if (responseDetail == null)
            return -1;

        return responseDetail.Id;
    }

    public async UniTask OnClicked_Badge()
    {        
        if (General.Instance.HomeBadgeController.isOpeningPopup)
        {
            return;
        }

        Broadcaster<bool>.Broadcast(EBroadcastKey.OpenBadgePopup, true);

        PEventSummary eventSummary = responseDetail.EventSummary;
        BadgeTableData badgeEntity = TableDataManager.Instance.GetData<BadgeTableData>(responseDetail.Id);
       
        (GetPopupInfoResponse getPopupInfoResponse, Response commonResponse) res_common = default;
        (SeasonPassPopupInfoResponse seasonPassPopupInfoResponse, Response commonResponse) res_seasonPass = default;

        if ((eBadgeDetailType)responseDetail.Type == eBadgeDetailType.BM)
        {
            List<BmGroupTableData> m_bmGroupEntities = TableDataManager.Instance.Extend.GetBmGroupList(badgeEntity.TargetId);
            if (m_bmGroupEntities == null)
            {
                Broadcaster<bool>.Broadcast(EBroadcastKey.OpenBadgePopup, false);
                return;
            }

            if ((eBMType)m_bmGroupEntities.FirstOrDefault().Type != eBMType.SeasonPass)
            {
                res_common = await APICommand.BillingShop.GetPopupInfoRequest(badgeEntity.Id, allowErrorCodes: new int[] { (int)APIStatusCode.ERROR_MASTER_NOT_FOUND });
            }
            else
            {
                res_seasonPass = await APICommand.Badge.SeasonPassPopupInfo(badgeEntity.Id, allowErrorCodes: new int[] { (int)APIStatusCode.ERROR_MASTER_NOT_FOUND });
            }
        }

        if ((res_common != default && res_common.commonResponse.Code != 0) || 
            (res_seasonPass != default && res_seasonPass.commonResponse.Code != 0))
        {
            DebugTool.LogError($"{res_common.commonResponse.Message}");

            // NOTE - 서버 측 예외 처리 요청
            // APIStatusCode.ERROR_MASTER_NOT_FOUND 상황이 발생했을 때, 유저 뱃지 정보를 초기화하고
            // HomeBadgeController를 통해 데이터를 업데이트한 뒤에 다시 버튼 처리를 이어간다.
            if ((res_common != default && res_common.commonResponse.Code == (int)APIStatusCode.ERROR_MASTER_NOT_FOUND) ||
                res_seasonPass != default && res_seasonPass.commonResponse.Code == (int)APIStatusCode.ERROR_MASTER_NOT_FOUND)
            {
                await General.Instance.HomeBadgeController.IntializeBadgeData(true);

                if (res_common != default)
                {
                    res_common = await APICommand.BillingShop.GetPopupInfoRequest(badgeEntity.Id);
                }
                else if (res_seasonPass != default)
                {
                    res_seasonPass = await APICommand.Badge.SeasonPassPopupInfo(badgeEntity.Id);
                }

                DebugTool.LogError($"Retry Code After ERROR_MASTER_NOT_FOUND(GetPopupInfo) {res_common.commonResponse.Message}");

                if ((res_common != default && res_common.commonResponse.Code != 0) ||
                    (res_seasonPass != default && res_seasonPass.commonResponse.Code != 0))
                {
                    Broadcaster<bool>.Broadcast(EBroadcastKey.OpenBadgePopup, false);
                    return;
                }
            }
            else
            {
                Broadcaster<bool>.Broadcast(EBroadcastKey.OpenBadgePopup, false);
                return;
            }
        }        

        if (responseDetail.Type == (int)eBadgeDetailType.Event)
        {
            eBadgeEntryType entryType = (eBadgeEntryType)TableDataManager.Instance.Extend.GetEventInfoByEventID(badgeEntity.TargetId).Type;
            
            // 타입에 따른 입장 처리
            switch (entryType)
            {
                case eBadgeEntryType.DailyDungeon:                                        
                    OpenDailyDungeonPopupAscyn(responseDetail.Id).Forget();
                    break;

                default:
                    break;
            }

            Broadcaster<bool>.Broadcast(EBroadcastKey.OpenBadgePopup, false);
        }

        else if (responseDetail.Type == (int)eBadgeDetailType.BM)
        {
            List<BmGroupTableData> bmGroupEntities = TableDataManager.Instance.Extend.GetBmGroupList(badgeEntity.TargetId);
            if (bmGroupEntities == null)
            {
                Broadcaster<bool>.Broadcast(EBroadcastKey.OpenBadgePopup, false);
                return;
            }

            switch ((eBMType)bmGroupEntities.FirstOrDefault().Type)
            {
                case eBMType.SeasonPass:
                    GlobalDataManager.Instance.GlobalUser.RefreshUserSeasonPassData(res_seasonPass.seasonPassPopupInfoResponse);                    
                    var passPopup = await UIManager.Instance.OpenPopup<SeasonPassDetailPopup>(eUIPopup.SeasonPassDetail);
                    passPopup.InitPopup(responseDetail.Id, res_seasonPass.seasonPassPopupInfoResponse.Pass.PassId, pBillingSummary, res_seasonPass.seasonPassPopupInfoResponse);
                    break;

                case eBMType.MonthlyPass:
                    var popup = await UIManager.Instance.OpenPopup<MonthlyPassShopPopup>(eUIPopup.MonthlyPassShopPopup);
                    popup.SetData(bmGroupEntities, res_common.getPopupInfoResponse, badgeEntity.Id);

                    break;

                case eBMType.ConsecutivePurchase:
                    var consecPopup = await UIManager.Instance.OpenPopup<ConsecutivePurchasePopup>(eUIPopup.ConsecutivePurchasePopup);
                    consecPopup.InitPopup(responseDetail.Id, bmGroupEntities.FirstOrDefault().LinkId, pBillingSummary, res_common.getPopupInfoResponse, pBillingSummary.CurStep);
                    break;

                case eBMType.Package_Event:
                    var eventPackagePopup = await UIManager.Instance.OpenPopup<EventPackagePopup>(eUIPopup.EventPackagePopup);
                    eventPackagePopup.InitPopup(responseDetail.Id, bmGroupEntities.FirstOrDefault().LinkId, pBillingSummary, res_common.getPopupInfoResponse);
                    break;

                default:
                    break;
            }

            Broadcaster<bool>.Broadcast(EBroadcastKey.OpenBadgePopup, false);
        }
    }

    private async UniTask OpenDailyDungeonPopupAscyn(int badgeId)
    {
        DailyDungeonTop popup = await UIManager.Instance.OpenPopup<DailyDungeonTop>(eUIPopup.DailyDungeonTop);
        await popup.InitPopup(badgeId);
    }

    public bool CheckDisplayBlock()
    {
        if (responseDetail == null)
            return true;

        return responseDetail.IsDisplayBlocked;
    }

    public bool IsResponseDetailNull()
    {
        return responseDetail == null;
    }

    public bool IsDisplayBlock()
    {
        return responseDetail.IsDisplayBlocked;
    }

    public void SetDisplayBlock(bool value)
    {
        responseDetail.IsDisplayBlocked = value;
    }

    private void LoadBadgeIcon()
    {
        string resourceName = string.Empty;

        if (responseDetail.Type == (int)eBadgeDetailType.BM)
        {
            if (bmType == eBMType.SeasonPass)
            {
                resourceName = TableDataManager.Instance.GetData<SeasonPassTableData>(responseDetail.BillingSummary[0].BmId).ResourceName;
            }
            else if (bmType == eBMType.Package_Event)
            {
                resourceName = TableDataManager.Instance.GetData<EventPackageTableData>(responseDetail.BillingSummary[0].BmId).ResourcesName;
            }

            Sprite cached = UIResourceHelper.Badge.GetBMBadgeIcon(bmType, resourceName);

            if (cached != null)
                image_icon.sprite = cached;
        }
    }
}
