namespace NServiceBus
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using Routing;

    class FileRoutingTableParser
    {
        public FileRoutingTableParser()
        {
            schema = new XmlSchemaSet();
            schema.Add("", XmlReader.Create(new StringReader(schemaText)));
        }

        public IEnumerable<EndpointInstance> Parse(XDocument document)
        {
            document.Validate(schema, null, true);

            var root = document.Root;
            var endpointElements = root.Descendants("endpoint");

            var instances = new List<EndpointInstance>();

            foreach (var e in endpointElements)
            {
                var endpointName = e.Attribute("name").Value;

                foreach (var i in e.Descendants("instance"))
                {
                    var discriminatorAttribute = i.Attribute("discriminator");
                    var discriminator = discriminatorAttribute?.Value;

                    var properties = i.Attributes().Where(a => a.Name != "discriminator");
                    var propertyDictionary = properties.ToDictionary(a => a.Name.LocalName, a => a.Value);

                    instances.Add(new EndpointInstance(endpointName, discriminator, propertyDictionary));
                }
            }

            return instances;
        }

        XmlSchemaSet schema;

        const string schemaText = @"<?xml version='1.0' encoding='utf-8'?>
<xs:schema attributeFormDefault='unqualified' elementFormDefault='qualified' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
  <xs:element name='endpoints'>
    <xs:complexType>
      <xs:sequence>
        <xs:element maxOccurs='unbounded' name='endpoint'>
          <xs:complexType>
            <xs:sequence>
              <xs:element maxOccurs='unbounded' minOccurs='0' name='instance'>
                <xs:complexType>
                  <xs:attribute name='discriminator' type='xs:string' use='optional' />
                  <xs:anyAttribute processContents='lax' />
                </xs:complexType>
              </xs:element>
            </xs:sequence>
            <xs:attribute name='name' type='xs:string' use='required' />
          </xs:complexType>
        </xs:element>
      </xs:sequence>
    </xs:complexType>
  </xs:element>
</xs:schema>
";
    }
}