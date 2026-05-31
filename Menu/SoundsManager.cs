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
        // MP3
        private static Dictionary<string, string> soundPaths = new Dictionary<string, string>();

        // BGM 전용 플레이어 및 리더 변수
        private static WaveOutEvent bgmOutput;
        private static AudioFileReader bgmReader;
        private static bool isBgmLooping = false;

        private static float masterVolume = 0.5f;

        public static float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = Math.Max(0.0f, Math.Min(1.0f, value));
                // BGM이 재생 중이라면 즉시 볼륨 변경 반영
                if (bgmReader != null)
                {
                    bgmReader.Volume = masterVolume;
                }
            }
        }

        public static void LoadALLSounds()
        {
            // 확장자를 .mp3로 변경
            string[] soundFiles = { "bg_music", "Piece_select", "Piece_attack", "Menu_icon_select", "Card_wall", "Card_timewalk", "Card_effect" };

            foreach (var soundName in soundFiles)
            {
                string path = Path.Combine(Application.StartupPath, "Sounds", $"{soundName}.mp3");

                if (File.Exists(path))
                {
                    soundPaths[soundName] = path; // 경로만 보관 (재생할 때 읽음)
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

            try
            {
                // 이미 재생 중인 BGM이 있다면 정지 및 메모리 해제
                StopBGM();

                bgmOutput = new WaveOutEvent();
                bgmReader = new AudioFileReader(path);

                bgmOutput.Init(bgmReader);
                bgmOutput.Play();
                isBgmLooping = true;

                // 🌟 BGM 무한 루프 구현: 재생이 끝나면 다시 처음(0초)으로 돌려서 플레이
                bgmOutput.PlaybackStopped += (sender, args) =>
                {
                    if (isBgmLooping && bgmReader != null && bgmOutput != null)
                    {
                        bgmReader.Position = 0; // 오디오 스트림을 처음으로 되감기
                        bgmOutput.Play();
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BGM 재생 실패: {ex.Message}");
            }
        }

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

        // 🌟 효과음 동시 재생(Fire and Forget) 함수
        public static void Play(string soundName)
        {
            if (!soundPaths.TryGetValue(soundName, out string path)) return;

            try
            {
                // 효과음은 불릴 때마다 독립된 장치(채널)를 생성하므로 서로의 소리를 끊지 않습니다.
                var sfxOutput = new WaveOutEvent();
                var sfxReader = new AudioFileReader(path);

                sfxOutput.Init(sfxReader);
                sfxOutput.Play();

                // 효과음 재생이 끝나면 자동으로 리소스를 닫고 메모리에서 해제(Dispose)
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