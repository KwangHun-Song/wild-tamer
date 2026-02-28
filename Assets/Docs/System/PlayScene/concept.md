# PlayScene 플로우 시스템 - 컨셉

## 개요

PlayScene 진입 시 초기화, 리소스 로딩, 페이지 진입 등 일련의 플로우를 FSM 기반 상태 트리로 관리한다. 각 상태(State)는 프리팹으로 만들어지며, 트리 노드 형식으로 구성되어 DFS 순서로 재귀 실행된다.

## 핵심 개념

### 1. 상태 트리 (State Tree)

상태들은 선형 순서가 아닌 **트리 구조**로 구성된다. 각 노드는 자식 노드들을 가질 수 있으며, 실행 시 DFS(깊이 우선 탐색) 방식으로 재귀적으로 실행된다.

```
[Root]
├── [Initialize]
│   ├── [LoadConfig]
│   └── [InitServices]
├── [LoadResources]
│   ├── [LoadUI]
│   └── [LoadGameData]
└── [EnterGame]
    └── [ShowMainPage]
```

- 각 노드는 자신의 작업을 수행한 후 자식 노드들을 순서대로 실행한다.
- 리프 노드는 자식이 없으므로 자신의 작업만 수행하고 완료된다.
- 모든 자식 실행이 끝나면 부모 노드도 완료된다.

### 2. 상태 프리팹

각 State는 **프리팹**으로 관리된다.

- 상태의 추가/제거/순서 변경이 프리팹 수정만으로 가능하다.
- 씬 파일을 수정하지 않고도 플로우를 변경할 수 있다.
- 프리팹 내부에 자식 State 프리팹을 배치하여 트리 구조를 형성한다.

### 3. SceneLauncher

씬 진입 시 가장 먼저 실행되는 진입점 클래스이다.

- 씬의 루트 State를 참조하고, 해당 State에 진입시킨다.
- 이후 상태 트리가 DFS로 재귀 실행되며 전체 플로우가 진행된다.

### 4. 플로우 실행 방식

```
SceneLauncher.Start()
  → RootState.Execute()
    → 자신의 OnExecute() 실행
    → 자식[0].Execute() (재귀)
    → 자식[1].Execute() (재귀)
    → ...
    → 모든 자식 완료 → 자신도 완료
```

각 State의 Execute는 비동기(UniTask)로 동작하여, 로딩이나 연출 등 시간이 필요한 작업을 자연스럽게 처리한다.

## PlayScene 하이에라키

```
PlayScene
├── SceneLauncher          ← 씬 진입점
├── States                 ← 상태 트리 프리팹 루트
│   └── (State 프리팹들이 여기에 배치)
├── Cameras
│   ├── UICamera           ← UI 전용 카메라 (상위 렌더 순서)
│   └── MainCamera         ← 기본 카메라
├── PageRoot               ← 페이지 인스턴스 생성 위치
└── PopupRoot              ← 팝업 인스턴스 생성 위치
```

### 카메라 구성

- **MainCamera**: 게임 월드를 렌더링하는 기본 카메라
- **UICamera**: UI를 렌더링하는 카메라. MainCamera보다 상위에 렌더링되어 UI가 항상 게임 위에 표시된다.

### PageRoot / PopupRoot

- PageChanger가 페이지를 생성할 때 PageRoot 하위에 인스턴스를 배치한다.
- PopupManager가 팝업을 생성할 때 PopupRoot 하위에 인스턴스를 배치한다.
- 두 루트를 분리하여 페이지와 팝업의 렌더 순서를 독립적으로 관리한다.
