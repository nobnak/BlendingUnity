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
			} else if (value is Color) {
				var v = (Color)value;
				writer.WriteStartArray();
				writer.WriteValue(v.r);
				writer.WriteValue(v.g);
				writer.WriteValue(v.b);
				writer.WriteValue(v.a);
				writer.WriteEndArray();
			} else if (value is Rect) {
				var v = (Rect)value;
				writer.WriteStartArray();
				writer.WriteValue(v.x);
				writer.WriteValue(v.y);
				writer.WriteValue(v.width);
				writer.WriteValue(v.height);
				writer.WriteEndArray();
			} else {
				throw new JsonSerializationException("Expected Vector");
			}
		}
		public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue, 
		                                 JsonSerializer serializer) {
			reader.FloatParseHandling = FloatParseHandling.Double;

			if (reader.TokenType != JsonToken.StartArray)
				goto EXISTING_VALUE;

			Vector4 v = Vector4.zero;
			int length = 0;
			while (length < 5) {
				if (!reader.Read())
					goto EXISTING_VALUE;
				if (reader.TokenType == JsonToken.EndArray)
					break;
				if (!IsFloat(reader.TokenType))
					goto EXISTING_VALUE;
				v[length++] = System.Convert.ToSingle(reader.Value);
			}

			if (length == 2) {
				if (objectType == typeof(Vector2))
					return (Vector2)v;
				goto EXISTING_VALUE;
			} else if (length == 3) {
				if (objectType == typeof(Vector3))
					return (Vector3)v;
				goto EXISTING_VALUE;
			} else if (length == 4) {
				if (objectType == typeof(Vector4))
					return v;
				if (objectType == typeof(Color))
					return (Color)v;
				if (objectType == typeof(Rect))
					return new Rect(v.x, v.y, v.z, v.w);
				goto EXISTING_VALUE;
			}
			
		EXISTING_VALUE:
				return existingValue;
		}
		public override bool CanConvert(System.Type objectType) {
			return objectType == typeof(Vector2) || objectType == typeof(Vector3) || objectType == typeof(Vector4) || objectType == typeof(Color);
		}

		bool CanTokenBeFloat(JsonReader reader) {
			return reader.Read() && IsFloat(reader.TokenType);
		}
		bool IsFloat(JsonToken tokenType) {
			return tokenType == JsonToken.Float || tokenType == JsonToken.Integer;
		}

		#endregion
	}
}
