# ♟️ Card Chess (카드 체스)

**전략적 체스 위에 펼쳐지는 카드 게임의 변수!** 기존 체스 규칙에 카드 시스템을 결합하여 진입 장벽은 낮추고, 역동성은 높인 **하이브리드 턴제 멀티플레이 보드게임**입니다.

---

## 🎮 프로젝트 개요 (Program Overview)

### 🧩 게임 소개
`Card Chess`는 전통적인 체스의 두뇌 싸움에 카드 게임의 무작위성과 성장 요소를 결합한 게임입니다.
복잡한 오프닝이나 특수 규칙을 몰라도 누구나 쉽게 즐길 수 있으며, 총 17종의 마법 카드를 활용한 기물 진화와 스킬 변수를 통해 실력 차이를 전략으로 극복할 수 있습니다.

### ✨ 핵심 시스템
* **🔄 진화 시스템:** 킹과 퀸을 제외한 모든 기물은 '폰(Pawn)'으로 시작하며, 카드를 사용하여 룩, 나이트, 비숍 등으로 자유롭게 진화시킬 수 있습니다.
* **🃏 스킬 카드 (OTK 방지 밸런스):** 전황을 뒤집는 다양한 마법/함정 카드가 존재합니다. 게임 템포 조절을 위해 **'1턴당 1마법 사용 제한'**이 적용되어 있습니다.
* **🌐 온라인 멀티플레이:** ASP.NET Core 8 SignalR 중계 서버를 통해 서로 다른 네트워크의 두 PC에서 6자리 방 코드로 1:1 대전과 채팅을 할 수 있습니다.
* **📖 인게임 가이드 및 환경설정:** 별도의 설명서 허브(Manual Hub) UI를 구축하여 기물, 카드, 규칙을 쉽게 열람할 수 있으며, 인게임 설정창에서 BGM/효과음 볼륨과 화면 모드를 조절할 수 있습니다.

### 🏆 승리 조건
* 상대방의 '킹(King)' 기물을 먼저 잡거나, 상대가 '항복'할 경우 승리합니다.

---

## 📜 턴 진행 방식

1.  **드로우 (Draw):** 턴 시작 시 공용 덱에서 카드 1장을 손패로 가져옵니다.
2.  **행동 (Action):** 아래의 행동을 순서에 상관없이 진행할 수 있습니다.
    * **이동/공격:** 기물 1개를 선택하여 이동하거나 적을 공격합니다. (필수/선택)
    * **카드 사용:** 진화, 타겟, 액티브, 함정 등 카드를 1장 사용합니다. 
3.  **턴 종료 (End Turn):** 행동을 마친 후 턴을 넘겨 상대방의 턴으로 전환합니다.

---

## 🛠️ 개발 환경 (Tech Stack)

* **Language:** C#
* **Framework:** .NET (Windows Forms)
* **IDE:** Visual Studio 2022
* **Network:** ASP.NET Core 8 SignalR
* **VCS:** Git & GitHub

---

## 👥 팀원 및 역할 (Team & Roles) - 5조

| 이름 | 역할 | 담당 업무 |
| :--- | :--- | :--- |
| **김재민(조장)** | System Architect | 인게임 플레이 UI(보드, 덱, 손패 제한) 레이아웃 최적화, 턴 진행 흐름(행동 및 턴 종료) 분리 제어, 승리 판정 및 체크/체크메이트 위험 구역 시각화 |
| **박정우** | Game Logic & UI/UX | 기물 상속 구조 및 이동/공격 로직 구현, 17종 마법/함정 카드 로직 연동 및 1턴 사용 제한(밸런스) 적용, 설명서 UI 뼈대 구축 및 인게임/설정창 리팩토링 |
| **장현빈** | Input & Network | 상태 머신 기반 드래그 앤 드롭 제어 및 단축키 적용, 인게임 로그 및 네트워크 통신망 구축, 난수 동기화(SharedRandom) 및 덱 재사용(무덤) 시스템 구현 |
| **전경원** | Design & Effect | 전체 비주얼 에셋 디자인 및 기물 상태별 애니메이션 렌더링, 사운드 매니저(BGM/SFX) 설계 및 연동, 스킬 이펙트 프레임 제어 및 초기 매치메이킹 연동 |

---

## 📂 프로젝트 파일 구조 (Project Structure)

완성된 아키텍처는 데이터 모델, 핵심 로직, UI 뷰, 그리고 개별 기물/카드의 모듈화를 기반으로 설계되었습니다.

```text
CardChess/
│
├─ Cards/           # 카드 시스템 구현체 (Active, Evolution, Field, Target, Trap 등)
├─ Core/            # 게임 코어 로직 (GameManager, BattleManager, TurnManager, NetworkSettings)
├─ Network/         # SignalR 클라이언트 및 자동 재접속 처리
├─ Server/          # ASP.NET Core 8 SignalR 방/중계 서버
├─ Input/           # 사용자 마우스/키보드 입력 제어 (InputController)
├─ Menu/            # 게임 외적 기능 (SettingsMenu, SoundsManager, Surrender)
├─ Models/          # 데이터 구조 및 열거형 (GameState, Position, CardType, PlayerType 등)
├─ Pieces/          # 체스 기물 객체 및 렌더링 애니메이션 (Pawn, King, Rook, Bishop 등)
├─ Resources/       # 인게임 그래픽 이미지 및 사운드 에셋 보관소
├─ UI/              # 게임판 및 카드 시각적 렌더링 클래스 (BoardView, CardView, MainForm)
│
└─ [Root Forms]     # 로비(Form1), 게임설명서(Main/Game/Card/PieceManual) 및 설정 파일
