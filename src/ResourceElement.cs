namespace XamlCombine
{
	using System.Xml;

	/// <summary>
	/// Represents XAML resource.
	/// </summary>
	public struct ResourceElement
	{
		/// <summary>
		/// Resource name.
		/// </summary>
		public string Key;

		/// <summary>
		/// Resource XML node.
		/// </summary>
		public XmlElement Element;

		/// <summary>
		/// XAML keys used in this resource.
		/// </summary>
		public string[] UsedKeys;
	}
}