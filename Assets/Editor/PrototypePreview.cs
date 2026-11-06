using System;
using UnityEngine;
using UnityEditor;

// FHD Game 창을 여는 편집 도구. 게임 실행 파일에는 포함되지 않습니다.
public static class PrototypePreview
{
    [MenuItem("Tools/Yeolguk Train/FHD Preview")]
    public static void Open() { ShowGame(); }
    public static string ShowGame()
    {
        var assembly = typeof(EditorWindow).Assembly;
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes = singleton.GetProperty("instance").GetValue(null);
        var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
        var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
        var groupClass = group.GetType();
        var sizeType = assembly.GetType("UnityEditor.GameViewSize");
        int total = (int)groupClass.GetMethod("GetTotalCount").Invoke(group, null);
        int index = -1;
        for (int i = 0; i < total; i++)
        {
            var item = groupClass.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
            if ((int)sizeType.GetProperty("width").GetValue(item) == 1920 &&
                (int)sizeType.GetProperty("height").GetValue(item) == 1080) { index = i; break; }
        }
        if (index < 0)
        {
            var mode = Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution");
            var size = Activator.CreateInstance(sizeType, new object[] { mode, 1920, 1080, "Prototype FHD" });
            groupClass.GetMethod("AddCustomSize").Invoke(group, new[] { size });
            index = total;
        }
        var type = assembly.GetType("UnityEditor.GameView");
        var window = EditorWindow.GetWindow(type);
        type.GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(window, index);
        window.Show();
        window.Focus();
        window.Repaint();
        EditorApplication.QueuePlayerLoopUpdate();
        return "Game view: 1920x1080, selected index " + index;
    }
}

