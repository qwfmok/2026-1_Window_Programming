using CardChess.Core;
using CardChess.Models;
using System;
using System.Threading.Tasks;

/// 전투 중추 구현은 여기서

/// 턴 페이즈 내부에서 관리하는 배틀매니저로 기존 턴매니저의 역할을 어느정도 승계시키기로 함

namespace CardChess
{
    public enum BattlePhase
    {
        Phase0_TurnCheck,
        Phase1_Draw,
        Phase2_Play,
        Phase3_End
    }

    public class BattleManager
    {
        private GameManager gameManager;
        private BattlePhase currentPhase = BattlePhase.Phase0_TurnCheck;

        // [수정] BattleManager 자체의 턴 추가 변수는 혼동을 주므로 삭제
        // public bool IsExtraTurnGranted { get; set; } = false;
        public bool IsPlayable => currentPhase == BattlePhase.Phase2_Play;

        // UI를 새로고침하라고 알려주는 알람
        public event Action<PlayerType> OnTurnChanged;
        public event Action<BattlePhase> OnPhaseChanged;

        public BattleManager(GameManager manager)
        {
            this.gameManager = manager;
        }

        public async Task ProcessNextPhase()
        {
            switch (currentPhase)
            {
                // 턴 체크 페이즈 -> 0.5초의 딜레이로 배틀페이즈 체크
                case BattlePhase.Phase0_TurnCheck:
                    OnTurnChanged?.Invoke(gameManager.State.CurrentTurn);
                    await Task.Delay(500);

                    currentPhase = BattlePhase.Phase1_Draw;
                    await ProcessNextPhase();
                    break;
                // 드로우 페이즈 -> 패에 카드를 증가시키는 시점
                case BattlePhase.Phase1_Draw:
                    gameManager.CardMgr.DrawCard(gameManager.State.CurrentTurn);
                    currentPhase = BattlePhase.Phase2_Play;
                    OnPhaseChanged?.Invoke(currentPhase);

                    await ProcessNextPhase();
                    break;

                // 플레이 페이즈 -> 플레이어의 턴 조작 대기
                case BattlePhase.Phase2_Play: 
                    break;

                // 엔드 페이즈 -> 상태이상, 잔존효과 처리
                case BattlePhase.Phase3_End:
                    gameManager.CleanUpTurnEffects();

                    if (gameManager.State.IsExtraTurnGranted)
                    {
                        gameManager.State.IsExtraTurnGranted = false;
                        OnTurnChanged?.Invoke(gameManager.State.CurrentTurn);
                    }
                    else
                    {
                        gameManager.State.CurrentTurn = (gameManager.State.CurrentTurn == PlayerType.Player1)
                            ? PlayerType.Player2
                            : PlayerType.Player1;
                    }
                    currentPhase = BattlePhase.Phase0_TurnCheck;
                    await ProcessNextPhase();
                    break;
            }
        }

        public async void RequestTurnEnd() // 턴 종료
        {
            if (currentPhase == BattlePhase.Phase2_Play)
            {
                currentPhase = BattlePhase.Phase3_End;
                await ProcessNextPhase();
            }
        }
    }
}