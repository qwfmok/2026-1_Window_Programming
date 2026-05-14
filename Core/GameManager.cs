using System;
using CardChess.Models;
using CardChess.Pieces;
using CardChess.Cards;

namespace CardChess.Core
{
    public class GameManager
    {
        // 게임의 모든 상태(보드판, 손패, 턴 등)를 들고 있는 객체
        public GameState State { get; private set; }

        // InputController에서 편하게 턴을 확인할 수 있도록 프로퍼티 제공
        public PlayerType CurrentTurn => State.CurrentTurn;

        public GameManager()
        {
            State = new GameState();
            InitializeBoard(); // 게임 시작 시 초기 세팅
        }

        // ♟️ 임시로 폰 기물들을 배치하는 함수 (나중에 다른 기물들도 추가될 예정)
        private void InitializeBoard()
        {
            // Player1 폰 8개 배치 (1행)
            for (int i = 0; i < 8; i++)
                State.SetPieceAt(new Position(1, i), new Pawn(PlayerType.Player1, new Position(1, i)));

            // Player2 폰 8개 배치 (6행)
            for (int i = 0; i < 8; i++)
                State.SetPieceAt(new Position(6, i), new Pawn(PlayerType.Player2, new Position(6, i)));
        }

        // 🛡️ 아군 기물인지 확인 (InputController에서 클릭 검증용으로 사용)
        public bool IsAllyPiece(Position pos, PlayerType player)
        {
            return State.IsAllyPiece(pos, player);
        }

        // ⚔️ 이동 또는 공격 시도 (InputController가 호출함)
        public void TryMoveOrAttack(Position from, Position to)
        {
            IPiece piece = State.GetPieceAt(from);
            if (piece == null) return;

            // 정우가 만든 기물 로직(CanMove, CanAttack)을 여기서 물어봄!
            bool canMove = piece.CanMove(to, State);
            bool canAttack = piece.CanAttack(to, State);

            if (canMove || canAttack)
            {
                // 이전 자리 비우고, 새 자리에 기물 넣기 (실제 이동 처리)
                State.SetPieceAt(from, null);
                State.SetPieceAt(to, piece);

                // 행동을 마쳤으니 턴 종료
                EndTurn();
            }
        }

        // 🃏 카드 사용 시도 (InputController가 호출함)
        public void TryUseCard(ICard card, Position targetPos)
        {
            // TODO: 나중에 정우 님이 ICard 인터페이스에 Use() 함수를 만들면 여기서 실행
            // 예: card.Use(State, targetPos);

            // 카드 사용 후 턴 종료
            EndTurn();
        }

        // 🔄 턴 넘기기
        private void EndTurn()
        {
            State.CurrentTurn = (State.CurrentTurn == PlayerType.Player1)
                                ? PlayerType.Player2
                                : PlayerType.Player1;
        }
    }
}