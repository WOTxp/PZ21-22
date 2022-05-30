using System.ComponentModel.DataAnnotations;

namespace Mathio.Models;

public class QuestionAnswerModel
{
    public string? QuestionId { get; set;}
    public string? Answer { get; init; }
}