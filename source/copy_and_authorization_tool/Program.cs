using System.Collections.Generic;
using tool_commons.modules;

namespace copy_and_authorization_tool
{
    class Program
    {
        static void Main(string[] args)
        {
            // 起動引数からパラメータ毎に切り分けた連想配列を生成
            Dictionary<string, string> hash_array = utility_tools.argument_decomposition(args);
        }
    }
}
