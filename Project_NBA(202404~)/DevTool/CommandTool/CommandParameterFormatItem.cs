using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
namespace NBA.Game
{
    public class CommandParameterFormatItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI tmp_name;
        [SerializeField] private TextMeshProUGUI tmp_type;
        [SerializeField] private TextMeshProUGUI tmp_description;

        [SerializeField] private TMP_InputField inputField_parameter;

        private CommandPopup.CommandInputFormat inputFormat;

        public void InitCommandParameterFormat(CommandPopup.CommandInputFormat format)
        {
            inputFormat = format;

            tmp_name.text = format.name;
            tmp_type.text = format.type;
            tmp_description.text = format.description;
        }

        public string GetInputFieldData()
        {
            return string.Format($"{inputFormat.name}:{inputFormat.type}:{inputField_parameter.text}");
        }
    }
}
#endif