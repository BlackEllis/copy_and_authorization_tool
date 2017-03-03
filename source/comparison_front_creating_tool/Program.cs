using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using tool_commons.model;
using tool_commons.modules;

namespace comparison_front_creating_tool
{
    class Program
    {
        static void Main(string[] args)
        {
            // 起動引数からパラメータ毎に切り分けた連想配列を生成
            Dictionary<string, string> hash_array = utility_tools.argument_decomposition(args);

            try
            {
                // json設定ファイルの読み取り
                Console.WriteLine("read setting from json : start");
                string json_file = "";
                if (hash_array.ContainsKey(constant.RESOURCES_KEY_EXTERNAL))
                    json_file = hash_array[constant.RESOURCES_KEY_EXTERNAL];
                else
                    json_file = constant.RESOURCES_DIR + constant.EXTERNAL_RESOURCE_FILENAME;
                json_module.setup(json_file);
                Console.WriteLine("read setting from json : end");

                // 各外部ファイルのディレクトリ先を設定
                string log_dir = get_value_from_json("log_file_dir", constant.LOG_FILE_DIR);
                string export_dir = get_value_from_json("export_dir", constant.EXPORT_DIR);
                string resources_dir = get_value_from_hasharray(hash_array, "RESOURCES_DIR", constant.RESOURCES_DIR);

                Console.WriteLine("setup log module : start");
                setup_logs(hash_array, log_dir); // ログ、エラーファイルのセットアップ
                Console.WriteLine("setup log module : end");

                comparison_table compari_table = default(comparison_table);

                var data_table = get_excel_data(resources_dir);
                if (data_table != null)
                {
                    Console.WriteLine("storing AD User Infos : start");
                    compari_table = create_comparison_data(data_table);
                    Console.WriteLine("storing AD User Infos : End");
                }

                string export_filename = get_value_from_json("export_xml_filename");
                if (export_filename == "") throw new Exception("出力ファイル名が未定義");

                // XMLへシリアライズ変換し出力
                Console.WriteLine("Export AD User to Serialize : start");
                export_serialize(compari_table, export_dir, get_value_from_json("export_xml_filename", constant.EXPORT_XML_FILENAME));
                Console.WriteLine("Export AD User to Serialize : End");

                loger_module.close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);

            }
            Console.WriteLine("press any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// 引数の連想配列値か引数の基本値から値を取り出す
        /// </summary>
        /// <param name="src_array">取り出す連想配列</param>
        /// <param name="key">連想配列のキー情報</param>
        /// <param name="default_value">基本値</param>
        /// <returns></returns>
        private static string get_value_from_hasharray(Dictionary<string, string> src_array, string key, string default_value)
        {
            if (src_array == null) return default_value;
            if (src_array.ContainsKey(key))
                return src_array[key];
            else
                return default_value;
        }

        /// <summary>
        /// json_moduleから値を取り出す
        /// </summary>
        /// <param name="json_key">取出したいjsonキー情報</param>
        /// <param name="default_value">基本値</param>
        /// <returns></returns>
        private static string get_value_from_json(string json_key, string default_value = "")
        {
            string dst_str = json_module.get_external_resource(json_key);
            if (dst_str == "") dst_str = default_value;

            return dst_str;
        }

        /// <summary>
        /// ログ、エラーファイルのセットアップ関数
        /// </summary>
        /// <param name="args">起動引数を連想配列にしたもの</param>
        /// <param name="log_dir"></param>
        private static void setup_logs(Dictionary<string, string>args, string log_dir)
        {

            // ログファイルの関係設定
            string log_file = get_value_from_json("default_log_filename", constant.DEFAULT_LOG_FILENAME);
            log_file = get_value_from_hasharray(args, constant.RESOURCES_KEY_LOG, log_file);
            string log_encode = get_value_from_json("log_file_encode", constant.LOG_FILE_ENCODE);

            // エラーファイルの関係設定
            string error_file = get_value_from_json("default_error_filename", constant.DEFAULT_ERROR_FILENAME);
            error_file = get_value_from_hasharray(args, constant.RESOURCES_KEY_ERRORLOG, error_file);
            string error_encode = get_value_from_json("error_file_encode", constant.ERROR_FILE_ENCODE);

#if DEBUG
            loger_module.loger_setup(log_file, log_dir, log_encode, "info", true);
            loger_module.loger_setup(error_file, log_dir, error_encode, "error", true);
#else
            loger_module.loger_setup(log_file, log_dir, log_encode, "info");
            loger_module.loger_setup(error_file, log_dir, error_encode, "error");
#endif
        }

        /// <summary>
        /// 設定引数を元にADモジュールオブジェクト生成
        /// </summary>
        /// <returns>ADモジュールオブジェクト</returns>
        private static active_direcory_module get_ad_object()
        {
            Func<string, char, string[]> isolat_from_str = (string src_str, char delimiter) =>
            {
                string pad_str = src_str.Replace(" ", "");
                string[] dst_strs = src_str.Split(delimiter);
                return dst_strs;
            };

            // ADからグループ
            var ad_obj = new active_direcory_module();

            string ad_server_name = get_value_from_json("ad_server");
            string ad_access_userid = get_value_from_json("access_userid");
            string ad_access_userpw = get_value_from_json("access_passwd");
            string ad_common_names = get_value_from_json("common_names");
            string ad_organizational_units = get_value_from_json("organizational_units");

            ad_obj.get_group_list(ad_server_name, isolat_from_str(ad_common_names, ','), isolat_from_str(ad_organizational_units, '\''), ad_access_userid, ad_access_userpw);

            return ad_obj;
        }

        /// <summary>
        /// 設定変数からSQL接続用モジュールの生成
        /// </summary>
        /// <returns>SQL接続用モジュールオブジェクト</returns>
        private static mysql_module create_mysql_module()
        {
            try
            {
                string db_host = get_value_from_json("db_server_name");
                string db_name = get_value_from_json("db_name");
                string db_user = get_value_from_json("db_userid");
                string db_pass = get_value_from_json("db_passwd");
                mysql_module mysql_connecer = mysql_module.setup_sql(db_host, db_name, db_user, db_pass);

                return mysql_connecer;
            }
            catch (Exception e)
            {
                loger_module.write_log(e.Message, "error", "info");
                return null;
            }
        }

        /// <summary>
        /// Excelファイルの読み取り
        /// </summary>
        /// <param name="resources_dir">リソースディレクトリ</param>
        /// <returns></returns>
        private static DataTable get_excel_data(string resources_dir)
        {
            try
            {
                string resource_excel_fail = get_value_from_json("resource_excel_fail");
                string open_sheet_name = get_value_from_json("open_sheet_name");
                string open_list_offset = get_value_from_json("open_list_offset");

                return excel_converter_module.read_excel_by_row(resource_excel_fail, resources_dir, open_sheet_name, int.Parse(open_list_offset));
            }
            catch (Exception e)
            {
                loger_module.write_log(e.Message, "error", "info");
                return null;
            }
        }

        /// <summary>
        /// シリアライズ元データ作成関数
        /// </summary>
        /// <returns></returns>
        private static comparison_table create_comparison_data(DataTable group_list)
        {
            comparison_table compari_table = new comparison_table();

            var ad_obj = get_ad_object();
            var mysql = create_mysql_module();

           foreach (KeyValuePair<string, group_info> ad_group_membars in ad_obj.groups_list)
            {
                // グループ情報の追加
                string query_str = $"Name = '{ad_group_membars.Key}'";
                DataRow[] result_rows = group_list.Select(query_str);
                if (result_rows.Length > 0)
                {
                    foreach (var result_row in result_rows)
                    {
                        try
                        {
                            comparsion_unit unit = new comparsion_unit();
                            unit.account_name = result_row["SamAccountName"].ToString();
                            unit.source_sid = ad_group_membars.Value.sid;
                            unit.target_sid = result_row["SID"].ToString();

                            if (!compari_table.comparsion_units.Contains(unit))
                                compari_table.comparsion_units.Add(unit);
                        }
                        catch (Exception e)
                        {
                            loger_module.write_log(e.Message, "error", "ifno");
                        }
                    }
                }

                if (ad_group_membars.Value.group_members.Count == 0) continue;

                // ユーザー毎に追加処理
                foreach (user_info membar_user in ad_group_membars.Value.group_members)
                {
                    try
                    {
                        var users = user_info.get_user_infos(mysql.connection_sql_str(), membar_user.account_name);
                        if ((users == null) || (users.Count == 0)) continue;

                        comparsion_unit unit = new comparsion_unit(membar_user, users[0]);
                        if (!compari_table.comparsion_units.Contains(unit))
                            compari_table.comparsion_units.Add(unit);
                    }
                    catch (Exception e)
                    {
                        loger_module.write_log(e.Message, "error", "ifno");
                    }
                }
            }

            return compari_table;
        }

        /// <summary>
        /// XMLファイルへシリアライズ変換し出力関数
        /// </summary>
        /// <param name="com_table">シリアライズ元データ</param>
        /// <param name="export_dir">出力ディレクトリ</param>
        /// <param name="export_filename">出力ファイル名</param>
        private static void export_serialize(comparison_table com_table, string export_dir, string export_filename)
        {
            try
            {
                if (com_table == null) throw new Exception("comparison_table 引数が未定義");
                if (!Directory.Exists(export_dir)) Directory.CreateDirectory(export_dir); // ディレクトが無ければ作成

                string output_filename = export_dir + export_filename;
                using (StreamWriter write_stream = new StreamWriter(output_filename, false, Encoding.GetEncoding(constant.EXTERNAL_RESOURCE_ENCODE)))
                {
                    // 名前空間の抑制
                    System.Xml.Serialization.XmlSerializerNamespaces serialize_ns = new System.Xml.Serialization.XmlSerializerNamespaces();
                    serialize_ns.Add(string.Empty, string.Empty);

                    // シリアライズ処理
                    System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(comparison_table));
                    serializer.Serialize(write_stream, com_table, serialize_ns);

                    write_stream.Flush();
                }
            }
            catch (Exception e)
            {
                loger_module.write_log(e.Message, "error", "ifno");
            }

        }
    }
}
