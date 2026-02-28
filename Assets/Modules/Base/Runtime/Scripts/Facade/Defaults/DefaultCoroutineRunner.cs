using System;
using System.Collections;
using UnityEngine;

namespace Base
{
    public class DefaultCoroutineRunner : MonoBehaviour, ICoroutineRunner
    {
        public void DoSecondsAfter(Action action, float seconds)
        {
            StartCoroutine(DelaySecondsRoutine(action, seconds));
        }

        public void DoFramesAfter(Action action, int frames)
        {
            StartCoroutine(DelayFramesRoutine(action, frames));
        }

        private static IEnumerator DelaySecondsRoutine(Action action, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            action?.Invoke();
        }

        private static IEnumerator DelayFramesRoutine(Action action, int frames)
        {
            for (int i = 0; i < frames; i++)
                yield return null;

            action?.Invoke();
        }
    }
}
