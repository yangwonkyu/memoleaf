# Core Implementation Notes

> 이전 구현을 설명하는 재작성 예제·흐름 자료입니다. 현재 게임별 적용 범위는 [README](../README.md)와 최신 사례 문서를 기준으로 합니다. 주문·카트 방식 담아담아와 최근 기록 기반 난이도 예시는 현재 짐싸기 게임이나 명세 적용 게임 전체에 적용되는 규칙이 아닙니다.

MemoLeaf 콘텐츠를 개발하며 반복해서 다룬 핵심 문제를 공개 가능한 형태로 정리했습니다.

> 아래 코드는 실제 제품 코드를 복사한 것이 아닙니다. 회사 프로젝트의 원본 코드·내부 데이터·서비스 주소를 공개하지 않기 위해 클래스명, 데이터 구조와 세부 수치를 바꾸고 핵심 설계만 독립적인 예제로 재작성했습니다.

## Evidence Map

| 역량 | 실제 적용 범위 | 공개 예제에서 확인할 부분 |
| --- | --- | --- |
| 공통 게임 프레임워크 | 타이머, 점수·콤보, 입력 잠금, 결과 수집, 적응형 난이도 | 최근 플레이 기록을 이용한 난이도 승강 정책 |
| 입력 상태 관리 | 선긋기, 드래그 앤 드롭, 연속 터치, Go/No-Go | 포인터 시작·진입·해제와 순서 오류 판정 |
| 런타임 서비스 연동 | 테마 조회, 원격 이미지 다운로드, 결과 JSON 전송 | 로컬 폴백을 먼저 적용하고 원격 에셋을 교체하는 흐름 |
| 데이터 기반 제작 | ScriptableObject 콘텐츠 정의, 반복 에셋 생성 | 폴더 구조를 데이터 에셋으로 변환하는 Editor 도구 |

## 1. Adaptive Difficulty Policy

### 문제

어르신 대상 콘텐츠에서는 한 번의 정답이나 오답만으로 난이도를 바꾸면 플레이 흐름이 불안정해집니다. 정확도뿐 아니라 반응시간도 함께 보되, 연속 오답에는 빠르게 대응할 필요가 있었습니다.

### 설계

- 최근 N회의 정오답과 반응시간만 유지합니다.
- 상승은 충분한 표본, 목표 정확도, 목표 반응시간을 모두 만족할 때만 허용합니다.
- 하강은 최근 짧은 구간의 반복 오답으로 빠르게 판단합니다.
- 난이도가 바뀌면 표본을 초기화해 같은 기록으로 연속 승강하지 않도록 합니다.

```csharp
using System.Collections.Generic;
using System.Linq;

public enum DifficultyStep { Seed, Sprout, Leaf, Tree }

public sealed class AdaptiveDifficultyPolicy
{
    private readonly Queue<bool> answers = new Queue<bool>();
    private readonly Queue<float> responseTimes = new Queue<float>();

    private const int EvaluationWindow = 5;
    private const int RecentMissWindow = 3;
    private const int MissesToDrop = 2;
    private const float AccuracyToRaise = 0.8f;

    public DifficultyStep Evaluate(
        DifficultyStep current,
        bool correct,
        float responseSeconds,
        float targetSeconds)
    {
        EnqueueBounded(answers, correct, EvaluationWindow);
        EnqueueBounded(responseTimes, responseSeconds, EvaluationWindow);

        int recentMisses = answers
            .Reverse()
            .Take(RecentMissWindow)
            .Count(answer => !answer);

        if (recentMisses >= MissesToDrop && current > DifficultyStep.Seed)
        {
            ClearSamples();
            return (DifficultyStep)((int)current - 1);
        }

        if (answers.Count < EvaluationWindow || current >= DifficultyStep.Tree)
            return current;

        float accuracy = answers.Count(answer => answer) / (float)answers.Count;
        float averageTime = responseTimes.Average();

        if (accuracy >= AccuracyToRaise && averageTime <= targetSeconds)
        {
            ClearSamples();
            return (DifficultyStep)((int)current + 1);
        }

        return current;
    }

    private static void EnqueueBounded<T>(Queue<T> queue, T value, int capacity)
    {
        queue.Enqueue(value);
        while (queue.Count > capacity)
            queue.Dequeue();
    }

    private void ClearSamples()
    {
        answers.Clear();
        responseTimes.Clear();
    }
}
```

### 검토 포인트

정답률만 사용하는 방식보다 느린 추측성 입력을 구분할 수 있고, 짧은 오답 구간에는 빠르게 난이도를 낮춰 이탈을 줄일 수 있습니다. 실제 게임에서는 판정 시점의 난이도로 라운드 결과를 먼저 기록한 뒤 다음 라운드부터 변경된 난이도를 적용했습니다.

## 2. Ordered Path Input State

### 문제

선긋기게임은 단순히 마지막 점을 눌렀는지만 확인하면 중간 점을 건너뛰거나 선을 끊어서 완성하는 입력을 구분할 수 없습니다. 드래그 도중 이전 점을 다시 스치는 정상 입력과 잘못된 점에 접근하는 오류도 분리해야 했습니다.

### 설계

- `Idle → Drawing → Locked` 상태로 입력 가능 시점을 제한합니다.
- 시작점에서만 드래그를 시작합니다.
- 다음 순서의 점과 직전 점만 정상 접촉으로 인정합니다.
- 다른 점에 일정 거리 안으로 접근하면 즉시 오류로 종료합니다.
- 포인터 해제 시 임시 선을 정리해 다음 입력과 섞이지 않게 합니다.

```csharp
using UnityEngine;

public sealed class OrderedPathValidator
{
    private enum InputState { Idle, Drawing, Locked }

    private InputState state = InputState.Idle;
    private int expectedIndex;
    private int pointCount;

    public void Begin(int startIndex, int totalPoints)
    {
        if (state == InputState.Locked || startIndex != 0)
            return;

        pointCount = totalPoints;
        expectedIndex = 1;
        state = InputState.Drawing;
    }

    public PathResult EnterPoint(int pointIndex)
    {
        if (state != InputState.Drawing)
            return PathResult.Ignored;

        int previousIndex = expectedIndex - 1;
        if (pointIndex == previousIndex)
            return PathResult.Continue;

        if (pointIndex != expectedIndex)
        {
            state = InputState.Idle;
            return PathResult.WrongOrder;
        }

        expectedIndex++;
        if (expectedIndex >= pointCount)
        {
            state = InputState.Locked;
            return PathResult.Completed;
        }

        return PathResult.Continue;
    }

    public bool IsWrongPointTooClose(
        int hoveredIndex,
        Vector2 pointer,
        Vector2 pointPosition,
        float errorRadius)
    {
        if (state != InputState.Drawing)
            return false;

        int previousIndex = expectedIndex - 1;
        bool isAllowed = hoveredIndex == expectedIndex || hoveredIndex == previousIndex;
        return !isAllowed && Vector2.Distance(pointer, pointPosition) <= errorRadius;
    }

    public void Release()
    {
        if (state == InputState.Drawing)
            state = InputState.Idle;
    }
}

public enum PathResult { Ignored, Continue, WrongOrder, Completed }
```

### 검토 포인트

입력 이벤트와 게임 판정을 분리하면 마우스와 터치 입력을 같은 규칙으로 처리할 수 있습니다. 실제 콘텐츠에서는 절차적으로 생성한 다각형·별 좌표, 임시 LineRenderer, 유휴 힌트와 결합해 라운드를 구성했습니다.

## 3. Runtime Theme Loading with Local Fallback

### 문제

담아담아는 서버가 선택한 테마와 원격 이미지를 게임에 적용하지만, 네트워크 완료를 기다리는 동안 빈 화면이 보이거나 일부 다운로드 실패 때문에 전체 플레이가 막혀서는 안 됐습니다.

### 설계

- 서버에서 받은 테마를 기준으로 로컬 폴백을 즉시 적용합니다.
- 배경·진열대·아이템 다운로드를 각각 시작하고 완료 수를 추적합니다.
- 성공한 리소스만 런타임 Sprite로 교체합니다.
- 실패한 항목은 이미 적용된 로컬 리소스를 유지합니다.

```csharp
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public sealed class RuntimeThemeLoader : MonoBehaviour
{
    public IEnumerator ApplyTheme(ThemePayload payload, Action onReady)
    {
        ApplyLocalFallback(payload.themeKey);

        int pending = 2;
        StartCoroutine(DownloadSprite(payload.backgroundUrl, sprite =>
        {
            if (sprite != null) ApplyBackground(sprite);
            pending--;
        }));

        StartCoroutine(DownloadSprite(payload.shelfUrl, sprite =>
        {
            if (sprite != null) ApplyShelf(sprite);
            pending--;
        }));

        yield return new WaitUntil(() => pending == 0);
        onReady?.Invoke();
    }

    private IEnumerator DownloadSprite(string url, Action<Sprite> completed)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            completed(null);
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            request.timeout = 20;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                completed(null);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
            completed(sprite);
        }
    }

    private void ApplyLocalFallback(string themeKey) { /* Resources 기반 즉시 적용 */ }
    private void ApplyBackground(Sprite sprite) { /* 현재 UI 교체 */ }
    private void ApplyShelf(Sprite sprite) { /* 현재 UI 교체 */ }
}

[Serializable]
public sealed class ThemePayload
{
    public string themeKey;
    public string backgroundUrl;
    public string shelfUrl;
}
```

### 검토 포인트

네트워크가 정상일 때는 동적으로 생성된 테마를 보여주고, 지연이나 실패가 발생하면 로컬 콘텐츠로 계속 플레이할 수 있습니다. 실제 구현에서는 테마 JSON 파싱, 여러 아이템 이미지 생성, 결과 JSON POST까지 같은 흐름에 연결했습니다.

## 4. Data-Driven Content Authoring

### 문제

재활용 분리수거는 품목이 늘어날 때마다 프리팹이나 게임 로직을 수정하면 제작 비용과 입력 실수가 함께 증가합니다. 품목의 이미지, 기본 분류, 함정 여부와 부분 정답 규칙을 데이터로 분리할 필요가 있었습니다.

### 설계

- 런타임 규칙은 `ScriptableObject` 데이터만 읽습니다.
- 폴더명을 카테고리 enum으로 해석합니다.
- 폴더 안의 Sprite를 순회해 데이터 에셋을 자동 생성합니다.
- 해석할 수 없는 폴더명은 경고와 안전한 기본값으로 처리합니다.

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Portfolio/Sorting Item")]
public sealed class SortingItemData : ScriptableObject
{
    public string displayName;
    public Sprite icon;
    public SortingCategory primary;
    public bool useSecondary;
    public SortingCategory secondary;

    public bool IsAccepted(SortingCategory droppedCategory, out int scoreRate)
    {
        if (droppedCategory == primary)
        {
            scoreRate = 100;
            return true;
        }

        if (useSecondary && droppedCategory == secondary)
        {
            scoreRate = 50;
            return true;
        }

        scoreRate = 0;
        return false;
    }
}

public enum SortingCategory { General, Paper, Plastic, Glass, Metal }
```

```csharp
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SortingDataGenerator
{
    public static int Generate(string imageRoot, string outputRoot)
    {
        Directory.CreateDirectory(outputRoot);
        int created = 0;

        foreach (string categoryFolder in Directory.GetDirectories(imageRoot))
        {
            string folderName = new DirectoryInfo(categoryFolder).Name;
            if (!Enum.TryParse(folderName, true, out SortingCategory category))
            {
                Debug.LogWarning($"Unknown category folder: {folderName}");
                category = SortingCategory.General;
            }

            foreach (string file in Directory.GetFiles(categoryFolder, "*.png"))
            {
                string assetPath = file.Replace('\\', '/');
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite == null) continue;

                SortingItemData data = ScriptableObject.CreateInstance<SortingItemData>();
                data.displayName = Path.GetFileNameWithoutExtension(file);
                data.icon = sprite;
                data.primary = category;

                string output = $"{outputRoot}/{data.displayName}.asset";
                AssetDatabase.CreateAsset(data, output);
                created++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return created;
    }
}
#endif
```

### 검토 포인트

콘텐츠 수가 늘어나도 게임 코드는 그대로 유지하고 데이터 에셋만 추가할 수 있습니다. 기획·아트 전달물이 정해진 폴더 규칙을 따르면 반복 등록 작업도 줄일 수 있습니다.

## Architecture Summary

```text
Opening / Guided Practice
           ↓
      Round Builder ← Difficulty Policy
           ↓
       Input State
           ↓
        Validation
       ↙          ↘
  Feedback      Result DTO
       ↓             ↓
   Next Round    WebGL / API
```

공통 계층은 게임의 생명주기와 결과 형식을 관리하고, 개별 콘텐츠는 문제 생성·입력 해석·고유 판정에 집중하도록 설계했습니다. 이를 통해 여러 미니게임이 서로 다른 조작 방식을 가지면서도 튜토리얼, 타이머, 난이도와 결과 수집 경험은 일관되게 유지할 수 있었습니다.
