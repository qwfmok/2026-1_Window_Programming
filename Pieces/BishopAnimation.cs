using CardChess.Models;
using CardChess.Pieces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Pieces
{
    public class BishopAnimation : PieceAnime
    {
        public IPiece LogicalPiece { get; private set; }
        public BishopAnimation(IPiece Bishop, float startX, float startY)
            : base(Bishop.Owner.ToString(), Bishop.Type.ToString(), startX, startY)
        {
            this.LogicalPiece = Bishop;
        }
        public void TriggerDeath()
        {
            this.State = PieceStatement.Dead;
            this.Intensity = 1.0f;
        }
    }
}
