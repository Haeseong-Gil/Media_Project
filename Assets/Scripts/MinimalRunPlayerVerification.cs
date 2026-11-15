#if UNITY_EDITOR || DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 개발 빌드에 명시적인 실행 인자를 줄 때만 작동한다. 보통 실행에서는 생성되지 않는다.
public sealed class MinimalRunPlayerVerification : MonoBehaviour
{
    private string output;
    private readonly List<string> passed = new List<string>();
    private readonly List<string> errors = new List<string>();
    private MinimalRunScreen screen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!Application.isEditor && !Debug.isDebugBuild) return;
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "--verify-minimal-run");
        if (index < 0) return;
        var probe = new GameObject("PlayerVerification").AddComponent<MinimalRunPlayerVerification>();
        probe.output = index + 1 < args.Length ? args[index + 1] : Path.Combine(Application.persistentDataPath, "Verification");
    }

    private IEnumerator Start()
    {
        Application.runInBackground = true;
        Time.timeScale = 0;
        Directory.CreateDirectory(output);
        Application.logMessageReceived += CaptureError;
        IEnumerator steps = Verify();
        Exception failure = null;
        while (true)
        {
            bool next;
            try { next = steps.MoveNext(); }
            catch (Exception error) { failure = error; break; }
            if (!next) break;
            yield return steps.Current;
        }
        if (failure == null && errors.Count > 0) failure = new Exception(string.Join("\n", errors));
        string result = (failure == null ? "PASS" : "FAIL: " + failure) +
            $"\nUnity {Application.unityVersion}; {Screen.width}x{Screen.height}; development={Debug.isDebugBuild}" +
            "\n" + string.Join("\n", passed);
        File.WriteAllText(Path.Combine(output, "player-result.txt"), result);
        Application.logMessageReceived -= CaptureError;
        Debug.Log(result);
        Application.Quit(failure == null ? 0 : 1);
    }

    private IEnumerator Verify()
    {
        yield return new WaitForEndOfFrame();
        screen = FindAnyObjectByType<MinimalRunScreen>();
        Require(screen != null && screen.IsAtTitle, "PrototypeMain title");
        Snapshot("01-title");
        Click("TitleOverlay/Body/StartButton");
        var model = screen.Model;
        Require(model.People == 6 && model.Food == 25 && model.Fuel == 100, "Start values 6/25/100");
        yield return new WaitForEndOfFrame();
        Snapshot("02-general");

        var hand = screen.transform.Find("TrainArea/PocketWatch/MinuteHand");
        Click("ControlArea/DriveButton");
        model.Advance(20);
        yield return new WaitForEndOfFrame();
        Require(model.Day == 1 && model.Food == 13 && Quaternion.Angle(hand.localRotation, Quaternion.Euler(0, 0, -120)) < 0.1f,
            "First meal and clock at 20s");
        model.Advance(model.ZoneDuration - model.ZoneElapsed);
        Click("ControlArea/DriveButton");
        Require(model.AtStation && !model.Moving && model.Fuel == 89 && model.Food == 1 &&
            Quaternion.Angle(hand.localRotation, Quaternion.Euler(0, 0, -240)) < 0.1f, "40s station, second meal, fuel and clock");
        Click("ControlArea/DispatchButton"); Click("ControlArea/ReturnButton");
        Require(model.PendingSurvivors == 5, "First station return opens choice");
        yield return new WaitForEndOfFrame();
        Snapshot("03-choice");
        Click("ChoiceToggleButton");
        yield return new WaitForEndOfFrame();
        double elapsed = model.Elapsed, day = model.DayElapsed, food = model.Food, fuel = model.Fuel;
        model.Advance(10000); screen.Drive(); screen.Dispatch(); screen.IncreaseCrew();
        Require(!screen.ChoiceVisible && model.ChoicePaused && model.Elapsed == elapsed && model.DayElapsed == day &&
            model.Food == food && model.Fuel == fuel && model.Team == 0 && !model.Moving, "Hidden choice freezes simulation and commands");
        Snapshot("04-hidden");
        Click("ChoiceToggleButton");
        yield return new WaitForEndOfFrame();
        Click("SurvivorOverlay/Body/AcceptButton");
        Require(model.People == 11 && !model.ChoicePaused, "Accept 5 and resume");
        yield return new WaitForEndOfFrame();
        for (int i = 0; i < 7; i++) Click("ControlArea/PlusButton");
        Click("ControlArea/DispatchButton");
        Require(model.Team == 10 && model.CurrentBonus == 1.5, "Choose and dispatch 10 with reserve and bonus");
        model.Advance(3); screen.Drive();
        Require(!model.Moving && model.CarriedFood > 0 && model.CarriedFuel > 0, "Search gains and departure blocked while team outside");
        yield return new WaitForEndOfFrame();
        Snapshot("05-search");
        double stockFood = model.Food, carryFood = model.CarriedFood;
        Click("ControlArea/ReturnButton");
        Require(Math.Abs(model.Food - stockFood - carryFood) < 0.00001 && model.Team == 0 && model.PendingSurvivors == 0, "Return deposits once; event does not repeat");
        model.Advance(10000); screen.Drive();
        Require(model.Succeeded && model.Day == 4 && model.Elapsed == 180, "Survive three days and finish at 180s");
        yield return new WaitForEndOfFrame();
        Snapshot("06-success");
        Click("FailureOverlay/Body/TitleButton");
        Require(screen.IsAtTitle, "Result returns to title");
        yield return new WaitForEndOfFrame();
        Click("TitleOverlay/Body/StartButton");
        Require(screen.Model != model && screen.Model.People == 6 && screen.Model.Food == 25 && screen.Model.Fuel == 100 &&
            screen.Model.Elapsed == 0 && screen.Model.HungerScore == 0 && !screen.Model.SurvivorEventOccurred, "New RUN resets all state");
        yield return new WaitForEndOfFrame();

        Click("ControlArea/DriveButton"); screen.Model.Advance(screen.Model.ZoneDuration); Click("ControlArea/DriveButton");
        Click("ControlArea/DispatchButton"); Click("ControlArea/ReturnButton");
        yield return new WaitForEndOfFrame();
        Click("SurvivorOverlay/Body/RejectButton");
        Require(screen.Model.People == 6 && !screen.Model.ChoicePaused && screen.Model.SurvivorEventOccurred, "Reject preserves population and resumes");
        double before = screen.Model.Elapsed; screen.Model.Advance(1);
        Require(screen.Model.Elapsed == before + 1, "Time progresses after decision");
        yield return new WaitForEndOfFrame();
        Snapshot("07-rejected");
    }

    private void Snapshot(string name)
    {
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG());
        Destroy(image);
        foreach (Text label in screen.GetComponentsInChildren<Text>(false))
        {
            if (label.font == null) throw new Exception("Missing font: " + label.name);
            if (label.preferredHeight > label.rectTransform.rect.height + 2)
                throw new Exception("Text exceeds its box: " + label.name + " / " + label.text);
        }
    }

    private void Click(string path)
    {
        Canvas.ForceUpdateCanvases();
        var go = GameObject.Find("MinimalRunCanvas/" + path);
        if (go == null) throw new Exception("Missing button: " + path);
        var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, go.GetComponent<RectTransform>().TransformPoint(go.GetComponent<RectTransform>().rect.center)) };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
        if (hits.Count == 0 || hits[0].gameObject != go || !go.GetComponent<Button>().interactable)
            throw new Exception("Blocked button: " + path + "; top=" + (hits.Count > 0 ? hits[0].gameObject.name : "none"));
        ExecuteEvents.Execute(go, data, ExecuteEvents.pointerClickHandler);
    }

    private void Require(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        passed.Add(name);
    }

    private void CaptureError(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + trace);
    }
}
#endif
