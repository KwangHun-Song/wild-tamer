# FiniteStateMachine Module

## 개요
`FiniteStateMachine`은 제네릭 FSM 프레임워크를 제공합니다.

## 설치
1. `Assets/Modules/FiniteStateMachine` 포함
2. 상태(`State`)와 전이(`StateTransition`)를 구현한 파생 클래스를 작성

## 사용 예시
```csharp
public class MyMachine : StateMachine<MyContext, MyTrigger> {
    protected override State<MyContext, MyTrigger> InitialState => idle;
    protected override State<MyContext, MyTrigger>[] States => states;
    protected override StateTransition<MyContext, MyTrigger>[] Transitions => transitions;
}

machine.SetUp();
machine.Update();
machine.ExecuteCommand(MyTrigger.Fire);
```

## 특징
- 조건 기반 자동 전이(`TryTransition`)
- 트리거 기반 전이(`ExecuteCommand`)
- 상태 변경 이벤트(`IStateChangeEvent`) 지원
