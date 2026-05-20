using CardChess.Models;
using CardChess.Pieces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Pieces
{
    public class QueenAnimation : PieceAnime
    {
        public IPiece LogicalPiece { get; private set; }
        public QueenAnimation(IPiece Queen, float startX, float startY)
            : base(Queen.Owner.ToString(), Queen.Type.ToString(), startX, startY)
        {
            this.LogicalPiece = Queen;
        }
        public void TriggerDeath()
        {
            this.State = PieceStatement.Dead;
            this.Intensity = 1.0f;
        }
    }
}