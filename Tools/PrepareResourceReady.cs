using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PrepareResourceReady
{
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play를 먼저 종료하세요.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/PrototypeMain.unity" || scene.isDirty)
            throw new InvalidOperationException("저장된 PrototypeMain 씬에서 실행하세요.");
        var root = GameObject.Find("MinimalRunCanvas");
        if (root == null || root.GetComponent<MinimalRunPresentation>() != null)
            throw new InvalidOperationException("대상 씬을 확인하세요. 이미 적용했다면 반복하지 않습니다.");
        var view = root.AddComponent<MinimalRunPresentation>();
        var so = new SerializedObject(view);
        so.FindProperty("screen").objectReferenceValue = root.GetComponent<MinimalRunScreen>();
        so.FindProperty("backgroundTarget").objectReferenceValue = root.transform.Find("TrainArea/Horizon").GetComponent<Image>();
        so.FindProperty("artNotice").objectReferenceValue = root.transform.Find("TrainArea/ArtNotice").gameObject;
        var targets = so.FindProperty("trainTargets"); targets.arraySize = 3;
        var labels = so.FindProperty("placeholderLabels"); labels.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            var train = root.transform.Find("TrainArea/TrainPlaceholder/Carriage" + i);
            targets.GetArrayElementAtIndex(i).objectReferenceValue = train.GetComponent<Image>();
            labels.GetArrayElementAtIndex(i).objectReferenceValue = train.Find("Label").gameObject;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        var backdrop = root.transform.Find("TrainArea/Horizon").GetComponent<RectTransform>();
        backdrop.anchoredPosition = new Vector2(0, -240);
        backdrop.sizeDelta = new Vector2(1920, 340);
        root.transform.Find("TrainArea/TrainPlaceholder").GetComponent<Image>().color = Color.clear;
        // 그림 사이의 빈 간격은 배경이 보이게 한다. 게임 수치와 이벤트는 건드리지 않는다.
        view.RefreshVisuals();
        PlayerSettings.productName = "Yeolguk Train Prototype";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = false;
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("씬 저장 실패");
        AssetDatabase.SaveAssets();
        return "리소스 연결 5칸과 지정 글꼴 지원, Windows 창 모드 설정 저장 완료";
    }
}
