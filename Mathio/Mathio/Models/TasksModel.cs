using System.ComponentModel.DataAnnotations;

namespace Mathio.Models;

public class TasksModel
{
    public string ID { get; set; }
    [Required]
    public string Author { get; set; }
    [Required]
    public string Title { get; set; }
    public string Description { get; set; }
}