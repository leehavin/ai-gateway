// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

using PurestAdmin.Application.RoleServices.Dtos;
using PurestAdmin.Core.Cache;
using PurestAdmin.Multiplex.Contracts.IAdminUser;

namespace PurestAdmin.Application.RoleServices;
/// <summary>
/// 角色服务
/// </summary>
[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.SYSTEM)]
public class RoleService(ISqlSugarClient db, Repository<RoleEntity> roleRepository, IAdminCache cache, ICurrentUser currentUser) : ApplicationService
{
    private readonly ISqlSugarClient _db = db;
    private readonly Repository<RoleEntity> _roleRepository = roleRepository;
    private readonly IAdminCache _cache = cache;
    private readonly ICurrentUser _currentUser = currentUser;
    /// <summary>
    /// 分页查询
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<PagedList<RoleOutput>> GetPagedListAsync(GetPagedListInput input)
    {
        var roles = await _currentUser.GetRoleTreeAsync();
        var roleOutputs = roles.First().Children.Adapt<List<RoleOutput>>();
        if (!input.Name.IsNullOrEmpty())
        {
            List<RoleOutput> FilterRoles(List<RoleOutput> all)
            {
                List<RoleOutput> ls = [];
                foreach (var item in all)
                {
                    if (item.Name.Contains(input.Name))
                    {
                        ls.Add(item);
                    }
                    if (item.Children?.Count > 0)
                    {
                        ls.AddRange(FilterRoles(item.Children));
                    }
                }
                return ls;
            }
            return FilterRoles(roleOutputs).ToPurestPagedList(input.PageIndex, input.PageSize); ;
        }
        return roleOutputs.ToPurestPagedList(input.PageIndex, input.PageSize);
    }

    /// <summary>
    /// 全量查询
    /// </summary>
    /// <param name="roleName"></param>
    /// <returns></returns>
    public async Task<List<RoleOutput>> GetRolesAsync(string roleName)
    {
        var list = await _db.Queryable<RoleEntity>()
            .WhereIF(!roleName.IsNullOrEmpty(), x => x.Name.Contains(roleName))
            .OrderByDescending(x => x.CreateTime)
            .ToListAsync();
        return list.Adapt<List<RoleOutput>>();
    }

    /// <summary>
    /// 单条查询
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<RoleOutput> GetAsync(long id)
    {
        var entity = await _roleRepository.GetByIdAsync(id) ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
        return entity.Adapt<RoleOutput>();
    }

    /// <summary>
    /// 添加
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<long> AddAsync(AddRoleInput input)
    {
        var entity = input.Adapt<RoleEntity>();
        return await _roleRepository.InsertReturnSnowflakeIdAsync(entity);
    }

    /// <summary>
    /// 编辑
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task PutAsync(long id, AddRoleInput input)
    {
        var entity = await _roleRepository.GetByIdAsync(id) ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
        var newEntity = input.Adapt(entity);
        _ = await _roleRepository.UpdateAsync(newEntity);
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task DeleteAsync(long id)
    {
        var entity = await _roleRepository.GetByIdAsync(id) ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
        _ = await _roleRepository.DeleteAsync(entity);
    }

    /// <summary>
    /// 赋给角色功能
    /// </summary>
    /// <param name="roleId">角色Id</param>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    public async Task AssignFunctionAsync(long roleId, long[] input)
    {
        _ = await _roleRepository.GetByIdAsync(roleId) ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        _ = await _db.Deleteable<RoleFunctionEntity>().Where(x => x.RoleId == roleId).ExecuteCommandAsync();

        var roleSecurityEntities = input.Select(x => new RoleFunctionEntity { RoleId = roleId, FunctionId = x }).ToList();
        _ = await _db.Insertable(roleSecurityEntities).ExecuteReturnSnowflakeIdListAsync();

        //移除对应角色的缓存的接口
        _cache.Remove(AdminClaimConst.CACHE_ROLESINTERFACE_PREFIX + roleId);
    }
    /// <summary>
    /// 获取角色的功能
    /// </summary>
    public async Task<List<FunctionOutput>> GetFunctionsAsync(long roleId)
    {
        var functions = await _db.Queryable<RoleFunctionEntity>()
            .LeftJoin<FunctionEntity>((rf, f) => rf.FunctionId == f.Id)
            .Where((rf, f) => rf.RoleId == roleId)
            .Select((rf, f) => f)
            .ToListAsync();
        return functions.Adapt<List<FunctionOutput>>();
    }
}
