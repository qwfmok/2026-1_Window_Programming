using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using NAudio.Wave; // NAudio 네임스페이스 추가


/// 사운드 관리 구현은 여기서

/// 음량 조절 구현을 위해 NAudio 라이브러리 사용

namespace CardChess.Menu
{
    public static class SoundsManager
    {
        // MP3를 딕셔너리로 메모리에 올려서 관리
        private static Dictionary<string, string> soundPaths = new Dictionary<string, string>();

        // BGM 전용 플레이어 및 리더 변수
        private static WaveOutEvent bgmOutput;
        private static AudioFileReader bgmReader;
        private static bool isBgmLooping = false;

        private static float masterVolume = 0.5f;

        // BGM이 재생 중일 때에도 환경 설정에 올려서 마스터 볼륨 반영
        public static float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = Math.Max(0.0f, Math.Min(1.0f, value));
                if (bgmReader != null)
                {
                    bgmReader.Volume = masterVolume;
                }
            }
        }

        public static void LoadALLSounds()
        {
            // NAudio 라이브러리를 채용하므로 확장자를 기존 Wav 파일에서 Mp3로 변경
            string[] soundFiles = { "bg_music", "Piece_select", "Piece_attack", "Menu_icon_select", "Card_wall", "Card_timewalk", "Card_effect" };

            foreach (var soundName in soundFiles)
            {
                string path = Path.Combine(Application.StartupPath, "Sounds", $"{soundName}.mp3");

                if (File.Exists(path))
                {
                    soundPaths[soundName] = path;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"사운드 파일 없음: {path}");
                }
            }
        }

        public static void PlayBGM(string soundName)
        {
            if (!soundPaths.TryGetValue(soundName, out string path)) return;

            // 중복 재생 방지
            try
            {
                StopBGM();

                bgmOutput = new WaveOutEvent();
                bgmReader = new AudioFileReader(path);

                bgmOutput.Init(bgmReader);
                bgmOutput.Play();
                isBgmLooping = true;

                bgmOutput.PlaybackStopped += (sender, args) =>
                {
                    if (isBgmLooping && bgmReader != null && bgmOutput != null)
                    {
                        bgmReader.Position = 0;
                        bgmOutput.Play();
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BGM 재생 실패: {ex.Message}");
            }
        }

        // BGM 끄기 버튼 이벤트 핸들러로 정지 후 메모리 해제
        public static void StopBGM()
        {
            isBgmLooping = false;

            if (bgmOutput != null)
            {
                bgmOutput.Stop();
                bgmOutput.Dispose();
                bgmOutput = null;
            }

            if (bgmReader != null)
            {
                bgmReader.Dispose();
                bgmReader = null;
            }
        }

        public static void Play(string soundName)
        {
            if (!soundPaths.TryGetValue(soundName, out string path)) return;

            try
            {
                // 효과음은 독립 재생하여 서로의 음향에 간섭하지 않도록 조정
                var sfxOutput = new WaveOutEvent();
                var sfxReader = new AudioFileReader(path);

                sfxOutput.Init(sfxReader);
                sfxOutput.Play();

                // 효과음 재생 종료 시 메모리 해제
                sfxOutput.PlaybackStopped += (sender, args) =>
                {
                    sfxOutput.Dispose();
                    sfxReader.Dispose();
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"효과음 재생 실패 ({soundName}): {ex.Message}");
            }
        }
    }
}