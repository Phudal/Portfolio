#if UNITY_EDITOR
using UnityEditor;
#endif

using Dimps.Utility.Singleton;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dimps.Application.API;
using Dimps.Application.API.Protobuf;
using Cysharp.Threading.Tasks;
using Dimps.Application.API.Message;
using System;
using System.Reflection;
using UnityEngine.Purchasing;
using System.Text;
using Google.Protobuf.Collections;
using System.Linq;
using System.Diagnostics.Contracts;

namespace GVNC.NBA.Util
{
    public class PacketTestManager : Singleton<PacketTestManager>, ISingleton
    {
        public string customURL = string.Empty;
        [Header("Request, Response는 빼고 넣으세요")]
        public string customType = string.Empty;

        private HashSet<Type> noneAuhtorizedType = new HashSet<Type>()
        {
            typeof(AccountCreateRequest),
            typeof(AccountSessionRequest),
            typeof(AccountLinkCheckRequest),
            typeof(AccountLinkDataSnsRequest),
            typeof(AccountLinkDataAuthKeyRequest),
            typeof(EncounterStartGameRequest),
            typeof(GetMaintenanceInfoRequest),
            typeof(StateRequest),
        };

#if UNITY_EDITOR
        private void Awake()
        {
            DontDestroyOnLoad(this);
        }
#endif

        #region ISingleton 관련 함수
        public async UniTask Init()
        {
            await UniTask.Yield();
        }

        public void Destroy()
        {
        }
        #endregion

        public List<string> CheckCurrentField()
        {
            Type requestType = Type.GetType("Dimps.Application.API.Message." + customType.Trim() + "Request");

            if (requestType == null)
            {
                Debug.LogError("Not found type - " + customType.Trim());
                return new List<string>();
            }

            FieldInfo[] allFields = requestType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            List<FieldInfo> fields = new List<FieldInfo>();
            foreach (FieldInfo field in allFields)
            {
                if (field.Name.Contains("unknownfields", StringComparison.OrdinalIgnoreCase))
                    continue;

                fields.Add(field);
            }

            List<string> ret = new List<string>();
            ret.Add(requestType.ToString());

            foreach (FieldInfo field in fields)
            {
                ret.Add(field.FieldType + " " + field.Name);
            }

            return ret;
        }

        public async UniTask SendTestPacket(string customParameter)
        {
            string url = customURL.Trim();

            if (string.IsNullOrEmpty(url))
            {
                Debug.LogError("URL is empty!!");
                return;
            }

            if (string.IsNullOrEmpty(customType.Trim()))
            {
                Debug.LogError("Custom Type is empty!!");
                return;
            }

            var config = APICommand.Instance.Config;
            var cancellationToken = APICommand.Instance.GetCancellationTokenSource().Token;

            Type requestType = Type.GetType("Dimps.Application.API.Message." + customType.Trim() + "Request");
            Type responseType = Type.GetType("Dimps.Application.API.Message." + customType.Trim() + "Response");

            if (requestType == null || responseType == null)
            {
                Debug.LogError("Not found type - " + customType.Trim());
                return;
            }

            var req = Activator.CreateInstance(requestType);

            FieldInfo[] allFields = requestType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            List<FieldInfo> fields = new List<FieldInfo>();
            foreach (FieldInfo field in allFields)
            {
                if (field.Name.Contains("unknownfields", StringComparison.OrdinalIgnoreCase))
                    continue;

                fields.Add(field);
            }

            string[] splited_parameter = customParameter.Split('|');

            for (int i = 0; i < fields.Count; i++)
            {
                if (splited_parameter[i] == string.Empty)
                {
                    continue;
                }

                if (fields[i].FieldType == typeof(bool))
                {
                    fields[i].SetValue(req, bool.Parse(splited_parameter[i]));
                }

                else if (fields[i].FieldType == typeof(System.Int32))
                {
                    fields[i].SetValue(req, int.Parse(splited_parameter[i]));
                }

                else if (fields[i].FieldType == typeof(long) || fields[i].FieldType == typeof(System.Int64))
                {
                    fields[i].SetValue(req, long.Parse(splited_parameter[i]));
                }

                else if (fields[i].FieldType == typeof(double))
                {
                    fields[i].SetValue(req, double.Parse(splited_parameter[i]));
                }

                else if (fields[i].FieldType == typeof(float))
                {
                    fields[i].SetValue(req, float.Parse(splited_parameter[i]));
                }

                else if (fields[i].FieldType == typeof(char))
                {
                    fields[i].SetValue(req, char.Parse(splited_parameter[i]));
                }

                else if (fields[i].FieldType == typeof(string))
                {
                    fields[i].SetValue(req, splited_parameter[i]);
                }

                else if (fields[i].FieldType == typeof(RepeatedField<bool>))
                {
                    RepeatedField<bool> rep = new RepeatedField<bool>();
                    foreach (string s in splited_parameter[i].Split(','))
                    {
                        rep.Add(bool.Parse(s));
                    }

                    fields[i].SetValue(req, rep);
                }

                else if (fields[i].FieldType == typeof(RepeatedField<System.Int32>))
                {
                    RepeatedField<int> rep = new RepeatedField<int>();
                    foreach (string s in splited_parameter[i].Split(','))
                    {
                        rep.Add(int.Parse(s));
                    }

                    fields[i].SetValue(req, rep);
                }

                else if (fields[i].FieldType == typeof(RepeatedField<long>) || fields[i].FieldType == typeof(RepeatedField<System.Int64>))
                {
                    RepeatedField<long> rep = new RepeatedField<long>();
                    foreach (string s in splited_parameter[i].Split(','))
                    {
                        rep.Add(long.Parse(s));
                    }

                    fields[i].SetValue(req, rep);
                }

                else if (fields[i].FieldType == typeof(RepeatedField<double>))
                {
                    RepeatedField<double> rep = new RepeatedField<double>();
                    foreach (string s in splited_parameter[i].Split(','))
                    {
                        rep.Add(double.Parse(s));
                    }

                    fields[i].SetValue(req, rep);
                }

                else if (fields[i].FieldType == typeof(RepeatedField<float>))
                {
                    RepeatedField<float> rep = new RepeatedField<float>();
                    foreach (string s in splited_parameter[i].Split(','))
                    {
                        rep.Add(float.Parse(s));
                    }

                    fields[i].SetValue(req, rep);
                }

                else if (fields[i].FieldType == typeof(RepeatedField<char>))
                {
                    RepeatedField<char> rep = new RepeatedField<char>();
                    foreach (string s in splited_parameter[i].Split(','))
                    {
                        rep.Add(char.Parse(s));
                    }

                    fields[i].SetValue(req, rep);
                }

                else if (fields[i].FieldType == typeof(RepeatedField<string>))
                {
                    RepeatedField<string> rep = new RepeatedField<string>();
                    foreach (string s in splited_parameter[i].Split(','))
                    {
                        rep.Add(s);
                    }

                    fields[i].SetValue(req, rep);
                }

                else
                {
                    Debug.LogError("Cannot Set Value Other Types - " + fields[i].FieldType);
                }

            }

            var reqMethod = typeof(APICore).GetMethod(nameof(APICore.SendRequest));
            var genericReqMethod = reqMethod.MakeGenericMethod(requestType, responseType);

            bool needAuthorized = !noneAuhtorizedType.Contains(requestType);

            object[] parameters = new object[]
            {
            url,
            req,
            config,
            cancellationToken,
            null,
            needAuthorized
            };

            var result = genericReqMethod.Invoke(null, parameters);

            if (result is UniTask uniTask)
            {
                await uniTask;
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PacketTestManager))]
    public class PacketTestEditor : Editor
    {
        private List<string> currentFields = new List<string>();

        private List<string> currentTextFields = new List<string>();

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            PacketTestManager manager = (PacketTestManager)target;

            if (currentFields.Count > 0)
            {
                GUILayout.Space(10);

                GUILayout.Label("Type Name", EditorStyles.boldLabel);
                GUILayout.Label(currentFields[0]);

                if (currentFields.Count > 1)
                {

                    if (currentTextFields.Count != currentFields.Count - 1)
                    {
                        for (int i = 1; i < currentFields.Count; i++)
                        {
                            currentTextFields.Add(string.Empty);
                        }
                    }

                    GUILayout.Space(10);
                    GUILayout.Label("Parameters", EditorStyles.boldLabel);

                    for (int i = 1; i < currentFields.Count; i++)
                    {
                        string[] splited = currentFields[i].Split(' ');
                        GUILayout.Label(splited[0]);
                        GUILayout.Label(splited[1]);

                        currentTextFields[i - 1] = GUILayout.TextArea(currentTextFields[i - 1]);
                    }
                }

                else
                {
                    GUILayout.Space(10);
                    GUILayout.Label("Parameter가 존재하지 않는 타입입니다.", EditorStyles.boldLabel);
                }
            }

            if (GUILayout.Button("Insert Parameters"))
            {
                currentFields = manager.CheckCurrentField();
            }

            if (GUILayout.Button("Send Test Packet"))
            {
                StringBuilder sb = new StringBuilder();

                foreach (string parameter in currentTextFields)
                {
                    sb.Append(parameter.Trim());
                    sb.Append('|');
                }

                if (sb.Length > 1)
                {
                    sb.Remove(sb.Length - 1, 1);
                }

                manager.SendTestPacket(sb.ToString()).Forget();
            }

            GUILayout.Space(30);
            GUILayout.Label("How to use", EditorStyles.boldLabel);
            GUILayout.Label("1. 패킷을 보낼 url, proto에 선언된 클래스 타입을 넣으세요");
            GUILayout.Label("2. Insert Parameters를 눌러 패킷에 들어갈 파라미터를 넣으세요");
            GUILayout.Label("3. Send Test Packet을 눌러 패킷을 보내세요.");
            GUILayout.Label("Repeated Field 형식은 ',' 구분자로 넣어주세요");
            GUILayout.Label("단, 해당 테스터는 파라미터로 객체 형식은 지원하지 않습니다.");
        }
    }
#endif
}