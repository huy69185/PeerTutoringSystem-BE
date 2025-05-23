﻿using Microsoft.EntityFrameworkCore;
using PeerTutoringSystem.Domain.Entities.Skills;
using PeerTutoringSystem.Domain.Interfaces.Skills;
using PeerTutoringSystem.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PeerTutoringSystem.Infrastructure.Repositories.Skills
{
    public class SkillRepository : ISkillRepository
    {
        private readonly AppDbContext _context;

        public SkillRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Skill> AddAsync(Skill skill)
        {
            await _context.Skills.AddAsync(skill);
            await _context.SaveChangesAsync();
            return skill;
        }

        public async Task<Skill> GetByIdAsync(Guid skillId)
        {
            return await _context.Skills.FindAsync(skillId);
        }

        public async Task<Skill> GetByNameAsync(string skillName)
        {
            return await _context.Skills
                .FirstOrDefaultAsync(s => s.SkillName == skillName);
        }

        public async Task<IEnumerable<Skill>> GetAllAsync()
        {
            return await _context.Skills.ToListAsync();
        }

        public async Task<Skill> UpdateAsync(Skill skill)
        {
            _context.Skills.Update(skill);
            await _context.SaveChangesAsync();
            return skill;
        }

        public async Task DeleteAsync(Guid skillId)
        {
            var skill = await _context.Skills.FindAsync(skillId);
            if (skill != null)
            {
                _context.Skills.Remove(skill);
                await _context.SaveChangesAsync();
            }
        }
    }
}