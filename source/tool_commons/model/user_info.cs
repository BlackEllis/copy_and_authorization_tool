using tool_commons.modules;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.Principal;

namespace tool_commons.model
{
    public class user_info : ad_unit_base, IEquatable<user_info>
    {
        public string first_name { get; }
        public string last_name { get; }
        public string mailaddress { get; }
        public string affiliation { get; }
        public string job_title { get; }
        public int del_flg { get; }

        /// <summary>
        /// コンストラクタでADから取得した情報を格納する
        /// </summary>
        /// <param name="set_obj">ADから取得したユーザーEntryオブジェクト</param>
        public user_info(DirectoryEntry set_obj)
        {
            Func<PropertyCollection, string, Object> check_parameter_to_str = (PropertyCollection in_array, string get_obj_name) =>
            {
                if (in_array.Contains(get_obj_name))
                {
                    var value = in_array[get_obj_name].Value;
                    return value != null ? value : "";
                }
                else
                    return "";
            };

            try
            {
                account_name = check_parameter_to_str(set_obj.Properties, "sAMAccountName").ToString();
                first_name = check_parameter_to_str(set_obj.Properties, "extensionAttribute2").ToString();
                last_name = check_parameter_to_str(set_obj.Properties, "extensionAttribute3").ToString();
                mailaddress = check_parameter_to_str(set_obj.Properties, "mail").ToString();
                affiliation = check_parameter_to_str(set_obj.Properties, "extensionAttribute6").ToString();
                job_title = check_parameter_to_str(set_obj.Properties, "title").ToString();
                sid = new SecurityIdentifier((byte[])set_obj.Properties["objectSid"][0], 0).ToString();

                if (!set_obj.Properties.Contains("userAccountControl")) del_flg = 0;
                else
                {
                    int val = (int)set_obj.Properties["userAccountControl"].Value;
                    if ((val & (int)ADS_UF.ACCOUNTDISABLE) == (int)ADS_UF.ACCOUNTDISABLE)   // 0x0002が立っている場合は無効
                        del_flg = 1; // 無効
                    else
                        del_flg = 0; // 有効
                }
            }
            catch (Exception e)
            {
                loger_manager.write_log(e.Message, "error");
            }
        }

        /// <summary>
        /// コンストラクタで内部変数にDBから取得した情報を格納する
        /// </summary>
        /// <param name="src">DBから取得したユーザー情報MySqlオブジェクト</param>
        public user_info(MySqlDataReader src)
        {
            try
            {
                account_name = src["account_name"].ToString();
                first_name = src["first_name"].ToString();
                last_name = src["last_name"].ToString();
                mailaddress = src["mail_address"].ToString();
                affiliation = src["affiliation"].ToString();
                job_title = src["job_title"].ToString();
                sid = src["s_id"].ToString();
                del_flg = int.Parse(src["del_flg"].ToString());
            }
            catch (Exception e)
            {
                loger_manager.write_log(e.Message, "error");
            }
        }

        /// <summary>
        /// 内部変数内容をログに出力関数
        /// </summary>
        /// <param name="indent"></param>
        public void disp_parameters(string indent="\t")
        {
            Func<string, bool> write_log = (string str) =>
            {
                loger_manager.write_log(str, "debug");
                return true;
            };

            write_log(indent + "account_name : " + account_name);
            write_log(indent + "first_name : " + first_name);
            write_log(indent + "last_name : " + last_name);
            write_log(indent + "mailaddress : " + mailaddress);
            write_log(indent + "affiliation : " + affiliation);
            write_log(indent + "job_title : " + job_title);
            write_log(indent + "sid : " + sid);
            write_log(indent + "del_flg : " + del_flg);
        }

        public string get_full_name()
        {
            return first_name + " " + last_name;
        }

        /// <summary>
        /// DBからユーザー情報を取得
        /// </summary>
        /// <param name="connection_sql">DB接続情報文字列</param>
        /// <param name="account_name">アカウントID</param>
        /// <param name="del_flg">無効フラグ 1:無効アカウント　0:有効アカウント</param>
        /// <returns></returns>
        public static List<user_info> get_user_infos(string connection_sql, string account_name, int del_flg=0)
        {
            try
            {
                using (var mysql_cone = new MySqlConnection(connection_sql))
                using (var mysql_commnd = mysql_cone.CreateCommand())
                {
                    mysql_cone.Open(); // データベースの接続開始

                    mysql_commnd.CommandText = @"SELECT * FROM user_info
                                                WHERE account_name = @ACCOUNT_NAME
                                                AND del_flg = @DEL_FLG"; // 実行SQL
                    mysql_commnd.Parameters.AddWithValue("@ACCOUNT_NAME", account_name); // バインド変数の割当
                    mysql_commnd.Parameters.AddWithValue("@DEL_FLG", del_flg);
                    var reader = mysql_commnd.ExecuteReader(); // クエリーの実行

                    List<user_info> dst_list = new List<user_info>();
                    while (reader.Read())
                    {
                        user_info obj = new user_info(reader);
                        if (obj.account_name.Length > 0)
                            dst_list.Add(obj);
                    }

                    return dst_list;
                }


            }
            catch (Exception e)
            {
                loger_manager.write_log(e.Message, "error");
                return null;
            }
        }

        /// <summary>
        /// 同じ内容可比較
        /// </summary>
        /// <param name="comparison_source"></param>
        /// <returns>同一であれば true 異なれば false</returns>
        public bool Equals(user_info comparison_source)
        {
            if (comparison_source == null) return false;
            return (this.account_name == comparison_source.account_name) && (this.sid == comparison_source.sid);
        }
    }
}
