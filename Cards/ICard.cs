using CardChess.Core;
using CardChess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Cards
{
    /// 카드 인터페이스 | 이름, 툴팁, 타입 및 사용 조건과 효과 실행부를 인터페이스로 지정
    public interface ICard
    {
        string Name { get; } // 카드 명칭
        string Description { get; } // 카드 툴팁
        CardType Type { get; } // 카드 타입

        /// 사용 조건 여부 및 카드 효과 처리
        /// 효과를 적용시킬 타겟의 좌표, 현재 게임 상태, 카드 매니저 객체
        bool CanUse(Position targetPos, GameState state);
        void Execute(Position targetPos, GameState state, CardManager cardManager);
    }
}
