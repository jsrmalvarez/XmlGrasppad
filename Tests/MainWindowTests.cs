using System.Xml;
using Xunit;
using XmlNotepad.Utilities;

public class MainWindowTests
{
    [Fact]
    public void IsMatchingNodeContext_ShouldReturnTrue_ForExactMatch()
    {
        // Arrange
        string content = "<book id=\"1\"><title>Introduction to XML</title></book>";
        string nodeXml = "<title>Introduction to XML</title>";
        int index = content.IndexOf(nodeXml);
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(content);
        XmlNode xmlNode = doc.SelectSingleNode("//title");

        // Act
        bool result = XmlNodeMatcher.IsMatchingNodeContext(content, index, nodeXml, xmlNode);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatchingNodeContext_ShouldReturnFalse_ForDifferentParent()
    {
        // Arrange
        string content = "<book id=\"1\"><title>Introduction to XML</title></book><book id=\"2\"><title>Introduction to XML</title></book>";
        string nodeXml = "<title>Introduction to XML</title>";
        int index = content.IndexOf(nodeXml);
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(content);
        XmlNode xmlNode = doc.SelectSingleNode("//book[@id='2']/title");

        // Act
        bool result = XmlNodeMatcher.IsMatchingNodeContext(content, index, nodeXml, xmlNode);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatchingNodeContext_ShouldReturnTrue_ForRepeatedValueWithCorrectParent()
    {
        // Arrange
        string content = "<book id=\"1\"><price>29.99</price></book><book id=\"2\"><price>29.99</price></book>";
        string nodeXml = "<price>29.99</price>";
        int index = content.IndexOf(nodeXml, content.IndexOf(nodeXml) + 1); // Second occurrence
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(content);
        XmlNode xmlNode = doc.SelectSingleNode("//book[@id='2']/price");

        // Act
        bool result = XmlNodeMatcher.IsMatchingNodeContext(content, index, nodeXml, xmlNode);

        // Assert
        Assert.True(result);
    }

    private bool IsMatchingNodeContext(string content, int index, string nodeXml, XmlNode xmlNode)
    {
        // Placeholder for the actual implementation of IsMatchingNodeContext
        return true;
    }
}
