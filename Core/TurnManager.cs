using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// 턴 관리 로직 구현은 여기서

/// 시간 왜곡 카드를 통한 추가 턴 또한 여기서 예외로 두어 관리

namespace CardChess.Core
{
    internal class TurnManager
    {
        public int Currentplayturn { get; private set; }
        public bool IsExtraTurnGranted { get; set; } = false;
        public TurnManager()
        {
            Currentplayturn = 1;
        }

        public void Turnswaptrigger()
        {
            if (IsExtraTurnGranted)
            {
                IsExtraTurnGranted = false;
                Console.WriteLine($"[효과 발동] 상대 턴이 스킵되어 여전히 {Currentplayturn}P의 턴입니다.");
                return;
            }

            if (Currentplayturn == 1)
                Currentplayturn = 2;
            else
                Currentplayturn = 1;
        }
    }
}