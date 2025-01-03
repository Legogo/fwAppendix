using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace fwp.scenes.utils
{
    using fwp.appendix;
    using fwp.utils.editor;
    using fwp.utils.editor.tabs;

    /// <summary>
    /// 
    /// FEED:
    /// base pathsub section
    /// 
    /// PROVIDE:
    /// buttons to open SceneProfil
    /// 
    /// give a list of folder to target (tab names)
    /// search within folder all scenes
    /// separate scenes with same parent folder
    /// 
    /// how to use :
    /// - inherite of this class to have your own window
    /// - implement sections names for tabs
    /// - you can override generateProfil to use some specific SceneProfil
    /// </summary>
    abstract public class EdWinBlueprintScenesSelector : WinEdTabs
    {
        /// <summary>
        /// assoc btw tab label and some sub bolbs
        /// tab label
        /// sub folder scene profiles[]
        /// </summary>
        Dictionary<string, List<SceneSubFolder>> sections = null;

        public bool hasSections => sections != null && sections.Count > 0;

        virtual protected bool useProgressBar() => true;

        /// <summary>
        /// can be replaced by different way to handle scene profil
        /// </summary>
        virtual protected SceneProfil generateProfil(string uid)
        {
            //Debug.Log("generating default profil : " + uid);
            return new SceneProfil(uid);
        }

        /// <summary>
        /// can be replaced by different way to hande subs
        /// </summary>
        virtual protected SceneSubFolder generateSub(string profilUid)
        {
            return new SceneSubFolder(profilUid);
        }

        protected override void onFocus(bool gainFocus)
        {
            base.onFocus(gainFocus);

            if (gainFocus)
            {
                if (!hasSections) refresh(true);
            }

        }

        protected override void onTabChanged(WrapperTab tab)
        {
            base.onTabChanged(tab);

            injectSubSection(tab.path); // tab change, reeval tab content
        }

        public override void refresh(bool force = false)
        {
            base.refresh(force);

            if (force)
            {
                SceneTools.refreshScenePathBuffer();
            }

            var state = tabsState; // getter edit/runtime tabs

            if (state != null && sections == null || force)
            {
                if (sections == null) sections = new Dictionary<string, List<SceneSubFolder>>();
                else sections.Clear();

                injectSubSections(state);
            }

        }

        /// <summary>
        /// inject all tabs path to sub sections
        /// </summary>
        void injectSubSections(WrapperTabs state)
        {
            var paths = state.paths;

            // each possible labels into sub folder blob
            foreach (var p in paths)
            {
                injectSubSection(p);
            }
        }

        void injectSubSection(string sectionPath)
        {
            //if (verbose) Debug.Log("SceneSelector :: refresh section : " + sectionPath);

            // remove if previous
            if (sections.ContainsKey(sectionPath))
            {
                sections.Remove(sectionPath);
            }

            List<SceneSubFolder> tabContent = solveTabFolder(sectionPath);

            if (tabContent != null)
            {
                sections.Add(sectionPath, tabContent);
            }

        }

        protected void drawSubSectionTab(string subSectionUid)
        {
            if (sections.Count <= 0)
                return;

            List<SceneSubFolder> subList = new List<SceneSubFolder>();

            if (sections.ContainsKey(subSectionUid))
            {
                subList = sections[subSectionUid];
            }

            GUILayout.BeginHorizontal();

            GUILayout.Label($"{subSectionUid} has x{subList.Count} sub-sections");

            if (GUILayout.Button("ping folder", GUILayout.Width(GuiHelpers.btnLabelWidth)))
            {
                GuiHelpers.selectFolder(subSectionUid, true);
            }

            if (GUILayout.Button("upfold all", GUILayout.Width(GuiHelpers.btnLabelWidth)))
            {
                for (int i = 0; i < subList.Count; i++)
                {
                    subList[i].toggled = false;
                }
            }

            GUILayout.EndHorizontal();

            for (int i = 0; i < subList.Count; i++)
            {
                subList[i].drawSection(filter);
            }

        }

        /// <summary>
        /// additionnal stuff under tabs zone
        /// </summary>
        protected override void drawFooter()
        {
            base.drawFooter();

            SceneSubFolder.drawAutoAddBuildSettings();
        }

        List<SceneSubFolder> solveTabFolder(string tabName)
        {
            List<SceneProfil> profils = getProfils(tabName);
            if (profils == null)
            {
                Debug.LogError("null profils while solving tabs ?");
                return null;
            }

            Dictionary<string, List<SceneProfil>> list = new Dictionary<string, List<SceneProfil>>();

            //Debug.Log("sorting x" + profils.Count + " profiles");

            // all profil will be matched based on the parent path
            foreach (SceneProfil profil in profils)
            {
                string parent = profil.parentPath;

                //Debug.Log(profil.label + " @ " + profil.parentPath);

                if (!list.ContainsKey(parent))
                {
                    //Debug.Log("added " + parent);
                    list.Add(parent, new List<SceneProfil>());
                }
                list[parent].Add(profil);
            }

            List<SceneSubFolder> output = new List<SceneSubFolder>();

            foreach (var kp in list)
            {
                SceneSubFolder sub = generateSub(kp.Key);

                sub.scenes = kp.Value;

                if (verbose) Debug.Log(sub.stringify());

                output.Add(sub);
            }

            //Debug.Log("solved x" + output.Count + " subs");

            return output;
        }

        /// <summary>
        /// g�n�re tout les profiles qui sont de la categorie
        /// </summary>
        protected List<SceneProfil> getProfils(string category)
        {
            List<SceneProfil> profils = new List<SceneProfil>();

            // works with Contains
            var cat_paths = SceneTools.getScenesPathsOfCategory(category, true);

            if (verbose)
                Debug.Log("category <b>" + category + "</b> match paths x" + cat_paths.Count);

            for (int i = 0; i < cat_paths.Count; i++)
            {
                string path = cat_paths[i];

#if UNITY_EDITOR
                if (useProgressBar())
                {
                    float progr = (i * 1f) / (cat_paths.Count * 1f);
                    if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("profil : " + category, "..." + path, progr))
                    {
                        return null;
                    }
                }
#endif

                SceneProfil.verbose = verbose;

                // generate a profil with given path
                SceneProfil sp = generateProfil(path);

                // check if the profil is already part of profils[]
                if (!sp.hasContent()) Debug.LogWarning(path + " has no content");
                else
                {
                    bool found = false;

                    // search in existing profils
                    foreach (var profil in profils)
                    {
                        if (profil.match(sp))
                            found = true;
                    }

                    // this profil is already in list
                    if (found)
                    {
                        if (verbose) Debug.Log("~ " + sp.label + " (lyrx" + sp.layers.Count + ") @ " + path);
                    }
                    else
                    {
                        profils.Add(sp);

                        if (verbose) Debug.Log("+ " + sp.label + " (lyrx" + sp.layers.Count + ") @ " + path);
                    }

                }
            }

            if (verbose)
            {
                Debug.Log("solved x" + profils.Count + " profiles");
                foreach (var p in profils)
                {
                    Debug.Log(p.stringify());
                }
            }


#if UNITY_EDITOR
            if (useProgressBar())
            {
                UnityEditor.EditorUtility.ClearProgressBar();
            }
#endif

            return profils;
        }

        public SceneProfil getOpenedProfil()
        {
            var category = sections[tabsState.getActiveTab().path];

            foreach (var profil in category)
            {
                foreach (var sp in profil.scenes)
                {
                    if (sp.isLoaded()) return sp;
                }
            }

            return null;
        }

        public void selectFolder(string path, bool unfold = false) => fwp.appendix.GuiHelpers.selectFolder(path, unfold);
    }

}