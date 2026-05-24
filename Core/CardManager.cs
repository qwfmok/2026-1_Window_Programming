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
        private Random _random;

        public CardManager(GameState state)
        {
            _state = state;
        }

        public void InitializeDecks(int seed)
        {
            _random = new Random(seed);
            _state.SharedRandom = _random;
            List<ICard> deckList = new List<ICard>();

            // 40장을 맞추기 위해 전체 카드 세트를 2번 반복해서 넣습니다 (총 42장)
            for (int loop = 0; loop < 2; loop++)
            {
                for (int i = 0; i < 3; i++)
                {
                    deckList.Add(new EvolutionCard("기사 서품", "폰을 나이트로 진화", PieceType.Knight));
                    deckList.Add(new EvolutionCard("성벽 강화", "폰을 룩으로 진화", PieceType.Rook));
                    deckList.Add(new EvolutionCard("사제 서품", "폰을 비숍으로 진화", PieceType.Bishop));
                }

                deckList.Add(new FieldSkillCard("애니비아 벽", "지정한 빈칸에 2턴간 유지되는 벽 생성"));
                deckList.Add(new FieldSkillCard("폰 소환", "지정한 빈칸에 내 폰을 1기 소환"));
                deckList.Add(new ActiveSkillCard("욕망의 항아리", "덱에서 카드를 2장 뽑습니다."));
                deckList.Add(new ActiveSkillCard("패 털이", "모두 손패를 버리고 다시 뽑습니다."));
                deckList.Add(new ActiveSkillCard("카드 뺏기", "상대의 손패 1장을 빼앗아옵니다."));
                deckList.Add(new ActiveSkillCard("턴 추가", "상대의 다음 턴을 스킵합니다."));
                deckList.Add(new ActiveSkillCard("랜덤 실행", "덱에서 카드 2장을 무작위로 즉시 시전"));
                deckList.Add(new TargetSkillCard("부활", "내 진영의 빈칸에 내 기물 부활"));
                deckList.Add(new TargetSkillCard("컨트롤 탈취", "상대 기물 1개의 소유권 강탈"));
                deckList.Add(new TargetSkillCard("기물 위치교환", "내 무작위 기물과 위치 교환"));
                deckList.Add(new TargetSkillCard("신성한 보호막", "공격을 1회 막아주는 보호막 부여"));
                deckList.Add(new TargetSkillCard("영혼 해방", "내 기물이 2턴 뒤 원래 위치로 복귀"));
                deckList.Add(new TargetSkillCard("존야", "1턴 동안 무적 및 행동 불가"));
                deckList.Add(new TargetSkillCard("복제", "인접한 빈칸 중 1곳에 기물 복제"));
                deckList.Add(new TargetSkillCard("판도라", "무작위 기물로 변이 (킹 제외)"));
                deckList.Add(new TrapCard("동전 던지기", "50% 확률로 공격 반사 및 파괴"));
            }

            // 섞기 (셔플)
            var shuffled = deckList.OrderBy(x => _random.Next()).ToList();

            // 공용 덱에 밀어넣기
            _state.SharedDeck.Clear();
            foreach (var card in shuffled)
            {
                _state.SharedDeck.Push(card);
            }
        }

        public void DrawCard(PlayerType player)
        {
            const int maxHandCount = 8;
            if (!_state.Hands.ContainsKey(player)) return;

            if (_state.Hands[player].Count >= maxHandCount)
            {
                MainForm.Instance.AddLog($"[{player}] 손패가 8장이라 더 이상 카드를 뽑을 수 없습니다.");
                return;
            }

            // 🌟 [추가] 덱을 다 썼는데 무덤에 카드가 있다면? 무덤을 섞어서 다시 덱으로!
            if (_state.SharedDeck.Count == 0 && _state.DiscardPile.Count > 0)
            {
                MainForm.Instance.AddLog("덱을 모두 소모하여 무덤의 카드를 다시 섞습니다!");
                // 공용 주사위로 무덤 섞기
                var shuffled = _state.DiscardPile.OrderBy(x => _state.SharedRandom.Next()).ToList();
                _state.DiscardPile.Clear();
                foreach (var c in shuffled) _state.SharedDeck.Push(c);
            }

            if (_state.SharedDeck.Count > 0)
            {
                ICard card = _state.SharedDeck.Pop();
                _state.Hands[player].Add(card);
            }
            else
            {
                MainForm.Instance.AddLog("[경고] 무덤까지 비어서 더 이상 뽑을 카드가 없습니다!");
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
                if (_state.Hands.ContainsKey(player))
                {
                    _state.Hands[player].Remove(card);
                    _state.DiscardPile.Add(card); // 다 쓴 카드를 허공에 버리지 않고 무덤에 감
                }
            }
        }
    }
}