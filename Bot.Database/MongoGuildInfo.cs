using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Database
{
	// Class that exists solely to serialize/deserialize to the Mongo GuildInfo collection
	class MongoGuildInfo
	{
		public ObjectId _id { get; set; }
		public long guild { get; set; }
		public string? controlChannel { get; set; }
		public long controlChannelId { get; set; }
		public string? chatChannel { get; set; }
		public long chatChannelId { get; set; }
		public string? townSquare { get; set; }
		public long townSquareId { get; set; }
		public string? dayCategory { get; set; }
		public long dayCategoryId { get; set; }
		public string? nightCategory { get; set; }
		public long nightCategoryId { get; set; }
		public string? storyTellerRole { get; set; }
		public long storyTellerRoleId { get; set; }
		public string? villagerRole { get; set; }
		public long villagerRoleId { get; set; }
		public string? authorName { get; set; }
		public long author { get; set; }
		public DateTime timestamp { get; set; }
	}
}
