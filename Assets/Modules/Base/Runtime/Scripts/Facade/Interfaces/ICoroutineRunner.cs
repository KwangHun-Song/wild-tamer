using System;
using System.Collections;
using UnityEngine;

namespace Base
{
    public interface ICoroutineRunner
    {
        Coroutine StartCoroutine(IEnumerator routine);
        void StopCoroutine(Coroutine coroutine);
        void StopAllCoroutines();
        void DoSecondsAfter(Action action, float seconds);
        void DoFramesAfter(Action action, int frames);
    }
}
