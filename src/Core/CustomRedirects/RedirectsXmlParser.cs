using System.Collections.Generic;
using System.IO;
using System.Xml;
using EPiServer.Logging;

namespace BVNetwork.NotFound.Core.CustomRedirects
{
    /// <summary>
    /// Class for reading and writing to the custom redirects file
    /// </summary>
    public class RedirectsXmlParser
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private XmlDocument _customRedirectsXmlFile = null;

        const string NEWURL = "new";
        const string OLDURL = "old";
        const string SKIPWILDCARD = "onWildCardMatchSkipAppend";

        /// <summary>
        /// Reads the custom redirects information from the specified xml file
        /// </summary>
        /// <param name="virtualFile">The virtual path to the xml file containing redirect settings</param>
        public RedirectsXmlParser(Stream xmlContent)
        {
            _customRedirectsXmlFile = new XmlDocument();
            if (xmlContent != null)
            {             
                _customRedirectsXmlFile.Load(xmlContent);
            }
            else
            {
                // Not on disk, not in a vpp, construct an empty one
                _customRedirectsXmlFile = new XmlDocument();
                _customRedirectsXmlFile.InnerXml = "<redirects><urls></urls></redirects>";
                Logger.Error("404 Handler: The Custom Redirects file '{0}' does not exist.", xmlContent);
            }
        }

        public RedirectsXmlParser()
        {
        }

        /// <summary>
        /// Parses the xml file and reads all redirects.
        /// </summary>
        /// <returns>A collection of CustomRedirect objects</returns>
        public CustomRedirectCollection Load()
        {
            const string URLPATH = "/redirects/urls/url";

            CustomRedirectCollection redirects = new CustomRedirectCollection();

            // Parse all url nodes
            XmlNodeList nodes = _customRedirectsXmlFile.SelectNodes(URLPATH);
            foreach (XmlNode node in nodes)
            {
                // Each url new url can have several old values
                // we need to create a redirect object for each pair
                XmlNode newNode = node.SelectSingleNode(NEWURL);

                XmlNodeList oldNodes = node.SelectNodes(OLDURL);
                foreach (XmlNode oldNode in oldNodes)
                {
                    bool skipWildCardAppend = false;
                    XmlAttribute skipWildCardAttr = oldNode.Attributes[SKIPWILDCARD];
                    if (skipWildCardAttr != null)
                    {
                        // If value parsing fails, it will be false by default. We do
                        // not really care to check if it fails, as we cannot do anything
                        // about it (throwing an exception is not a good idea here)
                        bool.TryParse(skipWildCardAttr.Value, out skipWildCardAppend);
                    }

                    // Create new custom redirect nodes
                    CustomRedirect redirect = new CustomRedirect(oldNode.InnerText, newNode.InnerText, skipWildCardAppend);
                    redirects.Add(redirect);
                }
            }

            return redirects;
        }

        public XmlDocument Export(List<CustomRedirect> redirects)
        {
            XmlDocument document = new XmlDocument();
            XmlDeclaration xmlDeclaration = document.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = document.DocumentElement;
            document.InsertBefore(xmlDeclaration, root);

            XmlElement redirectsElement = document.CreateElement(string.Empty, "redirects", string.Empty);
            document.AppendChild(redirectsElement);

            XmlElement urlsElement = document.CreateElement(string.Empty, "urls", string.Empty);
            redirectsElement.AppendChild(urlsElement);

            foreach (var redirect in redirects)
            {
                if (string.IsNullOrWhiteSpace(redirect.OldUrl) || string.IsNullOrWhiteSpace(redirect.NewUrl))
                {
                    continue;
                }

                XmlElement urlElement = document.CreateElement(string.Empty, "url", string.Empty);

                XmlElement oldElement = document.CreateElement(string.Empty, OLDURL, string.Empty);
                oldElement.AppendChild(document.CreateTextNode(redirect.OldUrl.Trim()));
                if (redirect.WildCardSkipAppend)
                {
                    XmlAttribute wildCardAttribute = document.CreateAttribute(string.Empty, SKIPWILDCARD, string.Empty);
                    wildCardAttribute.Value = "true";
                    oldElement.Attributes.Append(wildCardAttribute);
                }

                XmlElement newElement = document.CreateElement(string.Empty, NEWURL, string.Empty);
                newElement.AppendChild(document.CreateTextNode(redirect.NewUrl.Trim()));

                urlElement.AppendChild(oldElement);
                urlElement.AppendChild(newElement);

                urlsElement.AppendChild(urlElement);
            }

            return document;
        }
    }
}
