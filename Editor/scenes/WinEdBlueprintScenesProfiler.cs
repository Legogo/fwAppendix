using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;

namespace fwp.scenes
{
	/// <summary>
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
	abstract public class WinEdBlueprintScenesProfiler : EditorWindow
	{
		public bool verbose = false;

		//List<string> paths = new List<string>();

		int tabActive = 0;
		string[] tabsLabels;
		GUIContent[] tabs;
		Vector2 tabScroll;

		// le contenu a g�n�rer des tabs
		Dictionary<string, List<SceneSubFolder>> sections = null;

		string _filter = string.Empty;

		public class SceneSubFolder
        {
			public string folderName;
			public List<SceneProfil> scenes;
			public bool toggled;
			public bool hasContent(string filter)
			{
				if(filter.Length <= 0)
					return scenes.Count > 0;

				int cnt = 0;
                for (int i = 0; i < scenes.Count; i++)
                {
					if (scenes[i].uid.Contains(filter))
						cnt++;
                }

				return cnt > 0;
			}
		}

        private void OnEnable()
        {
            //Debug.Log("enable !");
            refreshLists();
        }

        private void OnValidate()
        {
			//refreshLists(true);
        }

		/// <summary>
		/// return all tabs names
		/// also will be base for paths searching
		/// </summary>
        abstract protected string[] generateSections();

		protected void refreshLists(bool force = false)
		{
			if (force)
				Debug.Log(GetType() + " force refreshing content");

			if(tabsLabels == null || force)
            {
				tabActive = 0;
				tabsLabels = generateSections();

				if (force)
					Debug.Log("tabs x" + tabsLabels.Length);

				tabs = new GUIContent[tabsLabels.Length];

				for (int i = 0; i < tabs.Length; i++)
                {
					tabs[i] = new GUIContent(tabsLabels[i]);
                }
			}

			if (sections == null || force)
			{
				sections = new Dictionary<string, List<SceneSubFolder>>();

				for (int i = 0; i < tabsLabels.Length; i++)
				{
					var tabContent = solveTabFolder(tabsLabels[i]);
					sections.Add(tabsLabels[i], tabContent);
				}

				if(force)
                {
					Debug.Log("sections x" + sections.Count);
				}
					
			}

		}

		private void Update()
		{
			if (Application.isPlaying)
					return;

			if (sections == null)
			{
				refreshLists();
			}
		}

		/// <summary>
		/// use draw() to add content
		/// </summary>
		void OnGUI()
		{
			if (GUILayout.Button("Scenes selector", getWinTitle()))
			{
				//Debug.Log("force refresh");
				refreshLists(true);
			}

			if (Application.isPlaying)
            {
				GUILayout.Label("not during runtime");
				return;
            }

			if (sections == null) return;

			//GUILayout.Label($"selector found a total of x{paths.Count} scenes in project");

			GUILayout.Space(10f);

			tabActive = drawTabsHeader(tabActive, tabs);

			drawFilterField();
			
			tabScroll = GUILayout.BeginScrollView(tabScroll);

            //var openedProfil = getOpenedProfil();

			GUILayout.EndScrollView();

			draw();
		}

		void drawFilterField()
        {

			GUILayout.BeginHorizontal();

			GUILayout.Label("filter", GUILayout.Width(50f));
			_filter = GUILayout.TextArea(_filter);

			if (GUILayout.Button("clear", GUILayout.Width(50f)))
				_filter = string.Empty;

			GUILayout.EndHorizontal();

			GUILayout.Space(10f);

			if(_filter.Length <= 0)
            {
				drawTogglableSections(tabActive);
            }
            else
            {
				solveSectionFiltered(tabActive);

			}

		}

		void drawTogglableSections(int tabIndex)
        {

			string nm = tabsLabels[tabIndex];
			var subList = sections[nm];

			GUILayout.BeginHorizontal();

			GUILayout.Label($"{nm} has x{subList.Count} sub-sections");

			if (GUILayout.Button("ping folder"))
			{
				pingFolder(nm);
			}

			GUILayout.EndHorizontal();

			for (int i = 0; i < subList.Count; i++)
			{
				SceneSubFolder section = subList[i];
				if (!section.hasContent(_filter))
				{
					GUILayout.Label(section.folderName);
				}
				else
				{

					// sub folder
					section.toggled = EditorGUILayout.Foldout(section.toggled, section.folderName + " (x" + section.scenes.Count + ")", true);

					if (section.toggled)
					{
						foreach (var profil in section.scenes)
						{
							drawSceneLine(profil);
						}
					}

				}

			}
		}

		void solveSectionFiltered(int tabIndex)
		{

			string nm = tabsLabels[tabIndex];
			var subList = sections[nm];

			List<SceneProfil> profils = new List<SceneProfil>();
			for (int i = 0; i < subList.Count; i++)
            {
                for (int j = 0; j < subList[i].scenes.Count; j++)
                {
					if (subList[i].scenes[j].uid.Contains(_filter))
						profils.Add(subList[i].scenes[j]);
                }
            }

            for (int i = 0; i < profils.Count; i++)
            {
				drawSceneLine(profils[i]);
            }
		}

		void drawSceneLine(SceneProfil profil)
        {

			if(_filter.Length > 0)
            {
				if (!profil.uid.Contains(_filter))
					return;
			}
			
			GUILayout.BeginHorizontal();

			// scene button
			if (GUILayout.Button(profil.editor_getButtonName())) // each profil
			{
				//if (EditorPrefs.GetBool(edLoadDebug)) section[i].loadDebug = true;
				//profil.editorLoad(false);
				onEditorSceneCall(profil, true, false);
			}

			// add/remove buttons
			bool present = SceneTools.isEditorSceneLoaded(profil.uid);
			string label = present ? "-" : "+";

			if (GUILayout.Button(label, GUILayout.Width(40f)))
			{
				if (!present) onEditorSceneCall(profil, true, true);
				else onEditorSceneCall(profil, false);
			}

			GUILayout.EndHorizontal();

		}

		/// <summary>
		/// additive only for loading
		/// </summary>
		virtual protected void onEditorSceneCall(SceneProfil profil, bool mustLoad, bool additive = false)
        {

			if(mustLoad)
            {
				profil.editorLoad(additive);
			}
            else
            {
				profil.editorUnload();
            }

		}

		/// <summary>
		/// additionnal stuff within scrollview
		/// </summary>
		virtual protected void draw()
        {

        }

		void drawButtonSceneSteam(string label, string path)
        {

			GUILayout.BeginHorizontal(GUILayout.Width(200f));

			// scene button
			if (GUILayout.Button(label)) // each profil
			{
				SceneLoaderEditor.loadScene(path);
			}

			// add/remove buttons
			bool present = SceneTools.isEditorSceneLoaded(path);
			label = present ? "-" : "+";

			if (GUILayout.Button(label, GUILayout.Width(40f)))
			{
				if (!present) SceneLoaderEditor.loadScene(path);
				else
				{
					SceneLoaderEditor.unloadScene(path);
				}
			}

			GUILayout.EndHorizontal();

		}

		List<SceneSubFolder> solveTabFolder(string tabName)
        {

			List<SceneProfil> profils = getProfils(tabName);

			Dictionary<string, List<SceneProfil>> list = new Dictionary<string, List<SceneProfil>>();

			//Debug.Log("sorting x" + profils.Count + " profiles");

			foreach(SceneProfil profil in profils)
			{
				string parent = profil.parentFolder;

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
				SceneSubFolder sub = new SceneSubFolder();
				
				sub.toggled = true;

				sub.folderName = kp.Key;
				sub.scenes = kp.Value;
				output.Add(sub);
            }

			//Debug.Log("solved x" + output.Count + " subs");

			return output;
		}

		protected List<SceneProfil> getProfils(string cat)
		{
			List<SceneProfil> profils = new List<SceneProfil>();

			// works with Contains
			var cat_paths = SceneTools.getScenesPathsOfCategory(cat);

			if(verbose)
				Debug.Log("category:" + cat + " paths x" + cat_paths.Count);

            foreach (string path in cat_paths)
            {
				SceneProfil sp = generateProfil(path);
				if(sp.isValid())
                {
					bool found = false;

					foreach(var profil in profils)
                    {
						if (profil.match(sp))
							found = true;
                    }

					if(!found)
                    {
						profils.Add(sp);

						if (verbose)
							Debug.Log(sp.uid);
                    }
						
				}
			}

			if (verbose)
				Debug.Log("solved x" + profils.Count + " profiles");

			return profils;
		}

		public SceneProfil getOpenedProfil()
        {
			var category = sections[tabsLabels[tabActive]];

			foreach(var profil in category)
			{
				foreach(var sp in profil.scenes)
                {
					if (sp.isLoaded()) return sp;
				}
            }

			return null;
        }

		abstract protected SceneProfil generateProfil(string uid);
        
		static public GUIContent[] generateTabsDatas(string[] labels)
		{
			GUIContent[] modeLabels = new GUIContent[labels.Length];

			//GUILayout.Label("no mode labels (x" + nms.Length + " candidates)");

			for (int i = 0; i < labels.Length; i++)
			{
				modeLabels[i] = new GUIContent(labels[i], "tooltip");
			}

			return modeLabels;
		}

		static public int drawTabsHeader(int tabSelected, GUIContent[] tabs)
		{
			//GUIStyle gs = new GUIStyle(GUI.skin.button)
			//int newTab = GUILayout.Toolbar((int)tabSelected, modeLabels, "LargeButton", GUILayout.Width(toolbarWidth), GUILayout.ExpandWidth(true));
			int newTab = GUILayout.Toolbar((int)tabSelected, tabs, "LargeButton");
			//if (newTab != (int)tabSelected) Debug.Log("changed tab ? " + tabSelected);

			return newTab;
		}



		static private GUIStyle gWinTitle;
		static public GUIStyle getWinTitle()
		{
			if (gWinTitle == null)
			{
				gWinTitle = new GUIStyle();

				gWinTitle.richText = true;
				gWinTitle.alignment = TextAnchor.MiddleCenter;
				gWinTitle.normal.textColor = Color.white;
				gWinTitle.fontSize = 20;
				gWinTitle.fontStyle = FontStyle.Bold;
				gWinTitle.margin = new RectOffset(10, 10, 10, 10);
				//gWinTitle.padding = new RectOffset(30, 30, 30, 30);

			}

			return gWinTitle;
		}

		/// <summary>
		/// use : EditorGUIUtility.PingObject
		/// </summary>
		static public void pingFolder(string assetsPath)
		{
			string path = "Assets/" + assetsPath;

			// Load object
			UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));

			// Select the object in the project folder
			Selection.activeObject = obj;

			// Also flash the folder yellow to highlight it
			EditorGUIUtility.PingObject(obj);
		}

	}

}
