﻿#nullable disable
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
using AtlasTracker.Models.Enum;
using AtlasTracker.Services;
using AtlasTracker.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace AtlasTracker.Controllers
{
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBTProjectService _projectService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IBTCompanyInfoService _companyInfoService;
        private readonly IBTRolesService _rolesService;
        private readonly IBTLookUpService _lookUpService;
        private readonly IBTTicketService _ticketService;
        private readonly IBTFileService _fileService;
        private readonly IBTTicketHistoryService _ticketHistoryService;

        public TicketsController(ApplicationDbContext context,
                                  IBTProjectService projectService,
                                  UserManager<AppUser> userManager,
                                  IBTRolesService rolesService,
                                  IBTLookUpService lookUpService,
                                  IBTCompanyInfoService companyInfoService,
                                  IBTTicketService ticketService,
                                  IBTFileService fileService,
                                  IBTTicketHistoryService ticketHistoryService)
        {
            _context = context;
            _projectService = projectService;
            _userManager = userManager;
            _rolesService = rolesService;
            _lookUpService = lookUpService;
            _companyInfoService = companyInfoService;
            _ticketService = ticketService;
            _fileService = fileService;
            _ticketHistoryService = ticketHistoryService;
        }

        // GET: Tickets
        //public async Task<IActionResult> Index()
        //{
        //    var applicationDbContext = _context.Tickets.Include(t => t.DeveloperUser).Include(t => t.OwnerUser).Include(t => t.Projects).Include(t => t.TicketPriority).Include(t => t.TicketStatus).Include(t => t.TicketTypes);
        //    return View(await applicationDbContext.ToListAsync());
        //}

        public async Task<IActionResult> MyTickets()
        {
            int companyId = User.Identity.GetCompanyId();
            string userId = _userManager.GetUserId(User);

            List<Ticket> tickets = await _ticketService.GetTicketsByUserIdAsync(userId, companyId);
            return View(tickets);
        }

        public async Task<IActionResult> AllTickets()
        {
            List<Ticket> tickets = new();
            int companyId = User.Identity.GetCompanyId();


            if (User.IsInRole(nameof(AppRole.Admin)) || User.IsInRole(nameof(AppRole.ProjectManager)))
            {
                tickets = await _ticketService.GetAllTicketsByCompanyAsync(companyId);

            }
            else
            {
                tickets = (await _ticketService.GetAllTicketsByCompanyAsync(companyId)).Where(t => t.Archived == false).ToList();
            }


            return View(tickets);
        }

        public async Task<IActionResult> ArchivedTickets()
        {
            int companyId = User.Identity.GetCompanyId();
            List<Ticket> tickets = await _ticketService.GetArchivedTicketsAsync(companyId);
            return View(tickets);
        }

        public async Task<IActionResult> UnassignedTickets()
        {
            string userId = _userManager.GetUserId(User);
            int companyId = User.Identity.GetCompanyId();
            List<Ticket> tickets = await _ticketService.GetUnassignedTicketsAsync(companyId);

            if (User.IsInRole(nameof(AppRole.Admin)))
            {
                return View(tickets);
            }
            else
            {
                List<Ticket> pmTickets = new();

                foreach (Ticket ticket in tickets)
                {
                    if (await _projectService.IsAssignedProjectManagerAsync(userId, ticket.ProjectId))
                    {
                        pmTickets.Add(ticket);
                    }

                }

                return View(pmTickets);
            }


        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignPM(int id)
        {

            AssignDeveloperViewModel model = new();

            model.Ticket = await _ticketService.GetTicketByIdAsync(id);
            model.Developers = new SelectList(await _projectService.GetProjectMembersByRoleAsync(model.Ticket.ProjectId, nameof(AppRole.Developer)), 
                                                "Id", "FullName");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDeveloper(AssignDeveloperViewModel model)
        {
            if (model.DeveloperId != null)
            {
                AppUser appUser = await _userManager.GetUserAsync(User);

                Ticket oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(model.Ticket!.Id);

                try
                {
                    await _ticketService.AssignTicketAsync(model.Ticket.Id, model.DeveloperId);
                }
                catch (Exception)
                {

                    throw;
                }

                Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(model.Ticket.Id);
                await _ticketHistoryService.AddHistoryAsync(oldTicket, newTicket, appUser.Id);
            }
            return RedirectToAction(nameof(Details));
        }

        [HttpPost]
        public async Task<IActionResult> AddTicketComment([Bind("Id, TicketId, Comment")] TicketComment ticketcomment)
        {
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                try
                {
                    ticketcomment.UserId = _userManager.GetUserId(User);
                    ticketcomment.Created = DateTime.UtcNow;

                    await _ticketService.AddTicketCommentAsync(ticketcomment);


                    //Add History
                    await _ticketHistoryService.AddHistoryAsync(ticketcomment.TicketId, nameof(TicketComment), ticketcomment.UserId);
                }
                catch (Exception)
                {

                    throw;
                }
            }

            return RedirectToAction("Details", new { id = ticketcomment.TicketId });
        }

        // GET: Tickets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                .Include(t => t.DeveloperUser)
                .Include(t => t.OwnerUser)
                .Include(t => t.Projects)
                .Include(t => t.TicketPriority)
                .Include(t => t.Attatchments)
                .Include(t => t.TicketStatus)
                .Include(t => t.TicketTypes)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // GET: Tickets/Create
        public async Task<IActionResult> Create()
        {
            AppUser appUser = await _userManager.GetUserAsync(User);
            if (User.IsInRole(nameof(AppRole.Admin)))
            {
                ViewData["ProjectId"] = new SelectList(await _projectService.GetAllProjectsByCompanyAsync(appUser.CompanyId), "Id", "Name");
            }
            else
            {
                ViewData["ProjectId"] = new SelectList(await _projectService.GetUserProjectsAsync(appUser.Id), "Id", "Name");
            }



            ViewData["TicketPriorityId"] = new SelectList(await _lookUpService.GetTicketPrioritiesAsync(), "Id", "Name");
            ViewData["TicketTypeId"] = new SelectList(await _lookUpService.GetTicketTypesAsync(), "Id", "Name");

            return View();
        }

        // POST: Tickets/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId,OwnerUserId,DeveloperUserId")] Ticket ticket)
        {
            AppUser appUser = await _userManager.GetUserAsync(User);
            string userId = _userManager.GetUserId(User);
            ModelState.Remove("OwnerUserId");

            if (ModelState.IsValid)
            {
                try
                {
                    ticket.Created = DateTime.UtcNow;
                    ticket.OwnerUserId = userId;

                    ticket.TicketStatusId = (await _ticketService.LookupTicketStatusIdAsync(nameof(EnumTicketStatus.New))).Value;

                    await _ticketService.AddNewTicketAsync(ticket);

                    //Ticket Hisotry
                    Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);
                    await _ticketHistoryService.AddHistoryAsync(null!, newTicket, appUser.Id);

                    //Ticket Notification


                }
                catch (Exception)
                {

                    throw;
                }

                return RedirectToAction(nameof(AllTickets));
            }

            if (User.IsInRole(nameof(AppRole.Admin)))
            {
                ViewData["ProjectId"] = new SelectList(await _projectService.GetAllProjectsByCompanyAsync(appUser.CompanyId), "Id", "Name");
            }
            else
            {
                ViewData["ProjectId"] = new SelectList(await _projectService.GetUserProjectsAsync(appUser.Id), "Id", "Name");
            }



            ViewData["TicketPriorityId"] = new SelectList(await _lookUpService.GetTicketPrioritiesAsync(), "Id", "Name");
            ViewData["TicketTypeId"] = new SelectList(await _lookUpService.GetTicketTypesAsync(), "Id", "Name");

            return View(ticket);
        }

        // GET: Tickets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);

            if (ticket == null)
            {
                return NotFound();
            }

            ViewData["TicketPriorityId"] = new SelectList(await _lookUpService.GetTicketPrioritiesAsync(), "Id", "Name", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(await _lookUpService.GetTicketStatusesAsync(), "Id", "Name", ticket.TicketStatusId);
            ViewData["TicketTypeId"] = new SelectList(await _lookUpService.GetTicketTypesAsync(), "Id", "Name", ticket.TicketTypeId);

            return View(ticket);
        }

        // POST: Tickets/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Created,Updated,Archived,ArchivedByProject,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId,OwnerUserId,DeveloperUserId")] Ticket ticket)
        {
            if (id != ticket.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                AppUser appUser = await _userManager.GetUserAsync(User);
                Ticket oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id); 
                try
                {
                    ticket.Updated = DateTime.UtcNow;
                    await _ticketService.UpdateTicketAsync(ticket);
                    
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketExists(ticket.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);
                await _ticketHistoryService.AddHistoryAsync(oldTicket, newTicket, appUser.Id);

                return RedirectToAction(nameof(AllTickets));
            }
            ViewData["TicketPriorityId"] = new SelectList(await _lookUpService.GetTicketPrioritiesAsync(), "Id", "Name", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(await _lookUpService.GetTicketStatusesAsync(), "Id", "Name", ticket.TicketStatusId);
            ViewData["TicketTypeId"] = new SelectList(await _lookUpService.GetTicketTypesAsync(), "Id", "Name", ticket.TicketTypeId);
            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTicketAttachment([Bind("Id,FormFile,Description,TicketId")] TicketAttatchment ticketAttachment)
        {
            string statusMessage;

            if (ModelState.IsValid && ticketAttachment.FormFile != null)
            {
                ticketAttachment.FileData = await _fileService.ConvertFileToByteArrayAsync(ticketAttachment.FormFile);
                ticketAttachment.FileName = ticketAttachment.FormFile.FileName;
                ticketAttachment.FileType = ticketAttachment.FormFile.ContentType;

                ticketAttachment.Created = DateTimeOffset.Now;
                ticketAttachment.UserId = _userManager.GetUserId(User);

                await _ticketService.AddTicketAttachmentAsync(ticketAttachment);
                statusMessage = "Success: New attachment added to Ticket.";
            }
            else
            {
                statusMessage = "Error: Invalid data.";

            }

            return RedirectToAction("Details", new { id = ticketAttachment.TicketId, message = statusMessage });
        }

        // GET: Tickets/Archive/5
        public async Task<IActionResult> Archive(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            int companyId = User.Identity.GetCompanyId();
            var ticket = await _ticketService.GetTicketByIdAsync(id.Value);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // POST: Tickets/Archive/5
        [HttpPost, ActionName("Archive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveConfirmed(int id)
        {
            int companyId = User.Identity.GetCompanyId();
            var ticket = await _ticketService.GetTicketByIdAsync(id);

            await _ticketService.ArchiveTicketAsync(ticket);

            return RedirectToAction(nameof(AllTickets));
        }

        // GET: Projects/Restore/5
        public async Task<IActionResult> Restore(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            int companyId = User.Identity.GetCompanyId();
            var project = await _ticketService.GetTicketByIdAsync(id.Value);


            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Projects/Restore/5
        [HttpPost, ActionName("Restore")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreConfirmed(int id)
        {
            int companyId = User.Identity.GetCompanyId();
            var ticket = await _ticketService.GetTicketByIdAsync(id);

            await _ticketService.RestoreTicketAsync(ticket);

            return RedirectToAction(nameof(AllTickets));
        }



        private bool TicketExists(int id)
        {
            return _context.Tickets.Any(e => e.Id == id);
        }

        //private async Task<bool> TicketExists(int id)
        //{
        //    int companyId = User.Identity.GetCompanyId();

        //    return (await _ticketService.GetAllTicketsByCompanyAsync(companyId)).Any(t => t.Id == id);
        //}
    }
}
