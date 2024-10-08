﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.ObjectModel;
using System.Linq;


namespace fwp.industries
{
    using facebook;
    using System;

    using Object = UnityEngine.Object;

    /// <summary>
    /// wrapper object to make a factory for a specific type
    /// </summary>
    abstract public class FactoryBase<FaceType> : IFactory where FaceType : class, iFactoryObject
    {
        /// <summary>
        /// ReadOnly wrapper around list in facebook
        /// </summary>
        public ReadOnlyCollection<FaceType> actives = null;

        /// <summary>
        /// all objects currently available for recycling
        /// </summary>
        public List<FaceType> inactives = new List<FaceType>();

        System.Type _factoryTargetType;

        public FactoryBase()
        {
            _factoryTargetType = getFactoryTargetType();

            // get handle to RO list
            actives = IndusReferenceMgr.instance.GetGroup<FaceType>();

            if (!Application.isPlaying) refresh();
        }

        /// <summary>
        /// what kind of object will be created by this factory
        /// </summary>
        protected System.Type getFactoryTargetType() => typeof(FaceType);

        public void refresh()
        {
            log("refresh()");

            inactives.Clear();

            Object[] presents = (Object[])fwp.appendix.AppendixUtils.gcts(typeof(FaceType));
            for (int i = 0; i < presents.Length; i++)
            {
                inject(presents[i] as FaceType);
            }

            log("refresh:after x{actives.Count}");
        }

        //abstract public System.Type getChildrenType();

        public bool hasCandidates() => actives.Count > 0 || inactives.Count > 0;
        public bool hasCandidates(int countCheck) => (actives.Count + inactives.Count) >= countCheck;

        /// <summary>
        /// cannot implem this
        /// must use facebook RO collects
        /// 
        /// SHK to facebook content
        /// just transfert list
        /// </summary>
        //public ReadOnlyCollection<FaceType> getActives() => actives;

#if UNITY_EDITOR

        public List<iFactoryObject> getActives()
        {
            List<iFactoryObject> tmp = new List<iFactoryObject>();
            foreach (var e in actives)
            {
                tmp.Add(e as iFactoryObject);
            }
            return tmp;
        }

        /// <summary>
        /// DEBUG ONLY
        /// </summary>
        public List<iFactoryObject> getInactives()
        {
            return inactives.Cast<iFactoryObject>().ToList();
        }

#endif

        public FaceType getRandomActive()
        {
            Debug.Assert(actives.Count > 0, GetType() + " can't return random one if active list is empty :: " + actives.Count + "/" + inactives.Count);

            return actives[UnityEngine.Random.Range(0, actives.Count)];
        }

        public FaceType getNextActive(FaceType curr)
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
        /// path = factory folder (if any) / subtype
        /// this will load object blob AND instantiate
        /// </summary>
        abstract protected void instantiateAsync(string path, Action<UnityEngine.Object> onPresence);

        /// <summary>
        /// immediate load, no async
        /// </summary>
        abstract protected Object instantiate(string path);

        virtual protected string solvePath(string subType)
        {
            string folder = getObjectPath();
            if (!string.IsNullOrEmpty(folder)) subType = folder + "/" + subType;
            return subType;
        }

        /// <summary>
        /// async creation
        /// </summary>
        protected void createAsync(string subType, Action<FaceType> onPresence = null)
        {
            log("<b>" + subType + "</b> not available (x" + inactives.Count + ") : new, ASYNC");

            string path = solvePath(subType);
            instantiateAsync(path, (instance) =>
            {
                onPresence.Invoke(
                    solveNew(instance));
            });
        }

        /// <summary>
        /// instant creation
        /// generate a new element in the pool
        /// </summary>
        protected FaceType create(string subType)
        {
            string path = getObjectPath() + "/" + subType;

            log("no " + subType + " available (x" + inactives.Count + ") : new");

            return solveNew(instantiate(path));
        }

        FaceType solveNew(Object copy)
        {
            log(" created:" + copy, copy);

            GameObject go = copy as GameObject;

            //Debug.Log("newly created object " + go.name, go);

            FaceType candidate = go.GetComponent<FaceType>();
            Debug.Assert(candidate != null, $"could not retrieve comp<{typeof(FaceType)}> on gObject:{go} ?? generated object is not factory compatible", go);

            inactives.Add(candidate);
            //recycle(candidate);

            return candidate;
        }

        /// <summary>
        /// path within Resources/ where to find object to load
        /// this is only the path, without the name of the object
        /// object name/uid is located on interface
        /// 
        /// ie : "Bullets" -> Resources/Bullets
        /// </summary>
        abstract protected string getObjectPath();

        //public iFactoryObject extractObject(string subType) => (iFactoryObject)extract(subType);

        /// <summary>
        /// use fetch instead
        /// same as fetch, but return interface type
        /// </summary>
        public iFactoryObject extract(string uid) => fetch(uid);

        /// <summary>
        /// use fetch instead
        /// same as fetch, but return interface type
        /// </summary>
        public void extractAsync(string uid, Action<iFactoryObject> onPresence)
            => fetchAsync(uid, onPresence);

        /// <summary>
        /// recycle OR create
        /// </summary>
        public FaceType fetch(string subType)
        {
            FaceType instance = extractFromInactives(subType);

            if (instance == null)
            {
                instance = create(subType);
            }

            inject(instance);

            return instance;
        }

        /// <summary>
        /// recycle OR create
        /// </summary>
        public void fetchAsync(string subType, Action<FaceType> onPresence)
        {
            FaceType instance = extractFromInactives(subType);

            if (instance != null) // recycling
            {
                inject(instance);
                onPresence.Invoke(instance);
            }
            else
            {
                createAsync(subType, (instance) =>
                {
                    inject(instance);
                    onPresence.Invoke(instance);
                });
            }
        }

        /*
        public T extract<T>(string subType, Action<FaceType> onPresence)
        {
            FaceType icand = extract(subType, onPresence);
            Component com = icand as Component;
            return com.GetComponent<T>();
        }
        */

        FaceType extractFromInactives(string subType)
        {
            FaceType instance = null;

            //will add an item in inactive
            //and go on
            if (inactives.Count > 0)
            {

                // search in available pool
                for (int i = 0; i < inactives.Count; i++)
                {
                    if (inactives[i].factoGetCandidateName() == subType)
                    {
                        instance = inactives[i];
                    }
                }

            }

            return instance;
        }

        void recycleInternal(FaceType candid)
        {
            if (recycle(candid))
            {
                candid.factoRecycle();
            }
        }

        /// <summary>
        /// indiquer a la factory qu'un objet a changé d'état de recyclage
        /// </summary>
        public bool recycle(FaceType candid)
        {
            bool dirty = false;

            bool present = actives.Contains(candid);
            //Debug.Assert(present, candid + " is not in actives array ?");
            if (present)
            {
                IndusReferenceMgr.instance.Delete(candid);
                //actives.Remove(candid);

                dirty = true;
            }

            present = inactives.Contains(candid);
            //Debug.Assert(!present, candid + " must not be already in inactives");
            if (!present)
            {
                inactives.Add(candid);

                // DO NOT, inf loop
                //candid.factoRecycle();

                IndusReferenceMgr.instance.Delete(candid); // rem facebook

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
        /// to flag as used by facto
        /// generaly called when object is created (by facto or instance of a scene)
        /// - auto if created by factory
        /// - also used by pre-existing object in scene to flag part of facto
        /// </summary>
        public void inject(FaceType candid)
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
                // into facebook
                //actives.Add(candid);

                MonoBehaviour cmp = candid as MonoBehaviour;
                if (cmp != null) cmp.enabled = true;

                //IndusReferenceMgr.instance.Register((FaceType)candid);
                IndusReferenceMgr.instance.Register<FaceType>(candid);

                dirty = true;
            }

            if (dirty)
                log("inject :: " + candid + " :: ↑" + actives.Count + "/ ↓" + inactives.Count);
        }

        /// <summary>
        /// called by a destroyed object
        /// </summary>
        public void destroy(FaceType candid)
        {
            IndusReferenceMgr.instance.Delete(candid);
            if (inactives.IndexOf(candid) > -1) inactives.Remove(candid);
        }

        public void recycleAll()
        {
            log("recycleAll()");

            List<FaceType> cands = new List<FaceType>();
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
            IndustriesVerbosity.sLog(getStamp() + content, target as Object);
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
    public interface iFactoryObject : IFacebook
    {

        /// <summary>
        /// the actual name of the object to instantiate
        /// to be able to compare signatures when extracting and recycling
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
