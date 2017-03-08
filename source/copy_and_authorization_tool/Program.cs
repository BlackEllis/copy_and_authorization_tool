using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using tool_commons.model;
using tool_commons.modules;

namespace copy_and_authorization_tool
{
    class Program
    {
        static void Main(string[] args)
        {
            // 起動引数からパラメータ毎に切り分けた連想配列を生成
            Dictionary<string, string> hash_array = utility_tools.argument_decomposition(args);

            try
            {
                Console.WriteLine("read setting from json : start");
                json_module.setup(
                    get_value_from_hasharray(hash_array,
                                            constant.RESOURCES_KEY_EXTERNAL,
                                            constant.RESOURCES_DIR + constant.EXTERNAL_RESOURCE_FILENAME)
                );
                Console.WriteLine("read setting from json : end");

                // 各外部ファイルのディレクトリ先を設定
                string log_dir = get_value_from_json("log_file_dir", constant.LOG_FILE_DIR);
                string export_dir = get_value_from_json("export_dir", constant.EXPORT_DIR);
                string resources_dir = get_value_from_hasharray(hash_array, "RESOURCES_DIR", constant.RESOURCES_DIR);

                Console.WriteLine("log feature setup : start");
                setup_logs(hash_array, log_dir); // ログ、エラーファイルのセットアップ
                Console.WriteLine("log feature setup : end");

                // デシリアライズした結果を連想配列に格納
                Console.WriteLine("deserialize to Dictionary : start");
                Dictionary<string, comparsion_unit> comparison_list = inport_serialize(get_value_from_json("inport_xml_filename"), resources_dir)?.transform_to_dictionary();
                Console.WriteLine("deserialize to Dictionary : end");

                string resouces_excel_file = get_value_from_json("copy_dir_comparison_file");
                string copy_dir_info_sheet = get_value_from_json("copy_dir_info_sheet");
                int copy_dir_info_sheet_offset = int.Parse(get_value_from_json("copy_dir_info_sheet_offset"));
                DataTable copy_info_table = excel_converter_module.read_excel_by_row(resouces_excel_file, resources_dir, copy_dir_info_sheet, copy_dir_info_sheet_offset);
                string exception_copy_dir_sheet = get_value_from_json("exception_copy_dir_sheet");
                int exception_copy_dir_sheet_offset = int.Parse(get_value_from_json("exception_copy_dir_sheet_offset"));
                DataTable exception_copy_table = excel_converter_module.read_excel_by_row(resouces_excel_file, resources_dir, exception_copy_dir_sheet, exception_copy_dir_sheet_offset);

                List<string> exception_list = new List<string>();
                foreach (DataRow row in exception_copy_table.Rows)
                {
                    string exception_copy_dir = row["対象外ディレクトリ名"].ToString();
                    if (!exception_copy_dir.Equals(""))
                        exception_list.Add(exception_copy_dir);
                }

                // robocopyをテストモードで動作させるか判定用パラメータ取得
                bool diff_mode = bool.Parse(get_value_from_hasharray(hash_array, constant.RESOURCES_KEY_DIFF_MODE, "False"));

                // ロボコピー実行関数
                foreach (DataRow row in copy_info_table.Rows)
                {
                    string src_dir = row["コピー元ディレクトリ"].ToString();
                    string dst_dir = row["コピー先ディレクトリ"].ToString();
                    string user_id = row["コピー先アクセスID"].ToString();
                    string user_pw = row["コピー先アクセスPW"].ToString();

                    Console.WriteLine("run robocopy process : start");
                    communication_with_external_server(src_dir, dst_dir, user_id, user_pw, true);
                    robocopy_process(src_dir, dst_dir, exception_list, row["コピーフィルター"].ToString(), diff_mode);
                    communication_with_external_server(src_dir, dst_dir, user_id, user_pw, false);
                    Console.WriteLine("run robocopy process : end");

                    if (diff_mode) continue;

                    Console.WriteLine("run conversion　association process : start");
                    conversion_association(src_dir, dst_dir, ref comparison_list, ref exception_list);
                    Console.WriteLine("run conversion　association process : end");
                }

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
        /// XMLデシリアライズ関数
        /// </summary>
        /// <param name="src_file">読み取りファイル</param>
        /// <param name="src_dir">読み取り先</param>
        /// <returns></returns>
        private static comparison_table inport_serialize(string src_file, string src_dir)
        {
            try
            {
                using (StreamReader read_stream = new StreamReader(src_dir + src_file, Encoding.GetEncoding(constant.EXTERNAL_RESOURCE_ENCODE)))
                {
                    System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(comparison_table));
                    comparison_table dst_table = (comparison_table)serializer.Deserialize(read_stream);
                    return dst_table;
                }
            }
            catch (Exception e)
            {
                loger_module.write_log(e.Message, "error", "info");
                return null;
            }
        }

        /// <summary>
        /// 外部サーバーのネットワークドライブ接続、切断関数
        /// </summary>
        /// <param name="server_name">接続先ネットワークドライブ</param>
        /// <param name="user_id">アクセスID</param>
        /// <param name="user_pw">アクセスPW</param>
        /// <param name="connection_mode">true: 接続　false: 切断</param>
        private static void communication_with_external_server(string src_dir, string dst_dir, string user_id, string user_pw, bool connection_mode=true)
        {
            // コピー元、コピー先何れかが共有フォルダであれば処理を行う
            // 両方共有フォルダだった場合は処理を行わない
            if (src_dir.StartsWith("\\\\") == dst_dir.StartsWith("\\\\")) return ;
            if (user_id.Equals("") || user_pw.Equals("")) return ;

            string target_name = src_dir.StartsWith("\\\\") ? src_dir : dst_dir;

            int cat_point = target_name.LastIndexOf("$");
            if (cat_point == -1) return; // 共有フォルダの＄が無ければ処理を打ち切る

            string connection_name = target_name.Substring(0, cat_point); // PC名＋共有フォルダまでの文字列の切り出し
            using (Process p = new Process())
            {
                if (connection_mode)
                    p.StartInfo.Arguments = string.Format("/C NET USE \"{0}\" \"{1}\" /user:\"{2}\"" , connection_name, user_pw, user_id);
                else
                    p.StartInfo.Arguments = string.Format("/C NET USE \"{0}\" /delete", connection_name);
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.Start();
                loger_module.write_log(p.StartInfo.Arguments);
                loger_module.write_log(p.StandardOutput.ReadToEnd());
                loger_module.write_log(p.StandardError.ReadToEnd(), "error", "info");
                p.WaitForExit();
            }
        }

        /// <summary>
        /// ロボコピー実行関数
        /// </summary>
        /// <param name="src_dir">コピー元ディレクト</param>
        /// <param name="dst_dir">コピー先ディレクトリ</param>
        /// <param name="copy_filter">コピーフィルター</param>
        /// <param name="exception_folder_list">対象外ディレクトリ一覧</param>
        /// <param name="copy_filter"></param>
        /// <param name="diff_mode"></param>
        private static void robocopy_process(string src_dir, string dst_dir, List<string> exception_folder_list=null, string copy_filter = "", bool diff_mode = false)
        {
            try
            {
                if (src_dir.Equals("") || dst_dir.Equals("")) return;
                if (copy_filter.Equals("")) copy_filter = "*.*";

                // コピー対象外ディレクトリ設定
                string exception_dir_str = "";
                foreach(string folder_name in exception_folder_list)
                {
                    if (exception_dir_str.Equals("")) exception_dir_str = " /XD";
                    exception_dir_str += $" {folder_name}";
                }

                using (Process p = new Process())
                {
                    if (diff_mode)
                        p.StartInfo.Arguments = string.Format("/C ROBOCOPY \"{0}\" \"{1}\" \"{2}\" /L" + get_value_from_json("robocopy_option") + exception_dir_str, src_dir, dst_dir, copy_filter);
                    else
                        p.StartInfo.Arguments = string.Format("/C ROBOCOPY \"{0}\" \"{1}\" \"{2}\"" + get_value_from_json("robocopy_option") + exception_dir_str, src_dir, dst_dir, copy_filter);
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.Start();
                    loger_module.write_log(p.StartInfo.Arguments);
                    loger_module.write_log(p.StandardOutput.ReadToEnd());
                    loger_module.write_log(p.StandardError.ReadToEnd(), "error", "info");
                    p.WaitForExit();
                }
            }
            catch (Exception e)
            {
                loger_module.write_log(e.Message, "error", "info");
            }
        }

        /// <summary>
        /// 権限移管関数
        /// </summary>
        /// <param name="src_dir">移管元開始ディレクトリフルパス</param>
        /// <param name="dst_dir">移管先開始ディレクトリフルパス</param>
        /// <param name="comparison_list">変換対象リスト</param>
        /// <returns></returns>
        private static bool conversion_association(string src_dir, string dst_dir, ref Dictionary<string, comparsion_unit> comparison_list, ref List<string> exception_list)
        {
            try
            {
                DirectoryInfo src_dir_info = new DirectoryInfo(src_dir);
                DirectoryInfo dst_dir_info = new DirectoryInfo(dst_dir);

                // 例外フォルダ名が含まれているか 例外対象であれば処理を打ち切る
                string foldername = src_dir_info.Name;
                foreach (string exception_foldername in exception_list)
                {
                    if (!foldername.Equals(exception_foldername)) continue;

                    loger_module.write_log($"例外対象: {src_dir_info.FullName}");
                    return true;
                }

                // フォルダの権限移管
                if (!directory_authority_replacement(src_dir_info, dst_dir_info, ref comparison_list))
                    throw new Exception("ディレクトリアクセス権設定でエラー");

                // ファイルの権限移管
                FileInfo[] src_file_list = src_dir_info.GetFiles();
                FileInfo[] dst_file_list = dst_dir_info.GetFiles();
                foreach (var src_file in src_file_list)
                {
                    bool check_flg = false;
                    foreach (var dst_file in dst_file_list)
                    {
                        if (!dst_file.Name.Equals(src_file.Name)) continue;

                        check_flg = file_authority_replacement(src_file, dst_file, ref comparison_list);
                        break;
                    }
                    if (!check_flg) loger_module.write_log($"該当ファイルがコピーされていないか、アクセス権がありません。：{src_file}", "error");
                }

                // フォルダを掘り下げる
                DirectoryInfo[] src_inclusion_dirs = src_dir_info.GetDirectories();
                DirectoryInfo[] dst_inclusion_dirs = dst_dir_info.GetDirectories();
                foreach (var src_inclusion_dir in src_inclusion_dirs)
                {
                    foreach (var dst_inclusion_dir in dst_inclusion_dirs)
                    {
                        if (!dst_inclusion_dir.Name.Equals(src_inclusion_dir.Name)) continue;

                        conversion_association(src_inclusion_dir.FullName, dst_inclusion_dir.FullName, ref comparison_list, ref exception_list);
                        break;
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                loger_module.write_log(e.Message, "error", "info");
                return false;
            }
        }

        /// <summary>
        /// ディレクトリアクセス権の移管
        /// </summary>
        /// <param name="src_dir">移管元ディレクトリ</param>
        /// <param name="dst_dir">移管先ディレクトリ</param>
        /// <param name="comparison_list">変換対象一覧</param>
        /// <returns></returns>
        private static bool directory_authority_replacement(DirectoryInfo src_dir, DirectoryInfo dst_dir, ref Dictionary<string, comparsion_unit> comparison_list)
        {
            try
            {
                DirectorySecurity src_dir_security = src_dir.GetAccessControl();    // 移管元のアクセス権取得
                DirectorySecurity dst_dir_security = dst_dir.GetAccessControl();    // 移管先のアクセス権取得

                foreach (FileSystemAccessRule src_rules in src_dir_security.GetAccessRules(true, true, typeof(NTAccount)))
                {
                    if (comparison_list.ContainsKey(src_rules.IdentityReference.Value.ToString()))
                        dst_dir_security.AddAccessRule(new FileSystemAccessRule(comparison_list[src_rules.IdentityReference.Value.ToString()].target_sid,
                                                                                src_rules.FileSystemRights,
                                                                                src_rules.InheritanceFlags,
                                                                                src_rules.PropagationFlags,
                                                                                src_rules.AccessControlType));
                    else
                        dst_dir_security.AddAccessRule(src_rules);
                }
                if (dst_dir_security.GetAccessRules(true, true, typeof(NTAccount)).Count > 0)
                    Directory.SetAccessControl(dst_dir.FullName, dst_dir_security); // アクセス権の設定
                else
                    throw new Exception($"アクセス権の設定が無い : {src_dir.FullName}");

                return true;
            }
            catch (Exception e)
            {
                loger_module.write_log(e.Message, "error", "info");
                return false;
            }

        }

        /// <summary>
        /// ファイルアクセス権の移管
        /// </summary>
        /// <param name="src_file">移管元ファイル名</param>
        /// <param name="dst_file">移管先ファイル</param>
        /// <param name="comparison_list">変換対象一覧</param>
        /// <returns></returns>
        private static bool file_authority_replacement(FileInfo src_file, FileInfo dst_file, ref Dictionary<string, comparsion_unit> comparison_list)
        {
            try
            {
                FileSecurity src_file_security = src_file.GetAccessControl();   // 移管元のアクセス権取得
                FileSecurity dst_file_security = dst_file.GetAccessControl();    // 移管先のアクセス権取得
                foreach (FileSystemAccessRule src_rules in src_file_security.GetAccessRules(true, true, typeof(NTAccount)))
                {
                    loger_module.write_log($"{src_rules.IdentityReference.ToString()}");
                    loger_module.write_log("適応先：" + ((src_rules.InheritanceFlags & InheritanceFlags.ContainerInherit) > 0 ? "このフォルダとサブフォルダ" : "このフォルダのみ"));
                    if (comparison_list.ContainsKey(src_rules.IdentityReference.ToString()))
                    {
                        // 変換対象があれば、S-IDを変換して登録
                        dst_file_security.AddAccessRule(new FileSystemAccessRule(comparison_list[src_rules.IdentityReference.ToString()].target_sid,
                                                                                    src_rules.FileSystemRights,
                                                                                    src_rules.InheritanceFlags,
                                                                                    src_rules.PropagationFlags,
                                                                                    src_rules.AccessControlType));
                        loger_module.write_log($"変換対象アカウント：{src_rules.IdentityReference.ToString()} → {comparison_list[src_rules.IdentityReference.ToString()].target_sid} | {src_rules.FileSystemRights.ToString()}", "error");
                    }
                    else
                    {
                        loger_module.write_log("適応先：" + ((src_rules.InheritanceFlags & InheritanceFlags.ContainerInherit) > 0 ? "このフォルダとサブフォルダ" : "このフォルダのみ"), "error");
                        loger_module.write_log($"変換対象外アカウント：{src_rules.IdentityReference.ToString()} | {src_rules.FileSystemRights.ToString()}", "error");
                        dst_file_security.AddAccessRule(src_rules); // 変換対象が無ければ、移管元の権限そのまま移管
                    }
                }
                if (dst_file_security.GetAccessRules(true, true, typeof(NTAccount)).Count > 0)
                    File.SetAccessControl(dst_file.FullName, dst_file_security);
                else
                    throw new Exception($"アクセス権の設定が無い : {src_file.FullName}");

                return true;
            }
            catch (Exception e)
            {
                loger_module.write_log(e.Message, "error", "info");
                return false;
            }
        }

        /// <summary>
        /// ディレクトリに設定されているのアクセス権を出力
        /// </summary>
        /// <param name="src_dir">比較元ディレクトリフルパス</param>
        /// <param name="dst_dir">比較先ディレクトリフルパス</param>
        private static void output_access_permission(string src_dir, string dst_dir)
        {
            Func<FileSystemAccessRule, string> security_output = (FileSystemAccessRule fsar) =>
            {
                return fsar.AccessControlType
                        + "\t" + fsar.IdentityReference
                        + "\t" + fsar.FileSystemRights.ToString()
                        + "\t" + fsar.IsInherited.ToString()
                        + "\t" + fsar.InheritanceFlags.ToString()
                        + "\t" + fsar.PropagationFlags.ToString();
            };

            if (src_dir.Equals("") || dst_dir.Equals(""))
            {
                loger_module.write_log($"比較対象設定が不適切\t \t\tsrc_dir:{src_dir}\n\t\tdst_dir:{dst_dir}");
                return;
            }

            DirectorySecurity src_dir_security = Directory.GetAccessControl(src_dir);
            DirectorySecurity dst_dir_security = Directory.GetAccessControl(dst_dir);

            loger_module.write_log("AccessControlType\tAccountName\tFileSystemRights\tIsInherited"
+ "\tInheritanceFlags\tPropagationFlags");
            AuthorizationRuleCollection src_rules = src_dir_security.GetAccessRules(true, true, typeof(NTAccount));
            for (int i = 0; i < src_rules.Count; i++)
                loger_module.write_log(security_output((FileSystemAccessRule)src_rules[i]));

            loger_module.write_log("AccessControlType\tAccountName\tFileSystemRights\tIsInherited"
+ "\tInheritanceFlags\tPropagationFlags");
            AuthorizationRuleCollection dst_rules = dst_dir_security.GetAccessRules(true, true, typeof(NTAccount));
            for (int i = 0; i < dst_rules.Count; i++)
                loger_module.write_log(security_output((FileSystemAccessRule)dst_rules[i]));
        }

    }
}
