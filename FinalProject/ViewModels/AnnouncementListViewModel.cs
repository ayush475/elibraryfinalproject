using FinalProject.Models;
using System.Collections.Generic;

namespace FinalProject.ViewModels
{
    // ViewModel to carry a list of Announcements and pagination information
    public class AnnouncementListViewModel
    {
        public IEnumerable<Announcement> Announcements { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }

    }
    }
