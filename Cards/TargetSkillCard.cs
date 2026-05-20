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
                case "컨트롤 탈취":
                    // 킹을 제외한 '상대방 기물'에만 사용 가능
                    return piece != null && piece.Owner != myPlayer && piece.Type != PieceType.King;

                case "기물 위치교환":
                case "신성한 보호막":
                case "복제":
                case "영혼 해방":
                    // '내 기물'에만 사용 가능 (위치교환은 내 기물끼리 랜덤 스왑한다고 가정)
                    return piece != null && piece.Owner == myPlayer;

                case "존야":
                case "판도라":
                    // 아군 적군 상관없이 '아무 기물'이나 지정 가능 (킹 제외하고 싶다면 추가 처리)
                    return piece != null && piece.Type != PieceType.King;

                default:
                    return true;
            }
        }

        public void Execute(Position targetPos, GameState state, CardManager cardManager)
        {
            var targetPiece = state.GetPieceAt(targetPos);
            PlayerType myPlayer = state.CurrentTurn;
            Random rand = new Random();

            switch (Name)
            {
                case "신성한 보호막":
                    targetPiece.HasShield = true;
                    Console.WriteLine($"[{Name}] {targetPos.Row},{targetPos.Col} 기물에 공격 1회 방어 보호막이 씌워졌습니다.");
                    break;

                case "존야":
                    targetPiece.IsFrozen = true;
                    Console.WriteLine($"[{Name}] {targetPos.Row},{targetPos.Col} 기물이 무적 및 행동 불가 상태가 되었습니다.");
                    break;

                case "컨트롤 탈취":
                    targetPiece.Owner = myPlayer;
                    Console.WriteLine($"[{Name}] 상대 기물의 소유권을 내 것으로 만들었습니다!");
                    break;

                case "영혼 해방":
                    // 현재 위치를 그림자(원래 위치)로 기억하고, 지속 턴을 2턴으로 설정
                    targetPiece.ShadowPosition = new Position(targetPos.Row, targetPos.Col);
                    targetPiece.ShadowTurns = 2;
                    Console.WriteLine($"[{Name}] 요네 E 발동! 2턴 후 {targetPos.Row},{targetPos.Col}로 다시 돌아옵니다.");
                    break;

                case "기물 위치교환":
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
                        Console.WriteLine($"[{Name}] 두 기물의 위치가 교환되었습니다.");
                    }
                    break;

                case "복제":
                    // 타겟 기물 주변의 빈칸 탐색
                    var emptyAdj = GetAdjacentEmptyPositions(targetPos, state);
                    if (emptyAdj.Count > 0)
                    {
                        Position clonePos = emptyAdj[rand.Next(emptyAdj.Count)];
                        IPiece clonedPiece = CreatePiece(targetPiece.Type, myPlayer, clonePos);
                        state.SetPieceAt(clonePos, clonedPiece);
                        Console.WriteLine($"[{Name}] 기물이 {clonePos.Row},{clonePos.Col} 칸에 복제되었습니다.");
                    }
                    break;

                case "판도라":
                    // 현재 기물을 파괴하고 랜덤한 새 기물로 변경 (킹 제외)
                    PieceType[] pandoraTypes = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen };
                    PieceType newType = pandoraTypes[rand.Next(pandoraTypes.Length)];

                    IPiece pandoraPiece = CreatePiece(newType, targetPiece.Owner, targetPos);
                    state.SetPieceAt(targetPos, pandoraPiece);
                    Console.WriteLine($"[{Name}] 판도라의 상자가 열려 {newType} 기물로 변이했습니다!");
                    break;

                case "부활":
                    // 💡 참고: GameState에 public List<PieceType> DeadPieces = new List<PieceType>(); 를 만들어 두어야 합니다.
                    // 현재 죽은 기물 목록을 가져와서 부활시킨다고 가정 (여기서는 임시로 랜덤 부활)
                    PieceType resurrectedType = PieceType.Knight; // 실제로는 state.DeadPieces 등에서 팝(Pop)
                    IPiece resPiece = CreatePiece(resurrectedType, myPlayer, targetPos);
                    state.SetPieceAt(targetPos, resPiece);
                    Console.WriteLine($"[{Name}] 빈칸에 {resurrectedType} 기물이 부활했습니다.");
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

        // 2. 주변 빈칸 찾기 (복제 스킬에 사용)
        private List<Position> GetAdjacentEmptyPositions(Position pos, GameState state)
        {
            List<Position> emptyPositions = new List<Position>();
            int[] dRow = { -1, 1, 0, 0, -1, -1, 1, 1 };
            int[] dCol = { 0, 0, -1, 1, -1, 1, -1, 1 };

            for (int i = 0; i < 8; i++)
            {
                Position nextPos = new Position(pos.Row + dRow[i], pos.Col + dCol[i]);
                if (state.IsWithinBoard(nextPos) && state.GetPieceAt(nextPos) == null)
                {
                    emptyPositions.Add(nextPos);
                }
            }
            return emptyPositions;
        }

        // 3. 체스판 위 모든 기물 가져오기 (위치 교환 스킬에 사용)
        private List<IPiece> GetAllPieces(GameState state)
        {
            List<IPiece> pieces = new List<IPiece>();
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    IPiece p = state.GetPieceAt(new Position(r, c));
                    if (p != null) pieces.Add(p);
                }
            }
            return pieces;
        }
    }
}
