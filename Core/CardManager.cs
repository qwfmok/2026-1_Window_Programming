using System;
using System.Collections.Generic;
using System.Linq;
using CardChess.Models;
using CardChess.Cards;

namespace CardChess.Core
{
    public class CardManager
    {
        private GameState _state;
        private Random _random = new Random();

        public CardManager(GameState state)
        {
            _state = state;
        }

        public void InitializeDecks()
        {
            foreach (PlayerType player in Enum.GetValues(typeof(PlayerType)))
            {
                // Enum.GetValues는 모든 값을 가져오므로 None 같은 값이 있다면 예외처리 필요
                // if (player == PlayerType.None) continue;

                List<ICard> deckList = new List<ICard>();

                // 1. 진화 카드 추가
                // 생성자 매개변수 확인: (이름, 설명, 결과기물타입, 코스트)
                for (int i = 0; i < 3; i++)
                {
                    deckList.Add(new EvolutionCard("기사 서품", "폰을 나이트로 진화", PieceType.Knight));
                    deckList.Add(new EvolutionCard("성벽 강화", "폰을 룩으로 진화", PieceType.Rook));
                    deckList.Add(new EvolutionCard("사제 서품", "폰을 비숍으로 진화", PieceType.Bishop));
                }

                // 2. 스킬 카드 추가
                // 생성자 매개변수 확인: (이름, 설명)
                deckList.Add(new ActiveSkillCard("욕망의 항아리", "카드 2장 드로우"));
                deckList.Add(new ActiveSkillCard("패 털이", "모두 버리고 다시 뽑기"));

                // 3. 셔플
                var shuffled = deckList.OrderBy(x => _random.Next()).ToList();

                // 4. GameState의 스택에 푸시
                if (_state.Decks.ContainsKey(player))
                {
                    _state.Decks[player].Clear();
                    foreach (var card in shuffled)
                    {
                        _state.Decks[player].Push(card);
                    }
                }
            }
        }

        public void DrawCard(PlayerType player)
        {
            // 덱과 손패 리스트가 초기화되어 있는지 확인 필수
            if (_state.Decks.ContainsKey(player) && _state.Decks[player].Count > 0)
            {
                ICard card = _state.Decks[player].Pop();
                if (_state.Hands.ContainsKey(player))
                {
                    _state.Hands[player].Add(card);
                }
            }
        }
        public void DrawMultiple(PlayerType player, int count)
        {
            for (int i = 0; i < count; i++)
            {
                DrawCard(player);
            }
        }
        public void UseCard(ICard card, Position targetPos, PlayerType player)
        {
            if (card == null) return;

            if (card.CanUse(targetPos, _state))
            {
                card.Execute(targetPos, _state, this);

                // 사용 후 손패에서 제거
                if (_state.Hands.ContainsKey(player))
                {
                    _state.Hands[player].Remove(card);
                }
            }
        }
    }
}