using System;

namespace MatchingServer {
    /// <summary>
    /// フレーム間の経過時間を司るタイマーのクラス
    /// 複数インスタンスを作る意味がないので、静的クラスとして作成
    /// </summary>
    static class DeltaTimer {
        //前回のフレームの日時
        private static DateTime prevFrameTime_ = DateTime.Now;

        /// <summary>
        /// 前フレームからの経過時間を取得する
        /// </summary>
        /// <returns></returns>
        public static double get() {
            var interval = DateTime.Now - prevFrameTime_;
            return interval.TotalMilliseconds;
        }

        /// <summary>
        /// 正常に取得できるよう更新する
        /// この更新を毎フレーム行わないと、正常な経過時間が測れないので注意
        /// </summary>
        public static void update() {
            prevFrameTime_ = DateTime.Now;
        }
    }
}
