using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace comparison_front_creating_tool.modules
{
    class json_module
    {
        private static Dictionary<string, string> _external_resources;

        public static string get_external_resource(string key) {
            if (_external_resources == null) return "";
            if (_external_resources.ContainsKey(key))
                return _external_resources[key];
            else
                return "";
        }
        public static void setup(string src_file)
        {
            try
            {
                using (var st_reader = new System.IO.StreamReader(src_file, System.Text.Encoding.UTF8))
                {
                    string json_str = st_reader.ReadToEnd();
                    XmlDictionaryReader xml_reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json_str), XmlDictionaryReaderQuotas.Max);
                    if (_external_resources == null) _external_resources = new Dictionary<string, string>();

                    string key = "";
                    while (xml_reader.Read())
                    {
                        switch (xml_reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                if (xml_reader.Depth == 0) break;

                                key = xml_reader.Name;

                                if (xml_reader.MoveToAttribute("item")) key = xml_reader.Value;
                                break;
                            case XmlNodeType.Text:
                                if (!_external_resources.ContainsKey(key))
                                    _external_resources.Add(key, xml_reader.Value);
                                else
                                    _external_resources[key] += xml_reader.Value;
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
