using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardChess.Models;
using CardChess.Core;
namespace CardChess.Pieces
{
    public class King : IPiece
    {
        // 킹의 현재 위치, 소유자, 종류 정의
        public Position CurrentPosition { get; set; }
        public PlayerType Owner { get; set; }
        public PieceType Type => PieceType.King;
        public bool HasShield { get; set; } = false;          // 신성한 보호막 (공격 1회 무시)
        public bool IsFrozen { get; set; } = false;           // 존야 (무적 및 이동/공격 불가)
        public Position? ShadowPosition { get; set; } = null;  // 영혼 해방 (돌아갈 원본 위치)
        public int ShadowTurns { get; set; } = 0;             // 영혼 해방 (남은 턴 수)
        // 생성자: 소유자와 초기 위치 설정
        public King(PlayerType owner, Position currentPosition)
        {
            Owner = owner;
            CurrentPosition = currentPosition;
        }

        // 이동 로직: 주변 8방향으로 딱 한 칸씩만 이동 가능
        public List<Position> GetMovablePositions(GameState state)
        {
            List<Position> positions = new List<Position>();

            // [존야 방어 로직] 얼어붙은 상태라면 아무 데도 갈 수 없음! (빈 리스트 반환)
            if (IsFrozen) return positions;

            // 상, 하, 좌, 우 및 대각선 포함 8방향 정의
            int[] dRow = { -1, 1, 0, 0, -1, -1, 1, 1 };
            int[] dCol = { 0, 0, -1, 1, -1, 1, -1, 1 };

            for (int i = 0; i < 8; i++)
            {
                int nextRow = CurrentPosition.Row + dRow[i];
                int nextCol = CurrentPosition.Col + dCol[i];
                Position nextPos = new Position(nextRow, nextCol);
                string wallKey = $"{nextRow},{nextCol}";
                if (state.ActiveWalls != null && state.ActiveWalls.ContainsKey(wallKey))
                {
                    // 이 칸에 벽이 있으면 '이 칸만' 스킵하고, 다음 방향(i+1)을 계속 검사합니다.
                    continue;
                }
                // 보드 안에 있고, 아군 기물만 없다면 이동/공격 가능
                if (state.IsWithinBoard(nextPos))
                {
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

        // 킹은 이동 가능한 범위 내의 모든 적을 공격할 수 있음
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