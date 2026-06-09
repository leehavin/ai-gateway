// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;
/// <summary>
/// 后台作业记录
/// </summary>
/// <remarks>详情参照Volo.Abp.BackgroundJobs;</remarks>
[SugarTable("sys_background_job_record")]
public class BackgroundJobRecordEntity
{
    /// <summary>
    /// Id
    ///</summary>
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public Guid Id { get; set; }

    /// <summary>
    /// Application name that scheduled this job.
    /// </summary>
    [SugarColumn(ColumnName = "application_name")]
    public string ApplicationName { get; set; }

    /// <summary>
    /// Type of the job.
    /// It's AssemblyQualifiedName of job type.
    /// </summary>
    [SugarColumn(ColumnName = "job_name")]
    public string JobName { get; set; }

    /// <summary>
    /// Job arguments as serialized string.
    /// </summary>
    [SugarColumn(ColumnName = "job_args")]
    public string JobArgs { get; set; }

    /// <summary>
    /// Try count of this job.
    /// A job is re-tried if it fails.
    /// </summary>
    [SugarColumn(ColumnName = "try_count")]
    public short TryCount { get; set; }

    /// <summary>
    /// Creation time of this job.
    /// </summary>
    [SugarColumn(ColumnName = "creation_time")]
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// Next try time of this job.
    /// </summary>
    [SugarColumn(ColumnName = "next_try_time")]
    public DateTime NextTryTime { get; set; }

    /// <summary>
    /// Last try time of this job.
    /// </summary>
    [SugarColumn(ColumnName = "last_try_time")]
    public DateTime? LastTryTime { get; set; }

    /// <summary>
    /// This is true if this job is continuously failed and will not be executed again.
    /// </summary>
    [SugarColumn(ColumnName = "is_abandoned")]
    public bool IsAbandoned { get; set; }

    /// <summary>
    /// Priority of this job.
    /// </summary>
    [SugarColumn(ColumnName = "priority")]
    public int Priority { get; set; } = 15;
}
