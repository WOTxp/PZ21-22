using System.ComponentModel.DataAnnotations;

namespace Mathio.Models;

public class NotesModel
{
    public string ID { get; set; }
    [Required]
    public string Task { get; set; }
    [Required]
    public string Content { get; set; }
}