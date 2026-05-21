using CardChess.Models;
using CardChess.Pieces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Pieces
{
    public class RookAnimation : PieceAnime
    {
        public IPiece LogicalPiece { get; private set; }
        public RookAnimation(IPiece Rook, float startX, float startY)
            : base(Rook.Owner.ToString(), Rook.Type.ToString(), startX, startY)
        {
            this.LogicalPiece = Rook;
        }
        public void TriggerDeath()
        {
            this.State = PieceStatement.Dead;
            this.Intensity = 1.0f;
        }
    }
}