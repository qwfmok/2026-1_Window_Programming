using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CardChess.Menu
{
    public static class SoundsManager // 얘가 사운드의 기본값이 되므로 상수 클래스로 구현
    {
        private static Dictionary<string, SoundPlayer> soundCache = new Dictionary<string, SoundPlayer>(); // 잠깐 메모리에 올려두는 용도
        
        public static void LoadALLSounds()
        {
            string[] soundFiles = { "bg_music", "Piece_select", "Piece_attack" }; // 사운드 배열. 사운드 추가될 때마다 "[filename].wav", 로 추가하면 됨

            foreach (var soundName in soundFiles)
            {
                string path = Path.Combine(Application.StartupPath, "Sounds", $"{soundName}.wav"); // 기존 Assets 폴더가 아닌 Sounds 폴더로 컴바인을 다르게 찍어줘야 함

                if (File.Exists(path))
                {
                    try
                    {
                        SoundPlayer player = new SoundPlayer(path);
                        player.Load(); // 메모리에 미리 로드
                        soundCache[soundName] = player;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"사운드 로드 실패 ({soundName}): {ex.Message}");
                    }
                }
            }
        }

        public static void PlayBGM(string soundName)
        {
            if (soundCache.TryGetValue(soundName, out SoundPlayer player))
            {
                player.PlayLooping();
            }
        }

        // 현재는 아무 기능도 없지만 추후에 on off 기능을 구현한다면 이거 호출해다 쓰면 됨
        public static void StopBGM(string soundName)
        {
            if (soundCache.TryGetValue(soundName, out SoundPlayer player))
            {
                player.Stop();
            }
        }

        public static void Play(string soundName)
        {
            if (soundCache.TryGetValue(soundName, out SoundPlayer player))
            {
                player.Play();
            }
        }
    }
}
