using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// 보드 로직 구현은 여기서

/// 보드의 칸을 일일히 구현하면 메인폼이 무거워지고 지저분해짐
/// 따라서 로직은 BoardManager, 그래픽은 BoardView 소스 파일에서 관리하니 참조

namespace CardChess.Core
{
    // 체스 보드 상수 정의 및 좌표를 확인하고 관리
    public class BoardManager
    {
        public const int MAX_ROW = 8;
        public const int MAX_COL = 8;

        public static bool IsValidPosition(int row, int col)
        {
            return row >= 0 && row < MAX_ROW && col >= 0 && col < MAX_COL;
        }
    }
}
