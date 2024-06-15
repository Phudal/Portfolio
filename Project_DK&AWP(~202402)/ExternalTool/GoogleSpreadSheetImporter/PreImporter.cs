using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "PreImporter", menuName = "Scriptable Object/PreImporter")]
public class PreImporter : ScriptableSingleton<PreImporter>
{
    private new static PreImporter instance;
    public static PreImporter Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<PreImporter>();

            if (instance == null)
                instance = Resources.Load<PreImporter>("ScriptableObject/PreImporter");

            return instance;
        }
    }

    private static string[] Scopes = { SheetsService.Scope.Spreadsheets };
    private static string ApplicationName = "Google Sheets API .NET Quickstart";

    [SerializeField]
    private string spreadSheetName = "Spread Sheet List";

    [SerializeField]
    private SerializableDictionary<string, string> SpreadSheetNameDictionary;

    private static UserCredential credential;
    private static SheetsService service;

    private static readonly String spreadSheetId = "1bGOwhnGMXKaDFFBis7r6KFmTPZo1fPLAbe6RDvRsTpY";
    private static String range = "SpreadSheetNameList";

    static IList<IList<object>> savedValue;    

    [MenuItem("GoogleSheet/PreImport Id")]
    public static void PreImportSpreadSheetId()
    {
        PreImporter.Instance.UserCredentialInitialize();

        service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName
        });

        PreImporter.Instance.ReadSpreadSheetExceptNull();
    }

    private void UserCredentialInitialize()
    {
        using (var stream =
            new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
        {
            string credPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);

            credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(credPath, true)).Result;
        }
    }

    private void ReadSpreadSheetExceptNull()
    {
        SpreadsheetsResource.ValuesResource.GetRequest request =
            service.Spreadsheets.Values.Get(spreadSheetId, range);

        ValueRange response = request.Execute();

        if (response.Values != null)
        {
            int cutIndex = 0;

            for (int index = 0; index < response.Values[0].Count; index++)
            {
                if (string.IsNullOrEmpty(response.Values[0][index].ToString()))
                {
                    cutIndex = index;
                    break;
                }
                cutIndex++;
            }

            // range = $"{range}!A1:{GetExcelColumnName(cutIndex)}";
            // response.Range = range;

            response.Range = $"{range}!A1:{GetExcelColumnName(cutIndex)}";
            

            for (int row = 0; row < response.Values.Count; row++)
            {
                for (; cutIndex < response.Values[row].Count;)
                    response.Values[row].RemoveAt(cutIndex);
            }


            SpreadSheetNameDictionary = new SerializableDictionary<string, string>();

            for (int row = 1; row < response.Values.Count; row++)
            {
                SpreadSheetNameDictionary.Add(response.Values[row][0].ToString(), response.Values[row][1].ToString());                
            }
            // savedValue = response.Values;

            EditorUtility.SetDirty(this);
        }        
    }

    private string GetExcelColumnName(int columnNumber)
    {
        string columnName = "";

        while (columnNumber > 0)
        {
            int modulo = (columnNumber - 1) % 26;
            columnName = Convert.ToChar('A' + modulo) + columnName;
            columnNumber = (columnNumber - modulo) / 26;
        }

        return columnName;
    }

    public SerializableDictionary<string, string> GetSpreadSheetIdDictionary()
    {
        return SpreadSheetNameDictionary;
    }    
}
