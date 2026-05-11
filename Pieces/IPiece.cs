using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardChess.Models;

namespace CardChess.Pieces
{
    internal interface IPiece
    {
        Position CurrentPosition { get; set; }
        PlayerType Owner { get; }
        PieceType Type { get; }

        // 이동 가능한 모든 좌표 반환
        List<Position> GetMovablePositions(GameState state);

        // 공격 가능한 모든 좌표 반환 (폰의 경우 이동과 공격 범위가 다름)
        List<Position> GetAttackablePositions(GameState state);

        // 특정 위치로 이동/공격이 가능한지 최종 확인
        bool CanMove(Position target, GameState state);
        bool CanAttack(Position target, GameState state);
    }
}
