using CardChess.Core;
using CardChess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Cards
{
    public interface ICard
    {
        string Name { get; }          // 카드 이름
        string Description { get; }   // 카드 효과 설명
        int Cost { get; }             // 사용 비용 (있을 경우)
        CardType Type { get; }        // 카드 종류

        /// 카드 사용 조건 확인
        bool CanUse(Position targetPos, GameState state);

        /// 카드의 실제 효과 실행
        void Execute(Position targetPos, GameState state);
    }
}
