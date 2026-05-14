using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.UI
{
    internal class BoardView
    {
        BoardView chboard;
        public Bitmap Boardimage { get; private set; }
        public int X { get; set; }
        public int Y { get; set; }

        public BoardView(string path)
        {
            Boardimage = new Bitmap(path);
            X = 50;
            Y = 50;
        }
    }
}
