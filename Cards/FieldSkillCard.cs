using CardChess.Core;
using CardChess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Cards
{
    public class FieldSkillCard : ICard
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Cost { get; set; }
        public CardType Type => CardType.FieldSkill;

        // 필드물(벽 등)이 유지될 턴 수
        public int Duration { get; set; } = 2;

        public bool CanUse(Position targetPos, GameState state)
        {
            // 해당 칸이 비어있어야 설치 가능
            return state.IsWithinBoard(targetPos) && state.GetPieceAt(targetPos) == null;
        }

        public void Execute(Position targetPos, GameState state)
        {
            // TODO: BoardManager나 GameState에 '장애물(Obstacle)' 설치 로직 호출
            // 예: state.PlaceObstacle(targetPos, Duration);
            // 기물이 아니기 때문에 IPiece와는 별개로 관리하거나, 
            // 'Wall'이라는 특수 기물을 소환하는 방식으로 구현할 수 있습니다.
        }
    }
}
