using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PreImporter))]
public class PreImportButton : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("PreImport"))
        {
            PreImporter.PreImportSpreadSheetId();
        }
    }
}
