using System;
using System.Threading.Tasks;

namespace MatchingServer {
    class Program {
        private static async Task Main(string[] args) {
            await Server.Run();
        }
    }
}
