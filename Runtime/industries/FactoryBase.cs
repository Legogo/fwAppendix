﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace fwp.industries
{
    /// <summary>
    /// wrapper object to make a factory for a specific type
    /// </summary>
    abstract public class FactoryBase
    {
        static public bool verbose = false;

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Window/Industries/(verbose) factory")]
        static public void toggleVerbose()
        {
            verbose = !verbose;
            Debug.LogWarning("toggling verbose for factories : " + verbose);
        }
#endif

        //List<FactoryObject> pool = new List<FactoryObject>();
        protected List<iFactoryObject> actives = new List<iFactoryObject>();
        List<iFactoryObject> inactives = new List<iFactoryObject>();

        System.Type _factoryTargetType;

        public FactoryBase()
        {
            _factoryTargetType = getFactoryTargetType();

            IndusReferenceMgr.instance.injectType(_factoryTargetType);

            if (!Application.isPlaying) refresh();
        }

        /// <summary>
        /// what kind of object will be created by this factory
        /// </summary>
        abstract protected System.Type getFactoryTargetType();

        public void refresh()
        {
            log("refresh()");

            actives.Clear();
            inactives.Clear();

            //List<T> actives = getActives<T>();
            Object[] presents = (Object[])GameObject.FindObjectsOfType(_factoryTargetType);
            for (int i = 0; i < presents.Length; i++)
            {
                inject(presents[i] as iFactoryObject);
            }

            log("refresh:after x{actives.Count}");
        }

        //abstract public System.Type getChildrenType();

        public bool hasCandidates() => actives.Count > 0 || inactives.Count > 0;
        public bool hasCandidates(int countCheck) => (actives.Count + inactives.Count) >= countCheck;

        /// <summary>
        /// just transfert list
        /// </summary>
        public List<iFactoryObject> getActives()
        {
            return actives;
        }

        /// <summary>
        /// only for debug
        /// </summary>
        public iFactoryObject[] getInactives()
        {
            return inactives.ToArray();
        }

        public List<T> getActives<T>() where T : iFactoryObject
        {
            List<T> tmp = new List<T>();
            for (int i = 0; i < actives.Count; i++)
            {
                T candid = (T)actives[i];
                if (candid == null) continue;
                tmp.Add(candid);
            }

            //Debug.Log(typeof(T)+" ? candid = "+tmp.Count + " / active count = " + actives.Count);

            return tmp;
        }

        public iFactoryObject getRandomActive()
        {
            Debug.Assert(actives.Count > 0, GetType() + " can't return random one if active list is empty :: " + actives.Count + "/" + inactives.Count);

            return actives[Random.Range(0, actives.Count)];
        }
        public iFactoryObject getNextActive(iFactoryObject curr)
        {
            int idx = actives.IndexOf(curr);
            if (idx > -1)
            {
                if (idx + 1 < actives.Count) return actives[idx + 1];
                return actives[0]; // loop
            }

            Debug.LogError(curr + " is not in factory ?");

            return null;
        }

        /// <summary>
        /// générer un nouveau element dans le pool
        /// </summary>
        protected iFactoryObject create(string subType)
        {
            string path = System.IO.Path.Combine(getObjectPath(), subType);
            Object obj = Resources.Load(path);

            if (obj == null)
            {
                Debug.LogWarning(getStamp() + " /! <color=red>null object</color> @ " + path);
                return null;
            }

            obj = GameObject.Instantiate(obj);

            log(" created:" + obj, obj);

            GameObject go = obj as GameObject;

            //Debug.Log("newly created object " + go.name, go);

            iFactoryObject candidate = go.GetComponent<iFactoryObject>();
            Debug.Assert(candidate != null, $"no candidate on {go} ?? generated object is not factory compatible", go);

            inactives.Add(candidate);
            //recycle(candidate);

            //for refs list
            //IndusReferenceMgr.refreshGroupByType(factoryTargetType);
            //IndusReferenceMgr.injectObject(candidate);

            return candidate;
        }
        abstract protected string getObjectPath();

        /// <summary>
        /// demander a la factory de filer un element dispo
        /// subType est le nom du prefab dans le dossier correspondant
        /// </summary>
        public iFactoryObject extract(string subType)
        {
            iFactoryObject obj = null;

            //will add an item in inactive
            //and go on
            if (inactives.Count > 0)
            {

                // search in available pool
                for (int i = 0; i < inactives.Count; i++)
                {
                    if (inactives[i].factoGetCandidateName() == subType)
                    {
                        obj = inactives[i];
                    }
                }

            }

            // none available, create a new one
            if (obj == null)
            {
                log("no " + subType + " available (x" + inactives.Count + ") creating one");
                obj = create(subType);
            }

            // created object might be null, if resource path is pointing to nothing
            if (obj == null)
                return null;

            // make it active
            inject(obj);

            //va se faire tout seul au setup()
            //obj.materialize();

            return obj;
        }

        public T extract<T>(string subType)
        {
            iFactoryObject icand = extract(subType);
            Component com = icand as Component;
            return com.GetComponent<T>();
        }

        void recycleInternal(iFactoryObject candid)
        {
            if (recycle(candid))
            {
                candid.factoRecycle();
            }
        }

        /// <summary>
        /// indiquer a la factory qu'un objet a changé d'état de recyclage
        /// </summary>
        public bool recycle(iFactoryObject candid)
        {
            bool dirty = false;

            bool present = actives.Contains(candid);
            //Debug.Assert(present, candid + " is not in actives array ?");
            if (present)
            {
                actives.Remove(candid);
                dirty = true;
            }

            present = inactives.Contains(candid);
            //Debug.Assert(!present, candid + " must not be already in inactives");
            if (!present)
            {
                inactives.Add(candid);

                // DO NOT, inf loop
                //candid.factoRecycle();

                IndusReferenceMgr.instance.removeObject(candid); // rem facebook

                dirty = true;
            }

            // move recycled object into facto scene
            MonoBehaviour comp = candid as MonoBehaviour;

            // edge case where recycling is called when destroying the object
            if (!IsNullOrDestroyed(comp))
            {
                if (comp.transform != null)
                {
                    comp.transform.SetParent(null);
                }

                // do something more ?
                //comp.gameObject.SetActive(false);
                //comp.enabled = false;

                /*
                //https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.MoveGameObjectToScene.html
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(
                    comp.gameObject,
                    UnityEngine.SceneManagement.SceneManager.GetSceneByName(TinyConst.scene_resources_facto));
                */
            }

            if (dirty)
                log(" :: recycle :: " + candid + " :: ↑" + actives.Count + "/ ↓" + inactives.Count);

            return dirty;
        }

        /// <summary>
        /// quand un objet est déclaré comme utilisé par le systeme
        /// généralement cette méthode est appellé a la création d'un objet lié a la facto
        /// </summary>
        public void inject(iFactoryObject candid)
        {
            Debug.Assert(candid != null, "candid to inject is null ?");

            bool dirty = false;

            if (inactives.Contains(candid))
            {
                inactives.Remove(candid);

                dirty = true;
            }

            if (actives.IndexOf(candid) < 0)
            {
                actives.Add(candid);

                //candid.factoMaterialize();

                MonoBehaviour cmp = candid as MonoBehaviour;
                if (cmp != null) cmp.enabled = true;

                IndusReferenceMgr.instance.injectObject(candid);

                dirty = true;
            }

            if (dirty)
                log("inject :: " + candid + " :: ↑" + actives.Count + "/ ↓" + inactives.Count);
        }

        /// <summary>
        /// called by a destroyed object
        /// </summary>
        public void destroy(iFactoryObject candid)
        {
            if (actives.IndexOf(candid) > -1) actives.Remove(candid);
            if (inactives.IndexOf(candid) > -1) inactives.Remove(candid);
        }

        public void recycleAll()
        {
            log("recycleAll()");

            List<iFactoryObject> cands = new List<iFactoryObject>();
            cands.AddRange(actives);

            // use INTERNAL to avoid inf loops

            for (int i = 0; i < cands.Count; i++)
            {
                recycleInternal(cands[i]);
                //recycle(cands[i]);
            }

            Debug.Assert(actives.Count <= 0);
        }

        string getStamp() => "<color=#3333aa>" + GetType() + "|" + _factoryTargetType + "</color>";

        void log(string content, object target = null)
        {

#if UNITY_EDITOR || industries
            bool showLog = verbose;

            if (showLog)
                Debug.Log(getStamp() + content, target as Object);
#endif
        }


        /// <summary>
        /// https://forum.unity.com/threads/how-to-check-if-a-gameobject-is-being-destroyed.1030849/
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsNullOrDestroyed(System.Object obj)
        {
            if (object.ReferenceEquals(obj, null)) return true;

            if (obj is UnityEngine.Object) return (obj as UnityEngine.Object) == null;

            return false;
        }

    }

    //public interface IFactory{}

    /// <summary>
    /// make ref compatible with factories
    /// </summary>
    public interface iFactoryObject : iIndusReference
    {

        /// <summary>
        /// the actual name of the object to instantiate
        /// Resources/{facto}/{CandidateName}
        /// </summary>
        string factoGetCandidateName();

        /// <summary>
        /// not called if app ask for a recycle
        /// only during event when factory is told to recycling everything
        /// </summary>
        void factoRecycle();

        /// <summary>
        /// when object is added to factory lists
        /// this is called when factory provide this object
        /// describe activation
        /// called when added to actives
        /// </summary>
        //void factoMaterialize();

        //string serialize();
    }
}
