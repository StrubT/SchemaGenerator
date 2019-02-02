using System.Collections.Generic;

namespace StrubT {

	public enum ElementType { Child, Attribute }

	public enum ValueType { String, Integer, Float, Boolean }

	public class Element {

		public ElementType Type { get; }

		public string Name { get; }

		public ISet<ValueType> Types { get; } = new HashSet<ValueType>();

		public ICollection<Element> Children { get; } = new List<Element>();

		public Element(ElementType type, string name) => (Type, Name) = (type, name);
	}
}
