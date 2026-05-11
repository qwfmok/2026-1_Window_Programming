using CardChess.Models;
using CardChess.Pieces.CardChess.Pieces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Pieces
{
    public class Pawn : IPiece
    {
        // 1. 프로퍼티 구현
        public Position CurrentPosition { get; set; }
        public PlayerType Owner { get; private set; }
        public PieceType Type => PieceType.Pawn;

        // 생성자: 생성 시 주인과 초기 위치를 정함
        public Pawn(PlayerType owner, Position position)
        {
            Owner = owner;
            CurrentPosition = position;
        }

        // 2. 이동 로직: 오직 전진만 가능
        public List<Position> GetMovablePositions(GameState state)
        {
            List<Position> moves = new List<Position>();

            // 플레이어에 따라 전진 방향 결정 (P1: 위로 -1, P2: 아래로 +1)
            int direction = (Owner == PlayerType.Player1) ? -1 : 1;
            Position forward = new Position(CurrentPosition.Row + direction, CurrentPosition.Col);

            // 보드 범위 안이고, 앞칸에 기물이 없어야 이동 가능
            if (state.IsWithinBoard(forward) && state.GetPieceAt(forward) == null)
            {
                moves.Add(forward);
            }

            return moves;
        }

        // 3. 공격 로직: 대각선 방향에 적이 있을 때만 가능
        public List<Position> GetAttackablePositions(GameState state)
        {
            List<Position> attacks = new List<Position>();
            int direction = (Owner == PlayerType.Player1) ? -1 : 1;

            // 왼쪽 대각선(-1), 오른쪽 대각선(+1) 체크
            int[] sideOffsets = { -1, 1 };
            foreach (int side in sideOffsets)
            {
                Position diag = new Position(CurrentPosition.Row + direction, CurrentPosition.Col + side);

                if (state.IsWithinBoard(diag))
                {
                    var targetPiece = state.GetPieceAt(diag);
                    // 대각선 위치에 기물이 존재하고, 그 기물이 내 기물이 아닐 때(적군)
                    if (targetPiece != null && targetPiece.Owner != this.Owner)
                    {
                        attacks.Add(diag);
                    }
                }
            }
            return attacks;
        }

        // 4. 인터페이스에서 요구하는 확인 함수 (이미 구현된 리스트에 포함되는지 체크)
        public bool CanMove(Position target, GameState state)
        {
            return GetMovablePositions(state).Contains(target);
        }

        public bool CanAttack(Position target, GameState state)
        {
            return GetAttackablePositions(state).Contains(target);
        }
    }
}