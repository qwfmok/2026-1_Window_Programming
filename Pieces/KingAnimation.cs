using CardChess.Models;
using CardChess.Pieces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Pieces
{
    public class KingAnimation : PieceAnime
    {
        public IPiece LogicalPiece { get; private set; }
        public KingAnimation(IPiece King, float startX, float startY)
            : base(King.Owner.ToString(), King.Type.ToString(), startX, startY)
        {
            this.LogicalPiece = King;
        }
        public void TriggerDeath()
        {
            this.State = PieceStatement.Dead;
            this.Intensity = 1.0f;
        }
    }
}