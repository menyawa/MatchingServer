using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace MatchingServer {
    /// <summary>
    /// 対戦ロビーのクラス
    /// </summary>
    sealed class Lobby : ElementsBase {
        private readonly List<Room> ROOMS = new List<Room>();

        /// <summary>
        /// プレイヤーをロビーのいずれかのルームに加入させ、入ったルームのインデックスを返す
        /// </summary>
        /// <returns></returns>
        public async Task<int> joinPlayerAsync(string id, string nickName, WebSocket webSocket, int maxPlayerCount) {
            int index = 0;
            Player myPlayer = null;
            Room room = null;
            //入室処理を複数のスレッドでほぼ同時に行うと新しく作成された空きルームを認識できなかったりするので、lockで排他制御する
            lock (ROOMS) {
                //希望した対戦人数で空いているルームを見つけ次第入れる
                bool allRoomIsClosed = true;
                for (; index < ROOMS.Count(); index++) {
                    room = ROOMS[index];
                    if (room.canJoin() == false) continue;

                    allRoomIsClosed = false;
                    try {
                        myPlayer = room.join(id, nickName, webSocket);
                        if (myPlayer == null) {
                            Debug.WriteLine("エラー：入室に失敗しました");
                            return Server.INVAID_ID;
                        }
                        break;
                    } catch (ArgumentException) {
                        Debug.WriteLine("エラー：入室に失敗しました(プレイヤーIDがおかしいようです)");
                        Debug.WriteLine("無効なID値を返します\n");
                        return Server.INVAID_ID;
                    }
                }

                //どこも空いていなかったら新たにルームを作成して入り、待機する
                if (allRoomIsClosed) {
                    ROOMS.Add(new Room(id, nickName, webSocket, maxPlayerCount));
                    //返すのはインデックスなので-1することに注意
                    index = ROOMS.Count() - 1;
                    return index;
                }
            }

            //既にあるルームに入った場合、自身が入ったことを他プレイヤーに通知する
            //lockステートメント内でawaitの待機はできないため、このように外でしなければいけないことに注意
            //またどの引数もnullにはなりえないため、ArgumentNullExceptionは発生しない(try-catchはいらない)
            var otherPlayersInRoom = room.getOtherPlayers(myPlayer);
            await myPlayer.sendMyDataToOthersAsync(otherPlayersInRoom, maxPlayerCount, MessageData.Type.Join);
            foreach (var otherPlayer in otherPlayersInRoom) {
                //同時に、既にルームに入っている他プレイヤーの情報を自身に通知する
                Player[] temp = { myPlayer };
                await otherPlayer.sendMyDataToOthersAsync(temp, maxPlayerCount, MessageData.Type.Join);
            }
            return index;

        }

        /// <summary>
        /// 指定したルームの指定プレイヤーを退出させ、プレイヤーのインスタンスを返す
        /// </summary>
        /// <param name="id"></param>
        /// <param name="roomIndex"></param>
        public async Task<Player> leavePlayerAsync(string id, int roomIndex) {
            var room = ROOMS[roomIndex];
            //入室の際と同様に、退出の際にも他プレイヤーに通知する
            try {
                var player = room.leave(id);
                await player.sendMyDataToOthersAsync(room.getOtherPlayers(player), room.MAX_PLAYER_COUNT, MessageData.Type.Leave);
                //CPUしかいなくなり次第ルームを消去する
                if (room.allPlayerIsCPU()) ROOMS.RemoveAt(roomIndex);
                return player;
                //ArgumentExceptionはArgumentNullExceptionのBaseクラスなので、catch句は1つでよい
            } catch (ArgumentException) {
                Debug.WriteLine("エラー：引数関係でエラーが発生したため、退出できませんでした");
                Debug.WriteLine("nullを返します\n");
                return null;
            }
        }

        public override string ToString() {
            string str = $"現在のルーム数： {ROOMS.Count()}\n";
            for (int index = 0; index < ROOMS.Count(); index++) {
                //ルーム番号は1から始まることに注意
                //またルーム間は2行開ける(プレイヤーの情報間の空行はは1行なので、それとの区別の意も込めて)
                str += $"ルーム{index + 1}\n";
                str += ROOMS[index].ToString() + "\n\n";
            }
            return str;
        }
    }
}
