using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Models
{
    public enum PieceStatement
    {
        Idle1,
        Idle2,
        // 대기 동작 2종 ---> Assets의 ..._num_1과 ..._num_2를 사용합니다.
        Attacking,
        Moving,
        Shaking,
        // 이동 및 착지 ---> Assets의 ..._num_3을 사용합니다.
        Dead
        // 사망 ---> Assets의 ..._num_4를 사용합니다.
    }
}
