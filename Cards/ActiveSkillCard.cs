using CardChess.Core;
using CardChess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardChess.Cards
{
    public class ActiveSkillCard : ICard
    {
        public string Name { get; set; }
        public string Description { get; set; }

        // 레거시 코드: 첫 기획 상 '코스트' 개념은 현재 버전에서 재미 및 템포 상승을 위해 구현하지 않기로 결정
        //public int Cost { get; set; }
        public CardType Type => CardType.ActiveSkill;
        public ActiveSkillCard(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public bool CanUse(Position targetPos, GameState state)
        {
            PlayerType myPlayer = state.CurrentTurn;
            PlayerType enemyPlayer = myPlayer == PlayerType.Player1 ? PlayerType.Player2 : PlayerType.Player1;

            switch (Name)
            {
                case "카드 뺏기":
                    return state.Hands[myPlayer].Count < 8 && state.Hands[enemyPlayer].Count > 0;
                case "랜덤 시전":
                    return state.SharedDeck.Count >= 2;
                default:
                    return true;
            }
        }

        public void Execute(Position targetPos, GameState state, CardManager cardManager)
        {
            if (Name == "두장 뽑기")
            {
                cardManager.DrawMultiple(state.CurrentTurn, 2);
                MainForm.Instance.AddLog($"[두장 뽑기] {state.CurrentTurn}가 카드를 2장 뽑습니다.");
            }

            // 손패 교환 카드를 소모하는 것을 핸드의 카운트 -1로 구현
            else if (Name == "손패 교환")
            {
                PlayerType myPlayer = state.CurrentTurn;
                if (state.Hands.ContainsKey(myPlayer))
                {
                    int myDrawCount = state.Hands[myPlayer].Count;
                    List<ICard> cardsToDiscard = state.Hands[myPlayer].ToList();
                    state.Hands[myPlayer].Clear();
                    if (myDrawCount > 0) cardManager.DrawMultiple(myPlayer, myDrawCount);
                    state.DiscardPile.AddRange(cardsToDiscard);
                }
                MainForm.Instance.AddLog($"[손패 교환] 발동! {state.CurrentTurn}가 손패를 모두 버리고 새로 뽑았습니다.");
            }

            else if (Name == "카드 뺏기")
            {
                PlayerType myPlayer = state.CurrentTurn;
                PlayerType enemyPlayer = (myPlayer == PlayerType.Player1) ? PlayerType.Player2 : PlayerType.Player1;

                if (state.Hands.ContainsKey(enemyPlayer) && state.Hands[enemyPlayer].Count > 0)
                {
                    int targetCardIndex = state.SharedRandom.Next(0, state.Hands[enemyPlayer].Count);

                    // 만약 "지정해서 뺏기"를 구현하고 싶다면, targetPos.Col 등에 인덱스를 담아왔다고 가정하고 처리 가능:
                    // int targetCardIndex = targetPos.Col; 

                    ICard stolenCard = state.Hands[enemyPlayer][targetCardIndex];

                    // 핸드 8장 미만인지 검사 후 상대 플레이어에게서 강탈 구현
                    if (state.Hands[myPlayer].Count < 8)
                    {
                        state.Hands[enemyPlayer].RemoveAt(targetCardIndex);
                        state.Hands[myPlayer].Add(stolenCard);

                        MainForm.Instance.AddLog($"{myPlayer}가 {enemyPlayer}의 패에서 [{stolenCard.Name}] 카드를 탈취했습니다!");
                    }
                    else
                    {
                        MainForm.Instance.AddLog("내 손패가 가득 차서 카드를 뺏어올 수 없습니다!");
                    }
                }
                else
                {
                    MainForm.Instance.AddLog("상대방의 손패가 비어있어 카드를 뺏을 수 없습니다.");
                }
            }

            else if (Name == "시간 왜곡")
            {
                PlayerType myPlayer = state.CurrentTurn;
                state.IsExtraTurnGranted = true;

                MainForm.Instance.AddLog($"{myPlayer}가 [시간 왜곡] 카드를 발동! 다음 상대방의 턴이 스킵됩니다.");
            }
            else if (Name == "랜덤 시전")
            {
                PlayerType myPlayer = state.CurrentTurn;
                
                // 덱 잔량 2매 이상인지 검사 후 랜덤실행 카드를 발동
                if (state.SharedDeck.Count >= 2)
                {
                    MainForm.Instance.AddLog($"[랜덤 시전] 발동! {myPlayer}의 덱에서 카드를 2장 뽑아 즉시 시전합니다.");

                    for (int i = 0; i < 2; i++)
                    {
                        ICard randomCard = state.SharedDeck.Pop();
                        List<Position> validTargets = new List<Position>();
                        for (int r = 0; r < 8; r++)
                        {
                            for (int c = 0; c < 8; c++)
                            {
                                Position candidate = new Position(r, c);
                                if (randomCard.CanUse(candidate, state))
                                {
                                    validTargets.Add(candidate);
                                }
                            }
                        }

                        if (validTargets.Count > 0)
                        {
                            Position randomPos = validTargets[state.SharedRandom.Next(validTargets.Count)];
                            MainForm.Instance.AddLog($" -> 무작위 발동 [{i + 1}번]: {randomCard.Name} (타겟 좌표: {randomPos.Row}, {randomPos.Col})");
                            randomCard.Execute(randomPos, state, cardManager);
                        }
                        else
                        {
                            MainForm.Instance.AddLog($" -> [{randomCard.Name}]은 현재 사용할 수 있는 대상이 없어 발동되지 않았습니다.");
                        }

                        state.DiscardPile.Add(randomCard);
                    }
                }
                else
                {
                    MainForm.Instance.AddLog("덱에 카드가 부족하여 [랜덤 실행]을 사용할 수 없습니다.");
                }
            }
        }
    }
}
