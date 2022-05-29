using System.ComponentModel.DataAnnotations;

namespace Mathio.Models;

public class QuestionAnswerModel
{
    [Required]
    public string? QuestionId;
    [Required]
    public string Answer;
}