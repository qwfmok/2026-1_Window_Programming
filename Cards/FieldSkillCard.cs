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
    public class FieldSkillCard : ICard
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public CardType Type => CardType.FieldSkill;
        public int Duration { get; set; } = 2;
        public FieldSkillCard(string name, string description)
        {
            Name = name;
            Description = description;

            // --- 필드 개입형 카드 중 지속 시간을 고려해야 하는 카드의 지속 시간 ---
            if (name == "방벽 건설")
            {
                Duration = 2;
            }  
        }

        public bool CanUse(Position targetPos, GameState state)
        {
            // 보드 바깥 또는 해당 위치에 장애물이 존재하는지 검사하여 조건에 부합하지 않으면 발동 불가 처리
            if (!state.IsWithinBoard(targetPos) || state.GetPieceAt(targetPos) != null)
            {
                return false;
            }

            if (Name == "방벽 건설" && state.ActiveWalls.ContainsKey($"{targetPos.Row},{targetPos.Col}"))
            {
                return false;
            }

            // 증원 카드는 아군 진영(Row 기준 상하 절반)에서만 발동 가능
            if (Name == "증원")
            {
                PlayerType myPlayer = state.CurrentTurn;

                if (myPlayer == PlayerType.Player1)
                {
                    return targetPos.Row >= 4 && targetPos.Row <= 7;
                }
                else
                {
                    return targetPos.Row >= 0 && targetPos.Row <= 3;
                }
            }

            return true;
        }

        // 필드 개입형 카드 효과 처리부
        public void Execute(Position targetPos, GameState state, CardManager cardManager)
        {
            if (Name == "방벽 건설")
            {
                string wallKey = $"{targetPos.Row},{targetPos.Col}";

                if (!state.ActiveWalls.ContainsKey(wallKey))
                {
                    state.ActiveWalls.Add(wallKey, Duration);
                    MainForm.Instance.AddLog($"[방벽을 건설합니다.] {targetPos.Row},{targetPos.Col} 칸에 벽 상태가 부여되었습니다. ({Duration}턴 유지)");
                }
            }
            else if (Name == "증원")
            {
                PlayerType currentPlayer = state.CurrentTurn;
                Pawn newPawn = new Pawn(currentPlayer, targetPos);
                state.SetPieceAt(targetPos, newPawn);

                MainForm.Instance.AddLog($"[병사를 증원합니다.] {targetPos.Row},{targetPos.Col} 칸에 {currentPlayer}의 폰이 소환되었습니다.");
            }
        }
    }
}
