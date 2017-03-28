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
                    utility_tools.get_value_from_hasharray(hash_array,
                                            constant.RESOURCES_KEY_EXTERNAL,
                                            constant.RESOURCES_DIR + constant.EXTERNAL_RESOURCE_FILENAME)
                );
                Console.WriteLine("read setting from json : end");

                // 各外部ファイルのディレクトリ先を設定
                string log_dir = json_module.get_external_resource("log_file_dir", constant.LOG_FILE_DIR);
                string export_dir = json_module.get_external_resource("export_dir", constant.EXPORT_DIR);
                string resources_dir = utility_tools.get_value_from_hasharray(hash_array, "RESOURCES_DIR", constant.RESOURCES_DIR);

                Console.WriteLine("log feature setup : start");
                setup_logs(hash_array, log_dir); // ログ、エラーファイルのセットアップ
                Console.WriteLine("log feature setup : end");

                // デシリアライズした結果を連想配列に格納
                Console.WriteLine("deserialize to Dictionary : start");
                Dictionary<string, comparsion_unit> comparison_list = inport_serialize(json_module.get_external_resource("inport_xml_filename"), resources_dir)?.transform_to_dictionary();
                Console.WriteLine("deserialize to Dictionary : end");

                string resouces_excel_file = json_module.get_external_resource("copy_dir_comparison_file");
                DataTable copy_info_table = excel_converter_module.read_excel_by_row(resouces_excel_file, resources_dir,
                                                                                    json_module.get_external_resource("copy_dir_info_sheet"),
                                                                                    int.Parse(json_module.get_external_resource("copy_dir_info_sheet_offset")));
                DataTable exception_copy_table = excel_converter_module.read_excel_by_row(resouces_excel_file, resources_dir,
                                                                                        json_module.get_external_resource("exception_copy_dir_sheet"),
                                                                                        int.Parse(json_module.get_external_resource("exception_copy_dir_sheet_offset")));

                if (copy_info_table == null) throw new Exception("コピー対比表情報取得に失敗しました");
                if (exception_copy_table == null) throw new Exception("例外対象一覧の取得に失敗しました");

                List<string> exception_list = new List<string>();
                foreach (DataRow row in exception_copy_table.Rows)
                {
                    string exception_copy_dir = row["対象外ディレクトリ名"].ToString();
                    if (!exception_copy_dir.Equals(""))
                        exception_list.Add(exception_copy_dir);
                }

                // robocopyをテストモードで動作させるか判定用パラメータ取得
                bool diff_mode = bool.Parse(utility_tools.get_value_from_hasharray(hash_array, constant.RESOURCES_KEY_DIFF_MODE, "False"));

                // ロボコピー実行関数
                foreach (DataRow row in copy_info_table.Rows)
                {
                    string src_dir = row["コピー元ディレクトリ"].ToString();
                    string dst_dir = row["コピー先ディレクトリ"].ToString();
                    string user_id = row["コピー先アクセスID"].ToString();
                    string user_pw = row["コピー先アクセスPW"].ToString();

                    communication_with_external_server(src_dir, dst_dir, user_id, user_pw, true);
                    Console.WriteLine("run robocopy process : start");
                    robocopy_process(src_dir, dst_dir, exception_list, row["コピーフィルター"].ToString(), diff_mode);
                    Console.WriteLine("run robocopy process : end");

                    if (diff_mode) continue;

                    Console.WriteLine("run conversion　association process : start");
                    conversion_association(src_dir, dst_dir, ref comparison_list, ref exception_list);
                    Console.WriteLine("run conversion　association process : end");
                    communication_with_external_server(src_dir, dst_dir, user_id, user_pw, false);
                }

            }
            catch (Exception e)
            {
                 loger_manager.write_log(e.Message, "error");
                Console.WriteLine(e.Message);
            }

            loger_manager.close();
            Console.WriteLine("press any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// ログ、エラーファイルのセットアップ関数
        /// </summary>
        /// <param name="args">起動引数を連想配列にしたもの</param>
        /// <param name="log_dir"></param>
        private static void setup_logs(Dictionary<string, string>args, string log_dir)
        {
            // ログファイルの関係設定
            string log_file = json_module.get_external_resource("default_log_filename", constant.DEFAULT_LOG_FILENAME);
            log_file = utility_tools.get_value_from_hasharray(args, constant.RESOURCES_KEY_LOG, log_file);
            string log_encode = json_module.get_external_resource("log_file_encode", constant.LOG_FILE_ENCODE);
            string wk_log_output_level = json_module.get_external_resource("default_log_output_level", (loger_module.E_LOG_LEVEL.E_ERROR | loger_module.E_LOG_LEVEL.E_WARNING).ToString());
            wk_log_output_level = utility_tools.get_value_from_hasharray(args, constant.RESOURCES_KEY_LOGLEVEL, wk_log_output_level);
            loger_module.E_LOG_LEVEL log_output_level = (loger_module.E_LOG_LEVEL)Enum.Parse(typeof(loger_module.E_LOG_LEVEL), wk_log_output_level);

            // 抽出ログファイルの関係設定
            string extracting_file = json_module.get_external_resource("default_extracting_filename", constant.DEFAULT_EXTRACTING_FILENAME);
            extracting_file = utility_tools.get_value_from_hasharray(args, constant.RESOURCES_KEY_EXTRACTINGLOG, extracting_file);
            string extracting_encode = json_module.get_external_resource("extracting_file_encode", constant.EXTRACTING_FILE_ENCODE);
            string wk_extracting_output_level = json_module.get_external_resource("default_extracting_output_level", loger_module.E_LOG_LEVEL.E_ALL.ToString());
            wk_extracting_output_level = utility_tools.get_value_from_hasharray(args, constant.RESOURCES_KEY_LOGLEVEL, wk_extracting_output_level);
            loger_module.E_LOG_LEVEL extracting_output_level = (loger_module.E_LOG_LEVEL)Enum.Parse(typeof(loger_module.E_LOG_LEVEL), wk_extracting_output_level);

            loger_manager.setup_manager();
#if DEBUG
            loger_manager.add_stream("info", log_file, log_dir, log_encode, log_output_level, true);
            loger_manager.add_stream("extracting", extracting_file, log_dir, extracting_encode, extracting_output_level, true);
#else
            loger_manager.add_stream("info", log_file, log_dir, log_encode, log_output_level);
            loger_manager.add_stream("extracting", extracting_file, log_dir, extracting_encode, extracting_output_level);
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
                loger_manager.write_log(e.Message, "error");
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
            string connection_name = target_name;
            if (cat_point != -1) connection_name = target_name.Substring(0, cat_point + 1); // PC名＋共有フォルダまでの文字列の切り出し

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
                loger_manager.write_log(p.StartInfo.Arguments);
                loger_manager.write_log(p.StandardOutput.ReadToEnd());
                loger_manager.write_log(p.StandardError.ReadToEnd(), "error");
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
                        p.StartInfo.Arguments = string.Format("/C ROBOCOPY \"{0}\" \"{1}\" \"{2}\" /L" + json_module.get_external_resource("robocopy_option") + exception_dir_str, src_dir, dst_dir, copy_filter);
                    else
                        p.StartInfo.Arguments = string.Format("/C ROBOCOPY \"{0}\" \"{1}\" \"{2}\"" + json_module.get_external_resource("robocopy_option") + exception_dir_str, src_dir, dst_dir, copy_filter);
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.Start();
                    loger_manager.write_log(p.StartInfo.Arguments);
                    loger_manager.write_log(p.StandardOutput.ReadToEnd());
                    loger_manager.write_log(p.StandardError.ReadToEnd(), "error");
                    p.WaitForExit();
                }
            }
            catch (Exception e)
            {
                 loger_manager.write_log(e.Message, "error");
            }
        }

        /// <summary>
        /// 権限移管関数
        /// </summary>
        /// <param name="src_dir">移管元開始ディレクトリフルパス</param>
        /// <param name="dst_dir">移管先開始ディレクトリフルパス</param>
        /// <param name="comparison_list">変換対象リスト</param>
        /// <param name="exception_list">対象外リスト</param>
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

                    loger_manager.write_log($"例外対象: {src_dir_info.FullName}", "exception");
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
                    foreach (var dst_file in dst_file_list)
                    {
                        if (!dst_file.Name.Equals(src_file.Name)) continue;

                        if(!file_authority_replacement(src_file, dst_file, ref comparison_list))
                            loger_manager.write_log($"該当ファイルがコピーされていないか、アクセス権がありません。：{src_file.FullName}", "error", "extracting");
                        break;
                    }
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
                 loger_manager.write_log(e.Message, "error");
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
                    int cat_pint = src_rules.IdentityReference.ToString().LastIndexOf('\\') + 1;
                    string account_name = src_rules.IdentityReference.ToString().Substring(cat_pint);
                    if (comparison_list.ContainsKey(account_name))
                    {

                        loger_manager.write_log($"適応先： {dst_dir.FullName} " + ((src_rules.InheritanceFlags & InheritanceFlags.ContainerInherit) > 0 ? "このフォルダとサブフォルダ" : "このフォルダのみ"), "conversion");
                        comparsion_unit unit = comparison_list[account_name];
                        if (unit.del_flg == 1)
                        {
                            loger_manager.write_log($"削除対象アカウント： {unit.account_name} {unit.conversion_original} | {src_rules.FileSystemRights.ToString()}", "conversion");
                            continue; // del_flgが1のものは権限設定処理を行わない
                        }

                        loger_manager.write_log($"変換対象アカウント： {unit.account_name} {unit.conversion_original} → {unit.after_conversion} | {src_rules.FileSystemRights.ToString()}", "conversion");
                        dst_dir_security.AddAccessRule(new FileSystemAccessRule(unit.after_conversion,
                                                                                src_rules.FileSystemRights,
                                                                                src_rules.InheritanceFlags,
                                                                                src_rules.PropagationFlags,
                                                                                src_rules.AccessControlType));
                    }
                    else
                    {
                        loger_manager.write_log($"適応先： {dst_dir.FullName} " + ((src_rules.InheritanceFlags & InheritanceFlags.ContainerInherit) > 0 ? "このフォルダとサブフォルダ" : "このフォルダのみ"), "extracting", "extracting");
                        loger_manager.write_log($"変換対象外アカウント：{account_name} | {src_rules.FileSystemRights.ToString()}", "extracting", "extracting");
                        dst_dir_security.AddAccessRule(src_rules);
                    }
                }
                if (dst_dir_security.GetAccessRules(true, true, typeof(NTAccount)).Count > 0)
                    dst_dir.SetAccessControl(dst_dir_security); // アクセス権の設定
                else
                    throw new Exception($"アクセス権の設定が無い : {src_dir.FullName}");

                return true;
            }
            catch (Exception e)
            {
                loger_manager.write_log($"{e.GetType()} {e.Message}", "error", "info");
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
                    int cat_pint = src_rules.IdentityReference.ToString().LastIndexOf('\\') + 1;
                    string account_name = src_rules.IdentityReference.ToString().Substring(cat_pint);
                    loger_manager.write_log($"{account_name}");
                    if (comparison_list.ContainsKey(account_name))
                    {
                        loger_manager.write_log($"適応先： {dst_file.FullName} " + ((src_rules.InheritanceFlags & InheritanceFlags.ContainerInherit) > 0 ? "このフォルダとサブフォルダ" : "このフォルダのみ"), "conversion");

                        comparsion_unit unit = comparison_list[account_name];
                        if (unit.del_flg == 1)
                        {
                            loger_manager.write_log($"削除対象アカウント： {unit.account_name} {unit.conversion_original} | {src_rules.FileSystemRights.ToString()}", "conversion");
                            continue; // del_flgが1のものは権限設定処理を行わない
                        }

                        loger_manager.write_log($"変換対象アカウント： {unit.account_name} {unit.conversion_original} → {unit.after_conversion} | {src_rules.FileSystemRights.ToString()}", "conversion");
                        dst_file_security.AddAccessRule(new FileSystemAccessRule(unit.after_conversion,
                                                                                    src_rules.FileSystemRights,
                                                                                    src_rules.InheritanceFlags,
                                                                                    src_rules.PropagationFlags,
                                                                                    src_rules.AccessControlType));
                    }
                    else
                    {
                        loger_manager.write_log($"適応先： {dst_file.FullName} " + ((src_rules.InheritanceFlags & InheritanceFlags.ContainerInherit) > 0 ? "このフォルダとサブフォルダ" : "このフォルダのみ"), "extracting");
                        loger_manager.write_log($"変換対象外アカウント：{account_name} | {src_rules.FileSystemRights.ToString()}", "extracting");
                        dst_file_security.AddAccessRule(src_rules); // 変換対象が無ければ、移管元の権限そのまま移管
                    }
                }
                if (dst_file_security.GetAccessRules(true, true, typeof(NTAccount)).Count > 0)
                    dst_file.SetAccessControl(dst_file_security);
                else
                    throw new Exception($"アクセス権の設定が無い : {src_file.FullName}");

                return true;
            }
            catch (Exception e)
            {
                 loger_manager.write_log(e.Message, "error");
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
                loger_manager.write_log($"比較対象設定が不適切\t \t\tsrc_dir:{src_dir}\n\t\tdst_dir:{dst_dir}");
                return;
            }

            DirectorySecurity src_dir_security = Directory.GetAccessControl(src_dir);
            DirectorySecurity dst_dir_security = Directory.GetAccessControl(dst_dir);

            loger_manager.write_log("AccessControlType\tAccountName\tFileSystemRights\tIsInherited"
+ "\tInheritanceFlags\tPropagationFlags");
            AuthorizationRuleCollection src_rules = src_dir_security.GetAccessRules(true, true, typeof(NTAccount));
            for (int i = 0; i < src_rules.Count; i++)
                loger_manager.write_log(security_output((FileSystemAccessRule)src_rules[i]));

            loger_manager.write_log("AccessControlType\tAccountName\tFileSystemRights\tIsInherited"
+ "\tInheritanceFlags\tPropagationFlags");
            AuthorizationRuleCollection dst_rules = dst_dir_security.GetAccessRules(true, true, typeof(NTAccount));
            for (int i = 0; i < dst_rules.Count; i++)
                loger_manager.write_log(security_output((FileSystemAccessRule)dst_rules[i]));
        }

    }
}
