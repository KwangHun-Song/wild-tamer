using System;
using Base;

namespace FiniteStateMachine
{
    public interface IStateChangeEvent<TEntity, TEnumTrigger> : IListener where TEnumTrigger : Enum
    {
        void OnStateChange(
            StateMachine<TEntity, TEnumTrigger> stateMachine,
            State<TEntity, TEnumTrigger> fromState,
            State<TEntity, TEnumTrigger> toState);
    }
}