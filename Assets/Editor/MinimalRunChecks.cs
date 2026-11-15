using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MinimalRunChecks
{
    [MenuItem("Tools/Yeolguk Train/Verify Minimal Run")]
    public static void MenuRun()
    {
        string result = Run(); Directory.CreateDirectory("Temp");
        File.WriteAllText("Temp/MinimalRunChecks.txt", result); Debug.Log(result);
    }

    public static string Run()
    {
        var passed = new List<string>();
        Check("새 시작값과 시작 전 정지", () => {
            var m = New(); m.Advance(900); Equal(m.Elapsed, 0); Equal(m.People, 6); Equal(m.Food, 25); Equal(m.Fuel, 100);
        }, passed);
        Check("출발 3과 운행 5초당 1 소비", () => {
            var m = Started(); m.ToggleDriving(); Equal(m.Fuel, 97); m.Advance(4); Equal(m.Fuel, 97); m.Advance(1); Equal(m.Fuel, 96);
        }, passed);
        Check("정차 시 연료 타이머 정지·재출발 시 이어 계산", () => {
            var m = Started(); m.ToggleDriving(); m.Advance(2); m.ToggleDriving(); m.Advance(10); Equal(m.FuelTickElapsed, 2);
            m.ToggleDriving(); Equal(m.Fuel, 94); m.Advance(2); Equal(m.Fuel, 94); m.Advance(1); Equal(m.Fuel, 93);
        }, passed);
        Check("연료 고갈 자동 정차·음수 방지", () => {
            var m = Started(new MinimalRunTuning { startingFuel = 5 }); m.ToggleDriving(); m.Advance(20);
            Equal(m.Fuel, 0); Assert(!m.Moving); Equal(m.ZoneElapsed, 10);
        }, passed);
        Check("출발 비용 부족 시 중복 차감 없음", () => {
            var m = Started(new MinimalRunTuning { startingFuel = 2 }); m.ToggleDriving(); m.ToggleDriving(); Equal(m.Fuel, 2); Assert(!m.Moving);
        }, passed);
        Check("일반 40초·폐역 20초 반복 및 정차 중 구역 보존", () => {
            var m = Started(); m.ToggleDriving(); m.Advance(39); Assert(!m.AtStation); m.Advance(1); Assert(m.AtStation); m.ToggleDriving(); m.Advance(20); Assert(m.AtStation);
            m.ToggleDriving(); m.Advance(19); Assert(m.AtStation); m.Advance(1); Assert(!m.AtStation);
        }, passed);
        Check("최소 3·최대 10·전원 파견 금지·한 팀 제한", () => {
            var m = Started(); Assert(!m.Dispatch(2) && !m.Dispatch(6)); Assert(m.Dispatch(5)); Assert(!m.Dispatch(3));
            var n = Started(new MinimalRunTuning { startingPeople = 20 }); Assert(!n.Dispatch(11)); Assert(n.Dispatch(10));
        }, passed);
        Check("3초 이산 수집 및 귀환 중복 입고 방지", () => {
            var m = Started(); m.Dispatch(3); m.Advance(2.9); Equal(m.CarriedFood, 0); m.Advance(0.1); Equal(m.CarriedFood, 6); Equal(m.CarriedFuel, 18);
            m.Return(); m.Return(); Equal(m.Food, 31); Equal(m.Fuel, 118); Equal(m.Team, 0); Equal(m.PendingSurvivors, 0);
        }, passed);
        Check("폐역 4배 획득", () => {
            var m = AtStation(); m.Dispatch(3); m.Advance(3); Equal(m.CarriedFood, 24); Equal(m.CarriedFuel, 72);
        }, passed);
        Check("9명 1.2배·10명 1.5배", () => {
            var a = Started(new MinimalRunTuning { startingPeople = 11 }); a.Dispatch(9); a.Advance(3); Equal(a.CarriedFood, 21.6); Equal(a.CarriedFuel, 64.8);
            var b = Started(new MinimalRunTuning { startingPeople = 11 }); b.Dispatch(10); b.Advance(3); Equal(b.CarriedFood, 30); Equal(b.CarriedFuel, 90);
        }, passed);
        Check("기본 위험 및 구역별 증가량", () => {
            var a = Started(); a.Dispatch(3); Equal(a.CurrentRiskPercent, 1.1); a.Advance(3); Equal(a.CurrentRiskPercent, 1.3);
            var b = AtStation(); b.Dispatch(3); b.Advance(3); Equal(b.CurrentRiskPercent, 1.15);
        }, passed);
        Check("3명 사고 후 2명 강제 귀환과 남은 자원 입고", () => {
            var m = Started(null, () => 0); m.Dispatch(3); m.Advance(3);
            Equal(m.People, 5); Equal(m.Team, 0); Equal(m.Food, 29); Equal(m.Fuel, 112); Assert(m.Report.Contains("강제 귀환"));
        }, passed);
        Check("5명·6명·8명의 손실 구간과 자원 차감", () => {
            foreach (int n in new[] { 5, 6, 8 }) {
                var m = Started(new MinimalRunTuning { startingPeople = 11 }, () => 0); m.Dispatch(n); m.Advance(3);
                int lost = n < 6 ? 1 : 2; Equal(m.Team, n - lost); Equal(m.People, 11 - lost); Equal(m.CarriedFood, (n - lost) * 2);
            }
        }, passed);
        Check("9명 사고 3명·자원 3명분 손실", () => {
            var m = Started(new MinimalRunTuning { startingPeople = 11 }, () => 0); m.Dispatch(9); m.Advance(3);
            Equal(m.Team, 6); Equal(m.People, 8); Equal(m.CarriedFood, 14.4); Equal(m.CarriedFuel, 43.2); Equal(m.CurrentRiskPercent, 1.7);
        }, passed);
        Check("10명 사고 4명·자원 5명분 손실 및 보너스 해제", () => {
            int checks = 0; var m = Started(new MinimalRunTuning { startingPeople = 11 }, () => checks++ == 0 ? 0 : 0.999999);
            m.Dispatch(10); m.Advance(3); Equal(m.Team, 6); Equal(m.People, 7); Equal(m.CarriedFood, 15); Equal(m.CarriedFuel, 45);
            Equal(m.CurrentRiskPercent, 1.7); Equal(m.CurrentBonus, 1); m.Advance(3); Equal(m.CarriedFood, 27); Equal(m.CarriedFuel, 81);
        }, passed);
        Check("일반 귀환은 사건 없음·첫 폐역 귀환만 5명", () => {
            var m = Started(); m.Dispatch(3); m.Return(); Equal(m.PendingSurvivors, 0); GoToStation(m); m.Dispatch(3); m.Return();
            Equal(m.PendingSurvivors, 5); Assert(m.SurvivorEventOccurred);
        }, passed);
        Check("폐역 강제 귀환도 첫 생존자 사건 발생", () => {
            var m = AtStation(null, () => 0); m.Dispatch(3); m.Advance(900);
            Equal(m.Team, 0); Equal(m.PendingSurvivors, 5); Equal(m.Elapsed, 43); Equal(m.Food, 17);
        }, passed);
        Check("결정 대기 중 모든 시간·자원·명령 정지", () => {
            var m = Choice(); double elapsed = m.Elapsed, fuel = m.Fuel, food = m.Food, day = m.DayElapsed;
            m.Advance(10000); m.ToggleDriving(); Assert(!m.Dispatch(3)); m.Return();
            Equal(m.Elapsed, elapsed); Equal(m.DayElapsed, day); Equal(m.Fuel, fuel); Equal(m.Food, food); Assert(!m.Moving && m.ChoicePaused);
        }, passed);
        Check("수용 +5·중복 처리 없음·두 번째 사건 없음", () => {
            var m = Choice(); m.DecideSurvivors(true); m.DecideSurvivors(true); Equal(m.People, 11);
            m.Dispatch(3); m.Return(); Equal(m.PendingSurvivors, 0); double t = m.Elapsed; m.Advance(1); Equal(m.Elapsed, t + 1);
        }, passed);
        Check("거절 인원 유지·이후 사건 없음", () => {
            var m = Choice(); m.DecideSurvivors(false); m.DecideSurvivors(true); Equal(m.People, 6);
            m.Dispatch(3); m.Return(); Equal(m.PendingSurvivors, 0);
        }, passed);
        Check("20·40·60초에 배급, 세 번째 끼니 후 다음 날", () => {
            var m = Started();
            m.Advance(19.9); Equal(m.Food, 25); Equal(m.TimeUntilMeal, 0.1);
            m.Advance(0.1); Equal(m.Food, 13); Equal(m.Day, 1);
            m.Advance(20); Equal(m.Food, 1); Equal(m.Day, 1);
            m.Advance(20); Equal(m.Food, 0); Equal(m.Day, 2);
            Equal(m.LastMealPaid, 1); Equal(m.HungerScore, 11d / 12);
            Equal(m.DayElapsed, 0); Equal(m.TimeUntilMeal, 20);
        }, passed);
        Check("배급 시 탐사대를 제외하고 부분 배급량만 차감", () => {
            var m = Started(new MinimalRunTuning { startingFood = 4 });
            m.Advance(19); Assert(m.Dispatch(3)); Equal(m.FoodPerMeal, 6);
            m.Advance(1); Equal(m.Food, 0); Equal(m.LastMealPaid, 4); Equal(m.LastMealRequired, 6);
            Equal(m.HungerScore, 1.0 / 3); Equal(m.LastMealHungerChange, 1.0 / 3); Assert(m.HasHadFoodShortage);
            m.Return(); Equal(m.FoodPerMeal, 12);
        }, passed);
        Check("미배급 1점·입고만으로 회복 없음·완전 배급 2회로 회복", () => {
            var m = Started(new MinimalRunTuning { startingFood = 0 });
            m.Advance(20); Equal(m.HungerScore, 1); Equal(m.LastMealHungerChange, 1);
            m.Dispatch(3); m.Advance(12); m.Return();
            Equal(m.Food, 24); Equal(m.HungerScore, 1);
            m.Advance(8); Equal(m.HungerScore, .5); Equal(m.LastMealHungerChange, -.5);
            m.Advance(20); Equal(m.HungerScore, 0); Equal(m.LastMealHungerChange, -.5);
            Assert(m.HasHadFoodShortage);
        }, passed);
        Check("완전 배급으로 0점 아래로 내려가지 않음", () => {
            var m = Started(new MinimalRunTuning { startingFood = 9 });
            m.Advance(20); Equal(m.HungerScore, .25);
            m.Dispatch(3); m.Advance(6); m.Return();
            m.Advance(14); Equal(m.HungerScore, 0); Equal(m.LastMealHungerChange, -.25);
        }, passed);
        Check("위기 경고 6점·미배급 9번이면 게임오버·실패 뒤 정지", () => {
            var m = Started(new MinimalRunTuning { startingFood = 0 });
            m.Advance(119); Equal(m.HungerScore, 5); Assert(!m.IsHungerCritical && !m.CouldFailAtNextMeal);
            m.Advance(1); Equal(m.HungerScore, 6); Assert(m.IsHungerCritical && !m.CouldFailAtNextMeal);
            m.Advance(40); Equal(m.HungerScore, 8); Assert(!m.Failed && m.CouldFailAtNextMeal);
            Equal(m.MinimumSafeRation, 1);
            m.Advance(100); Equal(m.HungerScore, 9); Assert(m.Failed); Equal(m.People, 0);
            Equal(m.Elapsed, 180); Assert(!m.CouldFailAtNextMeal);
        }, passed);
        Check("8.5점에서 최소 지급량 7 안내·정확히 9점도 실패", () => {
            var m = Started(new MinimalRunTuning { startingFood = 6, survivalGoalDays = 4 });
            m.Advance(180); Equal(m.HungerScore, 8.5); Assert(m.CouldFailAtNextMeal);
            Equal(m.MinimumSafeRation, 7);
            m.Dispatch(3); m.Advance(3); m.Return();
            Equal(m.Food, 6); Assert(m.CouldFailAtNextMeal);
            m.Advance(17); Equal(m.LastMealPaid, 6);
            Equal(m.HungerScore, 9); Assert(m.Failed);
        }, passed);
        Check("기본 시작 식량으로 첫날 두 끼를 배급하고 탐사 없이 3일 생존", () => {
            var m = Started();
            m.Advance(59); Equal(m.HungerScore, 0); Equal(m.Food, 1);
            m.Advance(1); Equal(m.HungerScore, 11d / 12); Equal(m.LastMealPaid, 1);
            m.Advance(120); Assert(m.Succeeded && !m.Failed);
            Equal(m.HungerScore, 6 + 11d / 12); Equal(m.Elapsed, 180);
        }, passed);
        Check("3일을 온전히 버티면 마지막 배급 직후 4일차 생존 성공", () => {
            var m = Started(new MinimalRunTuning { startingFood = 108 });
            m.ToggleDriving();
            m.Advance(179); Equal(m.Day, 3); Equal(m.HungerScore, 0); Assert(!m.IsFinished);
            m.Advance(10); Equal(m.Elapsed, 180); Equal(m.Day, 4); Equal(m.HungerScore, 0);
            Equal(m.LastMealRequired, 12); Equal(m.LastMealPaid, 12); Equal(m.People, 6);
            Assert(m.Succeeded && !m.Failed && m.IsFinished && !m.Moving && !m.CanAct);
            double fuel = m.Fuel;
            m.ToggleDriving(); Assert(!m.Dispatch(3)); m.Advance(100);
            Equal(m.Elapsed, 180); Equal(m.Fuel, fuel);
        }, passed);
        Check("3일차 마지막 배급에서 9점에 도달하면 실패가 먼저 처리됨", () => {
            var m = Started(new MinimalRunTuning { startingFood = 0 });
            m.Advance(179); Assert(!m.IsFinished);
            m.Advance(1); Equal(m.Elapsed, 180); Equal(m.HungerScore, 9);
            Assert(m.Failed && !m.Succeeded && m.IsFinished);
        }, passed);
        Check("큰 시간 간격과 분할 시간의 확률·자원 일치", () => {
            var ra = new System.Random(77); var rb = new System.Random(77);
            var a = Started(new MinimalRunTuning { startingPeople = 11 }, ra.NextDouble); var b = Started(new MinimalRunTuning { startingPeople = 11 }, rb.NextDouble);
            a.Dispatch(10); b.Dispatch(10); a.Advance(59); for (int i = 0; i < 590; i++) b.Advance(0.1);
            Equal(a.Team, b.Team); Equal(a.People, b.People); Equal(a.CarriedFood, b.CarriedFood); Equal(a.CarriedFuel, b.CarriedFuel); Equal(a.Food, b.Food); Equal(a.CurrentRiskPercent, b.CurrentRiskPercent);
        }, passed);
        Check("150초 강제 종료 없음·새 인스턴스 사건과 타이머 초기화", () => {
            var a = Choice(); a.DecideSurvivors(true); a.Advance(150 - a.Elapsed); Assert(!a.IsFinished);
            var b = New(); Equal(b.Elapsed, 0); Equal(b.People, 6); Assert(!b.Started && !b.SurvivorEventOccurred && !b.HasHadFoodShortage && b.HungerScore == 0);
        }, passed);
        return $"PASS {passed.Count}/{passed.Count}\n" + string.Join("\n", passed);
    }

    [MenuItem("Tools/Yeolguk Train/Verify Live UI")]
    public static async void VerifyLiveUI()
    {
        try { await VerifyLiveUIAsync(); }
        catch (Exception error)
        {
            File.WriteAllText("Temp/MinimalRunUIChecks.txt", "FAIL UI: " + error);
            Debug.LogException(error);
        }
    }

    private static async Task VerifyLiveUIAsync()
    {
        var screen = UnityEngine.Object.FindAnyObjectByType<MinimalRunScreen>();
        Assert(EditorApplication.isPlaying && screen != null && screen.IsAtTitle);
        Click("TitleOverlay/Body/StartButton"); var m = screen.Model;
        Assert(!screen.IsAtTitle && m.Started); Equal(m.People, 6); Equal(m.Food, 25); Equal(m.Fuel, 100);
        Click("ControlArea/DriveButton"); m.Advance(m.ZoneDuration); Click("ControlArea/DriveButton");
        Click("ControlArea/DispatchButton"); Equal(m.Team, 3); Click("ControlArea/ReturnButton"); Equal(m.PendingSurvivors, 5);
        double elapsed = m.Elapsed, food = m.Food, fuel = m.Fuel, day = m.DayElapsed;
        // 새로 활성화한 UI의 네이티브 입력 깊이가 다음 렌더 프레임에 갱신될 때까지 기다린다.
        await Task.Delay(150);
        Click("ChoiceToggleButton"); Assert(!screen.ChoiceVisible && GameObject.Find("MinimalRunCanvas/SurvivorOverlay") == null);
        await Task.Delay(150);
        m.Advance(10000); Equal(m.Elapsed, elapsed); Equal(m.DayElapsed, day); Equal(m.Food, food); Equal(m.Fuel, fuel); Assert(m.ChoicePaused);
        Assert(!GameObject.Find("MinimalRunCanvas/ControlArea/DriveButton").GetComponent<Button>().interactable);
        Click("ChoiceToggleButton"); Assert(screen.ChoiceVisible);
        await Task.Delay(150);
        Click("SurvivorOverlay/Body/AcceptButton"); Equal(m.People, 11); Assert(!m.ChoicePaused);
        await Task.Delay(150);
        Click("ControlArea/DispatchButton"); Click("ControlArea/ReturnButton"); Equal(m.PendingSurvivors, 0);
        m.Advance(10000); screen.Drive(); Assert(m.Succeeded); Equal(m.Elapsed, 180); Equal(m.Day, 4);
        await Task.Delay(150);
        Click("FailureOverlay/Body/TitleButton"); Assert(screen.IsAtTitle);
        await Task.Delay(150);
        Click("TitleOverlay/Body/StartButton"); Assert(screen.Model != m && screen.Model.Started); Equal(screen.Model.People, 6); Equal(screen.Model.Food, 25); Equal(screen.Model.Fuel, 100);
        Assert(!screen.Model.SurvivorEventOccurred && !screen.Model.HasHadFoodShortage && screen.Model.HungerScore == 0);
        string result = "PASS UI: title/start, dispatch, first-station event, hide/show, frozen clocks/resources/commands, accept, no repeat event, survival success/title/new RUN";
        File.WriteAllText("Temp/MinimalRunUIChecks.txt", result); Debug.Log(result);
    }

    [MenuItem("Tools/Yeolguk Train/Preview Survivor Choice")]
    public static void PreviewChoice()
    {
        var screen = UnityEngine.Object.FindAnyObjectByType<MinimalRunScreen>();
        Assert(EditorApplication.isPlaying && screen != null && screen.IsAtTitle);
        Click("TitleOverlay/Body/StartButton"); Click("ControlArea/DriveButton"); screen.Model.Advance(screen.Model.ZoneDuration);
        Click("ControlArea/DriveButton"); Click("ControlArea/DispatchButton"); Click("ControlArea/ReturnButton");
        Assert(screen.Model.ChoicePaused);
    }

    [MenuItem("Tools/Yeolguk Train/Toggle Choice Preview")]
    public static void TogglePreview() { UnityEngine.Object.FindAnyObjectByType<MinimalRunScreen>().ToggleChoiceVisibility(); }

    [MenuItem("Tools/Yeolguk Train/Preview Failure Screen")]
    public static void PreviewFailure()
    {
        var screen = UnityEngine.Object.FindAnyObjectByType<MinimalRunScreen>();
        Assert(EditorApplication.isPlaying && screen != null && screen.IsAtTitle);
        screen.StartNewRun();
        var model = screen.Model;
        int goal = model.Tuning.survivalGoalDays;
        try
        {
            // 기본 3일 시연에서는 시작 식량으로 인해 9점 전에 끝난다.
            model.Tuning.survivalGoalDays = Math.Max(goal, 4);
            model.Advance(10000);
        }
        finally { model.Tuning.survivalGoalDays = goal; }
        screen.Drive();
        Assert(model.Failed);
    }

    private static void Click(string path)
    {
        Canvas.ForceUpdateCanvases();
        var go = GameObject.Find("MinimalRunCanvas/" + path); Assert(go != null);
        var rect = go.GetComponent<RectTransform>();
        var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
        if (hits.Count == 0 || hits[0].gameObject != go || !go.GetComponent<Button>().interactable)
            throw new Exception("UI 클릭 확인 실패: " + path + ", 맨 위 입력 대상: " + (hits.Count > 0 ? hits[0].gameObject.name : "없음"));
        ExecuteEvents.Execute(go, data, ExecuteEvents.pointerClickHandler);
    }

    private static MinimalRunModel New(MinimalRunTuning t = null, Func<double> random = null) => new MinimalRunModel(t ?? new MinimalRunTuning(), random ?? (() => 0.999999));
    private static MinimalRunModel Started(MinimalRunTuning t = null, Func<double> random = null) { var m = New(t, random); m.Start(); return m; }
    private static void GoToStation(MinimalRunModel m) { m.ToggleDriving(); m.Advance(m.ZoneDuration); m.ToggleDriving(); }
    private static MinimalRunModel AtStation(MinimalRunTuning t = null, Func<double> random = null) { var m = Started(t, random); GoToStation(m); return m; }
    private static MinimalRunModel Choice() { var m = AtStation(); m.Dispatch(3); m.Return(); return m; }
    private static void Assert(bool value) { if (!value) throw new Exception("Assertion failed"); }
    private static void Equal(double actual, double expected) { if (Math.Abs(actual - expected) > 0.00001) throw new Exception($"Expected {expected}, got {actual}"); }
    private static void Check(string name, Action body, List<string> passed) { try { body(); passed.Add(name); } catch (Exception e) { throw new Exception(name + ": " + e.Message, e); } }
}
