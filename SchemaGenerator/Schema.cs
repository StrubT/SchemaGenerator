using System;
using System.Collections.Generic;
using System.Linq;

namespace StrubT.SchemaGenerator {

	public class Schema : SchemaNode {

		readonly HashSet<SchemaNode> _allNodes;

		public IReadOnlyCollection<SchemaNode> AllNodes { get; }

		public Schema(string name = null) : base(NodeType.Root, name) {

			Schema = this;

			_allNodes = new HashSet<SchemaNode>();
			AllNodes = _allNodes.AsReadOnly();
		}

		internal void AddNode(SchemaNode node) => _allNodes.Add(node);

		public override string ToString() => string.Join(" ", string.IsNullOrEmpty(Name) ? "unnamed" : string.Empty, "Schema", !string.IsNullOrEmpty(Name) ? $"'{Name ?? "N/A"}'" : string.Empty);
	}

	public class SchemaNode {

		internal Schema Schema;
		readonly HashSet<ContentType> _contentTypes;
		readonly HashSet<SchemaNode> _childNodes;

		public NodeType NodeType { get; }

		public string Name { get; }

		public IReadOnlyCollection<ContentType> ContentTypes { get; }

		public IReadOnlyCollection<SchemaNode> ChildNodes { get; }

		protected SchemaNode(NodeType nodeType = NodeType.Child, string name = null) {

			NodeType = nodeType;
			Name = name;

			_contentTypes = new HashSet<ContentType>();
			ContentTypes = _contentTypes.AsReadOnly();
			if (nodeType == NodeType.Root)
				AddContentType(ContentType.Root);

			_childNodes = new HashSet<SchemaNode>();
			ChildNodes = _childNodes.AsReadOnly();
		}

		public void AddContentType(ContentType contentType) => _contentTypes.Add(contentType);

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
			!string.IsNullOrEmpty(Name) ? $" '{Name ?? "N/A"}'" : string.Empty,
			"(", ContentTypes.Any() ? $"content type(s): {string.Join(", ", ContentTypes)}" : "no content", ", ",
			ChildNodes.Any() ? $"{ChildNodes.Count} child{(ChildNodes.Count != 1 ? "ren" : string.Empty)}" : "no children", ")");
	}

	public enum NodeType { Root, Child, Attribute }

	public enum ContentType {
		Root,
		Object, Array,
		String,
		Integer, Float, Boolean, Iso8601, /*Uri,*/
		Default = String, DateTime = Iso8601, TimeSpan = Iso8601
	}
}
