using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 원본 PNG는 수정하지 않는다. Sprite Editor 영역과 기존 UI의 그림·배치만 설정한다.
public static class ApplyCoreArt
{
    private const string Folder = "Assets/Art/Core/";

    [MenuItem("Tools/Yeolguk Train/Apply Core Art")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play를 먼저 종료하세요.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/PrototypeMain.unity" || scene.isDirty)
            throw new InvalidOperationException("미저장 변경이 없는 PrototypeMain 씬에서 실행하세요.");
        var root = GameObject.Find("MinimalRunCanvas");
        if (root == null) throw new InvalidOperationException("MinimalRunCanvas가 없습니다.");

        // Rect의 원점은 PNG 아래쪽이다. 기차는 투명 여백을 제외한 그림 주변에 2px를 남긴다.
        var background = Import("GeneralWasteland", new Rect(0, 125, 2048, 363));
        var landscape = Import("GeneralWastelandLoop", new Rect(0, 125, 2304, 363));
        var locomotive = Import("Locomotive", new Rect(26, 99, 1731, 667));
        var middle = Import("CarriageMiddle", new Rect(25, 81, 1720, 635));
        var rear = Import("CarriageRear", new Rect(24, 81, 1726, 639));
        var presentation = root.GetComponent<MinimalRunPresentation>();
        var so = new SerializedObject(presentation);
        Set(so, "generalBackground", background);
        Set(so, "scrollingLandscape", landscape);
        Set(so, "locomotive", locomotive); Set(so, "carriageOne", middle); Set(so, "carriageTwo", rear);

        var upper = root.transform.Find("TrainArea");
        var rail = upper.Find("Rail").GetComponent<RectTransform>();
        Set(so, "railPlaceholder", rail.gameObject);
        so.ApplyModifiedPropertiesWithoutUndo();

        const float scale = 0.28f;
        var train = upper.Find("TrainPlaceholder").GetComponent<RectTransform>();
        Sprite[] sprites = { locomotive, middle, rear };
        const float couplerOverlap = 4f;
        float trainWidth = sprites.Sum(sprite => sprite.rect.width * scale) - couplerOverlap * (sprites.Length - 1);
        Place(train, (1920 - trainWidth) / 2f, 345, trainWidth, 200);

        // 기관차를 오른쪽 끝에 두고 뒤따르는 객차를 왼쪽으로 붙인다.
        float right = trainWidth;
        for (int i = 0; i < sprites.Length; i++)
        {
            var rect = train.Find("Carriage" + i).GetComponent<RectTransform>();
            float width = sprites[i].rect.width * scale, height = sprites[i].rect.height * scale;
            Place(rect, right - width, 200 - height, width, height);
            right -= width - couplerOverlap;
        }
        Place(rail, 0, 529, 1920, 14);
        presentation.RefreshVisuals();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("씬 저장 실패");
        AssetDatabase.SaveAssets();
        Debug.Log("반복 배경과 기차 그림 연결 완료.");
    }

    private static Sprite Import(string name, Rect rect)
    {
        string path = Folder + name + ".png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("이미지를 가져오지 못했습니다: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var data = factory.GetSpriteEditorDataProviderFromObject(importer);
        data.InitSpriteEditorDataProvider();
        var previous = data.GetSpriteRects();
        GUID id = previous.Length == 1 && previous[0].name == name ? previous[0].spriteID : GUID.Generate();
        var spriteRect = new SpriteRect { name = name, rect = rect, pivot = new Vector2(0.5f, 0),
            alignment = SpriteAlignment.Custom, spriteID = id };
        data.SetSpriteRects(new[] { spriteRect });
        data.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(new[] { new SpriteNameFileIdPair(name, id) });
        data.Apply(); importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
    }

    private static void Set(SerializedObject so, string field, UnityEngine.Object value)
        => so.FindProperty(field).objectReferenceValue = value;

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
    }
}
