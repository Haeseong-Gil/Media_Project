using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MinimalRunBuildTools
{
    [MenuItem("Tools/Yeolguk Train/Prepare Windows Player")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play를 먼저 종료하세요.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/PrototypeMain.unity" || scene.isDirty)
            throw new InvalidOperationException("저장된 PrototypeMain 씬에서 실행하세요.");
        var renderer = AssetDatabase.LoadMainAssetAtPath("Assets/Settings/Renderer2D.asset");
        var so = new SerializedObject(renderer);
        var post = so.FindProperty("m_PostProcessData");
        if (post == null) throw new InvalidOperationException("2D 렌더러 설정을 확인하세요.");
        // 최소 플레이는 후처리 효과를 쓰지 않는다. 사용하지 않는 후처리 패스의 셰이더 경고를 막는다.
        post.objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(renderer);
        // 현재 글꼴에서도 탐사 정보의 마지막 보너스 줄까지 들어가게 한다.
        var team = GameObject.Find("MinimalRunCanvas/ControlArea/TeamPanel/Values").GetComponent<Text>();
        team.fontSize = 24;
        team.rectTransform.sizeDelta = new Vector2(496, 178);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("씬 저장 실패");
        AssetDatabase.SaveAssets();
        Debug.Log("Windows 최소 플레이: 사용하지 않는 2D 후처리 비활성화");
    }
}
