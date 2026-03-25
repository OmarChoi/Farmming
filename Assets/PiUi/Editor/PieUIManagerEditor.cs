using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(PiUIManager))]
public class PiUIManagerEditor : Editor
{

    PiUIManager myTarget;

    private void OnEnable()
    {
        myTarget = target as PiUIManager;
    }

    public override void OnInspectorGUI()
    {
        if (myTarget == null)
        {
            myTarget = target as PiUIManager;
            if (myTarget == null)
            {
                EditorGUILayout.HelpBox("PiUIManager target could not be resolved. Re-select the object after scripts finish compiling.", MessageType.Warning);
                DrawDefaultInspector( );
                return;
            }
        }

        DrawDefaultInspector( );
        PiUI[] piMenus = myTarget.GetComponentsInChildren<PiUI>( );
        string[] piMenuNames = new string[myTarget.nameMenu.Length];

        if (piMenus.Length != myTarget.nameMenu.Length)
        {
            for (int i = 0; i < myTarget.nameMenu.Length; i++)
            {
                piMenuNames[i] = myTarget.nameMenu[i].name;
            }
            myTarget.nameMenu = new PiUIManager.NameMenuPair[piMenus.Length];
            for (int j = 0; j < piMenus.Length; j++)
            {
                myTarget.nameMenu[j] = new PiUIManager.NameMenuPair( );
                myTarget.nameMenu[j].name = piMenus[j].name;
                myTarget.nameMenu[j].menu = piMenus[j];
            }
        }
    }
}
