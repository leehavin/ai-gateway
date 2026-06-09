// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity
{
    /// <summary>
    /// Oauth2用户
    /// </summary>
    [SugarTable("sys_oauth2_user")]
    public class OAuth2UserEntity
    {
        /// <summary>
        /// PersistenceId
        /// </summary>
        [SugarColumn(ColumnName = "persistence_id", IsPrimaryKey = true)]
        public long PersistenceId { get; set; }
        /// <summary>
        /// 创建时间
        ///</summary>
        [SugarColumn(ColumnName = "create_time")]
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// Id
        ///</summary>
        [SugarColumn(ColumnName = "id")]
        public long Id { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        [SugarColumn(ColumnName = "name")]
        public string Name { get; set; }
        /// <summary>
        /// Type
        ///</summary>
        [SugarColumn(ColumnName = "type")]
        public string Type { get; set; }
        /// <summary>
        /// 用户ID
        /// </summary>
        [SugarColumn(ColumnName = "user_id")]
        public long? UserId { get; set; }
    }
}
