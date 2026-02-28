using System;
using UnityEngine;

namespace FiniteStateMachine
{
    public class StateTransition<TEntity, TEnumTrigger> where TEnumTrigger : Enum
    {
        public State<TEntity, TEnumTrigger> FromState { get; }
        public State<TEntity, TEnumTrigger> ToState { get; }

        public bool HasTrigger { get; }
        public TEnumTrigger TransitionTrigger { get; }
        public Func<State<TEntity, TEnumTrigger>, bool> TransitionCondition { get; }

        public bool IsTransferable => TransitionCondition != null && TransitionCondition.Invoke(FromState);

        private StateTransition(
                State<TEntity, TEnumTrigger> inFromState,
                State<TEntity, TEnumTrigger> inToState,
                TEnumTrigger inTransitionTrigger,
                Func<State<TEntity, TEnumTrigger>, bool> inTransitionCondition,
                bool hasTrigger)
        {
            Debug.Assert(hasTrigger || inTransitionCondition != null,
                "StateTransition: transitionTrigger or transitionCondition must be set");

            FromState = inFromState;
            ToState = inToState;
            TransitionTrigger = inTransitionTrigger;
            TransitionCondition = inTransitionCondition;
            HasTrigger = hasTrigger;
        }

        #region Factory Methods

        public static StateTransition<TEntity, TEnumTrigger> Generate(
                State<TEntity, TEnumTrigger> inFromState,
                State<TEntity, TEnumTrigger> inToState,
                TEnumTrigger inTransitionTrigger,
                Func<State<TEntity, TEnumTrigger>, bool> inTransitionCondition)
        {
            return new StateTransition<TEntity, TEnumTrigger>(
                inFromState, inToState, inTransitionTrigger, inTransitionCondition, true);
        }

        // 컨디션 없이 전이 트리거만 설정
        public static StateTransition<TEntity, TEnumTrigger> Generate(
                State<TEntity, TEnumTrigger> inFromState,
                State<TEntity, TEnumTrigger> inToState,
                TEnumTrigger inTransitionTrigger)
        {
            return new StateTransition<TEntity, TEnumTrigger>(
                inFromState, inToState, inTransitionTrigger, null, true);
        }

        // 전이 트리거 없이 컨디션만 설정
        public static StateTransition<TEntity, TEnumTrigger> Generate(
                State<TEntity, TEnumTrigger> inFromState,
                State<TEntity, TEnumTrigger> inToState,
                Func<State<TEntity, TEnumTrigger>, bool> inTransitionCondition)
        {
            return new StateTransition<TEntity, TEnumTrigger>(
                inFromState, inToState, default, inTransitionCondition, false);
        }

        #endregion
    }
}