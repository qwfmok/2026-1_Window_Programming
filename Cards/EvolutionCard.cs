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
        public CardType Type => CardType.Evolution;
        public PieceType EvolutionTarget { get; set; }
        public EvolutionCard(string name, string description, PieceType target)
        {
            Name = name;
            Description = description;
            EvolutionTarget = target;
        }
        public bool CanUse(Position targetPos, GameState state)
        {
            var piece = state.GetPieceAt(targetPos);

            // 현재 좌표에 해당 플레이어가 주인이 되는 폰이 존재하는지 검사
            return piece != null && piece.Owner == state.CurrentTurn && piece.Type == PieceType.Pawn;
        }

        // 재정의할 기물의 타입을 받아 대상의 위치에서 대체하는 것으로 진화 구현
        public void Execute(Position targetPos, GameState state, CardManager cardManager)
        {
            state.ReplacePiece(targetPos, EvolutionTarget);

            MainForm.Instance.AddLog($"{Name} 발동! {targetPos}의 폰이 {EvolutionTarget}(으)로 진화했습니다.");
        }
    }
}
