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
        // 기물의 현재 위치, 소유자, 종류 정의
        public Position CurrentPosition { get; set; }

        // 👇 문제가 되던 곳! private를 확실하게 날려버렸습니다.
        public PlayerType Owner { get; set; }

        public PieceType Type => PieceType.Pawn;
        public bool HasShield { get; set; } = false;          // 신성한 보호막 (공격 1회 무시)
        public bool IsFrozen { get; set; } = false;           // 존야 (무적 및 이동/공격 불가)
        public Position? ShadowPosition { get; set; } = null; // 영혼 해방 (돌아갈 원본 위치, nullable)
        public int ShadowTurns { get; set; } = 0;             // 영혼 해방 (남은 턴 수)

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
            // [존야 방어 로직] 얼어붙은 상태라면 아무 데도 갈 수 없음! (빈 리스트 반환)
            if (IsFrozen) return moves;

            // 플레이어에 따라 전진 방향 결정 (P1: 위로 -1, P2: 아래로 +1)
            int direction = (Owner == PlayerType.Player1) ? -1 : 1;
            Position forward = new Position(CurrentPosition.Row + direction, CurrentPosition.Col);

            // 보드 범위 안이고, 앞칸에 기물이 없어야 이동 가능
            if (state.IsWithinBoard(forward))
            {
                // 직진하려는 앞칸에 '애니비아 벽'이 깔려있다면?
                string wallKey = $"{forward.Row},{forward.Col}";
                if (state.ActiveWalls != null && state.ActiveWalls.ContainsKey(wallKey))
                {
                    // 벽이 있으면 앞칸이 null(빈칸)이더라도 이동할 수 없으므로 그냥 반환합니다.
                    return moves;
                }
                if (state.GetPieceAt(forward) == null)
                {
                    moves.Add(forward);
                }
            }

            return moves;
        }

        // 공격 로직: 대각선 방향에 적 기물이 있을 때만 가능
        public List<Position> GetAttackablePositions(GameState state)
        {
            List<Position> attacks = new List<Position>();
            // [존야 방어 로직] 얼어붙은 상태라면 아무 데도 갈 수 없음! (빈 리스트 반환)
            if (IsFrozen) return attacks;

            int direction = (Owner == PlayerType.Player1) ? -1 : 1;
            // 왼쪽 대각선(-1), 오른쪽 대각선(+1) 체크
            int[] sideOffsets = { -1, 1 };

            foreach (int side in sideOffsets)
            {
                Position diag = new Position(CurrentPosition.Row + direction, CurrentPosition.Col + side);
                if (state.IsWithinBoard(diag))
                {
                    // 공격하려는 대각선 칸에 '애니비아 벽'이 깔려있다면?
                    string wallKey = $"{diag.Row},{diag.Col}";
                    if (state.ActiveWalls != null && state.ActiveWalls.ContainsKey(wallKey))
                    {
                        continue; // 벽이 쳐진 대각선 칸은 공격 대상에서 제외하고 반대쪽 대각선 검사로 넘어감
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