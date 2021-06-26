using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;

namespace MatchingServer {
    /// <summary>
    /// 対戦ロビーのクラス
    /// </summary>
    sealed class Lobby : ElementsBase {
        private readonly List<Room> ROOMS = new List<Room>();

        public Lobby() { }

        /// <summary>
        /// プレイヤーをロビーのいずれかのルームに加入させ、入ったルームのインデックスを返す
        /// </summary>
        /// <returns></returns>
        public int joinPlayer(string id, string nickName, WebSocket webSocket, int maxPlayerCount) {
            //希望した対戦人数で空いているルームを見つけ次第入れる
            for (int index = 0; index < ROOMS.Count(); index++) {
                var room = ROOMS[index];
                if(room.canJoin() && room.MAX_PLAYER_COUNT == maxPlayerCount) {
                    var player = room.join(id, nickName, webSocket);
                    //ルームに自身が入ったことを他プレイヤーに伝える
                    //こうすることでルームに自身が入った場合・他プレイヤーがルームに入った場合共に対応可能
                    player.sendMyDataToOthers(room.getOtherPlayers(player), maxPlayerCount, MessageData.Type.Join);
                    return index;
                }
            }

            //どこも空いていなかったら新たにルームを作成して入り、待機する
            ROOMS.Add(new Room(id, nickName, webSocket, maxPlayerCount));
            //返すのはインデックスなので-1することに注意
            return ROOMS.Count() - 1;
        }

        /// <summary>
        /// 指定したルームの指定プレイヤーを退出させ、プレイヤーのインスタンスを返す
        /// </summary>
        /// <param name="id"></param>
        /// <param name="roomIndex"></param>
        public Player leavePlayer(string id, int roomIndex) {
            return ROOMS[roomIndex].leave(id);
        }
    }
}
