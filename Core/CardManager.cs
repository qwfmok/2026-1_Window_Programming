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

                // 2. 필드 스킬 카드 추가
                deckList.Add(new FieldSkillCard("애니비아 벽", "지정한 빈칸에 2턴간 유지되는 이동/공격 불가 벽을 생성합니다."));
                deckList.Add(new FieldSkillCard("폰 소환", "지정한 빈칸에 내 폰을 1기 소환합니다."));

                // 3. 액티브 스킬 카드 추가
                deckList.Add(new ActiveSkillCard("욕망의 항아리", "덱에서 카드를 2장 뽑습니다."));
                deckList.Add(new ActiveSkillCard("패 털이", "양 플레이어 모두 손패를 버리고, 버린 장수만큼 다시 뽑습니다."));
                deckList.Add(new ActiveSkillCard("카드 뺏기", "상대의 손패 중 1장을 무작위로 빼앗아옵니다."));
                deckList.Add(new ActiveSkillCard("턴 추가", "상대의 다음 턴을 스킵하고 내 턴을 한 번 더 진행합니다."));
                deckList.Add(new ActiveSkillCard("랜덤 실행", "내 덱에서 카드 2장을 무작위로 뽑아 즉시 시전합니다."));

                // 4. 타겟 스킬 카드 추가
                deckList.Add(new TargetSkillCard("부활", "내 진영의 빈칸에 내 무덤의 기물을 부활시킵니다."));
                deckList.Add(new TargetSkillCard("컨트롤 탈취", "상대 기물 1개의 소유권을 내 것으로 만듭니다. (킹, 퀸 제외)"));
                deckList.Add(new TargetSkillCard("기물 위치교환", "지정한 내 기물과 다른 내 무작위 기물의 위치를 바꿉니다."));
                deckList.Add(new TargetSkillCard("신성한 보호막", "내 기물에 공격을 1회 막아주는 보호막을 부여합니다."));
                deckList.Add(new TargetSkillCard("영혼 해방", "요네 E. 내 기물이 2턴 뒤 원래 위치로 강제 복귀합니다."));
                deckList.Add(new TargetSkillCard("존야", "내 기물이 1턴 동안 무적 및 행동 불가 상태가 됩니다."));
                deckList.Add(new TargetSkillCard("복제", "내 기물을 인접한 빈칸 중 1곳에 똑같이 복제합니다."));
                deckList.Add(new TargetSkillCard("판도라", "내 기물을 다른 무작위 기물로 변이시킵니다. (킹 제외)"));

                // 5. 함정 카드 추가
                deckList.Add(new TrapCard("동전 던지기", "상대 공격 시 50% 확률로 공격을 반사하여 상대 기물을 파괴합니다."));
                
                // 셔플
                var shuffled = deckList.OrderBy(x => _random.Next()).ToList();

                // GameState의 스택에 푸시
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