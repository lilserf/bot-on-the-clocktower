using Bot.Api;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Bot.Database
{
	// Implementation of Bot.Api.ITown that serializes to MongoDb
	class MongoTownRecord : ITownRecord
	{
		public ObjectId _id { get; set; }
		[BsonElement("guild")]
		public ulong GuildId { get; set; }
		[BsonElement("controlChannel")]
		public string? ControlChannel { get; set; }
		[BsonElement("controlChannelId")]
		public ulong ControlChannelId { get; set; }
		[BsonElement("chatChannel")]
		public string? ChatChannel { get; set; }
		[BsonElement("chatChannelId")]
		public ulong ChatChannelId { get; set; }
		[BsonElement("townSquare")]
		public string? TownSquare { get; set; }
		[BsonElement("townSquareId")]
		public ulong TownSquareId { get; set; }
		[BsonElement("dayCategory")]
		public string? DayCategory { get; set; }
		[BsonElement("dayCategoryId")]
		public ulong DayCategoryId { get; set; }
		[BsonElement("nightCategory")]
		public string? NightCategory { get; set; }
		[BsonElement("nightCategoryId")]
		public ulong NightCategoryId { get; set; }
		[BsonElement("storyTellerRole")]
		public string? StorytellerRole { get; set; }
		[BsonElement("storyTellerRoleId")]
		public ulong StorytellerRoleId { get; set; }
		[BsonElement("villagerRole")]
		public string? VillagerRole { get; set; }
		[BsonElement("villagerRoleId")]
		public ulong VillagerRoleId { get; set; }
		[BsonElement("authorName")]
		public string? AuthorName { get; set; }
		[BsonElement("author")]
		public ulong Author { get; set; }
		[BsonElement("timestamp")]
		public DateTime Timestamp { get; set; }
	}
}
