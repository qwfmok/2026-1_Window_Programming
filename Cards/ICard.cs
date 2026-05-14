using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Cards
{
    // internal을 public으로 변경!
    public interface ICard
    {
        string Name { get; } // 👈 에러 해결: Name 속성 추가
    }
}
