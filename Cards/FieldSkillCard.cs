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

        // 필드물(벽 등)이 유지될 턴 수
        public int Duration { get; set; } = 2;
        public FieldSkillCard(string name, string description)
        {
            Name = name;
            Description = description;

            // 카드별로 턴 제한이 다르면 여기서 세팅 가능
            if (name == "애니비아 벽")
            {
                Duration = 2;
            }  
        }

        public bool CanUse(Position targetPos, GameState state)
        {
            // 해당 칸이 비어있어야 설치 가능
            return state.IsWithinBoard(targetPos) && state.GetPieceAt(targetPos) == null;
        }

        public void Execute(Position targetPos, GameState state, CardManager cardManager)
        {
            if (Name == "애니비아 벽")
            {
                string wallKey = $"{targetPos.Row},{targetPos.Col}";

                if (!state.ActiveWalls.ContainsKey(wallKey))
                {
                    state.ActiveWalls.Add(wallKey, Duration);
                    MainForm.Instance.AddLog($"[지형 생성] {targetPos.Row},{targetPos.Col} 칸에 벽 상태가 부여되었습니다. ({Duration}턴 유지)");
                }
            }
            else if (Name == "폰 소환")
            {
                PlayerType currentPlayer = state.CurrentTurn;
                Pawn newPawn = new Pawn(currentPlayer, targetPos);
                state.SetPieceAt(targetPos, newPawn);

                MainForm.Instance.AddLog($"[기물 소환] {targetPos.Row},{targetPos.Col} 칸에 {currentPlayer}의 폰이 소환되었습니다.");
            }
        }
    }
}
