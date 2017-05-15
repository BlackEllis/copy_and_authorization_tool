using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
                string log_dir = json_module.get_external_resource("log_file_dir", constant.LOG_FILE_DIR);
                string export_dir = json_module.get_external_resource("export_dir", constant.EXPORT_DIR);
                string resources_dir = utility_tools.get_value_from_hasharray(hash_array, "RESOURCES_DIR", constant.RESOURCES_DIR);

                Console.WriteLine("setup log module : start");
#if DEBUG
                setup_logs(hash_array, log_dir, true); // ログ、エラーファイルのセットアップ
#else
                setup_logs(hash_array, log_dir, false);
#endif
                Console.WriteLine("setup log module : end");

                comparison_table compari_table = default(comparison_table);

                var data_table = get_excel_data(resources_dir);
                if (data_table != null)
                {
                    Console.WriteLine("storing AD User Infos : start");
                    compari_table = create_comparison_data(data_table);
                    Console.WriteLine("storing AD User Infos : End");
                }

                string export_filename = json_module.get_external_resource("export_xml_filename");
                if (export_filename == "") throw new Exception("出力ファイル名が未定義");

                // XMLへシリアライズ変換し出力
                Console.WriteLine("Export AD User to Serialize : start");
                export_serialize(compari_table, export_dir, json_module.get_external_resource("export_xml_filename", constant.EXPORT_XML_FILENAME));
                Console.WriteLine("Export AD User to Serialize : End");

                loger_manager.close();
            }
            catch (Exception e)
            {
                loger_manager.write_log(e.Message, "error");
            }
            Console.WriteLine("press any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// ログ、エラーファイルのセットアップ関数
        /// </summary>
        /// <param name="args">起動引数を連想配列にしたもの</param>
        /// <param name="log_dir"></param>
        private static void setup_logs(Dictionary<string, string>args, string log_dir, bool debug_flg)
        {

            // ログファイルの関係設定
            string log_file = json_module.get_external_resource("default_log_filename", constant.DEFAULT_LOG_FILENAME);
            log_file = utility_tools.get_value_from_hasharray(args, constant.RESOURCES_KEY_LOG, log_file);
            string log_encode = json_module.get_external_resource("log_file_encode", constant.LOG_FILE_ENCODE);

            // 抽出ログファイルの関係設定
            string extracting_file = json_module.get_external_resource("default_extracting_filename", constant.DEFAULT_EXTRACTING_FILENAME);
            extracting_file = utility_tools.get_value_from_hasharray(args, constant.RESOURCES_KEY_EXTRACTINGLOG, extracting_file);
            string extracting_encode = json_module.get_external_resource("error_file_encode", constant.EXTRACTING_FILE_ENCODE);

            string max_output_capacity = json_module.get_external_resource("", "0");

            loger_manager.setup_manager();
            loger_manager.add_stream("info", log_file, log_dir, log_encode, loger_module.E_LOG_LEVEL.E_ALL,
                                        json_module.get_external_resource("default_log_filesize", "0"), debug_flg);
            loger_manager.add_stream("extracting", extracting_file, log_dir, extracting_encode, loger_module.E_LOG_LEVEL.E_ALL,
                                        json_module.get_external_resource("default_extracting_filesize", "0"), debug_flg);
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

            string ad_server_name = json_module.get_external_resource("src_ad_server");
            string ad_access_userid = json_module.get_external_resource("src_access_userid");
            string ad_access_userpw = json_module.get_external_resource("src_access_passwd");
            string ad_common_names = json_module.get_external_resource("src_common_names");
            string ad_organizational_units = json_module.get_external_resource("src_organizational_units");

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
                string db_host = json_module.get_external_resource("db_server_name");
                string db_name = json_module.get_external_resource("db_name");
                string db_user = json_module.get_external_resource("db_userid");
                string db_pass = json_module.get_external_resource("db_passwd");
                mysql_module mysql_connecer = mysql_module.setup_sql(db_host, db_name, db_user, db_pass);

                return mysql_connecer;
            }
            catch (Exception e)
            {
                loger_manager.write_log(e.Message, "error");
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
                string resource_excel_fail = json_module.get_external_resource("resource_excel_fail");
                string open_sheet_name = json_module.get_external_resource("open_sheet_name");
                string open_list_offset = json_module.get_external_resource("open_list_offset");

                return excel_converter_module.read_excel_by_row(resource_excel_fail, resources_dir, open_sheet_name, int.Parse(open_list_offset));
            }
            catch (Exception e)
            {
                loger_manager.write_log(e.Message, "error");
                return null;
            }
        }

        /// <summary>
        /// シリアライズ元データ作成関数
        /// </summary>
        /// <returns></returns>
        private static comparison_table create_comparison_data(DataTable group_list)
        {
            Func<string, string, string> transform_str_to_cug = (string src_str, string account_name_filter) =>
            {
                string trim_str = src_str.Trim(); // 前後の空白を排除
                if (trim_str.Equals("")) return "";

                int all_appear_position = account_name_filter.LastIndexOf('*'); // フィルターでしてした＊の出現位置取得
                if (all_appear_position == -1) return "";

                string group_header = account_name_filter.Substring(0, all_appear_position);
                return group_header + trim_str;
            };

            comparison_table compari_table = new comparison_table();

            active_direcory_module ad_obj = get_ad_object(); // 移動う元ADからグループ情報を取得
            mysql_module mysql = create_mysql_module();
            Dictionary<string, group_info> group_infos = get_group_infos_from_destination_ad(); // 移動先ADから取得したグループ情報の取得
            string ad_account_name_filter = json_module.get_external_resource("dst_account_name_filter");
            string destination_domain = json_module.get_external_resource("destination_domain");
            if (!destination_domain.Equals("")) destination_domain += "\\";

            if ((group_infos == null) || (group_infos.Count == 0))
            {
                loger_manager.write_log("ADからの取得グループ情報がありません", "warning");
                return null;
            }

            foreach (KeyValuePair<string, group_info> ad_group_membars in ad_obj.groups_list) // 移動元ADから取得したグループ情報を元にループ
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
                            string group_name = transform_str_to_cug(result_row["CUGコード"].ToString(), ad_account_name_filter);
                            comparsion_unit unit = new comparsion_unit();
                            unit.account_name = ad_group_membars.Value.account_name;
                            unit.conversion_original = ad_group_membars.Value.sid;

                            if (!group_infos.ContainsKey(group_name)) // CUGコードがリソースに設定されてなければ、移行対象外とする
                            {
                                unit.after_conversion = "";
                                unit.del_flg = 1;
                            }
                            else
                                unit.after_conversion = destination_domain + group_name;

                            if (!compari_table.comparsion_units.Contains(unit))
                                compari_table.comparsion_units.Add(unit);
                        }
                        catch (Exception e)
                        {
                            loger_manager.write_log(e.Message, "error");
                        }
                    }
                }
                else
                {
                    loger_manager.write_log($"該当するグループ名がありません グループ名: {ad_group_membars.Key}", "extracting", "extracting");
                }

                if (ad_group_membars.Value.group_members.Count == 0) continue;

                // ユーザー毎に追加処理
                foreach (user_info membar_user in ad_group_membars.Value.group_members)
                {
                    try
                    {
                        var users = user_info.get_user_infos(mysql.connection_sql_str(), membar_user.account_name);
                        if ((users == null) || (users.Count == 0)) continue;

                        comparsion_unit unit = new comparsion_unit(membar_user, users[0], membar_user.sid, destination_domain + users[0].account_name);
                        if (!compari_table.comparsion_units.Contains(unit))
                            compari_table.comparsion_units.Add(unit);
                    }
                    catch (Exception e)
                    {
                            loger_manager.write_log(e.Message, "error");
                    }
                }
            }

            compari_table.list_sort(); // 全て格納後、ソートを行う
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
                loger_manager.write_log(e.Message, "error");
            }

        }

        /// <summary>
        /// ADからグループ一覧の取得
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, group_info> get_group_infos_from_destination_ad()
        {
            Func<string, char, string[]> isolat_from_str = (string src_str, char delimiter) =>
            {
                string pad_str = src_str.Replace(" ", "");
                string[] dst_strs = src_str.Split(delimiter);
                return dst_strs;
            };

            string ad_server_name = json_module.get_external_resource("dst_ad_server");
            string ad_access_userid = json_module.get_external_resource("dst_access_userid");
            string ad_access_userpw = json_module.get_external_resource("dst_access_passwd");
            string ad_common_names = json_module.get_external_resource("dst_common_names");
            string ad_organizational_units = json_module.get_external_resource("dst_organizational_units");
            string ad_account_name_filter = json_module.get_external_resource("dst_account_name_filter");

            return active_direcory_module.get_group_infos(ad_server_name,
                                                        isolat_from_str(ad_common_names, ','),
                                                        isolat_from_str(ad_organizational_units, '\''),
                                                        ad_account_name_filter, ad_access_userid, ad_access_userpw);
        }
    }
}
