using CardChess.Core;
using CardChess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Cards
{
    public class TrapCard : ICard
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Cost { get; set; }
        public CardType Type => CardType.Trap;

        public bool CanUse(Position targetPos, GameState state)
        {
            // 플레이어당 활성화 가능한 함정 개수 제한이 있다면 여기서 체크
            // 예: return state.CurrentPlayerTraps.Count < 3;
            return true;
        }

        public void Execute(Position targetPos, GameState state)
        {
            // 1. 카드를 손패에서 제거
            // 2. GameState의 함정 슬롯(TrapSlot)에 이 카드를 등록
            // state.RegisterTrap(this);

            // 실제 발동 로직(동전 던지기 등)은 BattleManager에서 
            // '공격 발생 시' 함정 슬롯을 체크하여 실행하게 됩니다.
        }

        /// 실제 함정이 발동될 때 실행될 내부 로직
        public void OnTrigger(GameState state)
        {
            // 예: 동전 던지기 -> 앞면이면 공격 반사 로직 실행
        }
    }
}
