using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardChess.Models;
using CardChess.Core;
namespace CardChess.Pieces
{
    public class Knight : IPiece
    {
        // 나이트 클래스 정의 | 현재 좌표, 소유자, 기물 타입, 기타 기물 타게팅 카드 응용 변수
        public Position CurrentPosition { get; set; }
        public PlayerType Owner { get; set; }
        public PieceType Type => PieceType.Knight;

        public bool HasShield { get; set; } = false; // 보호막 | 공격 1회 무시
        public bool IsFrozen { get; set; } = false; // 존야 | 무적 및 이동불가
        public int FrozenTurns { get; set; } = 0;
        public Position? ShadowPosition { get; set; } = null; // 영혼 해방 | 돌아갈 위치
        public int ShadowTurns { get; set; } = 0; // 영혼 해방 | 남은 턴 수

        // 소유자와 초기 좌표 설정
        public Knight(PlayerType owner, Position currentPosition)
        {
            Owner = owner;
            CurrentPosition = currentPosition;
        }

        // 나이트의 기본 공격 및 이동 구현 | 다른 기물 뛰어넘을 수 있는 기능
        public List<Position> GetMovablePositions(GameState state)
        {
            List<Position> positions = new List<Position>();

            // 존야 상태일 경우 이동 불가능
            if (IsFrozen) return positions;

            // 나이트가 이동 가능한 8개 지점 정의 (2칸 직진 후 옆으로 1칸)
            int[] dRow = { -2, -2, -1, -1, 1, 1, 2, 2 };
            int[] dCol = { -1, 1, -2, 2, -2, 2, -1, 1 };

            for (int i = 0; i < 8; i++)
            {
                int nextRow = CurrentPosition.Row + dRow[i];
                int nextCol = CurrentPosition.Col + dCol[i];
                Position nextPos = new Position(nextRow, nextCol);
                
                // 보드 안에 있고, 해당 위치에 아군 기물만 없다면 점프 가능
                if (state.IsWithinBoard(nextPos))
                {
                    // 착지하려는 칸에 벽이 있다면
                    string wallKey = $"{nextRow},{nextCol}";
                    if (state.ActiveWalls != null && state.ActiveWalls.ContainsKey(wallKey))
                    {
                        continue; // 이 좌표는 점프해서 내릴 수 없으므로 스킵
                    }
                    IPiece target = state.GetPieceAt(nextPos);

                    // 빈 칸이거나 적군 기물이 있는 경우 리스트에 추가
                    if (target == null || target.Owner != this.Owner)
                    {
                        positions.Add(nextPos);
                    }
                }
            }
            return positions;
        }

        // 나이트는 이동 경로에 구애받지 않으므로 공격 범위도 이동 범위와 동일함
        public List<Position> GetAttackablePositions(GameState state)
        {
            return GetMovablePositions(state);
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