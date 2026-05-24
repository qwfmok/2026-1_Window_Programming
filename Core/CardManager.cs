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
            List<ICard> deckList = new List<ICard>();

            // 40장을 맞추기 위해 전체 카드 세트를 2번 반복해서 넣습니다 (총 42장)
            for (int loop = 0; loop < 2; loop++)
            {
                for (int i = 0; i < 3; i++)
                {
                    deckList.Add(new EvolutionCard("기사 서품", "폰을 나이트로 진화", PieceType.Knight));
                    deckList.Add(new EvolutionCard("골렘 연성", "폰을 룩으로 진화", PieceType.Rook));
                    deckList.Add(new EvolutionCard("사제 서품", "폰을 비숍으로 진화", PieceType.Bishop));
                }

                deckList.Add(new FieldSkillCard("방벽 건설", "지정한 빈칸에 2턴간 유지되는 벽 생성"));
                deckList.Add(new FieldSkillCard("증원", "지정한 빈칸에 내 폰을 1기 소환"));
                deckList.Add(new ActiveSkillCard("욕망의 항아리", "덱에서 카드를 2장 뽑습니다."));
                deckList.Add(new ActiveSkillCard("생각의 압수", "모두 손패를 버리고 다시 뽑습니다."));
                deckList.Add(new ActiveSkillCard("완벽한 약탈", "상대의 손패 1장을 빼앗아옵니다."));
                deckList.Add(new ActiveSkillCard("시간 왜곡", "상대의 다음 턴을 스킵합니다."));
                deckList.Add(new ActiveSkillCard("도둑들의 경매", "덱에서 카드 2장을 무작위로 즉시 시전"));
                deckList.Add(new TargetSkillCard("부활", "내 진영의 빈칸에 내 기물 부활"));
                deckList.Add(new TargetSkillCard("컨트롤 탈취", "상대 기물 1개의 소유권 강탈"));
                deckList.Add(new TargetSkillCard("기물 위치교환", "내 무작위 기물과 위치 교환"));
                deckList.Add(new TargetSkillCard("신성한 보호막", "공격을 1회 막아주는 보호막 부여"));
                deckList.Add(new TargetSkillCard("영혼 해방", "내 기물이 2턴 뒤 원래 위치로 복귀"));
                deckList.Add(new TargetSkillCard("존야", "1턴 동안 무적 및 행동 불가"));
                deckList.Add(new TargetSkillCard("복제", "인접한 빈칸 중 1곳에 기물 복제"));
                deckList.Add(new TargetSkillCard("판도라", "무작위 기물로 변이 (킹 제외)"));
                deckList.Add(new TrapCard("갬블 게임", "50% 확률로 공격 반사 및 파괴"));
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

            if (!_state.Hands.ContainsKey(player))
                return;

            // 손패가 8장이면 더 이상 뽑지 않음
            if (_state.Hands[player].Count >= maxHandCount)
            {
                MainForm.Instance.AddLog($"[{player}] 손패가 8장이라 더 이상 카드를 뽑을 수 없습니다.");
                return;
            }

            if (_state.SharedDeck.Count > 0)
            {
                ICard card = _state.SharedDeck.Pop();
                _state.Hands[player].Add(card);
            }
            else
            {
                MainForm.Instance.AddLog("[경고] 덱에 남은 카드가 없습니다!");
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