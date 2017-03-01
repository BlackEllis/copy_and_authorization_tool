using tool_commons.model;
using tool_commons.modules;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.Principal;

namespace tool_commons.modules
{
    class active_direcory_module
    {
        private static IDictionary<string, group_info> groups;

        public IDictionary<string, group_info> groups_list
        {
            get { return groups; }
        }
        public active_direcory_module() { }

        /// <summary>
        /// 対象ADのグループ一覧とそのメンバを取得
        /// </summary>
        /// <param name="host">接続先</param>
        /// <param name="common_names">調べるユーザー名、グループ名</param>
        /// <param name="organizational_units">所属組織</param>
        /// <param name="user">接続ユーザーIDを</param>
        /// <param name="pass">接続ユーザーパスワード</param>
        /// <param name="remain_hiierarch">取得階層</param>
        public void get_group_list(string host, string[] common_names, string[] organizational_units, string user="", string pass="")
        {
            DirectoryEntry objADAM = default(DirectoryEntry);
            DirectoryEntry objGroupEntry = default(DirectoryEntry);
            DirectorySearcher objSearchADAM = default(DirectorySearcher);
            SearchResultCollection objSearchResults = default(SearchResultCollection);
            SearchResult myResult = null;
            List<active_direcory_module> dst_list = new List<active_direcory_module>();

            // Enumerate groups
            try
            {
                string ldap_url = create_ldap_url_str(host, common_names, organizational_units);
                if (ldap_url != null)
                    objADAM = new DirectoryEntry(create_ldap_url_str(host, common_names, organizational_units), user, pass);
                else
                    objADAM = new DirectoryEntry();

                objADAM.RefreshCache();
                objSearchADAM = new DirectorySearcher(objADAM);
                objSearchADAM.Filter = "(&(objectClass=group))";
                objSearchADAM.SearchScope = SearchScope.Subtree;
                objSearchResults = objSearchADAM.FindAll();

                if (objSearchResults.Count == 0)
                    throw new Exception("No groups found");

                if (groups == null)
                    groups = new Dictionary<string, group_info>();

                foreach (SearchResult objResult in objSearchResults)
                {
                    myResult = objResult;
                    objGroupEntry = objResult.GetDirectoryEntry();
                    var sid = new SecurityIdentifier((byte[])objGroupEntry.Properties["objectSid"][0], 0).ToString();
                    var group_name = objGroupEntry.Properties["sAMAccountName"].Value.ToString();
                    if (!groups.ContainsKey(group_name))
                        groups.Add(group_name, new group_info(group_name, sid));
                    ExpandGroup(objGroupEntry, user, pass);
                }

            }
            catch (Exception e)
            {
                loger_module.write_log(e.Message);
            }
        }

        /// <summary>
        /// グループ情報取得の中間関数
        /// </summary>
        /// <param name="src_dir_entory"></param>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        private void ExpandGroup(DirectoryEntry src_dir_entory, string user, string pass)
        {
            try
            {
                using (var group = create_entry_obj(src_dir_entory.Path, user, pass))
                    Expand(group, user, pass);
            }
            catch (Exception e)
            {
                loger_module.write_log(e.Message, "error", "info");
            }
        }

        /// <summary>
        /// DirectorySearcher の Attribute Scope Query を使用して、グループを展開する
        /// </summary>
        /// <param name="group"></param>
        private void Expand(DirectoryEntry group, string user, string pass, string indent="\t")
        {
            try
            {
                string[] properties = new string[] { "member", "objectguid", "distinguishedName", "objectClass" };
                DirectorySearcher ds = new DirectorySearcher(group, "(objectClass=*)", properties, SearchScope.Base);
                ds.AttributeScopeQuery = "member";
                using (SearchResultCollection searchResultCollection = ds.FindAll())
                {
                    foreach (SearchResult result in searchResultCollection)
                    {
                        user_info user_obj = new user_info(result.GetDirectoryEntry());
                        ExpandGroup(create_entry_obj(result.Path, user, pass), user, pass);
                        var sid = new SecurityIdentifier((byte[])group.Properties["objectSid"][0], 0).ToString();
                        var group_name = group.Properties["sAMAccountName"].Value.ToString();
                        if (groups.ContainsKey(group_name))
                        {
                            groups[group_name].add_user_info(user_obj);
                        }
                        else
                            groups.Add(group_name, new group_info(group_name, sid, user_obj));
                    }
                }
            }
            catch (Exception e)
            {
                loger_module.write_log(e.Message, "error", "info");
            }

        }

        /// <summary>
        /// DirectoryEntoryオブジェクトを作成
        /// </summary>
        /// <param name="path"></param>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public static DirectoryEntry create_entry_obj(string path, string user, string pass)
        {
            return new DirectoryEntry(path, user, pass);
        }

        /// <summary>
        /// 検索条件に一致するユーザー情報をADから取り出す
        /// </summary>
        /// <param name="host">接続ADサーバー</param>
        /// <param name="common_names">検索表示名</param>
        /// <param name="organizational_units">検索所属</param>
        /// <param name="user">接続ユーザーID</param>
        /// <param name="pass">接続ユーザーパスワード</param>
        /// <returns></returns>
        public static List<user_info> get_infomation(string host, string[] common_names, string[] organizational_units, string user="", string pass="")
        {
            try
            {
                List<user_info> dst_list = new List<user_info>();
                var access_url = create_ldap_url_str(host, common_names, organizational_units);
                var directoryEntry = default(DirectoryEntry);
                if (access_url.Equals(""))
                    directoryEntry = new DirectoryEntry();
                else
                    directoryEntry = new DirectoryEntry(access_url, user, pass);
                var directorySearcher = new DirectorySearcher(directoryEntry); //引数を渡さなくてもいいかも

                var displayName = "*"; // 「*」をワイルドカードとして使える

                directorySearcher.Filter = String.Format("(&(objectClass=user)(DisplayName={0}))", displayName);
                directorySearcher.SizeLimit = int.MaxValue;
                directorySearcher.PageSize = int.MaxValue;

                var result = directorySearcher.FindOne();
                // null -> 該当 user が見つからない

                SearchResultCollection objs = directorySearcher.FindAll();
                foreach (SearchResult obj in objs)
                {
                    DirectoryEntry obj_entry = obj.GetDirectoryEntry();
                    user_info user_obj = new user_info(obj_entry);
                    if (user_obj.account_name.Length > 0)
                        dst_list.Add(user_obj);
                }

                return dst_list;
            }
            catch (Exception e)
            {
                loger_module.write_log(e.Message, "error", "info");
                return null;
            }

        }

        /// <summary>
        /// Ldapアクセス用の文字列を生成
        /// </summary>
        /// <param name="host"></param>
        /// <param name="common_names"></param>
        /// <param name="organizational_units"></param>
        /// <returns></returns>
        private static string create_ldap_url_str(string host, string[] common_names, string[] organizational_units)
        {
            const string ldap_protocol_str = "LDAP://";
            const string ldap_cn_str = "CN=";
            const string ldap_ou_str = "OU=";
            const string ldap_dc_str = "DC=";
            string dst_str = null;
            Func<string, string[], string, string> create_join_keyword = (string key_word, string[] param_strs, string padding_str) =>
            {
                string return_str = "";
                foreach (string param in param_strs)
                {
                    if (param != "")
                        return_str += (key_word + param + padding_str);
                }
                return return_str;
            };

            try
            {
                dst_str += create_join_keyword(ldap_cn_str, common_names, ",");
                dst_str += create_join_keyword(ldap_ou_str, organizational_units, ",");
                if ((host == null) || (host == "")) return null;

                dst_str += ldap_dc_str + host;
                dst_str = dst_str.Replace(".", ("," + ldap_dc_str));
                dst_str = ldap_protocol_str + host + "/" + dst_str;
                return dst_str;
            }
            catch (Exception e)
            {
                loger_module.write_log(e.Message, "error", "info");
                return null;
            }
        }

        /// <summary>
        /// 取得してあるグループ、メンバー情報をログに出力
        /// </summary>
        public void export_group_info()
        {
            if ((groups != null) && (groups.Count != 0))
            {
                foreach (KeyValuePair<string, group_info> arrays in groups)
                {
                    loger_module.write_log(arrays.Key);
                    foreach (user_info obj in arrays.Value.group_members)
                        obj.disp_parameters("\t");
                }
                loger_module.write_log("");
            }
        }
    }
}
