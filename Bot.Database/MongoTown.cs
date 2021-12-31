using Bot.Api;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Bot.Database
{
	// Implementation of Bot.Api.ITown that serializes to MongoDb
	class MongoTown : ITown
	{
		public ObjectId _id { get; set; }
		[BsonElement("guild")]
		public long GuildId { get; set; }
		[BsonElement("controlChannel")]
		public string? ControlChannel { get; set; }
		[BsonElement("controlChannelId")]
		public long ControlChannelId { get; set; }
		[BsonElement("chatChannel")]
		public string? ChatChannel { get; set; }
		[BsonElement("chatChannelId")]
		public long ChatChannelId { get; set; }
		[BsonElement("townSquare")]
		public string? TownSquare { get; set; }
		[BsonElement("townSquareId")]
		public long TownSquareId { get; set; }
		[BsonElement("dayCategory")]
		public string? DayCategory { get; set; }
		[BsonElement("dayCategoryId")]
		public long DayCategoryId { get; set; }
		[BsonElement("nightCategory")]
		public string? NightCategory { get; set; }
		[BsonElement("nightCategoryId")]
		public long NightCategoryId { get; set; }
		[BsonElement("storyTellerRole")]
		public string? StoryTellerRole { get; set; }
		[BsonElement("storyTellerRoleId")]
		public long StoryTellerRoleId { get; set; }
		[BsonElement("villagerRole")]
		public string? VillagerRole { get; set; }
		[BsonElement("villagerRoleId")]
		public long VillagerRoleId { get; set; }
		[BsonElement("authorName")]
		public string? AuthorName { get; set; }
		[BsonElement("author")]
		public long Author { get; set; }
		[BsonElement("timestamp")]
		public DateTime Timestamp { get; set; }
	}
}
