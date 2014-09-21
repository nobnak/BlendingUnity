using UnityEngine;
using Newtonsoft.Json;

namespace nobnak.Json {

	public class VectorJsonConverter : JsonConverter {
		#region implemented abstract members of JsonConverter
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			if (value is Vector2) {
				var v = (Vector2)value;
				writer.WriteStartArray();
				writer.WriteValue(v.x);
				writer.WriteValue(v.y);
				writer.WriteEndArray();
			} else if (value is Vector3) {
				var v = (Vector3)value;
				writer.WriteStartArray();
				writer.WriteValue(v.x);
				writer.WriteValue(v.y);
				writer.WriteValue(v.z);
				writer.WriteEndArray();
			} else if (value is Vector4) {
				var v = (Vector4)value;
				writer.WriteStartArray();
				writer.WriteValue(v.x);
				writer.WriteValue(v.y);
				writer.WriteValue(v.z);
				writer.WriteValue(v.w);
				writer.WriteEndArray();
			} else {
				throw new JsonSerializationException("Expected Vector");
			}
		}
		public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue, 
		                                 JsonSerializer serializer) {
			float v0, v1, v2, v3;

			reader.FloatParseHandling = FloatParseHandling.Double;

			if (reader.TokenType != JsonToken.StartArray)
				goto EXISTING_VALUE;

			if (!CanTokenBeFloat(reader))
				goto EXISTING_VALUE;
			v0 = System.Convert.ToSingle(reader.Value);

			if (!CanTokenBeFloat(reader))
				goto EXISTING_VALUE;
			v1 = System.Convert.ToSingle(reader.Value);

			if (objectType == typeof(Vector2))
				if (!reader.Read() || reader.TokenType != JsonToken.EndArray)
					goto EXISTING_VALUE;
				else
					return new Vector2(v0, v1);

			if (objectType == typeof(Vector3) || objectType == typeof(Vector4)) {
				if (!CanTokenBeFloat(reader))
					goto EXISTING_VALUE;
				v2 = System.Convert.ToSingle(reader.Value);
				if (objectType != typeof(Vector4))
					if (!reader.Read() || reader.TokenType != JsonToken.EndArray)
						goto EXISTING_VALUE;
					else
						return new Vector3(v0, v1, v2);
				
				if (!CanTokenBeFloat(reader))
					goto EXISTING_VALUE;
				v3 = System.Convert.ToSingle(reader.Value);
				if (!reader.Read() || reader.TokenType != JsonToken.EndArray)
					goto EXISTING_VALUE;
				else
					return new Vector4(v0, v1, v2, v3);
			}

		EXISTING_VALUE:
				return existingValue;
		}
		public override bool CanConvert(System.Type objectType) {
			return objectType == typeof(Vector2) || objectType == typeof(Vector3) || objectType == typeof(Vector4);
		}

		bool CanTokenBeFloat(JsonReader reader) {
			return reader.Read() && (reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Integer);
		}
		#endregion
	}
}
