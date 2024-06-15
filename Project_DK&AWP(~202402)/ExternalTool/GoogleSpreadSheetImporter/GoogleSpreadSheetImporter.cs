using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

using UnityEditor;

public class GoogleSpreadSheetImporter
{
    private static GoogleSpreadSheetImporter instance;

    public static GoogleSpreadSheetImporter Instance
    {
        get
        {
            if (instance == null)
                instance = new GoogleSpreadSheetImporter();

            return instance;
        }
    }

    static string[] Scopes = { SheetsService.Scope.Spreadsheets };
    static string ApplicationName = "Google Sheets API .NET Quickstart";

    public static string sheetName = "시트1";
    static string startIndex = "A1";
    static string endIndex = "G";

    static string spreadSheetName = "";
    static List<string> sheetsNameList = new List<string>();

    static IList<IList<object>> savedValue;
    static IList<IList<object>> tmp;

    static Dictionary<string, IList<IList<object>>> savedSheets = new Dictionary<string, IList<IList<object>>>();

    static UserCredential credential;

    static SheetsService service;

    static String spreadsheetId = "1_CAYeJZo137FKAR1NqPDZH-MYhuaZspO33bxxZG-0I8";

    static String range;

    
    [MenuItem("GoogleSheet/Import And Make CSV")]
    public static void GetAllSpreadSheets2CSV()
    {
        // 사용자 인증 정보 초기화
        GoogleSpreadSheetImporter.Instance.Initialize();

        service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName
        });

        spreadsheetId = PreImporter.Instance.GetSpreadSheetIdDictionary()["default"];

        range = sheetName + "!" + startIndex + ":" + endIndex;

        GoogleSpreadSheetImporter.Instance.ReadSpreadSheetExceptNull();

        // 암호화 코드 + StringBuilder 사용으로 최적화
         
        GoogleSpreadSheetImporter.Instance.WriteAll2EncryptedCSV_V3();                
    }

    public static void GetAllSpreadSheets2CSV(string _sheetName)
    {
        // 사용자 인증 정보 초기화
        GoogleSpreadSheetImporter.Instance.Initialize();

        service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName
        });

        spreadsheetId = PreImporter.Instance.GetSpreadSheetIdDictionary()[_sheetName];

        //{
        //    SpreadsheetsResource.GetRequest request =
        //        service.Spreadsheets.Get(spreadsheetId);

        //    Spreadsheet response = request.Execute();

        //    sheetName = response.Sheets[0].Properties.Title;
        //}

        range = sheetName + "!" + startIndex + ":" + endIndex;

        Debug.Log("Before Read Sheet id - " + spreadsheetId);

        GoogleSpreadSheetImporter.Instance.GetSheetName();
        GoogleSpreadSheetImporter.Instance.ReadSpreadSheetExceptNull();        

        Debug.Log("After Read Sheet id - " + spreadsheetId);

        // 암호화 코드 + StringBuilder 사용으로 최적화
        // GoogleSpreadSheetImporter.Instance.WriteAll2CSV();
        GoogleSpreadSheetImporter.Instance.WriteAll2EncryptedCSV_V3();
    }

    public void GetGoogleSpreadSheets()
    {
        // 사용자 인증 정보 초기화
        GoogleSpreadSheetImporter.Instance.Initialize();

        service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName
        });

        // spreadsheetId = "1_CAYeJZo137FKAR1NqPDZH-MYhuaZspO33bxxZG-0I8";

        range = sheetName + "!" + startIndex + ":" + endIndex;

        GoogleSpreadSheetImporter.Instance.ReadSpreadSheetExceptNull();
    }

    private void Initialize()
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

    private void CreateEntry()
    {
        var valueRange = new ValueRange();

        var objectList = new List<object>() { "Hello", "This", "was", "inserted", "via", "C#" };
        valueRange.Values = new List<IList<object>> { objectList };

        var appendRequest = service.Spreadsheets.Values.Append(valueRange, spreadsheetId, range);
        appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
        var appendResponse = appendRequest.Execute();
    }

    private void ReadSpreadSheet()
    {
        SpreadsheetsResource.ValuesResource.GetRequest request =
            service.Spreadsheets.Values.Get(spreadsheetId, range);

        ValueRange response = request.Execute();
        IList<IList<object>> values = response.Values;

        if (values != null && values.Count > 0)
        {
            // Console.WriteLine("종류별 나열");

            // Console.WriteLine("values Count - " + values.Count);

            savedValue = values;

            foreach (var row in values)
            {
                foreach (var column in row)
                {
                    Console.Write(column + " ");
                }
                Console.WriteLine();
            }

            // GoogleSpreadSheetImporter.Instance.Write2CSV();
            // Read2CSV();
        }
        else
        {
            Console.WriteLine("No Data Found.");
        }
    }

    private void ReadAllSpreadSheet()
    {
        if (sheetsNameList == null || sheetsNameList.Count < 1)
            GetSheetName();

        foreach (var _sheetName in sheetsNameList)
        {
            // range = $"{_sheetName}!{startIndex}:{endIndex}";
            range = _sheetName;

            SpreadsheetsResource.ValuesResource.GetRequest request =
                service.Spreadsheets.Values.Get(spreadsheetId, range);

            ValueRange response = request.Execute();

            if (response.Values != null)
            {
                savedSheets.Add(_sheetName, response.Values);

                savedValue = response.Values;
                Write2CSV(_sheetName);

                Debug.Log(_sheetName + " 저장중");
            }

        }
    }

    private void Write2CSV(string _fileName = "test")
    {
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@$"{_fileName}.csv", false,
            System.Text.Encoding.GetEncoding("utf-8")))
        {
            foreach (var row in savedValue)
            {
                foreach (var index in row)
                {
                    if (row.Last() == index)
                        file.Write(index);

                    else
                        file.Write(index + "\t");
                }
                file.WriteLine();
            }
        }
    }

    private void WriteAll2CSV()
    {
        string m_Path = Application.streamingAssetsPath;        

        m_Path += "/Resource/DataTable/";

        foreach (var _fileName in sheetsNameList)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@$"{m_Path}{_fileName}.csv", false,
                System.Text.Encoding.GetEncoding("utf-8")))
            {

                int ExceptIndex = -1;

                foreach (var row in savedSheets[_fileName])
                {                   
                    foreach (var index in row)
                    {
                        if (index.ToString()[0] == '#')
                        {
                            ExceptIndex = row.IndexOf(index);

                            // Debug.Log(ExceptIndex);
                            continue;
                        }

                        if (ExceptIndex != -1)
                        {
                            if (ExceptIndex == row.IndexOf(index))
                                continue;
                        }

                        if (row.Last() == index)
                        {
                            file.Write(index);
                        }

                        else
                        {
                            file.Write(index + "\t");
                        }
                    }
                    file.WriteLine();
                }
            }

            Debug.Log("시트 저장됨 - " + _fileName);
        }
    }

    private void WriteAll2EncryptedCSV_V1()
    {
        string m_Path = Application.streamingAssetsPath;

        m_Path += "/Resource/DataTable/";

        foreach (var _fileName in sheetsNameList)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@$"{m_Path}{_fileName}.csv", false,
                System.Text.Encoding.GetEncoding("utf-8")))
            {
                foreach (var row in savedSheets[_fileName])
                {
                    foreach (var index in row)
                    {
                        if (row.Last() == index)
                        {
                            file.Write(AESCrypto.Instance.AESEncrypt((string)index));
                        }

                        else
                        {
                            file.Write(AESCrypto.Instance.AESEncrypt((string)index) + "\t");
                        }
                    }
                    file.WriteLine();
                }
            }

            Debug.Log("시트 저장됨 - " + _fileName);
        }
    }

    private void WriteAll2EncryptedCSV_V2()
    {
        string m_Path = Application.streamingAssetsPath;

        m_Path += "/Resource/DataTable/";

        foreach (var _fileName in sheetsNameList)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@$"{m_Path}{_fileName}.csv", false,
                System.Text.Encoding.GetEncoding("utf-8")))
            {
                string buffer = "";

                foreach (var row in savedSheets[_fileName])
                {
                    foreach (var index in row)
                    {
                        if (row.Last() == index)
                        {
                            buffer += index;
                        }

                        else
                        {
                            buffer += index + "\t";
                        }
                    }
                    buffer += "\r\n";
                }

                file.WriteLine(AESCrypto.Instance.AESEncrypt(buffer));                
            }

            Debug.Log("시트 저장됨 - " + _fileName);
        }
    }

    private void WriteAll2EncryptedCSV_V3()
    {
        string m_Path = Application.streamingAssetsPath;

        m_Path += "/Resource/DataTable/";

        foreach (var _fileName in sheetsNameList)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@$"{m_Path}{_fileName}.csv", false,
                System.Text.Encoding.GetEncoding("utf-8")))
            {
                // string buffer = "";

                int ExceptIndex = -1;

                StringBuilder sb = new StringBuilder();

                foreach (var row in savedSheets[_fileName])
                {                    
                    foreach (var index in row)
                    {

                        if (index.ToString()[0] == '#')
                        {
                            ExceptIndex = row.IndexOf(index);

                            Debug.Log(ExceptIndex);
                            continue;
                        }

                        if (ExceptIndex != -1)
                        {
                            if (ExceptIndex == row.IndexOf(index))
                                continue;
                        }

                        if (row.Last() == index)
                        {                            
                            sb.Append(index);
                        }

                        else
                        {                            
                            sb.Append(index);
                            sb.Append("\t");                            
                        }
                    }                    
                    sb.Append("\r\n");
                }

                file.WriteLine(AESCrypto.Instance.AESEncrypt(sb.ToString()));
            }

            Debug.Log("시트 저장됨 - " + _fileName);
        }
    }

    private void Read2CSV()
    {
        using (System.IO.StreamReader file = new System.IO.StreamReader(@"test.csv"))
        {
            string tmp = file.ReadToEnd();

            foreach (var index in tmp)
            {
                if (index == '\t')
                    Debug.Log("탭");
                // Console.Write(" 탭 ");

                else
                    Debug.Log(index);
                    // Console.Write(index);
            }
        }
    }

    private void Updatesheet()
    {
        var valueRange = new ValueRange();

        var objectList = new List<object>() { "updated", "updated2" };
        valueRange.Values = new List<IList<object>> { objectList };

        var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        var appendResponse = updateRequest.Execute();
    }

    private void GetSpreadSheetName()
    {
        SpreadsheetsResource.GetRequest request =
            service.Spreadsheets.Get(spreadsheetId);

        Spreadsheet response = request.Execute();

        spreadSheetName = response.Properties.Title;
    }

    private void GetSheetName()
    {
        SpreadsheetsResource.GetRequest request =
            service.Spreadsheets.Get(spreadsheetId);

        Spreadsheet response = request.Execute();

        sheetsNameList = new List<string>();

        for (int i = 0; i < response.Sheets.Count; i++)
            sheetsNameList.Add(response.Sheets[i].Properties.Title);

        //foreach (var sheet in response.Sheets)
        //{
        //    // Console.WriteLine(sheet.Properties.Title);                       
        //    sheetsNameList.Add(sheet.Properties.Title);
        //}
    }

    private void ReadSpreadSheetExceptNull()
    {
        if (sheetsNameList == null || sheetsNameList.Count < 1)
            GetSheetName();

        foreach (var _sheetName in sheetsNameList)
        {
            // range = $"{_sheetName}!1:1";
            range = _sheetName;

            SpreadsheetsResource.ValuesResource.GetRequest request =
                service.Spreadsheets.Values.Get(spreadsheetId, range);

            ValueRange response = request.Execute();            

            if (response.Values != null)
            {
                int cutIndex = 0;

                for (int index = 0; index < response.Values[0].Count; index++)
                {
                    // Console.WriteLine(_sheetName + "현재 " + index + " - " + response.Values[0][index]);                        
                    if (string.IsNullOrEmpty(response.Values[0][index].ToString()))
                    {
                        cutIndex = index;
                        // Console.WriteLine(cutIndex + "에서 검출");
                        break;
                    }
                    cutIndex++;
                }

                range = $"{_sheetName}!A1:{GetExcelColumnName(cutIndex)}";

                // API 요청을 하지 않고 Sheet를 잘라서 트래픽을 최소화
                {
                    response.Range = range;

                    // Debug.Log("실행 전 -----------------------" + response.Values[0].Count);

                    for (int row = 0; row < response.Values.Count; row++)
                    {
                        for (; cutIndex < response.Values[row].Count;)
                        {
                            // Debug.Log("---------------------------------- 자른 값 - " + response.Values[row][cutIndex]);
                            response.Values[row].RemoveAt(cutIndex);
                        }
                    }

                    // Debug.Log("실행 후 -----------------------" + response.Values[0].Count);
                }

                if (savedSheets.ContainsKey(_sheetName))
                    savedSheets.Remove(_sheetName);

                savedSheets.Add(_sheetName, response.Values);

                savedValue = response.Values;
            }
            else
            {
                Debug.Log("Data Not Found... - " + _sheetName);
            }
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

}
