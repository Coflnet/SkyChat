namespace Coflnet.Sky.Chat.Models
{
    /// <summary>
    /// Contains the client and its api key (only place where the api key is visible)
    /// </summary>
    public class CientCreationResponse
    {
        public ModelClient Client { get; set; }
        public string ApiKey { get; set; }

        public CientCreationResponse(ModelClient client)
        {
            Client = client;
            ApiKey = client.ApiKey;
        }
    }
}