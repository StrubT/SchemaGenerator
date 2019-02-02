using System;
using System.IO;
using System.Net;
using System.Xml;
using Newtonsoft.Json;

namespace StrubT {

	public static class SchemaGenerator {

		public static void Main() {

			using (var webClient = new WebClient()) {
				webClient.Headers.Add(HttpRequestHeader.Referer, "https://StrubT.ch");

				using (var stream = new StreamReader(webClient.OpenRead("https://StrubT.ch/api/education/BSc")))
				using (var r = new JsonTextReader(stream))
					while (r.Read())
						switch (r.TokenType) {
							case JsonToken.StartObject:
							case JsonToken.StartArray:
								Console.WriteLine($"[{r.TokenType}]{r.Value}");
								break;

							case JsonToken.EndObject:
							case JsonToken.EndArray:
								Console.WriteLine($"[{r.TokenType}]");
								break;

							case JsonToken.PropertyName:
								Console.WriteLine($"[{r.TokenType}]{r.Value}");
								break;

							case JsonToken.Boolean:
							case JsonToken.Bytes:
							case JsonToken.Date:
							case JsonToken.Float:
							case JsonToken.Integer:
							case JsonToken.String:
								Console.WriteLine($"[{r.TokenType}]{r.Value}");
								break;

							case JsonToken.Null:
							case JsonToken.Undefined:
								Console.WriteLine($"[{r.TokenType}]");
								break;

							case JsonToken.Comment:
								break;

							default:
								throw new Exception();
						}

				using (var stream = new StreamReader(webClient.OpenRead("https://StrubT.ch/education/BSc/courses")))
				using (var r = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore }))
					while (r.MoveToNextAttribute() || r.Read())
						switch (r.NodeType) {
							case XmlNodeType.Element:
								Console.WriteLine($"[{r.NodeType},{r.Prefix}]{r.LocalName}");
								break;

							case XmlNodeType.EndElement:
								Console.WriteLine($"[{r.NodeType},{r.NamespaceURI}]{r.LocalName}");
								break;

							case XmlNodeType.Attribute:
								Console.WriteLine($"[{r.NodeType}]{r.Name}: {r.Value}");
								break;

							case XmlNodeType.CDATA:
							case XmlNodeType.Text:
								Console.WriteLine($"[{r.NodeType}]{r.Value}");
								break;

							case XmlNodeType.Comment:
							case XmlNodeType.Whitespace:
								break;

							default:
								throw new Exception();
						}
			}
		}
	}
}
