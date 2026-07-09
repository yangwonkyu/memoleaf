# Content Flowcharts

이 문서는 세 콘텐츠의 구현 흐름을 채용자가 빠르게 확인할 수 있도록 정리한 것입니다.

원본 회사 코드는 포함하지 않고, 공개 가능한 수준의 구조 설명과 흐름도만 제공합니다.

## Hidden Catch

### Gameplay Flow

```mermaid
flowchart TD
    A[Round start] --> B[Calculate difficulty parameters]
    B --> C[Select target animals]
    C --> D[Create question UI]
    D --> E[Create answer silhouette UI]
    E --> F[Spawn target, similar, and distractor animals]
    F --> G[Enable timer and drag input]
    G --> H[Player drags animal]
    H --> I[Drop on answer zone]
    I --> J{Target list contains animal type?}
    J -->|Yes| K[Play correct sound and punch feedback]
    K --> L[Reveal answer silhouette]
    L --> M[Register correct drop]
    M --> N{Remaining target count is zero?}
    N -->|No| G
    N -->|Yes| O[Round clear / next round]
    J -->|No| P[Register wrong drop]
    P --> Q[Return animal to original position]
    Q --> G
```

### Responsibility Flow

```mermaid
flowchart LR
    A[HiddenCatchGameManager] -->|difficulty params| B[AnimalSpawner]
    B -->|spawned animals| C[DraggableAnimal]
    C -->|drop event| D[AnswerDropZone]
    D -->|correct/wrong result| A
    D -->|reveal request| B
    A -->|idle timeout| C
```

## Shadow Kok

### Gameplay Flow

```mermaid
flowchart TD
    A[Build round] --> B[Select animals]
    B --> C[Assign answer colors]
    C --> D[Create animal slots]
    D --> E[Create answer and distractor color palette]
    E --> F[Preview colored animals]
    F --> G[Switch slots to shadow mode]
    G --> H[Show current quiz silhouette]
    H --> I[Player drags color piece]
    I --> J[Drop on slot]
    J --> K{Dropped color matches current answer?}
    K -->|Yes| L[Submit correct answer]
    L --> M[Play shader spread effect]
    M --> N[Move colored quiz image to slot]
    N --> O{More quiz targets?}
    O -->|Yes| H
    O -->|No| P[Build next round]
    K -->|No| Q[Submit wrong answer]
    Q --> H
```

### Responsibility Flow

```mermaid
flowchart LR
    A[ShadowGameManager] -->|round data| B[DropSlot]
    A -->|palette colors| C[ColorPiece]
    C -->|drag clone| B
    B -->|piece color| A
    A -->|shader params| D[ShadowSpread material]
    A -->|idle timeout| C
```

## Pair Match

### Gameplay Flow

```mermaid
flowchart TD
    A[Start round] --> B[Apply stage layout]
    B --> C[Shuffle pair ids]
    C --> D[Create card objects and empty slots]
    D --> E[Preview all fronts]
    E --> F[Flip all cards back]
    F --> G[Enable card input]
    G --> H[Select first card]
    H --> I[Select second card]
    I --> J{Card ids match?}
    J -->|Yes| K[Mark pair matched]
    K --> L[Play particle and scale feedback]
    L --> M[Add score/combo/result]
    M --> N{All pairs matched?}
    N -->|No| G
    N -->|Yes| O[Report round clear]
    J -->|No| P[Wait briefly]
    P --> Q[Flip cards back]
    Q --> R[Reset combo/result state]
    R --> G
```

### Responsibility Flow

```mermaid
flowchart LR
    A[StageController] -->|stage index| B[CardManager]
    B -->|card data| C[CardFlip]
    C -->|click event| B
    B -->|correct/wrong| D[ScoreManager]
    B -->|result events| E[CardGameResultCollector]
    A -->|round transition| B
```

