# 열국열차 첫 구현 기록 · 2026-10-17

## 현재 합의와 범위

목표는 최소한의 게임 RUN을 보여주는 2~3분 고정 시연이다. 확장 기획은 보류한다. 인계 문서는 시작 기준이며, 사용자의 최신 결정에 따라 프로토타입 명세가 더 축소·업데이트될 수 있다. 이번에는 `시작 버튼 → 상황 보고 변경`까지만 구현했다.

인계 원문: `C:/Users/hufag/OneDrive/바탕 화면/열국열차_개발인계_명세와초기세팅_v0.1 (3).md`.

## 직접 확인한 환경

| 항목 | 확인 결과 |
|---|---|
| 실제 프로젝트 | `C:/Users/hufag/OneDrive/바탕 화면/MediaProject_Unity/My project` |
| 채팅 작업 폴더 | `C:/Users/hufag/OneDrive/Documents/ChatGPT/미디어 프로젝트` — Unity 프로젝트와 별도 |
| 작업 전 Git | `main`, 변경 없음. HEAD `5e988d9` |
| 이전 커밋 | `5e988d9` 초기 실행 설정, `1d1f4a0` PrototypeMain 생성, `fb83ae0` 초기 체크인 |
| Editor | ProjectVersion.txt와 실행 중인 CLI 모두 `6000.6.6f1` |
| 인계 기록과 차이 | 기록의 `6000.3.23f1`과 실제 버전이 다름. 이번 작업에서 버전을 변경하지 않음 |
| 주요 패키지 | URP `17.6.0`, uGUI `2.6.0`, Input System `1.20.0`, Pipeline `0.8.0-exp.1`, Visual Studio 연동 `2.0.26`, Test Framework `1.8.0` |
| 패키지 근거 | manifest, lock, 해당 로컬 PackageCache 확인. AI Assistant 관련 직접 의존성 없음 |
| CLI | 기존 설치와 Editor 연결 정상. 설치·초기화 반복 없음 |
| 입력 설정 | `activeInputHandler: 1` — 새 Input System 사용 |
| 원래 씬 | `Assets/Scenes/PrototypeMain.unity`, 카메라와 Global Light 2D만 존재 |
| 화면 설정 | Player 기본 크기는 이미 1920×1080. Game 창은 Free Aspect였음 |
| Build 목록 | 아직 SampleScene 등록 상태. 이번에는 Editor의 PrototypeMain만 실행 |

원격 URL은 GitHub의 `Haeseong-Gil/Media_Project`로 확인했으나, 원격 통신·업로드·백업 상태는 검증하지 않았다.

## 변경 파일과 역할

- `Assets/Scenes/PrototypeMain.unity`: 기존 카메라·조명을 보존하고 임시 UI와 EventSystem 추가.
- `Assets/Scripts/PrototypeReportScreen.cs`: 실행 초기화, 임시 한글 글꼴, 시작 1회 처리, 보고 변경과 버튼 비활성화.
- `Assets/Editor/PrototypePreview.cs`: `Tools > Yeolguk Train > FHD Preview` 메뉴로 Game 창을 1920×1080에 맞춤. Editor 전용이며 게임 빌드에는 포함되지 않음.
- `Tools/BuildFirstScreen.cs`: UI를 처음 생성한 제작 도구. Assets 밖에 있어 게임에 포함되지 않음. 이미 UI가 있으면 덮어쓰지 않고 중단함. 다시 실행할 필요 없음.
- 관련 `.meta`: Unity가 생성하는 에셋 식별표. 씬과 스크립트 연결을 유지하므로 함께 보존해야 함.
- `Docs/FirstImplementation.md`: 이 기록. 자동 커밋·원격 업로드는 수행하지 않음.

Canvas(화면 UI를 올리는 도화지)는 `Screen Space - Overlay`, Canvas Scaler(해상도에 맞춰 UI 크기를 조절하는 장치)는 `Scale With Screen Size`, 기준 `1920×1080`, Match `0.5`다. 위쪽은 1920×432, 아래쪽은 1920×648이다. 수치는 아직 고정된 표시다.

버튼 연결은 Inspector의 `On Click()`에 `PrototypeCanvas → PrototypeReportScreen.StartDemo()`로 저장했다. StartDemo는 버튼이 눌렸을 때 실행되는 C# 함수다. 이미 시작했다면 다시 실행하지 않는 검사도 들어 있다.

한글은 Play 중 Windows의 맑은 고딕을 임시로 참조한다. 글꼴 파일을 복사하거나 정식 Neo둥근모 Pro를 적용한 것은 아니다. 정식 글꼴과 그림 11장은 아직 적용하지 않았다. OS 글꼴 참조 방식의 근거: https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/font/createdynamicfontfromosfont

## 확인 결과와 근거의 구분

**Codex 직접 확인**

- 게임 스크립트와 Editor 도구 컴파일: 오류 0, 경고 0.
- 실제 Play 상태에서 Canvas 크기 1920×1080, 한글 표시, 상·하단 UI를 캡처로 확인.
- 사용자의 클릭 직후 보고 문자열이 `폐역에 정차했습니다. 남은 물자를 탐색할 수 있지만, 현재 외부는 극고온입니다.`로 바뀐 것을 CLI로 읽어 확인.
- 같은 시점 Button의 `interactable`이 false인 것을 확인. 비활성 버튼은 다시 누를 수 없음.
- Play 종료 후 재실행하면 처음의 준비 보고로 돌아옴.
- 최종 Console 실제 집계: 오류 0, 경고 0. 작업 중 잘못 지정한 캡처 경로 때문에 CLI 도구 오류가 1회 있었으며 게임 오류와 구분함.
- Git 기본 공백 검사에는 Unity가 씬에 자동 저장하는 빈 YAML 값 뒤 공백이 보고됨. 게임·컴파일 오류가 아니며 자동 직렬화 형식을 일괄 수정하지 않음.
- CLI의 즉석 코드 컴파일에서 간헐적으로 `Illegal byte sequence` 오류가 발생함. 일반 게임 스크립트 컴파일은 정상. CLI 모의 포인터 입력은 성공 응답을 줬지만 보고를 바꾸지 못했으므로 클릭 성공의 근거로 사용하지 않음.

**사용자가 직접 확인 · 이 채팅에서 회신함**

- Unity Game 창에서 실제 마우스로 `시작`을 클릭.
- 보고가 바뀌고 버튼이 `시작 완료`로 바뀜.

**아직 확인하지 않은 것**

- Windows 실행 파일 빌드, 발표 장비 가독성, 정식 글꼴·아트 적용.
- 전체 시연 상태 전환, 타이머, 자원 계산, 탐사·사건·탈출·게임 안의 재시연 기능. 이들은 이번 구현 범위 밖이다.

## Unity에서 다시 확인하는 순서

1. Project 창(파일 목록)에서 `Assets > Scenes > PrototypeMain`을 더블클릭한다. 씬은 화면과 오브젝트 배치를 저장한 파일이다.
2. 상단 메뉴 `Tools > Yeolguk Train > FHD Preview`를 누른다. Game 창(게임 실행 미리보기)이 열리고 `Prototype FHD` 또는 1920×1080 고정 해상도로 맞춰진다. 새 설치나 프로젝트 생성은 필요 없다.
3. Unity 상단의 삼각형 ▶ Play를 누른다. 버튼 색이 바뀌면 게임이 실행 중이다.
4. Game 창 오른쪽 아래 `시작`을 누른다. 상황 보고가 폐역 설명으로 바뀌고, 버튼은 `시작 완료`로 어두워진다. 자원 수치는 그대로다.
5. `Window > General > Console`에서 빨간 오류와 노란 경고 수를 확인한다. Console은 실행 중 문제를 보여주는 창이다.
6. 상단 Play를 다시 눌러 종료한다. 다시 Play하면 첫 준비 화면으로 돌아온다. 이것은 Editor 재실행 확인이며 게임 안의 `다시 시연` 기능은 아직 없다.

화면 구성을 살펴보려면 Play를 종료한 뒤 Hierarchy(씬 안 오브젝트 목록)의 `PrototypeCanvas`를 선택한다. 오른쪽 Inspector(선택 대상의 설정 창)에 Canvas·Canvas Scaler·Prototype Report Screen이 보인다. `Ready Report`와 `Started Report`에서 두 보고 문구를 수정하고 `Ctrl+S`로 씬을 저장할 수 있다. 이미 저장된 이 문구는 C# 초기 문자열보다 Inspector 값이 우선한다. Play 중 수정한 값은 종료할 때 사라질 수 있으므로 편집은 Play를 끈 상태에서 한다.

## 기여 및 다음 작업

- 사용자: 범위·기획 의도와 최신 축소 가능성 결정, 실제 클릭 검증.
- AI 도움: 로컬 확인, C#과 임시 UI 작성, FHD 도구 추가, 컴파일·실행 상태 조회, 기록 작성.
- 현재 완료 단위: `버튼 1개 → 보고 변경`.
- 다음 구현 범위는 최신 명세와 사용자 결정으로 정한다. 인계 문서의 후속 목록을 자동으로 모두 구현하지 않는다.
