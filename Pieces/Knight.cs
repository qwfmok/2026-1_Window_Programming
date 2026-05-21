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
        // 나이트의 현재 위치, 소유자, 종류 정의
        public Position CurrentPosition { get; set; }
        public PlayerType Owner { get; set; }
        public PieceType Type => PieceType.Knight;

        public bool HasShield { get; set; } = false;          // 신성한 보호막 (공격 1회 무시)
        public bool IsFrozen { get; set; } = false;           // 존야 (무적 및 이동/공격 불가)
        public Position? ShadowPosition { get; set; } = null; // 영혼 해방 (돌아갈 원본 위치, nullable)
        public int ShadowTurns { get; set; } = 0;             // 영혼 해방 (남은 턴 수)

        // 생성자: 소유자와 초기 위치 설정
        public Knight(PlayerType owner, Position currentPosition)
        {
            Owner = owner;
            CurrentPosition = currentPosition;
        }

        // 이동 로직: L자 모양으로 점프하여 이동 (다른 기물을 뛰어넘을 수 있음)
        public List<Position> GetMovablePositions(GameState state)
        {
            List<Position> positions = new List<Position>();

            // [존야 방어 로직] 얼어붙은 상태라면 아무 데도 갈 수 없음! (빈 리스트 반환)
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
        // [수정됨] Contains 버그를 방지하기 위해 Any 방식으로 교체
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