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

        // --- 🃏 카드 관련 데이터 수정 ---
        // CardManager가 사용하는 Decks(Stack)와 Hands(List) 추가
        public Dictionary<PlayerType, Stack<ICard>> Decks { get; private set; }
        public Dictionary<PlayerType, List<ICard>> Hands { get; private set; }
        public Dictionary<PlayerType, List<ICard>> Traps { get; private set; }
        public Dictionary<string, int> ActiveWalls { get; private set; } = new Dictionary<string, int>();
        public bool IsGameOver { get; set; }
        public PlayerType? Winner { get; set; }
        public bool IsExtraTurnGranted { get; set; }
        public GameState()
        {
            Board = new IPiece[8, 8];
            // 덱과 손패, 함정 초기화
            Decks = new Dictionary<PlayerType, Stack<ICard>>();
            Hands = new Dictionary<PlayerType, List<ICard>>();
            Traps = new Dictionary<PlayerType, List<ICard>>();
            // 플레이어별 공간 할당 (Player1, Player2 전용)
            Decks[PlayerType.Player1] = new Stack<ICard>();
            Decks[PlayerType.Player2] = new Stack<ICard>();
            Hands[PlayerType.Player1] = new List<ICard>();
            Hands[PlayerType.Player2] = new List<ICard>();
            Traps[PlayerType.Player1] = new List<ICard>();
            Traps[PlayerType.Player2] = new List<ICard>();

            CurrentTurn = PlayerType.Player1;
            IsGameOver = false;
            Winner = null;
            IsExtraTurnGranted = false;
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
        // --- 🔄 기물 진화용 메서드 추가 ---
        // EvolutionCard가 Execute될 때 호출할 함수입니다.
        public void ReplacePiece(Position pos, PieceType newType)
        {
            IPiece oldPiece = GetPieceAt(pos);
            if (oldPiece == null) return;

            PlayerType owner = oldPiece.Owner;
            IPiece newPiece = null;

            // 기물 클래스들로 교체
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