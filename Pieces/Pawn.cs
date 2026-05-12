using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardChess.Models;

namespace CardChess.Pieces
{
    public class Pawn : IPiece
    {
        // 기물의 현재 위치, 소유자, 종류 정의
        public Position CurrentPosition { get; set; }
        public PlayerType Owner { get; private set; }
        public PieceType Type => PieceType.Pawn;

        // 생성자: 초기 주인과 위치 설정
        public Pawn(PlayerType owner, Position position)
        {
            Owner = owner;
            CurrentPosition = position;
        }

        // 이동 로직: 앞 칸이 비어있을 때만 1칸 전진 가능
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

        // 공격 로직: 대각선 방향에 적 기물이 있을 때만 가능
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
                    // 대각선 위치에 기물이 존재하고, 그 기물이 적군일 때만 추가
                    if (targetPiece != null && targetPiece.Owner != this.Owner)
                    {
                        attacks.Add(diag);
                    }
                }
            }
            return attacks;
        }

        // 타겟 좌표가 이동 가능한 리스트에 있는지 확인
        public bool CanMove(Position target, GameState state)
        {
            return GetMovablePositions(state).Contains(target);
        }

        // 타겟 좌표가 공격 가능한 리스트에 있는지 확인
        public bool CanAttack(Position target, GameState state)
        {
            return GetAttackablePositions(state).Contains(target);
        }
    }
}