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

		public static SchemaNode GenerateJsonSchema(JsonReader jsonReader) {
			var nesting = new List<SchemaNode> { new SchemaNode(NodeType.Root, null) };
			var propertyName = string.Empty;
			var contentType = ContentType.Default;

			using (jsonReader)
				while (jsonReader.Read())
					switch (jsonReader.TokenType) {
						case JsonToken.StartObject:
						case JsonToken.StartArray:
							var container = nesting.Last().Children.SingleOrDefault(c => c.Name == propertyName);
							if (container is null) nesting.Last().AddChild(container = new SchemaNode(NodeType.Child, propertyName));
							container.AddContentType(jsonReader.TokenType == JsonToken.StartArray ? ContentType.Array : ContentType.Object);
							nesting.Add(container);

							propertyName = string.Empty;
							break;

						case JsonToken.EndObject:
						case JsonToken.EndArray:
							nesting.RemoveAt(nesting.Count - 1);
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
							var value = nesting.Last().Children.SingleOrDefault(c => c.Name == propertyName);
							if (value is null) nesting.Last().AddChild(value = new SchemaNode(NodeType.Child, propertyName));
							value.AddContentType(contentType);

							propertyName = string.Empty;
							contentType = ContentType.Default;
							break;

						default:
							throw new Exception();
					}

			return nesting.Single();
		}

		public static SchemaNode GenerateXmlSchema(XmlReader xmlReader) {
			var nesting = new List<SchemaNode> { new SchemaNode(NodeType.Root, null) };

			using (xmlReader)
				while (xmlReader.Read())
					switch (xmlReader.NodeType) {
						case XmlNodeType.Element:
							var qualifiedName = $"[{xmlReader.NamespaceURI}]:{xmlReader.LocalName}";
							var container = nesting.Last().Children.SingleOrDefault(c => c.NodeType == NodeType.Child && c.Name == qualifiedName);
							if (container is null) nesting.Last().AddChild(container = new SchemaNode(NodeType.Child, qualifiedName));
							container.AddContentType(ContentType.Object);

							if (!xmlReader.IsEmptyElement) nesting.Add(container);

							while (xmlReader.MoveToNextAttribute()) {
								var attribute = container.Children.SingleOrDefault(c => c.NodeType == NodeType.Attribute && c.Name == xmlReader.Name);
								if (attribute is null) container.AddChild(attribute = new SchemaNode(NodeType.Attribute, xmlReader.Name));
								attribute.AddContentType(DetectContentType(xmlReader.Value));
							}
							break;

						case XmlNodeType.EndElement:
							nesting.RemoveAt(nesting.Count - 1);
							break;

						case XmlNodeType.CDATA:
						case XmlNodeType.Text:
							var value = nesting.Last().Children.SingleOrDefault(c => c.NodeType == NodeType.Child && c.Name is null);
							if (value is null) nesting.Last().AddChild(value = new SchemaNode(NodeType.Child, null));
							value.AddContentType(DetectContentType(xmlReader.Value));
							break;

						default:
							throw new Exception();
					}

			return nesting.Single();
		}
	}
}
