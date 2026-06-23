using CardChess;
using CardChess.Menu;
using CardChess.Models;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        string outputDirectory = args.Length > 0 ? args[0] : AppDomain.CurrentDomain.BaseDirectory;
        Directory.CreateDirectory(outputDirectory);

        try
        {
            TestLobbyStatus(outputDirectory);
            TestGameLayout(outputDirectory);
            Console.WriteLine("PASS lobby-status=ok card-text=ok fullscreen=ok");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            CardChess.Menu.SoundsManager.StopBGM();
        }
    }

    private static void TestLobbyStatus(string outputDirectory)
    {
        using (Form1 lobby = new Form1())
        {
            lobby.ClientSize = new Size(1584, 861);
            lobby.Show();
            PumpEvents(300);

            Label status = GetField<Label>(lobby, "lblNetworkStatus");
            status.Text = "서버를 깨우는 중... 최대 1분 정도 걸릴 수 있습니다.";
            Size measured = TextRenderer.MeasureText(status.Text, status.Font);
            Require(measured.Width <= status.ClientSize.Width, "lobby status text fits");
            SaveScreenshot(lobby, Path.Combine(outputDirectory, "lobby-status.png"));
            lobby.Close();
        }
    }

    private static void TestGameLayout(string outputDirectory)
    {
        string progressPath = Path.Combine(outputDirectory, "game-layout-progress.txt");
        File.WriteAllText(progressPath, "constructing\n");
        Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
        using (MainForm game = new MainForm(null, PlayerType.Player1, 24680, true, screenBounds))
        {
            File.AppendAllText(progressPath, "constructed\n");
            GetField<System.Windows.Forms.Timer>(game, "gameLoopTimer").Stop();
            game.Show();
            File.AppendAllText(progressPath, "shown\n");
            PumpEvents(1200);
            File.AppendAllText(progressPath, "events-pumped\n");

            Require(game.FormBorderStyle == FormBorderStyle.None, "game inherits fullscreen border mode");
            Require(game.Bounds == screenBounds, "game inherits fullscreen bounds");

            Panel hand = GetField<Panel>(game, "pnlPlayerHand");
            Button[] cards = hand.Controls.OfType<Button>()
                .Where(button => button.Tag != null)
                .ToArray();
            Require(cards.Length > 0, "hand cards rendered");
            foreach (Button card in cards)
            {
                PropertyInfo displayTextProperty = card.GetType().GetProperty("DisplayText");
                string displayText = displayTextProperty?.GetValue(card) as string;
                Require(!string.IsNullOrEmpty(displayText), "card display text exists");
                Size measured = TextRenderer.MeasureText(
                    displayText,
                    card.Font,
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                Require(measured.Width <= card.ClientSize.Width - 8, "card text fits: " + displayText);
            }

            File.AppendAllText(progressPath, "controls-verified\n");
            SaveScreenshot(hand, Path.Combine(outputDirectory, "game-hand.png"));
            File.AppendAllText(progressPath, "screenshot-saved\n");

            using (SettingsMenu settings = new SettingsMenu(game, true, null))
            {
                settings.Show(game);
                PumpEvents(100);
                Button screenButton = settings.Controls.OfType<Button>()
                    .OrderBy(button => button.Top)
                    .First();
                screenButton.PerformClick();
                PumpEvents(100);
                settings.Close();
            }
            Require(game.FormBorderStyle == FormBorderStyle.Sizable, "fullscreen game returns to window mode");
            Require(game.Bounds != screenBounds, "window mode restores non-fullscreen bounds");
            game.Close();
        }
    }

    private static T GetField<T>(object instance, string name) where T : class
    {
        FieldInfo field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        T value = field?.GetValue(instance) as T;
        if (value == null)
            throw new InvalidOperationException("Missing field: " + name);
        return value;
    }

    private static void SaveScreenshot(Control control, string path)
    {
        using (Bitmap bitmap = new Bitmap(control.Width, control.Height))
        {
            control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(path);
        }
    }

    private static void PumpEvents(int milliseconds)
    {
        DateTime end = DateTime.UtcNow.AddMilliseconds(milliseconds);
        while (DateTime.UtcNow < end)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
    }

    private static void Require(bool condition, string operation)
    {
        if (!condition)
            throw new InvalidOperationException("FAILED: " + operation);
    }
}
