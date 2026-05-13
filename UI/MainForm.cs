using CardChess.Pieces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CardChess.UI
{
    internal class MainForm
    {
        private void Assets(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(chboard.Boardimage, chboard.X, chboard.Y, CardChess.Core.BoardManager.BOARD_WIDTH, CardChess.Core.BoardManager.BOARD_HEIGHT);
            int cellSize = (int)(CardChess.Core.BoardManager.BOARD_WIDTH / CardChess.Core.BoardManager.MAX_COL);
        }
    }
}
