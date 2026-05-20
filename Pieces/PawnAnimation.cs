using CardChess.Models;
using CardChess.Pieces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Pieces
{
    public class PawnAnimation : PieceAnime
    {
        public IPiece LogicalPiece { get; private set; }
        public PawnAnimation(IPiece Pawn, float startX, float startY)
            : base(Pawn.Owner.ToString(), Pawn.Type.ToString(), startX, startY)
        {
            this.LogicalPiece = Pawn;
        }
        public void TriggerDeath()
        {
            this.State = PieceStatement.Dead;
            this.Intensity = 1.0f;
        }
    }
}