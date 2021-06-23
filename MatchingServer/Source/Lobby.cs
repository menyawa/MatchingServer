using System;
using System.Collections.Generic;

namespace MatchingServer {
    /// <summary>
    /// 対戦ロビーのクラス
    /// </summary>
    sealed class Lobby : ElementsBase {
        private readonly List<Room> ROOMS = new List<Room>();

        public Lobby() { }
    }
}
