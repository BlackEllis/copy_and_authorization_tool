using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace tool_commons.modules
{
    class utility_tools
    {
        /// <summary>
        /// =でつながれたパラメータと値を連想配列にして返す
        /// </summary>
        /// <param name="src_str_array">パラメータと値が混在している配列</param>
        /// <returns>パラメータをキーとした連想配列</returns>
        public static Dictionary<string, string> argument_decomposition(string[] src_str_array)
        {
            Dictionary<string, string> dst_hash_array = new Dictionary<string, string>();

            // 文字配列から要素毎に取り出す
            foreach (String str in src_str_array)
            {
                string element = str;

                // 1要素からパラメータ名と値分割する
                string[] str_array = element.Split('=');
                if (str_array.Count() > 1)　// パラメータ名と値が切り分けれない
                    dst_hash_array.Add(str_array[0], str_array[1]); // 切り分けたキー名と値の格納
                else
                    dst_hash_array.Add(str_array[0], str_array[0]); // キー名と値を同じ値で格納
            }

            return dst_hash_array;
        }

        /// <summary>
        /// phpのvar_dump関数
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="recursion"></param>
        /// <returns></returns>
        public static string var_dump(object obj, int recursion)
        {
            StringBuilder result = new StringBuilder();

            // Protect the method against endless recursion
            if (recursion < 5)
            {
                // Determine object type
                Type t = obj.GetType();

                // Get array with properties for this object
                PropertyInfo[] properties = t.GetProperties();

                foreach (PropertyInfo property in properties)
                {
                    try
                    {
                        // Get the property value
                        object value = property.GetValue(obj, null);

                        // Create indenting string to put in front of properties of a deeper level
                        // We'll need this when we display the property name and value
                        string indent = String.Empty;
                        string spaces = "|   ";
                        string trail = "|...";

                        if (recursion > 0)
                        {
                            indent = new StringBuilder(trail).Insert(0, spaces, recursion - 1).ToString();
                        }

                        if (value != null)
                        {
                            // If the value is a string, add quotation marks
                            string displayValue = value.ToString();
                            if (value is string) displayValue = String.Concat('"', displayValue, '"');

                            // Add property name and value to return string
                            result.AppendFormat("{0}{1} = {2}\n", indent, property.Name, displayValue);

                            try
                            {
                                if (!(value is ICollection))
                                {
                                    // Call var_dump() again to list child properties
                                    // This throws an exception if the current property value
                                    // is of an unsupported type (eg. it has not properties)
                                    result.Append(var_dump(value, recursion + 1));
                                }
                                else
                                {
                                    // 2009-07-29: added support for collections
                                    // The value is a collection (eg. it's an arraylist or generic list)
                                    // so loop through its elements and dump their properties
                                    int elementCount = 0;
                                    foreach (object element in ((ICollection)value))
                                    {
                                        string elementName = String.Format("{0}[{1}]", property.Name, elementCount);
                                        indent = new StringBuilder(trail).Insert(0, spaces, recursion).ToString();

                                        // Display the collection element name and type
                                        result.AppendFormat("{0}{1} = {2}\n", indent, elementName, element.ToString());

                                        // Display the child properties
                                        result.Append(var_dump(element, recursion + 2));
                                        elementCount++;
                                    }

                                    result.Append(var_dump(value, recursion + 1));
                                }
                            }
                            catch { }
                        }
                        else
                        {
                            // Add empty (null) property to return string
                            result.AppendFormat("{0}{1} = {2}\n", indent, property.Name, "null");
                        }
                    }
                    catch
                    {
                        // Some properties will throw an exception on property.GetValue()
                        // I don't know exactly why this happens, so for now i will ignore them...
                    }
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// 引数の連想配列値か引数の基本値から値を取り出す
        /// </summary>
        /// <param name="src_array">取り出す連想配列</param>
        /// <param name="key">連想配列のキー情報</param>
        /// <param name="default_value">基本値</param>
        /// <returns></returns>
        public static string get_value_from_hasharray(Dictionary<string, string> src_array, string key, string default_value)
        {
            if (src_array == null) return default_value;
            if (src_array.ContainsKey(key))
                return src_array[key];
            else
                return default_value;
        }
    }
}
