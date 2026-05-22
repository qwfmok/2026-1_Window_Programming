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
        public int Cost { get; set; }
        public CardType Type => CardType.ActiveSkill;
        public ActiveSkillCard(string name, string description)
        {
            Name = name;
            Description = description;
        }
        // Active 스킬은 보드의 위치와 상관없이 항상 사용 가능함
        public bool CanUse(Position targetPos, GameState state) => true;

        public void Execute(Position targetPos, GameState state, CardManager cardManager)
        {
            // 실제 효과는 이 클래스를 상속받은 개별 카드나 
            // 여기서 분기 처리를 통해 구현합니다.
            if (Name == "욕망의 항아리")
            {
                cardManager.DrawMultiple(state.CurrentTurn, 2);
                MainForm.Instance.AddLog($"[욕망의 항아리] {state.CurrentTurn}가 카드를 2장 뽑습니다.");
            }
            else if (Name == "패 털이")
            {
                PlayerType myPlayer = state.CurrentTurn;

                // 내 손패 처리 (방금 낸 '패 털이' 카드 1장을 뺀 나머지 개수만큼 뽑음)
                if (state.Hands.ContainsKey(myPlayer))
                {
                    int myDrawCount = state.Hands[myPlayer].Count - 1;
                    state.Hands[myPlayer].Clear(); // 내 손패 싹 비우기
                    if (myDrawCount > 0)
                    {
                        cardManager.DrawMultiple(myPlayer, myDrawCount);
                    }
                }
                MainForm.Instance.AddLog($"[패 털이] 발동! {state.CurrentTurn}가 손패를 모두 버리고 새로 뽑았습니다.");
            }
            else if (Name == "카드 뺏기")
            {
                // 현재 턴인 플레이어(나)와 상대 플레이어 구별
                PlayerType myPlayer = state.CurrentTurn;
                PlayerType enemyPlayer = (myPlayer == PlayerType.Player1) ? PlayerType.Player2 : PlayerType.Player1;

                // 상대방에게 탈취할 손패가 있는지 확인
                if (state.Hands.ContainsKey(enemyPlayer) && state.Hands[enemyPlayer].Count > 0)
                {
                    // 상대방 손패 중 랜덤으로 한 장 선택 (또는 UI에서 전달받은 특정 인덱스 사용)
                    Random rand = new Random();
                    int targetCardIndex = rand.Next(0, state.Hands[enemyPlayer].Count);

                    // 만약 "지정해서 뺏기"를 구현하고 싶다면, targetPos.Col 등에 인덱스를 담아왔다고 가정하고 처리 가능:
                    // int targetCardIndex = targetPos.Col; 

                    ICard stolenCard = state.Hands[enemyPlayer][targetCardIndex];

                    // 내 손패 제한(8장)이 있다면 체크 로직 추가 가능
                    if (state.Hands[myPlayer].Count < 8)
                    {
                        // 상대 손패에서 제거
                        state.Hands[enemyPlayer].RemoveAt(targetCardIndex);

                        // 내 손패에 추가
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
            else if (Name == "턴 추가")
            {
                PlayerType myPlayer = state.CurrentTurn;

                // GameState에 한 번 더 행동할 수 있는 권한을 부여합니다.
                state.IsExtraTurnGranted = true;

               MainForm.Instance.AddLog($"{myPlayer}가 [턴 추가] 카드를 발동! 다음 상대방의 턴이 스킵됩니다.");
            }
            else if (Name == "랜덤 실행")
            {
                PlayerType myPlayer = state.CurrentTurn;
                Random rand = new Random();

                // 덱에 카드가 최소 2장 이상 있는지 확인
                if (state.SharedDeck.Count >= 2)
                {
                    MainForm.Instance.AddLog($"[랜덤 실행] 발동! {myPlayer}의 덱에서 카드를 2장 뽑아 즉시 시전합니다.");

                    for (int i = 0; i < 2; i++)
                    {
                        // 손패(Hands)로 옮기지 않고, 덱에서 바로 카드를 꺼냅니다.
                        ICard randomCard = state.SharedDeck.Pop();

                        // 이 카드가 타겟이 필요한 카드일 경우를 대비해 무작위 좌표를 하나 생성합니다.
                        // (체스판이 8x8이므로 0~7 사이의 무작위 row, col)
                        Position randomPos = new Position(rand.Next(0, 8), rand.Next(0, 8));

                        // 만약 '진화 카드'가 뽑혔다면 폰이 있는 좌표를 찾아서 넣어주면 더 자연스럽습니다.
                        if (randomCard.Type == CardType.Evolution)
                        {
                            // 보드 전체를 돌며 현재 플레이어의 '폰'이 있는 위치를 탐색
                            var pawns = new List<Position>();
                            for (int r = 0; r < 8; r++)
                            {
                                for (int c = 0; c < 8; c++)
                                {
                                    Position p = new Position(r, c);
                                    if (state.IsAllyPiece(p, myPlayer) && state.GetPieceAt(p).Type == PieceType.Pawn)
                                    {
                                        pawns.Add(p);
                                    }
                                }
                            }
                            // 만약 폰이 필드에 있다면, 그 중 하나의 좌표를 타겟으로 지정
                            if (pawns.Count > 0)
                            {
                                randomPos = pawns[rand.Next(0, pawns.Count)];
                            }
                        }

                        // 카드 자체의 Execute를 즉시 실행! (CardManager는 인자로 받은 것 그대로 배달)
                        MainForm.Instance.AddLog($" -> 무작위 발동 [{i + 1}번]: {randomCard.Name} (타겟 좌표: {randomPos.Row}, {randomPos.Col})");
                        randomCard.Execute(randomPos, state, cardManager);
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
