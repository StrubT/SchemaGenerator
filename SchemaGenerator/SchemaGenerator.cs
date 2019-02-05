using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;

namespace StrubT.SchemaGenerator {

	public static class SchemaGenerator {

		static readonly string Iso8601DatePattern = @"(([+-]\d+)?\d{4}((-\d{2}){0,2}|(\d{2}){0,2}|(-W\d{2}(-\d)?)|(W\d{2}(\d)?)|(-?\d{3}))|--(\d{2}-?\d{2}))";
		static readonly string Iso8601TimePattern = @"(\d{2}((((:\d{2}){2}|(\d{2}){2})(\.\d{3})?)|(:\d{2})?|(\d{2})?)(Z|[+-]\d{2}(:?\d{2})?)?)";
		static readonly string Iso8601DateTimePattern = $"({Iso8601DatePattern}|{Iso8601TimePattern}|{Iso8601DatePattern}T{Iso8601TimePattern})";
		static readonly string Iso8601DurationPattern = $@"(P((?=.)((\d+Y)?(\d+M)?(\d+D)?|{Iso8601DatePattern})(T(?=.)((\d+H)?(\d+M)?(\d+S)?|{Iso8601TimePattern}))?|\d+W))";
		static readonly string Iso8601IntervalPattern = $@"((R\d*/)?({Iso8601DateTimePattern}/{Iso8601DateTimePattern}|{Iso8601DateTimePattern}/{Iso8601DurationPattern}|{Iso8601DurationPattern}/{Iso8601DateTimePattern}|{Iso8601DurationPattern}))";

		static Regex Iso8601Pattern { get; } = new Regex($"^({Iso8601DateTimePattern}|{Iso8601IntervalPattern})$", RegexOptions.Compiled);

		static ContentType DetectContentType(string value) {

			if (!string.IsNullOrEmpty(value)) {
				if (new[] { "true", "false" }.Any(s => s.Equals(value, StringComparison.InvariantCultureIgnoreCase)))
					return ContentType.Boolean;

				if (decimal.TryParse(value, out var @decimal))
					return @decimal % 1 == 0 ? ContentType.Integer : ContentType.Float;

				if (Iso8601Pattern.IsMatch(value))
					return ContentType.Iso8601;
			}

			return ContentType.Default;
		}

		public static Schema GenerateJsonSchema(JsonReader jsonReader) {
			var schema = new Schema();
			var nesting = new Stack<SchemaNode>();
			nesting.Push(schema);

			var propertyName = string.Empty;
			var contentType = ContentType.Default;

			using (jsonReader)
				while (jsonReader.Read())
					switch (jsonReader.TokenType) {
						case JsonToken.StartObject:
						case JsonToken.StartArray:
							var container = nesting.Peek().ChildNodes.SingleOrDefault(c => c.Name == propertyName);
							if (container is null) container = nesting.Peek().AddChildNode(name: propertyName);
							container.AddContentType(jsonReader.TokenType == JsonToken.StartArray ? ContentType.Array : ContentType.Object);
							nesting.Push(container);

							propertyName = string.Empty;
							break;

						case JsonToken.EndObject:
						case JsonToken.EndArray:
							nesting.Pop();
							break;

						case JsonToken.PropertyName:
							propertyName = (string)jsonReader.Value;
							break;

						case JsonToken.Boolean:
							contentType = ContentType.Boolean;
							goto case JsonToken.String;

						case JsonToken.Integer:
							contentType = ContentType.Integer;
							goto case JsonToken.String;

						case JsonToken.Float:
							contentType = ContentType.Float;
							goto case JsonToken.String;

						case JsonToken.Date:
							contentType = ContentType.DateTime;
							goto case JsonToken.String;

						case JsonToken.String:
							var value = nesting.Peek().ChildNodes.SingleOrDefault(c => c.Name == propertyName);
							if (value is null) value = nesting.Peek().AddChildNode(name: propertyName);
							value.AddContentType(contentType);
							if (jsonReader.TokenType == JsonToken.String)
								value.AddContentType(DetectContentType((string)jsonReader.Value));

							propertyName = string.Empty;
							contentType = ContentType.Default;
							break;

						default:
							throw new InvalidOperationException();
					}

			return schema;
		}

		public static Schema GenerateXmlSchema(XmlReader xmlReader) {
			var schema = new Schema();
			var nesting = new Stack<SchemaNode>();
			nesting.Push(schema);

			using (xmlReader)
				while (xmlReader.Read())
					switch (xmlReader.NodeType) {
						case XmlNodeType.Element:
							var qualifiedName = !string.IsNullOrEmpty(xmlReader.NamespaceURI) ? $"[{xmlReader.NamespaceURI}]:{xmlReader.LocalName}" : xmlReader.LocalName;
							var container = schema.AllNodes.SingleOrDefault(c => c.NodeType == NodeType.Child && c.Name == qualifiedName);
							if (!(container is null)) nesting.Peek().AddChildNode(container);
							else {
								container = nesting.Peek().ChildNodes.SingleOrDefault(c => c.NodeType == NodeType.Child && c.Name == qualifiedName);
								if (container is null) container = nesting.Peek().AddChildNode(NodeType.Child, qualifiedName);
							}
							container.AddContentType(ContentType.Object);

							if (!xmlReader.IsEmptyElement) nesting.Push(container);

							while (xmlReader.MoveToNextAttribute()) {
								var attribute = container.ChildNodes.SingleOrDefault(c => c.NodeType == NodeType.Attribute && c.Name == xmlReader.Name);
								if (attribute is null) attribute = container.AddChildNode(NodeType.Attribute, xmlReader.Name);
								attribute.AddContentType(DetectContentType(xmlReader.Value));
							}
							break;

						case XmlNodeType.EndElement:
							nesting.Pop();
							break;

						case XmlNodeType.CDATA:
						case XmlNodeType.Text:
							var value = nesting.Peek().ChildNodes.SingleOrDefault(c => c.NodeType == NodeType.Child && c.Name is null);
							if (value is null) value = nesting.Peek().AddChildNode(NodeType.Child);
							value.AddContentType(DetectContentType(xmlReader.Value));
							break;

						default:
							throw new InvalidOperationException();
					}

			return schema;
		}
	}
}
