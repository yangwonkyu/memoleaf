# Content Flowcharts

이 문서는 세 콘텐츠의 구현 흐름을 채용자가 빠르게 확인할 수 있도록 정리한 것입니다.

원본 회사 코드는 포함하지 않고, 공개 가능한 수준의 구조 설명과 흐름도만 제공합니다.

## Hidden Catch

### Gameplay Flow

```mermaid
flowchart TD
    A[라운드 시작] --> B[난이도별 파라미터 계산]
    B --> C[목표 동물 선정]
    C --> D[문제 UI 생성]
    D --> E[정답 실루엣 UI 생성]
    E --> F[목표/유사/오답 동물 스폰]
    F --> G[타이머 및 드래그 입력 활성화]
    G --> H[유저가 동물 드래그]
    H --> I[정답 영역에 드롭]
    I --> J{목표 목록에 포함된 동물인가?}
    J -->|정답| K[정답 사운드와 펀치 피드백]
    K --> L[실루엣 해금 연출]
    L --> M[정답 등록]
    M --> N{남은 목표 수가 0인가?}
    N -->|아니오| G
    N -->|예| O[라운드 클리어 / 다음 라운드]
    J -->|오답| P[오답 등록]
    P --> Q[동물을 원래 위치로 복귀]
    Q --> G
```

### Responsibility Flow

```mermaid
flowchart LR
    A[HiddenCatchGameManager] -->|난이도 파라미터 전달| B[AnimalSpawner]
    B -->|동물 스폰 및 문제 UI 생성| C[DraggableAnimal]
    C -->|드롭 이벤트| D[AnswerDropZone]
    D -->|정답/오답 결과 전달| A
    D -->|실루엣 해금 요청| B
    A -->|입력 대기 시간 초과| C
```

## Shadow Kok

### Gameplay Flow

```mermaid
flowchart TD
    A[라운드 생성] --> B[동물 선정]
    B --> C[정답 색상 배정]
    C --> D[동물 슬롯 생성]
    D --> E[정답/오답 색상 팔레트 생성]
    E --> F[색상이 보이는 프리뷰]
    F --> G[슬롯을 그림자 모드로 전환]
    G --> H[현재 문제 실루엣 표시]
    H --> I[유저가 색상 조각 드래그]
    I --> J[슬롯에 드롭]
    J --> K{현재 정답 색상과 일치하는가?}
    K -->|정답| L[정답 결과 전달]
    L --> M[직접 제작한 색상 확산 셰이더 재생]
    M --> N[색상 적용된 문제 이미지를 슬롯으로 이동]
    N --> O{남은 문제가 있는가?}
    O -->|예| H
    O -->|아니오| P[다음 라운드 생성]
    K -->|오답| Q[오답 결과 전달]
    Q --> H
```

### Responsibility Flow

```mermaid
flowchart LR
    A[ShadowGameManager] -->|라운드/정답 데이터| B[DropSlot]
    A -->|팔레트 색상 생성| C[ColorPiece]
    C -->|드래그 복제 오브젝트| B
    B -->|드롭된 색상 전달| A
    A -->|셰이더 파라미터 제어| D[ShadowSpread 머티리얼]
    A -->|입력 대기 시간 초과| C
```

## Pair Match

### Gameplay Flow

```mermaid
flowchart TD
    A[라운드 시작] --> B[스테이지 레이아웃 적용]
    B --> C[카드 짝 ID 셔플]
    C --> D[카드 오브젝트와 빈 슬롯 생성]
    D --> E[모든 카드 앞면 프리뷰]
    E --> F[모든 카드 뒷면으로 전환]
    F --> G[카드 입력 활성화]
    G --> H[첫 번째 카드 선택]
    H --> I[두 번째 카드 선택]
    I --> J{카드 ID가 같은가?}
    J -->|정답| K[카드 매칭 상태 고정]
    K --> L[직접 제작한 매치 파티클과 스케일 피드백]
    L --> M[점수/콤보/결과 데이터 반영]
    M --> N{모든 짝을 맞췄는가?}
    N -->|아니오| G
    N -->|예| O[라운드 클리어 보고]
    J -->|오답| P[짧은 대기]
    P --> Q[두 카드 뒷면으로 복귀]
    Q --> R[콤보/선택 상태 초기화]
    R --> G
```

### Responsibility Flow

```mermaid
flowchart LR
    A[StageController] -->|스테이지 인덱스 전달| B[CardManager]
    B -->|카드 데이터 설정| C[CardFlip]
    C -->|클릭 이벤트| B
    B -->|정답/오답 결과| D[ScoreManager]
    B -->|결과 이벤트 수집| E[CardGameResultCollector]
    A -->|라운드 전환| B
```
