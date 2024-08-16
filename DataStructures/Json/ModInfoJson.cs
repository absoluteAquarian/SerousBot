using Newtonsoft.Json;
using System;

namespace SerousBot.DataStructures.Json {
	[JsonConverter(typeof(ModInfoJsonConverter))]
	public class ModInfoJson {
		public string displayName;
		public string[] authors;
		public long steamID;
		public string[] dependencies;
		public string description;
	}

	internal class ModInfoJsonConverter : JsonConverter<ModInfoJson> {
		public override ModInfoJson ReadJson(JsonReader reader, Type objectType, ModInfoJson existingValue, bool hasExistingValue, JsonSerializer serializer) {
			ModInfoJson info = new();

			while (reader.Read()) {
				if (reader.TokenType != JsonToken.PropertyName)
					continue;

				string propertyName = (string)reader.Value;
				reader.Read();

				switch (propertyName) {
					case "name":
						info.displayName = (string)reader.Value;
						break;
					case "authors":
						info.authors = serializer.Deserialize<string[]>(reader);
						break;
					case "steamid":
						info.steamID = (long)reader.Value;
						break;
					case "dependencies":
						info.dependencies = serializer.Deserialize<string[]>(reader);
						break;
					case "description":
						info.description = string.Join("\n", serializer.Deserialize<string[]>(reader));
						break;
				}
			}

			return info;
		}

		public override void WriteJson(JsonWriter writer, ModInfoJson value, JsonSerializer serializer) {
			writer.WriteStartObject();

			writer.WritePropertyName("name");
			writer.WriteValue(value.displayName);

			writer.WritePropertyName("authors");
			writer.WriteStartArray();
			foreach (var author in value.authors)
				writer.WriteValue(author);
			writer.WriteEndArray();

			writer.WritePropertyName("steamid");
			writer.WriteValue(value.steamID);

			writer.WritePropertyName("dependencies");
			writer.WriteStartArray();
			foreach (var dependency in value.dependencies)
				writer.WriteValue(dependency);
			writer.WriteEndArray();

			writer.WritePropertyName("description");
			writer.WriteStartArray();
			foreach (var line in value.description.Split('\n'))
				writer.WriteValue(line);
			writer.WriteEndArray();

			writer.WriteEndObject();
		}
	}
}
