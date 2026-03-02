using System;
using System.Collections.Generic;
using Base;

namespace FiniteStateMachine
{
    public abstract class StateMachine<TEntity, TEnumTrigger> where TEnumTrigger : Enum
    {
        public TEntity Owner { get; private set; }
        public Notifier Notifier { get; } = new();
        public State<TEntity, TEnumTrigger> CurrentState { get; private set; }

        private readonly Dictionary<State<TEntity, TEnumTrigger>, List<StateTransition<TEntity, TEnumTrigger>>> transitionLookup = new();

        protected StateMachine(TEntity owner)
        {
            Owner = owner;
        }

        protected abstract State<TEntity, TEnumTrigger> InitialState { get; }
        protected abstract State<TEntity, TEnumTrigger>[] States { get; }
        protected abstract StateTransition<TEntity, TEnumTrigger>[] Transitions { get; }

        public void SetUp()
        {
            States.ForEach(state => state.SetUp(this, Owner));
            BuildTransitionLookup();

            ChangeState(InitialState);
        }

        public void Update()
        {
            CurrentState?.OnUpdate();
            TryTransition();
        }

        // 현재 상태를 체크해서 전이를 시도한다.
        public bool TryTransition()
        {
            if (CurrentState is null || !transitionLookup.TryGetValue(CurrentState, out var transitions))
                return false;

            foreach (var transition in transitions)
            {
                if (!transition.IsTransferable)
                    continue;

                ChangeState(transition.ToState);
                return true;
            }

            return false;
        }

        // 커맨드를 실행하여 전이를 시도한다.
        // 트리거 매칭만 확인하며, Condition은 무시한다.
        // Condition 기반 전이는 TryTransition/Update를 사용한다.
        public bool ExecuteCommand(TEnumTrigger inTrigger)
        {
            if (CurrentState is null || !transitionLookup.TryGetValue(CurrentState, out var transitions))
                return false;

            foreach (var transition in transitions)
            {
                if (!transition.HasTrigger)
                    continue;

                if (!transition.TransitionTrigger.Equals(inTrigger))
                    continue;

                ChangeState(transition.ToState);
                return true;
            }

            return false;
        }

        public bool SendMessage(int inCommand, object inData)
        {
            return CurrentState.OnReceiveCommand(inCommand, inData);
        }

        private void ChangeState(State<TEntity, TEnumTrigger> inState)
        {
            // 처음 상태가 시작될 때는 실행 중인 스테이트가 없다.
            var prevState = CurrentState;
            if (prevState is not null)
            {
                prevState.OnExit();
            }

            CurrentState = inState;
            inState.OnEnter();

            Notifier.Notify<IStateChangeEvent<TEntity, TEnumTrigger>>(listener => listener.OnStateChange(this, prevState, inState));
        }

        private void BuildTransitionLookup()
        {
            transitionLookup.Clear();

            foreach (var transition in Transitions)
            {
                if (!transitionLookup.TryGetValue(transition.FromState, out var transitions))
                {
                    transitions = new List<StateTransition<TEntity, TEnumTrigger>>();
                    transitionLookup.Add(transition.FromState, transitions);
                }

                transitions.Add(transition);
            }
        }
    }
}
