using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MinimalRunResourceChecks
{
    [MenuItem("Tools/Yeolguk Train/Verify Resource Slots")]
    public static void Verify()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Play를 종료한 뒤 검사하세요.");
        var view = UnityEngine.Object.FindAnyObjectByType<MinimalRunPresentation>();
        if (view == null) throw new Exception("리소스 연결 컴포넌트가 없습니다.");
        var texture = new Texture2D(8, 8);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f));
        string[] names = { "generalBackground", "stationBackground", "locomotive", "carriageOne", "carriageTwo" };
        var fields = new FieldInfo[names.Length];
        var previous = new object[names.Length];
        for (int i = 0; i < names.Length; i++) { fields[i] = typeof(MinimalRunPresentation).GetField(names[i], BindingFlags.Instance | BindingFlags.NonPublic); previous[i] = fields[i].GetValue(view); }
        try
        {
            for (int i = 0; i < names.Length; i++) fields[i].SetValue(view, sprite);
            view.RefreshVisuals();
            var root = view.transform;
            if (root.Find("TrainArea/Horizon").GetComponent<Image>().sprite != sprite) throw new Exception("배경 그림 연결 실패");
            for (int i = 0; i < 3; i++)
            {
                var train = root.Find("TrainArea/TrainPlaceholder/Carriage" + i);
                if (train.GetComponent<Image>().sprite != sprite || !train.GetComponent<Image>().preserveAspect || train.Find("Label").gameObject.activeSelf)
                    throw new Exception("기차 그림 연결 실패: " + i);
            }
            for (int i = 0; i < names.Length; i++) fields[i].SetValue(view, null);
            view.RefreshVisuals();
            for (int i = 0; i < 3; i++)
                if (!root.Find("TrainArea/TrainPlaceholder/Carriage" + i + "/Label").gameObject.activeSelf) throw new Exception("임시 도형 복원 실패");
        }
        finally
        {
            for (int i = 0; i < names.Length; i++) fields[i].SetValue(view, previous[i]);
            view.RefreshVisuals();
            UnityEngine.Object.DestroyImmediate(sprite);
            UnityEngine.Object.DestroyImmediate(texture);
        }
        const string result = "PASS resource slots: background, 3 train sprites, aspect preservation, placeholder labels and fallback restoration";
        Directory.CreateDirectory("Temp"); File.WriteAllText("Temp/MinimalRunResourceChecks.txt", result); Debug.Log(result);
    }
}
