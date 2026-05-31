using CardChess.Cards;
using CardChess.Models;
using CardChess.Pieces;
using System;
using System.Linq;
using System.Collections.Generic;

/// 게임 관리 구현은 여기서

namespace CardChess.Core
{
    public class GameManager
    {
        public event Action OnTurnEndRequired;
        public GameState State { get; private set; }
        public CardManager CardMgr { get; private set; }
        public PlayerType CurrentTurn => State.CurrentTurn;
        public bool IsLocalAction { get; set; } = true;
        public event Action<string> OnNetworkBroadcast;

        public GameManager(int seed)
        {
            State = new GameState();
            CardMgr = new CardManager(State);
            InitializeBoard();
            CardMgr.InitializeDecks(seed);

            CardMgr.DrawMultiple(PlayerType.Player1, 5);
            CardMgr.DrawMultiple(PlayerType.Player2, 5);
        }

        // 기물 배치
        private void InitializeBoard()
        {
            for (int i = 0; i < 8; i++)
            {
                if (i == 3) State.SetPieceAt(new Position(0, i), new Queen(PlayerType.Player2, new Position(0, i)));
                else if (i == 4) State.SetPieceAt(new Position(0, i), new King(PlayerType.Player2, new Position(0, i)));
                else State.SetPieceAt(new Position(0, i), new Pawn(PlayerType.Player2, new Position(0, i)));
            }
            for (int i = 0; i < 8; i++)
                State.SetPieceAt(new Position(1, i), new Pawn(PlayerType.Player2, new Position(1, i)));

            for (int i = 0; i < 8; i++)
                State.SetPieceAt(new Position(6, i), new Pawn(PlayerType.Player1, new Position(6, i)));

            for (int i = 0; i < 8; i++)
            {
                if (i == 3) State.SetPieceAt(new Position(7, i), new Queen(PlayerType.Player1, new Position(7, i)));
                else if (i == 4) State.SetPieceAt(new Position(7, i), new King(PlayerType.Player1, new Position(7, i)));
                else State.SetPieceAt(new Position(7, i), new Pawn(PlayerType.Player1, new Position(7, i)));
            }
        }

        public bool IsAllyPiece(Position pos, PlayerType player) => State.IsAllyPiece(pos, player);

        public bool TryMoveOrAttack(Position from, Position to,out string errorMessage)
        {
            errorMessage = "";

            if (State.IsGameOver)
            {
                errorMessage = "이미 게임이 종료되었습니다.";
                return false;
            }

            IPiece piece = State.GetPieceAt(from);
            if (piece == null)
            {
                errorMessage = "선택한 칸에 기물이 없습니다.";
                return false;
            }

            if (piece.CanMove(to, State) || piece.CanAttack(to, State))
            {
                IPiece targetPiece = State.GetPieceAt(to);

                if (targetPiece != null && targetPiece.Owner == piece.Owner)
                {
                    errorMessage = "아군 기물이 있는 곳으로는 이동할 수 없습니다.";
                    return false;
                }

                if (targetPiece != null && targetPiece.Owner != piece.Owner)
                {
                    if (targetPiece.IsFrozen)
                    {
                        errorMessage = "대상은 존야 상태이므로 공격할 수 없습니다!";
                        return false;
                    }

                    if (targetPiece.HasShield)
                    {
                        targetPiece.HasShield = false;
                        errorMessage = "대상의 신성한 보호막이 공격을 1회 방어했습니다!";

                        if (IsLocalAction) OnNetworkBroadcast?.Invoke($"MOVE,{from.Row},{from.Col},{to.Row},{to.Col}");

                        OnTurnEndRequired?.Invoke();
                        return true;
                    }

                    if (targetPiece.Type == PieceType.King)
                    {
                        State.SetPieceAt(from, null);
                        State.SetPieceAt(to, piece);
                        piece.CurrentPosition = to;
                        State.IsGameOver = true;
                        State.Winner = piece.Owner;
                        errorMessage = $"{piece.Owner}가 상대 킹을 잡았습니다. 게임 종료!";

                        if (IsLocalAction) OnNetworkBroadcast?.Invoke($"MOVE,{from.Row},{from.Col},{to.Row},{to.Col}");
                        return true;
                    }

                    if (targetPiece.Owner == PlayerType.Player1) State.Player1DeadPieces.Add(targetPiece.Type);
                    else State.Player2DeadPieces.Add(targetPiece.Type);
                }

                State.SetPieceAt(from, null);
                State.SetPieceAt(to, piece);
                piece.CurrentPosition = to;

                // 이동이나 공격 후 체크 또는 체크메이트 상태인지 확인
                string checkMessage;
                bool isCheckmate = CheckCheckmateAfterAction(piece.Owner, out checkMessage);

                if (!string.IsNullOrEmpty(checkMessage))
                {
                    errorMessage = checkMessage;
                }

                if (IsLocalAction)
                    OnNetworkBroadcast?.Invoke($"MOVE,{from.Row},{from.Col},{to.Row},{to.Col}");

                // 체크메이트가 아닐 때는 턴 종료
                if (!State.IsGameOver)
                {
                    OnTurnEndRequired?.Invoke();
                }

                return true;
            }
            else
            {
                errorMessage = $"[{piece.Type}] 해당 위치({to.Row},{to.Col})로는 이동하거나 공격할 수 없습니다!";
                return false;
            }
        }

        public void PassTurn()
        {
            if (State.IsGameOver) return;
            if (IsLocalAction) OnNetworkBroadcast?.Invoke("PASS");

            OnTurnEndRequired?.Invoke();
        }

        public bool TryUseCard(ICard card, Position targetPos, out string errorMessage)
        {
            errorMessage = "";

            if (State.IsGameOver)
            {
                errorMessage = "이미 게임이 종료되었습니다.";
                return false;
            }

            if (State.HasUsedCardThisTurn)
            {
                errorMessage = "한 턴에 스킬 카드는 딱 1장만 사용할 수 있습니다!";
                return false;
            }

            if (card == null)
            {
                errorMessage = "선택된 카드가 없습니다.";
                return false;
            }

            if (card.CanUse(targetPos, State))
            {
                PlayerType currentPlayer = State.CurrentTurn;

                CardMgr.UseCard(card, targetPos, State.CurrentTurn);
                State.HasUsedCardThisTurn = true;

                string checkMessage;
                CheckCheckmateAfterAction(currentPlayer, out checkMessage);

                if (!string.IsNullOrEmpty(checkMessage))
                {
                    errorMessage = checkMessage;
                }

                if (IsLocalAction)
                {
                    OnNetworkBroadcast?.Invoke($"CARD,{card.Name},{targetPos.Row},{targetPos.Col}");
                }

                return true;
            }
            else
            {
                errorMessage = $"[{card.Name}] 카드를 해당 위치에 사용할 수 없거나 조건이 맞지 않습니다.";
                return false;
            }
        }

        /// 턴 교체 권한은 배틀매니저에게 위임하고, 엔드 페이즈용 순수 상태이상 데이터 클리닝
        public void CleanUpTurnEffects()
        {
            UpdatePieceStatusEffects();
            UpdateWallTurns();
            State.HasUsedCardThisTurn = false;
        }

        private void UpdatePieceStatusEffects()
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    IPiece piece = State.GetPieceAt(new Position(r, c));
                    if (piece == null) continue;

                    if (piece.ShadowPosition != null)
                    {
                        if (piece.Owner == State.CurrentTurn) piece.ShadowTurns--;

                        if (piece.ShadowTurns <= 0)
                        {
                            Position origin = piece.ShadowPosition.Value;
                            IPiece occupyingPiece = State.GetPieceAt(origin);
                            if (occupyingPiece != null)
                            {
                                if (occupyingPiece.Owner == PlayerType.Player1) State.Player1DeadPieces.Add(occupyingPiece.Type);
                                else State.Player2DeadPieces.Add(occupyingPiece.Type);
                            }

                            State.SetPieceAt(origin, piece);
                            State.SetPieceAt(new Position(r, c), null);
                            piece.CurrentPosition = origin;
                            piece.ShadowPosition = null;
                        }
                    }

                    if (piece.IsFrozen && piece.Owner == State.CurrentTurn)
                    {
                        piece.IsFrozen = false;
                    }
                }
            }
        }

        private void UpdateWallTurns()
        {
            if (State.ActiveWalls == null || State.ActiveWalls.Count == 0) return;
            var keys = State.ActiveWalls.Keys.ToList();
            foreach (var key in keys)
            {
                State.ActiveWalls[key]--;
                if (State.ActiveWalls[key] <= 0) State.ActiveWalls.Remove(key);
            }
        }

        private Position? FindKing(PlayerType player)
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Position pos = new Position(row, col);
                    IPiece piece = State.GetPieceAt(pos);

                    if (piece != null && piece.Owner == player && piece.Type == PieceType.King)
                    {
                        return pos;
                    }
                }
            }

            return null;
        }

        private bool IsSquareUnderAttack(Position targetPos, PlayerType attacker)
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Position from = new Position(row, col);
                    IPiece piece = State.GetPieceAt(from);

                    if (piece == null)
                        continue;

                    if (piece.Owner != attacker)
                        continue;

                    if (CanPieceAttackSquare(from, targetPos))
                        return true;
                }
            }
            return false;
        }

        private bool CanPieceAttackSquare(Position from, Position targetPos)
        {
            IPiece piece = State.GetPieceAt(from);

            if (piece == null)
                return false;

            int dr = targetPos.Row - from.Row;
            int dc = targetPos.Col - from.Col;

            switch (piece.Type)
            {
                case PieceType.Pawn:
                    int pawnDir = piece.Owner == PlayerType.Player1 ? -1 : 1;

                    return dr == pawnDir && Math.Abs(dc) == 1;

                case PieceType.Knight:
                    return (Math.Abs(dr) == 2 && Math.Abs(dc) == 1) ||
                           (Math.Abs(dr) == 1 && Math.Abs(dc) == 2);

                case PieceType.Bishop:
                    return Math.Abs(dr) == Math.Abs(dc) &&
                           IsPathClear(from, targetPos);

                case PieceType.Rook:
                    return (dr == 0 || dc == 0) &&
                           IsPathClear(from, targetPos);

                case PieceType.Queen:
                    return (dr == 0 || dc == 0 || Math.Abs(dr) == Math.Abs(dc)) &&
                           IsPathClear(from, targetPos);

                case PieceType.King:
                    return Math.Abs(dr) <= 1 && Math.Abs(dc) <= 1;

                default:
                    return false;
            }
        }

        public List<Position> GetCheckingPiecePositions()
        {
            List<Position> checkingPieces = new List<Position>();

            CheckCheckingPiecesForKing(PlayerType.Player1, checkingPieces);
            CheckCheckingPiecesForKing(PlayerType.Player2, checkingPieces);

            return checkingPieces;
        }

        private void CheckCheckingPiecesForKing(PlayerType defender, List<Position> checkingPieces)
        {
            Position? kingPos = FindKing(defender);

            if (!kingPos.HasValue)
                return;

            PlayerType attacker = defender == PlayerType.Player1
                ? PlayerType.Player2
                : PlayerType.Player1;

            if (!IsInCheck(defender))
                return;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Position attackerPos = new Position(row, col);
                    IPiece piece = State.GetPieceAt(attackerPos);

                    if (piece == null)
                        continue;

                    if (piece.Owner != attacker)
                        continue;

                    if (CanPieceAttackSquare(attackerPos, kingPos.Value))
                    {
                        checkingPieces.Add(attackerPos);
                    }
                }
            }
        }

        private bool IsPathClear(Position from, Position to)
        {
            int rowStep = Math.Sign(to.Row - from.Row);
            int colStep = Math.Sign(to.Col - from.Col);

            int row = from.Row + rowStep;
            int col = from.Col + colStep;

            while (row != to.Row || col != to.Col)
            {
                Position current = new Position(row, col);

                if (State.GetPieceAt(current) != null)
                    return false;

                row += rowStep;
                col += colStep;
            }

            return true;
        }

        private bool IsInCheck(PlayerType player)
        {
            Position? kingPos = FindKing(player);

            if (!kingPos.HasValue)
                return true;

            PlayerType enemy = player == PlayerType.Player1 ? PlayerType.Player2 : PlayerType.Player1;

            return IsSquareUnderAttack(kingPos.Value, enemy);
        }

        private bool HasAnyLegalMoveToEscapeCheck(PlayerType player)
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Position from = new Position(row, col);
                    IPiece piece = State.GetPieceAt(from);

                    if (piece == null)
                        continue;

                    if (piece.Owner != player)
                        continue;

                    List<Position> candidates = new List<Position>();

                    candidates.AddRange(piece.GetMovablePositions(State));
                    candidates.AddRange(piece.GetAttackablePositions(State));

                    foreach (Position to in candidates)
                    {
                        IPiece targetPiece = State.GetPieceAt(to);

                        if (targetPiece != null && targetPiece.Owner == player)
                            continue;

                        Position originalPosition = piece.CurrentPosition;

                        State.SetPieceAt(from, null);
                        State.SetPieceAt(to, piece);
                        piece.CurrentPosition = to;

                        bool stillInCheck = IsInCheck(player);

                        State.SetPieceAt(to, targetPiece);
                        State.SetPieceAt(from, piece);
                        piece.CurrentPosition = originalPosition;

                        if (!stillInCheck)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool CheckCheckmateAfterAction(PlayerType attacker, out string message)
        {
            message = "";

            PlayerType defender = attacker == PlayerType.Player1 ? PlayerType.Player2 : PlayerType.Player1;

            bool defenderInCheck = IsInCheck(defender);

            if (!defenderInCheck)
                return false;

            bool canEscape = HasAnyLegalMoveToEscapeCheck(defender);

            if (!canEscape)
            {
                State.IsGameOver = true;
                State.Winner = attacker;

                message = $"체크메이트! {attacker} 승리!";
                return true;
            }

            message = $"체크! {defender}의 킹이 공격받고 있습니다.";
            return false;
        }

        public Position? GetCheckedKingPosition()
        {
            Position? player1King = FindKing(PlayerType.Player1);

            if (player1King.HasValue && IsInCheck(PlayerType.Player1))
            {
                return player1King.Value;
            }

            Position? player2King = FindKing(PlayerType.Player2);

            if (player2King.HasValue && IsInCheck(PlayerType.Player2))
            {
                return player2King.Value;
            }

            return null;
        }
    }
}