using System.ComponentModel.DataAnnotations;

namespace Mathio.Models;

public class QuestionModel
{
    public string ID { get; set; }
    [Required]
    public string Question { get; set; }
    [Required]
    public string Type { get; set; }
    public List<string> Answers { get; set; }
    [Required]
    public string CorrectAnswer { get; set; }
}