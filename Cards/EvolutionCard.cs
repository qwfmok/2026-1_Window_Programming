using CardChess.Core;
using CardChess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Cards
{
    public class EvolutionCard : ICard
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Cost { get; set; }
        public CardType Type => CardType.Evolution;
        public PieceType EvolutionTarget { get; set; } // 룩, 나이트, 비숍 중 하나
        public EvolutionCard(string name, string description, PieceType target)
        {
            Name = name;
            Description = description;
            EvolutionTarget = target;
        }
        public bool CanUse(Position targetPos, GameState state)
        {
            var piece = state.GetPieceAt(targetPos);
            // 내 기물이면서 폰인 경우에만 진화 가능
            return piece != null &&
                   piece.Owner == state.CurrentTurn &&
                   piece.Type == PieceType.Pawn;
        }

        public void Execute(Position targetPos, GameState state, CardManager cardManager)
        {
            state.ReplacePiece(targetPos, EvolutionTarget);

            MainForm.Instance.AddLog($"{Name} 발동! {targetPos}의 폰이 {EvolutionTarget}(으)로 진화했습니다.");
        }
    }
}
