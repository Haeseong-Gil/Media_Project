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
    [SerializeField] private Text resourcesText;
    [SerializeField] private Text reportText;
    [SerializeField] private Text teamText;
    [SerializeField] private Text crewText;
    [SerializeField] private Text survivorsText;
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
    [SerializeField] private Text titleDetails;
    [SerializeField] private GameObject survivorPanel;
    [SerializeField] private Text survivorDetails;
    [SerializeField] private Button choiceToggleButton;
    [SerializeField] private Text choiceToggleLabel;
    [SerializeField] private GameObject failurePanel;
    [SerializeField] private Text failureDetails;

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
        if (Model == null || !Model.Failed) return;
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
        SetLabel(zoneText, $"{(Model.AtStation ? "폐역" : "일반 구역")}  ·  {(Model.Moving ? "운행 중" : "정차 중")}    |    다음 구역까지 운행 {System.Math.Max(0, tuning.secondsPerZone - Model.ZoneElapsed):0.0}초");
        SetLabel(clockText, $"{Model.Day}일째  ·  하루 {Model.DayElapsed:0.0} / {tuning.secondsPerDay:0.#}초    |    {(Model.IsHungry ? "굶주림 " + Model.HungryDays + "일차 / 7일" : "굶주림 없음")}");
        SetLabel(resourcesText, $"식량  {Model.Food:0.0}  (하루 필요 {Model.People * tuning.foodPerPersonPerDay:0.#})       연료  {Model.Fuel:0.0}       전체 인원  {Model.People}명");
        SetLabel(reportText, Model.Report);
        string location = Model.TeamAtStation ? "폐역" : "일반 구역";
        SetLabel(teamText, Model.Team > 0
            ? $"탐사대 {Model.Team}명 · {location}\n운반 식량 {Model.CarriedFood:0.0} / 연료 {Model.CarriedFuel:0.0}\n위험 {Model.CurrentRiskPercent:0.00}% · 사고 시 {Model.CurrentLossCount}명 손실\n다음 판정 {System.Math.Max(0, tuning.secondsPerSearchTick - Model.SearchTickElapsed):0.0}초\n인원 획득 보너스 {Model.CurrentBonus:0.0}배"
            : "탐사대 없음\n운반 식량 0 / 연료 0\n최소 3명 · 최대 10명\n기차에 최소 1명이 남아야 합니다.");
        SetLabel(crewText, max >= tuning.minimumDispatch ? $"{selectedCrew} / {max}명" : "파견 불가");
        SetLabel(survivorsText, Model.ChoicePaused ? "수용 판단 대기 · 게임 정지" : Model.SurvivorEventOccurred ? "생존자 사건 처리 완료" : "첫 폐역 귀환 시 생존자 5명");
        SetLabel(driveLabel, Model.Moving ? "정차" : "출발 (연료 3)");
        driveButton.interactable = active && (Model.Moving || (Model.Team == 0 && Model.Fuel > 0 && Model.Fuel + 0.0000001 >= tuning.departureFuelCost));
        dispatchButton.interactable = canDispatch;
        returnButton.interactable = active && Model.Team > 0;
        minusButton.interactable = canDispatch && selectedCrew > tuning.minimumDispatch;
        plusButton.interactable = canDispatch && selectedCrew < max;
        acceptButton.interactable = rejectButton.interactable = !IsAtTitle && Model.ChoicePaused && !Model.Failed;

        SetLabel(noticeText, IsAtTitle ? "시작을 누르면 새 RUN · 시험값: 하루 300초 / 식량 1인당 3 / 출발 연료 3"
            : Model.Failed ? "RUN 종료 · 실패 화면에서 타이틀로 돌아갈 수 있습니다."
            : Model.ChoicePaused ? "수용·거절 결정 전까지 게임 정지 · 선택창을 숨겨도 시간·자원·위험은 멈춰 있습니다."
            : "시간 진행 중 · 탐사대 2명 이하 즉시 강제 귀환 · 수용 판단 중에는 게임 정지");
        if (titlePanel != null) titlePanel.SetActive(IsAtTitle);
        if (survivorPanel != null) survivorPanel.SetActive(!IsAtTitle && !Model.Failed && Model.ChoicePaused && ChoiceVisible);
        if (failurePanel != null) failurePanel.SetActive(!IsAtTitle && Model.Failed);
        if (choiceToggleButton != null) choiceToggleButton.gameObject.SetActive(!IsAtTitle && !Model.Failed && Model.ChoicePaused);
        SetLabel(choiceToggleLabel, ChoiceVisible ? "상태 보기 · 게임 정지" : "수용·거절 창 다시 열기");
        SetLabel(titleDetails, $"식량·연료와 공동체의 최소 생존 순환\n시작 인원 {tuning.startingPeople}명 / 식량 {tuning.startingFood:0.#} / 연료 {tuning.startingFuel:0.#}\n하루 {tuning.secondsPerDay:0.#}초 · 일반 구역에 정차한 상태로 시작");
        SetLabel(survivorDetails, $"생존자 {Model.PendingSurvivors}명이 합류를 요청합니다.\n현재 인원 {Model.People}명 → 수용 후 {Model.People + Model.PendingSurvivors}명\n하루 식량 필요량 {Model.People * tuning.foodPerPersonPerDay:0.#} → {(Model.People + Model.PendingSurvivors) * tuning.foodPerPersonPerDay:0.#}\n시간 제한 없음 · 상태 보기로 확인한 뒤 결정하세요.");
        SetLabel(failureDetails, Model.Report + $"\n경과 {Model.Elapsed:0}초 · 타이틀의 시작 버튼으로 새 RUN을 시작합니다.");
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
        if (ownsTemporaryFont && temporaryFont != null) Destroy(temporaryFont);
    }
}
