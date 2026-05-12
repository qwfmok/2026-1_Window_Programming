using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardChess.Models;
using CardChess.Cards;
using CardChess.Core;

namespace CardChess.Input
{
    public enum InputState
    {
        Idle,           // 대기 상태
        PieceSelected,  // 기물 선택됨
        CardSelected    // 카드 선택됨
    }

    public class InputController
    {
        public InputState CurrentState { get; private set; } = InputState.Idle; // 내 마우스 상태
        public Position? SelectedPosition { get; private set; }
        public ICard SelectedCard { get; private set; }
        public PlayerType MyPlayerType { get; set; } //플레이어 구분

        private GameManager gameManager;

        // 화면(UI)에 로그 텍스트를 전달할 이벤트
        public event EventHandler<string> OnLogMessage;

        public InputController(GameManager manager, PlayerType myPlayerType)
        {
            this.gameManager = manager;
            this.MyPlayerType = myPlayerType;
        }

        // 로그를 발생시키는 내부 헬퍼 함수
        private void SendLog(string message)
        {   // 현재 시간 메세지로 
            OnLogMessage?.Invoke(this, $"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        //  보드의 특정 칸을 클릭했을 때 쓰는 함수
        public void OnBoardClicked(Position clickedPos)
        {
            // [PvP 방어] 내 턴이 아니면 클릭 무시
            if (gameManager.CurrentTurn != MyPlayerType)
            {
                SendLog("내 턴이 아닙니다!");
                return;
            }
            // 내 턴일때 내가 빈손인지 들고있는지
            switch (CurrentState)
            {
                case InputState.Idle:
                    if (gameManager.IsAllyPiece(clickedPos, MyPlayerType))
                    {
                        SelectedPosition = clickedPos; //좌표 기억
                        CurrentState = InputState.PieceSelected; //상태 변경
                        SendLog($"{clickedPos.Row}, {clickedPos.Col} 위치의 내 기물을 선택했습니다.");
                    }
                    else
                    {
                        SendLog("선택할 수 없는 칸입니다.");
                    }
                    break;

                case InputState.PieceSelected: // 기물 들고있을 때 
                    // 들고있는거 한번 더 누르면 클릭했다면 선택 취소
                    if (SelectedPosition.HasValue && SelectedPosition.Value.Equals(clickedPos))
                    {
                        CancelSelection();
                        SendLog("기물 선택을 취소했습니다.");
                    }
                    else // 다른거 고르면 그곳으로 이동 시도
                    {
                        SendLog($"{SelectedPosition.Value.Row}, {SelectedPosition.Value.Col} 에서 {clickedPos.Row}, {clickedPos.Col}(으)로 이동/공격 시도!");

                        // GameManager로 이동 명령 전달
                        gameManager.TryMoveOrAttack(SelectedPosition.Value, clickedPos);
                        CancelSelection(); // 다시 빈손
                    }
                    break;

                case InputState.CardSelected: // 카드 들고있을 때
                    SendLog($"선택한 카드를 {clickedPos.Row}, {clickedPos.Col} 에 사용 시도!");

                    // GameManager로 카드 사용 명령 전달
                    gameManager.TryUseCard(SelectedCard, clickedPos);
                    CancelSelection(); // 카드를 썼으니 다시 빈손
                    break;
            }
        }

        // 손패의 카드를 클릭했을 때
        public void OnCardClicked(ICard card)
        {
            // 내 턴이 아니면 카드 조작 불가 
            if (gameManager.CurrentTurn != MyPlayerType) return;

            // 카드를 기억하고 상태를 '카드 선택됨'으로 변경
            SelectedCard = card;
            CurrentState = InputState.CardSelected;
            SelectedPosition = null;

            // 카드 타입에 이름(Name) 속성이 있다고 가정 (정우가 ICard에 추가해야 됨)
            SendLog($"'{card.Name}' 카드를 선택했습니다. 타겟을 지정하세요.");
        }

        // 입력 취소
        public void CancelSelection()
        {
            if (CurrentState != InputState.Idle)
            {
                CurrentState = InputState.Idle; //상태를 빈손으로
                SelectedPosition = null; //좌표 지우기
                SelectedCard = null; // 기억하던 카드 지우기
                // SendLog("입력이 초기화되었습니다."); // 너무 자주 뜨면 지저분하니 주석 처리
            }
        }
    }
}