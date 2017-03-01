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
    }

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
        [System.Xml.Serialization.XmlElement("source_sid")]
        public string source_sid { get; set; }
        [System.Xml.Serialization.XmlElement("target_sid")]
        public string target_sid { get; set; }
        [System.Xml.Serialization.XmlElement("del_flg")]
        public int del_flg { get; set; }

        /// <summary>
        ///  コンストラクタ
        /// </summary>
        public comparsion_unit() { }
        public comparsion_unit(user_info source, user_info target)
        {
            account_name = target.account_name;
            first_name = target.first_name;
            last_name = target.last_name;
            mailaddress = target.mailaddress;
            affiliation = target.affiliation;
            job_title = target.job_title;
            source_sid = source.sid;
            target_sid = target.sid;
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
