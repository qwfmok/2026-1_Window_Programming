using System.Collections.Generic;
using CardChess.Models;
using CardChess.Pieces;
using CardChess.Cards;

namespace CardChess.Core
{
    public class GameState
    {
        public IPiece[,] Board { get; private set; }
        public PlayerType CurrentTurn { get; set; }

        public List<ICard> Player1Hand { get; private set; }
        public List<ICard> Player2Hand { get; private set; }

        public bool IsGameOver { get; set; }
        public PlayerType? Winner { get; set; }

        public GameState()
        {
            Board = new IPiece[8, 8];
            Player1Hand = new List<ICard>();
            Player2Hand = new List<ICard>();

            CurrentTurn = PlayerType.Player1;
            IsGameOver = false;
            Winner = null;
        }

        public bool IsWithinBoard(Position position)
        {
            return position.Row >= 0 && position.Row < 8
                && position.Col >= 0 && position.Col < 8;
        }

        public IPiece GetPieceAt(Position position)
        {
            if (!IsWithinBoard(position)) // 여기 변경
                return null;
            return Board[position.Row, position.Col];
        }

        public void SetPieceAt(Position position, IPiece piece)
        {
            if (!IsWithinBoard(position)) // 여기 변경
                return;
            Board[position.Row, position.Col] = piece;

            if (piece != null)
            {
                piece.CurrentPosition = position;
            }
        }

        public bool IsEnemyPiece(Position position, PlayerType player)
        {
            IPiece piece = GetPieceAt(position);

            return piece != null && piece.Owner != player;
        }

        public bool IsAllyPiece(Position position, PlayerType player)
        {
            IPiece piece = GetPieceAt(position);

            return piece != null && piece.Owner == player;
        }
    }
}