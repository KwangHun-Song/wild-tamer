using System;
using Base;
using NUnit.Framework;

namespace FiniteStateMachine.Tests
{
    public enum TestTrigger
    {
        None,
        Attack,
        Retreat,
        Heal
    }

    public class TestEntity
    {
        public string Name { get; set; } = "TestEntity";
    }

    public class IdleState : State<TestEntity, TestTrigger>
    {
        public int EnterCount { get; private set; }
        public int ExitCount { get; private set; }

        public override void OnEnter()
        {
            EnterCount++;
        }

        public override void OnExit()
        {
            ExitCount++;
        }
    }

    public class AttackState : State<TestEntity, TestTrigger>
    {
        public int EnterCount { get; private set; }
        public int ExitCount { get; private set; }

        public override void OnEnter()
        {
            EnterCount++;
        }

        public override void OnExit()
        {
            ExitCount++;
        }
    }

    public class HealState : State<TestEntity, TestTrigger>
    {
        public int EnterCount { get; private set; }

        public override void OnEnter()
        {
            EnterCount++;
        }
    }

    public class TestStateMachine : StateMachine<TestEntity, TestTrigger>
    {
        public IdleState Idle { get; } = new();
        public AttackState Attack { get; } = new();
        public HealState Heal { get; } = new();

        private readonly StateTransition<TestEntity, TestTrigger>[] transitions;

        public TestStateMachine(TestEntity owner, StateTransition<TestEntity, TestTrigger>[] transitions = null)
            : base(owner)
        {
            transitions = transitions ?? new[]
            {
                StateTransition<TestEntity, TestTrigger>.Generate(Idle, Attack, TestTrigger.Attack),
                StateTransition<TestEntity, TestTrigger>.Generate(Attack, Idle, TestTrigger.Retreat),
                StateTransition<TestEntity, TestTrigger>.Generate(Idle, Heal, TestTrigger.Heal),
            };
        }

        protected override State<TestEntity, TestTrigger> InitialState => Idle;

        protected override State<TestEntity, TestTrigger>[] States => new State<TestEntity, TestTrigger>[]
        {
            Idle, Attack, Heal
        };

        protected override StateTransition<TestEntity, TestTrigger>[] Transitions => transitions;
    }

    public class StateMachineTests
    {
        private TestEntity entity;
        private TestStateMachine fsm;

        [SetUp]
        public void SetUp()
        {
            entity = new TestEntity();
            fsm = new TestStateMachine(entity);
            fsm.SetUp();
        }

        [Test]
        public void SetUp_SetsInitialState()
        {
            Assert.AreEqual(fsm.Idle, fsm.CurrentState);
        }

        [Test]
        public void SetUp_CallsEnterOnInitialState()
        {
            Assert.AreEqual(1, fsm.Idle.EnterCount);
        }

        [Test]
        public void SetUp_SetsOwnerOnStates()
        {
            Assert.AreEqual(entity, fsm.Idle.Owner);
            Assert.AreEqual(entity, fsm.Attack.Owner);
        }

        [Test]
        public void ExecuteCommand_ValidTrigger_ChangesState()
        {
            fsm.ExecuteCommand(TestTrigger.Attack);

            Assert.AreEqual(fsm.Attack, fsm.CurrentState);
        }

        [Test]
        public void ExecuteCommand_ValidTrigger_ReturnsTrue()
        {
            bool result = fsm.ExecuteCommand(TestTrigger.Attack);

            Assert.IsTrue(result);
        }

        [Test]
        public void ExecuteCommand_InvalidTrigger_ReturnsFalse()
        {
            bool result = fsm.ExecuteCommand(TestTrigger.Retreat);

            Assert.IsFalse(result);
        }

        [Test]
        public void ExecuteCommand_InvalidTrigger_DoesNotChangeState()
        {
            fsm.ExecuteCommand(TestTrigger.Retreat);

            Assert.AreEqual(fsm.Idle, fsm.CurrentState);
        }

        [Test]
        public void ExecuteCommand_CallsExitOnPreviousState()
        {
            fsm.ExecuteCommand(TestTrigger.Attack);

            Assert.AreEqual(1, fsm.Idle.ExitCount);
        }

        [Test]
        public void ExecuteCommand_CallsEnterOnNewState()
        {
            fsm.ExecuteCommand(TestTrigger.Attack);

            Assert.AreEqual(1, fsm.Attack.EnterCount);
        }

        [Test]
        public void ExecuteCommand_SequentialTransitions_Work()
        {
            fsm.ExecuteCommand(TestTrigger.Attack);
            fsm.ExecuteCommand(TestTrigger.Retreat);

            Assert.AreEqual(fsm.Idle, fsm.CurrentState);
            Assert.AreEqual(2, fsm.Idle.EnterCount);
        }

        #region Trigger-Only 전이 테스트

        [Test]
        public void TriggerOnly_MatchingTrigger_TransitionsState()
        {
            // Trigger만 설정된 전이: ExecuteCommand로만 전이됨
            var transitions = new[]
            {
                StateTransition<TestEntity, TestTrigger>.Generate(
                    fsm.Idle, fsm.Attack, TestTrigger.Attack),
            };

            var fsm = new TestStateMachine(new TestEntity(), transitions);
            fsm.SetUp();

            bool result = fsm.ExecuteCommand(TestTrigger.Attack);

            Assert.IsTrue(result);
            Assert.AreEqual(fsm.Attack, fsm.CurrentState);
        }

        [Test]
        public void TriggerOnly_TryTransition_DoesNotTransition()
        {
            // Trigger-only 전이는 Condition이 없으므로 TryTransition에서 전이되지 않음
            var transitions = new[]
            {
                StateTransition<TestEntity, TestTrigger>.Generate(
                    fsm.Idle, fsm.Attack, TestTrigger.Attack),
            };

            var fsm = new TestStateMachine(new TestEntity(), transitions);
            fsm.SetUp();

            bool result = fsm.TryTransition();

            Assert.IsFalse(result);
            Assert.AreEqual(fsm.Idle, fsm.CurrentState);
        }

        [Test]
        public void TriggerOnly_WrongTrigger_DoesNotTransition()
        {
            var transitions = new[]
            {
                StateTransition<TestEntity, TestTrigger>.Generate(
                    fsm.Idle, fsm.Attack, TestTrigger.Attack),
            };

            var fsm = new TestStateMachine(new TestEntity(), transitions);
            fsm.SetUp();

            bool result = fsm.ExecuteCommand(TestTrigger.Heal);

            Assert.IsFalse(result);
            Assert.AreEqual(fsm.Idle, fsm.CurrentState);
        }

        #endregion

        #region Condition-Only 전이 테스트

        [Test]
        public void ConditionOnly_TrueCondition_TransitionsViaTryTransition()
        {
            // Condition만 설정된 전이: TryTransition/Update로만 전이됨
            var transitions = new[]
            {
                StateTransition<TestEntity, TestTrigger>.Generate(
                    fsm.Idle, fsm.Attack, _ => true),
            };

            var fsm = new TestStateMachine(new TestEntity(), transitions);
            fsm.SetUp();

            bool result = fsm.TryTransition();

            Assert.IsTrue(result);
            Assert.AreEqual(fsm.Attack, fsm.CurrentState);
        }

        [Test]
        public void ConditionOnly_FalseCondition_DoesNotTransition()
        {
            var transitions = new[]
            {
                StateTransition<TestEntity, TestTrigger>.Generate(
                    fsm.Idle, fsm.Attack, _ => false),
            };

            var fsm = new TestStateMachine(new TestEntity(), transitions);
            fsm.SetUp();

            bool result = fsm.TryTransition();

            Assert.IsFalse(result);
            Assert.AreEqual(fsm.Idle, fsm.CurrentState);
        }

        [Test]
        public void ConditionOnly_NotTriggeredByExecuteCommand()
        {
            // Condition-only 전이는 HasTrigger=false이므로 ExecuteCommand로 매칭되지 않음
            var transitions = new[]
            {
                StateTransition<TestEntity, TestTrigger>.Generate(
                    fsm.Idle, fsm.Attack, _ => true),
            };

            var fsm = new TestStateMachine(new TestEntity(), transitions);
            fsm.SetUp();

            bool result = fsm.ExecuteCommand(TestTrigger.None);

            Assert.IsFalse(result);
            Assert.AreEqual(fsm.Idle, fsm.CurrentState);
        }

        [Test]
        public void ConditionOnly_DynamicCondition_TransitionsWhenBecomesTrue()
        {
            bool canTransition = false;

            var transitions = new[]
            {
                StateTransition<TestEntity, TestTrigger>.Generate(
                    fsm.Idle, fsm.Attack, _ => canTransition),
            };

            var fsm = new TestStateMachine(new TestEntity(), transitions);
            fsm.SetUp();

            // 조건 false일 때
            Assert.IsFalse(fsm.TryTransition());
            Assert.AreEqual(fsm.Idle, fsm.CurrentState);

            // 조건 true로 변경
            canTransition = true;
            Assert.IsTrue(fsm.TryTransition());
            Assert.AreEqual(fsm.Attack, fsm.CurrentState);
        }

        #endregion

        #region Trigger + Condition 혼합 전이 테스트

        [Test]
        public void Mixed_TriggerAndConditionTrue_TransitionsViaExecuteCommand()
        {
            // Trigger + Condition 전이: ExecuteCommand로 트리거 매칭 시 전이됨
            // (혼합 전이에서 ExecuteCommand는 Trigger만 체크)
            var transitions = new[]
            {
                StateTransition<TestEntity, TestTrigger>.Generate(
                    fsm.Idle, fsm.Attack, TestTrigger.Attack, _ => true),
            };

            var fsm = new TestStateMachine(new TestEntity(), transitions);
            fsm.SetUp();

            bool result = fsm.ExecuteCommand(TestTrigger.Attack);

            Assert.IsTrue(result);
            Assert.AreEqual(fsm.Attack, fsm.CurrentState);
        }

        [Test]
        public void Mixed_TriggerAndConditionTrue_TransitionsViaTryTransition()
        {
            // 혼합 전이도 Condition이 있으므로 TryTransition으로 전이 가능
            var transitions = new[]
            {
                StateTransition<TestEntity, TestTrigger>.Generate(
                    fsm.Idle, fsm.Attack, TestTrigger.Attack, _ => true),
            };

            var fsm = new TestStateMachine(new TestEntity(), transitions);
            fsm.SetUp();

            bool result = fsm.TryTransition();

            Assert.IsTrue(result);
            Assert.AreEqual(fsm.Attack, fsm.CurrentState);
        }

        [Test]
        public void Mixed_TriggerAndConditionFalse_DoesNotTransitionViaTryTransition()
        {
            // Condition이 false면 TryTransition으로 전이되지 않음
            var transitions = new[]
            {
                StateTransition<TestEntity, TestTrigger>.Generate(
                    fsm.Idle, fsm.Attack, TestTrigger.Attack, _ => false),
            };

            var fsm = new TestStateMachine(new TestEntity(), transitions);
            fsm.SetUp();

            bool result = fsm.TryTransition();

            Assert.IsFalse(result);
            Assert.AreEqual(fsm.Idle, fsm.CurrentState);
        }

        [Test]
        public void Mixed_MultipleTransitions_FirstMatchingWins()
        {
            // 여러 전이가 있을 때, 먼저 조건을 만족하는 전이가 실행됨
            var transitions = new[]
            {
                StateTransition<TestEntity, TestTrigger>.Generate(
                    fsm.Idle, fsm.Attack, _ => false),
                StateTransition<TestEntity, TestTrigger>.Generate(
                    fsm.Idle, fsm.Heal, _ => true),
                StateTransition<TestEntity, TestTrigger>.Generate(
                    fsm.Idle, fsm.Attack, _ => true),
            };

            var fsm = new TestStateMachine(new TestEntity(), transitions);
            fsm.SetUp();

            fsm.TryTransition();

            // 첫 번째(false) 건너뛰고, 두 번째(Heal, true)가 매칭
            Assert.AreEqual(fsm.Heal, fsm.CurrentState);
        }

        [Test]
        public void Update_CallsTryTransition()
        {
            var transitions = new[]
            {
                StateTransition<TestEntity, TestTrigger>.Generate(
                    fsm.Idle, fsm.Attack, _ => true),
            };

            var fsm = new TestStateMachine(new TestEntity(), transitions);
            fsm.SetUp();

            fsm.Update();

            Assert.AreEqual(fsm.Attack, fsm.CurrentState);
        }

        #endregion

        [Test]
        public void StateChangeEvent_NotifiesListeners()
        {
            var listener = new TestStateChangeListener();
            fsm.Notifier.Subscribe(listener);

            fsm.ExecuteCommand(TestTrigger.Attack);

            Assert.IsTrue(listener.WasCalled);
            Assert.AreEqual(fsm.Idle, listener.FromState);
            Assert.AreEqual(fsm.Attack, listener.ToState);
        }

        [Test]
        public void Owner_IsAccessibleFromState()
        {
            Assert.AreEqual(entity, fsm.CurrentState.Owner);
            Assert.AreEqual("TestEntity", fsm.CurrentState.Owner.Name);
        }

        private class TestStateChangeListener : IStateChangeEvent<TestEntity, TestTrigger>
        {
            public bool WasCalled { get; private set; }
            public State<TestEntity, TestTrigger> FromState { get; private set; }
            public State<TestEntity, TestTrigger> ToState { get; private set; }

            public void OnStateChange(
                StateMachine<TestEntity, TestTrigger> stateMachine,
                State<TestEntity, TestTrigger> fromState,
                State<TestEntity, TestTrigger> toState)
            {
                WasCalled = true;
                FromState = fromState;
                ToState = toState;
            }
        }
    }
}
