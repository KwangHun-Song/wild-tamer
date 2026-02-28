# 1.1 프로젝트 구조 - 컨셉

## 목적

프로젝트의 에셋과 코드를 명확한 기준으로 분류하여 유지보수성과 확장성을 확보한다.

## 폴더 구조

```
Assets/
├── Docs/                # 문서 (컨벤션, 마일스톤, 시스템별 문서)
├── Plugins/             # 서드파티 라이브러리 (DOTween, UniTask 등)
├── Modules/             # asmdef 기반 독립 모듈 (Base, FiniteStateMachine 등)
├── Scripts/             # 게임 로직 스크립트
├── Prefabs/             # 프리팹
├── Graphic/             # 그래픽 리소스
│   ├── Sprites/         #   스프라이트
│   ├── Materials/       #   머티리얼
│   ├── Animations/      #   애니메이션 클립 및 컨트롤러
│   ├── VFX/             #   이펙트
│   └── UI/              #   UI 이미지 및 아틀라스
├── Scenes/              # 유니티 씬 파일
└── Resources/           # Resources.Load로 접근하는 에셋
```

## 폴더별 역할

### Plugins

- **서드파티 라이브러리**를 통합 관리한다. (DOTween, UniTask 등)
- UPM으로 설치되지 않는 에셋스토어 플러그인이나 직접 추가한 라이브러리가 해당한다.
- UPM 패키지는 `Packages/manifest.json`으로 관리하므로 이 폴더에 포함하지 않는다.

### Modules

- Phase 1에서 구현하는 **베이스 시스템**들이 위치한다.
- 각 모듈은 **asmdef로 의존성을 분리**하여 독립적으로 관리한다.
- 데이터 관리, UI 프레임워크, 사운드, 오브젝트 풀링, 이벤트 시스템 등이 해당한다.
- **Base**: 핵심 서비스(Facade), Notifier, PageChanger, PopupManager, Utility 등
- **FiniteStateMachine**: 상태 기계 프레임워크 (Base.Runtime에 의존)

#### 모듈 폴더 구조

각 모듈은 **UPM(Unity Package Manager) 패키지 레이아웃**을 따른다.

```
Modules/
└── ModuleName/
    ├── Runtime/                          # 런타임 코드
    │   ├── Scripts/                      #   스크립트 파일
    │   │   └── ModuleClass.cs
    │   └── ModuleName.Runtime.asmdef     #   런타임 asmdef
    ├── Editor/                           # 에디터 전용 코드 (필요시)
    │   └── ModuleName.Editor.asmdef      #   에디터 asmdef
    └── Tests/                            # 테스트 코드 (필요시)
        └── ModuleName.Tests.asmdef       #   테스트 asmdef
```

#### 모듈 규칙

- **Runtime** 폴더는 필수이며, 런타임 asmdef를 포함한다.
- **Editor** 폴더는 에디터 전용 기능이 있을 때만 생성한다.
- **Tests** 폴더는 테스트가 필요한 경우에만 생성한다.
- 스크립트는 `Runtime/Scripts/` 하위에 배치한다.
- asmdef 이름은 `{ModuleName}.Runtime`, `{ModuleName}.Editor`, `{ModuleName}.Tests` 형식을 따른다.

### Scripts

- **게임 고유 로직** 스크립트가 위치한다.
- Phase 2 이후의 코어/추가 시스템 코드가 해당한다.
- Modules에 의존할 수 있으나, Modules는 Scripts에 의존하지 않는다.
- 내부는 **시스템별 폴더**로 분류한다.

```
Scripts/
├── Player/              # 플레이어 이동, 입력
├── Squad/               # 군집 및 부대
├── Combat/              # 전투, 테이밍
├── Monster/             # 몬스터 AI, 스폰
├── Map/                 # 월드맵, Fog of War, 미니맵
└── UI/                  # 게임 UI (도감, 업그레이드 등)
```

### Prefabs

- 게임 오브젝트 **프리팹**을 관리한다.
- 시스템별로 하위 폴더를 나눈다. (e.g., `Prefabs/Monster/`, `Prefabs/UI/`)

### Graphic

- 모든 **시각적 리소스**를 종류별로 분류하여 관리한다.

### Scenes

- 유니티 **씬 파일**을 관리한다.

### Resources

- `Resources.Load()`로 런타임에 동적 접근해야 하는 에셋을 관리한다.
- 가능하면 직접 참조나 Addressables를 우선하고, 다음 경우에만 사용한다:
  - 문자열 키로 동적 로드해야 하는 설정 에셋 (e.g., DOTweenSettings)
  - 코드에서 프리팹 참조 없이 동적 생성해야 하는 경우

## 의존성 방향

```
Scripts → Modules (허용)
Modules → Modules (허용, 단 순환 참조 금지)
Modules → Scripts (금지)
```
