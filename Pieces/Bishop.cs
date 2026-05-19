using CardChess.Core;
using CardChess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Pieces
{
    public class Bishop : IPiece
    {
        // 비숍의 현재 위치, 소유자, 종류 정의 (대문자로 수정 완료)
        public Position CurrentPosition { get; set; }
        public PlayerType Owner { get; set; }
        public PieceType Type => PieceType.Bishop;

        // 생성자: 소유자와 초기 위치 설정
        public Bishop(PlayerType owner, Position currentPosition)
        {
            Owner = owner;
            CurrentPosition = currentPosition;
        }

        // 비숍의 이동 및 공격 로직은 동일 (대각선상에 적이 있으면 잡음)
        public List<Position> GetMovablePositions(GameState state)
        {
            return GetLongRangeMoves(state);
        }

        public List<Position> GetAttackablePositions(GameState state)
        {
            return GetLongRangeMoves(state);
        }

        // 대각선 4방향으로 장애물을 만날 때까지 탐색하는 로직
        private List<Position> GetLongRangeMoves(GameState state)
        {
            List<Position> positions = new List<Position>();

            // 탐색할 대각선 4방향 정의
            int[] dRow = { -1, -1, 1, 1 };
            int[] dCol = { -1, 1, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int nextRow = CurrentPosition.Row + dRow[i];
                int nextCol = CurrentPosition.Col + dCol[i];

                // 보드 범위를 벗어나기 전까지 해당 방향으로 계속 전진
                while (state.IsWithinBoard(new Position(nextRow, nextCol)))
                {
                    Position nextPos = new Position(nextRow, nextCol);
                    string wallKey = $"{nextRow},{nextCol}";
                    if (state.ActiveWalls != null && state.ActiveWalls.ContainsKey(wallKey))
                    {
                        // 벽 뒤쪽으로는 아예 갈 수 없으므로, 해당 방향 탐색을 즉시 중단(break)합니다.
                        break;
                    }
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