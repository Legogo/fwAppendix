using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace fwp.industries
{
    /// <summary>
    /// manager of all FACTORIES
    /// </summary>
    static public class FactoriesMgr
    {
        static private List<IFactory> factos = new List<IFactory>(); // FactoryBase<iFactoryObject>

        /// <summary>
        /// for debug only
        /// gives copy
        /// </summary>
        static public IFactory[] getAllFactories() => factos.ToArray();

        static public void recycleEverything()
        {
            Debug.Log("Facto  :  recycle all");
            foreach (var facto in factos)
            {
                facto.recycleAll();
            }
        }

        /// <summary>
        /// ie => seek : BrainScrapling
        /// </summary>
        static public T getFactoryOf<T>() where T : IFactory
        {
            // already exists ?
            foreach (var f in factos)
            {
                Debug.Assert(f != null, "an item in factos[] is null ??");
                try
                {
                    // need to compare type
                    // can't cast if not matching
                    if(f.GetType() == typeof(T))
                    {
                        return (T)f;
                    }
                }
                catch
                {
                    Debug.LogError("facto cast :: can't cast " + typeof(T));
                }
            }

            return create<T>();
        }

        /// <summary>
        /// create the factory instance
        /// </summary>
        static private T create<T>() where T : IFactory
        {
            //https://stackoverflow.com/questions/731452/create-instance-of-generic-type-whose-constructor-requires-a-parameter

            //if (IndusReferenceMgr.verbose) Debug.Log("creating new facto : <b>" + typeof(T) + "</b>");

            T fb;

            object instance = Activator.CreateInstance<T>();
            Debug.Assert(instance != null);

            fb = (T)instance;
            Debug.Assert(fb != null, $"implem for {typeof(T)} , check typo ?");

            //FactoryBase fb = (FactoryBase)Activator.CreateInstance(typeof(FactoryBase), new object[] { tarType });
            factos.Add(fb);

            if (IndusReferenceMgr.verbose) Debug.Log($"Facto:   created new factory <b>{typeof(T)}</b> , total x{factos.Count}");

            return fb;
        }

        static public void inject(IFactory facto)
        {
            factos.Add(facto);
        }
    }

}
