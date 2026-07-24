using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities.HR;

namespace OmniFlow.Application.Employees.Queries
{
    public class TrainingCourseDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "Mandatory Compliance";
        public int DurationHours { get; set; }
        public bool IsMandatory { get; set; }
        public string Status { get; set; } = "NotStarted";
        public DateTime? CompletedAt { get; set; }
        public int Score { get; set; }
    }

    public record GetMyTrainingsQuery(Guid UserId) : IRequest<List<TrainingCourseDto>>;

    public class GetMyTrainingsQueryHandler : IRequestHandler<GetMyTrainingsQuery, List<TrainingCourseDto>>
    {
        private readonly IOmniFlowDbContext _context;

        public GetMyTrainingsQueryHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<List<TrainingCourseDto>> Handle(GetMyTrainingsQuery request, CancellationToken cancellationToken)
        {
            var courses = await _context.TrainingCourses
                .AsNoTracking()
                .Where(t => t.EmployeeUserId == request.UserId)
                .OrderBy(t => t.Status)
                .Select(t => new TrainingCourseDto
                {
                    Id = t.Id,
                    EmployeeUserId = t.EmployeeUserId,
                    Title = t.Title,
                    Description = t.Description,
                    Category = t.Category,
                    DurationHours = t.DurationHours,
                    IsMandatory = t.IsMandatory,
                    Status = t.Status.ToString(),
                    CompletedAt = t.CompletedAt,
                    Score = t.Score
                })
                .ToListAsync(cancellationToken);

            return courses;
        }
    }

    public class CompleteTrainingCourseCommand : IRequest<bool>
    {
        public Guid CourseId { get; set; }
        public int Score { get; set; } = 100;
    }

    public class CompleteTrainingCourseCommandHandler : IRequestHandler<CompleteTrainingCourseCommand, bool>
    {
        private readonly IOmniFlowDbContext _context;

        public CompleteTrainingCourseCommandHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(CompleteTrainingCourseCommand request, CancellationToken cancellationToken)
        {
            var course = await _context.TrainingCourses.FirstOrDefaultAsync(t => t.Id == request.CourseId, cancellationToken);
            if (course == null) return false;

            course.Status = TrainingStatus.Completed;
            course.CompletedAt = DateTime.UtcNow;
            course.Score = request.Score;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
