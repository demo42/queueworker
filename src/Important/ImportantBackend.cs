using System.Net.Http;
using System.Threading.Tasks;

namespace Important
{
    public class ImportantBackend
    {
        private HttpClient _client;

        public ImportantBackend(HttpClient client)
        {
            _client = client;
        }

        public async Task SubmitData(string data)
        {
            await Task.Delay(1000);
        }
    }
}