using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Authorization;

namespace AttendanceSystem.Controllers
{
    [Authorize]
    public class SubjectsController : Controller
    {
        private readonly AppDbContext _context;

        public SubjectsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Subjects
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var appDbContext = _context.Subjects.Include(s => s.Professor);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Subjects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subject = await _context.Subjects
                .Include(s => s.Professor)
                .FirstOrDefaultAsync(m => m.SubjectID == id);
            if (subject == null)
            {
                return NotFound();
            }

            return View(subject);
        }

        // GET: Subjects/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["ProfessorID"] = new SelectList(_context.Users, "Id", "Email");
            return View();
        }

        // POST: Subjects/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SubjectID,SubjectName,ProfessorID")] Subject subject)
        {
            if (!ModelState.IsValid)
            {
                var professorExists = await _context.Users.AnyAsync(u => u.Id == subject.ProfessorID);
                if (!professorExists)
                {
                    ModelState.AddModelError("ProfessorID", "Selected professor does not exist.");
                    ViewData["ProfessorID"] = new SelectList(_context.Users, "Id", "Email",subject.ProfessorID);
                    return View(subject);
                }
                _context.Add(subject);
                await _context.SaveChangesAsync();
                return RedirectToAction("Dashboard");
            }
            ViewData["ProfessorID"] = new SelectList(_context.Users, "Id", "Email", subject.ProfessorID);
            return View(subject);
        }

        // GET: Subjects/Edit/5

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
            {
                return NotFound();
            }
            ViewData["ProfessorID"] = new SelectList(_context.Users, "Id", "Email", subject.ProfessorID);
            return View(subject);
        }

        // POST: Subjects/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("SubjectID,SubjectName,ProfessorID")] Subject subject)
        {
            if (id != subject.SubjectID)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                try
                {
                    _context.Update(subject);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubjectExists(subject.SubjectID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Dashboard));
            }
            ViewData["ProfessorID"] = new SelectList(_context.Users, "Id", "Email", subject.ProfessorID);
            return View(subject);
        }


        // GET: Subjects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subject = await _context.Subjects
                .Include(s => s.Professor)
                .FirstOrDefaultAsync(m => m.SubjectID == id);
            if (subject == null)
            {
                return NotFound();
            }

            return View(subject);
        }

        // POST: Subjects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject != null)
            {
                _context.Subjects.Remove(subject);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Dashboard));
        }


        private bool SubjectExists(int id)
        {
            return _context.Subjects.Any(e => e.SubjectID == id);
        }
    }
}
