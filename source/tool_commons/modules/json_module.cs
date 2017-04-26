using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace tool_commons.modules
{
    class json_module
    {
        private static Dictionary<string, string> _external_resources;
        private static Dictionary<string, List<string>> _external_resources_list;

        /// <summary>
        /// jsonで設定されていた値の取得
        /// </summary>
        /// <param name="key">要素名</param>
        /// <returns>指定された要素の値</returns>
        public static string get_external_resource(string key, string default_value = "")
        {
            if (_external_resources == null) return default_value;
            if (_external_resources.ContainsKey(key))
                return _external_resources[key];
            else
                return default_value;
        }

        /// <summary>
        /// jsonで配列が設定されていた値の取得
        /// </summary>
        /// <param name="key">要素名</param>
        /// <returns>指定された要素の配列</returns>
        public static List<string> get_external_resource_list(string key, List<string> default_value = null)
        {
            if (_external_resources_list == null) return default_value;
            if (_external_resources_list.ContainsKey(key))
                return _external_resources_list[key];
            else
                return default_value;
        }

        /// <summary>
        /// 引数で設定されたファイルから要素名と値の取出し
        /// </summary>
        /// <param name="src_file"></param>
        public static void setup(string src_file)
        {
            try
            {
                using (FileStream file_st = new FileStream(src_file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var st_reader = new System.IO.StreamReader(file_st, System.Text.Encoding.UTF8))
                {
                    string json_str = st_reader.ReadToEnd();
                    XmlDictionaryReader xml_reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json_str), XmlDictionaryReaderQuotas.Max);
                    if (_external_resources == null) _external_resources = new Dictionary<string, string>();
                    if (_external_resources_list == null) _external_resources_list = new Dictionary<string, List<string>>();

                    bool list_element_flg = false;
                    string key = "";
                    while (xml_reader.Read())
                    {
                        switch (xml_reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                if (xml_reader.Depth == 0) break;
                                if (xml_reader.Name.Equals("item"))
                                    list_element_flg = !list_element_flg;
                                else
                                    key = xml_reader.Name;

                                break;
                            case XmlNodeType.EndElement:
                                if (xml_reader.Name.Equals("item"))
                                    list_element_flg = !list_element_flg;

                                break;
                            case XmlNodeType.Text:
                                if (list_element_flg)
                                {
                                    if (!_external_resources_list.ContainsKey(key))
                                        _external_resources_list.Add(key, new List<string>() { xml_reader.Value });
                                    else
                                        _external_resources_list[key].Add(xml_reader.Value);
                                }
                                else
                                {
                                    if (!_external_resources.ContainsKey(key))
                                        _external_resources.Add(key, xml_reader.Value);
                                    else
                                        _external_resources[key] += xml_reader.Value;
                                }
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
