# 작업 컨벤션

## 시스템 구현 워크플로우

각 시스템은 아래 5단계를 순서대로 진행한다. 각 단계의 산출물은 해당 시스템 폴더에 저장한다.

### 1단계: 컨셉 문서 (`concept.md`)

시스템이 **무엇을 하는지** 정의한다.

- 시스템의 목적과 역할
- 유저 관점의 동작 설명
- 레퍼런스 자료 (이미지, 영상, 타 게임 사례 등)
- 다른 시스템과의 관계

### 2단계: 설계 문서 (`design.md`)

시스템을 **어떻게 구조화할지** 설계한다.

- 핵심 클래스 및 인터페이스 구조
- 클래스 간 의존 관계 및 데이터 흐름
- 사용할 디자인 패턴 (있다면)
- 외부 시스템과의 인터페이스 정의

### 3단계: 구현 계획 문서 (`implementation_plan.md`)

설계를 바탕으로 **구체적인 구현 순서**를 정한다.

- 구현할 클래스/파일 목록
- 작업 순서 및 의존 관계
- 예상되는 기술적 난이도 및 리스크
- 테스트 방법

### 4단계: 구현

계획에 따라 **코드를 작성**한다.

- 코딩 컨벤션(`csharp_coding_convention.md`)을 준수한다
- 커밋 컨벤션(`commit_convention.md`)에 따라 작업 단위별로 커밋한다
- 구현 중 설계 변경이 필요하면 설계 문서를 먼저 업데이트한다

### 5단계: TRD (`trd.md`)

구현 완료 후 **기술 참고 문서(Technical Reference Document)**를 작성한다.

- 최종 구현된 클래스 구조 및 역할 설명
- 핵심 알고리즘 및 로직 설명
- 설계 대비 변경된 사항과 그 사유
- 성능 고려 사항 및 최적화 내용
- 알려진 제한 사항 및 향후 개선 가능 사항

## 문서 폴더 구조

```
Assets/Docs/
├── Convention/                        # 컨벤션 문서
├── System/                            # 시스템별 문서
│   ├── Phase1_Base/                   #   프로젝트 베이스
│   │   ├── 1.1_project_structure/
│   │   ├── 1.2_data_management/
│   │   ├── 1.3_ui_framework/
│   │   ├── 1.4_sound/
│   │   ├── 1.5_object_pooling/
│   │   ├── 1.6_event_system/
│   │   └── 1.7_resource_loading/
│   ├── Phase2_Core/                   #   코어 시스템
│   │   ├── 2.1_player_movement/
│   │   ├── 2.2_squad_system/
│   │   ├── 2.3_world_map/
│   │   ├── 2.4_monster/
│   │   ├── 2.5_auto_combat/
│   │   ├── 2.6_taming/
│   │   ├── 2.7_combat_vfx/
│   │   ├── 2.8_fog_of_war/
│   │   └── 2.9_minimap/
│   ├── Phase3_Additional/             #   추가 시스템
│   │   ├── 3.1_animal_codex/
│   │   ├── 3.2_roguelike_upgrade/
│   │   └── 3.3_game_flow/
│   └── Phase4_Polish/                 #   폴리싱 및 제출
│       ├── 4.1_balance/
│       └── 4.2_submission/
├── concept.md                         # 게임 컨셉 문서
└── milestone.md                       # 마일스톤
```

각 시스템 폴더 내부에 워크플로우에 따라 문서가 쌓인다:

```
예) 2.2_squad_system/
├── concept.md
├── design.md
├── implementation_plan.md
└── trd.md
```

## 문서 파일명 규칙

- 문서 파일명은 **snake_case**를 사용한다.
- C# 스크립트 파일명은 **클래스명과 일치**시킨다. (PascalCase)
