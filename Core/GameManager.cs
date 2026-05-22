using CardChess.Cards;
using CardChess.Models;
using CardChess.Pieces;
using System;
using System.Linq;

// 게임 관리 구현은 여기서

namespace CardChess.Core
{
    public class GameManager
    {
        public event Action OnTurnEndRequired;
        public GameState State { get; private set; }
        public CardManager CardMgr { get; private set; }
        public PlayerType CurrentTurn => State.CurrentTurn;
        public bool IsLocalAction { get; set; } = true;
        public event Action<string> OnNetworkBroadcast;

        public GameManager()
        {
            State = new GameState();
            CardMgr = new CardManager(State);
            InitializeBoard();
            CardMgr.InitializeDecks();

            CardMgr.DrawMultiple(PlayerType.Player1, 5);
            CardMgr.DrawMultiple(PlayerType.Player2, 5);
        }

        private void InitializeBoard() // 기물 배치 함수
        {
            for (int i = 0; i < 8; i++) // Player2의 기물 배치는 여기서 관리함 i값으로 간단하게 반복
            {
                if (i == 3) State.SetPieceAt(new Position(0, i), new Queen(PlayerType.Player2, new Position(0, i))); // 퀸 배치 함수
                else if (i == 4) State.SetPieceAt(new Position(0, i), new King(PlayerType.Player2, new Position(0, i))); // 킹 배치 함수
                else State.SetPieceAt(new Position(0, i), new Pawn(PlayerType.Player2, new Position(0, i))); // 그 외 폰 배치 함수
            }
            for (int i = 0; i < 8; i++)
                State.SetPieceAt(new Position(1, i), new Pawn(PlayerType.Player2, new Position(1, i)));

            for (int i = 0; i < 8; i++)
                State.SetPieceAt(new Position(6, i), new Pawn(PlayerType.Player1, new Position(6, i)));

            for (int i = 0; i < 8; i++) // 반대로 Player1의 기물 배치는 이걸로 관리됨
            {
                if (i == 3) State.SetPieceAt(new Position(7, i), new Queen(PlayerType.Player1, new Position(7, i))); // 얘는 퀸
                else if (i == 4) State.SetPieceAt(new Position(7, i), new King(PlayerType.Player1, new Position(7, i))); // 얘는 킹
                else State.SetPieceAt(new Position(7, i), new Pawn(PlayerType.Player1, new Position(7, i))); // 얜 당연히 폰 ㅇㅇ
            }
        }

        public bool IsAllyPiece(Position pos, PlayerType player) => State.IsAllyPiece(pos, player); // 동맹 기물 판단

        public void TryMoveOrAttack(Position from, Position to)
        {
            if (State.IsGameOver) return;

            IPiece piece = State.GetPieceAt(from);
            if (piece == null) return;

            if (piece.CanMove(to, State) || piece.CanAttack(to, State))
            {
                IPiece targetPiece = State.GetPieceAt(to);
                if (targetPiece != null && targetPiece.Owner == piece.Owner) return;

                if (targetPiece != null && targetPiece.Owner != piece.Owner)
                {
                    if (targetPiece.IsFrozen) return;

                    if (targetPiece.HasShield)
                    {
                        targetPiece.HasShield = false;
                        if (IsLocalAction) OnNetworkBroadcast?.Invoke($"MOVE,{from.Row},{from.Col},{to.Row},{to.Col}");

                        OnTurnEndRequired?.Invoke();
                        return;
                    }

                    if (targetPiece.Type == PieceType.King)
                    {
                        State.SetPieceAt(from, null);
                        State.SetPieceAt(to, piece);
                        piece.CurrentPosition = to;
                        State.IsGameOver = true;
                        State.Winner = piece.Owner;
                        if (IsLocalAction) OnNetworkBroadcast?.Invoke($"MOVE,{from.Row},{from.Col},{to.Row},{to.Col}");
                        return;
                    }

                    if (targetPiece.Owner == PlayerType.Player1) State.Player1DeadPieces.Add(targetPiece.Type);
                    else State.Player2DeadPieces.Add(targetPiece.Type);
                }

                State.SetPieceAt(from, null);
                State.SetPieceAt(to, piece);
                piece.CurrentPosition = to;

                if (IsLocalAction) OnNetworkBroadcast?.Invoke($"MOVE,{from.Row},{from.Col},{to.Row},{to.Col}");
                OnTurnEndRequired?.Invoke();
            }
        }

        public void PassTurn()
        {
            if (State.IsGameOver) return;
            if (IsLocalAction) OnNetworkBroadcast?.Invoke("PASS");

            OnTurnEndRequired?.Invoke();
        }

        public void TryUseCard(ICard card, Position targetPos)
        {
            if (State.IsGameOver || card == null) return;
            CardMgr.UseCard(card, targetPos, State.CurrentTurn);
            if (IsLocalAction) OnNetworkBroadcast?.Invoke($"CARD,{card.Name},{targetPos.Row},{targetPos.Col}");
        }

        /// <summary>
        /// 턴 교체 권한은 배틀매니저에게 위임하고, 엔드 페이즈용 순수 상태이상 데이터 클리닝만 수행합니다.
        /// </summary>
        public void CleanUpTurnEffects()
        {
            UpdatePieceStatusEffects();
            UpdateWallTurns();
        }

        private void UpdatePieceStatusEffects()
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    IPiece piece = State.GetPieceAt(new Position(r, c));
                    if (piece == null) continue;

                    if (piece.ShadowPosition != null)
                    {
                        if (piece.Owner == State.CurrentTurn) piece.ShadowTurns--;

                        if (piece.ShadowTurns <= 0)
                        {
                            Position origin = piece.ShadowPosition.Value;
                            IPiece occupyingPiece = State.GetPieceAt(origin);
                            if (occupyingPiece != null)
                            {
                                if (occupyingPiece.Owner == PlayerType.Player1) State.Player1DeadPieces.Add(occupyingPiece.Type);
                                else State.Player2DeadPieces.Add(occupyingPiece.Type);
                            }

                            State.SetPieceAt(origin, piece);
                            State.SetPieceAt(new Position(r, c), null);
                            piece.CurrentPosition = origin;
                            piece.ShadowPosition = null;
                        }
                    }

                    if (piece.IsFrozen && piece.Owner == State.CurrentTurn)
                    {
                        piece.IsFrozen = false;
                    }
                }
            }
        }

        private void UpdateWallTurns()
        {
            if (State.ActiveWalls == null || State.ActiveWalls.Count == 0) return;
            var keys = State.ActiveWalls.Keys.ToList();
            foreach (var key in keys)
            {
                State.ActiveWalls[key]--;
                if (State.ActiveWalls[key] <= 0) State.ActiveWalls.Remove(key);
            }
        }
    }
}

//using CardChess.Cards;
//using CardChess.Models;
//using CardChess.Pieces;
//using System;
//using System.Linq;

//namespace CardChess.Core
//{
//    public class GameManager
//    {
//        // 게임의 모든 상태(보드판, 손패, 턴 등)를 들고 있는 객체
//        public GameState State { get; private set; }
//        public CardManager CardMgr { get; private set; } // 카드 매니저 추가
//        // InputController에서 편하게 턴을 확인할 수 있도록 프로퍼티 제공
//        public PlayerType CurrentTurn => State.CurrentTurn;

//        public GameManager()
//        {
//            State = new GameState();
//            CardMgr = new CardManager(State); // 카드 매니저 생성
//            InitializeBoard(); // 게임 시작 시 초기 세팅
//            CardMgr.InitializeDecks(); // 덱 초기화 호출

//            CardMgr.DrawMultiple(PlayerType.Player1, 5); //5장씩 뽑음
//            CardMgr.DrawMultiple(PlayerType.Player2, 5);
//        }
//        public bool IsLocalAction { get; set; } = true; // 무한루프 방지용이랑 네트웨크 이벤트
//        public event Action<string> OnNetworkBroadcast;
//        // ♟️ 킹과 퀸은 원래 위치에, 나머지 모든 기물은 폰으로 배치하는 특수 초기화 로직
//        private void InitializeBoard()
//        {
//            // --- Player2 (위쪽 진영: Row 0, Row 1) --- 아래 방향(+1)으로 전진
//            // Row 0: 뒷줄 세팅 (Col 3: 퀸, Col 4: 킹, 나머지: 폰)
//            for (int i = 0; i < 8; i++)
//            {
//                if (i == 3)
//                    State.SetPieceAt(new Position(0, i), new Queen(PlayerType.Player2, new Position(0, i)));
//                else if (i == 4)
//                    State.SetPieceAt(new Position(0, i), new King(PlayerType.Player2, new Position(0, i)));
//                else
//                    State.SetPieceAt(new Position(0, i), new Pawn(PlayerType.Player2, new Position(0, i)));
//            }
//            // Row 1: 앞줄 8칸은 전부 폰
//            for (int i = 0; i < 8; i++)
//                State.SetPieceAt(new Position(1, i), new Pawn(PlayerType.Player2, new Position(1, i)));


//            // --- Player1 (아래쪽 진영: Row 6, Row 7) --- 위 방향(-1)으로 전진
//            // Row 6: 앞줄 8칸은 전부 폰
//            for (int i = 0; i < 8; i++)
//                State.SetPieceAt(new Position(6, i), new Pawn(PlayerType.Player1, new Position(6, i)));

//            // Row 7: 뒷줄 세팅 (Col 3: 퀸, Col 4: 킹, 나머지: 폰)
//            for (int i = 0; i < 8; i++)
//            {
//                if (i == 3)
//                    State.SetPieceAt(new Position(7, i), new Queen(PlayerType.Player1, new Position(7, i)));
//                else if (i == 4)
//                    State.SetPieceAt(new Position(7, i), new King(PlayerType.Player1, new Position(7, i)));
//                else
//                    State.SetPieceAt(new Position(7, i), new Pawn(PlayerType.Player1, new Position(7, i)));
//            }
//        }

//        // 🛡️ 아군 기물인지 확인 (InputController에서 클릭 검증용으로 사용)
//        public bool IsAllyPiece(Position pos, PlayerType player)
//        {
//            return State.IsAllyPiece(pos, player);
//        }

//        // ⚔️ 이동 또는 공격 시도 (InputController가 호출함)
//        public void TryMoveOrAttack(Position from, Position to)
//        {
//            if (State.IsGameOver)
//                return;

//            IPiece piece = State.GetPieceAt(from);
//            if (piece == null)
//                return;

//            bool canMove = piece.CanMove(to, State);
//            bool canAttack = piece.CanAttack(to, State);

//            if (canMove || canAttack)
//            {
//                IPiece targetPiece = State.GetPieceAt(to);

//                if (targetPiece != null && targetPiece.Owner == piece.Owner)
//                    return;

//                if (targetPiece != null && targetPiece.Owner != piece.Owner)
//                {
//                    // 존야 상태면 공격 불가
//                    if (targetPiece.IsFrozen)
//                    {
//                        Console.WriteLine("대상은 존야 상태이므로 공격할 수 없습니다!");
//                        return;
//                    }

//                    // 보호막이 있으면 보호막만 제거
//                    if (targetPiece.HasShield)
//                    {
//                        targetPiece.HasShield = false;
//                        if (IsLocalAction) OnNetworkBroadcast?.Invoke($"MOVE,{from.Row},{from.Col},{to.Row},{to.Col}");
//                        EndTurn(); // 내가한거 상대한테도 전송함
//                        Console.WriteLine("대상의 신성한 보호막이 공격을 1회 방어했습니다!");

//                        EndTurn();
//                        return;
//                    }

//                    // 왕을 잡으면 게임 종료
//                    if (targetPiece.Type == PieceType.King)
//                    {
//                        State.SetPieceAt(from, null);
//                        State.SetPieceAt(to, piece);
//                        piece.CurrentPosition = to;

//                        State.IsGameOver = true;
//                        State.Winner = piece.Owner;

//                        Console.WriteLine($"{piece.Owner}가 상대 킹을 잡았습니다. 게임 종료!");

//                        if (IsLocalAction) OnNetworkBroadcast?.Invoke($"MOVE,{from.Row},{from.Col},{to.Row},{to.Col}");
//                        return; // 위와 같이 상대에게 전송
//                    }

//                    // 일반 기물은 무덤으로 이동
//                    if (targetPiece.Owner == PlayerType.Player1)
//                        State.Player1DeadPieces.Add(targetPiece.Type);
//                    else
//                        State.Player2DeadPieces.Add(targetPiece.Type);

//                    Console.WriteLine($"{targetPiece.Owner}의 {targetPiece.Type}이(가) 파괴되어 무덤으로 이동했습니다.");
//                }

//                State.SetPieceAt(from, null);
//                State.SetPieceAt(to, piece);
//                piece.CurrentPosition = to;

//                // 정상적으로 이동/공격했을 때 전송
//                if (IsLocalAction) OnNetworkBroadcast?.Invoke($"MOVE,{from.Row},{from.Col},{to.Row},{to.Col}");
//                EndTurn();
//            }
//        }

//        // 🃏 카드 사용 시도 (InputController가 호출함)
//        public void TryUseCard(ICard card, Position targetPos)
//        {
//            if (State.IsGameOver)
//                return;

//            if (card == null)
//                return;

//            CardMgr.UseCard(card, targetPos, State.CurrentTurn);
//            if (IsLocalAction) OnNetworkBroadcast?.Invoke($"CARD,{card.Name},{targetPos.Row},{targetPos.Col}"); //또 전송 카드썼다고
//            // 카드는 사용해도 턴을 넘기지 않음
//            // 턴 변경은 기물 이동/공격 또는 턴 넘기기 버튼에서만 처리
//        }

//        // 임시였으나 내가 고쳤다!!!!
//        public void PassTurn()
//        {
//            if (State.IsGameOver)
//                return;

//            if (IsLocalAction) OnNetworkBroadcast?.Invoke("PASS"); // 턴넘기기 전송
//            EndTurn();
//        }


//        // 🔄 턴 넘기기
//        private void EndTurn()
//        {
//            if (State.IsGameOver)
//                return;

//            // 기물 상태 이상(요네 E, 존야 등) 업데이트
//            UpdatePieceStatusEffects();

//            // 깔려있는 애니비아 벽의 유지 턴수 감소
//            UpdateWallTurns();

//            State.CurrentTurn = (State.CurrentTurn == PlayerType.Player1)
//                                ? PlayerType.Player2
//                                : PlayerType.Player1;

//            CardMgr.DrawCard(State.CurrentTurn);
//        }


//        // [새로 추가할 함수] 턴이 지날 때마다 기물 상태 업데이트
//        private void UpdatePieceStatusEffects()
//        {
//            for (int r = 0; r < 8; r++)
//            {
//                for (int c = 0; c < 8; c++)
//                {
//                    IPiece piece = State.GetPieceAt(new Position(r, c));
//                    if (piece == null) continue;

//                    // [영혼 해방 (요네 E)] 턴 차감 및 강제 귀환 로직
//                    if (piece.ShadowPosition != null)
//                    {
//                        // 현재 턴의 주인 기물일 때만 턴을 깎음 (내 턴이 끝날 때 1 감소)
//                        if (piece.Owner == State.CurrentTurn)
//                        {
//                            piece.ShadowTurns--;
//                        }

//                        // 2턴이 다 지나서 0이 되면 원래 자리로 텔레포트!
//                        if (piece.ShadowTurns <= 0)
//                        {
//                            Position origin = piece.ShadowPosition.Value;
//                            Console.WriteLine($"[{piece.Type}] 영혼 해방 종료! {origin.Row},{origin.Col}로 강제 복귀합니다.");

//                            // 원래 위치에 누군가 서있다면? (그 기물을 파괴하고 내가 덮어씌움)
//                            IPiece occupyingPiece = State.GetPieceAt(origin);
//                            if (occupyingPiece != null)
//                            {
//                                Console.WriteLine($"복귀 지점에 있던 {occupyingPiece.Type}이(가) 짓밟혀 파괴되었습니다!");
//                                // 짓밟힌 기물(돌아올 위치에 있는 기물)을 소유자의 무덤으로 보냅니다.
//                                if (occupyingPiece.Owner == PlayerType.Player1)
//                                {
//                                    State.Player1DeadPieces.Add(occupyingPiece.Type);
//                                }
//                                else if (occupyingPiece.Owner == PlayerType.Player2)
//                                {
//                                    State.Player2DeadPieces.Add(occupyingPiece.Type);
//                                }
//                            }

//                            // 보드판 이동 처리
//                            State.SetPieceAt(origin, piece);
//                            State.SetPieceAt(new Position(r, c), null); // 지금 서있던 자리 비우기
//                            piece.CurrentPosition = origin;

//                            // 귀환했으니 그림자 위치 초기화
//                            piece.ShadowPosition = null;
//                        }
//                    }

//                    // [존야] 해제 로직 (1턴 뒤에 풀리게)
//                    if (piece.IsFrozen && piece.Owner == State.CurrentTurn)
//                    {
//                        piece.IsFrozen = false;
//                        Console.WriteLine($"{r},{c} 기물의 존야 상태가 해제되었습니다.");
//                    }
//                }
//            }
//        }
//        // [애니비아 벽 관리 함수] 턴이 지날 때마다 벽의 수명을 깎고 파괴함
//        private void UpdateWallTurns()
//        {
//            if (State.ActiveWalls == null || State.ActiveWalls.Count == 0) return;

//            // 딕셔너리를 돌면서 턴 감소 처리 (ToList()를 써야 안전하게 삭제 가능)
//            var keys = State.ActiveWalls.Keys.ToList();
//            foreach (var key in keys)
//            {
//                State.ActiveWalls[key]--;

//                // Duration이 다 달아서 0이 되면 벽 해제
//                if (State.ActiveWalls[key] <= 0)
//                {
//                    State.ActiveWalls.Remove(key);
//                    Console.WriteLine($"[지형 소멸] {key} 칸의 애니비아 벽이 녹아 사라졌습니다.");
//                }
//            }
//        }
//    }
//}