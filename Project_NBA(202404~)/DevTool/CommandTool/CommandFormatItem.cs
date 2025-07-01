using Cysharp.Threading.Tasks;
using Dimps.Application.API;
using NBA.Game;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
namespace NBA.Game
{
    public class CommandFormatItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI tmp_commandName;
        [SerializeField] private TextMeshProUGUI tmp_serviceBeanName;
        [SerializeField] private TextMeshProUGUI tmp_description;

        [SerializeField] private Transform trans_paramLayout;

        [SerializeField] private UICommonButton BtnExecute;

        [SerializeField] private CommandParameterFormatItem prefab;

        private List<CommandParameterFormatItem> parameterFormats = new List<CommandParameterFormatItem>();

        private CommandPopup.CommandJsonInfo commandJsonInfo;

        public void InitCommandInfo(CommandPopup.CommandJsonInfo info)
        {
            commandJsonInfo = info;

            tmp_commandName.text = info.commandName;
            tmp_serviceBeanName.text = info.serviceBeanName;
            tmp_description.text = info.description;

            prefab.gameObject.SetActive(false);

            for (int i = 0; i < info.inputValues.Count; i++)
            {
                CommandParameterFormatItem newParamItem = Instantiate(prefab, trans_paramLayout);
                newParamItem.InitCommandParameterFormat(info.inputValues[i]);
                newParamItem.gameObject.SetActive(true);

                parameterFormats.Add(newParamItem);
            }

            BtnExecute.onClick.RemoveAllListeners();
            BtnExecute.onClick.AddListener(() => RequestWithJson().Forget());
        }

        public async UniTask RequestWithJson()
        {
            Dictionary<string, object> paramDic = new Dictionary<string, object>();

            foreach (var paramFormat in parameterFormats)
            {
                if (paramFormat.gameObject.activeSelf == false)
                    continue;

                string[] split = paramFormat.GetInputFieldData().Split(":");

                if (Type.GetType(split[1]) == typeof(bool))
                {
                    paramDic.Add(split[0], bool.Parse(split[2]));
                }

                else if (Type.GetType(split[1]) == typeof(System.Int32) ||
                    string.Equals(split[1], "Integer", StringComparison.OrdinalIgnoreCase))
                {
                    paramDic.Add(split[0], int.Parse(split[2]));
                }

                else if (Type.GetType(split[1]) == typeof(long) ||
                    Type.GetType(split[1]) == typeof(System.Int64))
                {
                    paramDic.Add(split[0], long.Parse(split[2]));
                }

                else if (Type.GetType(split[1]) == typeof(float))
                {
                    paramDic.Add(split[0], float.Parse(split[2]));
                }

                else if (Type.GetType(split[1]) == typeof(double))
                {
                    paramDic.Add(split[0], double.Parse(split[2]));
                }

                else if (Type.GetType(split[1]) == typeof(char))
                {
                    paramDic.Add(split[0], char.Parse(split[2]));
                }

                else if (Type.GetType(split[1]) == typeof(string) ||
                    string.Equals(split[1], "string", StringComparison.OrdinalIgnoreCase))
                {
                    paramDic.Add(split[0], split[2]);
                }

                else
                {
                    Debug.LogError("Cannot Set Value Other Types - " + Type.GetType(split[1]).Name);
                }
            }

            string reqJson = JsonConvert.SerializeObject(paramDic);

            var res = await APICommand.System.ExecuteCommand(commandJsonInfo.commandName, reqJson);
        }
    }
}
#endif
