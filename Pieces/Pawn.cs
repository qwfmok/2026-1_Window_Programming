using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardChess.Models;
using CardChess.Core;

namespace CardChess.Pieces
{
    public class Pawn : IPiece
    {
        // 폰 클래스 정의 | 현재 좌표, 소유자, 기물 타입, 기타 기물 타게팅 카드 응용 변수
        public Position CurrentPosition { get; set; }
        public PlayerType Owner { get; set; }
        public PieceType Type => PieceType.Pawn;
        public bool HasShield { get; set; } = false; // 보호막 | 공격 1회 무시
        public bool IsFrozen { get; set; } = false; // 존야 | 무적 및 이동불가
        public int FrozenTurns { get; set; } = 0;
        public Position? ShadowPosition { get; set; } = null; // 영혼 해방 | 돌아갈 위치
        public int ShadowTurns { get; set; } = 0; // 영혼 해방 | 남은 턴 수

        // 소유자와 초기 좌표 설정
        public Pawn(PlayerType owner, Position position)
        {
            Owner = owner;
            CurrentPosition = position;
        }

        // 폰의 기본 공격 및 이동 구현
        public List<Position> GetMovablePositions(GameState state)
        {
            List<Position> moves = new List<Position>();
            // 존야 상태일 경우 이동 불가능
            if (IsFrozen) return moves;

            // 플레이어에 따라 전진 방향 결정 (P1: 위로 -1, P2: 아래로 +1)
            int direction = (Owner == PlayerType.Player1) ? -1 : 1;
            Position forward = new Position(CurrentPosition.Row + direction, CurrentPosition.Col);

            // 보드 범위 안이고, 앞칸에 기물이 없어야 이동 가능
            if (state.IsWithinBoard(forward))
            {
                // 직진 시 방벽이 있는 경우
                string wallKey = $"{forward.Row},{forward.Col}";
                if (state.ActiveWalls != null && state.ActiveWalls.ContainsKey(wallKey))
                {
                    // 벽이 있으면 앞칸이 빈칸이더라도 이동할 수 없음
                    return moves;
                }
                if (state.GetPieceAt(forward) == null)
                {
                    moves.Add(forward);
                }
            }
            return moves;
        }

        // 폰의 공격 로직 | 대각선 방향에 적 기물이 있을 때만 가능
        public List<Position> GetAttackablePositions(GameState state)
        {
            List<Position> attacks = new List<Position>();
            // 존야 상태일 경우 이동 불가능
            if (IsFrozen) return attacks;

            int direction = (Owner == PlayerType.Player1) ? -1 : 1;
            // 왼쪽 대각선(-1), 오른쪽 대각선(+1) 체크
            int[] sideOffsets = { -1, 1 };

            foreach (int side in sideOffsets)
            {
                Position diag = new Position(CurrentPosition.Row + direction, CurrentPosition.Col + side);
                if (state.IsWithinBoard(diag))
                {
                    // 공격 시 방벽이 있는 경우
                    string wallKey = $"{diag.Row},{diag.Col}";
                    if (state.ActiveWalls != null && state.ActiveWalls.ContainsKey(wallKey))
                    {
                        continue; // 벽이 쳐진 대각선 칸은 공격 대상에서 제외하고 반대쪽 대각선 검사
                    }

                    var targetPiece = state.GetPieceAt(diag);
                    // 대각선 위치에 기물이 존재하고, 그 기물이 적군일 때만 추가 (기존 로직)
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
            return GetMovablePositions(state).Any(p => p.Row == target.Row && p.Col == target.Col);
        }

        // 타겟 좌표가 공격 가능한 리스트에 있는지 확인
        public bool CanAttack(Position target, GameState state)
        {
            return GetAttackablePositions(state).Any(p => p.Row == target.Row && p.Col == target.Col);
        }
    }
}