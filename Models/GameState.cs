using CardChess.Cards;
using CardChess.Models;
using CardChess.Pieces;
using System;
using System.Collections.Generic;

namespace CardChess.Core
{
    public class GameState
    {
        public IPiece[,] Board { get; private set; }
        public PlayerType CurrentTurn { get; set; }

        // --- 카드 관련 데이터 수정 ---
        // 카드 관리 소스에서 사용되는 덱과 핸드를 리스트 형태로 구현
        public Stack<ICard> SharedDeck { get; private set; }
        public List<ICard> DiscardPile { get; private set; }
        public Dictionary<PlayerType, List<ICard>> Hands { get; private set; }
        public Dictionary<PlayerType, List<ICard>> Traps { get; private set; }
        public Dictionary<string, int> ActiveWalls { get; private set; } = new Dictionary<string, int>();
        public List<PieceType> Player1DeadPieces { get; set; } = new List<PieceType>();
        public List<PieceType> Player2DeadPieces { get; set; } = new List<PieceType>();
        public bool IsGameOver { get; set; }
        public PlayerType? Winner { get; set; }
        public bool IsExtraTurnGranted { get; set; }
        public bool HasUsedCardThisTurn { get; set; }
        public Random SharedRandom { get; set; }

        public GameState()
        {
            Board = new IPiece[8, 8];

            // 덱과 손패, 함정 초기화, 플레이어별 핸드 및 별도의 트랩 카드를 처리하는 리스트 할당
            SharedDeck = new Stack<ICard>();
            DiscardPile = new List<ICard>();
            Hands = new Dictionary<PlayerType, List<ICard>>();
            Traps = new Dictionary<PlayerType, List<ICard>>();

            Hands[PlayerType.Player1] = new List<ICard>();
            Hands[PlayerType.Player2] = new List<ICard>();
            Traps[PlayerType.Player1] = new List<ICard>();
            Traps[PlayerType.Player2] = new List<ICard>();

            CurrentTurn = PlayerType.Player1;
            IsGameOver = false;
            Winner = null;
            IsExtraTurnGranted = false;
            HasUsedCardThisTurn = false;
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
        // 기물 진화용 메소드 | 진화 카드의 실제 효과 처리부
        public void ReplacePiece(Position pos, PieceType newType)
        {
            IPiece oldPiece = GetPieceAt(pos);
            if (oldPiece == null) return;

            PlayerType owner = oldPiece.Owner;
            IPiece newPiece = null;

            // 해당 클래스의 기물 타입으로 교환
            switch (newType)
            {
                case PieceType.Rook: newPiece = new Rook(owner, pos); break;
                case PieceType.Knight: newPiece = new Knight(owner, pos); break;
                case PieceType.Bishop: newPiece = new Bishop(owner, pos); break;
                case PieceType.Queen: newPiece = new Queen(owner, pos); break;
            }

            if (newPiece != null)
            {
                SetPieceAt(pos, newPiece);
            }
        }
    }
}