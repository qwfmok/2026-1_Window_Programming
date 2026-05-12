using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardChess.Models;

namespace CardChess.Pieces
{
    public class Rook : IPiece
    {
        // 룩의 현재 위치, 소유자, 종류 정의
        public Position CurrentPosition { get; set; }
        public PlayerType Owner { get; set; }
        public PieceType Type => PieceType.Rook;

        // 생성자: 소유자와 초기 위치 설정
        public Rook(PlayerType owner, Position currentPosition)
        {
            Owner = owner;
            CurrentPosition = currentPosition;
        }

        // 룩의 이동 및 공격 로직은 동일 (직선상에 적이 있으면 잡음)
        public List<Position> GetMovablePositions(GameState state)
        {
            return GetLongRangeMoves(state);
        }

        public List<Position> GetAttackablePositions(GameState state)
        {
            return GetLongRangeMoves(state);
        }

        // 상하좌우 4방향으로 장애물을 만날 때까지 탐색하는 로직
        private List<Position> GetLongRangeMoves(GameState state)
        {
            List<Position> positions = new List<Position>();

            // 탐색할 4방향 정의 (상, 하, 좌, 우)
            int[] dRow = { -1, 1, 0, 0 };
            int[] dCol = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int nextRow = CurrentPosition.Row + dRow[i];
                int nextCol = CurrentPosition.Col + dCol[i];

                // 보드 범위를 벗어나기 전까지 해당 방향으로 계속 전진
                while (state.IsWithinBoard(new Position(nextRow, nextCol)))
                {
                    Position nextPos = new Position(nextRow, nextCol);
                    IPiece target = state.GetPieceAt(nextPos);

                    if (target == null)
                    {
                        // 빈 칸이면 추가하고 계속 전진
                        positions.Add(nextPos);
                    }
                    else
                    {
                        // 기물을 만났을 때: 적군이면 추가하고 중단, 아군이면 바로 중단
                        if (target.Owner != this.Owner)
                        {
                            positions.Add(nextPos);
                        }
                        break;
                    }

                    // 다음 칸으로 좌표 갱신
                    nextRow += dRow[i];
                    nextCol += dCol[i];
                }
            }

            return positions;
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