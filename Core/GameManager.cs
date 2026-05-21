using CardChess.Cards;
using CardChess.Models;
using CardChess.Pieces;
using System;
using System.Linq;

namespace CardChess.Core
{
    public class GameManager
    {
        // 게임의 모든 상태(보드판, 손패, 턴 등)를 들고 있는 객체
        public GameState State { get; private set; }
        public CardManager CardMgr { get; private set; } // 카드 매니저 추가
        // InputController에서 편하게 턴을 확인할 수 있도록 프로퍼티 제공
        public PlayerType CurrentTurn => State.CurrentTurn;

        public GameManager()
        {
            State = new GameState();
            CardMgr = new CardManager(State); // 카드 매니저 생성
            InitializeBoard(); // 게임 시작 시 초기 세팅
            CardMgr.InitializeDecks(); // 덱 초기화 호출
        }

        // ♟️ 킹과 퀸은 원래 위치에, 나머지 모든 기물은 폰으로 배치하는 특수 초기화 로직
        private void InitializeBoard()
        {
            // --- Player2 (위쪽 진영: Row 0, Row 1) --- 아래 방향(+1)으로 전진
            // Row 0: 뒷줄 세팅 (Col 3: 퀸, Col 4: 킹, 나머지: 폰)
            for (int i = 0; i < 8; i++)
            {
                if (i == 3)
                    State.SetPieceAt(new Position(0, i), new Queen(PlayerType.Player2, new Position(0, i)));
                else if (i == 4)
                    State.SetPieceAt(new Position(0, i), new King(PlayerType.Player2, new Position(0, i)));
                else
                    State.SetPieceAt(new Position(0, i), new Pawn(PlayerType.Player2, new Position(0, i)));
            }
            // Row 1: 앞줄 8칸은 전부 폰
            for (int i = 0; i < 8; i++)
                State.SetPieceAt(new Position(1, i), new Pawn(PlayerType.Player2, new Position(1, i)));


            // --- Player1 (아래쪽 진영: Row 6, Row 7) --- 위 방향(-1)으로 전진
            // Row 6: 앞줄 8칸은 전부 폰
            for (int i = 0; i < 8; i++)
                State.SetPieceAt(new Position(6, i), new Pawn(PlayerType.Player1, new Position(6, i)));

            // Row 7: 뒷줄 세팅 (Col 3: 퀸, Col 4: 킹, 나머지: 폰)
            for (int i = 0; i < 8; i++)
            {
                if (i == 3)
                    State.SetPieceAt(new Position(7, i), new Queen(PlayerType.Player1, new Position(7, i)));
                else if (i == 4)
                    State.SetPieceAt(new Position(7, i), new King(PlayerType.Player1, new Position(7, i)));
                else
                    State.SetPieceAt(new Position(7, i), new Pawn(PlayerType.Player1, new Position(7, i)));
            }
        }

        // 🛡️ 아군 기물인지 확인 (InputController에서 클릭 검증용으로 사용)
        public bool IsAllyPiece(Position pos, PlayerType player)
        {
            return State.IsAllyPiece(pos, player);
        }

        // ⚔️ 이동 또는 공격 시도 (InputController가 호출함)
        public void TryMoveOrAttack(Position from, Position to)
        {
            IPiece piece = State.GetPieceAt(from);
            if (piece == null) return;

            // 정우가 만든 기물 로직(CanMove, CanAttack)을 여기서 물어봄!
            bool canMove = piece.CanMove(to, State);
            bool canAttack = piece.CanAttack(to, State);

            if (canMove || canAttack)
            {
                IPiece targetPiece = State.GetPieceAt(to);

                if (targetPiece != null && targetPiece.Owner != piece.Owner)
                {
                    // [존야 방어] 타겟이 존야 상태라면 무적이므로 공격 불가능 (턴도 안 넘김)
                    if (targetPiece.IsFrozen)
                    {
                        Console.WriteLine("대상은 존야 상태이므로 공격할 수 없습니다!");
                        return;
                    }

                    // 신성한 보호막 방어] 타겟이 보호막을 가지고 있다면?
                    if (targetPiece.HasShield)
                    {
                        targetPiece.HasShield = false; // 보호막만 파괴
                        Console.WriteLine("대상의 신성한 보호막이 공격을 1회 방어했습니다!");

                        // 공격을 하긴 했으니 턴은 넘기되, 이동(덮어씌우기)은 취소함
                        EndTurn();
                        return;
                    }

                    // [무덤 시스템] 일반 타격 성공 시: 적 기물을 부활용 무덤 리스트로 보냄
                    // (주의: GameState.cs 안에 Player1DeadPieces, Player2DeadPieces 리스트가 선언되어 있어야 합니다!)
                    if (targetPiece.Owner == PlayerType.Player1)
                        State.Player1DeadPieces.Add(targetPiece.Type);
                    else
                        State.Player2DeadPieces.Add(targetPiece.Type);

                    Console.WriteLine($"{targetPiece.Owner}의 {targetPiece.Type}이(가) 파괴되어 무덤으로 이동했습니다.");
                }
                // 이전 자리 비우고, 새 자리에 기물 넣기 (실제 이동 처리)
                State.SetPieceAt(from, null);
                State.SetPieceAt(to, piece);

                // 기물 내부의 현재 좌표 데이터도 반드시 갱신해주어야 버그가 안 납니다!
                piece.CurrentPosition = to;

                // 행동을 마쳤으니 턴 종료
                EndTurn();
            }
        }

        // 🃏 카드 사용 시도 (InputController가 호출함)
        public void TryUseCard(ICard card, Position targetPos)
        {
            // TODO: 나중에 정우 님이 ICard 인터페이스에 Use() 함수를 만들면 여기서 실행
            // 예: card.Use(State, targetPos);
            CardMgr.UseCard(card, targetPos, State.CurrentTurn);
            // 카드 사용 후 턴 종료
            EndTurn();
        }


        // 🔄 턴 넘기기
        private void EndTurn()
        {
            // 기물 상태 이상(요네 E, 존야 등) 업데이트
            UpdatePieceStatusEffects();

            // 깔려있는 애니비아 벽의 유지 턴수 감소
            UpdateWallTurns();

            State.CurrentTurn = (State.CurrentTurn == PlayerType.Player1)
                                ? PlayerType.Player2
                                : PlayerType.Player1;
            CardMgr.DrawCard(State.CurrentTurn);
            State.CurrentTurn = (State.CurrentTurn == PlayerType.Player1)
                                ? PlayerType.Player2
                                : PlayerType.Player1;
            CardMgr.DrawCard(State.CurrentTurn);
        }
        // [새로 추가할 함수] 턴이 지날 때마다 기물 상태 업데이트
        private void UpdatePieceStatusEffects()
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    IPiece piece = State.GetPieceAt(new Position(r, c));
                    if (piece == null) continue;

                    // [영혼 해방 (요네 E)] 턴 차감 및 강제 귀환 로직
                    if (piece.ShadowPosition != null)
                    {
                        // 현재 턴의 주인 기물일 때만 턴을 깎음 (내 턴이 끝날 때 1 감소)
                        if (piece.Owner == State.CurrentTurn)
                        {
                            piece.ShadowTurns--;
                        }

                        // 2턴이 다 지나서 0이 되면 원래 자리로 텔레포트!
                        if (piece.ShadowTurns <= 0)
                        {
                            Position origin = piece.ShadowPosition.Value;
                            Console.WriteLine($"[{piece.Type}] 영혼 해방 종료! {origin.Row},{origin.Col}로 강제 복귀합니다.");

                            // 원래 위치에 누군가 서있다면? (그 기물을 파괴하고 내가 덮어씌움)
                            IPiece occupyingPiece = State.GetPieceAt(origin);
                            if (occupyingPiece != null)
                            {
                                Console.WriteLine($"복귀 지점에 있던 {occupyingPiece.Type}이(가) 짓밟혀 파괴되었습니다!");
                                // 짓밟힌 기물(돌아올 위치에 있는 기물)을 소유자의 무덤으로 보냅니다.
                                if (occupyingPiece.Owner == PlayerType.Player1)
                                {
                                    State.Player1DeadPieces.Add(occupyingPiece.Type);
                                }
                                else if (occupyingPiece.Owner == PlayerType.Player2)
                                {
                                    State.Player2DeadPieces.Add(occupyingPiece.Type);
                                }
                            }

                            // 보드판 이동 처리
                            State.SetPieceAt(origin, piece);
                            State.SetPieceAt(new Position(r, c), null); // 지금 서있던 자리 비우기
                            piece.CurrentPosition = origin;

                            // 귀환했으니 그림자 위치 초기화
                            piece.ShadowPosition = null;
                        }
                    }

                    // [존야] 해제 로직 (1턴 뒤에 풀리게)
                    if (piece.IsFrozen && piece.Owner == State.CurrentTurn)
                    {
                        piece.IsFrozen = false;
                        Console.WriteLine($"{r},{c} 기물의 존야 상태가 해제되었습니다.");
                    }
                }
            }
        }
        // [애니비아 벽 관리 함수] 턴이 지날 때마다 벽의 수명을 깎고 파괴함
        private void UpdateWallTurns()
        {
            if (State.ActiveWalls == null || State.ActiveWalls.Count == 0) return;

            // 딕셔너리를 돌면서 턴 감소 처리 (ToList()를 써야 안전하게 삭제 가능)
            var keys = State.ActiveWalls.Keys.ToList();
            foreach (var key in keys)
            {
                State.ActiveWalls[key]--;

                // Duration이 다 달아서 0이 되면 벽 해제
                if (State.ActiveWalls[key] <= 0)
                {
                    State.ActiveWalls.Remove(key);
                    Console.WriteLine($"[지형 소멸] {key} 칸의 애니비아 벽이 녹아 사라졌습니다.");
                }
            }
        }
    }
}