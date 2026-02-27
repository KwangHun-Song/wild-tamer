# 1.1 프로젝트 구조 - 컨셉

## 목적

프로젝트의 에셋과 코드를 명확한 기준으로 분류하여 유지보수성과 확장성을 확보한다.

## 폴더 구조

```
Assets/
├── Docs/                # 문서 (컨벤션, 마일스톤, 시스템별 문서)
├── DOTween/             # DOTween 플러그인 (서드파티)
├── Modules/             # asmdef 기반 독립 모듈 (Base 시스템)
├── Scripts/             # 게임 로직 스크립트
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

### Modules

- Phase 1에서 구현하는 **베이스 시스템**들이 위치한다.
- 각 모듈은 **asmdef로 의존성을 분리**하여 독립적으로 관리한다.
- 데이터 관리, UI 프레임워크, 사운드, 오브젝트 풀링, 이벤트 시스템 등이 해당한다.

### Scripts

- **게임 고유 로직** 스크립트가 위치한다.
- Phase 2 이후의 코어/추가 시스템 코드가 해당한다.
- Modules에 의존할 수 있으나, Modules는 Scripts에 의존하지 않는다.

### Graphic

- 모든 **시각적 리소스**를 종류별로 분류하여 관리한다.

### Scenes

- 유니티 **씬 파일**을 관리한다.

### Resources

- `Resources.Load()`로 런타임에 접근해야 하는 에셋을 관리한다.
- 꼭 필요한 경우에만 사용하고, 가능하면 직접 참조나 Addressables를 우선한다.

## 의존성 방향

```
Scripts → Modules (허용)
Modules → Modules (허용, 단 순환 참조 금지)
Modules → Scripts (금지)
```
