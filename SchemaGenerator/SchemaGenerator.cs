using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;

namespace StrubT.SchemaGenerator {

	public static class SchemaGenerator {

		public static Schema GenerateSchema(JsonReader reader, string name = null, string type = null) {

			var schema = new Schema(name);
			ReadToSchema(schema, reader, type);

			return schema;
		}

		public static void ReadToSchema(Schema schema, JsonReader reader, string type = null) {

			var nesting = new Stack<SchemaNode>();
			nesting.Push(schema);

			var propertyName = default(string);
			var schemaValue = default(SchemaValue);
			var arrayLength = 0;

			using (reader)
				while (reader.Read())
					switch (reader.TokenType) {
						case JsonToken.StartObject:
						case JsonToken.StartArray:
							var container = nesting.Peek().ChildNodes.SingleOrDefault(c => c.Name == propertyName);
							if (container is null) container = nesting.Peek().AddChildNode(name: propertyName);
							nesting.Push(container);

							propertyName = null;
							break;

						case JsonToken.EndObject:
							container = nesting.Pop();
							container.AddValue(new SchemaValue { Type = ContentType.Object }, type);
							break;

						case JsonToken.EndArray:
							container = nesting.Pop();
							container.AddValue(new SchemaValue { Type = ContentType.Array, ArrayLength = arrayLength }, type);
							arrayLength = 0;
							break;

						case JsonToken.PropertyName:
							propertyName = (string)reader.Value;
							break;

						case JsonToken.Null:
							schemaValue = new SchemaValue { Type = ContentType.Empty };
							goto case JsonToken.String;

						case JsonToken.Boolean:
							schemaValue = new SchemaValue { Type = ContentType.Boolean, BooleanValue = (bool)reader.Value };
							goto case JsonToken.String;

						case JsonToken.Integer:
							schemaValue = new SchemaValue { Type = ContentType.NumericInteger, NumericValue = (long)reader.Value };
							goto case JsonToken.String;

						case JsonToken.Float:
							schemaValue = new SchemaValue { Type = ContentType.NumericDecimal, NumericValue = (decimal)(double)reader.Value };
							goto case JsonToken.String;

						case JsonToken.Date:
							schemaValue = new SchemaValue { Type = ContentType.DateTime, DateTimeValue = (DateTime)reader.Value };
							goto case JsonToken.String;

						case JsonToken.String:
							var value = nesting.Peek().ChildNodes.SingleOrDefault(c => c.Name == propertyName);
							if (value is null) value = nesting.Peek().AddChildNode(name: propertyName);

							if (reader.TokenType == JsonToken.String) {
								value.AddValue(new SchemaValue { Type = ContentType.String, StringValue = (string)reader.Value }, type);
								value.AddValue(SchemaValue.ParseValue((string)reader.Value), type);
							} else
								value.AddValue(schemaValue, type);

							arrayLength++;
							propertyName = null;
							schemaValue = new SchemaValue { Type = ContentType.Empty };
							break;

						default:
							throw new InvalidOperationException($"Unhandled token type '{reader.TokenType:G}' encountered.");
					}
		}

		public static Schema GenerateSchema(XmlReader reader, string name = null, string type = null) {

			var schema = new Schema(name);
			ReadToSchema(schema, reader, type);

			return schema;
		}

		public static void ReadToSchema(Schema schema, XmlReader reader, string type = null) {

			var nesting = new Stack<SchemaNode>();
			nesting.Push(schema);

			using (reader)
				while (reader.Read())
					switch (reader.NodeType) {
						case XmlNodeType.Element:
							var qualifiedName = !string.IsNullOrEmpty(reader.NamespaceURI) ? $"[{reader.NamespaceURI}]:{reader.LocalName}" : reader.LocalName;
							var container = schema.AllNodes.SingleOrDefault(c => c.NodeType == NodeType.Child && c.Name == qualifiedName);
							if (!(container is null)) nesting.Peek().AddChildNode(container);
							else {
								container = nesting.Peek().ChildNodes.SingleOrDefault(c => c.NodeType == NodeType.Child && c.Name == qualifiedName);
								if (container is null) container = nesting.Peek().AddChildNode(NodeType.Child, qualifiedName);
							}
							if (!reader.IsEmptyElement) nesting.Push(container);

							container.AddValue(new SchemaValue { Type = ContentType.Object }, type);

							while (reader.MoveToNextAttribute()) {
								var attribute = container.ChildNodes.SingleOrDefault(c => c.NodeType == NodeType.Attribute && c.Name == reader.Name);
								if (attribute is null) attribute = container.AddChildNode(NodeType.Attribute, reader.Name);
								attribute.AddValue(SchemaValue.ParseValue(reader.Value), type);
							}
							break;

						case XmlNodeType.EndElement:
							nesting.Pop();
							break;

						case XmlNodeType.CDATA:
						case XmlNodeType.Text:
							var value = nesting.Peek().ChildNodes.SingleOrDefault(c => c.NodeType == NodeType.Child && c.Name is null);
							if (value is null) value = nesting.Peek().AddChildNode(NodeType.Child);
							value.AddValue(SchemaValue.ParseValue(reader.Value), type);
							break;

						case XmlNodeType.SignificantWhitespace:
							break;

						default:
							throw new InvalidOperationException($"Unhandled node type '{reader.NodeType:G}' encountered.");
					}
		}
	}
}
