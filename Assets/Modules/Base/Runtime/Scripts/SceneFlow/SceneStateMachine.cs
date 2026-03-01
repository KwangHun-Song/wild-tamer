using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Base
{
    public class SceneStateMachine : MonoBehaviour
    {
        public Notifier Notifier { get; } = new();
        public SceneState CurrentState { get; private set; }

        public async UniTask ExecuteAsync()
        {
            SetUpAll();
            var children = CollectDirectChildStates(transform);
            await ExecuteChildrenAsync(children);
        }

        private void SetUpAll()
        {
            var allStates = GetComponentsInChildren<SceneState>(true);
            foreach (var state in allStates)
            {
                state.OnSetUp(this);
                state.gameObject.SetActive(false);
            }
        }

        private async UniTask ExecuteChildrenAsync(List<SceneState> children)
        {
            foreach (var child in children)
            {
                await ExecuteStateAsync(child);
            }
        }

        private async UniTask ExecuteStateAsync(SceneState state)
        {
            if (!state.CanEnter())
                return;

            CurrentState = state;
            state.gameObject.SetActive(true);
            Notifier.Notify<ISceneStateEnterEvent>(l => l.OnSceneStateEnter(state));

            try
            {
                await state.ExecuteAsync();

                var childStates = CollectDirectChildStates(state.transform);
                await ExecuteChildrenAsync(childStates);
            }
            finally
            {
                Notifier.Notify<ISceneStateExitEvent>(l => l.OnSceneStateExit(state));
                state.gameObject.SetActive(false);
            }
        }

        private static List<SceneState> CollectDirectChildStates(Transform parent)
        {
            var states = new List<SceneState>();
            foreach (Transform child in parent)
            {
                if (child.TryGetComponent<SceneState>(out var state))
                {
                    states.Add(state);
                }
            }
            return states;
        }
    }
}
