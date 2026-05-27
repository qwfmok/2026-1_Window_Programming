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

// !!설명서는 주석 참조!!
// ㅇㅇㅇㅇ 수정하거나 발표, 보고서 등 자료에 쓰실 때 참고 ㅇㅇㅇㅇ

namespace CardChess.Models
{
    public abstract class PieceAnime
    {
        public string Owner { get; protected set; } // 플레이어 타입 받아오는 변수
        public string PieceType { get; protected set; } // 기물 타입 받아오는 변수
        public int Size { get; protected set; } = 70; // 기물 크기는 여기서 조절할 것

        public float X { get; set; }
        public float Y { get; set; }
        // 현재 좌표 수집 ---> 이동에 사용함

        protected float StartX, StartY; // 기물 이동의 시작 지점
        protected float TargetX, TargetY; // 기물 이동의 도착 지점이 됨
        protected float Moveprogress = 1.0f; // 이동 개시
        protected int Shakestep = 0; // 대충 이게 흔들림 효과 시작지점
        protected float Shakeoffset = 0; // 이건 최종적으로 흔들림 계산 끝난 값을 저장하는 곳
        protected float Intensity = 1.0f; // 기물 투명도
        protected float Idletime = 0; // 대기 자세 에셋 스왑용
        protected float Idleinterval = 770f; // 대기 자세 에셋 스왑 간격

        private static Image[] effectFramesCache = null;

        // --- 글로벌 이펙트(특히 추가턴주는 카드 관련) 변수부 ---
        protected bool isEffectPlaying = false;
        protected float effectProgress = 0f;
        protected float effectDuration = 250f;
        protected Size effectSize = new Size(96, 96);

        public PieceStatement State { get; protected set; } = PieceStatement.Idle1; // 여기서 스테이트먼트 시작 (보통 idle 1 상태)

        protected Image Frame1;
        protected Image Frame2; // 대기자세 2종
        protected Image Frameattack; // 공격자세 1종 (클릭 시)
        protected Image Framedeath; // 처치자세 1종 (잡힐 시)
        public IPiece AssociatedBackendPiece { get; set; } = null;
        protected static Image chainEffectImage;

        public Rectangle PieceBounds => new Rectangle((int)X, (int)(Y + Shakeoffset), Size, Size); // 쉽게 말하면 기물 히트박스

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
                string Assetpath = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "Assets"); // 현재 솔루션이 실행중인 경로 추적

                Frame1 = Image.FromFile(System.IO.Path.Combine(Assetpath, $"{Owner}_{PieceType}_num_1.png"));
                Frame2 = Image.FromFile(System.IO.Path.Combine(Assetpath, $"{Owner}_{PieceType}_num_2.png"));
                Frameattack = Image.FromFile(System.IO.Path.Combine(Assetpath, $"{Owner}_{PieceType}_num_3.png"));
                Framedeath = Image.FromFile(System.IO.Path.Combine(Assetpath, $"{Owner}_{PieceType}_num_4.png"));
                // 각 프레임에 맞는 이미지 출력하는 함수
                // 에셋을 넣을 때는 대기상태는 1,2 공격 자세를 3, 사망 연출을 4로 넣으면 ㅇㅋ
                // 오너 타입은 Player1 또는 Player2로 하고, 피스타입은 Bishop이나 Knight 등등 원하는 기물, 이후 _num_(..) 로 간단하게 필요한 에셋 넣으면 적용됨
                if (effectFramesCache == null)
                {
                    effectFramesCache = new Image[4];
                    for (int i = 0; i < 4; i++)
                    {
                        // 경로 추적해서 dust_ 이후 4개의 파일에 붙은 번호를 i를 늘려가면서 판단
                        string effectPath = System.IO.Path.Combine(Assetpath, $"effect_dust_{i + 1}.png");
                        if (System.IO.File.Exists(effectPath))
                        {
                            effectFramesCache[i] = Image.FromFile(effectPath);
                        }
                    }
                }
                if (chainEffectImage == null)
                {
                    string ChainAsset = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "Assets");
                    string chainPath = System.IO.Path.Combine(ChainAsset, "effect_zhonya.png"); // 존야 쓰면 나오는 쇠사슬 이펙트
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
                // 적절한 파일이 없으면 그냥 마젠타색상 사각형 생성하여 예외처리하는 용도
            }
        }

        /* ========== 이 밑으로는 기능 구현 ========== */

        // 클릭 시 상태 체크
        public void Onclick()
        {
            if (State == PieceStatement.Moving || State == PieceStatement.Shaking || State == PieceStatement.Dead)
                return;
            if (State == PieceStatement.Attacking)
                State = PieceStatement.Idle1;
            else
                State = PieceStatement.Attacking;
        }

        // 이동할 때 기물이 죽었는지 판단 후 시작지점 값에 현재 좌표 대입하고, 내가 클릭하는 지점을 Tx, Ty로 받아내서 이동하고자 하는 지점에 대입
        public void Movingposit(float Tx, float Ty)
        {
            if (State == PieceStatement.Dead)
                return;

            StartX = X;
            StartY = Y;
            TargetX = Tx - (Size / 2f);
            TargetY = Ty - (Size / 2f);

            Moveprogress = 0f;
            State = PieceStatement.Moving; // 이 때 기물 스테이트먼트는 Moving
        }

        // 애니메이션 구현
        public void Animating(float Timetodie)
        {
            if (isEffectPlaying)
            {
                effectProgress += (Timetodie / effectDuration); // 매 프레임 흐른 시간을 반영
                if (effectProgress >= 1.0f)
                {
                    effectProgress = 1.0f;
                    isEffectPlaying = false; // 4번 프레임까지 다 지나가면 재생 종료
                }
            }

            switch (State) // 각 상태는 switch case로 나눔
            {
                case PieceStatement.Idle1:
                case PieceStatement.Idle2:
                    Idletime += Timetodie;
                    if (Idletime >= Idleinterval)
                    {
                        Idletime = 0;
                        State = (State == PieceStatement.Idle1) ? PieceStatement.Idle2 : PieceStatement.Idle1;
                    }
                    break;
                // 대기 상태. 간단히 그냥 변수 증가로 idle1 이랑 2의 상태 교체하면서 idletime 0으로 만들어서 자체적으로 반복

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
                // 이동 상태
                // 이동 거리의 전체 진척도를 1로 보고, 0.06씩 증가시키면서 이동 처리 시작
                // 예외인 else로 먼저 들어가서 1에서 Moveprogress 증가시킨 값의 ^3을 빼면서 거리 폴오프
                // 이후 1.0 되면 if 들어가서 종료하고 Shaking 상태로 전환
                // 초기값이 1이 되는 이유는 이동 종료 상태를 나타내기 위해서임
                // 현빈 요청에 따라 이동 애니메이션 속도를 일부 증가

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
                // 흔들림 효과 상태
                // shakestep값 증가시켜서 10까지
                // 그 전에는 else로 들어가서 강도 16, 감쇠 값 10에서 시작하고 흔들림 값이 증가하면서 10으로 나뉘어서 점차 감쇠
                // 방향은 1f 이후 -1f로 상하상하 번갈아서 (대충 얘때문에 통통 튀는 느낌 난다는 뜻)
                // 이후 세 값을 강도 * 흔들림값 * 음수 양수로 좌우통제 해서 점차 약해지는 값을 만듬 (decay가 줄어드니까)

                case PieceStatement.Dead:
                    if (Intensity > 0f)
                    {
                        Intensity -= 0.06f;
                        if (Intensity < 0f)
                            Intensity = 0f;
                    }
                    break;
                    // 사망 상태
                    // 인텐시티 값을 6%씩 줄이면서 0이 될 때까지 수행
            }
        }

        // 그냥 그리기용 함수
        public void Onpainting(Graphics g)
        {
            Image Currentimage = GetImageload();

            if (Currentimage == null)
                return;

            if (State == PieceStatement.Dead) // 상태가 사망 상태가 되면 matrix에 인텐시티를 대입해서 히트박스에 출력하는 느낌
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

            if (AssociatedBackendPiece != null && AssociatedBackendPiece.IsFrozen && chainEffectImage != null) // 존야 상태인지 인식
            {
                // 체인 이미지 덮어써서 그려주는 추가 기능
                g.DrawImage(chainEffectImage, PieceBounds);
            }
        }

        // 이미지 매칭용 함수
        private Image GetImageload()
        {
            switch (State) // switch case로 구분해서 특정 Statement일때 각각의 이미지 반환
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

                default:
                    return Frame1; // 기본값은 그냥 대기자세
            }
        }
    }
}