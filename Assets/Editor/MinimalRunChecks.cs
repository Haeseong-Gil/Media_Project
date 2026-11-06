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
            var m = New(); m.Advance(900); Equal(m.Elapsed, 0); Equal(m.People, 6); Equal(m.Food, 100); Equal(m.Fuel, 1000);
        }, passed);
        Check("출발 3과 운행 5초당 1 소비", () => {
            var m = Started(); m.ToggleDriving(); Equal(m.Fuel, 997); m.Advance(4); Equal(m.Fuel, 997); m.Advance(1); Equal(m.Fuel, 996);
        }, passed);
        Check("정차 시 연료 타이머 정지·재출발 시 이어 계산", () => {
            var m = Started(); m.ToggleDriving(); m.Advance(2); m.ToggleDriving(); m.Advance(10); Equal(m.FuelTickElapsed, 2);
            m.ToggleDriving(); Equal(m.Fuel, 994); m.Advance(2); Equal(m.Fuel, 994); m.Advance(1); Equal(m.Fuel, 993);
        }, passed);
        Check("연료 고갈 자동 정차·음수 방지", () => {
            var m = Started(new MinimalRunTuning { startingFuel = 5 }); m.ToggleDriving(); m.Advance(20);
            Equal(m.Fuel, 0); Assert(!m.Moving); Equal(m.ZoneElapsed, 10);
        }, passed);
        Check("출발 비용 부족 시 중복 차감 없음", () => {
            var m = Started(new MinimalRunTuning { startingFuel = 2 }); m.ToggleDriving(); m.ToggleDriving(); Equal(m.Fuel, 2); Assert(!m.Moving);
        }, passed);
        Check("일반·폐역 60초 반복 및 정차 중 구역 보존", () => {
            var m = Started(); m.ToggleDriving(); m.Advance(60); Assert(m.AtStation); m.ToggleDriving(); m.Advance(20); Assert(m.AtStation);
            m.ToggleDriving(); m.Advance(60); Assert(!m.AtStation);
        }, passed);
        Check("최소 3·최대 10·전원 파견 금지·한 팀 제한", () => {
            var m = Started(); Assert(!m.Dispatch(2) && !m.Dispatch(6)); Assert(m.Dispatch(5)); Assert(!m.Dispatch(3));
            var n = Started(new MinimalRunTuning { startingPeople = 20 }); Assert(!n.Dispatch(11)); Assert(n.Dispatch(10));
        }, passed);
        Check("3초 이산 수집 및 귀환 중복 입고 방지", () => {
            var m = Started(); m.Dispatch(3); m.Advance(2.9); Equal(m.CarriedFood, 0); m.Advance(0.1); Equal(m.CarriedFood, 6); Equal(m.CarriedFuel, 18);
            m.Return(); m.Return(); Equal(m.Food, 106); Equal(m.Fuel, 1018); Equal(m.Team, 0); Equal(m.PendingSurvivors, 0);
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
            Equal(m.People, 5); Equal(m.Team, 0); Equal(m.Food, 104); Equal(m.Fuel, 1012); Assert(m.Report.Contains("강제 귀환"));
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
            Equal(m.Team, 0); Equal(m.PendingSurvivors, 5); Equal(m.Elapsed, 63); Equal(m.Food, 116);
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
        Check("첫 부족 0일차·이후 누적·8번째 부족에 실패", () => {
            var m = Started(new MinimalRunTuning { startingFood = 0 }); m.Advance(300); Assert(m.IsHungry); Equal(m.HungryDays, 0);
            m.Advance(1800); Equal(m.HungryDays, 6); Assert(!m.Failed); m.Advance(300); Equal(m.HungryDays, 7); Assert(m.Failed); Equal(m.People, 0);
        }, passed);
        Check("충분한 배급 후 굶주림 해제 및 다음 부족 0일차", () => {
            var m = Started(new MinimalRunTuning { startingFood = 0 }); m.Advance(300); m.Dispatch(3); m.Advance(9); m.Return();
            m.Advance(291); Assert(!m.IsHungry); Equal(m.Food, 0); m.Advance(300); Assert(m.IsHungry); Equal(m.HungryDays, 0);
        }, passed);
        Check("기본 인원·식량으로 65분 후 실패·실패 뒤 정지", () => {
            var m = Started(); m.Advance(10000); Assert(m.Failed); Equal(m.Elapsed, 3900); m.Advance(300); m.ToggleDriving(); Equal(m.Elapsed, 3900);
        }, passed);
        Check("큰 시간 간격과 분할 시간의 확률·자원 일치", () => {
            var ra = new System.Random(77); var rb = new System.Random(77);
            var a = Started(new MinimalRunTuning { startingPeople = 11 }, ra.NextDouble); var b = Started(new MinimalRunTuning { startingPeople = 11 }, rb.NextDouble);
            a.Dispatch(10); b.Dispatch(10); a.Advance(59); for (int i = 0; i < 590; i++) b.Advance(0.1);
            Equal(a.Team, b.Team); Equal(a.People, b.People); Equal(a.CarriedFood, b.CarriedFood); Equal(a.CarriedFuel, b.CarriedFuel); Equal(a.Food, b.Food); Equal(a.CurrentRiskPercent, b.CurrentRiskPercent);
        }, passed);
        Check("150초 강제 종료 없음·새 인스턴스 사건과 타이머 초기화", () => {
            var a = Choice(); a.DecideSurvivors(true); a.Advance(150); Assert(!a.Failed);
            var b = New(); Equal(b.Elapsed, 0); Equal(b.People, 6); Assert(!b.Started && !b.SurvivorEventOccurred && !b.IsHungry);
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
        Assert(!screen.IsAtTitle && m.Started); Equal(m.People, 6);
        Click("ControlArea/DriveButton"); m.Advance(60); Click("ControlArea/DriveButton");
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
        m.Advance(10000); screen.Drive(); Assert(m.Failed);
        await Task.Delay(150);
        Click("FailureOverlay/Body/TitleButton"); Assert(screen.IsAtTitle);
        await Task.Delay(150);
        Click("TitleOverlay/Body/StartButton"); Assert(screen.Model != m && screen.Model.Started); Equal(screen.Model.People, 6); Equal(screen.Model.Food, 100); Equal(screen.Model.Fuel, 1000);
        Assert(!screen.Model.SurvivorEventOccurred && !screen.Model.IsHungry);
        string result = "PASS UI: title/start, dispatch, first-station event, hide/show, frozen clocks/resources/commands, accept, no repeat event, failure/title/new RUN";
        File.WriteAllText("Temp/MinimalRunUIChecks.txt", result); Debug.Log(result);
    }

    [MenuItem("Tools/Yeolguk Train/Preview Survivor Choice")]
    public static void PreviewChoice()
    {
        var screen = UnityEngine.Object.FindAnyObjectByType<MinimalRunScreen>();
        Assert(EditorApplication.isPlaying && screen != null && screen.IsAtTitle);
        Click("TitleOverlay/Body/StartButton"); Click("ControlArea/DriveButton"); screen.Model.Advance(60);
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
        screen.StartNewRun(); screen.Model.Advance(10000); screen.Drive();
        Assert(screen.Model.Failed);
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
    private static void GoToStation(MinimalRunModel m) { m.ToggleDriving(); m.Advance(60); m.ToggleDriving(); }
    private static MinimalRunModel AtStation(MinimalRunTuning t = null, Func<double> random = null) { var m = Started(t, random); GoToStation(m); return m; }
    private static MinimalRunModel Choice() { var m = AtStation(); m.Dispatch(3); m.Return(); return m; }
    private static void Assert(bool value) { if (!value) throw new Exception("Assertion failed"); }
    private static void Equal(double actual, double expected) { if (Math.Abs(actual - expected) > 0.00001) throw new Exception($"Expected {expected}, got {actual}"); }
    private static void Check(string name, Action body, List<string> passed) { try { body(); passed.Add(name); } catch (Exception e) { throw new Exception(name + ": " + e.Message, e); } }
}
