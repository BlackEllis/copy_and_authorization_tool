using System;
using System.Collections.Generic;

namespace tool_commons.model
{
    [System.Xml.Serialization.XmlRoot("comparison_table")]
    public class comparison_table
    {
        [System.Xml.Serialization.XmlElement("comparsion_units")]
        public List<comparsion_unit> comparsion_units { get; set; }

        public comparison_table()
        {
            comparsion_units = new List<comparsion_unit>();
        }

        public void list_sort()
        {
            comparsion_units.Sort((a, b) => string.Compare(a.account_name, b.account_name));
        }

        public Dictionary<string, comparsion_unit> transform_to_dictionary()
        {
            Dictionary<string, comparsion_unit> dst_dictionary = new Dictionary<string, comparsion_unit>();
            if (comparsion_units == null) return null;
            foreach (comparsion_unit unit in comparsion_units)
            {
                dst_dictionary.Add(unit.conversion_original, unit);
            }
            return dst_dictionary;
        }
    }

    /// <summary>
    /// S-ID対比情報格納クラス
    /// </summary>
    public class comparsion_unit : IEquatable<comparsion_unit>
    {
        [System.Xml.Serialization.XmlAttribute("account_name")]
        public string account_name { get; set; }
        [System.Xml.Serialization.XmlElement("first_name")]
        public string first_name { get; set; }
        [System.Xml.Serialization.XmlElement("last_name")]
        public string last_name { get; set; }
        [System.Xml.Serialization.XmlElement("mailaddress")]
        public string mailaddress { get; set; }
        [System.Xml.Serialization.XmlElement("affiliation")]
        public string affiliation { get; set; }
        [System.Xml.Serialization.XmlElement("job_title")]
        public string job_title { get; set; }
        [System.Xml.Serialization.XmlElement("del_flg")]
        public int del_flg { get; set; }
        [System.Xml.Serialization.XmlElement("conversion_original")]
        public string conversion_original { get; set; }
        [System.Xml.Serialization.XmlElement("after_conversion")]
        public string after_conversion { get; set; }

        /// <summary>
        ///  コンストラクタ
        /// </summary>
        public comparsion_unit() { }
        public comparsion_unit(user_info source, user_info target, string src_conversion_original, string src_after_conversion)
        {
            account_name = target.account_name;
            first_name = target.first_name;
            last_name = target.last_name;
            mailaddress = target.mailaddress;
            affiliation = target.affiliation;
            job_title = target.job_title;
            conversion_original = src_conversion_original;
            after_conversion = src_after_conversion;
            del_flg = target.del_flg;
        }

        /// <summary>
        /// 比較メソッド
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public bool Equals(comparsion_unit src)
        {
            if (src == null) return false;
            return this.account_name == src.account_name;
        }
    }
}
