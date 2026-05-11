# ♟️ Card Chess (12조)

**전략적 체스 위에 펼쳐지는 카드 게임의 변수!**

기존 체스 규칙에 카드 시스템을 결합하여 진입 장벽은 낮추고, 역동성은 높인 하이브리드 턴제 보드게임입니다.

---

## 🎮 프로젝트 개요 (Program Overview)

### 🧩 게임 소개

**Chess Evolution**은 전통적인 체스의 두뇌 싸움에 카드 게임의 무작위성(운)과 성장 요소를 결합한 게임입니다.

복잡한 특수 규칙을 최소화하여 누구나 쉽게 즐길 수 있으며, 카드를 활용한 기물 진화와 스킬 사용을 통해 실력 차이를 전략과 변수로 보완합니다.

---

## ✨ 핵심 시스템

### 🔄 진화 시스템

* 킹과 퀸을 제외한 모든 기물은 **폰으로 시작**
* 획득한 기물 카드를 사용해 **룩, 나이트, 비숍 등으로 진화 가능**

### 🃏 스킬 카드

* 전황을 뒤집거나 유리하게 만드는 **특수 효과 카드**
* 한 턴에 **최대 1장 사용 가능**

### 🏆 승리 조건

* 기존 체스와 동일하게 **상대 킹 제거 시 승리**

---

## 📜 턴 진행 방식

1. **드로우**

   * 턴 시작 시 카드 1장 뽑기

2. **행동 (선택)**

   * 진화: 카드 사용하여 폰 → 상위 기물 변환
   * 스킬: 특수 효과 카드 사용 (턴당 1회 제한)
   * 이동/공격: 기물 1회 이동 또는 공격

3. **턴 종료**

   * 행동 완료 후 상대 턴으로 전환

---

## 🛠️ 개발 환경 (Tech Stack)

* **Language**: C#
* **Framework**: .NET 8.0 / 9.0 (Windows Forms)
* **IDE**: Visual Studio 2022
* **VCS**: Git (GitHub)

---

## 👥 팀원 및 역할 (Team & Roles)

| 이름  | 역할               | 담당 업무                          |
| --- | ---------------- | ------------------------------ |
| 김재민 | System Architect | UI-로직 통합, 보드 배열 및 승리 판정, 예외 처리 |
| 박정우 | Game Logic       | 기물 클래스, 이동/공격 로직, 카드 시스템       |
| 장현빈 | Input & AI       | 드래그 입력, 게임 로그, AI(PVE)         |
| 전경원 | Design & Effect  | 비주얼 디자인, 애니메이션, 사운드            |

---

## 🚀 개발 일정 (Development Roadmap)

* **Phase 1**: 체스 기본 규칙 및 보드 구조 구현
* **Phase 2**: UI 및 리소스 적용
* **Phase 3**: 카드 드로우 & 진화 시스템 구현
* **Phase 4**: 스킬 카드 및 밸런스 조정
* **Phase 5**: AI 구현 및 최적화

---

## ▶️ 실행 방법 (Getting Started)

```bash
git clone https://github.com/qwfmok/2026-1_Window_Programming.git
```

1. Visual Studio 2022 실행
2. `.sln` 파일 열기
3. **F5** 눌러 빌드 및 실행

---

## 📂 프로젝트 구조

```
CardChess
│
├─ Models
│   ├─ Position.cs
│   ├─ PlayerType.cs
│   ├─ PieceType.cs
│   ├─ CardType.cs
│   ├─ GameState.cs
│
├─ Pieces
│   ├─ IPiece.cs
│   ├─ Pawn.cs
│   ├─ Rook.cs
│   ├─ Bishop.cs
│   ├─ Knight.cs
│   ├─ King.cs
│   ├─ Queen.cs
│
├─ Cards
│   ├─ ICard.cs
│   ├─ EvolutionCard.cs
│   ├─ SkillCard.cs
│
├─ Core
│   ├─ GameManager.cs
│   ├─ BoardManager.cs
│   ├─ TurnManager.cs
│   ├─ BattleManager.cs
│
├─ Input
│   ├─ InputController.cs
│
├─ UI
│   ├─ MainForm.cs
│   ├─ BoardView.cs
│   ├─ CardView.cs
│
├─ Resources
│   ├─ Images
│   ├─ Sounds
```
## 수정체크
장현빈 체크 완료
