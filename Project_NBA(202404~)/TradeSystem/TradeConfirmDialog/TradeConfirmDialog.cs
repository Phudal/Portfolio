using Cysharp.Threading.Tasks;
using Dimps.Application.API;
using Dimps.Application.Common;
using Dimps.Application.Common.Button;
using Dimps.Application.Common.UI;
using Dimps.Application.Global;
using Dimps.Application.MasterData;
using Dimps.Application.MasterData.Master;
using Dimps.Application.Scene;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GVNC.Application.Trade
{
    public class TradeConfirmDialog : SceneBase<TradeConfirmDialog.Param>
    {
        public enum DialogType
        {
            Normal,
            CostView,
            ReverseCostView,
        }

        public class Param : ISceneParameter
        {
            public DialogType dialogType;
            public Action confirmAction;
            public ItemType costItemType;
            public int costItemId;
            public int cost;
            public string titleIndex;
            public string messageIndex;

            public Param(DialogType dialogType, ItemType itemType, int costItemId, int cost, string title, string message, Action confirmAction)
            {
                this.dialogType = dialogType;
                this.costItemType = itemType;
                this.costItemId = costItemId;
                this.cost = cost;
                this.titleIndex = title;
                this.messageIndex = message;
                this.confirmAction = confirmAction;
            }
        }

        [SerializeField] private UILocalizeText tmp_title;
        [SerializeField] private UILocalizeText tmp_message;

        [SerializeField, Header("Cost")]
        private GameObject gameObject_CostInfo;
        [SerializeField] private Image image_costIcon;
        [SerializeField] private Image image_curItemIcon;
        [SerializeField] private TextMeshProUGUI tmp_Cost;
        [SerializeField] private TextMeshProUGUI tmp_beforeMoney;
        [SerializeField] private TextMeshProUGUI tmp_afterMoney;

        [SerializeField, Header("Butttons")] private UICustomButton btn_OK;
        [SerializeField] private UICustomButton btn_Cancel;

        private Param curParam;

        public static void Open(Param param)
        {
            SceneProvider.Instance.AddScene(SceneContent.Type.TradeConfirmDialog, param);
        }

        protected override Param OnRootStart()
        {
            var param = new Param(DialogType.Normal, ItemType.None, 0, 0, string.Empty, string.Empty, null);
            return param;
        }
        protected override void OnInitialize(Param param)
        {
            curParam = param;

            btn_Cancel.onClick.AddListener(() => CloseDialog(null));
            btn_OK.onClick.AddListener(() => { 
                curParam.confirmAction?.Invoke();
                CloseDialog(null);
            });

        }

        protected override async UniTask OnLoad(CancellationToken token)
        {            
            await APIUtility.EditorSimpleLogin();

            await UniTask.Yield(cancellationToken: token);

            tmp_title.SetLocalizeText(curParam.titleIndex);
            string message = LanguageManager.Instance.GetOSTText(curParam.messageIndex).Replace("\\n", "\n");
            tmp_message.SetTextDirect(message);

            gameObject_CostInfo.SetActive(curParam.cost > 0 && (curParam.dialogType == DialogType.CostView || curParam.dialogType == DialogType.ReverseCostView));
            if (curParam.cost <= 0 || curParam.dialogType == DialogType.Normal)
            {
                return;
            }

            Sprite sprite = await GlobalFunction.GetItemIcon(curParam.costItemId, ItemType.TradingCard);
            image_costIcon.sprite = sprite;
            image_curItemIcon.sprite = sprite;

            int userHaveItemNum = GlobalDataManager.Instance.GlobalUser.UserInfo.GetUserHaveItemNum(curParam.costItemType, curParam.costItemId);
            
            tmp_beforeMoney.text = userHaveItemNum.ToString();
            tmp_Cost.text = curParam.cost.ToString();

            if (curParam.dialogType == DialogType.ReverseCostView)
            {                
                tmp_afterMoney.text = (userHaveItemNum + curParam.cost).ToString();
                btn_OK.Inactive = userHaveItemNum + curParam.cost < 0;
            }
            else
            {                
                tmp_afterMoney.text = (userHaveItemNum - curParam.cost).ToString();
                btn_OK.Inactive = userHaveItemNum - curParam.cost < 0;
            }
        }

        protected override async UniTask OnActivate(CancellationToken token)
        {
            await UniTask.Yield();
        }

        protected override void OnExecute(Param param)
        {

        }

        protected override void OnDispose()
        {

        }
    }
}