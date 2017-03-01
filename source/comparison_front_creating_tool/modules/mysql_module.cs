using System;

namespace comparison_front_creating_tool.modules
{
    class mysql_module
    {
        private string _connection_sql_str;
        public string connection_sql_str() { return _connection_sql_str; }
        public static mysql_module setup_sql(string host, string database, string user, string pass)
        {
            mysql_module dst_obj = new mysql_module();

            try
            {
                var mysql_builder = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder()
                {
                    Server = host,
                    Database = database,
                    UserID = user,
                    Password = pass
                };
                dst_obj._connection_sql_str = mysql_builder.ToString();
            }
            catch (Exception e)
            {
                loger_module.write_log(e.Message, "error", "info");
                return null;
            }

            return dst_obj;
        }
    }
}
