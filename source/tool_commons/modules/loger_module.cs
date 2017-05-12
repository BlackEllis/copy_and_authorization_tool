using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace tool_commons.modules
{
    class loger_module
    {
        public enum E_LOG_LEVEL : int
        {
            E_ALL = 0xff,
            E_ERROR = 0x01,
            E_WARNING = 0x02,
            E_DEBUG = 0x04,
            E_INFO = 0x08,
            E_USER_LOG = 0x10,
        };

        private E_LOG_LEVEL _output_level;
        private FileStream _fl_stream;
        private StreamWriter _stream;
        private String _file_name;
        private String _file_encode;
        private String _original_filename;
        private String _original_extension;
        private int _division_count;
        private long _max_output_size;

        /// <summary>
        /// ログ出力オブジェクトの作成
        /// </summary>
        /// <param name="file_name">出力ファイル名</param>
        /// <param name="output_dir">出力先ディレクトリ</param>
        /// <param name="file_encode">出力エンコード</param>
        /// <param name="debug_flg"></param>
        public static loger_module create_loger(string file_name, string output_dir, string file_encode, E_LOG_LEVEL log_level, string max_output_capacity, bool debug_flg = false)
        {
            try
            {
                string log_file_name = "";
                string log_dir = path_extracted(file_name); // ディレクト情報の抽出
                if (log_dir == "") // パス情報が無ければ、固定値にする
                {
                    log_dir = output_dir;
                    string join_filename = log_dir + file_name;
                    log_file_name = create_log_filename(join_filename, debug_flg);
                }
                else
                    log_file_name = create_log_filename(file_name, debug_flg);

                if (!Directory.Exists(log_dir)) Directory.CreateDirectory(log_dir); // ディレクトが無ければ作成

                return new loger_module(log_file_name, file_encode, log_level, max_output_capacity);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        /// <summary>
        /// 引数に渡されたファイル名に日時を付与してユニークな名前を生成
        /// </summary>
        /// <param name="file_name">出力ファイル名</param>
        /// <param name="debug_flg">デバッグフラグ（日時がつかなくなる）</param>
        /// <returns>生成されたファイル名称</returns>
        private static string create_log_filename(string file_name, bool debug_flg)
        {
            string name = file_name;
            string extension = ".log";
            string dst_filename = "";
            System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
            DateTime dt_obj = DateTime.Now;
            string dt_stmp = dt_obj.ToString("yyyy-MM-dd_") + dt_obj.ToString("HHmmss");
            int cat_point = file_name.LastIndexOf("."); // 拡張子

            if (cat_point != -1)
            {
                name = file_name.Substring(0, cat_point);
                extension = file_name.Substring(cat_point);
            }

            if (debug_flg == true)
                dst_filename = $"{name}{extension}";
            else
                dst_filename = $"{name}_{process.Id.ToString()}_{dt_stmp}{extension}";

            return dst_filename;

        }

        /// <summary>
        /// 最後に出現する区切り文字までを切り出す
        /// </summary>
        /// <param name="path">文字列</param>
        /// <param name="padding_font">区切り文字</param>
        /// <returns>最後に出現した具切り文字までの文字列</returns>
        private static string path_extracted(string path, string padding_font = "/")
        {
            string st_path = path.Replace("\\", padding_font);
            int cat_point = st_path.LastIndexOf(padding_font);
            if (cat_point == -1) return "";

            return st_path.Substring(0, cat_point);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="file_name">ログファイル名</param>
        /// <param name="file_encode">エンコード</param>
        /// <param name="log_level">出力ログレベル</param>
        loger_module(string file_name, string file_encode, E_LOG_LEVEL log_level, string max_output_capacity)
        {
            _output_level = log_level;
            _fl_stream = new FileStream(file_name, FileMode.Append, FileAccess.Write, FileShare.Read);
            _stream = new StreamWriter(_fl_stream, Encoding.GetEncoding(file_encode));
            _file_name = file_name;
            _file_encode = file_encode;

            int cat_point = file_name.LastIndexOf("."); // 拡張子との区切り文字位置を取得
            if (cat_point != -1)
            {
                _original_filename = file_name.Substring(0, cat_point);
                _original_extension = file_name.Substring(cat_point);
                _division_count = 1;
                _max_output_size = calc_to_byte(max_output_capacity);
            }
        }

        /// <summary>
        /// オブジェクトの後始末
        /// </summary>
        public void close()
        {
            if (_stream == null) return;

            _stream.Dispose();
            _stream = null;
            _fl_stream.Dispose();
            _fl_stream = null;
        }

        /// <summary>
        /// ログ出力
        /// </summary>
        /// <param name="str">出力文字列</param>
        /// <param name="log_category">ログ種別</param>
        /// <param name="replace_category">差し替えログ種別</param>
        private void write_log(string str, string log_category)
        {
            if ((str == null) || (str.Equals(""))) return;
            if (_stream.Equals(null))
            {
                Console.WriteLine(str); // ログ出力オブジェクトがない場合はコンソールに表示
                return;
            }

            try
            {
                FileInfo file_info = new FileInfo(_file_name);
                if ((_max_output_size != 0) && (file_info.Length > _max_output_size))
                {
                    _stream.Dispose();
                    _fl_stream.Dispose();
                    _file_name = $"{_original_filename}_{_division_count}{_original_extension}";
                    _fl_stream = new FileStream(_file_name, FileMode.Append, FileAccess.Write, FileShare.Read);
                    _stream = new StreamWriter(_fl_stream, Encoding.GetEncoding(_file_encode));
                    _division_count++;
                }

                DateTime dt_obj = DateTime.Now;
                string write_str = dt_obj.ToString("HH:mm:ss.fff\t");
                write_str += (!log_category.Equals("")) ? $"[{log_category}]" + ":\t" : "";
                write_str += str;

                _stream.Write(write_str);
                _stream.Flush();
                _stream.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// エラーログ出力関数
        /// </summary>
        /// <param name="str">出力文字列</param>
        public void write_log_error(string str)
        {
            if ((_output_level & E_LOG_LEVEL.E_ERROR) == 0) return;
            write_log(str, "ERROR");
        }

        /// <summary>
        /// 警告ログ出力関数
        /// </summary>
        /// <param name="str">出力文字列</param>
        public void write_log_warning(string str)
        {
            if ((_output_level & E_LOG_LEVEL.E_WARNING) == 0) return;
            write_log(str, "WARNING");
        }

        public void write_log_debug(string str)
        {
            if ((_output_level & E_LOG_LEVEL.E_DEBUG) == 0) return;
            write_log(str, "DEBUG");
        }

        /// <summary>
        /// インフォーメーションログ出力関数
        /// </summary>
        /// <param name="str">出力文字列</param>
        public void write_log_info(string str)
        {
            if ((_output_level & E_LOG_LEVEL.E_INFO) == 0) return;
            write_log(str, "INFO");
        }

        /// <summary>
        /// ユーザー定義のログ出力関数
        /// </summary>
        /// <param name="str">出力文字列</param>
        /// <param name="category">出力カテゴリ</param>
        public void write_log_userlog(string str, string category)
        {
            if ((_output_level & E_LOG_LEVEL.E_USER_LOG) == 0) return;
            string str_category = category.ToUpper();
            write_log(str, str_category);
        }

        /// <summary>
        /// 値＋単位からバイト単位を計算する関数
        /// </summary>
        /// <param name="str_val">値＋単位　※k~Pまで対応</param>
        /// <returns></returns>
        public long calc_to_byte(string str_val)
        {
            Dictionary<string, long> unit_list = new Dictionary<string, long>()
            {
                {"K", 1024},
                {"M", 1048576},
                {"G", 1073741824},
                {"T", 1099511627776},
                {"P", 1125899906842624}
            };

            if (str_val.Equals("0")) return 0;

            string upper_case = str_val.ToUpper();
            if (!System.Text.RegularExpressions.Regex.IsMatch(upper_case, @"\d{1,}?[KMGTP]{0,1}$")) return 0;

            int catpoint = (upper_case.Length - 1);
            long value = long.Parse(upper_case.Substring(0, catpoint));
            string unit = upper_case.Substring(catpoint);

            if (unit_list.ContainsKey(unit)) return value * unit_list[unit];
            else return value;
        }
    }
}
