using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class CSVReader
{
    private static CSVReader instance;
    public static CSVReader Instance
    {
        get
        {
            if (instance == null)
                instance = new CSVReader();
            return instance;
        }
    }

    public static string m_Path = Application.streamingAssetsPath;
    // private static string m_FilePath = "";

    // public static string[,] parsedData;    

    public static Dictionary<string, List<List<string>>> parsed_string = new Dictionary<string, List<List<string>>>();

    [MenuItem("GoogleSheet/Read All CSV")]
    public static void ReadAllCSV()
    {
        string[] fileNameArr;        
        fileNameArr = Directory.GetFiles(m_Path + @"\Resource\DataTable\", "*.csv");

        for (int i = 0; i < fileNameArr.Length; i++)
            ReadEncryptedCSV_V2(fileNameArr[i]);

        //foreach(var _fileName in fileNameArr)
        //{
        //    ReadEncryptedCSV_V2(_fileName);
        //}
    }

    [MenuItem("GoogleSheet/Refresh CSV")]
    public static void RefreshCSV()
    {
        parsed_string = new Dictionary<string, List<List<string>>>();
    }

    /// <summary>
    /// _fileName�� ��ü ��� �� Ȯ���ڸ� �־��ּ���.
    /// </summary>
    /// <param name="_fileName"></param>
    /// <returns></returns>
    public static List<RecordBase> GetCSVTuple(string _fileName)
    {
        // ���� �ε尡 �ȵ� ���¶��
        if (parsed_string.Keys.Count < 1)
            ReadAllCSV();

        _fileName = Path.GetFileName(_fileName).Replace(".csv", "");

        List<RecordBase> tmpList = new List<RecordBase>();

        // Ű�� ã�� ���ߴٸ�
        if (!parsed_string.ContainsKey(_fileName))
            return null;

        // Ű�� ã���� ��
        // 0���� 1���� �����Ͱ� �ƴϹǷ� �н�
        for (int i = 2; i < parsed_string[_fileName].Count; i++)
        {
            Type type = Type.GetType(_fileName);
            dynamic recordBase = Activator.CreateInstance(type);

            recordBase.RecordInitialize(parsed_string[_fileName][i].ToArray());
            tmpList.Add(recordBase);
        }

        return tmpList;
    }

    public static List<string> GetCSVRow(string _fileName, int rowNumber = 0)
    {
        // ���� �ε尡 �ȵ� ���¶��

        if (parsed_string.Keys.Count < 1)
            ReadAllCSV();

        return parsed_string[_fileName][rowNumber];
    }

    public static int GetCSVRowCount(string _fileName)
    {
        // ���� �ε尡 �ȵ� ���¶��
        if (parsed_string.Keys.Count < 1)
            ReadAllCSV();

        return parsed_string[_fileName].Count;
    }

    static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
    static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
    static char[] TRIM_CHARS = { '\"' };

    public static List<Dictionary<string, object>> Read(string file)
    {
        var list = new List<Dictionary<string, object>>();
        TextAsset data = new TextAsset(System.IO.File.ReadAllText(file));

        var lines = Regex.Split(data.text, LINE_SPLIT_RE);

        if (lines.Length <= 1) return list;

        var header = Regex.Split(lines[0], SPLIT_RE);
        for (var i = 1; i < lines.Length; i++)
        {

            var values = Regex.Split(lines[i], SPLIT_RE);
            if (values.Length == 0 || values[0] == "") continue;

            var entry = new Dictionary<string, object>();
            for (var j = 0; j < header.Length && j < values.Length; j++)
            {
                string value = values[j];
                value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");
                object finalvalue = value;
                int n;
                float f;
                if (int.TryParse(value, out n))
                {
                    finalvalue = n;
                }
                else if (float.TryParse(value, out f))
                {
                    finalvalue = f;
                }
                entry[header[j]] = finalvalue;
            }
            list.Add(entry);
        }
        return list;
    }

    public static void ReadCSV(string filePath)
    {
        string value = "";
        StreamReader reader = new StreamReader(filePath, Encoding.UTF8);
        value = reader.ReadToEnd();
        reader.Close();

        string[] arr = value.Split(new[] { "\r\n" }, StringSplitOptions.None);

        // ���� �̸��� Ű�� ����ϱ� ���� �̸� ����
        string tmpFileName = Path.GetFileName(filePath).Replace(".csv", "");

        List<List<string>> tmpDouble = new List<List<string>>();        

        for (int i = 0; i < arr.Length - 1; i++)
        {                             
            tmpDouble.Add(arr[i].Split('\t').ToList());
        }

        parsed_string.Add(tmpFileName, tmpDouble);
    }

    public static void ReadEncryptedCSV_V1(string filePath)
    {
        string value = "";
        StreamReader reader = new StreamReader(filePath, Encoding.UTF8);
        value = reader.ReadToEnd();
        reader.Close();

        string[] arr = value.Split(new[] { "\r\n" }, StringSplitOptions.None);

        // List<string[]> mytmpList = new List<string[]>();
        List<List<string>> beforeSplitList = new List<List<string>>();

        for (int i = 0; i < arr.Length; i++)
        {
            beforeSplitList.Add(arr[i].Split(new[] { "\t" }, StringSplitOptions.None).ToList());
        }

        List<List<string>> DecryptedList = new List<List<string>>();

        for (int i = 0; i < beforeSplitList.Count; i++)
        {
            DecryptedList.Add(new List<string>());
            for (int j = 0; j < beforeSplitList[i].Count; j++)
            {
                DecryptedList[i].Add(AESCrypto.Instance.AESDecrypt(beforeSplitList[i][j]));
                DecryptedList[i][j] = DecryptedList[i][j].Replace("\0", "");

                if (string.IsNullOrEmpty(DecryptedList[i][j]))
                {
                    DecryptedList[i].RemoveAt(j);
                }
            }

            if (DecryptedList[i].Count < 1)
            {
                DecryptedList.RemoveAt(i);
            }
        }

        // ���� �̸��� Ű�� ����ϱ� ���� �̸� ����
        string tmpFileName = Path.GetFileName(filePath).Replace(".csv", "");

        parsed_string.Add(tmpFileName, DecryptedList);
    }

    public static void ReadEncryptedCSV_V2(string filePath)
    {
        string value = "";
        StreamReader reader = new StreamReader(filePath, Encoding.UTF8);
        value = reader.ReadToEnd();
        reader.Close();

        value = AESCrypto.Instance.AESDecrypt(value);
        Debug.Log(value);

        string[] arr = value.Split(new[] { "\r\n" }, StringSplitOptions.None);

        List<List<string>> beforeSplitList = new List<List<string>>();

        for (int i = 0; i < arr.Length; i++)
        {
            beforeSplitList.Add(arr[i].Split(new[] { "\t" }, StringSplitOptions.None).ToList());
        }

        List<List<string>> DecryptedList = new List<List<string>>();

        // Split�� �ϸ� �������� EOF�� ���ԵǱ� ������ -1 ��ŭ �ݺ�
        for (int i = 0; i < beforeSplitList.Count - 1; i++)
        {
            DecryptedList.Add(new List<string>());
            for (int j = 0; j < beforeSplitList[i].Count; j++)
            {
                DecryptedList[i].Add(beforeSplitList[i][j]);
            }

            if (DecryptedList[i].Count < 1)
            {
                DecryptedList.RemoveAt(i);
            }
        }

        // ���� �̸��� Ű�� ����ϱ� ���� �̸� ����
        string tmpFileName = Path.GetFileName(filePath).Replace(".csv", "");

        parsed_string.Add(tmpFileName, DecryptedList);
    }
}
