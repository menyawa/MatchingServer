using System;

namespace MatchingServer {
    /// <summary>
    /// フレーム間の経過時間を司るタイマーのクラス
    /// 各接続に対し1つインスタンスを作成しなければならない(各接続ごとにメインループはあり、フレーム間の経過時間も違うため)ので、staticクラスにはできないことに注意
    /// </summary>
    class DeltaTimer {
        //前回のフレームの日時
        private DateTime prevFrameTime_ = DateTime.Now;

        /// <summary>
        /// 前フレームからの経過時間を取得する
        /// </summary>
        /// <returns></returns>
        public double get() {
            var interval = DateTime.Now - prevFrameTime_;
            return interval.TotalMilliseconds;
        }

        /// <summary>
        /// 正常に取得できるよう更新する
        /// この更新を毎フレーム行わないと、正常な経過時間が測れないので注意
        /// </summary>
        public void update() {
            prevFrameTime_ = DateTime.Now;
        }
    }
}
