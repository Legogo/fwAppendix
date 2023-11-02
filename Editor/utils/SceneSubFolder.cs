using fwp.appendix;
using fwp.appendix.user;
using fwp.scenes;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;

/// <summary>
/// gather all scenes profiles for a specific folder
/// permet de regrouper les sceneprofil dans un m�me container
/// </summary>
public class SceneSubFolder
{
    public string projectPath; // where the folder is located in Assets/
    public string folderName; // just folder name

    public string completePath => System.IO.Path.Combine(projectPath, folderName);

    public List<SceneProfil> scenes;

    public bool toggled
    {
        set
        {
            EditorPrefs.SetBool(completePath, value);
        }
        get
        {
            return EditorPrefs.GetBool(completePath, false);
        }
    }

    public SceneSubFolder(string basePath, string folderName)
    {
        projectPath = basePath;
        this.folderName = folderName;

        toggled = true;
    }

    public bool hasContent(string filter)
    {
        if (filter.Length <= 0)
            return scenes.Count > 0;

        int cnt = 0;
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].uid.Contains(filter))
                cnt++;
        }

        return cnt > 0;
    }
    public void drawSection(string filter)
    {

        if (!hasContent(filter))
        {
            GUILayout.Label(folderName);
        }
        else
        {

            // sub folder
            toggled = EditorGUILayout.Foldout(toggled, folderName + " (x" + scenes.Count + ")", true);
            if (toggled)
            {
                foreach (var profil in scenes)
                {
                    drawSceneLine(profil);
                }
            }
        }

    }

    virtual protected void drawSceneLine(SceneProfil profil)
    {
        GUILayout.BeginHorizontal();

        // scene button
        if (GUILayout.Button(profil.editor_getButtonName())) // each profil
        {
            //if (EditorPrefs.GetBool(edLoadDebug)) section[i].loadDebug = true;
            //profil.editorLoad(false);
            onEditorSceneCall(profil, true, false);
        }

        if (GUILayout.Button(">", GUILayout.Width(GuiHelpers.btnSymbWidth)))
        {
            GuiHelpers.pingScene(profil.path);
        }


        // add/remove buttons
        bool present = SceneTools.isEditorSceneLoaded(profil.uid);
        string label = present ? "-" : "+";

        if (GUILayout.Button(label, GUILayout.Width(GuiHelpers.btnSymbWidth)))
        {
            if (!present) onEditorSceneCall(profil, true, true);
            else onEditorSceneCall(profil, false);
        }

        GUILayout.EndHorizontal();
    }

    public const string _pref_autoAdd = "scenesAutoAdd";

    /// <summary>
    /// additive only for loading
    /// </summary>
    void onEditorSceneCall(SceneProfil profil, bool mustLoad, bool additive = false)
    {

        if (mustLoad)
        {
            profil.editorLoad(additive, MgrUserSettings.getEdBool(_pref_autoAdd));
        }
        else
        {
            profil.editorUnload();
        }

    }

    /// <summary>
    /// shk
    /// </summary>
    static public void drawAutoAdd()
    {
        EdUserSettings.drawBool("+ build settings", _pref_autoAdd);
    }

}
