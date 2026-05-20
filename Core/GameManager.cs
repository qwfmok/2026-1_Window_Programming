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

        // ♟️ 킹과 퀸은 원래 위치에, 나머지 모든 기물은 폰으로 배치하는 특수 초기화 로직
        private void InitializeBoard()
        {
            // --- Player2 (위쪽 진영: Row 0, Row 1) --- 아래 방향(+1)으로 전진
            // Row 0: 뒷줄 세팅 (Col 3: 퀸, Col 4: 킹, 나머지: 폰)
            for (int i = 0; i < 8; i++)
            {
                if (i == 3)
                    State.SetPieceAt(new Position(0, i), new Queen(PlayerType.Player2, new Position(0, i)));
                else if (i == 4)
                    State.SetPieceAt(new Position(0, i), new King(PlayerType.Player2, new Position(0, i)));
                else
                    State.SetPieceAt(new Position(0, i), new Pawn(PlayerType.Player2, new Position(0, i)));
            }
            // Row 1: 앞줄 8칸은 전부 폰
            for (int i = 0; i < 8; i++)
                State.SetPieceAt(new Position(1, i), new Pawn(PlayerType.Player2, new Position(1, i)));


            // --- Player1 (아래쪽 진영: Row 6, Row 7) --- 위 방향(-1)으로 전진
            // Row 6: 앞줄 8칸은 전부 폰
            for (int i = 0; i < 8; i++)
                State.SetPieceAt(new Position(6, i), new Pawn(PlayerType.Player1, new Position(6, i)));

            // Row 7: 뒷줄 세팅 (Col 3: 퀸, Col 4: 킹, 나머지: 폰)
            for (int i = 0; i < 8; i++)
            {
                if (i == 3)
                    State.SetPieceAt(new Position(7, i), new Queen(PlayerType.Player1, new Position(7, i)));
                else if (i == 4)
                    State.SetPieceAt(new Position(7, i), new King(PlayerType.Player1, new Position(7, i)));
                else
                    State.SetPieceAt(new Position(7, i), new Pawn(PlayerType.Player1, new Position(7, i)));
            }
            State.Player1Hand.Add(new ActiveSkillCard("파이어볼"));
            State.Player1Hand.Add(new ActiveSkillCard("힐링"));
            State.Player1Hand.Add(new ActiveSkillCard("순간이동"));
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