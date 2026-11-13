using UnityEngine;
using UnityEngine.UI;

// 게임 규칙을 바꾸지 않고 구역과 그림을 연결한다. 빈 항목은 기존 임시 도형으로 표시한다.
[DisallowMultipleComponent]
public sealed class MinimalRunPresentation : MonoBehaviour
{
    [Header("나중에 넣을 그림 · Sprite (2D and UI)")]
    [SerializeField] private Sprite generalBackground;
    [SerializeField] private Sprite stationBackground;
    [SerializeField] private Sprite locomotive;
    [SerializeField] private Sprite carriageOne;
    [SerializeField] private Sprite carriageTwo;

    [Header("연결된 화면 · 보통 수정하지 않음")]
    [SerializeField] private MinimalRunScreen screen;
    [SerializeField] private Image backgroundTarget;
    [SerializeField] private Image stationPlatform;
    [SerializeField] private Image[] trainTargets;
    [SerializeField] private GameObject[] placeholderLabels;
    [SerializeField] private GameObject artNotice;
    [SerializeField] private GameObject railPlaceholder;

    private readonly Color generalColor = new Color32(65, 72, 72, 255);
    private readonly Color stationColor = new Color32(76, 64, 53, 255);
    private readonly Color trainColor = new Color32(78, 100, 111, 255);
    private bool? previousStation;
    private bool? previousChoice;

    private void Start() { RefreshVisuals(); }

    private void LateUpdate()
    {
        if (screen == null || screen.Model == null) return;
        if (previousStation != screen.Model.AtStation || previousChoice != screen.Model.ChoicePaused)
            RefreshVisuals();
    }

    public void RefreshVisuals()
    {
        bool atStation = screen != null && screen.Model != null && screen.Model.AtStation;
        bool pending = screen != null && screen.Model != null && screen.Model.ChoicePaused;
        previousStation = atStation; previousChoice = pending;
        Sprite background = atStation ? stationBackground : generalBackground;
        if (stationPlatform != null)
        {
            bool showPlatform = atStation && stationBackground != null;
            stationPlatform.gameObject.SetActive(showPlatform);
            if (showPlatform)
            {
                background = generalBackground;
                SetImage(stationPlatform, stationBackground, stationColor, true);
            }
        }
        SetImage(backgroundTarget, background, atStation ? stationColor : generalColor, true);
        if (railPlaceholder != null)
            railPlaceholder.SetActive(background == null);
        Sprite[] sprites = { locomotive, carriageOne, carriageTwo };
        for (int i = 0; trainTargets != null && i < trainTargets.Length && i < sprites.Length; i++)
        {
            SetImage(trainTargets[i], sprites[i], trainColor, true);
            if (placeholderLabels != null && i < placeholderLabels.Length && placeholderLabels[i] != null)
                placeholderLabels[i].SetActive(sprites[i] == null);
        }
        // 결정창 토글과 안내 문구가 겹치지 않도록 한다.
        if (artNotice != null) artNotice.SetActive(!pending);
    }

    private static void SetImage(Image target, Sprite sprite, Color fallback, bool preserveAspect)
    {
        if (target == null) return;
        target.sprite = sprite;
        target.type = Image.Type.Simple;
        target.preserveAspect = preserveAspect;
        target.color = sprite == null ? fallback : Color.white;
        target.raycastTarget = false;
    }
}
