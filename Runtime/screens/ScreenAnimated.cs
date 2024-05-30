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
            public string state_closed; // name of the state when screen is closed
            public string state_opened;
        }

        protected ScreenAnimatedParameters parameters;

        //const string STATE_HIDING = "hiding";
        //const string STATE_OPENING = "opening";

        /// <summary>
        /// constructor / awake
        /// </summary>
        protected override void screenCreated()
        {
            base.screenCreated();

            parameters = generateAnimatedParams();
            
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                // seek one in immediate children only
                foreach (Transform child in transform)
                {
                    _animator = child.GetComponent<Animator>();
                }
            }

            if (!hasValidAnimator()) logwScreen("create : could not fetch a valid animator ?");

            //Debug.Assert(_animator != null, "screen animated animator missing ; voir avec andre");

            openedAnimatedScreens.Add(this);

            if(isAutoOpenDuringSetup())
            {
                logScreen("animated:auto open = hide on creation");
                toggleVisible(false);
            }
        }

        protected override void onScreenDestruction()
        {
            base.onScreenDestruction();

            openedAnimatedScreens.Remove(this);
        }

        virtual protected ScreenAnimatedParameters generateAnimatedParams()
        {
            var _parameters = new ScreenAnimatedParameters();
            _parameters.bool_open = "open";
            _parameters.state_closed = "closed";
            _parameters.state_opened = "opened";

            return _parameters;
        }

        protected override void screenSetupLate()
        {
            base.screenSetupLate();

            if (isAutoOpenDuringSetup()) // true by default
            {
                logScreen("animated:auto open");
                openAnimated();
            }
        }

        /// <summary>
        /// this context doesn't take into account any loading flow
        /// this MIGHT BE needed for context where engine needs to do stuff before opening
        /// </summary>
        virtual protected bool isAutoOpenDuringSetup() => true;

        /// <summary>
        /// do not call this if the screen is already opening ?
        /// this will be ignored if screen is already in opening process
        /// </summary>
        public void openAnimated()
        {
            logScreen(" open animating TAB : " + name, transform);

            //already animating ?

            if (isOpening())
            {
                if (verbose)
                {
                    logwScreen(" => open animated => coroutine d'opening tourne déjà ?");
                    logwScreen(" trying to re-open the same screen during it's opening ?");
                }

                return;
            }

            _coprocOpening = StartCoroutine(processAnimatingOpening());
        }

        virtual protected void setupBeforeOpening()
        {
            //toggleVisible(false); // setup : hide before setup-ing
        }

        virtual protected bool hasValidAnimator()
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
                IEnumerator process = processWaitUntilState(parameters.state_opened);
                while (process.MoveNext()) yield return null;
            }

            onOpeningAnimationDone();
        }

        /// <summary>
        /// do something at the end of opening animation
        /// </summary>
        virtual protected void onOpeningAnimationDone()
        {
            _coprocOpening = null;

            // this is done before "open animation"
            //toggleVisible(true); // opening animation done : jic

            _opened = true;
            logScreen("animated:opening:done");
        }

        /// <summary>
        /// called by external context
        /// for UI buttons
        /// </summary>
        public void actionClose() => closeAnimated();

        /// <summary>
        /// will animate
        /// </summary>
        public void closeAnimated()
        {
            //Debug.Log(getStamp() + " close animated ?");

            if (isClosing())
            {
                logwScreen(" ... already closing");
                return;
            }

            logScreen("CLOSING ...");

            _opened = false;

            setupBeforeClosing();

            _coprocClosing = StartCoroutine(processAnimatingClosing());
        }

        virtual protected void setupBeforeClosing()
        {
            logScreen("animated:closing:setup");
        }

        IEnumerator processAnimatingClosing()
        {
            yield return null;

            if (hasValidAnimator())
            {
                logScreen("animated:closing:animated ...");

                _animator.SetBool(parameters.bool_open, false);

                logScreen("animated:waiting for screen to end close animation");

                // wait for closed state
                IEnumerator process = processWaitUntilState(parameters.state_closed);
                while (process.MoveNext()) yield return null;

                //logScreen("animated:closing:animated state is done");
            }

            _coprocClosing = null;
            _opened = false;

            onClosingAnimationCompleted();
        }

        /// <summary>
        /// allow to change behavior
        /// default : unload the scene after hiding animation is done
        /// </summary>
        virtual protected bool isUnloadAfterClosing() => true;

        /// <summary>
        /// do more stuff after closing
        /// </summary>
        virtual protected void onClosingAnimationCompleted()
        {
            logScreen("animated:closing animation completed");

            if (isUnloadAfterClosing())
            {
                //won't if sticky
                unload();
            }
            else
            {
                hide();
            }
        }

        public bool isBusy()
        {
            if (isOpening()) return true;
            if (isClosing()) return true;
            return isOpen();
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

        //Coroutine waitUntilState(string state, System.Action onCompletion = null) => StartCoroutine(processWaitUntilState(state, onCompletion));
        //Coroutine waitExitState(string state, System.Action onCompletion = null) => StartCoroutine(processWaitExitState(state, onCompletion));

        IEnumerator processWaitUntilStateDone(string state)
        {
            // wait for state to start
            IEnumerator process = processWaitUntilState(state);
            while (process.MoveNext()) yield return null;

            // wait for state to end
            AnimatorStateInfo info;
            do
            {
                info = _animator.GetCurrentAnimatorStateInfo(0);
                yield return null;
            }
            while (info.normalizedTime >= 1f);
        }

        IEnumerator processWaitUntilState(string state, System.Action onCompletion = null)
        {
            //logScreen(" ... wait for state:" + state);

            AnimatorStateInfo info;

            //wait for state to start
            do
            {
                info = _animator.GetCurrentAnimatorStateInfo(0);
                yield return null;
            }
            while (info.IsName(state));

            //logScreen("state:" + state + " STARTED");

            onCompletion?.Invoke();
        }

        /// <summary>
        /// search from all opened screens
        /// </summary>
        static public ScreenAnimated getScreen(string screenName)
        {
            ScreenAnimated[] scs = fwp.appendix.qh.gcs<ScreenAnimated>();
            for (int i = 0; i < scs.Length; i++)
            {
                if (scs[i].isScreenOfSceneName(screenName)) return scs[i];
            }
            return null;
        }

        static public T getScreen<T>(string screenName) where T : ScreenAnimated
        {
            T[] scs = fwp.appendix.qh.gcs<T>();

            if (scs.Length <= 0) Debug.LogWarning("no screen <" + typeof(T) + "> present (to return screen of name : " + screenName + ")");
            else
            {
                for (int i = 0; i < scs.Length; i++)
                {
                    if (scs[i].isScreenOfSceneName(screenName)) return scs[i];
                }
            }

            return null;
        }

        static public void toggleScreen(string screenName)
        {
            ScreenAnimated so = (ScreenAnimated)ScreensManager.getOpenedScreen(screenName);

            // present ?
            if (so != null)
            {
                if (so.isBusy())
                    return;

                if (so.isOpen()) so.closeAnimated();
                else so.openAnimated();

                return;
            }

            // not there
            ScreensManager.open(screenName, (screen) =>
            {
                so = screen as ScreenAnimated;
                if (so != null)
                {
                    so.openAnimated();
                }
            });

        }

    }

}
