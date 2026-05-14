using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Core
{
    internal class TurnManager
    {
        public int Currentplayturn { get; private set; }

        public TurnManager()
        {
            Currentplayturn = 1; // 1p로 시작
        }
        public void Turnswaptrigger() // 현재 플레이어 나타냄
        {
            if (Currentplayturn == 1)
                Currentplayturn = 2;
            else
                Currentplayturn = 1;
        }
    }
}

// 메인에서 참조시켜서 기물 이동을 if로 작성
// 현재 row랑 col값 비교시킨다음
// 턴스왑트리거 불러오고 화면갱신