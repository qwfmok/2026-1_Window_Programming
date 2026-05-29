using CardChess.Core;
using CardChess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Cards
{
    public class TrapCard : ICard
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public CardType Type => CardType.Trap;

        public TrapCard(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public bool CanUse(Position targetPos, GameState state)
        {
            // 플레이어당 활성화 가능한 함정 개수 제한이 있다면 여기서 체크
            // 예: return state.CurrentPlayerTraps.Count < 3;
            return true;
        }

        public void Execute(Position targetPos, GameState state, CardManager cardManager)
        {
            PlayerType myPlayer = state.CurrentTurn;

            // GameState의 Traps 주머니에 이 함정 카드를 등록합니다.
            if (state.Traps.ContainsKey(myPlayer))
            {
                state.Traps[myPlayer].Add(this);
                MainForm.Instance.AddLog($"[함정 설치] {myPlayer}가 비밀리에 [{Name}] 함정을 설치했습니다.");
            }

            // 손패에서 제거하는 작업은 CardManager.UseCard에서 알아서 처리하므로 
            // 여기서는 주머니에 넣어주기만 하면 끝입니다!
        }

        /// 상대방이 내 기물을 공격할 때, 배틀 매니저 단에서 이 함수를 호출해 줍니다.
        /// 반사에 성공해서 상대 기물을 죽여야 하면 true, 실패해서 내가 그냥 맞아야 하면 false를 반환합니다.
        /// 실제 함정이 발동될 때 실행될 내부 로직
        public bool OnTrigger(Position fromPos, Position toPos, GameState state)
        {
            if (Name == "랜덤 방어")
            {
                MainForm.Instance.AddLog($"[함정 발동] '랜덤 방어' 함정이 작동합니다!");

                // 50% 확률 (0: 앞면/반사성공, 1: 뒷면/불발)
                if (state.SharedRandom.Next(0, 2) == 0)
                {
                    MainForm.Instance.AddLog(" -> 🪙 동전 앞면! 공격이 반사되어 공격 기물이 역으로 파괴됩니다!");

                    // 공격하러 들어온 상대 기물을 체스판에서 지워버림
                    state.SetPieceAt(fromPos, null);

                    return true; // 반사 성공 신호 (상대 공격 무효화용)
                }
                else
                {
                    MainForm.Instance.AddLog(" -> 🪙 동전 뒷면... 함정이 불발되었습니다. 공격이 그대로 진행됩니다.");
                    return false; // 반사 실패 신호
                }
            }

            return false;
        }
    }
}
