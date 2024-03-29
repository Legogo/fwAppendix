﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

namespace fwp.screens
{
    using fwp.scenes;

    public class ScreensManager
    {
        static public bool verbose;

        static protected List<ScreenObject> screens = new List<ScreenObject>();

        //usual screen names
        public enum ScreenNameGenerics
        {
            home, // home menu
            ingame, // ingame interface (ui)
            pause, // pause screen
            result, // end of round screen, result of round
            loading
        };

        /// <summary>
        /// a ScreenObject will sub here during its Awake
        /// </summary>
        static public void subScreen(ScreenObject so)
        {
            if (screens.Contains(so)) return;

            screens.Add(so);

            if(verbose)
                Debug.Log(so.name + "       is now subscribed to screens");
        }

        static public void unsubScreen(ScreenObject so)
        {
            if (!screens.Contains(so)) return;

            screens.Remove(so);

            if(verbose)
                Debug.Log(so.name + "       is now removed from screens (screen destroy)");
        }

        static protected void fetchScreens()
        {
            if (screens == null) screens = new List<ScreenObject>();
            screens.Clear();
            screens.AddRange(fwp.appendix.qh.gcs<ScreenObject>());
        }

        static public bool hasOpenScreenOfType(ScreenObject.ScreenType type)
        {
            ScreenObject so = getOpenedScreen();
            if (so == null) return false;
            return so.type == type;
        }

        /// <summary>
        /// returns NON-STICKY visible screen
        /// </summary>
        /// <returns></returns>
        static public ScreenObject getOpenedScreen()
        {
            if (screens == null) return null;
            return screens.Select(x => x).Where(x => x.isVisible()).FirstOrDefault();
        }

        /// <summary>
        /// returns NON-STICKY visible screen
        /// </summary>
        /// <returns></returns>
        static public ScreenObject getFirstOpenedScreen()
        {
            //fetchScreens();

            for (int i = 0; i < screens.Count; i++)
            {
                if (screens[i].isVisible())
                {
                    return screens[i];
                }
            }

            return null;
            //return screens.Select(x => x).Where(x => !x.sticky && x.isVisible()).FirstOrDefault();
        }

        static public List<ScreenObject> getLoadedScreens()
        {
            return screens;
        }

        /// <summary>
        /// si un screen visible contient "nm"
        /// </summary>
        static public bool isAScreenContainNameOpened(string nm)
        {
            List<ScreenObject> sos = getLoadedScreens();
            for (int i = 0; i < sos.Count; i++)
            {
                if (sos[i].isVisible())
                {
                    if (sos[i].name.Contains(nm)) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// to return the screen if already open
        /// to call the screen use open() flow instead
        /// </summary>
        static public ScreenObject getOpenedScreen(ScreenNameGenerics nm) => getOpenedScreen(nm.ToString());
        static public ScreenObject getOpenedScreen(System.Enum enu) => getOpenedScreen(enu.ToString());
        static public ScreenObject getOpenedScreen(string nm)
        {
            if(screens.Count <= 0)
            {
                //Debug.LogWarning("asking for a screen " + nm + " but screen count is 0");
                return null;
            }

            ScreenObject so = screens.Select(x => x).Where(x => x.isScreenOfSceneName(nm)).FirstOrDefault();
            
            /*
            if (so == null)
            {
                Debug.LogWarning($"{getStamp()} getScreen({nm}) <color=red>no screen that END WITH that name</color> (screens count : {screens.Count})");
                for (int i = 0; i < screens.Count; i++) Debug.Log("  #"+i+","+screens[i]);
            }
            */

            return so;
        }

        static public void unloadScreen(string nm)
        {
            ScreenObject so = getOpenedScreen(nm);
            if (so != null)
            {
                Debug.Log("unloading screen | asked name : " + nm);
                so.unload();
            }
        }

        /// <summary>
        /// deprecated
        /// </summary>
        static bool checkCompatibility(string nm)
        {
            string[] nms = System.Enum.GetNames(typeof(ScreenObject.ScreenType));
            for (int i = 0; i < nms.Length; i++)
            {
                if (nm.StartsWith(nms[i])) return true;
            }

            Debug.LogWarning("given screen " + nm + " is not compatible with screen logic ; must start with type in name");

            return false;
        }

        static public ScreenObject open(System.Enum enu, Action<ScreenObject> onComplete = null) => open(enu.ToString(), onComplete);
        static public ScreenObject open(string nm, Action<ScreenObject> onComplete) { return open(nm, string.Empty, onComplete); }

        /// <summary>
        /// best practice : should never call a screen by name but create a contextual enum
        /// this function won't return a screen that is not already loaded
        /// </summary>
        static public ScreenObject open(string nm, string filterName = "", Action<ScreenObject> onComplete = null)
        {
            
            // already present ?
            ScreenObject so = getOpenedScreen(nm);
            if (so != null)
            {
                if(verbose)
                    Debug.Log($"{getStamp()} | open:<b>{nm}</b> ({filterName}) | already present, changing visibility");

                // show
                changeScreenVisibleState(nm, true, filterName);
                
                onComplete?.Invoke(so);

                return so;
            }

            if(verbose)
                Debug.Log($"{getStamp()} | open:<b>{nm}</b> ({filterName}) | not already present, load");

            // not present : try to load it
            loadMissingScreen(nm, delegate (ScreenObject loadedScreen)
            {
                onComplete?.Invoke(loadedScreen);
            });

            return null;
        }

        static protected void changeScreenVisibleState(string scName, bool state, string containsFilter = "", bool force = false)
        {
            fetchScreens();

            //Debug.Log("opening " + scName + " (filter ? " + filter + ")");

            ScreenObject selected = getOpenedScreen(scName);
            if (selected == null)
            {
                Debug.LogWarning($"changeScreenVisibleState:{scName} : this ScreenObject doesn't exist ?");
                return;
            }

            bool hideOthers = selected.tags.HasFlag(ScreenObject.ScreenTags.hideOtherLayerOnShow);

            //Debug.Log(selected.name + " visibilty to " + state+" (filter ? "+containsFilter+" | dont hide other ? "+selected.dontHideOtherOnShow+" => hide others ? "+hideOthers+")");

            //on opening a specific screen we close all other non sticky screens
            if (hideOthers && state)
            {
                for (int i = 0; i < screens.Count; i++)
                {
                    if (screens[i] == selected) continue;

                    //do nothing with filtered screen
                    if (containsFilter.Length > 0 && screens[i].name.Contains(containsFilter)) continue;

                    screens[i].hide();
                    //Debug.Log("  L "+screens[i].name + " hidden");
                }

            }

            if (state) selected.show();
            else
            {
                if (force) selected.forceHide();
                else selected.hide(); // stickies won't hide
            }

        }

        static public void close(ScreenNameGenerics scName) { close(scName.ToString()); }
        static public void close(string scName) { close(scName, "", false); }
        static public void close(ScreenNameGenerics scName, bool force = false) { close(scName.ToString(), "", force); }
        static public void close(ScreenNameGenerics scName, string filter = "", bool force = false) { close(scName.ToString(), filter, force); }

        /// <summary>
        /// </summary>
        /// <param name="nameEnd"></param>
        /// <param name="force">if screen is sticky</param>
        static protected void close(string nameEnd, string filter = "", bool force = false)
        {
            changeScreenVisibleState(nameEnd, false, filter, force);
        }

        [ContextMenu("kill all")]
        public void killAll(string filterName = "")
        {
            fetchScreens();

            for (int i = 0; i < screens.Count; i++)
            {
                if (filterName.Length > 0)
                {
                    if (screens[i].name.EndsWith(filterName)) continue;
                }

                screens[i].hide();
            }
        }

        /// <summary>
        /// leader will be the only screen visible
        /// only works for overlays
        /// </summary>
        static public void setStandby(ScreenObject leader)
        {
            for (int i = 0; i < screens.Count; i++)
            {
                if(screens[i].type == ScreenObject.ScreenType.overlay)
                {
                    screens[i].setStandby(leader);
                }
            }
        }

        static protected void loadMissingScreen(string screenName, Action<ScreenObject> onComplete)
        {
            // don't, let the context choose to show it or not
            //ScreenLoading.showLoadingScreen();

            // first search if already exists
            ScreenObject so = getOpenedScreen(screenName);
            if (so != null)
            {
                onComplete(so);
                return;
            }

            if(verbose)
                Debug.Log("loadMissingScreen | screen to open : <b>" + screenName + "</b>");

            SceneLoader.queryScene(screenName, (assoc) =>
            {
                so = getOpenedScreen(screenName);
                if (so == null)
                {
                    Debug.LogError(getStamp() + " | end of screen loading (name given : " + screenName + ") but no <ScreenObject> returned");
                }
                onComplete(so);
            });

        }

        /// <summary>
        /// just display, no state change
        /// </summary>
        /// <param name="state"></param>
        static public void callPauseScreen(bool state)
        {

            if (state) ScreensManager.open(ScreensManager.ScreenNameGenerics.pause);
            else ScreensManager.close(ScreensManager.ScreenNameGenerics.pause);

        }

        static string getStamp()
        {
            return "~~ScreensManager~~";
        }

    }

}
