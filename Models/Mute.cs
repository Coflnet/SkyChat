
using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Coflnet.Sky.Chat.Models
{
    [DataContract]
    public class Mute
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
        [DataMember(Name = "target")]
        [System.ComponentModel.DataAnnotations.StringLength(32)]
        public string Uuid { get; set; }
        /// <summary>
        /// Uuid of user performing the mute
        /// </summary>
        /// <value></value>
        [DataMember(Name = "muter")]
        [System.ComponentModel.DataAnnotations.StringLength(32)]
        public string Muter { get; set; }
        /// <summary>
        /// Uuid of user performing the mute
        /// </summary>
        /// <value></value>
        [DataMember(Name = "unMuter")]
        [System.ComponentModel.DataAnnotations.StringLength(32)]
        public string UnMuter { get; set; }
        /// <summary>
        /// Message for the user
        /// </summary>
        /// <value></value>
        [DataMember(Name = "message")]
        public string Message { get; set; }
        /// <summary>
        /// Internal reason
        /// </summary>
        /// <value></value>
        [DataMember(Name = "reason")]
        public string Reason { get; set; }
        /// <summary>
        /// Normalized rule used for repeat-sanction calculation after quoted
        /// message content has been removed.
        /// </summary>
        [DataMember(Name = "rule")]
        public MuteRule Rule { get; set; }
        /// <summary>
        /// What client software added the mute
        /// </summary>
        /// <value></value>
        [DataMember(Name = "clientId")]
        public int ClientId { get; set; }
        /// <summary>
        /// What client software added the mute
        /// </summary>
        /// <value></value>
        [DataMember(Name = "umClientId")]
        public int UnMuteClientId { get; set; }
        /// <summary>
        /// When this was created
        /// </summary>
        /// <value></value>
        [System.ComponentModel.DataAnnotations.Timestamp]
        [DataMember(Name = "timestamp")]
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// Until when this is active
        /// </summary>
        /// <value></value>
        [DataMember(Name = "expires")]
        public DateTime Expires { get; set; }
        /// <summary>
        /// The state of the mute
        /// </summary>
        /// <value></value>
        public MuteStatus Status {get;set;}
    }

    public enum MuteStatus 
    {
        NONE,
        SLOW_MODE,
        VERY_SLOW_MODE,
        FILTER = 4,
        MUTE = 8,
        CANCELED = 16
    }

    public enum MuteRule
    {
        NONE,
        BE_NICE,
        NO_UNSOLICITED_ADVERTISING
    }

    public class UnMute
    {
        /// <summary>
        /// Uuid of the target user
        /// </summary>
        /// <value></value>
        [DataMember(Name = "target")]
        [System.ComponentModel.DataAnnotations.StringLength(32)]
        public string Uuid { get; set; }
        /// <summary>
        /// Uuid of user performing the mute
        /// </summary>
        /// <value></value>
        [DataMember(Name = "unMuter")]
        [System.ComponentModel.DataAnnotations.StringLength(32)]
        public string UnMuter { get; set; }
        /// <summary>
        /// Internal reason
        /// </summary>
        /// <value></value>
        [DataMember(Name = "reason")]
        public string Reason { get; set; }
    }
}
