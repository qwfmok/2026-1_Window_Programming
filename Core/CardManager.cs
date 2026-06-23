using System;
using System.Collections.Generic;
using System.Linq;
using CardChess.Models;
using CardChess.Cards;

/// 덱 로직 구현은 여기서

/// 카드명 변경의 경우에는 이 덱 부분에서 관리한 후
/// 해당 카드의 타입에 맞는 클래스와 Main Form의 딕셔너리에서도 관리하여 파일명과 꼬이지 않도록 해야 함

namespace CardChess.Core
{
    public class CardManager
    {
        private GameState _state;
        private SynchronizedRandom _random;

        public CardManager(GameState state)
        {
            _state = state;
        }

        public void InitializeDecks(int seed)
        {
            _random = new SynchronizedRandom(seed);
            _state.SharedRandom = _random;
            List<ICard> deckList = new List<ICard>();

            for (int loop = 0; loop < 2; loop++)
            {
                for (int i = 0; i < 3; i++)
                {
                    deckList.Add(new EvolutionCard("기사 진화", "폰을 나이트로 진화", PieceType.Knight));
                    deckList.Add(new EvolutionCard("룩 진화", "폰을 룩으로 진화", PieceType.Rook));
                    deckList.Add(new EvolutionCard("비숍 진화", "폰을 비숍으로 진화", PieceType.Bishop));
                }

                deckList.Add(new FieldSkillCard("방벽 건설", "지정한 빈칸에 2턴간 유지되는 벽 생성"));
                deckList.Add(new FieldSkillCard("증원", "지정한 빈칸에 내 폰을 1기 소환"));
                deckList.Add(new ActiveSkillCard("두장 뽑기", "덱에서 카드를 2장 뽑습니다."));
                deckList.Add(new ActiveSkillCard("손패 교환", "모두 손패를 버리고 다시 뽑습니다."));
                deckList.Add(new ActiveSkillCard("카드 뺏기", "상대의 손패 1장을 빼앗아옵니다."));
                deckList.Add(new ActiveSkillCard("시간 왜곡", "상대의 다음 턴을 스킵합니다."));
                deckList.Add(new ActiveSkillCard("랜덤 시전", "덱에서 카드 2장을 무작위로 즉시 시전"));
                deckList.Add(new TargetSkillCard("부활", "내 진영의 빈칸에 내 기물 부활"));
                deckList.Add(new TargetSkillCard("기물 뺏기", "상대 본진 밖의 킹/퀸이 아닌 기물 1개 강탈"));
                deckList.Add(new TargetSkillCard("위치 교환", "내 무작위 기물과 위치 교환"));
                deckList.Add(new TargetSkillCard("봉인", "1턴 동안 무적 및 행동 불가"));
                deckList.Add(new TargetSkillCard("복제", "인접한 빈칸 중 1곳에 기물 복제"));
                deckList.Add(new TargetSkillCard("랜덤 진화", "내 기물을 무작위 기물로 변이 (킹 제외)"));
                deckList.Add(new TrapCard("랜덤 방어", "50% 확률로 공격 반사 및 파괴"));
            }

            // 덱 셔플 후 공용 덱으로 지정
            var shuffled = deckList.OrderBy(x => _random.Next()).ToList();
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

            // 탈진 로직 | 덱 잔량이 0이라면 버려진 카드들로 덱 재구성
            if (_state.SharedDeck.Count == 0 && _state.DiscardPile.Count > 0)
            {
                MainForm.Instance.AddLog("덱을 모두 소모하여 무덤의 카드를 다시 섞습니다!");

                // 무덤 셔플
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
                bool removedFromHand = _state.Hands.ContainsKey(player) && _state.Hands[player].Remove(card);
                card.Execute(targetPos, _state, this);
                if (removedFromHand)
                {
                    _state.DiscardPile.Add(card);
                }
            }
        }

        public void BeginRandomCapture()
        {
            _random?.BeginCapture();
        }

        public List<int> EndRandomCapture()
        {
            return _random?.EndCapture() ?? new List<int>();
        }

        public void QueueRandomReplay(IEnumerable<int> values)
        {
            _random?.QueueReplay(values);
        }
    }

    public sealed class SynchronizedRandom : Random
    {
        private readonly Queue<int> replayValues = new Queue<int>();
        private readonly List<int> capturedValues = new List<int>();
        private bool isCapturing;

        public SynchronizedRandom(int seed) : base(seed)
        {
        }

        public void BeginCapture()
        {
            capturedValues.Clear();
            isCapturing = true;
        }

        public List<int> EndCapture()
        {
            isCapturing = false;
            return new List<int>(capturedValues);
        }

        public void QueueReplay(IEnumerable<int> values)
        {
            replayValues.Clear();
            if (values == null) return;

            foreach (int value in values)
            {
                replayValues.Enqueue(value);
            }
        }

        public override int Next()
        {
            int generated = base.Next();
            return ResolveValue(generated, 0, int.MaxValue);
        }

        public override int Next(int maxValue)
        {
            int generated = base.Next(maxValue);
            return ResolveValue(generated, 0, maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            int generated = base.Next(minValue, maxValue);
            return ResolveValue(generated, minValue, maxValue);
        }

        private int ResolveValue(int generated, int minValue, int maxValue)
        {
            int value = generated;
            if (replayValues.Count > 0)
            {
                int replay = replayValues.Dequeue();
                if (replay >= minValue && replay < maxValue)
                {
                    value = replay;
                }
            }

            if (isCapturing)
            {
                capturedValues.Add(value);
            }

            return value;
        }
    }
}
