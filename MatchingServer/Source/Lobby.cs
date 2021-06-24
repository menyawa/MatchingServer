using System;
using System.Collections.Generic;

namespace MatchingServer {
    /// <summary>
    /// 対戦ロビーのクラス
    /// </summary>
    sealed class Lobby : ElementsBase {
        private readonly List<Room> ROOMS = new List<Room>();

        public Lobby() { }

        /// <summary>
        /// プレイヤーをロビーのいずれかのルームに加入させ、入れたかどうかを返す
        /// </summary>
        /// <returns></returns>
        public bool joinPlayer(string id, string nickName) {
            foreach (var room in ROOMS) {
                if (room.canJoin()) {
                    room.join(id, nickName);
                    return true;
                }
            }

            return false;
        }
    }
}
