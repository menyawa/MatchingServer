using System;

namespace MatchingServer {
    /// <summary>
    /// オンラインプレイにおける、プレイヤー1人あたりのクラス
    /// </summary>
    class Player {
        //プレイヤーID
        public readonly string ID;
        //ニックネーム
        public readonly string NICK_NAME;

        //実際に操作されるクライアントか、CPUなのか
        public enum Type {
            Client, 
            CPU
        }
        //途中で切断されてCPUに切り替わる可能性があることに注意
        public Type type_;

        private Player(string id, string nickName, Type type) {
            ID = id;
            NICK_NAME = nickName;
            type_ = type;
        }

        /// <summary>
        /// 指定されたID、ニックネームのクライアントのプレイヤーを生成して返す
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nickName"></param>
        /// <returns></returns>
        public static Player createClient(string id, string nickName) {
            return new Player(id, nickName, Type.Client);
        }

        /// <summary>
        /// 指定された番号でCPUのプレイヤーのインスタンスを生成して返す
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static Player createCPU(int number) {
            return new Player(DateTime.Now.ToString(), $"CPU{number}", Type.CPU);
        }

        /// <summary>
        /// 有効な(真正な)idかどうか
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool isCorrect(string id) {
            //null、空文字、スペースのみでなければ有効とみなす
            return string.IsNullOrEmpty(id) == false && string.IsNullOrWhiteSpace(id) == false;
        }
    }
}
