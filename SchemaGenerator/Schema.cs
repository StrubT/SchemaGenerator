using System.Collections.Generic;

namespace StrubT.SchemaGenerator {

	public class SchemaNode {

		readonly HashSet<ContentType> _contentTypes;
		readonly List<SchemaNode> _children;

		public NodeType NodeType { get; }

		public string Name { get; }

		public ISet<ContentType> ContentTypes { get; }

		public IReadOnlyCollection<SchemaNode> Children { get; }

		public SchemaNode(NodeType nodeType, string name) {

			NodeType = nodeType;
			Name = name;

			_contentTypes = new HashSet<ContentType>();
			ContentTypes = _contentTypes.AsReadOnlySet();
			if (nodeType == NodeType.Root)
				AddContentType(ContentType.Root);

			_children = new List<SchemaNode>();
			Children = _children.AsReadOnly();
		}

		public void AddContentType(ContentType contentType) => _contentTypes.Add(contentType);

		public void AddChild(SchemaNode child) => _children.Add(child);

		public override string ToString() => $"{NodeType} node '{Name ?? "N/A"}' (content type(s): {string.Join(", ", ContentTypes)}, {Children.Count} children)";
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