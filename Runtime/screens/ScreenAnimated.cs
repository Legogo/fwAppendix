﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ces écrans ne doivent pas avoir de lien fort avec le maze
/// ils doivent etre TOUS load/unload dynamiquement en fonction des besoins
/// ex : on a pas de raison de les faire réagir au setup de la map
/// </summary>

namespace fwp.screens
{
    abstract public class ScreenAnimated : ScreenObject
    {
        static public List<ScreenAnimated> openedAnimatedScreens = new List<ScreenAnimated>();

        protected Animator _animator;

        Coroutine _coprocOpening;   // opening
        Coroutine _coprocClosing;   // closing
        bool _opened = false;       // interactable

        /// <summary>
        /// contains all data that can vary in other contexts
        /// </summary>
        public struct ScreenAnimatedParameters
        {
            public string bool_open;
            public string state_closed;
            public string state_opened;
        }

        protected ScreenAnimatedParameters parameters;

        //const string STATE_HIDING = "hiding";
        //const string STATE_OPENING = "opening";

        protected override void screenCreated()
        {
            base.screenCreated();

            parameters = new ScreenAnimatedParameters();
            parameters.bool_open = "open";
            parameters.state_closed = "closed";
            parameters.state_opened = "opened";

            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                if(transform.childCount > 0)
                {
                    _animator = transform.GetChild(0).GetComponent<Animator>();
                }
            }

            if(!hasValidAnimator())
            {
                Debug.LogWarning(getStamp() + " animator is NOT VALID");
            }

            //Debug.Assert(_animator != null, "screen animated animator missing ; voir avec andre");

            openedAnimatedScreens.Add(this);

            toggleVisible(false); // creation : make it invisible by default (but still active)
        }

        protected override void screenSetupLate()
        {
            base.screenSetupLate();

            if (isAutoOpenDuringSetup())
            {
                openAnimated();
            }
        }

        /// <summary>
        /// this context doesn't take into account any loading flow
        /// this MIGHT BE needed for context where engine needs to do stuff before opening
        /// </summary>
        virtual protected bool isAutoOpenDuringSetup()
        {
            return true;
        }

        protected override void onScreenDestruction()
        {
            base.onScreenDestruction();

            openedAnimatedScreens.Remove(this);
        }

        /// <summary>
        /// do not call this if the screen is already opening ?
        /// this will be ignored if screen is already in opening process
        /// </summary>
        public void openAnimated()
        {
            if (verbose) Debug.Log(getStamp() + " open animating TAB : " + name, transform);

            //already animating ?

            if (isOpening())
            {
                if (verbose)
                {
                    Debug.LogWarning(getStamp() + " => open animated => coroutine d'opening tourne déjà ?");
                    Debug.LogWarning(getStamp() + " trying to re-open the same screen during it's opening ?");
                }

                return;
            }

            _coprocOpening = StartCoroutine(processAnimatingOpening());
        }

        virtual protected void setupBeforeOpening()
        {
            //toggleVisible(false); // setup : hide before setup-ing
        }

        bool hasValidAnimator()
        {
            if (_animator == null) return false;
            if (_animator.runtimeAnimatorController == null) return false;
            return true;
        }

        IEnumerator processAnimatingOpening()
        {
            setupBeforeOpening();

            toggleVisible(true); // opening : just before animating (after setup)

            ScreenLoading.hideLoadingScreen(); // laby screen, now animating open screen

            if (hasValidAnimator())
            {
                _animator.SetBool(parameters.bool_open, true);

                //animator state change...
                yield return null;
                yield return null;
                yield return null;

                //... do something spec for animating screen
                //while (_animator.GetCurrentAnimatorStateInfo(0).IsName(STATE_OPENING)) yield return null;
                while (!_animator.GetCurrentAnimatorStateInfo(0).IsName(parameters.state_opened)) yield return null;
            }

            evtOpeningAnimationDone();
        }

        /// <summary>
        /// do something at the end of opening animation
        /// </summary>
        virtual protected void evtOpeningAnimationDone()
        {
            _coprocOpening = null;

            // this is done before "open animation"
            //toggleVisible(true); // opening animation done : jic

            _opened = true;

            if (verbose) Debug.Log(getStamp() + " OPENED");
        }

        /// <summary>
        /// called by external context
        /// for UI buttons
        /// </summary>
        public void actionClose()
        {
            closeAnimated();
        }

        virtual public void closeAnimated()
        {
            //Debug.Log(getStamp() + " close animated ?");

            if (isClosing())
            {
                Debug.LogWarning(" ... already closing");
                return;
            }

            if (verbose) Debug.Log(getStamp() + " CLOSING ...");

            _coprocClosing = StartCoroutine(processAnimatingClosing());
        }

        virtual protected void setupBeforeClosing()
        {
            _opened = false;
        }

        IEnumerator processAnimatingClosing()
        {
            yield return null; // laisser le temps a coprocClosing d'etre assigné :shrug:

            setupBeforeClosing();

            yield return null;
            yield return null;
            yield return null;

            if (hasValidAnimator())
            {
                _animator.SetBool(parameters.bool_open, false);

                Debug.Log("waiting for screen to close");

                while (!_animator.GetCurrentAnimatorStateInfo(0).IsName(parameters.state_closed)) yield return null;
            }

            evtClosingAnimationCompleted();
        }

        /// <summary>
        /// après l'anim de fermeture
        /// </summary>
        void evtClosingAnimationCompleted()
        {
            _opened = false; // jic
            _coprocClosing = null;

            onClosingAnimationCompleted();
        }

        virtual protected void onClosingAnimationCompleted()
        {
            unload();
        }

        /// <summary>
        /// /! 
        /// APRES anim open
        /// AVANT anim close
        /// </summary>
        public bool isOpen() => _opened;

        public bool isOpening() => _coprocOpening != null;
        public bool isClosing() => _coprocClosing != null;

        /// <summary>
        /// something above ?
        /// </summary>
        virtual protected bool isInteractable() => _opened;

        /// <summary>
        /// search from all opened screens
        /// </summary>
        static public ScreenAnimated getScreen(string screenName)
        {
            ScreenAnimated[] scs = GameObject.FindObjectsOfType<ScreenAnimated>();
            for (int i = 0; i < scs.Length; i++)
            {
                if (scs[i].isScreenOfSceneName(screenName)) return scs[i];
            }
            return null;
        }
        static public T getScreen<T>(string screenName) where T : ScreenAnimated
        {
            T[] scs = GameObject.FindObjectsOfType<T>();
            if (scs.Length <= 0) Debug.LogWarning("no screen <" + typeof(T) + "> present");

            for (int i = 0; i < scs.Length; i++)
            {
                if (scs[i].isScreenOfSceneName(screenName)) return scs[i];
            }
            return null;
        }

        static public ScreenAnimated toggleScreen(string scNameToOpen)
        {
            ScreenObject so = ScreensManager.getOpenedScreen(scNameToOpen);
            Debug.Log(so);

            ScreenAnimated sa = so as ScreenAnimated;
            Debug.Log(sa);

            if (sa.isOpen())
            {
                sa.closeAnimated();
                return sa;
            }

            Debug.LogWarning("toggling " + scNameToOpen + " did nothing ; screen is opening ?");

            return null;
        }

    }

}
