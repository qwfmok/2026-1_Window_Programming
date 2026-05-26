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
                case "죽은 자의 소생":
                    // 반드시 기물이 없는 '빈칸'이어야 함
                    if (piece != null) return false;

                    // 지정한 좌표(targetPos)가 내 진영인지 검사
                    // (가정: Player1은 보드 아래쪽 4~7행, Player2는 보드 위쪽 0~3행을 사용)
                    if (myPlayer == PlayerType.Player1)
                    {
                        // 1P 턴일 때는 찍은 칸의 Row가 4~7 사이일 때만 true(사용 가능) 반환
                        return targetPos.Row >= 4 && targetPos.Row <= 7;
                    }
                    else
                    {
                        // 2P 턴일 때는 찍은 칸의 Row가 0~3 사이일 때만 true 반환
                        return targetPos.Row >= 0 && targetPos.Row <= 3;
                    }
                case "마인드 컨트롤":
                    // 기본 검증: 찍은 칸에 기물이 있어야 하고, 내 기물이 아니어야 함 (상대 기물 타겟 필수)
                    if (piece == null || piece.Owner == myPlayer)
                    {
                        return false;
                    }

                    // 밸런스 제한: 킹과 퀸은 컨트롤을 탈취할 수 없음
                    if (piece.Type == PieceType.King || piece.Type == PieceType.Queen)
                    {
                        return false;
                    }

                    // 위치 제한: 상대방의 가장 깊숙한 뒷줄 1~2열은 조종 불가
                    if (myPlayer == PlayerType.Player1)
                    {
                        // 1P가 쓸 때: 2P의 최상단 뒷줄인 Row 0, Row 1 이면 사용 불가(false)
                        if (targetPos.Row == 0 || targetPos.Row == 1)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // 2P가 쓸 때: 1P의 최하단 뒷줄인 Row 7, Row 6 이면 사용 불가(false)
                        if (targetPos.Row == 7 || targetPos.Row == 6)
                        {
                            return false;
                        }
                    }

                    // 위의 모든 까다로운 방어 조건을 다 통과했다면 사용 가능
                    return true;

                //  순수하게 '내 기물' 전체(킹 포함)에 쓸 수 있는 스킬들
                case "시프트 체인지":
                case "신성한 보호막":
                case "영혼 해방":
                case "존야의 시계": 
                    return piece != null && piece.Owner == myPlayer;

                // '내 기물' + '주변 빈칸'이 필요한 스킬
                case "그림자분신술":
                    if (piece == null || piece.Owner != myPlayer) return false;
                    return GetAdjacentEmptyPositions(targetPos, state).Count > 0;

                // '내 기물'이지만 '킹은 제외'해야 하는 스킬
                case "판도라":
                    // 판도라로 내 킹을 변이시키면 게임이 터지므로 킹은 제외!
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

            if (Name != "죽은 자의 소생" && targetPiece == null)
            {
                MainForm.Instance.AddLog($"[{Name}] 타겟 기물이 존재하지 않아 스킬이 허공에 빗나갔습니다!");
                return;
            }

            switch (Name)
            {
                case "신성한 보호막":
                    targetPiece.HasShield = true;
                    MainForm.Instance.AddLog($"[{Name}] {targetPos.Row},{targetPos.Col} 기물에 공격 1회 방어 보호막이 씌워졌습니다.");
                    break;

                case "존야의 시계":
                    targetPiece.IsFrozen = true;
                    MainForm.Instance.AddLog($"[{Name}] {targetPos.Row},{targetPos.Col} 기물이 무적 및 행동 불가 상태가 되었습니다.");
                    break;

                case "마인드 컨트롤":
                    targetPiece.Owner = myPlayer;
                    MainForm.Instance.AddLog($"[{Name}] 상대 기물의 소유권을 내 것으로 만들었습니다!");
                    break;

                case "영혼 해방":
                    // 현재 위치를 그림자(원래 위치)로 기억하고, 지속 턴을 2턴으로 설정
                    targetPiece.ShadowPosition = new Position(targetPos.Row, targetPos.Col);
                    targetPiece.ShadowTurns = 2;
                    MainForm.Instance.AddLog($"[{Name}] 영혼 해방 발동! 2턴 후 {targetPos.Row},{targetPos.Col}로 다시 돌아옵니다.");
                    break;

                case "시프트 체인지":
                    // 내 기물 중 킹이 아니고 타겟이 아닌 기물들을 찾아 랜덤으로 하나 고름
                    var allyPieces = GetAllPieces(state).Where(p => p.Owner == myPlayer && p.Type != PieceType.King && p != targetPiece).ToList();
                    if (allyPieces.Count > 0)
                    {
                        var swapTarget = allyPieces[rand.Next(allyPieces.Count)];
                        Position pos1 = targetPiece.CurrentPosition;
                        Position pos2 = swapTarget.CurrentPosition;

                        // 보드판 및 기물 내부 좌표 크로스 스왑
                        state.SetPieceAt(pos1, swapTarget);
                        state.SetPieceAt(pos2, targetPiece);
                        swapTarget.CurrentPosition = pos1;
                        targetPiece.CurrentPosition = pos2;
                        MainForm.Instance.AddLog($"[{Name}] 두 기물의 위치가 교환되었습니다.");
                    }
                    break;

                case "그림자분신술":
                    // 타겟 기물 주변의 빈칸 탐색
                    var emptyAdj = GetAdjacentEmptyPositions(targetPos, state);
                    if (emptyAdj.Count > 0)
                    {
                        Position clonePos = emptyAdj[rand.Next(emptyAdj.Count)];
                        IPiece clonedPiece = CreatePiece(targetPiece.Type, myPlayer, clonePos);
                        state.SetPieceAt(clonePos, clonedPiece);
                        MainForm.Instance.AddLog($"[{Name}] 기물이 {clonePos.Row},{clonePos.Col} 칸에 복제되었습니다.");
                    }
                    break;

                case "판도라":
                    // 현재 기물을 파괴하고 랜덤한 새 기물로 변경 (킹 제외)
                    PieceType[] pandoraTypes = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen };
                    PieceType newType = pandoraTypes[rand.Next(pandoraTypes.Length)];

                    IPiece pandoraPiece = CreatePiece(newType, targetPiece.Owner, targetPos);
                    state.SetPieceAt(targetPos, pandoraPiece);
                    MainForm.Instance.AddLog($"[{Name}] 판도라의 상자가 열려 {newType} 기물로 변이했습니다!");
                    break;

                case "죽은 자의 소생":
                    // 내 무덤 리스트 가져오기
                    var myGraveyard = (myPlayer == PlayerType.Player1) ? state.Player1DeadPieces : state.Player2DeadPieces;

                    if (myGraveyard.Count > 0)
                    {
                        // 💡 가장 마지막에 죽은 기물을 꺼냅니다. 
                        // (원한다면 rand.Next()를 써서 무덤 속 기물 중 랜덤으로 살릴 수도 있습니다)
                        int lastIndex = myGraveyard.Count - 1;
                        PieceType resurrectedType = myGraveyard[lastIndex];

                        // 부활시킬 거니까 무덤 리스트에서는 삭제 (Pop)
                        myGraveyard.RemoveAt(lastIndex);

                        // 실제 기물 생성 및 보드 배치
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
