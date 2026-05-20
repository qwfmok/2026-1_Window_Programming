using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardChess.Models;
using CardChess.Core;
namespace CardChess.Pieces
{
    public interface IPiece
    {
        Position CurrentPosition { get; set; }
        PlayerType Owner { get; }
        PieceType Type { get; }
        bool HasShield { get; set; }        // 신성한 보호막 여부
        bool IsFrozen { get; set; }         // 존야(무적/정지) 여부
        Position? ShadowPosition { get; set; } // 영혼 해방(요네 E) 돌아갈 위치
        int ShadowTurns { get; set; }       // 영혼 해방 남은 턴 수

        // 이동 가능한 모든 좌표 반환
        List<Position> GetMovablePositions(GameState state);

        // 공격 가능한 모든 좌표 반환 (폰의 경우 이동과 공격 범위가 다름)
        List<Position> GetAttackablePositions(GameState state);

        // 특정 위치로 이동/공격이 가능한지 최종 확인
        bool CanMove(Position target, GameState state);
        bool CanAttack(Position target, GameState state);
    }
}
