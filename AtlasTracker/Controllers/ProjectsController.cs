#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtlasTracker.Data;
using AtlasTracker.Models;
using AtlasTracker.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using AtlasTracker.Extensions;
using Microsoft.AspNetCore.Authorization;
using AtlasTracker.Models.ViewModels;
using AtlasTracker.Models.Enum;
using AtlasTracker.Services;

namespace AtlasTracker.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBTProjectService _projectService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IBTCompanyInfoService _companyInfoService;
        private readonly IBTRolesService _rolesService;
        private readonly IBTLookUpService _lookUpService;
        private readonly IBTFileService _fileService;

        public ProjectsController(ApplicationDbContext context,
                                  IBTProjectService projectService,
                                  UserManager<AppUser> userManager,
                                  IBTRolesService rolesService,
                                  IBTLookUpService lookUpService,
                                  IBTCompanyInfoService companyInfoService, 
                                  IBTFileService fileService)
        {
            _context = context;
            _projectService = projectService;
            _userManager = userManager;
            _rolesService = rolesService;
            _lookUpService = lookUpService;
            _companyInfoService = companyInfoService;
            _fileService = fileService;
        }

        //My Projects
        public async Task<IActionResult> Index()
        {
            int companyId = User.Identity.GetCompanyId();
            string userId = _userManager.GetUserId(User);
            var projects = await _projectService.GetUserProjectsAsync(userId);
            return View(projects);
        }

        public async Task<IActionResult> MyProjects()
        {
            string userId = _userManager.GetUserId(User);
            List<Project> project = await _projectService.GetUserProjectsAsync(userId);

            return View(project);
        }

        public async Task<IActionResult> AllProjects()
        {
            List<Project> projects = new();
            int companyId = User.Identity.GetCompanyId();

            if (User.IsInRole(nameof(AppRole.Admin))|| User.IsInRole(nameof(AppRole.ProjectManager)))
            {
                projects = await _companyInfoService.GetAllProjectsAsync(companyId); 
            }
            else
            {
                projects = await _projectService.GetAllProjectsByCompanyAsync(companyId);
            }

            return View(projects);
        }


        //Archived Projects
        public async Task<IActionResult> ArchivedProjects()
        {
            int companyId = User.Identity.GetCompanyId();
            List<Project> projects = await _projectService.GetArchivedProjectsByCompany(companyId);

            return View(projects);
        }

        //Unassigned Projects
        public async Task<IActionResult> UnassignedProjects()
        {
            int companyId = User.Identity.GetCompanyId();

            List<Project> projects = new();

            projects = await _projectService.GetUnassignedProjectsAsync(companyId);

            return View(projects);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignPM(int? projectId)
        {
            if (projectId == null)
            {
                return NotFound();
            }

            int companyId = User.Identity.GetCompanyId();

            AssignPMViewModel model = new();

            model.Project = await _projectService.GetProjectByIdAsync(projectId.Value, companyId);
            model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(nameof(AppRole.ProjectManager), companyId), "Id", "FullName");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPM(AssignPMViewModel model)
        {
            if (!string.IsNullOrEmpty(model.PMID))
            {
                await _projectService.AddProjectManagerAsync(model.PMID, model.Project.Id);
                return RedirectToAction(nameof(AllProjects));
            }
            return RedirectToAction(nameof(AssignPM), new { projectId = model.Project!.Id});
        }



        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            string userId = _userManager.GetUserId(User);
            AppUser appUser =  _context.Users.Find(userId);

            int companyId = User.Identity.GetCompanyId();

            Project project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // GET: Projects/Create
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Create()
        {
            int companyId = User.Identity.GetCompanyId();

            AddProjectWithPMViewModel model = new();

            model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(nameof(AppRole.ProjectManager), companyId), "Id", "FullName");
            model.PriorityList = new SelectList(await _lookUpService.GetProjectPrioritiesAsync(), "Id", "Name");
            return View(model);
        }

        // POST: Projects/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddProjectWithPMViewModel model)
        {
            int companyId = User.Identity.GetCompanyId();

            if (ModelState.IsValid)
            {
                try
                {
                    if (model.Project?.LogoFormFile != null)
                    {
                        model.Project.LogoData = await _fileService.ConvertFileToByteArrayAsync(model.Project.LogoFormFile);
                        model.Project.LogoName = model.Project.LogoFormFile.Name;
                        model.Project.LogoType = model.Project.LogoFormFile.ContentType;
                    }

                    model.Project.CompanyId = companyId;
                    model.Project.Created = DateTime.UtcNow;

                    model.Project.StartDate = DateTime.SpecifyKind(model.Project.StartDate, DateTimeKind.Utc);
                    model.Project.EndDate = DateTime.SpecifyKind(model.Project.EndDate, DateTimeKind.Utc);

                    await _projectService.AddNewProjectAsync(model.Project);

                    if (!string.IsNullOrEmpty(model.PMID))
                    {
                        await _projectService.AddProjectManagerAsync(model.PMID, model.Project.Id);
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {

                    throw;
                }
            }
            model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(nameof(AppRole.ProjectManager), companyId), "Id", "FullName");
            model.PriorityList = new SelectList(await _lookUpService.GetProjectPrioritiesAsync(), "Id", "Name");
            return View(model.Project);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            int companyId = User.Identity.GetCompanyId();
            AddProjectWithPMViewModel model = new();

            model.Project = await _projectService.GetProjectByIdAsync(id.Value, companyId);
            if (model.Project == null)
            {
                return NotFound();
            }

            AppUser projectManager = await _projectService.GetProjectManagerAsync(model.Project.Id);
            if (projectManager != null)
            {
                model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(nameof(AppRole.ProjectManager), companyId), "Id", "FullName", projectManager!.Id);

            }
            else
            {
                model.PriorityList = new SelectList(await _lookUpService.GetProjectPrioritiesAsync(), "Id", "Name");

            }

            return View(model);
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AddProjectWithPMViewModel model, Project project)
        {
            int companyId = User.Identity.GetCompanyId();


            if (model != null)
            {
                try
                {
                    if (model.Project?.LogoFormFile != null)
                    {
                        model.Project.LogoData = await _fileService.ConvertFileToByteArrayAsync(model.Project.LogoFormFile);
                        model.Project.LogoName = model.Project.LogoFormFile.Name;
                        model.Project.LogoType = model.Project.LogoFormFile.ContentType;
                    }

                    model.Project.Created = DateTime.SpecifyKind(model.Project.Created, DateTimeKind.Utc);
                    model.Project.StartDate = DateTime.SpecifyKind(model.Project.StartDate, DateTimeKind.Utc);
                    model.Project.EndDate = DateTime.SpecifyKind(model.Project.EndDate, DateTimeKind.Utc);

                    await _projectService.UpdateProjectAsync(project);

                    if (!string.IsNullOrEmpty(model.PMID))
                    {
                        await _projectService.AddProjectManagerAsync(model.PMID, model.Project.Id);
                    }

                    return RedirectToAction("Index");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ProjectExists(model.Project!.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(nameof(AppRole.ProjectManager), companyId), "Id", "FullName");
            model.PriorityList = new SelectList(await _lookUpService.GetProjectPrioritiesAsync(), "Id", "Name");
            return View(model);
        }

        // GET: Projects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.Company)
                .Include(p => p.ProjectPriority)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ProjectExists(int id)
        {
            int companyId = User.Identity.GetCompanyId();
            return (await _projectService.GetAllProjectsByCompanyAsync(companyId)).Any(p => p.Id == id);
        }
    }
}
