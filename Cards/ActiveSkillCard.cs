using CardChess.Core;
using CardChess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Cards
{
    public class ActiveSkillCard : ICard
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Cost { get; set; }
        public CardType Type => CardType.ActiveSkill;

        public bool CanUse(Position targetPos, GameState state) => true; // 바로 사용 가능

        public void Execute(Position targetPos, GameState state)
        {
            // 드로우 2장, 모든 카드 버리기 등 시스템 제어 로직
        }
    }
}
