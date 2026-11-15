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

    private void Start()
    {
        if (backgroundTarget != null && scrollingLandscape != null)
            CreateLandscape();
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
        leftTile = CreateLandscapeTile("LeftTile");
        rightTile = CreateLandscapeTile("RightTile");
        UpdateLandscape();
    }

    private RectTransform CreateLandscapeTile(string name)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.sprite = scrollingLandscape;
        image.raycastTarget = false;
        var rect = image.rectTransform;
        rect.SetParent(landscapeViewport, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.up;
        rect.pivot = Vector2.zero;
        return rect;
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
    }

    private void AdvanceLandscapeMotion()
    {
        var run = screen?.Model;
        if (landscapeRun != run)
        {
            landscapeRun = run;
            previousElapsed = previousTravel = landscapeVelocity = landscapeDistance = 0;
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
            stationPlatform.gameObject.SetActive(showPlatform);
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
