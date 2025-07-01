#if UNITY_EDITOR
using Cysharp.Threading.Tasks; 
using UnityEngine;
using UnityEngine.UI;
using NBA.Common;
using Dimps.Application.API;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using NBA.Lib;



namespace NBA.Game
{
    public class CommandPopup : UIPopupBase
    {

        public class CommandJsonInfo
        {
            [Newtonsoft.Json.JsonProperty("commandName")]
            public string commandName;

            [Newtonsoft.Json.JsonProperty("serviceBeanName")]
            public string serviceBeanName;

            [Newtonsoft.Json.JsonProperty("description")]
            public string description;

            [Newtonsoft.Json.JsonProperty("inputValues")]
            public List<CommandInputFormat> inputValues;
        }

        public class CommandInputFormat
        {
            [Newtonsoft.Json.JsonProperty("name")]
            public string name;

            [Newtonsoft.Json.JsonProperty("type")]
            public string type;

            [Newtonsoft.Json.JsonProperty("description")]
            public string description;
        }


        [SerializeField] private ScrollRect scroll;

        [SerializeField] private Transform trans_Content;
        [SerializeField] private CommandFormatItem formatPrefab;

        [SerializeField] private UICommonButton BtnClose;

        public async UniTask InitPopupAsync()
        {
            formatPrefab.gameObject.SetActive(false);

            var res = await APICommand.System.CommandList();

            List<CommandJsonInfo> jsonInfoList = JsonConvert.DeserializeObject<List<CommandJsonInfo>>(res.commandResponse.CommandJson);

            foreach (CommandJsonInfo info in jsonInfoList)
            {
                CommandFormatItem formatObjcet = Instantiate(formatPrefab, trans_Content);
                formatObjcet.InitCommandInfo(info);
                formatObjcet.gameObject.SetActive(true);
            }

            await UniTask.Yield();

            scroll.verticalNormalizedPosition = 1.0f;

            BtnClose.onClick.RemoveAllListeners();
            BtnClose.onClick.AddListener(OnClickClose);
        }

        public override void OnClickClose()
        {
            base.OnClickClose();
        }

    }
}
#endif