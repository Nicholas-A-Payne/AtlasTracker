using AtlasTracker.Data;
using AtlasTracker.Models;
using AtlasTracker.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AtlasTracker.Services
{
    public class BTRoleService : IBTRolesService
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;

        public BTRoleService(ApplicationDbContext context,
                            RoleManager<IdentityRole> roleManager, 
                            UserManager<AppUser> userManager)
        {
            _context = context;
            _roleManager = roleManager;
            _userManager = userManager;
        }


        public async Task<bool> AddUserToRoleAsync(AppUser user, string roleName)
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetRoleNameByIdAsync(string roleId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<IdentityRole>> GetRolesAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(AppUser user)
        {
            throw new NotImplementedException();
        }

        public async Task<List<AppUser>> GetUsersInRoleAsync(string roleName, int companyId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<AppUser>> GetUsersNotInRoleAsync(string roleName, int companyId)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> IsUserInRoleAsync(AppUser user, string roleName)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> RemoveUserFromRoleAsync(AppUser user, string roleName)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> RemoveUserFromRolesAsync(AppUser user, IEnumerable<string> roles)
        {
            throw new NotImplementedException();
        }
    }
}
