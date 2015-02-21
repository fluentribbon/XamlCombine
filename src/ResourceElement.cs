namespace XamlCombine
{
	using System.Xml;

	/// <summary>
	/// Represents XAML resource.
	/// </summary>
	public struct ResourceElement
	{
		public ResourceElement(string key, XmlElement element, string[] usedKeys)
		{
			this.Key = key;
			this.Element = element;
			this.UsedKeys = usedKeys;
		}

		/// <summary>
		/// Resource name.
		/// </summary>
		public string Key { get; private set; }

		/// <summary>
		/// Resource XML node.
		/// </summary>
		public XmlElement Element { get; private set; }

		/// <summary>
		/// XAML keys used in this resource.
		/// </summary>
		public string[] UsedKeys { get; private set; }
	}
}