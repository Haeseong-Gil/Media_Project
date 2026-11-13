using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// v0.4의 '이번에 제작할 최소 플레이'만 반영하는 1회 제작 도구.
public static class UpgradeMinimalPlay
{
    private static Font font;
    public static string Build()
    {
        const string path = "Assets/Scenes/PrototypeMain.unity";
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play를 먼저 종료하세요.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("열린 씬의 미저장 변경을 먼저 확인하세요.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != path) scene = EditorSceneManager.OpenScene(path);
        if (GameObject.Find("MinimalRunCanvas") != null) throw new InvalidOperationException("최소 플레이 UI가 이미 있습니다. 덮어쓰지 않습니다.");
        var old = GameObject.Find("PrototypeCanvas");
        if (old != null) old.SetActive(false); // 삭제하지 않고 첫 구현을 보존한다.
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var root = new GameObject("MinimalRunCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MinimalRunScreen));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var upper = Panel(root.transform, "TrainArea", 0, 0, 1920, 720, new Color32(52, 62, 68, 255));
        Label(upper, "Title", "열국열차  /  최소 플레이 v0.4", 40, 28, 1300, 56, 36);
        Label(upper, "ArtNotice", "임시 도형 · 모든 수치는 조정용", 1350, 38, 530, 44, 25);
        var zone = Label(upper, "Zone", "일반 구역 · 정차 중", 40, 112, 1800, 50, 30);
        var clock = Label(upper, "Clock", "시작 전 · 시간 정지", 40, 176, 1800, 48, 28);
        Panel(upper, "Horizon", 0, 298, 1920, 220, new Color32(65, 72, 72, 255));
        var train = Panel(upper, "TrainPlaceholder", 240, 352, 1440, 180, new Color32(26, 37, 46, 255));
        for (int i = 0; i < 3; i++)
        {
            var carriage = Panel(train, "Carriage" + i, i * 486, 0, i == 2 ? 468 : 470, 180, new Color32(78, 100, 111, 255));
            var label = Label(carriage, "Label", i == 0 ? "기관차" : "객차 " + i, 20, 64, 420, 60, 32);
            label.alignment = TextAnchor.MiddleCenter;
        }
        Panel(upper, "Rail", 0, 548, 1920, 14, new Color32(22, 28, 32, 255));
        var resources = Panel(upper, "Resources", 32, 610, 1856, 82, new Color32(27, 39, 49, 255));
        var resourceText = Label(resources, "Values", "식량 · 연료 · 전체 인원", 24, 18, 1808, 50, 30);

        var lower = Panel(root.transform, "ControlArea", 0, 720, 1920, 360, new Color32(16, 23, 31, 255));
        var reportPanel = Panel(lower, "ReportPanel", 32, 20, 712, 248, new Color32(27, 38, 49, 255));
        Label(reportPanel, "Title", "상황 보고", 20, 12, 672, 44, 28);
        var report = Label(reportPanel, "ReportText", "RUN 시작을 누르면 시간이 흐릅니다.", 20, 64, 672, 166, 26);
        var expedition = Panel(lower, "TeamPanel", 764, 20, 536, 248, new Color32(27, 38, 49, 255));
        Label(expedition, "Title", "탐사 · 한 팀 운용", 20, 12, 496, 44, 28);
        var team = Label(expedition, "Values", "탐사대 없음\n운반 식량 0\n운반 연료 0", 20, 64, 496, 166, 26);

        Text driveLabel;
        var drive = MakeButton(lower, "DriveButton", "RUN 시작", 1320, 20, 268, out driveLabel);
        Text ignored;
        var dispatch = MakeButton(lower, "DispatchButton", "탐사 파견", 1608, 20, 280, out ignored);
        var minus = MakeButton(lower, "MinusButton", "−", 1320, 96, 64, out ignored);
        var crew = Label(lower, "CrewCount", "1명", 1392, 104, 124, 48, 25);
        crew.alignment = TextAnchor.MiddleCenter;
        var plus = MakeButton(lower, "PlusButton", "+", 1524, 96, 64, out ignored);
        var ret = MakeButton(lower, "ReturnButton", "즉시 귀환", 1608, 96, 280, out ignored);
        var accept = MakeButton(lower, "AcceptButton", "생존자 수용", 1320, 172, 268, out ignored);
        var reject = MakeButton(lower, "RejectButton", "거절", 1608, 172, 280, out ignored);
        var survivors = Label(lower, "Survivors", "생존자 요청 없음", 1320, 242, 568, 42, 24);
        var notice = Label(lower, "TemporaryRules", "임시 조정값 사용 · 신뢰와 좀비 위험은 제외", 32, 296, 1856, 48, 25);

        var controller = root.GetComponent<MinimalRunScreen>();
        var so = new SerializedObject(controller);
        Set(so, "zoneText", zone); Set(so, "clockText", clock); Set(so, "resourcesText", resourceText);
        Set(so, "reportText", report); Set(so, "teamText", team); Set(so, "crewText", crew);
        Set(so, "survivorsText", survivors); Set(so, "driveLabel", driveLabel); Set(so, "noticeText", notice);
        Set(so, "driveButton", drive); Set(so, "dispatchButton", dispatch); Set(so, "returnButton", ret);
        Set(so, "minusButton", minus); Set(so, "plusButton", plus); Set(so, "acceptButton", accept); Set(so, "rejectButton", reject);
        so.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(drive.onClick, controller.Drive);
        UnityEventTools.AddPersistentListener(dispatch.onClick, controller.Dispatch);
        UnityEventTools.AddPersistentListener(ret.onClick, controller.Return);
        UnityEventTools.AddPersistentListener(minus.onClick, controller.DecreaseCrew);
        UnityEventTools.AddPersistentListener(plus.onClick, controller.IncreaseCrew);
        UnityEventTools.AddPersistentListener(accept.onClick, controller.Accept);
        UnityEventTools.AddPersistentListener(reject.onClick, controller.Reject);
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("씬 저장 실패");
        Selection.activeGameObject = root;
        return "v0.4 최소 플레이 저장: 1920×1080, 720:360, 기존 화면은 비활성으로 보존";
    }

    private static void Set(SerializedObject so, string name, UnityEngine.Object obj) { so.FindProperty(name).objectReferenceValue = obj; }
    private static RectTransform Panel(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Place(go.GetComponent<RectTransform>(), parent, x, y, w, h);
        var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
        return go.GetComponent<RectTransform>();
    }
    private static Text Label(Transform parent, string name, string content, float x, float y, float w, float h, int size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        Place(go.GetComponent<RectTransform>(), parent, x, y, w, h);
        var text = go.GetComponent<Text>(); text.font = font; text.fontSize = size;
        text.color = new Color32(232, 237, 239, 255); text.text = content; text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }
    private static Button MakeButton(Transform parent, string name, string title, float x, float y, float width, out Text label)
    {
        var rect = Panel(parent, name, x, y, width, 64, new Color32(46, 109, 126, 255));
        var image = rect.GetComponent<Image>(); image.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.disabledColor = new Color(0.42f, 0.42f, 0.42f, 1); button.colors = colors;
        label = Label(rect, "Label", title, 4, 4, width - 8, 56, 27); label.alignment = TextAnchor.MiddleCenter;
        return button;
    }
    private static void Place(RectTransform rect, Transform parent, float x, float y, float w, float h)
    {
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
    }
}
