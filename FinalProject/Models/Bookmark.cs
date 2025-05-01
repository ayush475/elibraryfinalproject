
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    // Represents a bookmark made by a member for a book.
    public class Bookmark
    {
        // Primary key for the Bookmark entity.
        [Key]
        public int BookmarkId { get; set; }

        // Foreign key for the Member who created the bookmark. Required.
        [Required]
        public int MemberId { get; set; }

        // Foreign key for the Book that was bookmarked. Required.
        [Required]
        public int BookId { get; set; }

        // Date and time the bookmark was added.
        [DataType(DataType.DateTime)]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // Navigation properties

        // The Member who created the bookmark.
        [ForeignKey("MemberId")]
        public virtual required Member Member { get; set; }

        // The Book that was bookmarked.
        [ForeignKey("BookId")]
        public virtual  required Book Book { get; set; }
    }
}