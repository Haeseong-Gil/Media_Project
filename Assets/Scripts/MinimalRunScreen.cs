using UnityEngine;
using UnityEngine.UI;

public sealed class MinimalRunScreen : MonoBehaviour
{
    [Header("합의한 프로토타입 시험값")]
    [SerializeField] private MinimalRunTuning tuning = new MinimalRunTuning();
    [Header("리소스 교체 · 비워 두면 Windows 임시 글꼴")]
    [SerializeField] private Font uiFont;
    [Header("상태 화면")]
    [SerializeField] private Text zoneText;
    [SerializeField] private Text clockText;
    [SerializeField] private RectTransform clockMinuteHand;
    [SerializeField] private Text hungerText;
    [SerializeField] private Text hungerWarningText;
    [SerializeField] private Text hungerRationText;
    [SerializeField] private Image hungerFill;
    [SerializeField] private Text resourcesText;
    [SerializeField] private Text reportText;
    [SerializeField] private Text teamText;
    [SerializeField] private Text crewText;
    [SerializeField] private Text driveLabel;
    [SerializeField] private Text noticeText;
    [SerializeField] private Button driveButton;
    [SerializeField] private Button dispatchButton;
    [SerializeField] private Button returnButton;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button rejectButton;
    [Header("타이틀·결정창·실패 화면")]
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject survivorPanel;
    [SerializeField] private Text survivorDetails;
    [SerializeField] private Button choiceToggleButton;
    [SerializeField] private Text choiceToggleLabel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text resultHeading;
    [SerializeField] private Text resultReason;
    [SerializeField] private Text resultDetails;

    private static readonly Color32 HungerAmber = new Color32(226, 154, 38, 255);
    private static readonly Color32 HungerOrange = new Color32(234, 107, 31, 255);
    private static readonly Color32 HungerRationRed = new Color32(0xC4, 0x2A, 0x12, 255);

    private Sprite hungerGradient;
    private Font temporaryFont;
    private bool rebuildTextRequested;
    private bool ownsTemporaryFont;
    private int selectedCrew;
    public bool IsAtTitle { get; private set; } = true;
    public bool ChoiceVisible { get; private set; } = true;
    public MinimalRunModel Model { get; private set; }

    private void Awake()
    {
        temporaryFont = uiFont;
        if (temporaryFont == null)
        {
            temporaryFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕" }, 28);
            ownsTemporaryFont = temporaryFont != null;
        }
        Font.textureRebuilt += OnFontRebuilt;
        if (temporaryFont != null)
            foreach (Text label in GetComponentsInChildren<Text>(true)) label.font = temporaryFont;
        if (!tuning.IsValid(out string error))
        {
            SetLabel(reportText, error);
            foreach (Button button in GetComponentsInChildren<Button>(true)) button.interactable = false;
            return;
        }
        Model = new MinimalRunModel(tuning);
        if (hungerFill != null)
            CreateHungerGradient();
        selectedCrew = tuning.minimumDispatch;
        Refresh();
    }

    private void Update()
    {
        if (Model == null) return;
        if (!IsAtTitle) Model.Advance(Time.deltaTime);
        Refresh();
    }

    private void OnFontRebuilt(Font font) { if (font == temporaryFont) rebuildTextRequested = true; }

    private void LateUpdate()
    {
        if (!rebuildTextRequested) return;
        rebuildTextRequested = false;
        foreach (Text label in GetComponentsInChildren<Text>(false))
        {
            label.cachedTextGenerator.Invalidate();
            label.SetVerticesDirty();
        }
    }

    public void StartNewRun()
    {
        if (!IsAtTitle) return;
        Model = new MinimalRunModel(tuning);
        Model.Start();
        selectedCrew = tuning.minimumDispatch;
        ChoiceVisible = true;
        IsAtTitle = false;
        Refresh();
    }

    public void ReturnToTitle()
    {
        if (Model == null || !Model.IsFinished) return;
        IsAtTitle = true;
        ChoiceVisible = true;
        Refresh();
    }

    public void Drive() { if (!IsAtTitle && Model != null) { Model.ToggleDriving(); Refresh(); } }
    public void Dispatch() { if (!IsAtTitle && Model != null) { Model.Dispatch(selectedCrew); Refresh(); } }
    public void Return() { if (!IsAtTitle && Model != null) { Model.Return(); Refresh(); } }
    public void DecreaseCrew() { if (CanSelectCrew()) { selectedCrew--; Refresh(); } }
    public void IncreaseCrew() { if (CanSelectCrew()) { selectedCrew++; Refresh(); } }
    public void Accept() { Decide(true); }
    public void Reject() { Decide(false); }

    private void Decide(bool accept)
    {
        if (IsAtTitle || Model == null) return;
        Model.DecideSurvivors(accept);
        ChoiceVisible = true;
        Refresh();
    }

    public void ToggleChoiceVisibility()
    {
        if (IsAtTitle || Model == null || !Model.ChoicePaused) return;
        ChoiceVisible = !ChoiceVisible;
        // 보이는 창만 바꾸고 결정 대기 상태는 유지한다. 게임 진행은 계속 정지한다.
        Refresh();
    }

    private bool CanSelectCrew() => !IsAtTitle && Model != null && Model.CanAct && !Model.Moving && Model.Team == 0;

    private void Refresh()
    {
        if (Model == null) return;
        int max = Model.MaximumSelectable;
        selectedCrew = Mathf.Clamp(selectedCrew, tuning.minimumDispatch, Mathf.Max(tuning.minimumDispatch, max));
        bool active = !IsAtTitle && Model.CanAct;
        bool canDispatch = active && !Model.Moving && Model.Team == 0 && max >= tuning.minimumDispatch;
        SetLabel(zoneText, $"{(Model.AtStation ? "폐역" : "일반 구역")}  ·  {(Model.Moving ? "운행 중" : "정차 중")}    |    다음 구역까지 운행 {System.Math.Ceiling(System.Math.Max(0, Model.ZoneDuration - Model.ZoneElapsed)):0}초");
        RefreshHungerMeter();
        if (clockMinuteHand != null)
            clockMinuteHand.localRotation = Quaternion.Euler(0f, 0f, -360f * (float)(Model.DayElapsed / tuning.secondsPerDay));
        SetLabel(resourcesText, $"식량 {Model.Food:0.#}  (배급 {Model.FoodPerMeal:0.#} 필요 · {System.Math.Ceiling(Model.TimeUntilMeal):0}초 후)       연료 {Model.Fuel:0.#}       인원 {Model.People}명");
        string mealReport = Model.HasHadFoodShortage
            ? $"\n최근 배급 {Model.LastMealPaid:0.##} / {Model.LastMealRequired:0.##} · 굶주림 {Model.LastMealHungerChange:+0.##;-0.##;0}"
            : "";
        SetLabel(reportText, Model.Report + mealReport);
        string location = Model.TeamAtStation ? "폐역" : "일반 구역";
        string bonus = Model.CurrentBonus > 1 ? $"\n인원 획득 보너스 {Model.CurrentBonus:0.#}배" : "";
        SetLabel(teamText, Model.Team > 0
            ? $"탐사대 {Model.Team}명 · {location}\n운반 식량 {Model.CarriedFood:0.#} / 연료 {Model.CarriedFuel:0.#}\n위험 {Model.CurrentRiskPercent:0.##}% · 사고 시 {Model.CurrentLossCount}명 손실\n다음 판정 {System.Math.Ceiling(System.Math.Max(0, tuning.secondsPerSearchTick - Model.SearchTickElapsed)):0}초{bonus}"
            : "탐사대 없음");
        SetLabel(crewText, max >= tuning.minimumDispatch ? $"{selectedCrew} / {max}명" : "파견 불가");
        SetLabel(driveLabel, Model.Moving ? "정차" : "출발 (연료 3)");
        driveButton.interactable = active && (Model.Moving || (Model.Team == 0 && Model.Fuel > 0 && Model.Fuel + 0.0000001 >= tuning.departureFuelCost));
        dispatchButton.interactable = canDispatch;
        returnButton.interactable = active && Model.Team > 0;
        minusButton.interactable = canDispatch && selectedCrew > tuning.minimumDispatch;
        plusButton.interactable = canDispatch && selectedCrew < max;
        acceptButton.interactable = rejectButton.interactable = !IsAtTitle && Model.ChoicePaused && !Model.IsFinished;

        SetLabel(noticeText, IsAtTitle || Model.IsFinished ? ""
            : Model.ChoicePaused ? "일시 정지 · 생존자 선택 대기"
            : $"목표 {tuning.survivalGoalDays}일 생존");
        if (titlePanel != null) titlePanel.SetActive(IsAtTitle);
        if (survivorPanel != null) survivorPanel.SetActive(!IsAtTitle && !Model.IsFinished && Model.ChoicePaused && ChoiceVisible);
        if (resultPanel != null) resultPanel.SetActive(!IsAtTitle && Model.IsFinished);
        if (choiceToggleButton != null) choiceToggleButton.gameObject.SetActive(!IsAtTitle && !Model.IsFinished && Model.ChoicePaused);
        SetLabel(choiceToggleLabel, ChoiceVisible ? "상태 보기" : "선택창 다시 열기");
        SetLabel(survivorDetails, $"생존자 {Model.PendingSurvivors}명이 합류를 요청합니다.\n현재 인원 {Model.People}명 → 수용 후 {Model.People + Model.PendingSurvivors}명\n끼니당 식량 필요량 {Model.FoodPerMeal:0.#} → {(Model.People + Model.PendingSurvivors) * tuning.foodPerPersonPerMeal:0.#}\n시간 제한 없음");
        if (!IsAtTitle && Model.IsFinished)
            RefreshResult();
    }

    private void RefreshHungerMeter()
    {
        string day = $"{Model.Day}일째";
        string score = $"{Model.HungerScore:0.0} / {MinimalRunModel.HungerFailureScore:0}";
        bool critical = Model.IsHungerCritical;
        bool rationWarning = Model.CouldFailAtNextMeal;
        string ration = rationWarning ? $"다음 배급 최소 {Model.MinimumSafeRation:0.##} 필요" : "";

        if (hungerFill == null)
        {
            string hunger = critical ? $"<color=#FFBA67>위기 · 굶주림 {score}</color>" : $"굶주림 {score}";
            if (rationWarning) hunger += $"  ·  {ration}";
            SetLabel(clockText, hungerText == null ? $"{day}    |    {hunger}" : day);
            SetLabel(hungerText, hunger);
            return;
        }

        SetLabel(clockText, day);
        SetLabel(hungerText, score);
        SetLabel(hungerWarningText, critical ? "위기" : "");
        SetLabel(hungerRationText, ration);

        float current = (float)Model.HungerScore;
        float warningScore = (float)MinimalRunModel.HungerWarningScore;
        float failureScore = (float)MinimalRunModel.HungerFailureScore;
        hungerFill.fillAmount = Mathf.Clamp01(current / failureScore);
        Color fillColor = Color.Lerp(HungerAmber, HungerOrange, Mathf.InverseLerp(0f, warningScore, current));
        hungerFill.color = rationWarning
            ? HungerRationRed
            : Color.Lerp(fillColor, HungerRationRed, Mathf.InverseLerp(warningScore, failureScore, current));
    }

    private void CreateHungerGradient()
    {
        const int height = 32;
        var gradientTexture = new Texture2D(1, height, TextureFormat.RGBA32, false)
        {
            name = "Hunger Fill Gradient",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color32[height];
        for (int i = 0; i < height; i++)
        {
            // 밝은 윗부분과 어두운 아랫부분을 유지하며 UI 색상만 바꾼다.
            float y = i / (height - 1f);
            float bottomShade = Mathf.Lerp(0.59f, 1f, Mathf.InverseLerp(0f, 0.65f, y));
            float topShade = Mathf.Lerp(0.64f, 1f, Mathf.InverseLerp(1f, 0.8f, y));
            byte shade = (byte)Mathf.RoundToInt(Mathf.Min(bottomShade, topShade) * 255f);
            pixels[i] = new Color32(shade, shade, shade, 255);
        }

        gradientTexture.SetPixels32(pixels);
        gradientTexture.Apply(false, true);
        hungerGradient = Sprite.Create(gradientTexture, new Rect(0, 0, 1, height), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect);
        hungerFill.sprite = hungerGradient;
    }

    private void RefreshResult()
    {
        SetLabel(resultHeading, Model.Succeeded ? "생존 성공" : "생존 실패");
        SetLabel(resultReason, Model.Succeeded
            ? $"{tuning.survivalGoalDays}일을 버티고 {Model.Day}일차에 도달했습니다"
            : "굶주림 지수가 한계에 도달했습니다");
        long totalSeconds = (long)System.Math.Floor(Model.Elapsed);
        long minutes = totalSeconds / 60;
        long seconds = totalSeconds % 60;
        string survivalTime = minutes > 0 ? $"{minutes}분 {seconds}초" : $"{seconds}초";
        double previousScore = Model.HungerScore - Model.LastMealHungerChange;
        SetLabel(resultDetails,
            $"<color=#FFBA67>{previousScore:0.00} → {Model.HungerScore:0.00}</color>\n" +
            $"{Model.LastMealPaid:0.##} / {Model.LastMealRequired:0.##}\n" +
            survivalTime);
    }

    private static void SetLabel(Text label, string content)
    {
        if (label == null || label.text == content) return;
        label.text = content;
        label.cachedTextGenerator.Invalidate();
        label.SetVerticesDirty();
    }

    private void OnDestroy()
    {
        Font.textureRebuilt -= OnFontRebuilt;
        if (hungerGradient != null)
        {
            Destroy(hungerGradient.texture);
            Destroy(hungerGradient);
        }
        if (ownsTemporaryFont && temporaryFont != null) Destroy(temporaryFont);
    }
}
