using CardChess.Core;
using CardChess.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// 비숍

namespace CardChess.Pieces
{
    public class Bishop : IPiece
    {
        // 비숍 클래스 정의 | 현재 좌표, 소유자, 기물 타입, 기타 기물 타게팅 카드 응용 변수
        public Position CurrentPosition { get; set; }
        public PlayerType Owner { get; set; }
        public PieceType Type => PieceType.Bishop;
        public bool HasShield { get; set; } = false; // 보호막 | 공격 1회 무시
        public bool IsFrozen { get; set; } = false; // 존야 | 무적 및 이동불가
        public int FrozenTurns { get; set; } = 0;
        public Position? ShadowPosition { get; set; } = null; // 영혼 해방 | 돌아갈 위치
        public int ShadowTurns { get; set; } = 0; // 영혼 해방 | 남은 턴 수

        // 소유자와 초기 좌표 설정
        public Bishop(PlayerType owner, Position currentPosition)
        {
            Owner = owner;
            CurrentPosition = currentPosition;
        }

        // 비숍의 기본 공격 및 이동 구현
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

            // 존야 상태일 경우 이동 불가능
            if (IsFrozen) return positions;

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
                        // 방벽 만났을 경우 이동할 수 없으므로, 해당 방향 탐색 즉시 중단
                        break;
                    }
                    IPiece target = state.GetPieceAt(nextPos);

                    if (target == null)
                    {
                        // 빈 칸이면 추가하고 전진
                        positions.Add(nextPos);
                    }
                    else
                    {
                        // 기물을 만났을 때 적군이면 추가하고 중단, 아군이면 바로 중단
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
            return GetMovablePositions(state).Any(p => p.Row == target.Row && p.Col == target.Col);
        }

        // 타겟 좌표가 공격 가능한 리스트에 있는지 확인
        public bool CanAttack(Position target, GameState state)
        {
            return GetAttackablePositions(state).Any(p => p.Row == target.Row && p.Col == target.Col);
        }
    }
}