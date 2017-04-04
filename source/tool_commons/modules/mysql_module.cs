using System;

namespace tool_commons.modules
{
    class mysql_module
    {
        private string _connection_sql_str;
        public string connection_sql_str() { return _connection_sql_str; }
        public static mysql_module setup_sql(string host, string database, string user, string pass, string charset="utf8")
        {
            mysql_module dst_obj = new mysql_module();

            try
            {
                var mysql_builder = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder()
                {
                    Server = host,
                    Database = database,
                    UserID = user,
                    Password = pass,
                    CharacterSet = charset
                };
                dst_obj._connection_sql_str = mysql_builder.ToString();
            }
            catch (Exception e)
            {
                loger_manager.write_log(e.Message, "error");
                return null;
            }

            return dst_obj;
        }
    }
}
