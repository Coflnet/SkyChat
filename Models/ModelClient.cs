
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Coflnet.Sky.Chat.Models
{
    /// <summary>
    /// Client software capeable of sending messages
    /// </summary>
    [DataContract]
    [Table("Clients")]
    public class ModelClient
    {
        /// <summary>
        /// Internal id for updating
        /// </summary>
        /// <value></value>
        [IgnoreDataMember]
        [JsonIgnore]
        public int Id { get; set; }
        /// <summary>
        /// Uuid of the target user
        /// </summary>
        /// <value></value>
        [DataMember(Name = "name")]
        [System.ComponentModel.DataAnnotations.MaxLength(32)]
        public string Name { get; set; }
        /// <summary>
        /// ApiKey 
        /// </summary>
        /// <value></value>
        [IgnoreDataMember]
        [JsonIgnore]
        public string ApiKey { get; set; }
        /// <summary>
        /// Per minute send quota
        /// </summary>
        /// <value></value>
        [DataMember(Name = "quota")]
        public int Quota { get; set; }
        /// <summary>
        /// User who is responsible for this client
        /// </summary>
        /// <value></value>
        [DataMember(Name = "contact")]
        public string Contact { get; set; }
        /// <summary>
        /// Webhook to post new messages too
        /// </summary>
        /// <value></value>
        [DataMember(Name = "webhook")]
        public string WebHook { get; set; }
        /// <summary>
        /// Auth header value for the webhook
        /// </summary>
        /// <value></value>
        [DataMember(Name = "webhookAuth")]
        public string WebhookAuth { get; set; }
        /// <summary>
        /// When this was created
        /// </summary>
        /// <value></value>
        [System.ComponentModel.DataAnnotations.Timestamp]
        [DataMember(Name = "created")]
        public DateTime Created { get; set; }
    }
}