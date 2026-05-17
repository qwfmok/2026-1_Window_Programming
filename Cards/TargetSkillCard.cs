using CardChess.Core;
using CardChess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Cards
{
    public class TargetSkillCard : ICard
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Cost { get; set; }
        public CardType Type => CardType.TargetSkill;

        public bool CanUse(Position targetPos, GameState state)
        {
            // 예: "위치 교환"의 경우 내 기물인지 확인 등
            return true;
        }

        public void Execute(Position targetPos, GameState state)
        {
            // 요네 E, 복제, 마인드 컨트롤 등 대상 중심 로직 구현
        }
    }
}
