using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class DataScriptGenerator
{
    [MenuItem("GoogleSheet/Generate Data Scripts")]
    public static void GenerateDataScript()
    {
        string m_Path = Application.streamingAssetsPath;

        string[] fileNameArr;
        fileNameArr = Directory.GetFiles(m_Path + "/Resource/DataTable/", "*.csv");

        foreach (var _fileName in fileNameArr)
        {
            string _csFileName = _fileName.Replace(".csv", ".cs");
            _csFileName = _csFileName.Replace("StreamingAssets/Resource/DataTable", "Scripts/DataFiles");

            using (System.IO.StreamWriter file = new System.IO.StreamWriter($@"{_csFileName}", false,
                System.Text.Encoding.GetEncoding("utf-8")))
            {
                string fileHeadName = Path.GetFileNameWithoutExtension(_csFileName);

                List<string> DataTypeRow = CSVReader.GetCSVRow(fileHeadName, 0);
                List<string> DataNameRow = CSVReader.GetCSVRow(fileHeadName, 1);
                List<string> FirstDataRow = CSVReader.GetCSVRow(fileHeadName, 2);

                file.WriteLine("using UnityEngine;");
                file.WriteLine("using System;");
                file.WriteLine("using System.Collections;");
                file.WriteLine("using System.Collections.Generic;");
                file.WriteLine();
                file.WriteLine("// This is generated data script");
                file.WriteLine();
                file.WriteLine("[System.Serializable]");
                file.WriteLine($"public class {fileHeadName} : RecordBase");
                file.WriteLine("{");

                // 변수 생성
                for (int i = 0; i < DataTypeRow.Count; i++)
                {
                    if (DataTypeRow[i].EndsWith("]") ||
                        DataTypeRow[i].Contains("List<") ||
                        DataTypeRow[i].Contains("Dictionary<"))
                    {
                        file.Write($"\tpublic string {DataNameRow[i]}");
                    }

                    else
                        file.Write($"\tpublic {DataTypeRow[i]} {DataNameRow[i]}");

                    // 배열일 때 초기화 x
                    if (DataTypeRow[i].EndsWith("]"))
                    {
                        // file.WriteLine(";");
                        file.WriteLine($" = \"{FirstDataRow[i]}\";");
                    }

                    // 제너릭 타입일 때 초기화
                    else if (DataTypeRow[i].Contains("List<") ||
                        DataTypeRow[i].Contains("Dictionary<"))
                    {
                        // file.WriteLine($" = new {DataTypeRow[i]}();");
                        file.WriteLine($" = \"{FirstDataRow[i]}\";");
                    }

                    else if (DataTypeRow[i] == "string")
                        file.WriteLine($" = \"{FirstDataRow[i]}\";");

                    else if (DataTypeRow[i] == "char")
                        file.WriteLine($" = \'{FirstDataRow[i]}\';");

                    else
                        file.WriteLine($" = {FirstDataRow[i]};");
                }

                // 생성자 생성
                file.WriteLine();
                file.WriteLine("\t// Default Constructor");
                file.Write($"\tpublic {fileHeadName}()");
                file.WriteLine("{ }");
                file.WriteLine();

                // 생성자 변수 선언
                file.Write($"\tpublic {fileHeadName}(");
                for (int i = 0; i < DataTypeRow.Count; i++)
                {
                    if (i != 0)
                        file.Write(", ");

                    if (DataTypeRow[i].EndsWith("]") ||
                        DataTypeRow[i].Contains("List<") ||
                        DataTypeRow[i].Contains("Dictionary<"))
                    {
                        file.Write($"string _{DataNameRow[i]}");
                    }

                    else
                        file.Write($"{DataTypeRow[i]} _{DataNameRow[i]}");
                }
                file.WriteLine(")");
                file.WriteLine("\t{");
                file.WriteLine("\t\tparameterList = new List<object>();");
                for (int i = 0; i < DataTypeRow.Count; i++)
                {
                    file.WriteLine($"\t\tparameterList.Add(this.{DataNameRow[i]} = _{DataNameRow[i]});");

                }
                file.WriteLine("\t}");

                // 오버라이딩
                file.WriteLine();
                file.WriteLine("\tpublic override void DataSet(List<object> list)");
                file.WriteLine("\t{");
                file.WriteLine("\t\tfor (int i = 0; i < list.Count; i++)");
                file.WriteLine("\t\t\tparameterList[i] = list[i];");
                file.WriteLine("\t}");

                
                // RecordInitialize()
                file.WriteLine("\tpublic override void RecordInitialize() { }");
                file.WriteLine("\tpublic override void RecordInitialize(params string[] mylist)");
                file.WriteLine("\t{");
                file.WriteLine("\t\tparameterList = new List<object>();");

                for (int i = 0; i < DataTypeRow.Count; i++)
                {
                    if (DataTypeRow[i].Contains("List<") ||
                        DataTypeRow[i].Contains("Dictionary<"))
                        continue;

                    // file.WriteLine($"\t\tthis.{DataNameRow[i]} = ({DataTypeRow[i]})mylist[{i}];");

                    switch (DataTypeRow[i])
                    {
                        case "bool":
                            file.WriteLine($"\t\tparameterList.Add(this.{DataNameRow[i]} = Convert.ToBoolean(mylist[{i}]));");
                            break;

                        case "int":
                            file.WriteLine($"\t\tparameterList.Add(this.{DataNameRow[i]} = Convert.ToInt32(mylist[{i}]));");
                            break;

                        case "float":
                            file.WriteLine($"\t\tparameterList.Add(this.{DataNameRow[i]} = Convert.ToDouble(mylist[{i}]));");
                            break;

                        case "double":
                            file.WriteLine($"\t\tparameterList.Add(this.{DataNameRow[i]} = Convert.ToDouble(mylist[{i}]));");
                            break;

                        case "char":
                            file.WriteLine($"\t\tparameterList.Add(this.{DataNameRow[i]} = Convert.ToChar(mylist[{i}]));");
                            break;

                        case "string":
                            file.WriteLine($"\t\tparameterList.Add(this.{DataNameRow[i]} = Convert.ToString(mylist[{i}]));");
                            break;

                        // 배열, enum, 제너릭 타입 등
                        default:
                            file.WriteLine($"\t\tparameterList.Add(this.{DataNameRow[i]} = Convert.ToString(mylist[{i}]));");
                            break;
                    }
                }

                file.WriteLine("\t}");

                // 배열 Getter
                for (int i = 0; i < DataTypeRow.Count; i++)
                {
                    if (DataTypeRow[i].EndsWith("]"))
                    {
                        file.WriteLine($"\tpublic {DataTypeRow[i]} Get{DataNameRow[i]}Arr()");
                        file.WriteLine("\t{");

                        if (DataTypeRow[i] != "string[]")
                            file.WriteLine($"\t\treturn Array.ConvertAll({DataNameRow[i]}.Split(' '), " +
                                $"s => {DataTypeRow[i].Replace("[]", "")}.Parse(s));");
                        else
                            file.WriteLine($"\t\treturn {DataNameRow[i]}.Split(' ');");

                        // file.WriteLine($"\t\treturn {DataNameRow[i]}.Split(' ');");
                        file.WriteLine("\t}");
                    }
                }


                // 클래스 끝 괄호
                        file.WriteLine("}");
            }

            Debug.Log("Data File Generated - " + _csFileName);
        }
    }

    [MenuItem("GoogleSheet/Generate Manager Scripts")]
    public static void GenerateManagerScript()
    {
        string m_Path = Application.streamingAssetsPath;

        string[] fileNameArr;
        fileNameArr = Directory.GetFiles(m_Path + "/Resource/DataTable/", "*.csv");

        foreach (var _fileName in fileNameArr)
        {
            string _csFileName = _fileName.Replace(".csv", "Manager.cs");
            _csFileName = _csFileName.Replace("StreamingAssets/Resource/DataTable", "Scripts/DataManager");

            using (System.IO.StreamWriter file = new System.IO.StreamWriter($@"{_csFileName}", false,
                System.Text.Encoding.GetEncoding("utf-8")))
            {
                string fileHeadName = Path.GetFileNameWithoutExtension(_csFileName).Replace("Manager", "");

                //List<string> DataTypeRow = CSVReader.GetCSVRow(fileHeadName, 0);
                List<string> DataNameRow = CSVReader.GetCSVRow(fileHeadName, 1);
                //List<string> FirstDataRow = CSVReader.GetCSVRow(fileHeadName, 2);

                file.WriteLine("using UnityEngine;");
                file.WriteLine("using System;");
                file.WriteLine("using System.Collections;");
                file.WriteLine("using System.Collections.Generic;");
                file.WriteLine();
                file.WriteLine("// This is generated data script");
                file.WriteLine();
                file.WriteLine("[System.Serializable]");
                file.WriteLine($"public class {fileHeadName}Manager : DataManagerBase");
                file.WriteLine("{");
                file.WriteLine($"\tprivate List<{fileHeadName}> dataTable = new List<{fileHeadName}>();");
                file.WriteLine();

                // saved Instance
                file.WriteLine($"\tprivate static {fileHeadName}Manager savedInstance;");

                // Instance
                file.WriteLine($"\tpublic static {fileHeadName}Manager Instance");
                file.WriteLine("\t{");
                file.WriteLine("\t\tget");
                file.WriteLine("\t\t{");
                file.WriteLine("\t\t\tif (instance == null)");
                file.WriteLine("\t\t\t{");
                file.WriteLine($"\t\t\t\tinstance = new {fileHeadName}Manager();");
                file.WriteLine($"\t\t\t\tsavedInstance = ({fileHeadName}Manager)instance;");
                file.WriteLine($"\t\t\t\treturn savedInstance;");
                file.WriteLine("\t\t\t}");                

                file.WriteLine($"\t\t\telse if (instance.GetType() != typeof({fileHeadName}Manager))");
                file.WriteLine("\t\t\t{");
                file.WriteLine($"\t\t\t\t return savedInstance = new {fileHeadName}Manager();");
                file.WriteLine("\t\t\t}");
                file.WriteLine($"\t\treturn ({fileHeadName}Manager)instance;");
                file.WriteLine("\t\t}");
                file.WriteLine("\t}");

                // MakeTable()
                file.WriteLine();
                file.WriteLine("\tprivate void MakeTable()");
                file.WriteLine("\t{");
                file.WriteLine($"\t\tfor (int i = 2; i < CSVReader.GetCSVRowCount(\"{fileHeadName}\"); i++)");
                file.WriteLine("\t\t{");
                file.WriteLine($"\t\t\t{fileHeadName} t = new {fileHeadName}();");
                file.WriteLine($"\t\t\tt.RecordInitialize(CSVReader.GetCSVRow(\"{fileHeadName}\", i).ToArray());");
                file.WriteLine($"\t\t\tdataTable.Add(t);");
                file.WriteLine("\t\t}");
                file.WriteLine("\t}");

                // GetTable()
                file.WriteLine();
                file.WriteLine($"\tpublic List<{fileHeadName}> GetTable()");
                file.WriteLine("\t{");
                file.WriteLine("\t\tif (dataTable.Count < 1)");
                file.WriteLine("\t\t\tMakeTable();");
                file.WriteLine();
                file.WriteLine("\t\treturn dataTable;");
                file.WriteLine("\t}");

                // GetRecordById()
                file.WriteLine();
                file.WriteLine($"\tpublic {fileHeadName} GetRecordById(string _id)");
                file.WriteLine("\t{");
                file.WriteLine("\t\tif (dataTable.Count < 1)");
                file.WriteLine("\t\t\tMakeTable();");
                file.WriteLine();
                file.WriteLine("\t\tforeach (var r in dataTable)");
                file.WriteLine("\t\t{");
                file.WriteLine($"\t\t\tif (_id == r.{DataNameRow[0]})");
                file.WriteLine("\t\t\t{");
                file.WriteLine("\t\t\t\treturn r;");
                file.WriteLine("\t\t\t}");
                file.WriteLine("\t\t}");
                file.WriteLine("\t\treturn null;");
                file.WriteLine("\t}");


                file.WriteLine("}");
            }
        }
    }
}