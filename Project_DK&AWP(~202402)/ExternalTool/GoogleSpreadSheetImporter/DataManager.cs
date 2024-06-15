using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    private DataManager instance;
    public DataManager Instance
    {
        get
        {
            if (instance != null)
                instance = new DataManager();
            return instance;
        }
    }

    // public Dictionary<string, List<RecordBase>> DataTableDictionary = new Dictionary<string, List<RecordBase>>();

    [SerializeField]
    public SerializableDictionary<string, DataManagerBase> RegisteredDataManager = new SerializableDictionary<string, DataManagerBase>();

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        string m_Path = Application.streamingAssetsPath;

        //RegisterTableManager<Sheet1Manager>(Sheet1Manager.Instance);
        //RegisterTableManager<TestTableManager>(TestTableManager.Instance);
        //RegisterTableManager<시트1Manager>(시트1Manager.Instance);   

        //Debug.Log(GetDataTableRecord<TestTableManager, TestTable>("101").Name);
        //Debug.Log(GetDataTableRecord<TestTableManager, TestTable>("101").GetdAArr()[1]);
        //Debug.Log(GetDataTable<TestTableManager, TestTable>()[0].Name);
        //Debug.Log(GetDataTable<TestTableManager, TestTable>()[4].Name);
    }

    private void RegisterTableManager<T>(T target) where T : DataManagerBase
    {       
        RegisteredDataManager.Add(typeof(T).ToString(), target);
    }

    public List<RecordType> GetDataTable<TableManager, RecordType>() 
        where TableManager : DataManagerBase 
        where RecordType : RecordBase
    {
        string _tableManagerName = typeof(TableManager).ToString();

        // table manganger 키가 존재하면 매니저를 dynamic으로 생성 후 RecordBase의 List를 반환
        if (RegisteredDataManager.ContainsKey(_tableManagerName))
        {
            Type managerType = Type.GetType(_tableManagerName);
            dynamic dynamicTableManager = Activator.CreateInstance(managerType);

            List<RecordType> dynamicList = dynamicTableManager.GetTable();

            return dynamicList;
        }

        Debug.LogError("Key Not Found");

        return null;        
    }

    public RecordType GetDataTableRecord<TableManager, RecordType>(string _id)
        where TableManager : DataManagerBase
        where RecordType : RecordBase
    {
        string _tableManagerName = typeof(TableManager).ToString();

        // table manganger 키가 존재하면 매니저를 dynamic으로 생성 후 GetRecordById() 호출
        if (RegisteredDataManager.ContainsKey(_tableManagerName))
        {
            Type managerType = Type.GetType(_tableManagerName);
            dynamic dynamicTableManager = Activator.CreateInstance(managerType);

            RecordType dynamicRecord = dynamicTableManager.GetRecordById(_id);

            return dynamicRecord;
        }
        Debug.LogError("Key Not Found");

        return null;
    }

    public string[] GetString2Arr(string _target)
    {
        return _target.Split(' ');
    }
}
