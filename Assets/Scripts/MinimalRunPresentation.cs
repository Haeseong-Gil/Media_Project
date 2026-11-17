using UnityEngine;
using UnityEngine.UI;

// 게임 규칙을 바꾸지 않고 구역과 그림을 연결한다. 빈 항목은 기존 임시 도형으로 표시한다.
[DisallowMultipleComponent]
public sealed class MinimalRunPresentation : MonoBehaviour
{
    [Header("나중에 넣을 그림 · Sprite (2D and UI)")]
    [SerializeField] private Sprite generalBackground;
    [SerializeField] private Sprite scrollingLandscape;
    [SerializeField, Min(0f)] private float landscapeSpeed = 500f;
    [SerializeField, Min(0.01f)] private float landscapeAccelerationSeconds = 3f;
    [SerializeField, Min(0.01f)] private float landscapeBrakingSeconds = 5f;
    [SerializeField] private Sprite stationBackground;
    [SerializeField] private Sprite stationBenchPlatform;
    [SerializeField] private Sprite locomotive;
    [SerializeField] private Sprite carriageOne;
    [SerializeField] private Sprite carriageTwo;

    [Header("연결된 화면 · 보통 수정하지 않음")]
    [SerializeField] private MinimalRunScreen screen;
    [SerializeField] private Image backgroundTarget;
    [SerializeField] private Image stationPlatform;
    [SerializeField] private Image[] trainTargets;
    [SerializeField] private GameObject[] placeholderLabels;
    [SerializeField] private GameObject artNotice;
    [SerializeField] private GameObject railPlaceholder;

    private readonly Color generalColor = new Color32(65, 72, 72, 255);
    private readonly Color stationColor = new Color32(76, 64, 53, 255);
    private readonly Color trainColor = new Color32(78, 100, 111, 255);
    private bool? previousStation;
    private bool? previousChoice;
    private RectTransform landscapeViewport;
    private RectTransform leftTile;
    private RectTransform rightTile;
    private MinimalRunModel landscapeRun;
    private double previousElapsed;
    private double previousTravel;
    private double landscapeVelocity;
    private double landscapeDistance;
    private RectTransform stationStrip;
    private float stationPatternWidth;
    private double? stationStartDistance;
    private double? stationEndDistance;

    private void Start()
    {
        if (backgroundTarget != null && scrollingLandscape != null)
        {
            CreateLandscape();
            if (stationPlatform != null && stationBackground != null && stationBenchPlatform != null)
                CreateStationStrip();
        }
        RefreshVisuals();
    }

    private void LateUpdate()
    {
        if (screen == null || screen.Model == null) return;
        if (previousStation != screen.Model.AtStation || previousChoice != screen.Model.ChoicePaused)
            RefreshVisuals();
        UpdateLandscape();
    }

    private void CreateLandscape()
    {
        landscapeViewport = new GameObject("ScrollingLandscape", typeof(RectTransform), typeof(RectMask2D))
            .GetComponent<RectTransform>();
        landscapeViewport.SetParent(backgroundTarget.rectTransform, false);
        landscapeViewport.anchorMin = Vector2.zero;
        landscapeViewport.anchorMax = Vector2.one;
        landscapeViewport.offsetMin = landscapeViewport.offsetMax = Vector2.zero;
        leftTile = CreateTile("LeftTile", scrollingLandscape, landscapeViewport);
        rightTile = CreateTile("RightTile", scrollingLandscape, landscapeViewport);
        leftTile.anchorMax = rightTile.anchorMax = Vector2.up;
        UpdateLandscape();
    }

    private static RectTransform CreateTile(string name, Sprite sprite, RectTransform parent)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        var rect = image.rectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        return rect;
    }

    private void CreateStationStrip()
    {
        var reference = stationPlatform.rectTransform;
        float scale = reference.rect.width / stationBackground.rect.width;
        float stationWidth = stationBackground.rect.width * scale;
        float benchWidth = stationBenchPlatform.rect.width * scale;
        // 원본 플랫폼 양끝의 투명 여백을 겹쳐 바닥에 틈이 나지 않게 한다.
        float overlap = Mathf.Min(stationWidth, benchWidth) * 0.008f;
        stationPatternWidth = stationWidth + 2f * benchWidth - 3f * overlap;
        if (stationPatternWidth <= 0f) return;

        // 풍경 위에 그리되 같은 마스크 안에 두어 양쪽 화면 밖은 잘라낸다.
        stationStrip = new GameObject("ScrollingStation", typeof(RectTransform)).GetComponent<RectTransform>();
        stationStrip.SetParent(landscapeViewport, false);
        stationStrip.anchorMin = stationStrip.anchorMax = Vector2.up;
        stationStrip.pivot = Vector2.zero;
        stationStrip.sizeDelta = Vector2.zero;
        float baseline = reference.anchoredPosition.y - reference.rect.height - backgroundTarget.rectTransform.anchoredPosition.y;
        stationStrip.anchoredPosition = new Vector2(0f, baseline);
        stationStrip.gameObject.SetActive(false);

        int copies = Mathf.CeilToInt(landscapeViewport.rect.width / stationPatternWidth) + 1;
        for (int i = 0; i < copies; i++)
        {
            float start = i * stationPatternWidth;
            float firstBench = start + stationWidth - overlap;
            AddStationTile("Station", stationBackground, start, scale);
            AddStationTile("Benches", stationBenchPlatform, firstBench, scale);
            AddStationTile("Benches", stationBenchPlatform, firstBench + benchWidth - overlap, scale);
        }
    }

    private void AddStationTile(string name, Sprite sprite, float offset, float scale)
    {
        var tile = CreateTile(name, sprite, stationStrip);
        tile.sizeDelta = sprite.rect.size * scale;
        tile.anchoredPosition = new Vector2(offset, 0f);
    }

    private void UpdateLandscape()
    {
        if (landscapeViewport == null || !landscapeViewport.gameObject.activeSelf) return;
        Rect viewport = landscapeViewport.rect;
        Rect sprite = scrollingLandscape.rect;
        float width = Mathf.Max(viewport.width, viewport.height * sprite.width / sprite.height);
        if (width <= 0) return;
        AdvanceLandscapeMotion();
        float offset = (float)(landscapeDistance % width);
        leftTile.sizeDelta = rightTile.sizeDelta = new Vector2(width, 0f);
        leftTile.anchoredPosition = new Vector2(-offset, 0f);
        rightTile.anchoredPosition = new Vector2(width - offset, 0f);
        UpdateStationStrip(viewport.width);
    }

    private void UpdateStationStrip(float viewportWidth)
    {
        if (stationStrip == null || landscapeRun == null) return;
        if (screen.IsAtTitle)
        {
            if (stationStartDistance.HasValue) ResetStationStrip();
            return;
        }

        bool atStation = landscapeRun.AtStation;
        if (atStation && (!stationStartDistance.HasValue || stationEndDistance.HasValue))
        {
            stationStartDistance ??= landscapeDistance + viewportWidth;
            stationEndDistance = null;
            for (int i = 0; i < stationStrip.childCount; i++)
                stationStrip.GetChild(i).gameObject.SetActive(true);
            stationStrip.gameObject.SetActive(true);
        }
        if (!stationStartDistance.HasValue) return;

        // 첫 타일은 화면 오른쪽에서 들어온다. 반복 위치는 계속 배경의 누적 이동값을 따른다.
        double offset = landscapeDistance - stationStartDistance.Value;
        double firstPattern = System.Math.Floor(System.Math.Max(0d, offset) / stationPatternWidth) * stationPatternWidth;
        stationStrip.anchoredPosition = new Vector2((float)(firstPattern - offset), stationStrip.anchoredPosition.y);
        if (atStation) return;

        // 이탈 순간 화면 오른쪽 끝에 닿은 타일까지 보존하고, 그 뒤에는 새 타일을 내보내지 않는다.
        stationEndDistance ??= landscapeDistance + viewportWidth;
        bool hasRemainingTile = false;
        for (int i = 0; i < stationStrip.childCount; i++)
        {
            var tile = (RectTransform)stationStrip.GetChild(i);
            double tileStart = stationStartDistance.Value + firstPattern + tile.anchoredPosition.x;
            bool remains = tileStart < stationEndDistance.Value && tileStart + tile.rect.width > landscapeDistance;
            if (tile.gameObject.activeSelf != remains)
                tile.gameObject.SetActive(remains);
            hasRemainingTile |= remains;
        }
        if (!hasRemainingTile) ResetStationStrip();
    }

    private void ResetStationStrip()
    {
        stationStartDistance = stationEndDistance = null;
        if (stationStrip != null)
            stationStrip.gameObject.SetActive(false);
    }

    private void AdvanceLandscapeMotion()
    {
        var run = screen?.Model;
        if (landscapeRun != run)
        {
            landscapeRun = run;
            previousElapsed = previousTravel = landscapeVelocity = landscapeDistance = 0;
            ResetStationStrip();
        }
        if (run == null) return;

        double elapsed = run.Elapsed - previousElapsed;
        double travel = run.TravelElapsed - previousTravel;
        previousElapsed = run.Elapsed;
        previousTravel = run.TravelElapsed;

        if (screen.IsAtTitle || !run.CanAct)
        {
            landscapeVelocity = 0;
            return;
        }

        // 연료가 떨어져 프레임 중간에 정차하면, 그 프레임의 남은 시간에는 감속한다.
        AdvanceLandscape(travel, landscapeSpeed, landscapeAccelerationSeconds);
        AdvanceLandscape(elapsed - travel, 0, landscapeBrakingSeconds);
    }

    private void AdvanceLandscape(double seconds, double targetSpeed, float transitionSeconds)
    {
        if (seconds <= 0) return;
        if (landscapeVelocity == targetSpeed)
        {
            landscapeDistance += targetSpeed * seconds;
            return;
        }

        double rate = landscapeSpeed / Mathf.Max(0.01f, transitionSeconds);
        double rampTime = System.Math.Min(seconds, System.Math.Abs(targetSpeed - landscapeVelocity) / rate);
        double nextSpeed = rampTime < seconds
            ? targetSpeed
            : landscapeVelocity + System.Math.Sign(targetSpeed - landscapeVelocity) * rate * rampTime;
        landscapeDistance += (landscapeVelocity + nextSpeed) * 0.5 * rampTime + targetSpeed * (seconds - rampTime);
        landscapeVelocity = nextSpeed;
    }

    public void RefreshVisuals()
    {
        bool atStation = screen != null && screen.Model != null && screen.Model.AtStation;
        bool pending = screen != null && screen.Model != null && screen.Model.ChoicePaused;
        previousStation = atStation; previousChoice = pending;
        Sprite background = atStation && stationBackground != null ? stationBackground : generalBackground;
        if (stationPlatform != null)
        {
            bool showPlatform = atStation && stationBackground != null;
            stationPlatform.gameObject.SetActive(showPlatform && stationStrip == null);
            if (showPlatform)
            {
                background = generalBackground;
                SetImage(stationPlatform, stationBackground, stationColor, true);
            }
        }
        SetImage(backgroundTarget, background, atStation ? stationColor : generalColor, true);
        if (landscapeViewport != null)
        {
            bool showLandscape = background != null && background == generalBackground;
            landscapeViewport.gameObject.SetActive(showLandscape);
            backgroundTarget.enabled = !showLandscape;
        }
        if (railPlaceholder != null)
            railPlaceholder.SetActive(background == null);
        Sprite[] sprites = { locomotive, carriageOne, carriageTwo };
        for (int i = 0; trainTargets != null && i < trainTargets.Length && i < sprites.Length; i++)
        {
            SetImage(trainTargets[i], sprites[i], trainColor, true);
            if (placeholderLabels != null && i < placeholderLabels.Length && placeholderLabels[i] != null)
                placeholderLabels[i].SetActive(sprites[i] == null);
        }
        // 결정창 토글과 안내 문구가 겹치지 않도록 한다.
        if (artNotice != null) artNotice.SetActive(!pending);
    }

    private static void SetImage(Image target, Sprite sprite, Color fallback, bool preserveAspect)
    {
        if (target == null) return;
        target.sprite = sprite;
        target.type = Image.Type.Simple;
        target.preserveAspect = preserveAspect;
        target.color = sprite == null ? fallback : Color.white;
        target.raycastTarget = false;
    }
}
