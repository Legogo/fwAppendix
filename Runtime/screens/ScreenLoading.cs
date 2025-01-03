﻿using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// to call loading screen before everything else put <EngineLoadingScreenFeeder> in startup scene
/// 
/// by default screen cannot be displayed
/// it must be created by using the runtimeInit call
/// when it's created the system will be able to use show/hide routines
/// </summary>

namespace fwp.screens
{
    using fwp.scenes;

    public class ScreenLoading : ScreenObject
    {

        static protected ScreenLoading _instance;

        Camera cam;
        public Text txt;

        public override void reactOpen()
        {
            base.reactOpen();

            logScreen("loading:open");
        }

        public override void reactClose()
        {
            base.reactClose();

            logScreen("loading:close");
        }

        /// <summary>
        /// must be called by hand
        /// </summary>
        //[RuntimeInitializeOnLoadMethod]
        static public void runetimeInit()
        {
            //to make sure loading screen exist

            string scName = "screen-loading";

            if (!SceneLoader.isSceneAdded(scName) && SceneTools.checkIfCanBeLoaded(scName))
            {
                SceneManager.LoadSceneAsync(scName, LoadSceneMode.Additive);
            }

        }

        protected override void screenCreated()
        {
            base.screenCreated();

            _instance = this;

            if (txt != null) txt.enabled = false;

            cam = GetComponent<Camera>();
            if (cam == null) cam = GetComponentInChildren<Camera>();
        }

        static public void showLoadingScreen()
        {
            if (_instance == null) return;
            _instance.open();
        }

        static public void hideLoadingScreen()
        {
            if (_instance == null) return;
            _instance.close();
        }

    }

}
