using UnityEngine;
using UnityEngine.UI;

// 첫 구현: 시작 버튼은 보고만 바꾼다. 시간과 자원은 아직 진행하지 않는다.
public sealed class PrototypeReportScreen : MonoBehaviour
{
    [SerializeField] private Text reportText;
    [SerializeField] private Button startButton;
    [SerializeField] private Text buttonText;
    [SerializeField, TextArea(2, 5)] private string readyReport =
        "시연 준비가 끝났습니다. 시작 버튼을 눌러 폐역의 상황을 확인하세요.";
    [SerializeField, TextArea(2, 5)] private string startedReport =
        "폐역에 정차했습니다. 남은 물자를 탐색할 수 있지만, 현재 외부는 극고온입니다.";

    private bool hasStarted;
    private Font temporaryFont;

    private void Awake()
    {
        // 확보한 정식 글꼴을 적용하기 전, Windows의 한글 글꼴을 임시 사용한다.
        temporaryFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕" }, 28);
        if (temporaryFont != null)
        {
            foreach (Text label in GetComponentsInChildren<Text>(true))
                label.font = temporaryFont;
        }

        hasStarted = false;
        reportText.text = readyReport;
        startButton.interactable = true;
        buttonText.text = "시작";
    }

    public void StartDemo()
    {
        if (hasStarted) return;
        hasStarted = true;
        reportText.text = startedReport;
        buttonText.text = "시작 완료";
        startButton.interactable = false;
    }

    private void OnDestroy()
    {
        if (temporaryFont != null) Destroy(temporaryFont);
    }
}
