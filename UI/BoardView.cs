using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CardChess.Core;
using CardChess.Models;
using CardChess.Pieces;

// 보드의 실제 출력은 여기서 구현

namespace CardChess.View
{
    public class BoardView
    {
        private Panel pnlBoard; // 윈도우 폼 컨트롤 중 패널보드 기반
        private GameManager gameManager;
        public float XOffset { get; private set; }
        public float YOffset { get; private set; }
        public float CellWidth { get; private set; }
        public float CellHeight { get; private set; }

        private Image boardImage = null;   // 보드 이미지
        private Image boardBgImage = null; // 보드 프레임 이미지

        private Dictionary<IPiece, PieceAnime> pieceAnimations = new Dictionary<IPiece, PieceAnime>();

        public Dictionary<IPiece, PieceAnime> PieceAnimations => pieceAnimations;

        public BoardView(Panel pnlBoard, GameManager gameManager)
        {
            this.pnlBoard = pnlBoard;
            this.gameManager = gameManager;

            InitResources();
            CalculateBoardDimensions();
        }

        private void InitResources()
        {
            string framePath = Path.Combine(Application.StartupPath, "Assets", "background.png"); // 이걸로 보드 프레임 로딩
            if (File.Exists(framePath)) boardBgImage = Image.FromFile(framePath);
            string boardPath = Path.Combine(Application.StartupPath, "Assets", "board.png"); // 이건 보드 로딩
            if (File.Exists(boardPath)) boardImage = Image.FromFile(boardPath);
            // 파일 경로는 둘 다 바이너리/디버그 안에 Assets 폴더임

            typeof(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                         ?.SetValue(pnlBoard, true);
            // 화면 갱신 시 깜빡이는 버퍼링 방지
        }

        public void CalculateBoardDimensions()
        {
            if (pnlBoard == null) return;

            // background.png의 비대칭 비율을 반영함
            XOffset = pnlBoard.Width * (60.5f / 996f); // 앞자리 숫자 높이거나 낮추면 보드 프레임이 좌우로 이동함 중간값은 60.5
            YOffset = pnlBoard.Height * (50f / 1012f); // 앞자리 숫자 높이거나 낮추면 마찬가지로 프레임은 상하로 이동함 중간값은 50

            float gridWidth = pnlBoard.Width * (880f / 996f);
            float gridHeight = pnlBoard.Height * (880f / 1012f);

            CellWidth = gridWidth / BoardManager.MAX_COL;
            CellHeight = gridHeight / BoardManager.MAX_ROW;
        }

        // 얘는 마우스 좌표 인식하는 로직임

        public bool TryConvertPixelToPosition(int mouseX, int mouseY, out Position position)
        {
            position = default; // 변수초기화
            CalculateBoardDimensions();

            int col = (int)((mouseX - XOffset) / CellWidth);
            int row = (int)((mouseY - YOffset) / CellHeight);

            if (CardChess.Core.BoardManager.IsValidPosition(row, col))
            {
                position = new Position(row, col);
                return true; // 마우스가 보드 내부에서 특정 칸을 클릭했을 때고
            }
            return false; // 이건 반대로 마우스가 보드판 바깥을 클릭함
        }

        public void DrawBoard(Graphics g)
        {
            // Graphics 객체가 Dispose되었거나 상태가 나쁘면 즉시 리턴 (안전장치)
            if (g == null) return;
            try { g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; } catch { return; }

            // 테두리 프레임 그리기
            if (boardBgImage != null)
                g.DrawImage(boardBgImage, 0, 0, pnlBoard.Width, pnlBoard.Height);

            // 내부에 체스판 그려넣기
            if (boardImage != null)
            {
                CalculateBoardDimensions();
                g.DrawImage(boardImage, XOffset, YOffset, CellWidth * BoardManager.MAX_COL, CellHeight * BoardManager.MAX_ROW);
            }
            // 애니비아 벽(지형) 렌더링
            // =========================================================================================
            // 데이터가 없거나, 그리는 도중 데이터가 변경되어 예외가 발생하더라도 
            // 아래 기물 그리기 로직에 영향이 가지 않도록 try-catch로 단단히 감쌉니다.
            try
            {
                // Null 체크 (기본)
                if (gameManager.State != null && gameManager.State.ActiveWalls != null && gameManager.State.ActiveWalls.Count > 0)
                {
                    // 그리는 도중 데이터가 변경되면 Iteration Error가 발생하여 기물이 안 그려집니다.
                    // ToList()를 호출하여 복사본(스냅샷)을 만들어 안전하게 순회합니다.
                    var wallsSafeList = gameManager.State.ActiveWalls.ToList();

                    foreach (var wall in wallsSafeList)
                    {
                        // Key 검증 (혹시라도 데이터 포맷이 "R,C"가 아니면 스킵)
                        if (string.IsNullOrEmpty(wall.Key) || !wall.Key.Contains(',')) continue;

                        try
                        {
                            string[] coords = wall.Key.Split(',');
                            if (coords.Length < 2) continue;

                            // 안전하게 int로 변환 (int.Parse 대신 TryParse 사용)
                            if (!int.TryParse(coords[0], out int row)) continue;
                            if (!int.TryParse(coords[1], out int col)) continue;

                            // 타일의 실제 X, Y 좌표 계산
                            float x = XOffset + col * CellWidth;
                            float y = YOffset + row * CellHeight;

                            // 반투명한 얼음색(하늘색)으로 타일 채우기 (Alpha 120)
                            using (SolidBrush iceBrush = new SolidBrush(Color.FromArgb(120, 135, 206, 235)))
                            {
                                g.FillRectangle(iceBrush, x, y, CellWidth, CellHeight);
                            }

                            // 남은 턴 수를 텍스트로 표시 (가운데 정렬)
                            using (Font font = new Font("맑은 고딕", 12, FontStyle.Bold))
                            using (SolidBrush textBrush = new SolidBrush(Color.DarkBlue))
                            {
                                string text = $"{wall.Value}턴";

                                SizeF textSize = g.MeasureString(text, font);
                                float textX = x + (CellWidth - textSize.Width) / 2;
                                float textY = y + (CellHeight - textSize.Height) / 2;

                                g.DrawString(text, font, textBrush, textX, textY);
                            }
                        }
                        catch
                        {
                            // 한 타일 그리기 실패 시 해당 타일만 스킵하고 다음 타일로 넘어갑니다.
                            continue;
                        }
                    }
                }
            }
            catch
            {
                // 벽 그리기 전체 로직이 터지더라도 그냥 넘어갑니다. (기물은 그려야 하니까)
            }
            // =========================================================================================

            // 기물 렌더링
            foreach (var anime in pieceAnimations.Values)
            {
                anime.Onpainting(g);
            }
        }

        public void UpdateLoopTick()
        {
            UpdatePiecePixelPositions();

            foreach (var anime in pieceAnimations.Values)
            {
                anime.Animating(20f);
            }
        }

        private void UpdatePiecePixelPositions()
        {
            CalculateBoardDimensions();
            for (int row = 0; row < BoardManager.MAX_ROW; row++)
            {
                for (int col = 0; col < BoardManager.MAX_COL; col++)
                {
                    IPiece piece = gameManager.State.GetPieceAt(new Position(row, col));
                    if (piece != null && pieceAnimations.ContainsKey(piece))
                    {
                        PieceAnime anime = pieceAnimations[piece];
                        if (anime.State == PieceStatement.Idle1 || anime.State == PieceStatement.Idle2 || anime.State == PieceStatement.Attacking)
                        {
                            anime.X = XOffset + col * CellWidth + (CellWidth - anime.Size) / 2f;
                            anime.Y = YOffset + row * CellHeight + (CellHeight - anime.Size) / 2f;
                        }
                    }
                }
            }
        }
        public void SyncPiecesWithBackend()
        {
            CalculateBoardDimensions();
            var currentPieces = new List<IPiece>();
            for (int r = 0; r < BoardManager.MAX_ROW; r++)
            {
                for (int c = 0; c < BoardManager.MAX_COL; c++)
                {
                    var piece = gameManager.State.GetPieceAt(new Position(r, c));
                    if (piece != null) currentPieces.Add(piece);
                }
            }

            foreach (var kp in pieceAnimations.ToList())
            {
                if (!currentPieces.Contains(kp.Key) && kp.Value.State != PieceStatement.Dead)
                    pieceAnimations.Remove(kp.Key);
            }

            for (int row = 0; row < BoardManager.MAX_ROW; row++)
            {
                for (int col = 0; col < BoardManager.MAX_COL; col++)
                {
                    IPiece piece = gameManager.State.GetPieceAt(new Position(row, col));
                    if (piece != null && !pieceAnimations.ContainsKey(piece))
                    {
                        float startX = XOffset + col * CellWidth + (CellWidth - 70f) / 2f;
                        float startY = YOffset + row * CellHeight + (CellHeight - 70f) / 2f;

                        PieceAnime anime = new ConcretePieceAnime(piece.Owner.ToString(), piece.Type.ToString(), startX, startY);
                        pieceAnimations.Add(piece, anime);
                    }
                }
            }
        }
        public void HandleMovementAnimation()
        {
            SyncPiecesWithBackend();
            CalculateBoardDimensions();

            for (int row = 0; row < BoardManager.MAX_ROW; row++)
            {
                for (int col = 0; col < BoardManager.MAX_COL; col++)
                {
                    IPiece piece = gameManager.State.GetPieceAt(new Position(row, col));
                    if (piece != null && pieceAnimations.ContainsKey(piece))
                    {
                        PieceAnime anime = pieceAnimations[piece];
                        float targetX = XOffset + col * CellWidth + (CellWidth / 2f);
                        float targetY = YOffset + row * CellHeight + (CellHeight / 2f);

                        if (Math.Abs(anime.X - (targetX - anime.Size / 2f)) > 5 || Math.Abs(anime.Y - (targetY - anime.Size / 2f)) > 5)
                        {
                            if (anime.State != PieceStatement.Moving)
                                anime.Movingposit(targetX, targetY);
                        }
                    }
                }
            }
        }
        private class ConcretePieceAnime : PieceAnime
        {
            public ConcretePieceAnime(string owner, string piecetype, float startX, float startY)
                : base(owner, piecetype, startX, startY) { }
        }
    }
}