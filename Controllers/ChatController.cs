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
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

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
        private readonly IMuteList muteList;
        private readonly IConfiguration configuration;

        /// <summary>
        /// Creates a new instance of <see cref="ChatController"/>
        /// </summary>
        /// <param name="service"></param>
        /// <param name="muteServices"></param>
        /// <param name="muteList"></param>
        /// <param name="configuration"></param>
        public ChatController(
            ChatService service,
            IEnumerable<IMuteService> muteServices,
            IMuteList muteList,
            IConfiguration configuration)
        {
            this.service = service;
            this.muteServices = muteServices;
            this.muteList = muteList;
            this.configuration = configuration;
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
        /// <param name="adminToken"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("internal/client")]
        public async Task<ActionResult<CientCreationResponse>> CreateClient(
            [FromBody] ModelClient client,
            [FromHeader(Name = "X-Chat-Admin-Token")] string adminToken)
        {
            if (!HasValidDeletionToken(configuration["CLIENT_ADMIN_TOKEN"], adminToken))
                return NotFound();
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
        [Authorize("ApiToken")]
        public async Task<Mute> MuteUser([FromBody] Mute mute, [FromHeader] string authorization)
        {
            AssertAuthHeader(authorization);
            if (mute != null)
                mute.Rule = MuteService.GetRule(mute);
            await Parallel.ForEachAsync(muteServices, async (s,c) =>
            {
                await s.MuteUser(mute, authorization);
            });
            return mute;
        }
        /// <summary>
        /// Get all active mutes 
        /// </summary>
        /// <param name="authorization"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("mutes")]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IEnumerable<Mute>> GetMutes([FromHeader] string authorization)
        {
            AssertAuthHeader(authorization);
            return await muteList.GetMutes(authorization);
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

        /// <summary>
        /// Deletes stored public-chat messages for a verified Minecraft account.
        /// This internal endpoint must be called only after the account-facing
        /// service has authenticated the requester and resolved the linked UUID.
        /// </summary>
        [HttpDelete]
        [Route("messages/user/{uuid}")]
        [ProducesResponseType(typeof(UserMessageDeletionResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<UserMessageDeletionResult>> DeleteUserMessages(
            string uuid,
            [FromHeader(Name = "X-Chat-Deletion-Token")] string deletionToken)
        {
            if (!HasValidDeletionToken(configuration["MESSAGE_DELETION_TOKEN"], deletionToken))
                return NotFound();
            if (!Guid.TryParseExact(uuid, "N", out var parsedUuid))
                return BadRequest("uuid must be a canonical 32-character Minecraft UUID");

            var canonicalUuid = parsedUuid.ToString("N");
            var (messagesDeleted, reportsDeleted) =
                await service.DeleteUserMessages(canonicalUuid);
            return Ok(new UserMessageDeletionResult(
                canonicalUuid,
                messagesDeleted,
                reportsDeleted));
        }

        internal static bool HasValidDeletionToken(string expected, string supplied)
        {
            var expectedBytes = Encoding.UTF8.GetBytes(expected ?? "");
            var suppliedBytes = Encoding.UTF8.GetBytes(supplied ?? "");
            return expectedBytes.Length >= 32
                && expectedBytes.Length == suppliedBytes.Length
                && CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
        }
    }

    public record UserMessageDeletionResult(
        string Uuid,
        int MessagesDeleted,
        int ReportsDeleted);
}
