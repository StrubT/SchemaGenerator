using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace StrubT.SchemaGenerator {

	public class Schema : SchemaNode {

		readonly HashSet<string> _allSchemaTypes;
		readonly HashSet<SchemaNode> _allNodes;

		public IReadOnlyCollection<string> AllSchemaTypes { get; }

		public IReadOnlyCollection<SchemaNode> AllNodes { get; }

		public Schema(string name = null) : base(NodeType.Root, name) {

			Schema = this;

			_allSchemaTypes = new HashSet<string>();
			AllSchemaTypes = _allSchemaTypes.AsReadOnly();

			_allNodes = new HashSet<SchemaNode>();
			AllNodes = _allNodes.AsReadOnly();
		}

		internal void AddSchemaType(string schemaType) => _allSchemaTypes.Add(schemaType);

		internal void AddNode(SchemaNode node) => _allNodes.Add(node);

		//public static Schema Load(Stream stream) { } // TODO

		public void Persist(string mainFile, string typedFileFormat = null) {

			File.WriteAllText(mainFile, JsonConvert.SerializeObject(SerializeSchema(this)), Encoding.UTF8);

			if (typedFileFormat != null)
				foreach (var schemaType in AllSchemaTypes)
					File.WriteAllText(string.Format(typedFileFormat, schemaType), JsonConvert.SerializeObject(SerializeSchema(this, schemaType)), Encoding.UTF8);
		}

		static Dictionary<string, object> SerializeSchema(Schema schema, string type = null) {

			var ret = new Dictionary<string, object>();
			if (!string.IsNullOrEmpty(schema.Name)) ret.Add("name", schema.Name);
			if (string.IsNullOrEmpty(type)) ret.Add("schemaTypes", schema.AllSchemaTypes.ToList());
			ret.Add("children", schema.ChildNodes.Where(n => string.IsNullOrEmpty(type) || n.SchemaTypes.Contains(type)).Select(n => SerializeSchema(n, type)).ToList());

			return ret;
		}

		static Dictionary<string, object> SerializeSchema(SchemaNode node, string type = null) {

			var ret = new Dictionary<string, object>();
			if (!string.IsNullOrEmpty(node.Name)) ret.Add("name", node.Name);
			ret.Add("type", Enum.GetName(typeof(NodeType), node.NodeType));
			if (string.IsNullOrEmpty(type)) ret.Add("schemaTypes", node.SchemaTypes.ToList());
			ret.Add("contentTypes", node.ContentTypes.Select(t => Enum.GetName(typeof(ContentType), t)).ToList());

			var retValue = new Dictionary<string, object>();
			if (node.ValueCount.Total > 0) retValue.Add("totalCount", node.ValueCount.Total);
			if (node.ValueCount.Empty > 0) retValue.Add("emptyCount", node.ValueCount.Empty);
			if (node.ValueLength.Min.HasValue) retValue.Add("length", new Dictionary<string, object> { { "min", node.ValueLength.Min }, { "max", node.ValueLength.Max } });
			if (node.NumericValues.Min.HasValue) retValue.Add("numeric", new Dictionary<string, object> { { "min", node.NumericValues.Min }, { "max", node.NumericValues.Max } });
			if (node.DateTimeValues.Min.HasValue) retValue.Add("dateTime", new Dictionary<string, object> { { "min", node.DateTimeValues.Min }, { "max", node.DateTimeValues.Max } });
			if (node.TimeSpanValues.Min.HasValue) retValue.Add("timeSpan", new Dictionary<string, object> { { "min", node.TimeSpanValues.Min }, { "max", node.TimeSpanValues.Max } });
			if (retValue.Any()) ret.Add("value", retValue);

			ret.Add("children", node.ChildNodes.Where(n => string.IsNullOrEmpty(type) || n.SchemaTypes.Contains(type)).Select(n => SerializeSchema(n, type)).ToList());

			return ret;
		}

		public override string ToString() => string.Join(" ", string.IsNullOrEmpty(Name) ? "unnamed" : string.Empty, "Schema", !string.IsNullOrEmpty(Name) ? $"'{Name}'" : string.Empty);
	}

	public class SchemaNode {

		internal Schema Schema;
		readonly HashSet<string> _schemaTypes;
		readonly HashSet<ContentType> _contentTypes;
		readonly HashSet<SchemaNode> _childNodes;

		public NodeType NodeType { get; }

		public string Name { get; }

		public IReadOnlyCollection<string> SchemaTypes { get; }

		public IReadOnlyCollection<ContentType> ContentTypes { get; }

		public IReadOnlyCollection<SchemaNode> ChildNodes { get; }

		public (int Total, int Empty) ValueCount { get; private set; }

		public (int? Min, int? Max) ValueLength { get; private set; }

		public (decimal? Min, decimal? Max) NumericValues { get; private set; }

		public (DateTime? Min, DateTime? Max) DateTimeValues { get; private set; }

		public (TimeSpan? Min, TimeSpan? Max) TimeSpanValues { get; private set; }

		protected SchemaNode(NodeType nodeType = NodeType.Child, string name = null) {

			NodeType = nodeType;
			Name = name;

			_schemaTypes = new HashSet<string>();
			SchemaTypes = _schemaTypes.AsReadOnly();

			_contentTypes = new HashSet<ContentType>();
			ContentTypes = _contentTypes.AsReadOnly();
			if (nodeType == NodeType.Root)
				_contentTypes.Add(ContentType.Root);

			_childNodes = new HashSet<SchemaNode>();
			ChildNodes = _childNodes.AsReadOnly();
		}

		public void AddValue(SchemaValue value, string type = null) {

			if (!string.IsNullOrWhiteSpace(type)) {
				Schema.AddSchemaType(type);
				_schemaTypes.Add(type);
			}

			_contentTypes.Add(value.Type);

			ValueCount = (ValueCount.Total + 1, ValueCount.Empty + (value.Type == ContentType.Empty ? 1 : 0));

			switch (value.Type) {
				case ContentType.String:
					ValueLength = GetMinMax(ValueLength, value.StringValue.Length);
					break;

				case ContentType.NumericInteger:
				case ContentType.NumericDecimal:
					NumericValues = GetMinMax(NumericValues, value.NumericValue.Value);
					break;

				case ContentType.Boolean:
					break;

				case ContentType.DateTime:
					DateTimeValues = GetMinMax(DateTimeValues, value.DateTimeValue.Value);
					break;

				case ContentType.TimeSpan:
					TimeSpanValues = GetMinMax(TimeSpanValues, value.TimeSpanValue.Value);
					break;

				case ContentType.Array:
					ValueLength = GetMinMax(ValueLength, value.ArrayLength.Value);
					break;

				case ContentType.Empty:
				case ContentType.Root:
				case ContentType.Object:
					break;

				default:
					throw new ArgumentException();
			}

			(T? Min, T? Max) GetMinMax<T>((T? Min, T? Max) range, T newValue) where T : struct, IComparable<T> =>
				(!range.Min.HasValue || range.Min.Value.CompareTo(newValue) < 0 ? range.Min : newValue, !range.Max.HasValue || range.Max.Value.CompareTo(newValue) > 0 ? range.Max : newValue);
		}

		public SchemaNode AddChildNode(NodeType nodeType = NodeType.Child, string name = null) {

			var childNode = new SchemaNode(nodeType, name) { Schema = Schema };
			AddChildNode(childNode);
			return childNode;
		}

		public void AddChildNode(SchemaNode childNode) {

			if (childNode.Schema != Schema)
				throw new ArgumentException();

			Schema.AddNode(childNode);
			_childNodes.Add(childNode);
		}

		public override string ToString() => string.Join(string.Empty,
			string.IsNullOrEmpty(Name) ? "unnamed " : string.Empty,
			$"{NodeType} node",
			!string.IsNullOrEmpty(Name) ? $" '{Name}'" : string.Empty,
			"(", ContentTypes.Any() ? $"content type(s): {string.Join(", ", ContentTypes)}" : "no content", ", ",
			ChildNodes.Any() ? $"{ChildNodes.Count} child{(ChildNodes.Count != 1 ? "ren" : string.Empty)}" : "no children", ")");
	}
}
