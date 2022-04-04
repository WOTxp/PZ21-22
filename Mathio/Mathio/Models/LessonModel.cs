using System.ComponentModel.DataAnnotations;

namespace Mathio.Models;

public class LessonModel
{
    public string ID { get; set; }
    [Required]
    public string Content { get; set; }
}