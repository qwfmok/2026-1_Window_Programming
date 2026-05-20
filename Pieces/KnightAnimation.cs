using CardChess.Models;
using CardChess.Pieces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Pieces
{
    public class KnightAnimation : PieceAnime
    {
        public IPiece LogicalPiece { get; private set; }
        public KnightAnimation(IPiece knight, float startX, float startY)
            : base(knight.Owner.ToString(), knight.Type.ToString(), startX, startY)
        {
            this.LogicalPiece = knight;
        }
        public void TriggerDeath()
        {
            this.State = PieceStatement.Dead;
            this.Intensity = 1.0f;
        }
    }
}
