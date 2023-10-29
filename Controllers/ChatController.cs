using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Coflnet.Sky.Chat.Models;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Collections.Generic;
using Coflnet.Sky.Chat.Services;
using Coflnet.Sky.Core;

namespace Coflnet.Sky.Chat.Controllers
{
    /// <summary>
    /// Main Controller handling tracking
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ProducesResponseType(typeof(ErrorResponse), 500), ProducesResponseType(200)]
    public class ChatController : ControllerBase
    {
        private readonly ChatService service;
        private readonly IEnumerable<IMuteService> muteServices;

        /// <summary>
        /// Creates a new instance of <see cref="ChatController"/>
        /// </summary>
        /// <param name="service"></param>
        /// <param name="muteServices"></param>
        public ChatController(ChatService service, IEnumerable<IMuteService> muteServices)
        {
            this.service = service;
            this.muteServices = muteServices;
        }

        /// <summary>
        /// Sends a message
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="authorization"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("send")]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<ChatMessage> SendMessage([FromBody] ChatMessage msg, [FromHeader] string authorization)
        {
            AssertAuthHeader(authorization);
            await service.SendMessage(msg, authorization);
            return msg;
        }

        private static void AssertAuthHeader(string authorization)
        {
            if (string.IsNullOrEmpty(authorization))
                throw new ApiException("missing_authorization", "The required authorization header wasn't passed. Set it to the token you api received.");
        }

        /// <summary>
        /// Create a nw Client
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("internal/client")]
        public async Task<CientCreationResponse> CreateClient([FromBody] ModelClient client)
        {
            return new CientCreationResponse(await service.CreateClient(client));
        }

        /// <summary>
        /// Create a new mute for an user
        /// </summary>
        /// <param name="mute">Data about the mute</param>
        /// <param name="authorization"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("mute")]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<Mute> MuteUser([FromBody] Mute mute, [FromHeader] string authorization)
        {
            AssertAuthHeader(authorization);
            await Parallel.ForEachAsync(muteServices, async (s,c) =>
            {
                await s.MuteUser(mute, authorization);
            });
            return mute;
        }
        /// <summary>
        /// Create a new mute for an user
        /// </summary>
        /// <param name="mute">Data about the mute</param>
        /// <param name="authorization"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("mute")]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<UnMute> UnMuteUser([FromBody] UnMute mute, [FromHeader] string authorization)
        {
            AssertAuthHeader(authorization);
            await Task.WhenAll(muteServices.Select(s => s.UnMuteUser(mute, authorization)));
            return mute;
        }
    }
}
