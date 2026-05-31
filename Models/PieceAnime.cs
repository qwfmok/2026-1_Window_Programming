using CardChess.Pieces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

/// 기물 애니메이션 구현은 여기서

/// 기물 좌표에 사각형 바운드를 통해 히트박스를 설정
/// 좌표 받아내서 이미지 덧그리는 방식으로 애니메이션 구현

namespace CardChess.Models
{
    public abstract class PieceAnime
    {
        // 플레이어, 기물 타입과 크기 조절
        public string Owner { get; protected set; }
        public string PieceType { get; protected set; }
        public int Size { get; protected set; } = 70;

        public float X { get; set; }
        public float Y { get; set; }

        // --- 기물의 이동 좌표 관련 변수부 ---
        protected float StartX, StartY;
        protected float TargetX, TargetY;
        protected float Moveprogress = 1.0f;
        protected int Shakestep = 0;
        protected float Shakeoffset = 0;
        protected float Intensity = 1.0f;
        protected float Idletime = 0;
        protected float Idleinterval = 770f;

        private static Image[] effectFramesCache = null;

        // --- 글로벌 이펙트(특히 추가턴주는 카드 관련) 변수부 ---
        protected bool isEffectPlaying = false;
        protected float effectProgress = 0f;
        protected float effectDuration = 250f;
        protected Size effectSize = new Size(96, 96);

        public PieceStatement State { get; protected set; } = PieceStatement.Idle1;

        protected Image Frame1;
        protected Image Frame2;
        protected Image Frameattack;
        protected Image Framedeath;
        public IPiece AssociatedBackendPiece { get; set; } = null;
        protected static Image chainEffectImage;

        // 히트박스용 사각형 바운드
        public Rectangle PieceBounds => new Rectangle((int)X, (int)(Y + Shakeoffset), Size, Size);

        public PieceAnime(string owner, string Piecetype, float StartX, float StartY)
        {
            this.Owner = owner;
            this.PieceType = Piecetype;
            this.X = StartX;
            this.Y = StartY;

            Imageloading();
        }

        public void Imageloading()
        {
            try
            {
                string Assetpath = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "Assets");

                Frame1 = Image.FromFile(System.IO.Path.Combine(Assetpath, $"{Owner}_{PieceType}_num_1.png"));
                Frame2 = Image.FromFile(System.IO.Path.Combine(Assetpath, $"{Owner}_{PieceType}_num_2.png"));
                Frameattack = Image.FromFile(System.IO.Path.Combine(Assetpath, $"{Owner}_{PieceType}_num_3.png"));
                Framedeath = Image.FromFile(System.IO.Path.Combine(Assetpath, $"{Owner}_{PieceType}_num_4.png"));
                // 각 프레임에 맞는 이미지 출력하는 함수
                // 에셋을 넣을 때는 대기상태는 1,2 공격 자세를 3, 사망 연출을 4로 넣으면 작동됨
                // 오너 타입은 Player1 또는 Player2로 하고, 피스타입은 Bishop이나 Knight 등등 원하는 기물, 이후 _num_(..) 로 간단하게 필요한 에셋 넣으면 적용됨
                if (effectFramesCache == null)
                {
                    effectFramesCache = new Image[4];
                    for (int i = 0; i < 4; i++)
                    {
                        string effectPath = System.IO.Path.Combine(Assetpath, $"effect_dust_{i + 1}.png");
                        if (System.IO.File.Exists(effectPath))
                        {
                            effectFramesCache[i] = Image.FromFile(effectPath);
                        }
                    }
                }

                // 존야 쇠사슬 이펙트 구현부
                if (chainEffectImage == null)
                {
                    string ChainAsset = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "Assets");
                    string chainPath = System.IO.Path.Combine(ChainAsset, "effect_zhonya.png");
                    if (System.IO.File.Exists(chainPath))
                    {
                        chainEffectImage = Image.FromFile(chainPath);
                    }
                }
            }
            catch
            {
                Bitmap bmp = new Bitmap(Size, Size);
                using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.Magenta);
                Frame1 = Frame2 = Frameattack = Framedeath = bmp;
            }
        }

        /* ========== 이 밑으로는 기능 구현 ========== */

        // 기물 클릭 시 상태 체크
        public void Onclick()
        {
            if (State == PieceStatement.Moving || State == PieceStatement.Shaking || State == PieceStatement.Dead)
                return;
            if (State == PieceStatement.Attacking)
                State = PieceStatement.Idle1;
            else
                State = PieceStatement.Attacking;
        }

        // 이동할 때 기물이 처치되었는지 판단 후, 현재 클릭하는 지점을 체크하여 이동 지점에 대입
        public void Movingposit(float Tx, float Ty)
        {
            if (State == PieceStatement.Dead)
                return;

            StartX = X;
            StartY = Y;
            TargetX = Tx - (Size / 2f);
            TargetY = Ty - (Size / 2f);

            Moveprogress = 0f;
            State = PieceStatement.Moving;
        }

        // 애니메이션 구현부
        public void Animating(float Timetodie)
        {
            if (isEffectPlaying)
            {
                effectProgress += (Timetodie / effectDuration);
                if (effectProgress >= 1.0f)
                {
                    effectProgress = 1.0f;
                    isEffectPlaying = false;
                }
            }

            switch (State)
            {
                // Idle 1 2로 상태 교환하면서 대기 자세 반복 구현
                case PieceStatement.Idle1:
                case PieceStatement.Idle2:
                    Idletime += Timetodie;
                    if (Idletime >= Idleinterval)
                    {
                        Idletime = 0;
                        State = (State == PieceStatement.Idle1) ? PieceStatement.Idle2 : PieceStatement.Idle1;
                    }
                    break;

                // 이동 거리를 증가시키면서 이동 처리 시작하고, 증가시킨 값을 점차 키워가며 거리 감쇠 구현
                case PieceStatement.Moving:
                    Moveprogress += 0.07f;
                    if (Moveprogress >= 1.0f)
                    {
                        Moveprogress = 1.0f;
                        X = TargetX;
                        Y = TargetY;
                        Shakestep = 0;
                        State = PieceStatement.Shaking;
                        isEffectPlaying = true;
                        effectProgress = 0f;
                    }
                    else
                    {
                        float falloff = 1f - (float)Math.Pow(1f - Moveprogress, 3);
                        X = StartX + (TargetX - StartX) * falloff;
                        Y = StartY + (TargetY - StartY) * falloff;

                        float Jumpheight = 60f;
                        float Jumpoffset = 4 * Jumpheight * Moveprogress * (1f - Moveprogress);
                        Y -= Jumpoffset;
                    }
                    break;

                // 이동 완료 시 흔들림 값이 증가하면서 점차 줄어드는 식으로 구현해 통통 튀는 느낌을 구현
                case PieceStatement.Shaking:
                    Shakestep++;
                    if (Shakestep >= 11)
                    {
                        Shakeoffset = 0;
                        State = PieceStatement.Idle1;
                    }
                    else
                    {
                        float Shakestrength = 16f;
                        float Decay = (10f - Shakestep) / 10f;
                        float Directing = (Shakestep % 2 == 0) ? 1f : -1f;
                        Shakeoffset = Shakestrength * Decay * Directing;
                    }
                    break;

                case PieceStatement.Dead:
                    if (Intensity > 0f)
                    {
                        Intensity -= 0.06f;
                        if (Intensity < 0f)
                            Intensity = 0f;
                    }
                    break;
            }
        }

        // 실제 그래픽 그리기
        public void Onpainting(Graphics g)
        {
            Image Currentimage = GetImageload();

            if (Currentimage == null)
                return;

            // 상태가 사망 상태가 되면 인텐시티 값만큼 히트박스에 출력
            if (State == PieceStatement.Dead)
            {
                using (var Imageelement = new System.Drawing.Imaging.ImageAttributes())
                {
                    var colorMatrix = new System.Drawing.Imaging.ColorMatrix { Matrix33 = Intensity };
                    Imageelement.SetColorMatrix(colorMatrix);

                    g.DrawImage
                        (
                        Currentimage,
                        PieceBounds,
                        0, 0, Currentimage.Width, Currentimage.Height,
                        GraphicsUnit.Pixel,
                        Imageelement
                        );
                }
            }
            else
            {
                g.DrawImage(Currentimage, PieceBounds);
            }

            if (isEffectPlaying && effectFramesCache != null)
            {
                int frameIndex = (int)(effectProgress * 4);
                if (frameIndex > 3) frameIndex = 3;

                if (effectFramesCache[frameIndex] != null)
                {
                    // 기물 발밑 정중앙에 이펙트가 걸치도록 좌표 계산
                    float effectX = X + (Size / 2f) - (effectSize.Width / 2f);
                    float effectY = Y + Size - (effectSize.Height / 2f);

                    g.DrawImage(effectFramesCache[frameIndex], effectX, effectY, effectSize.Width, effectSize.Height);
                }
            }

            // 존야 상태인지 검사하고 체인 이펙트를 히트박스에 출력
            if (AssociatedBackendPiece != null && AssociatedBackendPiece.IsFrozen && chainEffectImage != null)
            {
                g.DrawImage(chainEffectImage, PieceBounds);
            }
        }

        // 이미지 매칭용 함수
        private Image GetImageload()
        {
            switch (State)
            {
                case PieceStatement.Idle1:
                    return Frame1;

                case PieceStatement.Idle2:
                    return Frame2;

                case PieceStatement.Attacking:
                case PieceStatement.Moving:
                case PieceStatement.Shaking:
                    return Frameattack;

                case PieceStatement.Dead:
                    return Framedeath;

                // 기본값은 대기자세
                default:
                    return Frame1;
            }
        }
    }
}