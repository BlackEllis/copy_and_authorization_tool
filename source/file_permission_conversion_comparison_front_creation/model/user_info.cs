using file_permission_conversion_comparison_front_creation.modules;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.Principal;

namespace file_permission_conversion_comparison_front_creation.model
{
    class user_info
    {
        // Active Directoryのアカウント状態フラグ
        private const int ADS_UF_SCRIPT = 1;                                         // 0x1
        private const int ADS_UF_ACCOUNTDISABLE = 2;                                 // 0x2
        private const int ADS_UF_HOMEDIR_REQUIRED = 8;                               // 0x8
        private const int ADS_UF_LOCKOUT = 16;                                       // 0x10
        private const int ADS_UF_PASSWD_NOTREQD = 32;                                // 0x20
        private const int ADS_UF_PASSWD_CANT_CHANGE = 64;                            // 0x40
        private const int ADS_UF_ENCRYPTED_TEXT_PASSWORD_ALLOWED = 128;              // 0x80
        private const int ADS_UF_TEMP_DUPLICATE_ACCOUNT = 256;                       // 0x100
        private const int ADS_UF_NORMAL_ACCOUNT = 512;                               // 0x200
        private const int ADS_UF_INTERDOMAIN_TRUST_ACCOUNT = 2048;                   // 0x800
        private const int ADS_UF_WORKSTATION_TRUST_ACCOUNT = 4096;                   // 0x1000
        private const int ADS_UF_SERVER_TRUST_ACCOUNT = 8192;                        // 0x2000
        private const int ADS_UF_DONT_EXPIRE_PASSWD = 65536;                         // 0x10000
        private const int ADS_UF_MNS_LOGON_ACCOUNT = 131072;                         // 0x20000
        private const int ADS_UF_SMARTCARD_REQUIRED = 262144;                        // 0x40000
        private const int ADS_UF_TRUSTED_FOR_DELEGATION = 524288;                    // 0x80000
        private const int ADS_UF_NOT_DELEGATED = 1048576;                            // 0x100000
        private const int ADS_UF_USE_DES_KEY_ONLY = 2097152;                         // 0x200000
        private const int ADS_UF_DONT_REQUIRE_PREAUTH = 4194304;                     // 0x400000
        private const int ADS_UF_PASSWORD_EXPIRED = 8388608;                         // 0x800000
        private const int ADS_UF_TRUSTED_TO_AUTHENTICATE_FOR_DELEGATION = 16777216;  // 0x1000000

        public string account_name { get; }
        public string first_name { get; }
        public string last_name { get; }
        public string mailaddress { get; }
        public string affiliation { get; }
        public string job_title { get; }
        public string sid { get; }
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
                    if ((val & ADS_UF_ACCOUNTDISABLE) == ADS_UF_ACCOUNTDISABLE)   // 0x0002が立っている場合は無効
                        del_flg = 1; // 無効
                    else
                        del_flg = 0; // 有効
                }
            }
            catch (Exception e)
            {
                loger_module.write_log(e.Message, "error", "info");
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
                account_name = src["g_code"].ToString();
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
                loger_module.write_log(e.Message, "error", "info");
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
                loger_module.write_log(str, "debug", "info");
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
        /// <param name="g_code">グローバルID</param>
        /// <param name="del_flg">無効フラグ 1:無効アカウント　0:有効アカウント</param>
        /// <returns></returns>
        public static List<user_info> get_user_infos(string connection_sql, string g_code, int del_flg=0)
        {
            try
            {
                using (var mysql_cone = new MySqlConnection(connection_sql))
                using (var mysql_commnd = mysql_cone.CreateCommand())
                {
                    mysql_cone.Open(); // データベースの接続開始

                    mysql_commnd.CommandText = @"SELECT * FROM user_info
                                                WHERE g_code = @G_CODE
                                                AND del_flg = @DEL_FLG"; // 実行SQL
                    mysql_commnd.Parameters.AddWithValue("@G_CODE", g_code); // バインド変数の割当
                    mysql_commnd.Parameters.AddWithValue("@DEL_FLG", del_flg);
                    var reader = mysql_commnd.ExecuteReader(); // クエリーの事項

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
                loger_module.write_log(e.Message, "error", "info");
                return null;
            }
        }
    }
}
