using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// 구현을 위해 그냥 제가 임시테스트를 넣어보았습니다!!!!!! - 현빈
namespace CardChess.Cards
{
    // ICard를 상속받는 임시 테스트용 카드
    public class ActiveSkillCard : ICard
    {
        public string Name { get; set; }

        public ActiveSkillCard(string name)
        {
            Name = name;
        }
    }
}