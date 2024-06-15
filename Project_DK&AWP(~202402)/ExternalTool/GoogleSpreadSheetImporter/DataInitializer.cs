using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataInitializer : MonoBehaviour
{
    private void Start()
    {
        DataInitialize();
    }

    public void DataInitialize()
    {
        string[] fileNameArr;
        fileNameArr = Directory.GetFiles(Application.streamingAssetsPath + "/Resource/DataTable/", "*.csv");

        for (int i = 0; i < fileNameArr.Length; i++)
        {
            fileNameArr[i] = fileNameArr[i].Replace(".csv", "");

            string tmp = "TestTable"; // fileNameArr[i];

            // fileNameArr[0] my = new fileNameArr[0]();

            // TestTable myTable = new TestTable();

            ////////////
            ///
            Type type = Type.GetType(tmp);
            dynamic node = Activator.CreateInstance(type);
          
            Debug.Log(node.ID);            
        }
    }
}
