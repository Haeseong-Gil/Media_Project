using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Unity CLI에서 한 번 실행해 기존 씬에 편집 가능한 UI를 저장하는 제작 도구.
// Assets 밖에 있어 게임에 포함되지 않는다. 재실행 시 기존 UI를 덮어쓰지 않는다.
public static class BuildFirstScreen
{
    private static Font font;
    private static readonly Color Ink = new Color32(229, 234, 238, 255);

    public static string Build()
    {
        const string path = "Assets/Scenes/PrototypeMain.unity";
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Play 모드를 먼저 종료해야 합니다.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("열린 씬에 저장하지 않은 변경이 있습니다. 먼저 확인하세요.");

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != path) scene = EditorSceneManager.OpenScene(path);
        if (GameObject.Find("PrototypeCanvas") != null)
            throw new InvalidOperationException("PrototypeCanvas가 이미 있습니다. 덮어쓰지 않습니다.");
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
            throw new InvalidOperationException("기존 EventSystem을 먼저 확인하세요.");

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var root = new GameObject("PrototypeCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PrototypeReportScreen));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var upper = Panel(root.transform, "TrainArea", 0, 0, 1920, 432, new Color32(74, 53, 44, 255));
        Label(upper, "Title", "열국열차  /  폐역 정차", 48, 24, 1300, 60, 36);
        Label(upper, "ArtNotice", "임시 화면 · 그림 적용 전", 1430, 30, 440, 48, 26);
        Panel(upper, "Station", 1320, 140, 370, 180, new Color32(99, 84, 70, 255));
        Label(upper, "StationName", "폐역", 1430, 160, 200, 55, 32);
        Panel(upper, "Rail", 0, 370, 1920, 14, new Color32(31, 30, 33, 255));
        var train = Panel(upper, "TrainPlaceholder", 240, 208, 1440, 144, new Color32(39, 50, 59, 255));
        Panel(train, "Locomotive", 0, 0, 470, 144, new Color32(91, 112, 120, 255));
        Panel(train, "Carriage1", 486, 0, 470, 144, new Color32(69, 89, 99, 255));
        Panel(train, "Carriage2", 972, 0, 468, 144, new Color32(69, 89, 99, 255));
        Label(train, "TrainLabel", "기관차                         객차 1                         객차 2", 40, 44, 1360, 56, 32);

        var lower = Panel(root.transform, "ManagementArea", 0, 432, 1920, 648, new Color32(18, 25, 33, 255));
        var resources = Panel(lower, "Resources", 40, 24, 1840, 88, new Color32(32, 44, 55, 255));
        Label(resources, "ResourceValues", "에너지  50 / 100       식량  18  (0.6일분)       주민  30명       신뢰  60 / 100", 28, 16, 1784, 56, 30);
        var explorer = Panel(lower, "ExplorerPanel", 40, 136, 540, 272, new Color32(27, 38, 49, 255));
        Label(explorer, "ExplorerTitle", "탐사자 1명 · 기차에서 대기", 24, 18, 492, 50, 30);
        Label(explorer, "ExplorerValues", "운반 식량  0\n노출  0 / 100\n탐사 진행 전", 24, 86, 492, 160, 28);
        var reportPanel = Panel(lower, "ReportPanel", 604, 136, 1276, 272, new Color32(27, 38, 49, 255));
        Label(reportPanel, "ReportTitle", "상황 보고", 28, 18, 1220, 48, 30);
        var report = Label(reportPanel, "ReportText", "시연 준비가 끝났습니다. 시작 버튼을 눌러 폐역의 상황을 확인하세요.", 28, 88, 1220, 154, 30);
        Label(lower, "WeatherAndRisk", "기후  극고온       좀비 위험  20 / 100", 48, 432, 1280, 60, 30);
        Label(lower, "WaitingNotice", "설명 및 선택 대기 중 — 시간이 멈춰 있습니다.", 48, 508, 1270, 54, 28);
        Label(lower, "ScopeNotice", "첫 구현: 시작 → 상황 보고  /  타이머·자원 변화는 후속 작업", 48, 576, 1370, 44, 24);

        var buttonRect = Panel(lower, "StartButton", 1480, 448, 400, 104, new Color32(49, 121, 136, 255));
        var buttonImage = buttonRect.GetComponent<Image>();
        buttonImage.raycastTarget = true;
        var button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1);
        button.colors = colors;
        var buttonLabel = Label(buttonRect, "Label", "시작", 0, 0, 400, 104, 32);
        buttonLabel.alignment = TextAnchor.MiddleCenter;

        var controller = root.GetComponent<PrototypeReportScreen>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("reportText").objectReferenceValue = report;
        serialized.FindProperty("startButton").objectReferenceValue = button;
        serialized.FindProperty("buttonText").objectReferenceValue = buttonLabel;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(button.onClick, controller.StartDemo);

        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("씬 저장 실패");
        Selection.activeGameObject = root;
        return "PrototypeMain 저장 완료: FHD Canvas, 40/60 영역, 고정 수치, 시작 버튼과 보고 연결";
    }

    private static RectTransform Panel(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        Place(rect, parent, x, y, w, h);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Image>().raycastTarget = false;
        return rect;
    }

    private static Text Label(Transform parent, string name, string content, float x, float y, float w, float h, int size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        Place(go.GetComponent<RectTransform>(), parent, x, y, w, h);
        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = Ink;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        text.text = content;
        return text;
    }

    private static void Place(RectTransform rect, Transform parent, float x, float y, float w, float h)
    {
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(w, h);
    }
}
