using System.ComponentModel.DataAnnotations;

namespace Mathio.Models;

public class TasksFinModel
{
    public string ID { get; set; }
    [Required]
    public string Task { get; set; }
    public int Score { get; set; }
}