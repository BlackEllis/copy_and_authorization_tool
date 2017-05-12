using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace tool_commons.modules
{
    class loger_manager
    {
        private static Dictionary<string, loger_module> _stream_list = null;

        public static void setup_manager()
        {
            if (_stream_list != null) return;

            _stream_list = new Dictionary<string, loger_module>();
        }

        public static void add_stream(string stream_name, string file_name, string output_dir, string file_encode, loger_module.E_LOG_LEVEL log_level, string max_output_capacity="", bool debug_flg = false)
        {
            loger_module obj = loger_module.create_loger(file_name, output_dir, file_encode, log_level, max_output_capacity, debug_flg);
            if (obj == null) return;
            if (_stream_list == null) return;
            if (_stream_list.ContainsKey(stream_name)) return;

            _stream_list.Add(stream_name, obj);
        }

        public static void write_log(string str, string category="INFO", string stream_name = "info")
        {
            string str_category = category.ToUpper();
            if ((_stream_list == null) || (_stream_list.Count == 0))
            {
                System.Console.WriteLine($"{str_category}\t{str}");
                return;
            }

            loger_module write_loger;

            if (_stream_list.ContainsKey(stream_name)) write_loger = _stream_list[stream_name];
            else write_loger = _stream_list.First().Value;

            switch (str_category)
            {
                case "ERROR":
                    write_loger.write_log_error(str);
                    break;
                case "WARNING":
                    write_loger.write_log_warning(str);
                    break;
                case "DEBUG":
                    write_loger.write_log_debug(str);
                    break;
                case "INFO":
                    write_loger.write_log_info(str);
                    break;
                default:
                    write_loger.write_log_userlog(str, str_category);
                    break;
            }
        }

        public static void close()
        {
            foreach (KeyValuePair<string, loger_module> row in _stream_list)
                row.Value.close();
        }
    }
}
