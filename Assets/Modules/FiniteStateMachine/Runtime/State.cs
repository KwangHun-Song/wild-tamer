using System;

namespace FiniteStateMachine
{
    public abstract class State<TEntity, TEnumTrigger> : IEquatable<State<TEntity, TEnumTrigger>> where TEnumTrigger : Enum
    {
        public TEntity Owner { get; private set; }
        public StateMachine<TEntity, TEnumTrigger> StateMachine { get; private set; }

        public void SetUp(StateMachine<TEntity, TEnumTrigger> inStateMachine, TEntity inOwner)
        {
            StateMachine = inStateMachine;
            Owner = inOwner;

            OnSetUp();
        }

        protected virtual void OnSetUp() { }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual bool OnReceiveCommand(int inCommand, object inData) { return false; }

        #region IEquatable

        // IEquatable 비교는 타입이 같은지 비교한다. 상태 타입당 하나의 인스턴스를 전제한다.
        public bool Equals(State<TEntity, TEnumTrigger> other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return this.GetType() == other.GetType();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as State<TEntity, TEnumTrigger>);
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }

        #endregion
    }
}