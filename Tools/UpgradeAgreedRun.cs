using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 기존 2:1 화면을 보존하고 결정창·타이틀·실패 화면을 더하는 1회 도구.
public static class UpgradeAgreedRun
{
    private static Font font;
    public static string Build()
    {
        const string path = "Assets/Scenes/PrototypeMain.unity";
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play를 먼저 종료하세요.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("미저장 씬 변경을 먼저 확인하세요.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != path) scene = EditorSceneManager.OpenScene(path);
        var root = GameObject.Find("MinimalRunCanvas");
        if (root == null) throw new InvalidOperationException("기존 MinimalRunCanvas를 찾을 수 없습니다.");
        if (root.transform.Find("TitleOverlay") != null) throw new InvalidOperationException("새 UI가 이미 있습니다. 덮어쓰지 않습니다.");
        var controller = root.GetComponent<MinimalRunScreen>();
        var so = new SerializedObject(controller);
        var defaults = new MinimalRunTuning();
        var tuning = so.FindProperty("tuning");
        foreach (var field in typeof(MinimalRunTuning).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var property = tuning.FindPropertyRelative(field.Name);
            if (field.FieldType == typeof(int)) property.intValue = (int)field.GetValue(defaults);
            else if (field.FieldType == typeof(double)) property.doubleValue = (double)field.GetValue(defaults);
        }
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        root.transform.Find("TrainArea/Title").GetComponent<Text>().text = "열국열차  /  최소 RUN";
        root.transform.Find("TrainArea/ArtNotice").GetComponent<Text>().text = "프로토타입 시험 설정 · 임시 도형";
        root.transform.Find("ControlArea/AcceptButton").gameObject.SetActive(false);
        root.transform.Find("ControlArea/RejectButton").gameObject.SetActive(false);
        Label(root.transform.Find("ControlArea"), "SelectionRules", "전원 파견 불가\n탐사대 2명 이하 즉시 귀환", 1320, 174, 568, 68, 25);

        var title = Panel(root.transform, "TitleOverlay", 0, 0, 1920, 1080, new Color32(12, 20, 28, 255), true);
        var titleBody = Panel(title, "Body", 360, 240, 1200, 560, new Color32(30, 44, 55, 255));
        Label(titleBody, "Heading", "열국열차", 60, 260, 1080, 80, 48).alignment = TextAnchor.MiddleCenter;
        Text ignored;
        var start = Button(titleBody, "StartButton", "시작", 430, 408, 340, out ignored);
        UnityEventTools.AddPersistentListener(start.onClick, controller.StartNewRun);

        var choice = Panel(root.transform, "SurvivorOverlay", 0, 0, 1920, 1080, new Color(0.02f, 0.04f, 0.07f, 0.72f), true);
        var choiceBody = Panel(choice, "Body", 420, 280, 1080, 540, new Color32(30, 44, 55, 255));
        Label(choiceBody, "Heading", "생존자 수용 판단 · 게임 정지", 50, 34, 980, 70, 36);
        var details = Label(choiceBody, "Details", "생존자 5명", 50, 128, 980, 220, 29);
        var accept = Button(choiceBody, "AcceptButton", "5명 수용", 150, 414, 340, out ignored);
        var reject = Button(choiceBody, "RejectButton", "거절", 590, 414, 340, out ignored);
        UnityEventTools.AddPersistentListener(accept.onClick, controller.Accept);
        UnityEventTools.AddPersistentListener(reject.onClick, controller.Reject);

        var failure = Panel(root.transform, "FailureOverlay", 0, 0, 1920, 1080, new Color(0.04f, 0.02f, 0.03f, 0.88f), true);
        var failBody = Panel(failure, "Body", 360, 280, 1200, 520, new Color32(53, 35, 42, 255));
        Label(failBody, "Heading", "생존 실패 · RUN 종료", 60, 40, 1080, 80, 42);
        var failDetails = Label(failBody, "Details", "굶주림 7일차", 60, 150, 1080, 190, 29);
        var back = Button(failBody, "TitleButton", "타이틀로 돌아가기", 400, 392, 400, out ignored);
        UnityEventTools.AddPersistentListener(back.onClick, controller.ReturnToTitle);

        Text toggleLabel;
        var toggle = Button(root.transform, "ChoiceToggleButton", "상태 보기 · 게임 정지", 1400, 24, 480, out toggleLabel);
        UnityEventTools.AddPersistentListener(toggle.onClick, controller.ToggleChoiceVisibility);
        Set(so, "titlePanel", title.gameObject);
        Set(so, "survivorPanel", choice.gameObject); Set(so, "survivorDetails", details);
        Set(so, "choiceToggleButton", toggle); Set(so, "choiceToggleLabel", toggleLabel);
        Set(so, "failurePanel", failure.gameObject); Set(so, "failureDetails", failDetails);
        Set(so, "acceptButton", accept); Set(so, "rejectButton", reject);
        so.ApplyModifiedPropertiesWithoutUndo();
        choice.gameObject.SetActive(false); failure.gameObject.SetActive(false); toggle.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("씬 저장 실패");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        Selection.activeGameObject = root;
        return "합의한 시험값·타이틀·정지 상태 보기·실패 화면 저장 완료";
    }

    private static void Set(SerializedObject so, string name, UnityEngine.Object value) { so.FindProperty(name).objectReferenceValue = value; }
    private static RectTransform Panel(Transform parent, string name, float x, float y, float w, float h, Color color, bool block = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Place(go.GetComponent<RectTransform>(), parent, x, y, w, h);
        var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = block;
        return go.GetComponent<RectTransform>();
    }
    private static Text Label(Transform parent, string name, string value, float x, float y, float w, float h, int size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        Place(go.GetComponent<RectTransform>(), parent, x, y, w, h);
        var text = go.GetComponent<Text>(); text.font = font; text.fontSize = size;
        text.text = value; text.color = new Color32(234, 240, 242, 255); text.raycastTarget = false;
        return text;
    }
    private static Button Button(Transform parent, string name, string value, float x, float y, float w, out Text label)
    {
        var rect = Panel(parent, name, x, y, w, 72, new Color32(47, 111, 128, 255), true);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
        var colors = button.colors; colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 1); button.colors = colors;
        label = Label(rect, "Label", value, 4, 4, w - 8, 64, 28); label.alignment = TextAnchor.MiddleCenter;
        return button;
    }
    private static void Place(RectTransform rect, Transform parent, float x, float y, float w, float h)
    {
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
    }
}
