using CardChess.Core;
using CardChess.Models;
using CardChess.Pieces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Cards
{
    public class TargetSkillCard : ICard
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public CardType Type => CardType.TargetSkill;
        public TargetSkillCard(string name, string description)
        {
            Name = name;
            Description = description;
        }
        public bool CanUse(Position targetPos, GameState state)
        {
            if (!state.IsWithinBoard(targetPos)) 
                return false;

            var piece = state.GetPieceAt(targetPos);
            PlayerType myPlayer = state.CurrentTurn;

            switch (Name)
            {
                case "부활":
                    // 기물이 없는 빈 칸이 아니라면 발동 불가 처리
                    if (piece != null)
                        return false;

                    var graveyard = myPlayer == PlayerType.Player1
                        ? state.Player1DeadPieces
                        : state.Player2DeadPieces;
                    if (graveyard.Count == 0)
                        return false;

                    // 부활 카드는 아군 진영(Row 기준 상하 절반)에서만 발동 가능
                    if (myPlayer == PlayerType.Player1)
                    {
                        return targetPos.Row >= 4 && targetPos.Row <= 7;
                    }
                    else
                    {
                        return targetPos.Row >= 0 && targetPos.Row <= 3;
                    }
                case "기물 뺏기":
                    // 해당 좌표에 기물의 존재와 소유권 검증 후 아니라면 발동 불가 처리 | 킹과 퀸은 밸런스 문제로 탈취 불가능
                    if (piece == null || piece.Owner == myPlayer)
                    {
                        return false;
                    }
                    if (piece.Type == PieceType.King || piece.Type == PieceType.Queen)
                    {
                        return false;
                    }

                    // First Turn Kill을 막기 위해 상대방 본진 내부라면 발동 불가능
                    if (myPlayer == PlayerType.Player1)
                    {
                        if (targetPos.Row == 0 || targetPos.Row == 1)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (targetPos.Row == 7 || targetPos.Row == 6)
                        {
                            return false;
                        }
                    }

                    return true;

                // 자신의 기물인지 검증 후 조건 내에서 즉시 발동할 수 있는 카드
                case "방어막":
                case "유체화":
                case "봉인": 
                    return piece != null && piece.Owner == myPlayer;

                case "위치 교환":
                    if (piece == null || piece.Owner != myPlayer)
                        return false;
                    return GetAllPieces(state).Any(p =>
                        p.Owner == myPlayer && p.Type != PieceType.King && p != piece);

                // 소유권 및 주변 좌표의 상태 영향을 받아 발동할 수 있는 카드
                case "복제":
                    if (piece == null || piece.Owner != myPlayer) return false;
                    return GetAdjacentEmptyPositions(targetPos, state).Count > 0;

                // 소유권 및 기물의 타입에 영향을 받아 발동할 수 있는 카드
                case "랜덤 진화":
                    return piece != null && piece.Owner == myPlayer && piece.Type != PieceType.King;

                default:
                    return true;
            }
        }

        public void Execute(Position targetPos, GameState state, CardManager cardManager)
        {
            var targetPiece = state.GetPieceAt(targetPos);
            PlayerType myPlayer = state.CurrentTurn;
            Random rand = state.SharedRandom;

            if (Name != "부활" && targetPiece == null)
            {
                MainForm.Instance.AddLog($"[{Name}] 타겟 기물이 존재하지 않아 스킬이 허공에 빗나갔습니다!");
                return;
            }

            // --- 대상 지정형 카드의 효과 처리부 ---
            switch (Name)
            {
                case "방어막":
                    targetPiece.HasShield = true;
                    targetPiece.FrozenTurns = 2;
                    MainForm.Instance.AddLog($"[{Name}] {targetPos.Row},{targetPos.Col} 기물에 공격 1회 방어 보호막이 씌워졌습니다.");
                    break;

                case "봉인":
                    targetPiece.IsFrozen = true;
                    targetPiece.FrozenTurns = 2;
                    MainForm.Instance.AddLog($"[{Name}] {targetPos.Row},{targetPos.Col} 기물이 봉인되었습니다. 무적 및 행동 불가 상태입니다.");
                    break;

                case "기물 뺏기":
                    targetPiece.Owner = myPlayer;
                    MainForm.Instance.AddLog($"[{Name}] 상대 기물의 소유권을 내 것으로 만들었습니다!");
                    break;

                case "유체화":
                    targetPiece.ShadowPosition = new Position(targetPos.Row, targetPos.Col);
                    targetPiece.ShadowTurns = 2;
                    MainForm.Instance.AddLog($"[{Name}] 영혼 해방 발동! 2턴 후 {targetPos.Row},{targetPos.Col}로 다시 돌아옵니다.");
                    break;

                // 보드 내부에서 두 기물의 위치를 확인한 후 내부 좌표를 스왑하는 방식으로 구현
                case "위치 교환":
                    var allyPieces = GetAllPieces(state).Where(p => p.Owner == myPlayer && p.Type != PieceType.King && p != targetPiece).ToList();
                    if (allyPieces.Count > 0)
                    {
                        var swapTarget = allyPieces[rand.Next(allyPieces.Count)];
                        Position pos1 = targetPiece.CurrentPosition;
                        Position pos2 = swapTarget.CurrentPosition;

                        state.SetPieceAt(pos1, swapTarget);
                        state.SetPieceAt(pos2, targetPiece);

                        swapTarget.CurrentPosition = pos1;
                        targetPiece.CurrentPosition = pos2;
                        MainForm.Instance.AddLog($"[{Name}] 두 기물이 서로의 위치를 바꿉니다.");
                    }
                    break;

                // 대상 기물에 인접한 빈칸을 검사하여 동일한 객체를 생성
                case "복제":
                    var emptyAdj = GetAdjacentEmptyPositions(targetPos, state);
                    if (emptyAdj.Count > 0)
                    {
                        Position clonePos = emptyAdj[rand.Next(emptyAdj.Count)];
                        IPiece clonedPiece = CreatePiece(targetPiece.Type, myPlayer, clonePos);
                        state.SetPieceAt(clonePos, clonedPiece);
                        MainForm.Instance.AddLog($"[{Name}] 기물이 {clonePos.Row},{clonePos.Col} 칸에 복제되었습니다.");
                    }
                    break;

                // 배열 내부에 진화체가 될 기물을 정의하고 무작위로 결정하여 새 객체로 지정한 후 대상 기물의 소유자와 위치 정보를 대입하여 구현
                case "랜덤 진화":
                    PieceType[] pandoraTypes = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen };
                    PieceType newType = pandoraTypes[rand.Next(pandoraTypes.Length)];

                    IPiece pandoraPiece = CreatePiece(newType, targetPiece.Owner, targetPos);
                    state.SetPieceAt(targetPos, pandoraPiece);
                    MainForm.Instance.AddLog($"[{Name}] 판도라의 상자가 열려 {newType} 기물로 변이했습니다!");
                    break;

                //
                case "부활":
                    var myGraveyard = (myPlayer == PlayerType.Player1) ? state.Player1DeadPieces : state.Player2DeadPieces;

                    if (myGraveyard.Count > 0)
                    {
                        int lastIndex = myGraveyard.Count - 1;
                        PieceType resurrectedType = myGraveyard[lastIndex];
                        myGraveyard.RemoveAt(lastIndex);

                        IPiece resPiece = CreatePiece(resurrectedType, myPlayer, targetPos);
                        state.SetPieceAt(targetPos, resPiece);
                        MainForm.Instance.AddLog($"[{Name}] 빈칸에 {resurrectedType} 기물이 성공적으로 부활했습니다!");
                    }
                    break;
            }
        }
        // 1. 기물 생성기 (부활, 복제에 사용)
        private IPiece CreatePiece(PieceType type, PlayerType owner, Position pos)
        {
            switch (type)
            {
                case PieceType.Pawn: return new Pawn(owner, pos);
                case PieceType.Knight: return new Knight(owner, pos);
                case PieceType.Bishop: return new Bishop(owner, pos);
                case PieceType.Rook: return new Rook(owner, pos);
                case PieceType.Queen: return new Queen(owner, pos);
                case PieceType.King: return new King(owner, pos);
                default: return new Pawn(owner, pos);
            }
        }

        // 2. 체스판 위 모든 기물 가져오기 (위치 교환 스킬에 사용)
        private List<IPiece> GetAllPieces(GameState state)
        {
            List<IPiece> list = new List<IPiece>();
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    if (state.Board[r, c] != null) list.Add(state.Board[r, c]);
            return list;
        }

        // 3. 주변 빈칸 찾기 (복제 스킬에 사용)
        private List<Position> GetAdjacentEmptyPositions(Position center, GameState state)
        {
            List<Position> emptyPos = new List<Position>();
            int[] dRow = { -1, 1, 0, 0, -1, -1, 1, 1 };
            int[] dCol = { 0, 0, -1, 1, -1, 1, -1, 1 };

            for (int i = 0; i < 8; i++)
            {
                Position check = new Position(center.Row + dRow[i], center.Col + dCol[i]);
                if (state.IsWithinBoard(check) && state.GetPieceAt(check) == null)
                {
                    emptyPos.Add(check);
                }
            }
            return emptyPos;
        }
    }
}
