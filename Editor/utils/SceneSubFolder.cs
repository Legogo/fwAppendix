using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using fwp.appendix;
using fwp.appendix.user;
using fwp.scenes;

/// <summary>
/// gather all scenes profiles for a specific folder
/// scenes[] will be override externaly
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

    public SceneSubFolder(string folderPath)
    {
        projectPath = folderPath;

        if (projectPath.Length <= 0)
        {
            Debug.LogWarning("no base path given ?");
        }

        folderName = folderPath.Substring(folderPath.LastIndexOf("/") + 1);
    }

    public bool hasContentMatchingFilter(string filter)
    {
        if (string.IsNullOrEmpty(filter)) return scenes.Count > 0;

        int cnt = 0;
        for (int i = 0; i < scenes.Count; i++)
        {
            //Debug.Log(scenes[i].label + " vs " + filter);
            if (scenes[i].matchFilter(filter))
                cnt++;
        }

        return cnt > 0;
    }

    public void drawSection(string filter)
    {
        // has any profil matching filter
        if (!hasContentMatchingFilter(filter)) return;

        // sub folder
        toggled = EditorGUILayout.Foldout(toggled, folderName + " (x" + scenes.Count + ")", true);
        if (toggled)
        {
            if (filter.Length <= 0)
            {
                if (GUILayout.Button("+all", GUILayout.Width(GuiHelpers.btnSymbWidth)))
                {
                    sectionLoadAll();
                }
            }

            foreach (var profil in scenes)
            {
                if(profil.matchFilter(filter))
                {
                    drawLineContent(profil);
                }
            }
        }
        
    }

    void sectionLoadAll()
    {
        Debug.Log("load all");
        foreach(var p in scenes)
        {
            p.editorLoad(
                replaceContext: false, 
                forceAddBuildSettings: true);
        }
    }

    virtual protected void logSceneDetails(SceneProfil profil)
    {
        Debug.Log("profil:" + profil.label);

        Debug.Log("  -> layers x" + profil.layers.Count);
        foreach (var elmt in profil.layers)
            Debug.Log(elmt);

        Debug.Log("  -> deps x" + profil.deps.Count);
        foreach (var dep in profil.deps)
            Debug.Log(dep);

        // and ping scene
        GuiHelpers.pingScene(profil.pingScenePath);
    }

    /// <summary>
    /// whatever is drawn in a profil line
    /// true : pressed button & load is called
    /// </summary>
    virtual protected bool drawLineContent(SceneProfil profil)
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("?", GUILayout.Width(GuiHelpers.btnSymbWidthSmall)))
        {
            logSceneDetails(profil);
        }

        bool load = false;

        // scene button
        if (GUILayout.Button(profil.editor_getButtonName())) // each profil
        {
            //if (EditorPrefs.GetBool(edLoadDebug)) section[i].loadDebug = true;
            //profil.editorLoad(false);
            onEditorSceneCall(profil, true);
            load = true;
        }

        // add/remove buttons
        bool present = SceneTools.isEditorSceneLoaded(profil.label);
        //bool present = profil.isLoaded();
        string label = present ? "-" : "+";

        if (GUILayout.Button(label, GUILayout.Width(GuiHelpers.btnSymbWidth)))
        {
            if (!present)
            {
                onEditorSceneCall(profil, false);
                reactSceneCall(profil, true);
                load = true;
            }
            else
            {
                onEditorSceneRemoval(profil);
                reactSceneCall(profil, false);
            }
        }

        GUILayout.EndHorizontal();

        return load;
    }

    /// <summary>
    /// when user calls for a scene
    /// load or unload
    /// </summary>
    virtual protected void reactSceneCall(SceneProfil profil, bool load)
    { }

    virtual public string stringify()
    {
        //return "@path:" + projectPath + " @folder:" + folderName + ", total scenes x" + scenes.Count;
        return "@folder:" + folderName + ", total scenes x" + scenes.Count;
    }

    public const string _pref_autoAddBuildSettings = "scenesAutoAddBuildSettings";

    /// <summary>
    /// additive only for loading
    /// </summary>
    void onEditorSceneCall(SceneProfil profil, bool replaceContext)
    {
        profil.setDirty();
        profil.editorLoad(replaceContext, MgrUserSettings.getEdBool(_pref_autoAddBuildSettings));
    }

    void onEditorSceneRemoval(SceneProfil profil)
    {
        profil.setDirty();
        profil.editorUnload();
    }

    /// <summary>
    /// helper to draw a line to toggle a bool linked to edpprefs
    /// </summary>
    static public void drawToggle(string label, string ppref)
    {
        EdUserSettings.drawBool("+" + label, ppref);
    }

    /// <summary>
    /// shk
    /// </summary>
    static public void drawAutoAddBuildSettings()
    {
        drawToggle("build settings", _pref_autoAddBuildSettings);
    }



}
