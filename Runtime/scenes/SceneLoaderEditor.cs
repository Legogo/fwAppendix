using System.Collections.Generic;


using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace fwp.scenes
{
	using fwp.appendix;

	public class SceneLoaderEditor
	{


#if UNITY_EDITOR
		static public void loadScene(string nm, OpenSceneMode mode = OpenSceneMode.Additive)
		{
			string path = AppendixUtils.getPathOfSceneInProject(nm);
			if (path.Length <= 0)
			{
				Debug.LogWarning($" no path for {nm} in build settings");
				return;
			}

			Debug.Log("(editor) OpenScene(" + path + ")");

			EditorSceneManager.OpenScene(path, mode);
		}

		static public void loadSceneByBuildSettingsPresence(string nm, bool forceAddToBuildSettings = false, OpenSceneMode mode = OpenSceneMode.Additive)
		{

			// check if in build settings
			if (!AppendixUtils.isSceneInBuildSettings(nm, true))
			{
				//  if NOT add to build settings

				if (forceAddToBuildSettings)
				{
					AppendixUtils.addSceneToBuildSettings(nm);
					Debug.Log($"added {nm} was re-added to build settings");
				}

			}

			string path = AppendixUtils.getBuildSettingsFullPathOfScene(nm);
			if (path.Length <= 0)
			{
				Debug.LogWarning($" no path for {nm} in build settings");
				return;
			}

			EditorSceneManager.OpenScene(path, mode);
		}


#endif

	}

}
