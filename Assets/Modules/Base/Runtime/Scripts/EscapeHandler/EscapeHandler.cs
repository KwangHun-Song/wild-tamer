using System.Collections.Generic;
using UnityEngine;

namespace Base
{
    public class EscapeHandler : MonoBehaviour, IEscapeHandler
    {
        public Notifier Notifier { get; } = new();

        private readonly Stack<IEscapeListener> stack = new();

        public void Register(IEscapeListener listener)
        {
            stack.Push(listener);
        }

        public void Unregister(IEscapeListener listener)
        {
            stack.Pop();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && stack.Count > 0)
                stack.Peek().OnEscape();
        }
    }
}
