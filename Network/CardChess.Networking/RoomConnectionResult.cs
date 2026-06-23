namespace CardChess.Networking
{
    public sealed class RoomConnectionResult
    {
        public bool Success { get; set; }
        public string RoomCode { get; set; }
        public string PlayerToken { get; set; }
        public int PlayerNumber { get; set; }
        public bool PeerConnected { get; set; }
        public string Error { get; set; }
    }
}
