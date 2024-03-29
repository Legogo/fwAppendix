﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

namespace fwp.scenes
{
    public class SceneLoaderFeederBase : MonoBehaviour
    {
        protected List<string> scene_names;
        protected SceneLoaderRunner runner;

        /// <summary>
        /// starts feed process
        /// contextCall is meant to filter if feeder must be called again
        /// </summary>
        public void feed()
        {
            Debug.Log(GetType() + "::feed()", transform);

            string[] nms = solveNames();

            //Debug.Log(EngineObject.getStamp(this) + " now feeding "+nms.Length+" names", transform);
            //for (int i = 0; i < nms.Length; i++) { Debug.Log("  L " + nms[i]);}

            runner = SceneLoader.loadScenes(nms, (assocs) => 
            {
                //Debug.Log("feed destroy");
                GameObject.Destroy(this);
            });
        }

        private void OnDestroy()
        {
            runner = null;
            //Debug.Log(EngineObject.getStamp(this) + " done feeding !");
        }

        public bool isFeeding() { return runner != null; }

        virtual protected string[] solveNames()
        {
            if (scene_names == null) scene_names = new List<string>();
            scene_names.Clear();
            return scene_names.ToArray();
        }

        protected void addWithPrefix(string prefix, string nm)
        {
            addWithPrefix(prefix, new string[] { nm });
        }

        protected void addWithPrefix(string prefix, string[] names)
        {
            if (names == null)
            {
                Debug.LogWarning("names is null for prefix " + prefix);
                return;
            }

            if (names.Length <= 0) return;

            //Debug.Log(prefix + " count ? " + names.Length);

            for (int i = 0; i < names.Length; i++)
            {
                scene_names.Add(prefix + names[i]);
            }
        }

        public string[] getNames()
        {
            return scene_names.ToArray();
        }

    }
}